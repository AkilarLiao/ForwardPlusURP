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

        public void Setup(RenderTargetIdentifier depthRenderTargetIdentifier)
        {
            m_targetDepthRenderTargetIdentifier = depthRenderTargetIdentifier;
        }

        public override void Configure(CommandBuffer cmd,
            RenderTextureDescriptor cameraTextureDescriptor)
        {
            var descriptor = cameraTextureDescriptor;
            descriptor.colorFormat = RenderTextureFormat.RFloat;
            descriptor.depthBufferBits = 32;
            descriptor.msaaSamples = 1;
            descriptor.width = Screen.width;
            descriptor.height = Screen.height;
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
    }
}
