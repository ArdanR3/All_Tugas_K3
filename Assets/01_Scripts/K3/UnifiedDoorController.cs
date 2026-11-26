using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.XR.CoreUtils;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Pengontrol Pintu Terpadu (Unified Door Controller):
/// Menyatukan Model Pintu (Free Wood Door Pack / Custom), Titik Pivot Engsel, Animasi, Audio SFX,
/// Deteksi Proximity Otomatis (VR Camera / Player), dan Interaksi Langsung VR (XRSimpleInteractable).
/// </summary>
public class UnifiedDoorController : MonoBehaviour
{
    [Header("Komponen Terpadu")]
    [Tooltip("Transform objek daun pintu atau PIVOT Engsel yang berputar")]
    public Transform pivotEngsel;

    [Tooltip("Collider area sensor pemicu pintu")]
    public BoxCollider triggerArea;

    [Tooltip("Animator untuk pintu jika menggunakan state machine")]
    public Animator doorAnimator;

    [Header("Pengaturan Ayunan Pintu")]
    [Tooltip("Status pintu saat ini")]
    public bool isOpen = false;

    [Tooltip("Kecepatan ayunan pintu saat membuka/menutup")]
    public float openSpeed = 2.5f;

    [Tooltip("Sudut rotasi saat terbuka (-90 membuka ke dalam/luar)")]
    public float openAngle = -90.0f;

    [Tooltip("Sudut rotasi saat tertutup")]
    public float closeAngle = 0.0f;

    [Header("Fitur Otomatis & Tombol")]
    [Tooltip("Pintu otomatis membuka saat karakter/kamera pemain mendekat")]
    public bool autoOpenOnPlayer = true;

    [Tooltip("Pemain dapat menekan tombol E / interaksi manual")]
    public bool allowKeyInteract = true;

    [Tooltip("Jarak deteksi pendekatan otomatis (dalam meter)")]
    public float proximityDistance = 2.8f;

    [Header("Audio SFX")]
    public AudioSource audioSource;
    public AudioClip openSound;
    public AudioClip closeSound;

    private bool isPlayerInTrigger = false;
    private bool isPlayerInRange = false;
    private Transform playerTransform;
    private DoorScript.Door nativeDoorScript;
    private XRSimpleInteractable xrInteractable;

    void Awake()
    {
        AutoWireComponents();
    }

    void Start()
    {
        AutoWireComponents();
        FindPlayerReference();
        SetupVRInteractable();
    }

    void Update()
    {
        // 1. Cek jarak pemain (fail-safe untuk deteksi trigger & tombol)
        CheckPlayerProximity();

        // 2. Ayunan rotasi halus pada daun pintu / engsel
        if (pivotEngsel != null)
        {
            float targetY = isOpen ? openAngle : closeAngle;
            Quaternion targetRot = Quaternion.Euler(0f, targetY, 0f);
            pivotEngsel.localRotation = Quaternion.Slerp(
                pivotEngsel.localRotation,
                targetRot,
                Time.deltaTime * 5f * openSpeed
            );
        }

        // 3. Sinkronisasi Animator jika ada
        if (doorAnimator != null)
        {
            doorAnimator.SetBool("DoorOpen", isOpen);
            doorAnimator.SetBool("IsOpen", isOpen);
        }

        // 4. Interaksi tombol manual (E / Space) saat pemain berada di dekat area sensor pintu
        if (allowKeyInteract && (isPlayerInTrigger || isPlayerInRange) && ReadInteractKey())
        {
            ToggleDoor();
        }
    }

