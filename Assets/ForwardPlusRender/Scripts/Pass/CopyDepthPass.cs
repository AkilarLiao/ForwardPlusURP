using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CustomRender
{
    public class CopyDepthPass : ScriptableRenderPass
    {
        public CopyDepthPass()
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingPrepasses;
            m_copyCameraDepthRT.Init("_CopyCameraDepthRT");
        }

        public void Setup(RenderTargetIdentifier depthRenderTargetIdentifier,
            float renderScale)
        {
            m_targetDepthRenderTargetIdentifier = depthRenderTargetIdentifier;
            m_renderScale = renderScale;
        }

        public override void Configure(CommandBuffer cmd,
            RenderTextureDescriptor cameraTextureDescriptor)
        {
            var descriptor = cameraTextureDescriptor;
            descriptor.colorFormat = RenderTextureFormat.RFloat;
            descriptor.depthBufferBits = 32;
            descriptor.msaaSamples = 1;
            descriptor.width = (int)(descriptor.width / m_renderScale);
            descriptor.height = (int)(descriptor.height / m_renderScale);
            cmd.GetTemporaryRT(m_copyCameraDepthRT.id, descriptor, FilterMode.Point);
            ConfigureTarget(m_copyCameraDepthRT.Identifier());
        }
        public override void Execute(ScriptableRenderContext context,
            ref RenderingData renderingData)
        {
            CommandBuffer command = CommandBufferPool.Get(mc_profilerTag);

            command.SetGlobalTexture("_CameraDepthAttachment",
                m_targetDepthRenderTargetIdentifier);
            command.Blit(m_targetDepthRenderTargetIdentifier,
                m_copyCameraDepthRT.Identifier());

            context.ExecuteCommandBuffer(command);
            CommandBufferPool.Release(command);
        }

        /// <inheritdoc/>
        public override void FrameCleanup(CommandBuffer command)
        {
            command.ReleaseTemporaryRT(m_copyCameraDepthRT.id);
        }

        private RenderTargetHandle m_copyCameraDepthRT;
        private RenderTargetIdentifier m_targetDepthRenderTargetIdentifier;
        const string mc_profilerTag = "Copy Depth";
        private float m_renderScale = 1.0f;
    }
}
