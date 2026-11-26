using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// SOAL 3 UTS Pemrograman Berbasis AR/VR
/// Mekanisme pemeriksaan peralatan pada Tool Check Area berdasarkan kategori (HandTool, PowerTool, MeasuringTool).
/// Khusus Meja Hand Tools: Mendeteksi objek Box/Cube. Saat diletakkan di meja, langsung mengeluarkan output ke Console!
/// </summary>
public class ToolCheckArea : MonoBehaviour
{
    [Header("UI Informasi Pemeriksaan")]
    [Tooltip("UI/Text banner yang akan muncul jika objek yang diletakkan sesuai kategori")]
    public GameObject infoText;

    [Header("Konfigurasi Kategori Alat")]
    [Tooltip("Tag alat yang sesuai untuk area pemeriksaan ini (HandTool / PowerTool / MeasuringTool)")]
    public string targetToolTag = "HandTool";

    [Tooltip("Nama kategori alat untuk ditampilkan pada Debug Log Console")]
    public string categoryName = "Hand Tools (Box/Cube)";

    private void Start()
    {
        // Pada kondisi awal, teks informasi disembunyikan
        if (infoText != null)
        {
            infoText.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Abaikan trigger zone lain atau collider pemain
        if (other == null || other.isTrigger) return;

        string objName = GetObjectName(other);

        if (IsMatchingTool(other))
        {
            // 1. Tampilkan UI Informasi
            if (infoText != null)
            {
                infoText.SetActive(true);
            }

            // 2. Tampilkan Log Jelas di Unity Console
            Debug.Log($"<color=#00FF00><b>[TOOL CHECK - VALIDASI BERHASIL]</b></color> " +
                      $"Objek '<b>{objName}</b>' TELAH SESUAI dan diletakkan di atas Meja <b>{categoryName}</b>! " +
                      $"(Tag: {targetToolTag})");
        }
        else
        {
            // Cek jika objek yang masuk adalah peralatan grabbable lain tapi salah meja
            bool isOtherGrabbable = other.GetComponentInParent<XRGrabInteractable>() != null ||
                                    (other.attachedRigidbody != null && !other.attachedRigidbody.isKinematic);

            if (isOtherGrabbable && !other.CompareTag("Untagged") && other.tag != targetToolTag)
            {
                Debug.LogWarning($"<color=#FFA500><b>[TOOL CHECK - PERALATAN SALAH MEJA]</b></color> " +
                                 $"Objek '<b>{objName}</b>' (Tag: {other.tag}) BUKAN {targetToolTag}! " +
                                 $"Meja ini khusus untuk kategori <b>{categoryName}</b>.");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other == null || other.isTrigger) return;

        if (IsMatchingTool(other))
        {
            // Sembunyikan UI Informasi saat objek diambil
            if (infoText != null)
            {
                infoText.SetActive(false);
            }

            string objName = GetObjectName(other);
            Debug.Log($"<color=#00D2FF>[TOOL CHECK - OBJEK DIANGKAT]</color> " +
                      $"Peralatan '<b>{objName}</b>' telah diangkat keluar dari Meja <b>{categoryName}</b>. Status di-reset.");
        }
    }

    private bool IsMatchingTool(Collider other)
    {
        if (other == null || string.IsNullOrEmpty(targetToolTag))
            return false;

        if (other.CompareTag(targetToolTag))
            return true;

        if (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag(targetToolTag))
            return true;

        if (other.transform.root != null && other.transform.root.CompareTag(targetToolTag))
            return true;

        return false;
    }

    private string GetObjectName(Collider col)
    {
        if (col.attachedRigidbody != null)
            return col.attachedRigidbody.gameObject.name;
        if (col.transform.parent != null)
            return col.transform.parent.gameObject.name;
        return col.gameObject.name;
    }
}
