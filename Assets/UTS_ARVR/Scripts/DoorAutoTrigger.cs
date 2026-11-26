using UnityEngine;
using Unity.XR.CoreUtils;

/// <summary>
/// SOAL 4 UTS Pemrograman Berbasis AR/VR
/// Mengontrol Animator pintu penghubung antar area workshop secara otomatis:
/// - Saat Player mendekati / masuk area trigger pintu -> parameter boolean isOpen = true (Pintu Terbuka)
/// - Saat Player menjauhi / keluar area trigger pintu -> parameter boolean isOpen = false (Pintu Tertutup)
/// </summary>
public class DoorAutoTrigger : MonoBehaviour
{
    [Header("Referensi Animator Pintu")]
    public Animator doorAnimator;

    [Tooltip("Nama parameter boolean pada Animator Controller")]
    public string boolParameterName = "isOpen";

    [Header("Pengaturan Deteksi Player VR")]
    [Tooltip("Radius deteksi jarak horizontal kamera VR sebagai dukungan tambahan trigger collider")]
    public float proximityRadius = 2.6f;

    private bool isDoorOpen = false;
    private bool triggeredByCollider = false;

    private void Start()
    {
        if (doorAnimator == null)
        {
            doorAnimator = GetComponentInParent<Animator>();
            if (doorAnimator == null && transform.parent != null)
            {
                doorAnimator = transform.parent.GetComponentInChildren<Animator>();
            }
        }

        if (doorAnimator != null)
        {
            doorAnimator.SetBool(boolParameterName, false);
        }
    }

    private void Update()
    {
        // Dukungan deteksi jarak terhadap Main Camera (VR Headset) agar pintu tetap responsif
        // saat diuji menggunakan XR Interaction Simulator maupun Teleportation
        if (!triggeredByCollider && Camera.main != null)
        {
            Vector3 camPos = Camera.main.transform.position;
            Vector3 doorPos = transform.position;
            float distXZ = Vector2.Distance(new Vector2(camPos.x, camPos.z), new Vector2(doorPos.x, doorPos.z));

            if (!isDoorOpen && distXZ <= proximityRadius)
            {
                SetDoorOpen(true);
            }
            else if (isDoorOpen && distXZ > proximityRadius + 0.4f)
            {
                SetDoorOpen(false);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayerCollider(other))
        {
            triggeredByCollider = true;
            SetDoorOpen(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayerCollider(other))
        {
            triggeredByCollider = false;
            SetDoorOpen(false);
        }
    }

    private void SetDoorOpen(bool open)
    {
        if (isDoorOpen == open) return;
        isDoorOpen = open;

        if (doorAnimator != null)
        {
            doorAnimator.SetBool(boolParameterName, open);
            string doorName = transform.parent != null ? transform.parent.name : gameObject.name;
            Debug.Log($"[DoorAutoTrigger - {doorName}] Player {(open ? "mendekati" : "menjauhi")} pintu -> {boolParameterName} = {open}");
        }
    }

    private bool IsPlayerCollider(Collider other)
    {
        if (other == null) return false;
        if (other.CompareTag("Player")) return true;
        if (other.transform.root != null && other.transform.root.CompareTag("Player")) return true;
        if (other.GetComponent<CharacterController>() != null) return true;
        if (other.GetComponentInParent<XROrigin>() != null) return true;

        string n = other.gameObject.name.ToLowerInvariant();
        if (n.Contains("player") || n.Contains("xr origin") || n.Contains("camera"))
            return true;

        return false;
    }
}

