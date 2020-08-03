using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CustomRender
{
    public class DepthOnlyPass : ScriptableRenderPass
    {   
        public DepthOnlyPass()
        {
            m_filteringSettings = new FilteringSettings(RenderQueueRange.opaque,
                ~0);
            renderPassEvent = RenderPassEvent.BeforeRenderingPrepasses;
        }
        
        public override void Execute(ScriptableRenderContext context,
            ref RenderingData renderingData)
        {
            CommandBuffer command = CommandBufferPool.Get(mc_profilerTag);
            using (new ProfilingScope(command, m_profilingSampler))
            {
                context.ExecuteCommandBuffer(command);
                command.Clear();

                var sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;
                var drawSettings = CreateDrawingSettings(m_shaderTagId, ref renderingData,
                    sortFlags);
                drawSettings.perObjectData = PerObjectData.None;

                ref CameraData cameraData = ref renderingData.cameraData;
                Camera camera = cameraData.camera;

                context.DrawRenderers(renderingData.cullResults, ref drawSettings,
                    ref m_filteringSettings);
            }
            context.ExecuteCommandBuffer(command);
            CommandBufferPool.Release(command);
        }
        FilteringSettings m_filteringSettings;
        const string mc_profilerTag = "Depth Prepass";
        ProfilingSampler m_profilingSampler = new ProfilingSampler(mc_profilerTag);
        ShaderTagId m_shaderTagId = new ShaderTagId("DepthOnly");
    }
}
