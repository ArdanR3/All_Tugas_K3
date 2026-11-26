using UnityEngine;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

/// <summary>
/// SOAL 1 UTS Pemrograman Berbasis AR/VR
/// Mendampingi CharacterController pada XR Origin untuk memastikan pengguna TIDAK DAPAT
/// menembus dinding, meja, mesin, maupun struktur lingkungan lainnya baik saat bergerak
/// dengan locomotion maupun translasi HMD pada XR Interaction Simulator.
/// </summary>
[DefaultExecutionOrder(-1000)]
[RequireComponent(typeof(CharacterController))]
public class VRWorkshopBoundaryGuard : MonoBehaviour
{
    [Header("Konfigurasi Batas Fisik Pemain")]
    public float bodyRadius = 0.32f;
    public float capsuleBottomY = 0.35f;
    public float capsuleTopY = 1.65f;

    private CharacterController characterController;
    private XROrigin xrOrigin;
    private Transform cameraTransform;
    private CapsuleCollider helperCapsule;
    private readonly Collider[] overlapBuffer = new Collider[24];

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        xrOrigin = GetComponent<XROrigin>();

        // Pastikan Main Camera bertag "MainCamera" agar XR Interaction Simulator (Camera.main) dapat menggerakkan HMD & Controller
        if (xrOrigin != null && xrOrigin.Camera != null)
        {
            xrOrigin.Camera.gameObject.tag = "MainCamera";
            cameraTransform = xrOrigin.Camera.transform;
        }
        else
        {
            Camera childCam = GetComponentInChildren<Camera>(true);
            if (childCam != null)
            {
                childCam.gameObject.tag = "MainCamera";
                cameraTransform = childCam.transform;
            }
        }

        EnsureXRInteractionBindings();

        // Buat helper capsule non-aktif untuk perhitungan Physics.ComputePenetration yang presisi
        GameObject helperObj = new GameObject("_BoundaryProbe");
        helperObj.hideFlags = HideFlags.HideAndDontSave;
        helperObj.transform.SetParent(transform, false);
        helperCapsule = helperObj.AddComponent<CapsuleCollider>();
        helperCapsule.isTrigger = true;
        helperCapsule.enabled = false;
    }

    private void EnsureXRInteractionBindings()
    {
        XRInteractionManager mgr = FindFirstObjectByType<XRInteractionManager>();
        TeleportationProvider tpProvider = GetComponentInChildren<TeleportationProvider>(true);

        // Pastikan seluruh Teleport Interactor kompatibel dengan bit layer Teleport (bit 1 & bit 31)
        int teleportMask = (1 << 1) | (1 << 31);
        foreach (var ray in GetComponentsInChildren<XRRayInteractor>(true))
        {
            if (ray.gameObject.name.Contains("Teleport"))
            {
                ray.interactionLayers = teleportMask;
            }
        }

        // Pastikan NearFarInteractor (Left & Right Hand) dapat melakukan Grab (bit 0 Default & bit 2 Grab)
        int grabMask = (1 << 0) | (1 << 2);
        foreach (var nf in GetComponentsInChildren<NearFarInteractor>(true))
        {
            nf.interactionLayers = grabMask;
        }

        // Sinkronisasi seluruh TeleportationArea di scene
        foreach (var tpArea in FindObjectsByType<TeleportationArea>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            tpArea.interactionLayers = -1;
            if (mgr != null && tpArea.interactionManager == null)
                tpArea.interactionManager = mgr;
            if (tpProvider != null && tpArea.teleportationProvider == null)
                tpArea.teleportationProvider = tpProvider;
        }

        // Sinkronisasi seluruh XRGrabInteractable di scene
        foreach (var grab in FindObjectsByType<XRGrabInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            grab.interactionLayers = grabMask;
            if (mgr != null && grab.interactionManager == null)
                grab.interactionManager = mgr;
        }
    }

    private void Start()
    {
        ResolveCamera();
    }

    private void ResolveCamera()
    {
        if (xrOrigin != null && xrOrigin.Camera != null)
        {
            cameraTransform = xrOrigin.Camera.transform;
        }
        else if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void LateUpdate()
    {
        if (cameraTransform == null)
        {
            ResolveCamera();
            if (cameraTransform == null) return;
        }

        // Sinkronisasi center CharacterController dengan posisi X-Z kamera VR
        Vector3 localCam = transform.InverseTransformPoint(cameraTransform.position);
        if (characterController != null)
        {
            float h = Mathf.Clamp(localCam.y, 1.0f, 2.2f);
            characterController.height = h;
            characterController.radius = bodyRadius;
            characterController.center = new Vector3(localCam.x, h * 0.5f, localCam.z);
        }

        // Cek tabrakan terhadap struktur lingkungan (dinding, meja, mesin)
        Vector3 headPos = cameraTransform.position;
        Vector3 p1 = new Vector3(headPos.x, transform.position.y + capsuleBottomY, headPos.z);
        Vector3 p2 = new Vector3(headPos.x, transform.position.y + capsuleTopY, headPos.z);

        helperCapsule.radius = bodyRadius;
        helperCapsule.height = Mathf.Max(capsuleTopY - capsuleBottomY + bodyRadius * 2f, bodyRadius * 2.1f);
        helperCapsule.center = Vector3.zero;
        Vector3 capsuleCenterWorld = (p1 + p2) * 0.5f;

        int hitCount = Physics.OverlapCapsuleNonAlloc(p1, p2, bodyRadius, overlapBuffer, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hitCount; i++)
        {
            Collider col = overlapBuffer[i];
            if (col == null) continue;
            if (col.transform.IsChildOf(transform)) continue;

            // Abaikan lantai teleport & lantai dasar
            if (col.GetComponent<TeleportationArea>() != null) continue;
            if (col.gameObject.name.Contains("Floor") || col.gameObject.name.Contains("Ground")) continue;

            // Abaikan objek alat yang bisa di-grab (Rigidbody dinamis / XRGrabInteractable)
            if (col.GetComponentInParent<XRGrabInteractable>() != null) continue;
            if (col.attachedRigidbody != null && !col.attachedRigidbody.isKinematic) continue;

            // Hitung penetrasi dan dorong pemain keluar dari dinding/meja/mesin
            helperCapsule.enabled = true;
            bool overlapped = Physics.ComputePenetration(
                helperCapsule, capsuleCenterWorld, Quaternion.identity,
                col, col.transform.position, col.transform.rotation,
                out Vector3 dir, out float dist
            );
            helperCapsule.enabled = false;

            if (overlapped && dist > 0.001f)
            {
                Vector3 push = new Vector3(dir.x, 0f, dir.z);
                if (push.sqrMagnitude > 0.0001f)
                {
                    push = push.normalized * (dist + 0.01f);
                    transform.position += push;
                    capsuleCenterWorld += push;
                }
            }
        }
    }
}

