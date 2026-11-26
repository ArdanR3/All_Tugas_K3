using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

/// <summary>
/// Menangani area tujuan akhir pemindahan objek (Kriteria Modul 6).
/// Menghitung jumlah objek yang berhasil dibawa dari Storage Area ke Destination Area,
/// serta memastikan sistem TeleportationProvider aktif dan terhubung ke XROrigin di scene.
/// </summary>
public class DestinationDropZone : MonoBehaviour
{
    [Header("Latihan M06 Settings")]
    public int targetCount = 3;
    public TextMesh statusTextMesh;

    private readonly HashSet<XRGrabInteractable> deliveredObjects = new HashSet<XRGrabInteractable>();

    private void Awake()
    {
        EnsureLocomotionSetup();
    }

    private void Start()
    {
        UpdateStatusText();
    }

    /// <summary>
    /// Memastikan TeleportationProvider dan LocomotionMediator terpasang pada XROrigin yang aktif,
    /// dan semua TeleportationArea terhubung agar teleportasi langsung berfungsi tanpa hambatan.
    /// </summary>
    public static void EnsureLocomotionSetup()
    {
#if UNITY_2023_1_OR_NEWER
        var origin = Object.FindFirstObjectByType<XROrigin>();
#else
        var origin = Object.FindObjectOfType<XROrigin>();
#endif
        if (origin == null) return;

        var mediator = origin.GetComponentInChildren<LocomotionMediator>();
        if (mediator == null)
        {
            var bodyTransformer = origin.GetComponentInChildren<XRBodyTransformer>();
            if (bodyTransformer == null)
            {
                bodyTransformer = origin.gameObject.AddComponent<XRBodyTransformer>();
            }
            bodyTransformer.xrOrigin = origin;

            mediator = origin.gameObject.AddComponent<LocomotionMediator>();
            mediator.xrOrigin = origin;
        }

        var tpProvider = origin.GetComponentInChildren<TeleportationProvider>();
        if (tpProvider == null)
        {
            tpProvider = origin.gameObject.AddComponent<TeleportationProvider>();
            tpProvider.mediator = mediator;
        }

#if UNITY_2023_1_OR_NEWER
        var tpAreas = Object.FindObjectsByType<TeleportationArea>(FindObjectsSortMode.None);
#else
        var tpAreas = Object.FindObjectsOfType<TeleportationArea>();
#endif
        foreach (var area in tpAreas)
        {
            if (area.teleportationProvider == null)
            {
                area.teleportationProvider = tpProvider;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        var grabItem = other.GetComponentInParent<XRGrabInteractable>();
        if (grabItem != null && !deliveredObjects.Contains(grabItem))
        {
            deliveredObjects.Add(grabItem);
            Debug.Log($"<color=green>[VR Latihan]</color> Objek '{grabItem.gameObject.name}' berhasil ditaruh di Destination Area! ({deliveredObjects.Count}/{targetCount})");
            UpdateStatusText();

            if (deliveredObjects.Count >= targetCount)
            {
                Debug.Log("<color=cyan><b>🎉 [VR Latihan] MISI LATIHAN SELESAI!</b> Minimal 3 objek berhasil dipindahkan dari Storage Area ke Destination Area sesuai kriteria Modul 6!</color>");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var grabItem = other.GetComponentInParent<XRGrabInteractable>();
        if (grabItem != null && deliveredObjects.Contains(grabItem))
        {
            deliveredObjects.Remove(grabItem);
            UpdateStatusText();
        }
    }

    private void UpdateStatusText()
    {
        if (statusTextMesh != null)
        {
            if (deliveredObjects.Count >= targetCount)
            {
                statusTextMesh.text = $"[AREA TUJUAN - DROP ZONE]\n★ STATUS: MISI SELESAI! ★\nObjek Terkumpul: {deliveredObjects.Count}/{targetCount}\n(Kerja Bagus!)";
                statusTextMesh.color = Color.green;
            }
            else
            {
                statusTextMesh.text = $"[AREA TUJUAN - DROP ZONE]\nPindahkan 3 Objek ke Sini\nObjek Terkumpul: {deliveredObjects.Count}/{targetCount}";
                statusTextMesh.color = Color.yellow;
            }
        }
    }
}
