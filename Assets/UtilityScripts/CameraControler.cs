using UnityEngine;

public class CameraControler : MonoBehaviour
{
    public float KeyboardMoveSpeed { set { m_moveSpeed = value; } }
    public float DragMoveSpeed { set { m_speed = value; } }
    public float AccumulateMoveRatio { set { m_accumulateMoveRatio = value; } }

    private void OnEnable()
    {
        m_selfTransform = transform;
        if (m_useBornInfo)
        {
            m_selfTransform.position = m_bornPosition;
            m_selfTransform.localEulerAngles = m_rotation;
        }

        if (CollisionManager.Instance != null)
        {
            CollisionManager.Instance.m_pressEventHandler += ProcessTouchPress;
            CollisionManager.Instance.m_moveEventHandler += ProcessTouchMove;
            CollisionManager.Instance.m_releaseEventHandler += ProcessTouchRelease;
        }

        DisplayFPS displayFPS = GetComponent<DisplayFPS>();
        if(displayFPS)
            displayFPS.AppendExtendString += AppendExtendStringCB;
    }

    void AppendExtendStringCB(out string text)
    {
        if (m_ShowPosition)
            text = System.String.Format("\nCameraPosition:{0}", m_selfTransform.position);
        else
            text = "";
    }

    private void OnDisable()
    {
        DisplayFPS displayFPS = GetComponent<DisplayFPS>();
        if (displayFPS)
            displayFPS.AppendExtendString -= AppendExtendStringCB;

        if (!CollisionManager.IsApplicationIsQuitting() &&(CollisionManager.Instance != null))
        {
            CollisionManager.Instance.m_releaseEventHandler -= ProcessTouchRelease;
            CollisionManager.Instance.m_moveEventHandler -= ProcessTouchMove;
            CollisionManager.Instance.m_pressEventHandler -= ProcessTouchPress;
        }
    }
    
    private void Update()
    {
        ProcessRotate();
        ProcessRoomIn();

        ProcessKeyboardMovement();
        if (m_pressed)
            m_pressTime += Time.deltaTime;

        if (m_deltaMovement.sqrMagnitude <= mc_fMinDeltaSquareMagnitude)
        {
            m_deltaMovement = Vector2.zero;
            return;
        }

        float fDestRatio = Mathf.Min(mc_fDeltaMoveScaleRatio * Time.deltaTime, 1.0f);
        Vector2 DestVelocity = m_deltaMovement * fDestRatio;
        if ((Mathf.Abs(DestVelocity.x) > 0.0f) ||
            (Mathf.Abs(DestVelocity.y) > 0.0f))
        {
            if (!m_isRoomIn)
            {
                Vector3 FaceDirection, RightDirection;
                GetMoveDirection(out FaceDirection, out RightDirection);
                Vector3 FixVelocity = Vector2.zero;
                FixVelocity += DestVelocity.y * FaceDirection;
                FixVelocity += DestVelocity.x * RightDirection;
                if (FixVelocity.magnitude <= 100.0f)
                    m_selfTransform.position -= FixVelocity;
                else
                    Debug.Log(string.Format("Happen Huge Distance: {0}", FixVelocity.magnitude));
            }
            else
            {
            }
        }
        m_deltaMovement -= DestVelocity;
    }

    void ProcessRotate()
    {
        if (Input.GetMouseButtonDown(1))
        {
            m_mouseRightButtonPress = true;
            m_oldMousePosition = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(1))
        {
            m_mouseRightButtonPress = false;
        }

        if (m_secondTouchMode != SECOND_TOUCH_MODE.ROTATE)
            return;

        if (m_mouseRightButtonPress)
        {
            m_mouseRightButtonPress = true;
            Vector3 Delta = Input.mousePosition - m_oldMousePosition;
            Vector3.Magnitude(Delta);
            m_oldMousePosition = Input.mousePosition;
            m_accYawAngle += m_rotateSpeed * Delta.x;
            m_accPitchAngle += m_rotateSpeed * Delta.y;
        }
        float fScaleValue = Time.deltaTime * m_destRotationScaleValue;
        fScaleValue = Mathf.Min(fScaleValue, 1.0f);
        Vector3 eulerAngles = m_selfTransform.eulerAngles;
        if (Mathf.Abs(m_accYawAngle) > 0.0f)
        {
            float fRotAngle = m_accYawAngle * fScaleValue;
            m_accYawAngle -= fRotAngle;
            eulerAngles.y += fRotAngle;
            m_selfTransform.eulerAngles = eulerAngles;
        }

        if (Mathf.Abs(m_accPitchAngle) > 0.0f)
        {
            float fRotAngle = m_accPitchAngle * fScaleValue;
            m_accPitchAngle -= fRotAngle;
            eulerAngles.x -= fRotAngle;
            m_selfTransform.eulerAngles = eulerAngles;
        }
    }