    public void AutoWireComponents()
    {
        // 1. Kusen pintu 'Frame' harus tetap diam di dinding
        Transform doorChild = transform.Find("Door");
        if (doorChild != null)
        {
            Transform frameChild = doorChild.Find("Frame");
            if (frameChild != null)
            {
                frameChild.SetParent(transform, true);
                Debug.Log($"[{gameObject.name}] Kusen 'Frame' dipisahkan ke root agar tetap kokoh saat pintu mengayun.");
            }
        }

        // 2. Tentukan objek daun pintu yang berputar
        if (pivotEngsel == null || (pivotEngsel.name.Contains("PIVOT") && pivotEngsel.childCount == 0))
        {
            if (doorChild != null)
            {
                pivotEngsel = doorChild;
            }
            else
            {
                Transform pivotChild = transform.Find("PIVOT_Engsel");
                if (pivotChild != null && pivotChild.childCount > 0)
                {
                    pivotEngsel = pivotChild;
                }
                else
                {
                    Transform panel = transform.Find("Door_Panel");
                    if (panel != null) pivotEngsel = panel;
                }
            }
        }

        // 3. Nonaktifkan skrip DoorScript.Door lama agar tidak menimpa rotasi pintu
        nativeDoorScript = GetComponentInChildren<DoorScript.Door>();
        if (nativeDoorScript != null)
        {
            nativeDoorScript.enabled = false;
            if (openSound == null && nativeDoorScript.openDoor != null) openSound = nativeDoorScript.openDoor;
            if (closeSound == null && nativeDoorScript.closeDoor != null) closeSound = nativeDoorScript.closeDoor;
        }

        // 4. Cari Animator
        if (doorAnimator == null)
        {
            doorAnimator = GetComponentInChildren<Animator>();
            if (doorAnimator == null && pivotEngsel != null)
            {
                doorAnimator = pivotEngsel.GetComponent<Animator>();
            }
        }

        // 5. Temukan TRG_PintuArea
        if (triggerArea == null)
        {
            Transform trg = transform.Find("TRG_PintuArea");
            if (trg != null) triggerArea = trg.GetComponent<BoxCollider>();
        }

        // 6. Siapkan AudioSource
        if (audioSource == null)
        {
            audioSource = GetComponentInChildren<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.spatialBlend = 1.0f; // 3D sound
        audioSource.maxDistance = 15f;
        audioSource.playOnAwake = false;

        // Muat SFX default dari Resources jika kosong
        if (openSound == null) openSound = Resources.Load<AudioClip>("Audio/SFX/Door_Open");
        if (closeSound == null) closeSound = Resources.Load<AudioClip>("Audio/SFX/Door_Close");
    }

    private void SetupVRInteractable()
    {
        // Pasang XRSimpleInteractable agar pemain VR dapat mengklik/menyentuh pintu dengan controller ray
        GameObject interactTarget = pivotEngsel != null ? pivotEngsel.gameObject : gameObject;
        xrInteractable = interactTarget.GetComponent<XRSimpleInteractable>();
        if (xrInteractable == null)
        {
            xrInteractable = interactTarget.AddComponent<XRSimpleInteractable>();
        }

        xrInteractable.selectEntered.RemoveListener(OnVRSelect);
        xrInteractable.selectEntered.AddListener(OnVRSelect);
    }

    private void OnVRSelect(BaseInteractionEventArgs args)
    {
        ToggleDoor();
    }

    private void FindPlayerReference()
    {
        if (playerTransform != null) return;

        if (Camera.main != null)
        {
            playerTransform = Camera.main.transform;
            return;
        }

#if UNITY_2023_1_OR_NEWER
        var origin = Object.FindFirstObjectByType<XROrigin>();
#else
        var origin = Object.FindObjectOfType<XROrigin>();
#endif
        if (origin != null)
        {
            playerTransform = origin.Camera != null ? origin.Camera.transform : origin.transform;
            return;
        }

        GameObject playerGo = GameObject.FindWithTag("Player");
        if (playerGo == null) playerGo = GameObject.Find("07_PLAYER");
        if (playerGo != null) playerTransform = playerGo.transform;
    }

    private void CheckPlayerProximity()
    {
        if (playerTransform == null)
        {
            FindPlayerReference();
            if (playerTransform == null) return;
        }

        Vector3 doorPos = transform.position;
        Vector3 pPos = playerTransform.position;
        float horizontalDist = Vector2.Distance(new Vector2(doorPos.x, doorPos.z), new Vector2(pPos.x, pPos.z));

        isPlayerInRange = (horizontalDist <= proximityDistance);

        if (autoOpenOnPlayer)
        {
            if (isPlayerInRange && !isOpen)
            {
                OpenDoor();
            }
            else if (!isPlayerInRange && !isPlayerInTrigger && isOpen && horizontalDist > (proximityDistance + 0.6f))
            {
                CloseDoor();
            }
        }
    }

    public void OnPlayerEnterTrigger()
    {
        isPlayerInTrigger = true;
        if (autoOpenOnPlayer && !isOpen)
        {
            OpenDoor();
        }
    }

    public void OnPlayerExitTrigger()
    {
        isPlayerInTrigger = false;
        if (autoOpenOnPlayer && !isPlayerInRange && isOpen)
        {
            CloseDoor();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayerCollider(other))
        {
            OnPlayerEnterTrigger();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (IsPlayerCollider(other))
        {
            isPlayerInTrigger = true;
            if (autoOpenOnPlayer && !isOpen)
            {
                OpenDoor();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayerCollider(other))
        {
            OnPlayerExitTrigger();
        }
    }

    public static bool IsPlayerCollider(Collider col)
    {
        if (col == null) return false;
        if (col.CompareTag("Player") || col.CompareTag("MainCamera")) return true;
        if (col.transform.root != null && (col.transform.root.CompareTag("Player") || col.transform.root.name.Contains("PLAYER") || col.transform.root.name.Contains("Player"))) return true;
        if (col.GetComponentInParent<XROrigin>() != null) return true;
        if (col.GetComponentInParent<Camera>() != null) return true;
        if (col.name.Contains("Camera") || col.name.Contains("XR Origin") || col.name.Contains("Hand") || col.name.Contains("Controller") || col.name.Contains("HMD")) return true;
        return false;
    }

    [ContextMenu("Buka Pintu")]
    public void OpenDoor()
    {
        if (isOpen) return;
        isOpen = true;
        PlaySFX(openSound);

        if (nativeDoorScript != null) nativeDoorScript.open = true;

        if (doorAnimator != null)
        {
            doorAnimator.SetBool("DoorOpen", true);
            doorAnimator.SetBool("IsOpen", true);
        }

        Debug.Log($"[{gameObject.name}] Pintu TERBUKA (Engsel mengayun ke {openAngle}°)");
    }

    [ContextMenu("Tutup Pintu")]
    public void CloseDoor()
    {
        if (!isOpen) return;
        isOpen = false;
        PlaySFX(closeSound);

        if (nativeDoorScript != null) nativeDoorScript.open = false;

        if (doorAnimator != null)
        {
            doorAnimator.SetBool("DoorOpen", false);
            doorAnimator.SetBool("IsOpen", false);
        }

        Debug.Log($"[{gameObject.name}] Pintu TERTUTUP (Engsel kembali ke {closeAngle}°)");
    }

    public void ToggleDoor()
    {
        if (isOpen) CloseDoor();
        else OpenDoor();
    }

    private void PlaySFX(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
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
        try
        {
            if (Keyboard.current != null && (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame))
                return true;
        }
        catch { }
#endif

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (pivotEngsel != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(pivotEngsel.position, 0.08f);
            Gizmos.DrawLine(pivotEngsel.position, pivotEngsel.position + Vector3.up * 2.1f);
        }

        if (triggerArea != null)
        {
            Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.35f);
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = triggerArea.transform.localToWorldMatrix;
            Gizmos.DrawCube(triggerArea.center, triggerArea.size);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(triggerArea.center, triggerArea.size);
            Gizmos.matrix = oldMatrix;
        }

        if (proximityDistance > 0f)
        {
            Gizmos.color = new Color(0f, 0.8f, 1f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, proximityDistance);
        }
    }
}
