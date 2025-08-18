using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed;
    private Vector2 curMovementInput;
    public float jumpPower;
    public LayerMask groundLayerMask;
    [HideInInspector] public bool isOnTrampoline = false;
    public float sprintSpeedMuliti = 1.5f;
    public float sprintStamina;
    [HideInInspector] public bool isSprinting = false;

    [Header("Look")]
    public Transform cameraContainer;
    public float minXLook;
    public float maxXLook;
    private float camCurXRot;
    public float lookSensitivity;

    [Header("Camera Views")]
    // public Transform thirdPersonCamPos;
    // private Vector3 firstPersonLoc;
    // private Quaternion firstPersonAng;
    // public bool isThirdPerson = false;
    // public GameObject model;
    // public GameObject equipCamera;
    public Action inventory;
    [Header("Climbing Walls")]
    public LayerMask wallLayerMask;
    public float wallCheckDistance = 0.5f;
    public float climbSpeed = 3f;
    public float climbOverHeight = 1.5f;
    public float climbOverForward = 0.5f;
    private bool isClimbing = false;
    private bool isTouchingWall = false;
    private Vector3 wallNormal;


    private Vector2 mouseDelta;

    [HideInInspector]
    public bool canLook = true;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked; //Wether to lock the mouse or not (Make it invisible)
    }

    private void FixedUpdate()
    {
        DrawGroundRays();
        if (isSprinting)
        {
            bool hasStamina = CharacterManager.Instance.Player.condition.UseStamina(sprintStamina * Time.deltaTime);
            if (hasStamina)
            {
                Move();
            }
            else
            {
                isSprinting = false;
                Move();
            }
        }
        else
        {
            Move();
        }

        CheckWall();

        if (isClimbing)
        {
            ClimbingMovement();
        }
        else
        {
            Move();
        }

    }

    private void LateUpdate()
    {
        if (canLook)
        {
            CameraLook();
        }
    }

    public void OnLookInput(InputAction.CallbackContext context)
    {
        mouseDelta = context.ReadValue<Vector2>();
    }

    public void OnMoveInput(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            curMovementInput = context.ReadValue<Vector2>();
        }
        else if (context.phase == InputActionPhase.Canceled)
        {
            curMovementInput = Vector2.zero;
        }
    }

    public void onSprintInput(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            isSprinting = true;
        }
        else if (context.phase == InputActionPhase.Canceled)
        {
            isSprinting = false;
        }
    }


    public void OnJumpInput(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            if (isClimbing)
            {
                StopClimbing();
            }
            else if (IsGrounded())
            {
                Debug.Log("Jump");
                float finalJumpPower = jumpPower;
                if (isOnTrampoline)
                {
                    finalJumpPower *= 2f;
                }
                rb.AddForce(Vector2.up * finalJumpPower, ForceMode.Impulse);
            }
            else if (isTouchingWall)
            {
                StartClimbing();
            }
        }
    }
    public void OnAttackInput(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            if (CharacterManager.Instance.Player.equip.curEquip != null)
            {
                CharacterManager.Instance.Player.equip.curEquip.OnAttackInput();
            }
        }
    }

    private void Move()
    {
        float speed = moveSpeed;
        if (isSprinting)
        {
            speed *= sprintSpeedMuliti;
        }
        Vector3 dir = transform.forward * curMovementInput.y + transform.right * curMovementInput.x;
        dir *= speed;
        dir.y = rb.velocity.y;
        rb.velocity = dir;
    }

    void CameraLook()
    {
        camCurXRot += mouseDelta.y * lookSensitivity; // Y 값에 민감도를 곱하며, 
        camCurXRot = Mathf.Clamp(camCurXRot, minXLook, maxXLook); // Clamp을 통해서, 회전을 제한한다
        cameraContainer.localEulerAngles = new Vector3(-camCurXRot, 0, 0); // 적용한 것을 camcotainer의 x값에 한다 (위 아래)

        transform.eulerAngles += new Vector3(0, mouseDelta.x * lookSensitivity, 0); // 현재 x 각도를 유지를하면서, Y (양 옆)을 추가하면서, 상하좌우의 Look을 구현한다.
    }

    bool IsGrounded()
    {
        Ray[] rays = new Ray[4]
        {
            // 플레이어의 앞, 뒤, 옆을 안 보이는 Ray를 통해, 땅 위에 있는지를 판단.
            new Ray(transform.position + (transform.forward * 0.2f) + (transform.up * 0.01f), Vector3.down),
            new Ray(transform.position + (-transform.forward * 0.2f) + (transform.up * 0.01f), Vector3.down),
            new Ray(transform.position + (transform.right * 0.2f) + (transform.up * 0.01f), Vector3.down),
            new Ray(transform.position + (-transform.right * 0.2f) + (transform.up * 0.01f), Vector3.down)
        };

        for (int i = 0; i < rays.Length; i++)
        {
            if (Physics.Raycast(rays[i], 0.3f, groundLayerMask))
            {
                Debug.Log("Isgrounded");
                return true;
            }
        }

        return false;
    }

    public void ToggleCursor(bool toggle)
    {
        Cursor.lockState = toggle ? CursorLockMode.None : CursorLockMode.Locked;
        canLook = !toggle;
    }

    public void OnInventoryButton(InputAction.CallbackContext CallbackContext)
    {
        if (CallbackContext.phase == InputActionPhase.Started)
        {
            inventory?.Invoke();
            ToggleCursor();
        }
    }

    private void ToggleCursor()
    {
        bool toggle = Cursor.lockState == CursorLockMode.Locked;
        Cursor.lockState = toggle ? CursorLockMode.None : CursorLockMode.Locked;
        canLook = !toggle;
    }

    private void CheckWall()
    {
        //Ray origin point will be on the bottom of the player.
        Vector3 origin = transform.position + Vector3.up * 0.01f;
        Vector3 direction = transform.forward;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, wallCheckDistance, wallLayerMask))
        {
            isTouchingWall = true;
            wallNormal = hit.normal;
        }
        else
        {
            isTouchingWall = false;
            if (isClimbing)
            {
                //If the player is stil climbing and the raycast does not see a wall anymore it will
                //Move the player up and to forward instantenously. 
                Vector3 climbOverOffset = Vector3.up * 0.5f + transform.forward * 0.2f;
                rb.position += climbOverOffset;
                StopClimbing();
            }
        }
    }

    private void StartClimbing()
    {
        isClimbing = true;
        rb.useGravity = false;
        rb.velocity = Vector3.zero;
    }

    private void StopClimbing()
    {
        isClimbing = false;
        rb.useGravity = true;
    }

    private void ClimbingMovement()
    {
        // Vertical climbing movement based on player input (Y axis)
        Vector3 vertical = Vector3.up * curMovementInput.y * climbSpeed;

        Vector3 horizontal = Vector3.zero;
        if (Mathf.Abs(curMovementInput.x) > 0.01f)
        {
            //wallNormal = vector pointing away from the wall's surface
            //Vector3.up world up ward direction
            //Both of these combined will give a vector perpendicular to both sideways directions. 
            //Thus allowing the movement of left and right while sticking on the wall 
            Vector3 wallRight = Vector3.Cross(wallNormal, Vector3.up).normalized;
            horizontal = wallRight * curMovementInput.x * climbSpeed;
        }

        Vector3 wallStick = -wallNormal * 0.3f;

        //The movement for going up and down as well as left and right.
        rb.velocity = vertical + horizontal;

        rb.position += wallStick * Time.fixedDeltaTime;
    }

    // Visual Learning drawing the 4 rays inorder to see where they actually are. 
    void DrawGroundRays()
    {
        Vector3 origin = transform.position + (transform.up * 0.01f);
        float offset = 0.2f;
        float rayLength = 0.3f;

        // Define four ray origins
        Vector3[] rayOrigins = new Vector3[]
        {
            origin + (transform.forward * offset),   // Front
            origin + (-transform.forward * offset),  // Back
            origin + (transform.right * offset),     // Right
            origin + (-transform.right * offset)     // Left
        };

        foreach (Vector3 start in rayOrigins)
        {
            Debug.DrawRay(start, Vector3.down * rayLength, Color.red);
        }
    }
}