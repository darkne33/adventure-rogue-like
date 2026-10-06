#if UNITY_EDITOR
using Features.Quests.Scripts;
using UnityEditor;
using UnityEngine;
using Zenject;

namespace Features.Quests.Editor
{
    public static class ProgressionTools
    {
        private const string MenuPath = "Tools/Little Rush/Progression/Complete All Quests and Unlock All";

        [MenuItem(MenuPath)]
        private static void CompleteAllQuestsAndUnlockAll()
        {
            if (EditorApplication.isPlaying)
            {
                QuestService service = ProjectContext.HasInstance
                    ? ProjectContext.Instance.Container?.TryResolve<QuestService>()
                    : null;
                if (service == null || service.Configuration == null)
                {
                    Debug.LogWarning("Quest progression is not initialized yet. Use this command after game bootstrap finishes.");
                    return;
                }

                CompleteProgression(service);
                return;
            }

            ProgressionConfiguration configuration =
                AssetDatabase.LoadAssetAtPath<ProgressionConfiguration>(ProgressionConfiguration.Address);
            if (configuration == null)
            {
                Debug.LogError($"Could not load progression configuration at {ProgressionConfiguration.Address}.");
                return;
            }

            using (var service = new QuestService(new PlayerWallet()))
            {
                service.Initialize(configuration);
                CompleteProgression(service);
            }
        }

        [MenuItem(MenuPath, true)]
        private static bool CanCompleteProgression() =>
            !EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPlaying;

        private static void CompleteProgression(QuestService service)
        {
            service.CompleteAllQuestsAndUnlockAllForEditor();
            Debug.Log($"Completed all {service.TotalCount} quests and unlocked all {service.Unlocks.Count} catalog entries. " +
                      "Progress is saved; quest silver rewards remain available through CLAIM.");
        }
    }
}
#endif
