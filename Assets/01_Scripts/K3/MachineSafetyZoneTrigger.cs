using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
#if UNITY_XR_MANAGEMENT
using Unity.XR.CoreUtils;
#endif
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Sistem Kontrol Palang Pengaman K3 (Safety Barrier Controller):
/// Mengontrol palang pengaman otomatis (Safety Barrier), lampu peringatan status (Warning Lamp),
/// deteksi proximity pemain VR/kamera, trigger collider, dan interaksi manual (tombol 'E' / XR Ray).
/// 
/// Secara default beroperasi dalam mode AutomaticGate (Palang Tertutup saat siaga/tidak ada orang,
/// otomatis terbuka dengan lampu hijau saat karakter mendekat, dan otomatis menutup kembali
/// dengan lampu merah saat karakter meninggalkan area sensor).
/// </summary>
public class MachineSafetyZoneTrigger : MonoBehaviour
{
    public enum BarrierMode
    {
        [Tooltip("Palang tertutup saat siaga. Otomatis terbuka saat didekati, menutup saat ditinggalkan.")]
        AutomaticGate,
        [Tooltip("Palang menutup saat pemain memasuki zona bahaya mesin.")]
        HazardZoneInterlock
    }

    [Header("Pengaturan Mode Operasi")]
    [SerializeField] private BarrierMode barrierMode = BarrierMode.AutomaticGate;

    [Header("Safety Barrier")]
    [SerializeField] private Animator barrierAnimator;
    [SerializeField] private string barrierParamName = "BarrierClosed";

    [Header("Warning Lamp")]
    [SerializeField] private Renderer warningLampRenderer;
    [SerializeField] private Light warningLight;
    [SerializeField] private Color openColor = new Color(0.1f, 1.0f, 0.25f, 1.0f);   // Hijau (Akses Aman / Terbuka)
    [SerializeField] private Color closedColor = new Color(1.0f, 0.15f, 0.1f, 1.0f); // Merah (Siaga / Tertutup / Bahaya)

    [Header("Deteksi Proximity")]
    [SerializeField] private float proximityDistance = 3.2f;
    [SerializeField] private bool autoTriggerOnProximity = true;
    [SerializeField] private bool allowKeyToggle = true;

    private int barrierParamHash;
    private MaterialPropertyBlock propBlock;
    private bool isBarrierCurrentlyClosed = true;
    private bool isPlayerNearby = false;
    private Transform cachedPlayerTransform;
    private float nextPlayerSearchTime = 0f;

    private void Awake()
    {
        barrierParamHash = Animator.StringToHash(barrierParamName);
        propBlock = new MaterialPropertyBlock();

        ResolveReferences();
        AutoAlignTriggerPosition();

        // Inisialisasi awal: Palang tertutup rapat & lampu menyala merah siaga
        if (barrierMode == BarrierMode.AutomaticGate)
        {
            CloseBarrier(true);
        }
        else
        {
            OpenBarrier(true);
        }
    }

    private void Start()
    {
        ResolveReferences();
        SetupXRInteractable();
        FindPlayerTransform();

        // Pertegas state di frame awal untuk memastikan Animator controller sinkron
        if (barrierMode == BarrierMode.AutomaticGate)
        {
            CloseBarrier(true);
        }
        else
        {
            OpenBarrier(true);
        }
    }

    private void ResolveReferences()
    {
        if (barrierAnimator == null)
        {
            barrierAnimator = GetComponentInChildren<Animator>();
            if (barrierAnimator == null && transform.parent != null)
            {
                barrierAnimator = transform.parent.GetComponentInChildren<Animator>();
            }
        }

        if (warningLampRenderer == null)
        {
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                if (r.name.ToLowerInvariant().Contains("lamp") || r.name.ToLowerInvariant().Contains("bulb"))
                {
                    warningLampRenderer = r;
                    break;
                }
            }
            if (warningLampRenderer == null && transform.parent != null)
            {
                var parentRends = transform.parent.GetComponentsInChildren<Renderer>();
                foreach (var r in parentRends)
                {
                    if (r.name.ToLowerInvariant().Contains("lamp") || r.name.ToLowerInvariant().Contains("bulb"))
                    {
                        warningLampRenderer = r;
                        break;
                    }
                }
            }
        }