    private bool GetDestOffestRoomInRage(out float offestRange)
    {
        offestRange = 0.0f;
#if UNITY_EDITOR_WIN
        if ((!m_isRoomIn) || (!m_pressed))
            return false;
        offestRange = m_deltaX;
#else
        Touch touch1 = Input.GetTouch(0);
        Touch touch2 = Input.GetTouch(1);

        if ((touch1.phase != TouchPhase.Moved) && (touch2.phase != TouchPhase.Moved))
            return false;

        Vector2 touchPosition1 = touch1.position;
        Vector2 touchPosition2 = touch2.position;

        offestRange = (m_oldTouchPosition1 - m_oldTouchPosition2).sqrMagnitude < 
            (touchPosition1 - touchPosition2).sqrMagnitude ? -m_roomInDistance : m_roomInDistance;        

        m_oldTouchPosition1 = touchPosition1;
        m_oldTouchPosition2 = touchPosition2;
#endif
        return true;
    }

    private void ProcessRoomIn()
    {
        if (m_secondTouchMode != SECOND_TOUCH_MODE.ROOM_IN)
            return;

        float offestRange;
        if (!GetDestOffestRoomInRage(out offestRange))
            return;        

        Vector3 position = m_selfTransform.position;
        Vector3 direction = m_selfTransform.forward;
        Vector3 moveVector = direction * offestRange;
        Vector3 destPosition = position + moveVector;

        if ((destPosition.y >= m_roomInMinHeight) &&
            (destPosition.y <= m_roomInMaxHeight))
            m_selfTransform.position = destPosition;
    }

    private void ProcessKeyState()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            m_theMoveStates[(int)MoveState.MoveState_Forward] = true;
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            m_theMoveStates[(int)MoveState.MoveState_Left] = true;
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            m_theMoveStates[(int)MoveState.MoveState_Back] = true;
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            m_theMoveStates[(int)MoveState.MoveState_Right] = true;
        }

        if (Input.GetKeyUp(KeyCode.W))
        {
            m_theMoveStates[(int)MoveState.MoveState_Forward] = false;
        }
        else if (Input.GetKeyUp(KeyCode.A))
        {
            m_theMoveStates[(int)MoveState.MoveState_Left] = false;
        }
        else if (Input.GetKeyUp(KeyCode.S))
        {
            m_theMoveStates[(int)MoveState.MoveState_Back] = false;
        }
        else if (Input.GetKeyUp(KeyCode.D))
        {
            m_theMoveStates[(int)MoveState.MoveState_Right] = false;
        }

        if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl))
            m_isRoomIn = true;
        else if (Input.GetKeyUp(KeyCode.LeftControl) || Input.GetKeyUp(KeyCode.RightControl))
            m_isRoomIn = false;
    }
    private void ProcessKeyboardMovement()
    {
        ProcessKeyState();
        Vector3 moveMent = Vector3.zero;
        for (int index = 0; index < m_theMoveStates.Length; ++index)
        {
            if (!m_theMoveStates[index])
                continue;
            switch ((MoveState)index)
            {
                case MoveState.MoveState_Forward:
                    moveMent += m_moveSpeed * Vector3.forward;
                    break;
                case MoveState.MoveState_Left:
                    moveMent += m_moveSpeed * Vector3.left;
                    break;
                case MoveState.MoveState_Right:
                    moveMent += m_moveSpeed * Vector3.right;
                    break;
                case MoveState.MoveState_Back:
                    moveMent += m_moveSpeed * Vector3.back;
                    break;
            }
        }
        m_selfTransform.Translate(moveMent * Time.deltaTime);
    }    

    //The process touch founcitons
    private void ProcessTouchPress(CollisionManager.TouchInformation touchInfo,
        CollisionManager.TouchInformation touchInfo1, bool injectGUI, GameObject hitGameObject)
    {
        if (injectGUI)
            return;
        ProcessPressMovement(touchInfo);
        m_pressTime = 0.0f;
        m_touchPressPosition = touchInfo.m_touchPosition;
        m_deltaMovement = Vector2.zero;
        m_pressed = true;
    }

    private void ProcessTouchRelease(CollisionManager.TouchInformation touchInfo,
        CollisionManager.TouchInformation touchInfo1, bool injectGUI, GameObject hitGameObject)
    {
        if (m_pressed && (!m_isRoomIn))
        {
            Vector2 DeletaRange = m_touchPressPosition - touchInfo.m_touchPosition;
            if ((m_pressTime < mc_fMaxPressTime) && 
                (DeletaRange.sqrMagnitude >= mc_fMoveCheckRange * mc_fMoveCheckRange))
                m_deltaMovement += (DeletaRange / m_pressTime) * mc_fReleaseMoveTime * m_accumulateMoveRatio;
        }
        m_pressed = false;
        m_pressTime = 0.0f;
    }

    private void ProcessTouchMove(CollisionManager.TouchInformation touchInfo,
        CollisionManager.TouchInformation touchInfo1, bool injectGUI, GameObject hitGameObject)
    {
        if (m_pressed == false)
            return;
        m_deltaX = touchInfo.m_touchDelta.x;
        ProcessPressMovement(touchInfo);
    }

    private void GetMoveDirection(out Vector3 faceDirection, out Vector3 rightDirection)
    {
        faceDirection = m_selfTransform.forward;
        faceDirection.y = 0.0f;
        faceDirection = faceDirection.normalized;
        rightDirection = Quaternion.AngleAxis(90.0f, Vector3.up) * faceDirection;
    }

    private bool IsEnlarge(Vector2 oldP1, Vector2 oldP2, Vector2 newP1, Vector2 newP2)
    {
        float length1 = (oldP1 - oldP2).sqrMagnitude;
        float length2 = (newP1 - newP2).sqrMagnitude;
        return length1 < length2;
    }

    private void ProcessPressMovement(CollisionManager.TouchInformation touchInfo)
    {
        if (!m_isRoomIn)
        {
            Vector3 FaceDirection, RightDirection;
            GetMoveDirection(out FaceDirection, out RightDirection);
            Vector3 DestVelocity = Vector3.zero;
            DestVelocity += touchInfo.m_touchDelta.y * m_speed * FaceDirection;
            DestVelocity += touchInfo.m_touchDelta.x * m_speed * RightDirection;
            m_selfTransform.position -= DestVelocity;
        }
    }    

    private const float mc_fMoveCheckRange = 1.0f;
    private const float mc_fReleaseMoveTime = 0.1f;
    private const float mc_fDeltaMoveScaleRatio = 5.0f;
    private const float mc_fMinDeltaSquareMagnitude = 0.1f * 0.1f;
    private const float mc_fMaxPressTime = 0.5f;
    private bool m_pressed = false;

