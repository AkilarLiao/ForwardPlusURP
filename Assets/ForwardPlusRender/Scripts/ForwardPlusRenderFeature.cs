using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

namespace CustomRender
{
    public class ForwardPlusRenderFeature : ScriptableRendererFeature
    {
        public override void Create()
        {
            if (m_targetPrecomputeFrustumsCS)
            {
                if (m_precomputeFrustumsPass != null)
                    m_precomputeFrustumsPass.Release();

                m_precomputeFrustumsPass = new PrecomputeFrustumsPass(
                    m_targetPrecomputeFrustumsCS);
            }
            
            m_depthOnlyPass = new DepthOnlyPass();
                
            m_copyDepthPass = new CopyDepthPass();

            if (m_targetForwardPlusLightCullingCS)
            {
                if(m_tileLightCullingPass != null)
                    m_tileLightCullingPass.Release();

                m_tileLightCullingPass = new TileLightCullingPass(
                    m_targetForwardPlusLightCullingCS);
            }

            if (m_samplingShader)
            {
                Material samplingMaterial = CoreUtils.CreateEngineMaterial(
                    m_samplingShader);
                m_copyColorPass = new CopyColorPass(
                    RenderPassEvent.BeforeRenderingPostProcessing, samplingMaterial);
             }

            if (m_showTileLightGridShader && m_heatMap)
            {
                m_showDebugTileLightGridMaterial = CoreUtils.CreateEngineMaterial(
                    m_showTileLightGridShader);
                m_showDebugTileLightGridMaterial.SetTexture("_HeatMap", m_heatMap);
                m_showLightGridsPass = new ShowLightGridsPass(m_showDebugTileLightGridMaterial);
            }
            m_backgroundRT.Init("_BackGroundRT");
        }

        private void OnDisable()
        {
            if (m_showDebugTileLightGridMaterial)
                CoreUtils.Destroy(m_showDebugTileLightGridMaterial);

            if (m_tileLightCullingPass != null)
                m_tileLightCullingPass.Release();

            if (m_precomputeFrustumsPass != null)
                m_precomputeFrustumsPass.Release();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer,
            ref RenderingData renderingData)
        {
            int numFrustumsX, numFrustumsY;            
            Vector2 screenSizeRatio;
            SetupScreenReferenceInfo(ref renderingData, out numFrustumsX, out numFrustumsY,
                out screenSizeRatio);

            if (m_precomputeFrustumsPass != null)
            {
                m_precomputeFrustumsPass.Setup(ref screenSizeRatio, m_frustumTileSize,
                    m_inverseProjectionMatrixFloats, numFrustumsX, numFrustumsY);
                renderer.EnqueuePass(m_precomputeFrustumsPass);
            }

            renderer.EnqueuePass(m_depthOnlyPass);

            if (m_copyDepthPass != null)
            {
                m_copyDepthPass.Setup(renderer.cameraDepth, 
                    renderingData.cameraData.renderScale);
                renderer.EnqueuePass(m_copyDepthPass);
            }

            if (m_tileLightCullingPass != null)
            {
                m_tileLightCullingPass.Setup(
                    m_precomputeFrustumsPass.GetFrustumsBuffer(), 
                    m_inverseProjectionMatrixFloats,
                    ref screenSizeRatio, m_frustumTileSize, numFrustumsX,
                    numFrustumsY);
                renderer.EnqueuePass(m_tileLightCullingPass);
            }

            if (m_copyColorPass!= null)
            {
                Downsampling downsamplingMethod =
                    UniversalRenderPipeline.asset.opaqueDownsampling;
                m_copyColorPass.Setup(renderer.cameraColorTarget,
                    m_backgroundRT, downsamplingMethod);
                renderer.EnqueuePass(m_copyColorPass);
            }

            if ((m_showLightGridsPass == null) || m_showTileLightGridRatio <= 0.0f)
                return;
            m_showDebugTileLightGridMaterial.SetColor("_GridColor", m_tileLightGridColor);
            m_showDebugTileLightGridMaterial.SetFloat("_Show", m_showTileLightGridRatio);
            m_showLightGridsPass.Setup(renderer.cameraColorTarget);
            renderer.EnqueuePass(m_showLightGridsPass);
        }

        private void SetupScreenReferenceInfo(ref RenderingData renderingData,
            out int numFrustumsX, out int numFrustumsY, out Vector2 screenSizeRatio)
        {
            Camera camera = renderingData.cameraData.camera;
            Matrix4x4 matrix = GL.GetGPUProjectionMatrix(camera.projectionMatrix,
                false);
            matrix = matrix.inverse;
            for (int rowIndex = 0; rowIndex<4; ++rowIndex)
            {
                for (int columnIndex = 0; columnIndex<4; ++columnIndex)
                {
                    m_inverseProjectionMatrixFloats[columnIndex + rowIndex*4]
                        = matrix[columnIndex, rowIndex];
                }
            }
            ref RenderTextureDescriptor cameraTargetDescriptor =
                ref renderingData.cameraData.cameraTargetDescriptor;
            int screenWidth =
                (int)(cameraTargetDescriptor.width / renderingData.cameraData.renderScale);
            int screenHeight =
                (int)(cameraTargetDescriptor.height / renderingData.cameraData.renderScale);

            numFrustumsX = (int)System.Math.Ceiling(screenWidth / 16.0f);
            numFrustumsY = (int)System.Math.Ceiling(screenHeight / 16.0f);

            screenSizeRatio  = new Vector2(1.0f / screenWidth, 1.0f / screenHeight);
            m_frustumTileSize[0] = numFrustumsX;
            m_frustumTileSize[1] = numFrustumsY;
        }

        [SerializeField]
        private ComputeShader m_targetPrecomputeFrustumsCS = null;
        [SerializeField]
        private ComputeShader m_targetForwardPlusLightCullingCS = null;
        [SerializeField]
        private Shader m_samplingShader = null;

        [SerializeField]
        private Shader m_showTileLightGridShader = null;

        [SerializeField]
        private Texture2D m_heatMap = null;

        [SerializeField]
        private Color m_tileLightGridColor = Color.white;

        [Range(0.0f, 1.0f)]
        [SerializeField]
        private float m_showTileLightGridRatio = 0.0f;

        private Material m_showDebugTileLightGridMaterial = null;

        private float[] m_inverseProjectionMatrixFloats = new float[16];
        private RenderTargetHandle m_backgroundRT;
        //referece pass
        private PrecomputeFrustumsPass m_precomputeFrustumsPass = null;
        private DepthOnlyPass m_depthOnlyPass = null;
        private CopyDepthPass m_copyDepthPass = null;
        private TileLightCullingPass m_tileLightCullingPass = null;
        private CopyColorPass m_copyColorPass = null;
        private ShowLightGridsPass m_showLightGridsPass = null;
        private int[] m_frustumTileSize = new int[2];
    }    
}


