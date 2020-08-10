using UnityEngine;
using UnityEngine.EventSystems;

public class CollisionManager : Singleton<CollisionManager>
{
    //The touch eventType
    public enum TOUCH_EVENT_TYPE
    {
        PRESS,
        RELEASE,
        MOVE,
        NONE
    };    

    //The touch information declartion
    public struct TouchInformation
    {
        public TOUCH_EVENT_TYPE m_type;
        public Vector2 m_touchPosition;
        public Vector2 m_touchDelta;
        public void Clear()
        {
            m_type = TOUCH_EVENT_TYPE.NONE;
            m_touchPosition = Vector2.zero;
            m_touchDelta = Vector2.zero;
        }
    };

    // event 為關鍵字，可提供delegate成員無法使用=及呼叫invok，可確保不會被外部使用，造成bug.
    public delegate void TouchEventHandler(TouchInformation touchInfo, TouchInformation touchInfo1,
        bool injectGUI, GameObject hitGameObject);
    public event TouchEventHandler m_pressEventHandler = null;
    public event TouchEventHandler m_moveEventHandler = null;
    public event TouchEventHandler m_releaseEventHandler = null;

    private void SetupPerTouchInformation(ref Touch theTouch, ref TouchInformation touchInformation)
    {
        touchInformation.m_touchDelta -= theTouch.deltaPosition;
        touchInformation.m_touchPosition = theTouch.position;
        TouchPhase TheTouchPhase = theTouch.phase;
        if (TheTouchPhase == TouchPhase.Began)
        {
            touchInformation.m_type = TOUCH_EVENT_TYPE.PRESS;
            m_firstTouch = true;
        }
        else if (TheTouchPhase == TouchPhase.Ended)
        {
            touchInformation.m_type = TOUCH_EVENT_TYPE.RELEASE;
            m_firstTouch = false;
        }
        else if (TheTouchPhase == TouchPhase.Moved)
            touchInformation.m_type = TOUCH_EVENT_TYPE.MOVE;
    }


    private void SetupTouchInformation()
    {
        m_touchInformation.Clear();
        m_touchInformation1.Clear();
#if UNITY_EDITOR || UNITY_STANDALONE
        m_touchInformation.m_touchPosition = Input.mousePosition;
        if (Input.GetMouseButton(0))
        {
            if (!m_firstTouch)
            {
                m_firstTouch = true;
                m_touchInformation.m_type = TOUCH_EVENT_TYPE.PRESS;
            }
            else
            {
                m_touchInformation.m_touchDelta.x -= Input.GetAxis("Mouse X");
                m_touchInformation.m_touchDelta.y -= Input.GetAxis("Mouse Y");
                if ((Mathf.Abs(m_touchInformation.m_touchDelta.x) > 0.0f) ||
                    (Mathf.Abs(m_touchInformation.m_touchDelta.y) > 0.0f))
                    m_touchInformation.m_type = TOUCH_EVENT_TYPE.MOVE;
                else
                    m_touchInformation.m_type = TOUCH_EVENT_TYPE.NONE;
            }
        }
        else
        {
            if (m_firstTouch)
            {
                m_firstTouch = false;
                m_touchInformation.m_type = TOUCH_EVENT_TYPE.RELEASE;
            }
            else
                m_touchInformation.m_type = TOUCH_EVENT_TYPE.NONE;
        }
#else
        if ((Input.touchCount <= 0))
            return;
        Touch theTouch = Input.GetTouch(0);
        SetupPerTouchInformation(ref theTouch, ref m_touchInformation);
        if (Input.touchCount >= 2)
        {
            theTouch = Input.GetTouch(1);
            SetupPerTouchInformation(ref theTouch, ref m_touchInformation1);
        }
#endif
    }

    private void Update()
    {        
        SetupTouchInformation();
        if (m_touchInformation.m_type != TOUCH_EVENT_TYPE.NONE)
        {
            GameObject hitGameObject = null;
            bool injectGUI = false;
            if (EventSystem.current != null)
            {
#if UNITY_EDITOR || UNITY_STANDALONE
                injectGUI = EventSystem.current.IsPointerOverGameObject();
#else           
                injectGUI = EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
#endif
            }
            if(injectGUI == false)
            {
                if (Camera.main)
                {
                    Ray testRay = Camera.main.ScreenPointToRay(
                        new Vector3(m_touchInformation.m_touchPosition.x, m_touchInformation.m_touchPosition.y,
                        0.0f));
                    RaycastHit[] theHits = Physics.RaycastAll(testRay, Mathf.Infinity);

                    float compareDistance = -1;
                    for (int index = 0; index<theHits.Length; ++index)
                    {
                        if ((compareDistance < 0.0f) ||　(compareDistance > theHits[index].distance))
                        {
                            compareDistance = theHits[index].distance;
                            hitGameObject = theHits[index].collider.gameObject;
                        }
                    }
                }
            }

            switch (m_touchInformation.m_type)
            {
                case TOUCH_EVENT_TYPE.PRESS:
                    if (m_pressEventHandler != null)
                        m_pressEventHandler(m_touchInformation, m_touchInformation1, injectGUI, hitGameObject);
                    break;
                case TOUCH_EVENT_TYPE.RELEASE:
                    if (m_releaseEventHandler != null)
                        m_releaseEventHandler(m_touchInformation, m_touchInformation1, injectGUI, hitGameObject);
                    break;
                case TOUCH_EVENT_TYPE.MOVE:
                    if (m_moveEventHandler != null)
                        m_moveEventHandler(m_touchInformation, m_touchInformation1, injectGUI, hitGameObject);
                    break;
            }
        }
    }
    private TouchInformation m_touchInformation;
    private TouchInformation m_touchInformation1;
    private bool m_firstTouch = false;
}
