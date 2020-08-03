using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CustomRender
{
    public class TileLightCullingPass : ScriptableRenderPass
    {
        public static uint GetCurrentLightCount() {return ms_currentLightCount; }
        public TileLightCullingPass(ComputeShader TileLightCullingCS)
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingPrepasses;   
            m_targetTileLightCullingCS = TileLightCullingCS;
            m_tileLightCullingCSID = m_targetTileLightCullingCS.FindKernel("CSMain");
            int stride = Marshal.SizeOf(typeof(CustomRender.TileLightCullingPass.
                LightData)) * 4;
            if (m_lightListBuffer != null)
            {
                m_lightListBuffer.Dispose();
                m_lightListBuffer = null;
            }
            m_lightListBuffer = new ComputeBuffer((int)mc_maxLightCount,
                stride, ComputeBufferType.Default);

            m_targetTileLightCullingCS.SetBuffer(
                m_tileLightCullingCSID,
                "_Lights",
                m_lightListBuffer);

            if (m_currentLightIndexBuffer != null)
            {
                m_currentLightIndexBuffer.Dispose();
                m_currentLightIndexBuffer = null;
            }
            m_currentLightIndexBuffer = new ComputeBuffer(1, 4,
                ComputeBufferType.Default);
            m_currentLightIndexBuffer.SetData(m_zeroLightIndexBuffer);
            m_targetTileLightCullingCS.SetBuffer(
                m_tileLightCullingCSID,
                "_CurrentIndex",
                m_currentLightIndexBuffer);

            //clear all lightData
            for (int i = 0; i < mc_maxLightCount; ++i)
            {
                m_lightsDatas[i] = new LightData(
                    Vector3.zero, 0, Vector3.zero, 0, Vector4.zero);
            }

            m_copyCameraDepthRTIdentifier = 
                new RenderTargetIdentifier("_CopyCameraDepthRT");
        }

        public void Release()
        {
            if (m_lightListBuffer != null)
            {
                m_lightListBuffer.Dispose();
                m_lightListBuffer = null;
            }

            if (m_currentLightIndexBuffer != null)
            {
                m_currentLightIndexBuffer.Dispose();
                m_currentLightIndexBuffer = null;
            }

            if (m_lightsGridRT)
                SafeDestroy(m_lightsGridRT);
        }

        public void Setup(ComputeBuffer frustumBuffer,
            float[] inverseMatrixFloats, ref Vector2 screenSizeRatio,
            int[] frustumTileSize, int numfrustumsX, int numfrustumsY)
        {
            m_targetTileLightCullingCS.SetBuffer(m_tileLightCullingCSID, "_Frustums",
                frustumBuffer);
            m_targetTileLightCullingCS.SetFloats("_InverseProjection", inverseMatrixFloats);
#if UNITY_EDITOR
            bool isNumfrustumsChange = true;
#else
            bool isNumfrustumsChange = false;
#endif
            if ((m_oldNumfrustumsX != numfrustumsX) ||
                (m_oldNumfrustumsY != numfrustumsY))
            {
                isNumfrustumsChange = true;
                m_oldNumfrustumsX = numfrustumsX;
                m_oldNumfrustumsY = numfrustumsY;
                if (m_lightsIndexBuffer != null)
                {
                    m_lightsIndexBuffer.Release();
                    m_lightsIndexBuffer.Dispose();
                }
                m_lightsIndexBuffer = new ComputeBuffer(
                    mc_lightsPreTile * m_oldNumfrustumsX * m_oldNumfrustumsY, 4,
                    ComputeBufferType.Default);
            }
            if (isNumfrustumsChange)
            {
                m_targetTileLightCullingCS.SetBuffer(m_tileLightCullingCSID,
                    "_LightsIndexBuffer", m_lightsIndexBuffer);
            }

            if ((!m_lightsGridRT) || (m_lightsGridRT.width != numfrustumsX) ||
                (m_lightsGridRT.height != numfrustumsY))
            {
                if (m_lightsGridRT)
                    SafeDestroy(m_lightsGridRT);

                m_lightsGridRT = new RenderTexture(numfrustumsX, numfrustumsY,
                    0, RenderTextureFormat.RGInt, RenderTextureReadWrite.Linear);
                m_lightsGridRT.name = "LightsGrid";
                m_lightsGridRT.hideFlags = HideFlags.HideAndDontSave;
                m_lightsGridRT.filterMode = FilterMode.Point;
                m_lightsGridRT.enableRandomWrite = true;
                m_lightsGridRT.Create();                
            }
            
            if (isNumfrustumsChange)
            {
                m_targetTileLightCullingCS.SetTexture(m_tileLightCullingCSID,
                    "_LightsGridRT", m_lightsGridRT);
                m_targetTileLightCullingCS.SetVector("_ScreenSizeRatio",
                    screenSizeRatio);
                m_targetTileLightCullingCS.SetInts("_FrustumTileSize", frustumTileSize);
            }

            m_currentLightIndexBuffer.SetData(m_zeroLightIndexBuffer);
            m_targetTileLightCullingCS.SetBuffer(
                m_tileLightCullingCSID,
                "_CurrentIndex",
                m_currentLightIndexBuffer);
        }
        public override void Execute(ScriptableRenderContext context,
            ref RenderingData renderingData)
        {
            SetupLightDatas(ref renderingData.lightData);
            Camera camera = renderingData.cameraData.camera;
            Matrix4x4 matrix = camera.worldToCameraMatrix;
            for (int rowIndex = 0; rowIndex<4; ++rowIndex)
            {
                for (int columnIndex = 0; columnIndex<4; ++columnIndex)
                {
                    m_worldToViewMatrixFloats[columnIndex + rowIndex*4]
                        = matrix[columnIndex, rowIndex];
                }
            }

            m_targetTileLightCullingCS.SetFloats("_WorldToViewMatrix",
                m_worldToViewMatrixFloats);

            CommandBuffer cmd = CommandBufferPool.Get("ForwardPlusLightCulling");
            var depthDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            depthDescriptor.colorFormat = RenderTextureFormat.RFloat;
            depthDescriptor.width = 
                (int)(depthDescriptor.width / renderingData.cameraData.renderScale);
            depthDescriptor.height = 
                (int)(depthDescriptor.height / renderingData.cameraData.renderScale);
            depthDescriptor.depthBufferBits = 32;
            
            cmd.SetComputeTextureParam(m_targetTileLightCullingCS, m_tileLightCullingCSID,
                "_DepthBuffer", m_copyCameraDepthRTIdentifier);

            cmd.DispatchCompute(
                m_targetTileLightCullingCS, m_tileLightCullingCSID,
                m_lightsGridRT.width, m_lightsGridRT.height, 1); 

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            CommandBufferPool.Release(cmd);

            //set the lights list globally
            Shader.SetGlobalTexture("_LightsGridRT", m_lightsGridRT);
            Shader.SetGlobalBuffer("_LightsIndexBuffer", m_lightsIndexBuffer);
            Shader.SetGlobalBuffer("_Lights", m_lightListBuffer);
        }

        private void SafeDestroy<T>(T obj) where T : Object
        {
            if (obj == null)
                return;
            if (Application.isEditor)
                Object.DestroyImmediate(obj);
            else
                Object.Destroy(obj);

            obj = null;
        }

        private void SetupLightDatas(ref UnityEngine.Rendering.Universal.LightData
            lightData)
        {
            //populate the buffer with le list of lights
            var lights = lightData.visibleLights;
            int index = 0;
            uint destLightCount = 0;
            Vector4 lightAttenuation;
            for (int i = 0; i<lights.Length; ++i)
            {
                VisibleLight light = lights[i];
                Vector4 pos = light.localToWorldMatrix.GetColumn(3);
                if (lightData.mainLightIndex != i)
                {
                    Vector3 col = new Vector3(
                        light.finalColor.r,
                        light.finalColor.g,
                        light.finalColor.b);

                    SetupLightAttenuation(ref light, out lightAttenuation);

                    //calculate light attenuation
                    float lightRangeSqr = light.range * light.range;
                    float fadeStartDistanceSqr = 0.8f * 0.8f * lightRangeSqr;
                    float fadeRangeSqr = (fadeStartDistanceSqr - lightRangeSqr);
                    float oneOverFadeRangeSqr = 1.0f / fadeRangeSqr;
                    float lightRangeSqrOverFadeRangeSqr = -lightRangeSqr / fadeRangeSqr;
                    float oneOverLightRangeSqr = 1.0f / Mathf.Max(0.0001f,
                        lightRangeSqr);
                    lightAttenuation.x = Application.isMobilePlatform || 
                        SystemInfo.graphicsDeviceType ==
                        GraphicsDeviceType.Switch ? oneOverFadeRangeSqr : oneOverLightRangeSqr;
                    lightAttenuation.y = lightRangeSqrOverFadeRangeSqr;

                    m_lightsDatas[index++] = new LightData(
                        pos, 1, col, light.range, lightAttenuation);
                    ++destLightCount;
                    if (index >= mc_maxLightCount)
                        break;
                }
                else
                {
                    Shader.SetGlobalVector("_MainLightColor", light.finalColor);
                    Shader.SetGlobalVector("_MainLightPosition", pos.normalized);
                }
            }

            if (destLightCount != ms_currentLightCount)
            {
                //清空index~ms_currentLightCount
                if (ms_currentLightCount > destLightCount)
                {
                    for (int i = index; i < ms_currentLightCount; ++i)
                    {
                        m_lightsDatas[i] = new LightData(
                            Vector3.zero, 0.0f, Vector3.zero, 0.0f, mc_defaultLightAttenuation);
                    }
                }
                ms_currentLightCount = destLightCount;
            }

            m_lightListBuffer.SetData(m_lightsDatas);            
        }

        private void SetupLightAttenuation(ref VisibleLight light, out Vector4 lightAttenuation)
        {
            lightAttenuation = mc_defaultLightAttenuation;
            //calculate light attenuation
            float lightRangeSqr = light.range * light.range;
            float fadeStartDistanceSqr = 0.8f * 0.8f * lightRangeSqr;
            float fadeRangeSqr = (fadeStartDistanceSqr - lightRangeSqr);
            float oneOverFadeRangeSqr = 1.0f / fadeRangeSqr;
            float lightRangeSqrOverFadeRangeSqr = -lightRangeSqr / fadeRangeSqr;
            float oneOverLightRangeSqr = 1.0f / Mathf.Max(0.0001f,
                lightRangeSqr);
            lightAttenuation.x = Application.isMobilePlatform ||
                SystemInfo.graphicsDeviceType ==
                GraphicsDeviceType.Switch ? oneOverFadeRangeSqr : oneOverLightRangeSqr;
            lightAttenuation.y = lightRangeSqrOverFadeRangeSqr;
        }

        private static uint ms_currentLightCount = 0;
        private uint[] m_zeroLightIndexBuffer = { 0 };

        private ComputeShader m_targetTileLightCullingCS = null;
        private int m_tileLightCullingCSID = -1;
        private float[] m_worldToViewMatrixFloats = new float[16];

        //only point lights at the moment
        public struct LightData
        {
            public Vector3 m_worldSpacePosition;
            public float m_enabled;
            public Vector3 m_color;
            public float m_range;
            public Vector4 m_attenuation;
            public LightData(Vector3 worldSpacePosition, float enabled,
                Vector3 color, float range, Vector4 attenuation)
            {
                m_worldSpacePosition = worldSpacePosition;
                m_enabled = enabled;
                m_color = color;
                m_range = range;
                m_attenuation = attenuation;
            }
        }
        private Vector4 mc_defaultLightAttenuation = new Vector4(0.0f, 1.0f, 0.0f, 1.0f);
        private const uint mc_maxLightCount = 256; //16 * 16
        private LightData[] m_lightsDatas = new LightData[mc_maxLightCount];

        private ComputeBuffer m_lightListBuffer;
        private ComputeBuffer m_currentLightIndexBuffer;
        private ComputeBuffer m_lightsIndexBuffer;

        private int m_oldNumfrustumsX = -1;
        private int m_oldNumfrustumsY = -1;

        private const int mc_lightsPreTile = 64;
        private RenderTexture m_lightsGridRT = null;
        private RenderTargetIdentifier m_copyCameraDepthRTIdentifier;
    }
}