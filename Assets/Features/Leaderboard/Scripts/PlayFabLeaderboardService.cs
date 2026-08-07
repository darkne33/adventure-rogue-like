using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

namespace Features.Leaderboard
{
    public sealed class PlayFabLeaderboardService : ILeaderboardService
    {
        public const string RoomsVisitedStatistic = "RoomsVisited";

        private const int MaxResultsCount = 10;
        private const int MinimumPlayerNameLength = 3;
        private const int MaximumPlayerNameLength = 25;
        private const string StartRunFunctionName = "StartLeaderboardRun";
        private const string VisitRoomFunctionName = "VisitLeaderboardRoom";
        private const string CustomIdPlayerPrefsKey = "little_rush.playfab.custom_id";
        private const string PlayerNamePlayerPrefsKey = "little_rush.playfab.player_name";

        public bool IsConfigured => string.IsNullOrWhiteSpace(PlayFabSettings.TitleId) == false;
        public string PlayerId { get; private set; }
        public string PlayerName { get; private set; }
        public string StatisticName => RoomsVisitedStatistic;

        private readonly string _customId;
        private readonly SemaphoreSlim _loginGate = new(1, 1);
        private readonly SemaphoreSlim _runGate = new(1, 1);

        private bool _isLoggedIn;
        private bool _displayNameSynchronized;
        private string _activeRunId;

        public PlayFabLeaderboardService()
        {
            _customId = LoadOrCreateCustomId();
            PlayerName = LoadOrCreatePlayerName(_customId);
        }

        public async UniTask<IReadOnlyList<LeaderboardEntry>> GetTop(
            CancellationToken cancellationToken)
        {
            var request = new GetLeaderboardRequest
            {
                StatisticName = RoomsVisitedStatistic,
                StartPosition = 0,
                MaxResultsCount = MaxResultsCount
            };

            GetLeaderboardResult result = await ExecuteAuthenticatedRequest<GetLeaderboardResult>(
                (onSuccess, onError) =>
                    PlayFabClientAPI.GetLeaderboard(request, onSuccess, onError),
                cancellationToken);

            var entries = new List<LeaderboardEntry>(result.Leaderboard?.Count ?? 0);
            if (result.Leaderboard == null)
                return entries;

            foreach (PlayerLeaderboardEntry entry in result.Leaderboard)
            {
                string displayName = string.IsNullOrWhiteSpace(entry.DisplayName)
                    ? "PLAYER"
                    : entry.DisplayName;
                entries.Add(new LeaderboardEntry(entry.Position + 1, entry.PlayFabId,
                    displayName, entry.StatValue));
            }

            return entries;
        }

        public async UniTask SetPlayerName(string playerName,
            CancellationToken cancellationToken)
        {
            string normalizedName = NormalizePlayerName(playerName);
            if (string.Equals(PlayerName, normalizedName, StringComparison.Ordinal))
                return;

            var request = new UpdateUserTitleDisplayNameRequest
            {
                DisplayName = normalizedName
            };

            await ExecuteAuthenticatedRequest<UpdateUserTitleDisplayNameResult>(
                (onSuccess, onError) =>
                    PlayFabClientAPI.UpdateUserTitleDisplayName(request, onSuccess, onError),
                cancellationToken);

            PlayerName = normalizedName;
            _displayNameSynchronized = true;
            PlayerPrefs.SetString(PlayerNamePlayerPrefsKey, PlayerName);
            PlayerPrefs.Save();
        }

        public async UniTask StartRun(CancellationToken cancellationToken)
        {
            await _runGate.WaitAsync(cancellationToken);
            try
            {
                _activeRunId = null;
                await StartRunOnServer(cancellationToken);
            }
            finally
            {
                _runGate.Release();
            }
        }

        public async UniTask ReportRoomVisited(int roomSequence,
            CancellationToken cancellationToken)
        {
            if (roomSequence <= 0)
                throw new ArgumentOutOfRangeException(nameof(roomSequence));

            await EnsureRunStarted(cancellationToken);

            VisitRoomResponse response = await ExecuteCloudScript<VisitRoomResponse>(
                VisitRoomFunctionName,
                new
                {
                    runId = _activeRunId,
                    roomSequence
                },
                cancellationToken);

            if (response.RoomsVisited < roomSequence)
                throw new PlayFabCloudScriptException(
                    "CloudScript returned an invalid visited-room count.");
        }

        private async UniTask EnsureRunStarted(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_activeRunId) == false)
                return;

