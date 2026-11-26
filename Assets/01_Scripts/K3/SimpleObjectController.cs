using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class SimpleObjectController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float turnSpeed = 120f;

    [Header("Camera Follow Settings")]
    [Tooltip("Kamera otomatis mengikuti pergerakan karakter (Third Person View)")]
    public bool followCamera = true;
    public Camera playerCamera;
    public Vector3 cameraOffset = new Vector3(0f, 1.8f, -2.8f);
    public float cameraFollowSpeed = 8f;
    public float cameraRotationSpeed = 10f;
    public bool enableZoom = true;
    public float minZoomDistance = 1.5f;
    public float maxZoomDistance = 5.0f;
    public bool preventWallClipping = true;

    private float horizontal;
    private float vertical;
    private Rigidbody rb;
    private bool cameraInitialized = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.freezeRotation = true;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.interpolation = RigidbodyInterpolation.Interpolate; // Menghilangkan physics micro-jitter
        }

        InitCamera();
    }

    void Update()
    {
        ReadInput();

        if (turnSpeed <= 0f)
        {
            Vector3 movement = new Vector3(horizontal, 0f, vertical).normalized;
            transform.position += movement * moveSpeed * Time.deltaTime;
        }
        else
        {
            Move();
            Turn();
        }
    }

    void LateUpdate()
    {
        UpdateCameraFollow();
    }

    void ReadInput()
    {
        horizontal = 0f;
        vertical = 0f;

        // 1. Try reading standard Legacy Input
        try
        {
            horizontal = Input.GetAxisRaw("Horizontal");
            vertical = Input.GetAxisRaw("Vertical");
        }
        catch
        {
            // Legacy input might be disabled in new project settings
        }

        // 2. If no legacy input detected, fallback to New Input System Keyboard
        #if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            var kb = Keyboard.current;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) vertical = 1f;
            else if (kb.sKey.isPressed || kb.downArrowKey.isPressed) vertical = -1f;

            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) horizontal = 1f;
            else if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) horizontal = -1f;
        }
        #endif
    }

    void Move()
    {
        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 moveDelta = forward * (vertical * moveSpeed * Time.deltaTime);

        if (rb != null && !rb.isKinematic)
        {
            rb.MovePosition(rb.position + moveDelta);
        }
        else
        {
            transform.position += moveDelta;
        }
    }

    void Turn()
    {
        float rotDelta = horizontal * turnSpeed * Time.deltaTime;

        if (rb != null && !rb.isKinematic)
        {
            Quaternion deltaRot = Quaternion.Euler(0f, rotDelta, 0f);
            rb.MoveRotation(rb.rotation * deltaRot);
        }
        else
        {
            transform.Rotate(0f, rotDelta, 0f);
        }
    }

    void InitCamera()
    {
        if (!followCamera) return;

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                playerCamera = FindFirstObjectByType<Camera>();
            }
        }

        if (playerCamera != null)
        {
            // Jika kamera merupakan child langsung dari karakter, unparent agar pergerakannya mulus (smooth lerp)
            if (playerCamera.transform.parent == transform)
            {
                playerCamera.transform.SetParent(null, true);
            }

            // Snap langsung ke posisi belakang karakter
            Vector3 targetPos = transform.position + (transform.rotation * cameraOffset);
            playerCamera.transform.position = targetPos;
            playerCamera.transform.LookAt(transform.position + Vector3.up * 1.1f);
            cameraInitialized = true;
        }
    }

    void UpdateCameraFollow()
    {
        if (!followCamera) return;

        if (playerCamera == null)
        {
            InitCamera();
            if (playerCamera == null) return;
        }

        // Jika kamera memiliki script CameraFollow yang sedang aktif, biarkan CameraFollow yang menangani
        var camFollowScript = playerCamera.GetComponent<CameraFollow>();
        if (camFollowScript != null && camFollowScript.enabled)
        {
            return;
        }

        if (!cameraInitialized)
        {
            InitCamera();
        }

        HandleCameraZoom();

        // Posisi target kamera di belakang karakter sesuai arah hadap
        Vector3 targetPos = transform.position + (transform.rotation * cameraOffset);
        Vector3 lookTarget = transform.position + Vector3.up * 1.1f;

        // Anti tembus tembok (Wall Clipping Prevention)
        if (preventWallClipping)
        {
            Vector3 camRayDir = targetPos - lookTarget;
            float camRayDist = camRayDir.magnitude;
            if (camRayDist > 0.001f)
            {
                if (Physics.Raycast(lookTarget, camRayDir.normalized, out RaycastHit hit, camRayDist))
                {
                    if (hit.transform != transform && !hit.collider.isTrigger)
                    {
                        targetPos = hit.point - (camRayDir.normalized * 0.25f);
                    }
                }
            }
        }

        // Lerp posisi kamera secara halus
        playerCamera.transform.position = Vector3.Lerp(
            playerCamera.transform.position,
            targetPos,
            cameraFollowSpeed * Time.deltaTime
        );

        // Slerp rotasi menghadap karakter secara halus
        Vector3 direction = lookTarget - playerCamera.transform.position;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            playerCamera.transform.rotation = Quaternion.Slerp(
                playerCamera.transform.rotation,
                targetRotation,
                cameraRotationSpeed * Time.deltaTime
            );
        }
    }

    void HandleCameraZoom()
    {
        if (!enableZoom) return;

        float scroll = 0f;
        try
        {
            scroll = Input.GetAxis("Mouse ScrollWheel");
        }
        catch { }

        #if ENABLE_INPUT_SYSTEM
        if (scroll == 0f && Mouse.current != null)
        {
            scroll = Mouse.current.scroll.ReadValue().y * 0.001f;
        }
        #endif

        if (Mathf.Abs(scroll) > 0.001f)
        {
            float currentDist = cameraOffset.magnitude;
            float newDist = Mathf.Clamp(currentDist - scroll * 2f, minZoomDistance, maxZoomDistance);
            cameraOffset = cameraOffset.normalized * newDist;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"Collision dengan: {collision.gameObject.name}");
    }

    private void OnGUI()
    {
        // HUD Petunjuk Kontrol Praktikum M02
        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 12;
        style.alignment = TextAnchor.MiddleLeft;

        GUILayout.BeginArea(new Rect(15, 15, 340, 95), style);
        GUILayout.Label("<b>KONTROL PRAKTIKUM M02 (OBJ_Controller)</b>");
        GUILayout.Label("• <b>W / S</b> : Maju / Mundur");
        GUILayout.Label("• <b>A / D</b> : Belok Kiri / Kanan (Kamera Ikut)");
        GUILayout.Label("• <b>Scroll Mouse</b> : Zoom Kamera Dekat / Jauh");
        GUILayout.EndArea();
    }
}
