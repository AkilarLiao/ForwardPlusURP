using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CustomRender
{   
    public class ShowLightGridsPass : ScriptableRenderPass
    {
        public ShowLightGridsPass(Material showLightGridMaterial)
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            m_targetShowLightGridMaterial = showLightGridMaterial;      
        }

        public void Setup(RenderTargetIdentifier colorRenderTargetIdentifier)
        {
            m_colorRenderTargetIdentifier = colorRenderTargetIdentifier;
        }

        public override void Execute(ScriptableRenderContext context,
            ref RenderingData renderingData)
        {
            CommandBuffer command = CommandBufferPool.Get(mc_profilerTag);
            using (new ProfilingScope(command, m_profilingSampler))
            {
                command.Blit(null,
                    m_colorRenderTargetIdentifier,
                    m_targetShowLightGridMaterial, 0);
            }
            context.ExecuteCommandBuffer(command);
            CommandBufferPool.Release(command);
        }
            
        private const string mc_profilerTag = "DrawLightGridPass";
        private ProfilingSampler m_profilingSampler = new ProfilingSampler(mc_profilerTag);
        private Material m_targetShowLightGridMaterial = null;
        private RenderTargetIdentifier m_colorRenderTargetIdentifier;
    }
}
