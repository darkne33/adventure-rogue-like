using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace LittleRush.Rendering
{
    public sealed class HeightFogRendererFeature : ScriptableRendererFeature
    {
        [SerializeField]
        private Material material;

        [SerializeField]
        private RenderPassEvent injectionPoint = RenderPassEvent.BeforeRenderingTransparents;

        private HeightFogPass heightFogPass;

        public override void Create()
        {
            heightFogPass = new HeightFogPass
            {
                renderPassEvent = injectionPoint
            };
        }

        public override void AddRenderPasses(
            ScriptableRenderer renderer,
            ref RenderingData renderingData)
        {
            if (material == null || heightFogPass == null)
                return;

            var cameraData = renderingData.cameraData;

            if (cameraData.cameraType == CameraType.Preview ||
                cameraData.cameraType == CameraType.Reflection ||
                cameraData.renderType == CameraRenderType.Overlay)
            {
                return;
            }

#if UNITY_EDITOR
            if (cameraData.isSceneViewCamera &&
                UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                return;
            }
#endif

            heightFogPass.renderPassEvent = injectionPoint;
            heightFogPass.Setup(material);
            renderer.EnqueuePass(heightFogPass);
        }

        private sealed class HeightFogPass : ScriptableRenderPass
        {
            private const string PassName = "Height Fog";
            private static readonly Vector4 FullscreenScaleBias = new(1f, 1f, 0f, 0f);

            private Material material;

            public HeightFogPass()
            {
                profilingSampler = new ProfilingSampler(PassName);
                ConfigureInput(ScriptableRenderPassInput.Depth);
                requiresIntermediateTexture = true;
            }

            public void Setup(Material passMaterial)
            {
                material = passMaterial;
            }

            public override void RecordRenderGraph(
                RenderGraph renderGraph,
                ContextContainer frameData)
            {
                var resourceData = frameData.Get<UniversalResourceData>();

                if (resourceData.isActiveTargetBackBuffer ||
                    !resourceData.cameraDepthTexture.IsValid())
                {
                    return;
                }

                var source = resourceData.activeColorTexture;
                var destinationDescriptor = renderGraph.GetTextureDesc(source);
                destinationDescriptor.name = "_HeightFogColor";
                destinationDescriptor.clearBuffer = false;

                var destination = renderGraph.CreateTexture(destinationDescriptor);

                using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                           PassName,
                           out var passData,
                           profilingSampler))
                {
                    passData.source = source;
                    passData.material = material;

                    builder.UseTexture(source, AccessFlags.Read);
                    builder.UseTexture(resourceData.cameraDepthTexture, AccessFlags.Read);
                    builder.SetRenderAttachment(destination, 0, AccessFlags.Write);

                    builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                    {
                        Blitter.BlitTexture(
                            context.cmd,
                            data.source,
                            FullscreenScaleBias,
                            data.material,
                            0);
                    });
                }

                resourceData.cameraColor = destination;
            }

            private sealed class PassData
            {
                public TextureHandle source;
                public Material material;
            }
        }
    }
}
