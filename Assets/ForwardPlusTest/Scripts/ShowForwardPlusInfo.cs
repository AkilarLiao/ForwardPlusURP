using UnityEngine;
using CustomRender;

public class ShowForwardPlusInfo : MonoBehaviour
{
    // Start is called before the first frame update
    private void Start()
    {
        DisplayFPS displayFPS = GetComponent<DisplayFPS>();
        if (displayFPS != null)
            displayFPS.AppendExtendString += AppendExtendStringCB;
    }
    private void OnDestroy()
    {
        DisplayFPS displayFPS = GetComponent<DisplayFPS>();
        if (displayFPS != null)
            displayFPS.AppendExtendString -= AppendExtendStringCB;
    }

    private void AppendExtendStringCB(out string text)
    {
        text = string.Format("\nCurrentLightCount{0}",
            TileLightCullingPass.GetCurrentLightCount());
    }

    // Update is called once per frame
    private void Update()
    {   
    }
}