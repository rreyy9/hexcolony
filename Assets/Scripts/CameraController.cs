using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 10f;

    [Header("Mouse Drag Settings")]
    [SerializeField] private float dragSpeed = 0.5f;

    private PlayerInputActions inputActions;
    private Vector2 moveInput;
    private bool isDragging = false;

    void Awake()
    {
        inputActions = new PlayerInputActions();
    }

    void OnEnable()
    {
        // Enable the Camera action map
        inputActions.Camera.Enable();

        // Subscribe to input events
        inputActions.Camera.Move.performed += OnMove;
        inputActions.Camera.Move.canceled += OnMove;

        inputActions.Camera.Drag.performed += OnDragStart;
        inputActions.Camera.Drag.canceled += OnDragEnd;
    }

    void OnDisable()
    {
        // Unsubscribe from events
        inputActions.Camera.Move.performed -= OnMove;
        inputActions.Camera.Move.canceled -= OnMove;

        inputActions.Camera.Drag.performed -= OnDragStart;
        inputActions.Camera.Drag.canceled -= OnDragEnd;

        // Disable the Camera action map
        inputActions.Camera.Disable();
    }

    void Update()
    {
        HandleKeyboardMovement();
        HandleMouseDrag();
    }

    void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    void OnDragStart(InputAction.CallbackContext context)
    {
        isDragging = true;
    }

    void OnDragEnd(InputAction.CallbackContext context)
    {
        isDragging = false;
    }

    void HandleKeyboardMovement()
    {
        if (moveInput != Vector2.zero)
        {
            Vector3 moveDirection = new Vector3(moveInput.x, 0, moveInput.y);
            transform.position += moveDirection * moveSpeed * Time.deltaTime;
        }
    }

    void HandleMouseDrag()
    {
        if (isDragging)
        {
            Vector2 delta = inputActions.Camera.DragDelta.ReadValue<Vector2>();
            Vector3 move = new Vector3(-delta.x * dragSpeed * Time.deltaTime, 0, -delta.y * dragSpeed * Time.deltaTime);
            transform.position += move;
        }
    }
}