using System;
using System.Collections.Generic;
using System.Linq;
using Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Configs;
using Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Core;
using Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Data;
using Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Factories;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Package.Logging.Editor.CustomPackages.Package.Logging.Editor.Scripts.Windows
{
    public class LoggingWindow : EditorWindow
    {
        [SerializeField] private VisualTreeAsset _scrollVisualTreeAsset;
        [SerializeField] private VisualTreeAsset _categoryVisualTreeAsset;
        [SerializeField] private VisualTreeAsset _buttonVisualTreeAsset;
        [SerializeField] private VisualTreeAsset _labelVisualTreeAsset;

        private readonly List<(LoggingData, DropdownField)> _dropdownFields = new();

        [MenuItem("Tools/Logging Window", false, 2)]
        public static void OpenWindow()
        {
            var window = GetWindow<LoggingWindow>();
            window.titleContent = new GUIContent("Logger Window");
        }

        [DidReloadScripts]
        private static void OnAfterAssemblyReload()
        {
            SyncCategories();
        }

        public void CreateGUI()
        {
            _scrollVisualTreeAsset.CloneTree(rootVisualElement);
            _dropdownFields.Clear();

            var scrollView = rootVisualElement.Q<ScrollView>("scroll-view");
            var categories = LoggingConfig.Instance.Data;
            scrollView.Add(MakeLabel("CATEGORIES"));

            foreach (var categoryView in categories.Select(MakeCategoryDropdown))
            {
                scrollView.Add(categoryView);
            }

            scrollView.Add(MakeLabel("CONFIGURATION"));
            scrollView.Add(MakeButton("ALL VERBOSE", () => OnLogLevel(LogLevel.Verbose)));
            scrollView.Add(MakeButton("ALL DEBUG", () => OnLogLevel(LogLevel.Debug)));
            scrollView.Add(MakeButton("ALL INFO", () => OnLogLevel(LogLevel.Info)));
            scrollView.Add(MakeButton("ALL WARN", () => OnLogLevel(LogLevel.Warn)));
            scrollView.Add(MakeButton("ALL ERROR", () => OnLogLevel(LogLevel.Error)));
            scrollView.Add(MakeButton("DISABLE ALL LOGS", () => OnLogLevel(LogLevel.None)));
            scrollView.Add(MakeButton("TEST LOGS", OnTestLogs));
            scrollView.Add(MakeLabel("SETTINGS"));
            scrollView.Add(MakeCredentialsDropdown());
        }

        private TemplateContainer MakeCategoryDropdown(LoggingData data)
        {
            var choices = Enum.GetNames(typeof(LogLevel)).ToList();
            var visual = _categoryVisualTreeAsset.Instantiate();
            rootVisualElement.Add(visual);
            var dropdownField = visual.contentContainer.Q<DropdownField>("category-dropdown-view");
            dropdownField.label = data.CategoryName;
            dropdownField.choices = choices;
            dropdownField.index = (int)data.LogLevel;
            dropdownField.RegisterValueChangedCallback(c => OnChangeCategory(c.newValue, data));

            _dropdownFields.Add((data, dropdownField));

            return visual;
        }

        private TemplateContainer MakeCredentialsDropdown()
        {
            var choices = Enum.GetNames(typeof(LoggingCredentials)).ToList();
            var visual = _categoryVisualTreeAsset.Instantiate();
            rootVisualElement.Add(visual);
            var dropdownField = visual.contentContainer.Q<DropdownField>("category-dropdown-view");
            dropdownField.label = "Logging Credentials";
            dropdownField.choices = choices;
            dropdownField.index = (int)LoggingConfig.Instance.Credentials;
            dropdownField.RegisterValueChangedCallback(c => OnChangeCredentials(c.newValue));

            return visual;
        }

        private TemplateContainer MakeButton(string nameButton, Action action)
        {
            var visual = _buttonVisualTreeAsset.Instantiate();
            rootVisualElement.Add(visual);
            var button = visual.contentContainer.Q<Button>("button-view");
            button.text = nameButton;
            button.clickable.clicked += () => action?.Invoke();

            return visual;
        }

        private TemplateContainer MakeLabel(string nameLabel)
        {
            var visual = _labelVisualTreeAsset.Instantiate();
            rootVisualElement.Add(visual);
            var label = visual.contentContainer.Q<Label>("label-view");
            label.text = nameLabel;

            return visual;
        }

        private static void SyncCategories()
        {
            if (LoggingConfig.Instance == null)
            {
                return;
            }

            var allCategories = GetAllCategories();
            var currentCategories = LoggingConfig.Instance.Data;
            var updateCategories = new List<LoggingData>();

            foreach (var category in allCategories)
            {
                var isContains = false;

                foreach (var current in currentCategories)
                {
                    if (current.CategoryName == category)
                    {
                        isContains = true;
                        updateCategories.Add(current);

                        break;
                    }
                }

                if (!isContains)
                {
                    updateCategories.Add(new LoggingData { CategoryName = category });
                }
            }

            LoggingConfig.Instance.Data = updateCategories;
            LoggingConfig.Instance.Save();
        }

        private static List<string> GetAllCategories()
        {
            return GetAllDerivedTypes(typeof(LogCategory<>)).Select(x => x.Name).ToList();
        }

        private static IEnumerable<Type> GetAllDerivedTypes(Type baseType)
        {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .ToArray();

            var results = new List<Type>();
            GetAllDerivedTypesRecursively(types, baseType, ref results);

            return results;
        }

        private static void GetAllDerivedTypesRecursively(IEnumerable<Type> types, Type baseType, ref List<Type> results)
        {
            var enumerable = types as Type[] ?? types.ToArray();

            var derivedTypes = baseType.IsGenericType
                ? enumerable.Where(x => x.BaseType is { IsGenericType: true } && x.BaseType.GetGenericTypeDefinition() == baseType).ToArray()
                : enumerable.Where(x => x != baseType && baseType.IsAssignableFrom(x)).ToArray();

            results.AddRange(derivedTypes);

            foreach (var derivedType in derivedTypes)
            {
                GetAllDerivedTypesRecursively(enumerable, derivedType, ref results);
            }
        }

        private static void Save()
        {
            LoggerFactory.UpdateMinLogLevels();
            LoggingConfig.Instance.Save();
        }

        private static void OnTestLogs()
        {
            Log.Editor.Verbose("Verbose");
            Log.Editor.Debug("Debug");
            Log.Editor.Info("Info");
            Log.Editor.Warn("Warn");
            Log.Editor.Error("Error");

            try
            {
                throw new ArgumentException();
            }
            catch (Exception exception)
            {
                Log.Editor.Error("Error", exception);
            }
        }

        private void OnLogLevel(LogLevel logLevel)
        {
            foreach (var data in LoggingConfig.Instance.Data.Where(data => !data.IsLockDuringBuild))
            {
                data.LogLevel = logLevel;
            }

            foreach (var (loggerData, dropdownField) in _dropdownFields)
            {
                if (!loggerData.IsLockDuringBuild)
                {
                    dropdownField.SetValueWithoutNotify(logLevel.ToString());
                }
            }

            Save();
        }

        private static void OnChangeCategory(string newValue, LoggingData data)
        {
            var isValid = Enum.TryParse<LogLevel>(newValue, out var value);

            if (!isValid) return;

            data.LogLevel = value;

            Save();
        }

        private static void OnChangeCredentials(string newValue)
        {
            var isValid = Enum.TryParse<LoggingCredentials>(newValue, out var value);

            if (!isValid) return;

            LoggingConfig.Instance.Credentials = value;

            Save();
        }
    }
}