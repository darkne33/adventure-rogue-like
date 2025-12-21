using System;

namespace Core
{
    public static class SceneNames
    {
        public const string BootstrapScene = "Bootstrap";
        public const string GameScene = "Game";
        public const string TestGameScene = "TestGame";

        public static string GetSceneNameByType(SceneNameType sceneNameType)
        {
            return sceneNameType switch
            {
                SceneNameType.GameScene => GameScene,
                SceneNameType.TestGameScene => TestGameScene,
                SceneNameType.BootstrapScene => BootstrapScene,
                _ => throw new Exception("Unknown scene name: " + sceneNameType)
            };
        }

        public enum SceneNameType
        {
            BootstrapScene,
            GameScene,
            TestGameScene
        }
    }
}