"use strict";

var LEADERBOARD_STATISTIC_NAME = "RoomsVisited";
var RUN_STATE_KEY = "leaderboardRunStateV2";
var RUN_LIFETIME_MS = 12 * 60 * 60 * 1000;
var MIN_ROOM_INTERVAL_MS = 1000;
var MAX_ROOMS_PER_RUN = 1000;

handlers.StartLeaderboardRun = function (args, context) {
    var now = Date.now();
    var state = {
        version: 2,
        status: "active",
        runId: createRunId(now),
        visitedRooms: 0,
        startedAt: now,
        lastAcceptedAt: 0,
        expiresAt: now + RUN_LIFETIME_MS,
        clientVersion: args && typeof args.clientVersion === "string"
            ? args.clientVersion.substring(0, 32)
            : "unknown"
    };

    saveRunState(state);
    return {
        runId: state.runId,
        expiresAt: state.expiresAt
    };
};

handlers.VisitLeaderboardRoom = function (args, context) {
    if (!args || typeof args.runId !== "string" || args.runId.length === 0 || args.runId.length > 96)
        throw new Error("InvalidRunId");

    if (!isPositiveInteger(args.roomSequence))
        throw new Error("InvalidRoomSequence");

    var state = loadRunState();
    if (!state || state.version !== 2)
        throw new Error("LeaderboardRunNotStarted");

    if (state.status !== "active" || state.runId !== args.runId)
        throw new Error("LeaderboardRunMismatch");

    var now = Date.now();
    if (now > state.expiresAt)
        throw new Error("LeaderboardRunExpired");

    if (args.roomSequence > MAX_ROOMS_PER_RUN)
        throw new Error("LeaderboardRoomLimitExceeded");

    if (args.roomSequence <= state.visitedRooms) {
        updateLeaderboardStatistic(state.visitedRooms);
        return {
            accepted: false,
            duplicate: true,
            roomsVisited: state.visitedRooms
        };
    }

    if (args.roomSequence !== state.visitedRooms + 1)
        throw new Error("LeaderboardRoomSequenceSkipped");

    if (state.visitedRooms > 0 && now - state.lastAcceptedAt < MIN_ROOM_INTERVAL_MS)
        throw new Error("LeaderboardRoomVisitedTooQuickly");

    state.visitedRooms = args.roomSequence;
    state.lastAcceptedAt = now;

    // The statistic uses Maximum aggregation. Updating it first makes a retry safe
    // if saving the internal run state fails after the statistic request succeeds.
    updateLeaderboardStatistic(state.visitedRooms);
    saveRunState(state);

    return {
        accepted: true,
        duplicate: false,
        roomsVisited: state.visitedRooms
    };
};

function createRunId(now) {
    var randomPart = Math.floor(Math.random() * 0x7fffffff).toString(36);
    return currentPlayerId + "-" + now.toString(36) + "-" + randomPart;
}

function isPositiveInteger(value) {
    return typeof value === "number" && isFinite(value) &&
        Math.floor(value) === value && value > 0;
}

function loadRunState() {
    var result = server.GetUserInternalData({
        PlayFabId: currentPlayerId,
        Keys: [RUN_STATE_KEY]
    });

    if (!result.Data || !result.Data[RUN_STATE_KEY] ||
        !result.Data[RUN_STATE_KEY].Value)
        return null;

    try {
        return JSON.parse(result.Data[RUN_STATE_KEY].Value);
    } catch (error) {
        log.error("Invalid leaderboard run state", { error: error });
        return null;
    }
}

function saveRunState(state) {
    var data = {};
    data[RUN_STATE_KEY] = JSON.stringify(state);
    server.UpdateUserInternalData({
        PlayFabId: currentPlayerId,
        Data: data
    });
}

function updateLeaderboardStatistic(score) {
    server.UpdatePlayerStatistics({
        PlayFabId: currentPlayerId,
        ForceUpdate: false,
        Statistics: [{
            StatisticName: LEADERBOARD_STATISTIC_NAME,
            Value: score
        }]
    });
}
