using UnityEngine;

/// <summary>
/// Proximity UI Controller untuk Modul Praktikum AR/VR: Sistem Menu Spasial (Spatial UI) di Lab K3
/// Menampilkan panel UI secara otomatis saat pemain mendekati area kerja mesin (IsTrigger)
/// dan menyembunyikannya kembali saat pemain menjauh untuk mencegah cognitive overload dan motion sickness.
/// </summary>
public class ProximityUI : MonoBehaviour
{
    [Tooltip("Panel UI World Space yang akan dikontrol visibilitasnya")]
    public GameObject panelUI;

    private void Start()
    {
        if (panelUI != null)
        {
            panelUI.SetActive(false); // Awal mulai: panel sembunyi
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("MainCamera") || other.CompareTag("Player"))
        {
            if (panelUI != null)
            {
                panelUI.SetActive(true); // Dekat mesin: panel langsung muncul
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("MainCamera") || other.CompareTag("Player"))
        {
            if (panelUI != null)
            {
                panelUI.SetActive(false); // Menjauh: panel sembunyi lagi
            }
        }
    }
}

