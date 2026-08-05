using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class FireFieldPuddleUpgrade
{
    private const string TargetPrefabPath =
        "Assets/Features/CharacterAbilities/Content/FireFieldTallYellow_Ability.prefab";
    private const string PuddleTexturePath =
        "Assets/Features/CharacterAbilities/Content/FireFieldPuddle.png";
    private const string BubblesSourcePath =
        "Assets/third-party/Retro Arsenal/Prefabs/Environment/Bubbles/Boiling/LavaBoilingBubbles.prefab";
    private const string CompletionMarkerPath = "Temp/FireFieldPuddleUpgrade.completed";
    private const string PreviewPath = "Temp/FireFieldPuddlePreview.png";
    private const string PuddleName = "Fire Puddle";
    private const string BubblesName = "Puddle Bubbles";
    private const float DefaultRadius = 3.5f;
    private const float PuddleBaseDiameter = 2f;

    static FireFieldPuddleUpgrade() =>
        EditorApplication.update += RunWhenReady;

    [MenuItem("Tools/Little Rush/Upgrade Fire Field Puddle")]
    public static void Run()
    {
        try
        {
            ConfigurePuddleTexture();
            UpgradePrefab();
            RenderPreview();
            File.WriteAllText(CompletionMarkerPath, DateTime.UtcNow.ToString("O"));
            Debug.Log($"Fire field puddle upgraded. Preview: {PreviewPath}");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static void RunWhenReady()
    {
        if (File.Exists(CompletionMarkerPath))
        {
            EditorApplication.update -= RunWhenReady;
            return;
        }

        if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        EditorApplication.update -= RunWhenReady;
        Run();
    }

    private static void ConfigurePuddleTexture()
    {
        AssetDatabase.ImportAsset(PuddleTexturePath,
            ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

        TextureImporter importer = AssetImporter.GetAtPath(PuddleTexturePath) as TextureImporter;
        if (importer == null)
            throw new InvalidOperationException($"Texture importer not found for {PuddleTexturePath}.");

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 256f;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.sRGBTexture = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 512;
        importer.SaveAndReimport();
    }

    private static void UpgradePrefab()
    {
        Sprite puddleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PuddleTexturePath);
        GameObject bubblesSource = AssetDatabase.LoadAssetAtPath<GameObject>(BubblesSourcePath);

        if (puddleSprite == null)
            throw new InvalidOperationException($"Sprite not found at {PuddleTexturePath}.");
        if (bubblesSource == null)
            throw new InvalidOperationException($"Bubbles source not found at {BubblesSourcePath}.");

        GameObject root = PrefabUtility.LoadPrefabContents(TargetPrefabPath);
        try
        {
            RemoveChild(root.transform, PuddleName);
            RemoveChild(root.transform, BubblesName);

            Transform puddle = CreatePuddle(root.transform, puddleSprite);
            CreateBubbles(root.transform, bubblesSource);
            BindPuddleVisual(root, puddle);

            PrefabUtility.SaveAsPrefabAsset(root, TargetPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static Transform CreatePuddle(Transform parent, Sprite puddleSprite)
    {
        GameObject puddleObject = new(PuddleName);
        Transform puddle = puddleObject.transform;
        puddle.SetParent(parent, false);
        puddle.localPosition = new Vector3(0f, 0.0125f, 0f);
        puddle.localRotation = Quaternion.Euler(90f, 0f, 0f);
        puddle.localScale = Vector3.one * DefaultRadius;

        SpriteRenderer renderer = puddleObject.AddComponent<SpriteRenderer>();
        renderer.sprite = puddleSprite;
        renderer.color = new Color(1f, 1f, 1f, 0.72f);
        renderer.sortingOrder = -2;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;

        return puddle;
    }

    private static void CreateBubbles(Transform parent, GameObject source)
    {
        ParticleSystem sourceParticles = source.GetComponent<ParticleSystem>();
        ParticleSystemRenderer sourceRenderer = source.GetComponent<ParticleSystemRenderer>();
        if (sourceParticles == null || sourceRenderer == null)
            throw new InvalidOperationException($"{BubblesSourcePath} has no root particle system.");

        GameObject bubblesObject = new(BubblesName);
        Transform bubbles = bubblesObject.transform;
        bubbles.SetParent(parent, false);
        bubbles.localPosition = new Vector3(0f, 0.025f, 0f);

        ParticleSystem particles = bubblesObject.AddComponent<ParticleSystem>();
        ParticleSystemRenderer renderer = bubblesObject.GetComponent<ParticleSystemRenderer>();
        EditorUtility.CopySerialized(sourceParticles, particles);
        EditorUtility.CopySerialized(sourceRenderer, renderer);

        ParticleSystem.MainModule main = particles.main;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 48;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(7f);

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = DefaultRadius;
        shape.radiusThickness = 0.88f;
        shape.rotation = new Vector3(-90f, 0f, 0f);

        renderer.sortingOrder = -1;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private static void BindPuddleVisual(GameObject root, Transform puddle)
    {
        FireFieldDamageArea damageArea = root.GetComponent<FireFieldDamageArea>();
        if (damageArea == null)
            throw new InvalidOperationException($"{TargetPrefabPath} has no {nameof(FireFieldDamageArea)}.");

        SerializedObject serializedArea = new(damageArea);
        SerializedProperty puddleVisual = serializedArea.FindProperty("_puddleVisual");
        SerializedProperty puddleBaseDiameter = serializedArea.FindProperty("_puddleBaseDiameter");
        if (puddleVisual == null || puddleBaseDiameter == null)
            throw new InvalidOperationException("Fire field puddle serialized properties were not found.");

        puddleVisual.objectReferenceValue = puddle;
        puddleBaseDiameter.floatValue = PuddleBaseDiameter;
        serializedArea.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void RemoveChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null)
            UnityEngine.Object.DestroyImmediate(child.gameObject);
    }

    private static void RenderPreview()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TargetPrefabPath);
        if (prefab == null)
            throw new InvalidOperationException($"Prefab not found at {TargetPrefabPath}.");

        Scene previewScene = EditorSceneManager.NewPreviewScene();
        RenderTexture renderTexture = null;
        Texture2D screenshot = null;
        Material groundMaterial = null;

        try
        {
            GameObject effect = PrefabUtility.InstantiatePrefab(prefab, previewScene) as GameObject;
            effect.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            foreach (ParticleSystem particleSystem in effect.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.ShapeModule shape = particleSystem.shape;
                if (shape.enabled && shape.shapeType is ParticleSystemShapeType.Circle or
                    ParticleSystemShapeType.CircleEdge)
                    shape.radius = DefaultRadius;

                particleSystem.Simulate(1.15f, true, true, true);
            }

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            SceneManager.MoveGameObjectToScene(ground, previewScene);
            ground.transform.position = new Vector3(0f, -0.065f, 0f);
            ground.transform.localScale = new Vector3(1.3f, 1f, 1.3f);
            UnityEngine.Object.DestroyImmediate(ground.GetComponent<Collider>());

            Shader groundShader = Shader.Find("Universal Render Pipeline/Lit");
            if (groundShader != null)
            {
                groundMaterial = new Material(groundShader);
                groundMaterial.color = new Color(0.085f, 0.065f, 0.055f, 1f);
                ground.GetComponent<MeshRenderer>().sharedMaterial = groundMaterial;
            }

            GameObject lightObject = new("Preview Light", typeof(Light));
            SceneManager.MoveGameObjectToScene(lightObject, previewScene);
            lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            Light light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.88f, 0.72f);
            light.intensity = 1.2f;

            GameObject cameraObject = new("Preview Camera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(cameraObject, previewScene);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.transform.position = new Vector3(0f, 7.2f, -7.2f);
            camera.transform.LookAt(new Vector3(0f, 0.25f, 0f));
            camera.orthographic = true;
            camera.orthographicSize = 5.15f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.02f, 0.025f, 1f);
            camera.allowHDR = true;

            renderTexture = new RenderTexture(768, 768, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = renderTexture;
            camera.Render();

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTexture;
            screenshot = new Texture2D(768, 768, TextureFormat.RGBA32, false);
            screenshot.ReadPixels(new Rect(0f, 0f, 768f, 768f), 0, 0);
            screenshot.Apply();
            RenderTexture.active = previous;

            File.WriteAllBytes(PreviewPath, screenshot.EncodeToPNG());
        }
        finally
        {
            if (screenshot != null)
                UnityEngine.Object.DestroyImmediate(screenshot);
            if (renderTexture != null)
                UnityEngine.Object.DestroyImmediate(renderTexture);
            if (groundMaterial != null)
                UnityEngine.Object.DestroyImmediate(groundMaterial);
            EditorSceneManager.ClosePreviewScene(previewScene);
        }
    }
}
