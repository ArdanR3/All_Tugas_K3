using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Mengontrol pergerakan kamera agar mengikuti karakter (3rd Person View) secara halus (smooth).
/// Dapat ditempelkan langsung pada Main Camera atau digunakan mandiri.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target Karakter")]
    public Transform target;

    [Header("Pengaturan Posisi & Sudut Kamera")]
    public Vector3 offset = new Vector3(0f, 1.8f, -2.8f);
    public float smoothSpeed = 8f;
    public float rotationSpeed = 10f;

    [Header("Fitur Tambahan")]
    public bool enableZoom = true;
    public float minZoomDistance = 1.5f;
    public float maxZoomDistance = 5.0f;
    public bool preventWallClipping = true;

    private bool isInitialized = false;

    void Start()
    {
        FindTarget();
        SnapToTarget();
    }

    void LateUpdate()
    {
        if (target == null)
        {
            FindTarget();
            if (target == null) return;
        }

        if (!isInitialized)
        {
            SnapToTarget();
        }

        HandleZoom();

        // Posisi target kamera di belakang karakter sesuai arah hadap karakter
        Vector3 desiredPosition = target.position + (target.rotation * offset);
        Vector3 lookTarget = target.position + Vector3.up * 1.1f;

        // Anti tembus tembok (Wall Clipping Prevention)
        if (preventWallClipping)
        {
            Vector3 camRayDir = desiredPosition - lookTarget;
            float camRayDist = camRayDir.magnitude;
            if (camRayDist > 0.001f)
            {
                if (Physics.Raycast(lookTarget, camRayDir.normalized, out RaycastHit hit, camRayDist))
                {
                    if (hit.transform != target && !hit.collider.isTrigger)
                    {
                        desiredPosition = hit.point - (camRayDir.normalized * 0.25f);
                    }
                }
            }
        }

        // Interpolasi posisi secara halus
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Pandangan kamera selalu mengarah ke karakter
        Vector3 direction = lookTarget - transform.position;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    public void SnapToTarget()
    {
        if (target == null) return;

        // Jika kamera merupakan child dari target, lepaskan agar bebas diinterpolasi
        if (transform.parent == target)
        {
            transform.SetParent(null, true);
        }

        Vector3 desiredPosition = target.position + (target.rotation * offset);
        transform.position = desiredPosition;
        transform.LookAt(target.position + Vector3.up * 1.1f);
        isInitialized = true;
    }

    void HandleZoom()
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
            float currentDist = offset.magnitude;
            float newDist = Mathf.Clamp(currentDist - scroll * 2f, minZoomDistance, maxZoomDistance);
            offset = offset.normalized * newDist;
        }
    }

    private void FindTarget()
    {
        GameObject player = GameObject.Find("OBJ_Controller");
        if (player == null) player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            target = player.transform;
        }
    }
}
