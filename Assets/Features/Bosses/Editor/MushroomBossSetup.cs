using System;
using System.Linq;
using Features.Bosses.Scripts;
using Features.Bosses.UI;
using Features.Enemies.Scripts;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Features.Bosses.Editor
{
    public static class MushroomBossSetup
    {
        private const string Root = "Assets/Features/Bosses/Content/Mushroom_Boss";
        private const string PrefabPath = Root + "/Prefabs/Mushroom_Boss.prefab";
        private const string BombPath = "Assets/Features/Enemies/Content/BombEnemy/Prefabs/BombEnemy_Normal.prefab";
        private const string IndicatorMaterialPath =
            "Assets/Features/Enemies/Content/BombEnemy/Prefabs/ExplosionIndicator.mat";

        // Explicit setup only: importing scripts never changes prefabs, scenes or settings.
        [MenuItem("Tools/Little Rush/Bosses/Set Up Mushroom")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before setting up Mushroom.");

            GameObject model = Require<GameObject>(Root + "/Models/Mushroom.fbx");
            AnimationClip idle = Require<AnimationClip>(Root + "/Animations/Idle.anim");
            AnimationClip jump = Require<AnimationClip>(Root + "/Animations/Attack_Jump.anim");
            AnimationClip head = Require<AnimationClip>(Root + "/Animations/Attack_Head.anim");
            Texture2D texture = Require<Texture2D>(Root + "/Textures/Mushroom_Basecolor.png");
            Material bodyTemplate = Require<Material>(
                "Assets/Features/Bosses/Content/WoodGuard_Boss/Materials/WoodyMat.mat");
            Material indicatorTemplate = Require<Material>(IndicatorMaterialPath);
            GameObject bomb = Require<GameObject>(BombPath);
            GameObject landingEffect = Require<GameObject>(Root + "/Prefabs/SparkleNovaRed.prefab");
            GameObject headEffect = Require<GameObject>(Root + "/Prefabs/NovaRed.prefab");
            BossHealthCanvas healthCanvas = Require<GameObject>(
                "Assets/Features/Bosses/UI/Prefabs/BossHealthCanvas.prefab").GetComponent<BossHealthCanvas>();

            EnsureFolder(Root + "/Configs");
            EnsureFolder(Root + "/Materials");
            EnsureFolder(Root + "/Prefabs");
            Material material = CreateBodyMaterial(bodyTemplate, texture);
            GameObject jumpIndicator = CreateIndicator(bomb, indicatorTemplate, false);
            GameObject headIndicator = CreateIndicator(bomb, indicatorTemplate, true);
            AnimatorController controller = CreateController(idle, jump, head);

            MushroomBossConfiguration movement = GetOrCreate<MushroomBossConfiguration>(
                Root + "/Configs/MushroomBossConfiguration.asset", out bool newMovement);
            if (newMovement)
            {
                var serialized = new SerializedObject(movement);
                Property(serialized, "GroundMask").intValue = LayerMask.GetMask("Ground");
                Property(serialized, "MovementObstacleMask").intValue =
                    LayerMask.GetMask("Obstacle", "Wall", "Door");
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            MushroomJumpAttackConfiguration jumpAttack = GetOrCreate<MushroomJumpAttackConfiguration>(
                Root + "/Configs/MushroomJumpAttackConfiguration.asset", out bool newJumpAttack);
            if (newJumpAttack)
            {
                var serialized = new SerializedObject(jumpAttack);
                SetAsset(serialized, "IndicatorPrefab", jumpIndicator);
                SetAsset(serialized, "EffectPrefab", landingEffect);
                Property(serialized, "Duration").floatValue = jump.length;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            MushroomHeadAttackConfiguration headAttack = GetOrCreate<MushroomHeadAttackConfiguration>(
                Root + "/Configs/MushroomHeadAttackConfiguration.asset", out bool newHeadAttack);
            if (newHeadAttack)
            {
                var serialized = new SerializedObject(headAttack);
                SetAsset(serialized, "IndicatorPrefab", headIndicator);
                SetAsset(serialized, "EffectPrefab", headEffect);
                Property(serialized, "Duration").floatValue = head.length;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            BossConfig config = GetOrCreate<BossConfig>(Root + "/Configs/BossConfig_Mushroom.asset",
                out bool newConfig);
            if (newConfig)
            {
                var serialized = new SerializedObject(config);
                Property(serialized, "DisplayName").stringValue = "Mushroom";
                Property(serialized, "MaxHealth").intValue = 2200;
                Property(serialized, "Exp").intValue = 25;
                Property(serialized, "InitialAttackDelay").floatValue = 1.5f;
                SetAsset(serialized, "HealthCanvasPrefab", healthCanvas);
                Property(serialized, "AttackPhases").arraySize = 0;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            var serializedConfig = new SerializedObject(config);
            SerializedProperty attacks = Property(serializedConfig, "Attacks");
            if (newConfig || attacks.arraySize == 0)
            {
                attacks.arraySize = 2;
                attacks.GetArrayElementAtIndex(0).objectReferenceValue = jumpAttack;
                attacks.GetArrayElementAtIndex(1).objectReferenceValue = headAttack;
                serializedConfig.ApplyModifiedPropertiesWithoutUndo();
            }

            CreateBossPrefab(model, material, controller, idle, jump, head, config, movement);
            if (newMovement)
            {
                CapsuleCollider body = Require<GameObject>(PrefabPath).GetComponent<CapsuleCollider>();
                var serialized = new SerializedObject(movement);
                Property(serialized, "BodyRadius").floatValue = body.radius;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            AssetDatabase.SaveAssets();
            Selection.activeObject = Require<GameObject>(PrefabPath);
            EditorGUIUtility.PingObject(Selection.activeObject);
            Debug.Log("Mushroom boss created: " + PrefabPath +
                      ". Tune health in BossConfig_Mushroom, movement in MushroomBossConfiguration, " +
                      "and attacks in MushroomJumpAttackConfiguration and MushroomHeadAttackConfiguration. " +
                      "Effect points: LandingEffectOrigin and HeadImpactOrigin.");
        }

        private static Material CreateBodyMaterial(Material template, Texture2D texture)
        {
            string path = Root + "/Materials/Mushroom.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
                return material;
            material = new Material(template) { name = "Mushroom" };
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static GameObject CreateIndicator(GameObject bomb, Material template, bool head)
        {
            string name = head ? "HeadAttack_Indicator" : "JumpAttack_Indicator";
            string path = Root + "/Prefabs/" + name + ".prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
                return existing;

            Transform source = bomb.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(child => child.name == "ExplosionIndicator");
            if (source == null || source.GetComponent<MeshFilter>()?.sharedMesh == null)
                throw new InvalidOperationException("The bomb has no ExplosionIndicator mesh.");

            string materialPath = Root + "/Materials/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(template) { name = name };
                Bounds bounds = source.GetComponent<MeshFilter>().sharedMesh.bounds;
                material.SetFloat("_GradientMode", head ? 0f : 1f);
                material.SetVector("_GradientAxis", Vector3.up);
                material.SetVector("_GradientOrigin", head ? Vector3.zero :
                    new Vector3(bounds.center.x, bounds.min.y, bounds.center.z));
                material.SetFloat("_GradientLength", head ? bounds.extents.x : bounds.size.y);
                AssetDatabase.CreateAsset(material, materialPath);
            }

            var indicator = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            try
            {
                indicator.layer = LayerMask.NameToLayer("Ignore Raycast");
                // Keep the bomb's cylinder silhouette. The head warning is a thin ground disc.
                indicator.transform.localScale = new Vector3(1f, head ? 0.01f : source.localScale.y, 1f);
                indicator.GetComponent<MeshFilter>().sharedMesh = source.GetComponent<MeshFilter>().sharedMesh;
                MeshRenderer renderer = indicator.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                return PrefabUtility.SaveAsPrefabAsset(indicator, path);
            }
            finally
            {
                Object.DestroyImmediate(indicator);
            }
        }

        private static AnimatorController CreateController(AnimationClip idle, AnimationClip jump,
            AnimationClip head)
        {
            string path = Root + "/Animations/MushroomBoss.controller";
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller != null)
                return controller;

            var settings = AnimationUtility.GetAnimationClipSettings(idle);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(idle, settings);
            EditorUtility.SetDirty(idle);
            controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState idleState = machine.AddState("Idle", new Vector3(200f, 60f));
            idleState.motion = idle;
            machine.AddState("Attack_Jump", new Vector3(450f, 20f)).motion = jump;
            machine.AddState("Attack_Head", new Vector3(450f, 140f)).motion = head;
            machine.defaultState = idleState;
            // Code samples attack poses; automatic transitions would compete with impact timing.
            return controller;
        }

        private static void CreateBossPrefab(GameObject model, Material material,
            AnimatorController controller, AnimationClip idle, AnimationClip jump, AnimationClip head,
            BossConfig config, MushroomBossConfiguration movement)
        {
            bool created = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null;
            GameObject root = created ? new GameObject("Mushroom_Boss") :
                PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Transform visual = root.transform.Find("Visual");
                if (visual == null)
                {
                    GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
                    instance.name = "Visual";
                    visual = instance.transform;
                    idle.SampleAnimation(instance, 0f);
                    Bounds sourceBounds = GetModelBounds(root.transform, instance);
                    visual.localScale *= 3.5f / Mathf.Max(0.01f, sourceBounds.size.y);
                    Bounds scaledBounds = GetModelBounds(root.transform, instance);
                    visual.localPosition -= new Vector3(scaledBounds.center.x, scaledBounds.min.y,
                        scaledBounds.center.z);
                    foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
                    {
                        Material[] materials = renderer.sharedMaterials;
                        for (int index = 0; index < materials.Length; index++)
                            materials[index] = material;
                        renderer.sharedMaterials = materials;
                    }
                }

                Bounds bounds = GetModelBounds(root.transform, visual.gameObject);
                Animator animator = GetOrAdd<Animator>(visual.gameObject);
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                MushroomBossFacade boss = GetOrAdd<MushroomBossFacade>(root);
                Rigidbody body = GetOrAdd<Rigidbody>(root);
                body.useGravity = false;
                body.isKinematic = true;
                body.constraints = RigidbodyConstraints.FreezeAll;

                CapsuleCollider capsule = root.GetComponent<CapsuleCollider>();
                if (capsule == null)
                {
                    capsule = root.AddComponent<CapsuleCollider>();
                    capsule.center = bounds.center;
                    capsule.radius = Mathf.Max(0.5f, Mathf.Min(bounds.size.x, bounds.size.z) * 0.35f);
                    capsule.height = Mathf.Max(bounds.size.y, capsule.radius * 2f);
                }

                Transform ground = Child(root.transform, "AttackOrigin", Vector3.zero);
                Transform landing = Child(root.transform, "LandingEffectOrigin", Vector3.zero);
                Transform impact = Child(root.transform, "HeadImpactOrigin", new Vector3(0f, 0f, 1.8f));
                Transform target = Child(root.transform, "DamageTarget", bounds.center);
                var serialized = new SerializedObject(boss);
                serialized.FindProperty("_bossConfig").objectReferenceValue = config;
                serialized.FindProperty("_movementConfiguration").objectReferenceValue = movement;
                serialized.FindProperty("_collider").objectReferenceValue = capsule;
                serialized.FindProperty("_animator").objectReferenceValue = animator;
                serialized.FindProperty("_jumpClip").objectReferenceValue = jump;
                serialized.FindProperty("_headClip").objectReferenceValue = head;
                serialized.FindProperty("_attackOrigin").objectReferenceValue = ground;
                serialized.FindProperty("_landingEffectOrigin").objectReferenceValue = landing;
                serialized.FindProperty("_headImpactOrigin").objectReferenceValue = impact;
                SetAsset(serialized, "TargetToShootDamage", target);
                Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
                SerializedProperty rendererList = serialized.FindProperty("_meshRenderers");
                rendererList.arraySize = renderers.Length;
                for (int index = 0; index < renderers.Length; index++)
                    rendererList.GetArrayElementAtIndex(index).objectReferenceValue = renderers[index];
                serialized.ApplyModifiedPropertiesWithoutUndo();

                ConfigureDamageNumbers(root, bounds);
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                    child.gameObject.layer = LayerMask.NameToLayer("Enemy");
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                if (created) Object.DestroyImmediate(root);
                else PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureDamageNumbers(GameObject root, Bounds bounds)
        {
            Transform anchor = root.transform.Find("DamageNumbersCanvas");
            if (anchor == null)
            {
                var canvasObject = new GameObject("DamageNumbersCanvas", typeof(RectTransform), typeof(Canvas));
                anchor = canvasObject.transform;
                anchor.SetParent(root.transform, false);
                anchor.localPosition = new Vector3(bounds.center.x, bounds.max.y + 0.5f, bounds.center.z);
                ((RectTransform)anchor).sizeDelta = new Vector2(9.4479f, 1.3079f);
                canvasObject.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            }

            EnemyDamageNumberView view = GetOrAdd<EnemyDamageNumberView>(root);
            var serialized = new SerializedObject(view);
            serialized.FindProperty("_worldCanvas").objectReferenceValue = anchor.GetComponent<Canvas>();
            if (serialized.FindProperty("_fontAsset").objectReferenceValue == null)
            {
                EnemyDamageNumberView template = Require<GameObject>(
                    "Assets/Features/Enemies/Content/BunEnemy/Prefabs/BunEnemy.prefab")
                    .GetComponent<EnemyDamageNumberView>();
                if (template != null)
                    serialized.FindProperty("_fontAsset").objectReferenceValue =
                        new SerializedObject(template).FindProperty("_fontAsset").objectReferenceValue;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Bounds GetModelBounds(Transform root, GameObject visual)
        {
            Bounds result = default;
            bool initialized = false;
            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                Mesh baked = null;
                try
                {
                    Bounds localBounds = renderer.localBounds;
                    if (renderer is SkinnedMeshRenderer skinned && skinned.sharedMesh != null)
                    {
                        baked = new Mesh();
                        skinned.BakeMesh(baked);
                        localBounds = baked.bounds;
                    }
                    for (int x = 0; x < 2; x++)
                    for (int y = 0; y < 2; y++)
                    for (int z = 0; z < 2; z++)
                    {
                        Vector3 point = root.InverseTransformPoint(renderer.transform.TransformPoint(new Vector3(
                            x == 0 ? localBounds.min.x : localBounds.max.x,
                            y == 0 ? localBounds.min.y : localBounds.max.y,
                            z == 0 ? localBounds.min.z : localBounds.max.z)));
                        if (!initialized) { result = new Bounds(point, Vector3.zero); initialized = true; }
                        else result.Encapsulate(point);
                    }
                }
                finally
                {
                    if (baked != null) Object.DestroyImmediate(baked);
                }
            }
            if (!initialized)
                throw new InvalidOperationException("Mushroom model has no renderers.");
            return result;
        }

        private static Transform Child(Transform parent, string name, Vector3 position)
        {
            Transform child = parent.Find(name);
            if (child != null) return child;
            child = new GameObject(name).transform;
            child.SetParent(parent, false);
            child.localPosition = position;
            return child;
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component =>
            target.GetComponent<T>() ?? target.AddComponent<T>();

        private static T Require<T>(string path) where T : Object =>
            AssetDatabase.LoadAssetAtPath<T>(path) ??
            throw new InvalidOperationException("Required asset is missing: " + path);

        private static SerializedProperty Property(SerializedObject target, string name) =>
            target.FindProperty("<" + name + ">k__BackingField") ??
            throw new InvalidOperationException("Missing serialized property: " + name);

        private static void SetAsset(SerializedObject target, string name, Object value) =>
            Property(target, name).objectReferenceValue = value;

        private static T GetOrCreate<T>(string path, out bool created) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            created = asset == null;
            if (!created) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
        }
    }
}