        if (warningLight == null)
        {
            warningLight = GetComponentInChildren<Light>();
            if (warningLight == null && transform.parent != null)
            {
                warningLight = transform.parent.GetComponentInChildren<Light>();
            }
        }
    }

    private void AutoAlignTriggerPosition()
    {
        // Posisi trigger tetap sesuai posisi aslinya
    }

    private void SetupXRInteractable()
    {
        if (barrierAnimator != null)
        {
            var interactable = barrierAnimator.GetComponent<XRSimpleInteractable>();
            if (interactable == null)
            {
                interactable = barrierAnimator.gameObject.AddComponent<XRSimpleInteractable>();
            }

            interactable.selectEntered.RemoveListener(OnVRSelect);
            interactable.selectEntered.AddListener(OnVRSelect);
        }
    }

    private void OnVRSelect(BaseInteractionEventArgs args)
    {
        ToggleBarrier();
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
#if UNITY_2023_1_OR_NEWER
        var origin = Object.FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
#else
        var origin = Object.FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
#endif
        if (origin != null)
        {
            cachedPlayerTransform = origin.Camera != null ? origin.Camera.transform : origin.transform;
            return cachedPlayerTransform;
        }

        return null;
    }

    private void Update()
    {
        if (cachedPlayerTransform == null && Time.time >= nextPlayerSearchTime)
        {
            nextPlayerSearchTime = Time.time + 1.0f;
            FindPlayerTransform();
        }

        // Deteksi Proximity Otomatis (Planar XZ distance)
        if (autoTriggerOnProximity && cachedPlayerTransform != null)
        {
            Vector3 checkCenter = barrierAnimator != null ? barrierAnimator.transform.position : transform.position;
            Vector3 diff = cachedPlayerTransform.position - checkCenter;
            diff.y = 0; // Abaikan perbedaan tinggi
            float dist = diff.magnitude;

            if (dist <= proximityDistance)
            {
                if (!isPlayerNearby)
                {
                    isPlayerNearby = true;
                    OnPlayerApproached();
                }
            }
            else if (isPlayerNearby && dist > proximityDistance + 0.8f)
            {
                isPlayerNearby = false;
                OnPlayerDeparted();
            }
        }

        // Interaksi manual melalui keyboard 'E' atau Space
        if (allowKeyToggle && isPlayerNearby && ReadInteractKey())
        {
            ToggleBarrier();
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

    public static bool IsPlayerCollider(Collider other)
    {
        if (other == null) return false;
        if (other.CompareTag("Player") || other.CompareTag("MainCamera")) return true;
        if (other.transform.root != null && (other.transform.root.CompareTag("Player") || other.transform.root.CompareTag("MainCamera"))) return true;

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
        OnPlayerApproached();
    }

    private void OnTriggerStay(Collider other)
    {
        if (!IsPlayerCollider(other)) return;

        isPlayerNearby = true;
        if (barrierMode == BarrierMode.AutomaticGate && isBarrierCurrentlyClosed)
        {
            OpenBarrier();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayerCollider(other)) return;

        isPlayerNearby = false;
        OnPlayerDeparted();
    }

    private void OnPlayerApproached()
    {
        if (barrierMode == BarrierMode.AutomaticGate)
        {
            OpenBarrier();
        }
        else
        {
            CloseBarrier();
            Debug.Log("[BARRIER_SYSTEM] PERINGATAN: Karakter memasuki zona bahaya mesin! Palang diturunkan.");
        }
    }

    private void OnPlayerDeparted()
    {
        if (barrierMode == BarrierMode.AutomaticGate)
        {
            CloseBarrier();
        }
        else
        {
            OpenBarrier();
            Debug.Log("[BARRIER_SYSTEM] Karakter meninggalkan zona bahaya mesin. Status kembali aman.");
        }
    }

    public void OpenBarrier(bool force = false)
    {
        if (!isBarrierCurrentlyClosed && !force) return;

        isBarrierCurrentlyClosed = false;
        SetBarrierAnimatorBool(false);
        SetWarningLampColor(openColor);
        Debug.Log("[BARRIER_SYSTEM] Karakter mendekati sensor -> Palang Pengaman TERBUKA otomatis (Akses Diberikan).");
    }

    public void CloseBarrier(bool force = false)
    {
        if (isBarrierCurrentlyClosed && !force) return;

        isBarrierCurrentlyClosed = true;
        SetBarrierAnimatorBool(true);
        SetWarningLampColor(closedColor);
        Debug.Log("[BARRIER_SYSTEM] Karakter meninggalkan sensor -> Palang Pengaman TERTUTUP otomatis (Area Diamankan).");
    }

    public void ToggleBarrier()
    {
        if (isBarrierCurrentlyClosed)
        {
            OpenBarrier();
        }
        else
        {
            CloseBarrier();
        }
    }

    private void SetBarrierAnimatorBool(bool isClosed)
    {
        if (barrierAnimator != null)
        {
            barrierAnimator.SetBool(barrierParamHash, isClosed);
        }
    }

    public void SetWarningLampColor(Color targetColor)
    {
        if (warningLampRenderer != null)
        {
            if (propBlock == null) propBlock = new MaterialPropertyBlock();
            warningLampRenderer.GetPropertyBlock(propBlock);
            propBlock.SetColor("_BaseColor", targetColor);
            propBlock.SetColor("_Color", targetColor);
            propBlock.SetColor("_EmissionColor", targetColor * 2.5f);
            warningLampRenderer.SetPropertyBlock(propBlock);

            if (warningLampRenderer.material != null)
            {
                warningLampRenderer.material.color = targetColor;
                if (warningLampRenderer.material.HasProperty("_EmissionColor"))
                {
                    warningLampRenderer.material.SetColor("_EmissionColor", targetColor * 2.5f);
                }
            }
        }

        if (warningLight != null)
        {
            warningLight.color = targetColor;
        }
    }
}
