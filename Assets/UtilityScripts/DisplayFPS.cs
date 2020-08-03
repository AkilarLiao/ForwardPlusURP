using UnityEngine;

public class DisplayFPS : MonoBehaviour
{
    public delegate void AppendExtendStringCB(out string text);
    public AppendExtendStringCB AppendExtendString { get; set; }

    private void Start()
    {
        m_style = new GUIStyle();
        m_style.alignment = m_leftSide ? TextAnchor.UpperLeft : TextAnchor.UpperRight;
        m_style.normal.textColor = m_textColor;
        m_restPrintFPSTime = m_printFPSTime;
    }
    private void Update()
    {
        m_deltaTime += (Time.deltaTime - m_deltaTime) * 0.1f;
        m_restPrintFPSTime -= Time.deltaTime;
        if (m_restPrintFPSTime < 0.0f)
            m_restPrintFPSTime = 0.0f;
    }

    private void OnGUI()
    {
        if (!m_showDebugInfo)
            return;
        if (m_restPrintFPSTime <= 0.0)
        {
            m_restPrintFPSTime = m_printFPSTime;
            int width = Screen.width, height = Screen.height;
            ms_tempPrintRect = new Rect(0, height - (int)(1.0f * (float)height), width, height * 2 / 100);
            m_style.fontSize = height * 5 / 100;

            if (m_displayFPS)
            {
                float msec = m_deltaTime * 1000.0f;
                float fPS = 1.0f / m_deltaTime;
                ms_tempFPSText = string.Format("  {0:0.0} ms ({1:0.} fps)", msec, fPS);
            }
            else
                ms_tempFPSText = "";

            if (!m_onlyShowFPS)
            {   
                ms_tempFPSText += "\ngraphicsDeviceType:" + SystemInfo.graphicsDeviceType;
                ms_tempFPSText += "\ngraphicsDeviceName:" + SystemInfo.graphicsDeviceName;
                ms_tempFPSText += "\ngraphicsMultiThreaded:" + SystemInfo.graphicsMultiThreaded;
                ms_tempFPSText += "\nsupportsInstancing:" + SystemInfo.supportsInstancing;
                ms_tempFPSText += "\nsupportsComputeShaders:" + SystemInfo.supportsComputeShaders;
                ms_tempFPSText += "\nmaxComputeBufferInputsCompute:" + SystemInfo.maxComputeBufferInputsCompute;
                ms_tempFPSText += "\nmaxComputeBufferInputsFragment:" + SystemInfo.maxComputeBufferInputsFragment;
                
            }

            if (AppendExtendString != null)
            {
                string text;
                AppendExtendString(out text);
                ms_tempFPSText += text;
            }
        }
        if (ms_tempFPSText == null)
            return;
        
        GUI.Label(ms_tempPrintRect, ms_tempFPSText, m_style);
    }

    
    [SerializeField]
    private Color m_textColor = new Color(0.0f, 0.0f, 0.5f, 1.0f);    
    [SerializeField]
    private bool m_showDebugInfo = true;
    [SerializeField]
    private bool m_onlyShowFPS = false;
    [SerializeField]
    private bool m_leftSide = false;
    [SerializeField]
    private bool m_displayFPS = true;

    private float m_deltaTime = 0.0f;
    private GUIStyle m_style = null;
    private float m_printFPSTime = 1.0f;
    private float m_restPrintFPSTime = 0.0f;
    private static string ms_tempFPSText = null;
    private static Rect ms_tempPrintRect;
}