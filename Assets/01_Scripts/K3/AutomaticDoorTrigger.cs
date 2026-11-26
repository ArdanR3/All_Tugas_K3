using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Sistem Pintu Otomatis Modul M02:
/// - Otomatis terbuka saat karakter / VR Rig mendekat atau masuk ke area trigger.
/// - Otomatis tertutup saat karakter meninggalkan area trigger.
/// - Mendukung penekanan tombol 'E' / Space serta fallback planar distance.
/// </summary>
public class AutomaticDoorTrigger : MonoBehaviour
{
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private float proximityDistance = 3.0f;
    [SerializeField] private bool autoOpenOnProximity = true;

    private static readonly int DoorOpen = Animator.StringToHash("DoorOpen");
    private bool isPlayerNearby = false;
    private bool isDoorCurrentlyOpen = false;
    private Transform cachedPlayerTransform;
    private float nextPlayerSearchTime = 0f;

    private void Awake()
    {
        ResolveAnimator();
    }

    private void ResolveAnimator()
    {
        if (doorAnimator == null)
        {
            // 1. Cari pada parent langsung
            doorAnimator = GetComponentInParent<Animator>();
            
            // 2. Jika tidak ada, cari pada saudara/children di bawah parent (DOOR_SYSTEM)
            if (doorAnimator == null && transform.parent != null)
            {
                doorAnimator = transform.parent.GetComponentInChildren<Animator>();
            }

            // 3. Fallback pada objek ini sendiri
            if (doorAnimator == null)
            {
                doorAnimator = GetComponent<Animator>();
            }
        }
    }

    private void Start()
    {
        ResolveAnimator();
        FindPlayerTransform();
    }

    private Transform FindPlayerTransform()
    {
        if (cachedPlayerTransform != null) return cachedPlayerTransform;

        // 1. Tag "Player"
        GameObject p = GameObject.FindWithTag("Player");
        if (p != null)
        {
            cachedPlayerTransform = p.transform;
            return cachedPlayerTransform;
        }

        // 2. Camera.main
        if (Camera.main != null)
        {
            cachedPlayerTransform = Camera.main.transform;
            return cachedPlayerTransform;
        }

        // 3. XR Origin / VR Rig
        GameObject xr = GameObject.Find("XR Origin Hands (XR Rig)") 
                     ?? GameObject.Find("XR Origin") 
                     ?? GameObject.Find("07_PLAYER")
                     ?? GameObject.Find("XR Interaction Simulator");
        if (xr != null)
        {
            cachedPlayerTransform = xr.transform;
            return cachedPlayerTransform;
        }

        return null;
    }

    private void Update()
    {
        if (doorAnimator == null)
        {
            ResolveAnimator();
        }

        // Periodic player search if not found yet
        if (cachedPlayerTransform == null && Time.time >= nextPlayerSearchTime)
        {
            nextPlayerSearchTime = Time.time + 1.0f;
            FindPlayerTransform();
        }

        // Fallback proximity check (sangat penting untuk VR jika collider kamera tidak memicu trigger)
        if (autoOpenOnProximity && cachedPlayerTransform != null)
        {
            Vector3 diff = cachedPlayerTransform.position - transform.position;
            diff.y = 0; // horizontal planar distance
            float dist = diff.magnitude;

            if (dist <= proximityDistance)
            {
                if (!isDoorCurrentlyOpen)
                {
                    isPlayerNearby = true;
                    OpenDoor();
                }
            }
            else if (isPlayerNearby && dist > proximityDistance + 0.8f)
            {
                isPlayerNearby = false;
                CloseDoor();
            }
        }

        // Fitur opsional: Pemain dapat menekan tombol 'E' saat berada di dekat pintu untuk toggle buka/tutup
        if (isPlayerNearby && ReadInteractKey())
        {
            ToggleDoor();
        }
    }

    public static bool IsPlayerCollider(Collider other)
    {
        if (other == null) return false;
        if (other.CompareTag("Player")) return true;
        if (other.transform.root != null && other.transform.root.CompareTag("Player")) return true;

        string nameLower = other.name.ToLowerInvariant();
        string rootLower = other.transform.root != null ? other.transform.root.name.ToLowerInvariant() : "";

        if (nameLower.Contains("player") || rootLower.Contains("player")) return true;
        if (nameLower.Contains("xr") || rootLower.Contains("xr")) return true;
        if (nameLower.Contains("camera") || nameLower.Contains("head")) return true;
        if (nameLower.Contains("hand") || nameLower.Contains("controller")) return true;
        if (nameLower.Contains("simulator")) return true;

        if (Camera.main != null && (other.transform == Camera.main.transform || other.transform.IsChildOf(Camera.main.transform)))
            return true;

        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayerCollider(other)) return;

        isPlayerNearby = true;
        OpenDoor();
    }

    private void OnTriggerStay(Collider other)
    {
        if (!IsPlayerCollider(other)) return;

        isPlayerNearby = true;
        if (!isDoorCurrentlyOpen)
        {
            OpenDoor();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayerCollider(other)) return;

        isPlayerNearby = false;
        CloseDoor();
    }

    public void OpenDoor()
    {
        if (doorAnimator != null)
        {
            // Kondisi transisi ke DoorAnimation (buka): DoorOpen == true
            doorAnimator.SetBool(DoorOpen, true);
            isDoorCurrentlyOpen = true;
            Debug.Log("[DOOR_SYSTEM] Karakter mendekati sensor -> Pintu TERBUKA otomatis.");
        }
    }

    public void CloseDoor()
    {
        if (doorAnimator != null)
        {
            // Kondisi transisi ke DoorAnimationClosed (tutup): DoorOpen == false
            doorAnimator.SetBool(DoorOpen, false);
            isDoorCurrentlyOpen = false;
            Debug.Log("[DOOR_SYSTEM] Karakter meninggalkan sensor -> Pintu TERTUTUP otomatis.");
        }
    }

    public void ToggleDoor()
    {
        if (isDoorCurrentlyOpen)
        {
            CloseDoor();
        }
        else
        {
            OpenDoor();
        }
    }

    private bool ReadInteractKey()
    {
        try
        {
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space)) return true;
        }
        catch { }

        #if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)
                return true;
        }
        #endif

        return false;
    }
}