            await _runGate.WaitAsync(cancellationToken);
            try
            {
                if (string.IsNullOrWhiteSpace(_activeRunId))
                    await StartRunOnServer(cancellationToken);
            }
            finally
            {
                _runGate.Release();
            }
        }

        private async UniTask StartRunOnServer(CancellationToken cancellationToken)
        {
            StartRunResponse response = await ExecuteCloudScript<StartRunResponse>(
                StartRunFunctionName,
                new
                {
                    clientVersion = Application.version
                },
                cancellationToken);

            if (string.IsNullOrWhiteSpace(response.RunId))
                throw new PlayFabCloudScriptException(
                    "CloudScript did not return a leaderboard run ID.");

            _activeRunId = response.RunId;
        }

        private async UniTask<T> ExecuteCloudScript<T>(string functionName,
            object functionParameter, CancellationToken cancellationToken)
        {
            var request = new ExecuteCloudScriptRequest
            {
                FunctionName = functionName,
                FunctionParameter = functionParameter,
                GeneratePlayStreamEvent = false,
                RevisionSelection = CloudScriptRevisionOption.Live
            };

            ExecuteCloudScriptResult result =
                await ExecuteAuthenticatedRequest<ExecuteCloudScriptResult>(
                    (onSuccess, onError) =>
                        PlayFabClientAPI.ExecuteCloudScript(request, onSuccess, onError),
                    cancellationToken);

            if (result.Error != null)
                throw new PlayFabCloudScriptException(result.Error);

            if (result.FunctionResult == null)
                throw new PlayFabCloudScriptException(
                    $"CloudScript function {functionName} returned no result.");

            string json = JsonConvert.SerializeObject(result.FunctionResult);
            T response = JsonConvert.DeserializeObject<T>(json);
            if (response == null)
                throw new PlayFabCloudScriptException(
                    $"CloudScript function {functionName} returned an invalid result.");

            return response;
        }

        private async UniTask EnsureLoggedIn(CancellationToken cancellationToken)
        {
            if (IsConfigured == false)
                throw new InvalidOperationException(
                    "PlayFab Title ID is not configured in PlayFabSharedSettings.");

            if (_isLoggedIn && PlayFabClientAPI.IsClientLoggedIn() &&
                _displayNameSynchronized)
                return;

            await _loginGate.WaitAsync(cancellationToken);
            try
            {
                if (_isLoggedIn == false || PlayFabClientAPI.IsClientLoggedIn() == false)
                    await Login(cancellationToken);

                if (_displayNameSynchronized == false)
                    await UpdateDisplayName(PlayerName, cancellationToken);

                _displayNameSynchronized = true;
            }
            finally
            {
                _loginGate.Release();
            }
        }

        private async UniTask Login(CancellationToken cancellationToken)
        {
            var request = new LoginWithCustomIDRequest
            {
                TitleId = PlayFabSettings.TitleId,
                CustomId = _customId,
                CreateAccount = true,
                InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
                {
                    GetPlayerProfile = true
                }
            };

            LoginResult result = await ExecuteRequest<LoginResult>(
                (onSuccess, onError) =>
                    PlayFabClientAPI.LoginWithCustomID(request, onSuccess, onError),
                cancellationToken);

            PlayerId = result.PlayFabId;
            _isLoggedIn = true;
            string remoteDisplayName = result.InfoResultPayload?.PlayerProfile?.DisplayName;
            _displayNameSynchronized = string.Equals(remoteDisplayName, PlayerName,
                StringComparison.Ordinal);
        }

        private static async UniTask UpdateDisplayName(string playerName,
            CancellationToken cancellationToken)
        {
            var request = new UpdateUserTitleDisplayNameRequest
            {
                DisplayName = playerName
            };

            await ExecuteRequest<UpdateUserTitleDisplayNameResult>(
                (onSuccess, onError) =>
                    PlayFabClientAPI.UpdateUserTitleDisplayName(request, onSuccess, onError),
                cancellationToken);
        }

        private static async UniTask<T> ExecuteRequest<T>(
            Action<Action<T>, Action<PlayFabError>> startRequest,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var completionSource = new UniTaskCompletionSource<T>();

            using (cancellationToken.Register(() =>
                       completionSource.TrySetCanceled(cancellationToken)))
            {
                startRequest(
                    result => completionSource.TrySetResult(result),
                    error => completionSource.TrySetException(
                        new PlayFabLeaderboardException(error)));

                return await completionSource.Task;
            }
        }

        private async UniTask<T> ExecuteAuthenticatedRequest<T>(
            Action<Action<T>, Action<PlayFabError>> startRequest,
            CancellationToken cancellationToken)
        {
            try
            {
                await EnsureLoggedIn(cancellationToken);
                return await ExecuteRequest(startRequest, cancellationToken);
            }
            catch (PlayFabLeaderboardException exception)
                when (IsAuthenticationError(exception))
            {
                ResetAuthentication();
                await EnsureLoggedIn(cancellationToken);
                return await ExecuteRequest(startRequest, cancellationToken);
            }
        }

        private static bool IsAuthenticationError(PlayFabLeaderboardException exception) =>
            exception.Error?.Error is PlayFabErrorCode.NotAuthenticated or
                PlayFabErrorCode.InvalidSessionTicket;

        private void ResetAuthentication()
        {
            PlayFabClientAPI.ForgetAllCredentials();
            PlayerId = null;
            _isLoggedIn = false;
            _displayNameSynchronized = false;
        }

        private sealed class StartRunResponse
        {
            [JsonProperty("runId")]
            public string RunId { get; set; }
        }

        private sealed class VisitRoomResponse
        {
            [JsonProperty("roomsVisited")]
            public int RoomsVisited { get; set; }
        }

        private static string NormalizePlayerName(string playerName)
        {
            string normalizedName = (playerName ?? string.Empty)
                .Replace('\r', ' ')
                .Replace('\n', ' ')
                .Trim();
            if (normalizedName.Length < MinimumPlayerNameLength)
                throw new ArgumentException(
                    $"Player name must contain at least {MinimumPlayerNameLength} characters.",
                    nameof(playerName));

            if (normalizedName.Length > MaximumPlayerNameLength)
                normalizedName = normalizedName.Substring(0, MaximumPlayerNameLength);

            return normalizedName;
        }

        private static string LoadOrCreateCustomId()
        {
            string customId = PlayerPrefs.GetString(CustomIdPlayerPrefsKey, string.Empty);
            if (string.IsNullOrWhiteSpace(customId) == false)
                return customId;

            customId = Guid.NewGuid().ToString("N");
            PlayerPrefs.SetString(CustomIdPlayerPrefsKey, customId);
            PlayerPrefs.Save();
            return customId;
        }

        private static string LoadOrCreatePlayerName(string customId)
        {
            string playerName = PlayerPrefs.GetString(PlayerNamePlayerPrefsKey, string.Empty);
            if (string.IsNullOrWhiteSpace(playerName) == false)
                return playerName;

            string suffix = customId.Substring(Math.Max(0, customId.Length - 6))
                .ToUpperInvariant();
            playerName = $"PLAYER {suffix}";
            PlayerPrefs.SetString(PlayerNamePlayerPrefsKey, playerName);
            PlayerPrefs.Save();
            return playerName;
        }
    }

    public sealed class PlayFabLeaderboardException : Exception
    {
        public PlayFabError Error { get; }

        public PlayFabLeaderboardException(PlayFabError error)
            : base(error?.GenerateErrorReport() ?? "Unknown PlayFab error")
        {
            Error = error;
        }

        public string GetUserMessage()
        {
            if (Error == null)
                return "PLAYFAB REQUEST FAILED";

            return Error.Error switch
            {
                PlayFabErrorCode.PlayerCreationDisabled => "ACCOUNT CREATION IS DISABLED",
                PlayFabErrorCode.InvalidTitleId => "CHECK PLAYFAB TITLE ID",
                PlayFabErrorCode.AccountNotFound => "PLAYFAB ACCOUNT NOT FOUND",
                PlayFabErrorCode.NotAuthenticated => "PLAYFAB LOGIN REQUIRED",
                PlayFabErrorCode.ServiceUnavailable => "PLAYFAB IS UNAVAILABLE",
                _ => string.IsNullOrWhiteSpace(Error.ErrorMessage)
                    ? "PLAYFAB REQUEST FAILED"
                    : Error.ErrorMessage.ToUpperInvariant()
            };
        }
    }

    public sealed class PlayFabCloudScriptException : Exception
    {
        public PlayFabCloudScriptException(string message)
            : base(message)
        {
        }

        public PlayFabCloudScriptException(ScriptExecutionError error)
            : base(error == null
                ? "Unknown CloudScript error"
                : $"{error.Error}: {error.Message}")
        {
        }
    }
}
