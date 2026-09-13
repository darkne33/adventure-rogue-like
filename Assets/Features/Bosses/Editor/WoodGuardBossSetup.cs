using System;
using System.Linq;
using Features.Bosses.Scripts;
using Features.Bosses.UI;
using Features.Enemies.Scripts;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Features.Bosses.Editor
{
    public static class WoodGuardBossSetup
    {
        private const string Root = "Assets/Features/Bosses/Content/WoodGuard_Boss";
        private const string ConfigFolder = Root + "/Configs";
        private const string BossPath = Root + "/Prefabs/WoodGuard_Boss.prefab";
        private const string SourceRoomPath =
            "Assets/Features/Level/Level_1/Rooms/EnemiesRoom_Level_1_1_Medium.prefab";
        public const string RoomPath =
            "Assets/Features/Level/Level_1/Rooms/BossRoom_WoodGuard_Medium.prefab";

        // Explicit menu/Unity MCP entry point; nothing runs on import or domain reload.
        [MenuItem("Tools/Little Rush/Bosses/Set Up Wood Guard")]
        public static void Setup()
        {
            GameObject roots = RequireAsset<GameObject>(Root + "/Prefabs/HorizontalWoodAttack_Piece.prefab");
            AnimationClip idle = RequireAsset<AnimationClip>(Root + "/Animations/Idle.anim");
            AnimationClip attack = RequireAsset<AnimationClip>(Root + "/Animations/Attack.anim");
            RequireAsset<GameObject>(BossPath);
            RequireAsset<GameObject>(SourceRoomPath);
            EnsureFolder(ConfigFolder);

            Material indicatorMaterial = GetIndicatorMaterial();
            WoodGuardHorizontalAttackConfiguration horizontal = GetOrCreate<WoodGuardHorizontalAttackConfiguration>(
                ConfigFolder + "/WoodGuardHorizontalAttackConfiguration.asset", out bool newHorizontal);
            if (newHorizontal)
            {
                SetReference(horizontal, "<RootsPrefab>k__BackingField", roots);
                SetReference(horizontal, "<IndicatorMaterial>k__BackingField", indicatorMaterial);
            }

            BossConfig bossConfiguration = GetOrCreate<BossConfig>(
                ConfigFolder + "/BossConfig_WoodGuard.asset", out bool newBossConfiguration);
            if (newBossConfiguration)
            {
                var serialized = new SerializedObject(bossConfiguration);
                serialized.FindProperty("<DisplayName>k__BackingField").stringValue = "Wood Guard";
                serialized.FindProperty("<HealthCanvasPrefab>k__BackingField").objectReferenceValue =
                    RequireAsset<GameObject>("Assets/Features/Bosses/UI/Prefabs/BossHealthCanvas.prefab")
                        .GetComponent<BossHealthCanvas>();
                SerializedProperty attacks = serialized.FindProperty("<Attacks>k__BackingField");
                attacks.arraySize = 1;
                attacks.GetArrayElementAtIndex(0).objectReferenceValue = horizontal;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            AnimatorController controller = GetController(idle, attack);
            ConfigureBossPrefab(bossConfiguration, controller, attack);
            CreateRoom();
            AssetDatabase.SaveAssets();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(RoomPath);
            EditorGUIUtility.PingObject(Selection.activeObject);
            Debug.Log("Wood Guard configured. Room: " + RoomPath + ". Attack settings: " + ConfigFolder);
        }

        private static AnimatorController GetController(AnimationClip idle, AnimationClip attack)
        {
            string path = Root + "/Animations/WoodGuardBoss.controller";
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller != null)
                return controller;

            var idleSettings = AnimationUtility.GetAnimationClipSettings(idle);
            idleSettings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(idle, idleSettings);
            var attackSettings = AnimationUtility.GetAnimationClipSettings(attack);
            attackSettings.loopTime = false;
            AnimationUtility.SetAnimationClipSettings(attack, attackSettings);
            EditorUtility.SetDirty(idle);
            EditorUtility.SetDirty(attack);

            controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState idleState = stateMachine.AddState("Idle", new Vector3(240f, 80f));
            idleState.motion = idle;
            AnimatorState attackState = stateMachine.AddState("Attack", new Vector3(480f, 80f));
            attackState.motion = attack;
            stateMachine.defaultState = idleState;
            return controller;
        }

        private static Material GetIndicatorMaterial()
        {
            return RequireAsset<Material>(
                "Assets/Features/Enemies/Content/BombEnemy/Prefabs/ExplosionIndicator.mat");
        }

        private static void ConfigureBossPrefab(BossConfig bossConfiguration,
            AnimatorController controller, AnimationClip attack)
        {
            GameObject prefab = PrefabUtility.LoadPrefabContents(BossPath);
            try
            {
                GameObject enemyTemplate = RequireAsset<GameObject>(
                    "Assets/Features/Enemies/Content/BunEnemy/Prefabs/BunEnemy.prefab");
                Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
                Bounds bounds = GetLocalBounds(prefab.transform, renderers);
                foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
                    child.gameObject.layer = enemyTemplate.layer;

                WoodGuardBossFacade boss = GetOrAdd<WoodGuardBossFacade>(prefab);
                Rigidbody body = GetOrAdd<Rigidbody>(prefab);
                body.useGravity = false;
                body.isKinematic = false;
                body.constraints = RigidbodyConstraints.FreezeAll;

                if (prefab.GetComponent<Collider>() == null)
                {
                    CapsuleCollider collider = prefab.AddComponent<CapsuleCollider>();
                    collider.center = bounds.center;
                    collider.radius = Mathf.Max(0.5f, Mathf.Min(bounds.size.x, bounds.size.z) * 0.25f);
                    collider.height = Mathf.Max(collider.radius * 2f, bounds.size.y);
                }

                Animator animator = GetOrAdd<Animator>(prefab);
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

                Transform attackOrigin = GetOrCreateChild(prefab.transform, "AttackOrigin", Vector3.zero);
                Transform target = GetOrCreateChild(prefab.transform, "DamageTarget", bounds.center);
                SetReference(boss, "_bossConfig", bossConfiguration);
                SetReference(boss, "<TargetToShootDamage>k__BackingField", target);
                var serializedBoss = new SerializedObject(boss);
                SerializedProperty rendererList = serializedBoss.FindProperty("_meshRenderers");
                rendererList.arraySize = renderers.Length;
                for (int i = 0; i < renderers.Length; i++)
                    rendererList.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
                serializedBoss.ApplyModifiedPropertiesWithoutUndo();

                SetReference(boss, "_attackOrigin", attackOrigin);
                SetReference(boss, "_animator", animator);
                SetReference(boss, "_attackClip", attack);
                ConfigureHealthViews(prefab, enemyTemplate, bounds);
                PrefabUtility.SaveAsPrefabAsset(prefab, BossPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }
        }

        private static void ConfigureHealthViews(GameObject prefab, GameObject template, Bounds bounds)
        {
            Transform healthRoot = prefab.transform.Find("BossHealthBar");
            if (healthRoot == null)
            {
                var canvasObject = new GameObject("BossHealthBar", typeof(RectTransform), typeof(Canvas));
                healthRoot = canvasObject.transform;
                healthRoot.SetParent(prefab.transform, false);
                healthRoot.localPosition = new Vector3(bounds.center.x, bounds.max.y + 1f, bounds.center.z);
                ((RectTransform)healthRoot).sizeDelta = new Vector2(9.4479f, 1.3079f);
                canvasObject.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
                GameObject healthPrefab = RequireAsset<GameObject>("Assets/Features/Enemies/General/HpBar.prefab");
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(healthPrefab, healthRoot);
                instance.transform.localPosition = Vector3.zero;
            }

            EnemyHealthView health = GetOrAdd<EnemyHealthView>(prefab);
            SetReference(health, "_healthSlider", healthRoot.GetComponentInChildren<Slider>(true));
            EnemyDamageNumberView damageNumbers = GetOrAdd<EnemyDamageNumberView>(prefab);
            SetReference(damageNumbers, "_worldCanvas", healthRoot.GetComponentInChildren<Canvas>(true));
            EnemyDamageNumberView templateView = template.GetComponent<EnemyDamageNumberView>();
            if (templateView != null)
            {
                var serialized = new SerializedObject(templateView);
                SetReference(damageNumbers, "_fontAsset", serialized.FindProperty("_fontAsset").objectReferenceValue);
            }
        }

        private static void CreateRoom()
        {
            // Preserve the user's room placement and geometry on subsequent calls.
            bool isNewRoom = AssetDatabase.LoadAssetAtPath<GameObject>(RoomPath) == null;
            if (isNewRoom && !AssetDatabase.CopyAsset(SourceRoomPath, RoomPath))
                throw new InvalidOperationException("Could not copy the medium room to " + RoomPath);

            GameObject prefab = PrefabUtility.LoadPrefabContents(RoomPath);
            try
            {
                prefab.name = "BossRoom_WoodGuard_Medium";
                Room room = prefab.GetComponent<Room>();
                if (room == null)
                    throw new InvalidOperationException("The medium room has no Room component.");
                if (isNewRoom)
                    room.SetEditorRoomData(new BossRoomData { RoomDoors = room.RoomData.RoomDoors });
                bool isNewPoint = prefab.transform.Find("WoodGuard_BossSpawnPoint") == null;
                Transform pointTransform = GetOrCreateChild(prefab.transform, "WoodGuard_BossSpawnPoint",
                    Vector3.zero);

                Collider ground = prefab.GetComponentsInChildren<Collider>(true)
                    .Where(collider => !collider.isTrigger &&
                        (collider.name == "Ground" || collider.gameObject.layer == LayerMask.NameToLayer("Ground")))
                    .OrderByDescending(collider => collider.bounds.size.x * collider.bounds.size.z)
                    .FirstOrDefault();
                if (isNewPoint && ground != null)
                    pointTransform.position = new Vector3(ground.bounds.center.x,
                        ground.bounds.max.y, ground.bounds.center.z);

                BossSpawnPoint point = GetOrAdd<BossSpawnPoint>(pointTransform.gameObject);
                SetReference(point, "<BossPrefab>k__BackingField",
                    RequireAsset<GameObject>(BossPath).GetComponent<BossFacade>());
                PrefabUtility.SaveAsPrefabAsset(prefab, RoomPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }
        }

        private static Bounds GetLocalBounds(Transform root, Renderer[] renderers)
        {
            bool initialized = false;
            Bounds result = new(Vector3.up, Vector3.one * 2f);
            foreach (Renderer renderer in renderers)
            {
                Bounds bounds = renderer.bounds;
                for (int x = 0; x < 2; x++)
                for (int y = 0; y < 2; y++)
                for (int z = 0; z < 2; z++)
                {
                    Vector3 point = root.InverseTransformPoint(new Vector3(
                        x == 0 ? bounds.min.x : bounds.max.x,
                        y == 0 ? bounds.min.y : bounds.max.y,
                        z == 0 ? bounds.min.z : bounds.max.z));
                    if (!initialized)
                    {
                        result = new Bounds(point, Vector3.zero);
                        initialized = true;
                    }
                    else
                        result.Encapsulate(point);
                }
            }
            return result;
        }

        private static Transform GetOrCreateChild(Transform parent, string name, Vector3 localPosition)
        {
            Transform child = parent.Find(name);
            if (child != null)
                return child;
            child = new GameObject(name).transform;
            child.SetParent(parent, false);
            child.localPosition = localPosition;
            return child;
        }

        private static T GetOrAdd<T>(GameObject gameObject) where T : Component =>
            gameObject.GetComponent<T>() ?? gameObject.AddComponent<T>();

        private static T GetOrCreate<T>(string path, out bool created) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            created = asset == null;
            if (!created)
                return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static T RequireAsset<T>(string path) where T : Object =>
            AssetDatabase.LoadAssetAtPath<T>(path) ??
            throw new InvalidOperationException("Required asset is missing: " + path);

        private static void SetReference(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
                throw new InvalidOperationException($"Missing serialized field {field} on {target.name}.");
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
        }
    }
}