#if !UNITY_EDITOR_WIN
    private Vector2 m_oldTouchPosition1 = new Vector2(0, 0);
    private Vector2 m_oldTouchPosition2 = new Vector2(0, 0);    
#endif

    [SerializeField]
    private float m_roomInDistance = 10.0f;

    enum SECOND_TOUCH_MODE
    {
        ROTATE,
        ROOM_IN
    };

    [SerializeField]
    private SECOND_TOUCH_MODE m_secondTouchMode = SECOND_TOUCH_MODE.ROTATE;

    [SerializeField]
    private float m_roomInMinHeight = 20.0f;

    [SerializeField]
    private float m_roomInMaxHeight = 500.0f;    

    [SerializeField]
    private float m_destRotationScaleValue = 15.0f;
    [SerializeField]
    private float m_rotateSpeed = 0.8f;
    //keyboard move speed
    [SerializeField]
    private float m_moveSpeed = 10.0f;
    //drag move speed
    [SerializeField]
    private float m_speed = 0.1f;
    [SerializeField]
    private float m_accumulateMoveRatio = 1.0f;
    [SerializeField]
    private bool m_useBornInfo = false;
    [SerializeField]
    private Vector3 m_bornPosition = Vector3.zero;
    [SerializeField]
    private Vector3 m_rotation = Vector3.zero;
    //=========================for touche movement=========================    
    private Transform m_selfTransform = null;
    //The touch press position
    private Vector2 m_touchPressPosition = Vector2.zero;
    //The delegate declartion
    private delegate void TouchEventHandler();
    private float m_pressTime = 0.0f;
    private Vector2 m_deltaMovement = Vector2.zero;
    //=========================for mouse rotate=========================    
    private Vector3 m_oldMousePosition;
    private bool m_mouseRightButtonPress;
    private float m_accYawAngle = 0.0f;
    private float m_accPitchAngle = 0.0f;
    //=========================for key movement=========================
    private enum MoveState
    {
        MoveState_Forward,
        MoveState_Back,
        MoveState_Left,
        MoveState_Right,
        MoveState_Max
    }
    private bool[] m_theMoveStates = new bool[(int)MoveState.MoveState_Max];

    private bool m_isRoomIn = false;
    private float m_deltaX = 0.0f;
    //private bool m_mouseLeftButtonPress = false;
    [SerializeField]
    private bool m_ShowPosition = false;
}