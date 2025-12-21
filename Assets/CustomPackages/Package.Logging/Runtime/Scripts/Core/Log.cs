namespace Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Core
{
    public static class Log
    {
        [UnityEngine.Scripting.Preserve]
        public class Unity : LogCategory<Unity>
        {
        }

        [UnityEngine.Scripting.Preserve]
        public class Editor : LogCategory<Editor>
        {
        }

        [UnityEngine.Scripting.Preserve]
        public class Rewards : LogCategory<Rewards>
        {
        }

        [UnityEngine.Scripting.Preserve]
        public class Gameplay : LogCategory<Gameplay>
        {
        }
        
        [UnityEngine.Scripting.Preserve]
        public class Sounds : LogCategory<Gameplay>
        {
        }

        [UnityEngine.Scripting.Preserve]
        public class Addressables : LogCategory<Addressables>
        {
        }

        [UnityEngine.Scripting.Preserve]
        public class GlobalEvent : LogCategory<GlobalEvent>
        {
        }
        [UnityEngine.Scripting.Preserve]
        public class UI : LogCategory<UI>
        {
        }

        [UnityEngine.Scripting.Preserve]
        public class AutoBattle : LogCategory<AutoBattle>
        {
        }

        [UnityEngine.Scripting.Preserve]
        public class Audio : LogCategory<Audio>
        {
        }

        [UnityEngine.Scripting.Preserve]
        public class SaveSystem : LogCategory<SaveSystem>
        {
        }
    }
}