using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CustomRender
{
    public class PrecomputeFrustumsPass : ScriptableRenderPass
    {
        public PrecomputeFrustumsPass(ComputeShader targetPrecomputeFrustumsCS)
        {
            if (!targetPrecomputeFrustumsCS)
                return;
            m_targetPrecomputeFrustumsCS = targetPrecomputeFrustumsCS;
            m_precomputeFrustumsCSID =
                m_targetPrecomputeFrustumsCS.FindKernel("CSMain");
            renderPassEvent = RenderPassEvent.BeforeRendering;
        }

        //~PrecomputeFrustumsPass()
        //{
        //    if (m_frustumsBuffer != null)
        //    {
        //        m_frustumsBuffer.Release();
        //        m_frustumsBuffer.Dispose();
        //    }
        //}

        public void Release()
        {
            if (m_frustumsBuffer != null)
            {
                m_frustumsBuffer.Release();
                m_frustumsBuffer.Dispose();
            }
        }

        public void Setup(ref Vector2 screenSizeRatio, int[] frustumTileSize, 
            float[] inverseMatrixFloats, int numFrustumsX, int numFrustumsY)
        {
            if (!m_targetPrecomputeFrustumsCS)
                return;
            m_targetPrecomputeFrustumsCS.SetFloats("_InverseProjection",
                inverseMatrixFloats);

            bool isScreenResolutionChange = false;
            if ((m_frustumsBuffer == null) || (m_numFrustumsX != numFrustumsX) ||
                (m_numFrustumsY != numFrustumsY))
            {
                isScreenResolutionChange = true;
                m_numFrustumsX = numFrustumsX;
                m_numFrustumsY = numFrustumsY;
                if (m_frustumsBuffer != null)
                {
                    m_frustumsBuffer.Release();
                    m_frustumsBuffer.Dispose();
                }
                int stride = Marshal.SizeOf(typeof(PreBuildFrustum));
                m_frustumsBuffer = new ComputeBuffer(m_numFrustumsX * m_numFrustumsY, stride);
            }
#if UNITY_EDITOR
            isScreenResolutionChange = true;
#endif
            if (isScreenResolutionChange)
            {
                //m_targetPrecomputeFrustumsCS.SetVector("_ScreenResolutionData",
                //screenResolutionData);
                m_targetPrecomputeFrustumsCS.SetVector("_ScreenSizeRatio",
                    screenSizeRatio);
                m_targetPrecomputeFrustumsCS.SetInts("_FrustumTileSize", frustumTileSize);
                m_targetPrecomputeFrustumsCS.SetBuffer(m_precomputeFrustumsCSID,
                    "_Frustums", m_frustumsBuffer);                
            }
        }
        public override void Execute(ScriptableRenderContext context,
            ref RenderingData renderingData)
        {
            if (!m_targetPrecomputeFrustumsCS)
                return;
            int dispacthX = (int)System.Math.Ceiling(m_numFrustumsX / 16.0f);
            int dispacthY = (int)System.Math.Ceiling(m_numFrustumsY / 16.0f);
            CommandBuffer command = CommandBufferPool.Get("PreComputeFrustums");
            command.DispatchCompute(m_targetPrecomputeFrustumsCS, m_precomputeFrustumsCSID,
                dispacthX, dispacthY, 1);
            context.ExecuteCommandBuffer(command);
            CommandBufferPool.Release(command);
        }
        public ComputeBuffer GetFrustumsBuffer() { return m_frustumsBuffer; }
        private ComputeShader m_targetPrecomputeFrustumsCS = null;
        private int m_precomputeFrustumsCSID = -1;
        private ComputeBuffer m_frustumsBuffer = null;
        struct Plane
        {
            public Vector3 m_normal;
            public float m_distance;  //distance from origin
            public Plane(ref Vector3 normal, float distance)
            {
                this.m_normal = normal;
                this.m_distance = distance;
            }
        };
        struct PreBuildFrustum
        {
            public Plane m_leftPlane;
            public Plane m_upPlane;
            public Plane m_rightPlane;
            public Plane m_downPlane;
            public PreBuildFrustum(ref Plane leftPlane, ref Plane upPlane, ref Plane rightPlane,
                ref Plane downPlane)
            {
                m_leftPlane = leftPlane;
                m_upPlane = upPlane;
                m_rightPlane = rightPlane;
                m_downPlane = downPlane;
            }
        };
        private int m_numFrustumsX = -1;
        private int m_numFrustumsY = -1;
    }
}
