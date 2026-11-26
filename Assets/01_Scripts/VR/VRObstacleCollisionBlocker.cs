using UnityEngine;
using Unity.XR.CoreUtils;

/// <summary>
/// Mencegah pemain/kamera VR menembus rintangan lingkungan secara langsung (HMD/WASD translation / roomscale).
/// Pemain HANYA diperbolehkan berpindah melewati rintangan menggunakan Teleportasi.
/// Memperhitungkan batas horizontal 2D / kolom vertikal tubuh manusia sehingga rintangan dengan tinggi
/// visual berapa pun (termasuk barikade setinggi lutut) tetap menjadi pembatas yang kokoh dan tidak dapat ditembus.
/// Posisi dan bentuk visual obstacle tetap dipertahankan tanpa perubahan.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class VRObstacleCollisionBlocker : MonoBehaviour
{
    [Header("Pengaturan Tabrakan VR")]
    [Tooltip("Radius batas tubuh/kepala pemain (dalam meter)")]
    public float headRadius = 0.45f;

    [Tooltip("Batas bawah Y relatif dari posisi obstacle untuk mendeteksi pemain")]
    public float verticalMin = -1.0f;

    [Tooltip("Batas atas Y relatif dari posisi obstacle untuk mendeteksi pemain")]
    public float verticalMax = 3.5f;

    [Tooltip("Tampilkan efek peringatan saat menabrak rintangan")]
    public bool showWarningOnPush = true;

    private BoxCollider boxCollider;
    private Transform xrCameraTransform;
    private XROrigin xrOrigin;
    private Vector3 lastSafePosition;
    private bool hasValidSafePos;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
    }

    private void Start()
    {
        FindPlayerCamera();
    }

    private void FindPlayerCamera()
    {
        if (Camera.main != null)
        {
            xrCameraTransform = Camera.main.transform;
            xrOrigin = Camera.main.GetComponentInParent<XROrigin>();
            if (xrOrigin == null)
            {
#if UNITY_2023_1_OR_NEWER
                xrOrigin = Object.FindFirstObjectByType<XROrigin>();
#else
                xrOrigin = FindObjectOfType<XROrigin>();
#endif
            }
            lastSafePosition = xrCameraTransform.position;
            hasValidSafePos = true;
        }
    }

    private void LateUpdate()
    {
        if (xrCameraTransform == null)
        {
            FindPlayerCamera();
            if (xrCameraTransform == null) return;
        }

        Vector3 currentHeadPos = xrCameraTransform.position;

        // Cek apakah perpindahan seketika adalah Teleportasi yang sah (> 1.2m dalam satu frame)
        if (hasValidSafePos)
        {
            float frameDisplacement = Vector3.Distance(currentHeadPos, lastSafePosition);
            if (frameDisplacement > 1.2f)
            {
                // Ini adalah aksi teleportasi. Jika titik pendaratan tidak berada di dalam rintangan, izinkan.
                if (!IsPointInsideObstacleVolume(currentHeadPos, 0.1f))
                {
                    lastSafePosition = currentHeadPos;
                    return;
                }
            }
        }

        // Cek apakah pemain menabrak atau mencoba menembus zona rintangan
        if (IsPointInsideObstacleVolume(currentHeadPos, headRadius))
        {
            // Hitung vektor dorongan keluar agar pemain tetap berada di luar rintangan
            Vector3 pushOffset = CalculatePushOutOffset(currentHeadPos, headRadius);

            // Terapkan koreksi ke XROrigin (atau parent rig pemain)
            if (xrOrigin != null)
            {
                xrOrigin.transform.position += pushOffset;
            }
            else if (xrCameraTransform.parent != null)
            {
                xrCameraTransform.parent.position += pushOffset;
            }
            else
            {
                xrCameraTransform.position += pushOffset;
            }

            // Simpan posisi setelah dikoreksi sebagai posisi aman
            lastSafePosition = xrCameraTransform.position;
            hasValidSafePos = true;
        }
        else
        {
            // Posisi aman di luar rintangan, simpan sebagai checkpoint
            lastSafePosition = currentHeadPos;
            hasValidSafePos = true;
        }
    }

    /// <summary>
    /// Mengecek apakah suatu titik di world space berada di dalam kolom volume rintangan
    /// (memperhitungkan radius pemain dan tinggi manusia dari lantai hingga plafon).
    /// </summary>
    public bool IsPointInsideObstacleVolume(Vector3 worldPoint, float radius)
    {
        if (boxCollider == null) boxCollider = GetComponent<BoxCollider>();

        Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
        Vector3 boxCenter = boxCollider.center;
        Vector3 boxSize = boxCollider.size;

        // Cek ketinggian vertikal (Y lokal)
        // Rintangan visual mungkin setinggi 60cm, namun zona keselamatan K3 memblokir seluruh ketinggian manusia
        float localMinY = boxCenter.y - (boxSize.y * 0.5f) + verticalMin;
        float localMaxY = boxCenter.y + (boxSize.y * 0.5f) + verticalMax;

        if (localPoint.y < localMinY || localPoint.y > localMaxY)
        {
            return false;
        }

        // Hitung jarak horizontal di bidang X-Z lokal dalam satuan meter dunia (world meters)
        Vector3 lossyScale = transform.lossyScale;
        float halfWidthX = (boxSize.x * 0.5f) * lossyScale.x;
        float halfLengthZ = (boxSize.z * 0.5f) * lossyScale.z;

        float distMetersX = (localPoint.x - boxCenter.x) * lossyScale.x;
        float distMetersZ = (localPoint.z - boxCenter.z) * lossyScale.z;

        float clampedX = Mathf.Clamp(distMetersX, -halfWidthX, halfWidthX);
        float clampedZ = Mathf.Clamp(distMetersZ, -halfLengthZ, halfLengthZ);

        float dx = distMetersX - clampedX;
        float dz = distMetersZ - clampedZ;
        float horizontalDistance = Mathf.Sqrt(dx * dx + dz * dz);

        return horizontalDistance < radius;
    }

    /// <summary>
    /// Menghitung vektor dorongan keluar (dalam world space) agar pemain tidak menembus batas rintangan.
    /// </summary>
    private Vector3 CalculatePushOutOffset(Vector3 worldPoint, float radius)
    {
        if (boxCollider == null) boxCollider = GetComponent<BoxCollider>();

        Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
        Vector3 boxCenter = boxCollider.center;
        Vector3 boxSize = boxCollider.size;
        Vector3 lossyScale = transform.lossyScale;

        float halfWidthX = (boxSize.x * 0.5f) * lossyScale.x;
        float halfLengthZ = (boxSize.z * 0.5f) * lossyScale.z;

        float distMetersX = (localPoint.x - boxCenter.x) * lossyScale.x;
        float distMetersZ = (localPoint.z - boxCenter.z) * lossyScale.z;

        float clampedX = Mathf.Clamp(distMetersX, -halfWidthX, halfWidthX);
        float clampedZ = Mathf.Clamp(distMetersZ, -halfLengthZ, halfLengthZ);

        float dx = distMetersX - clampedX;
        float dz = distMetersZ - clampedZ;
        float horizontalDistance = Mathf.Sqrt(dx * dx + dz * dz);

        Vector3 pushDirectionWorld;
        float requiredPushDistance;

        if (horizontalDistance > 0.001f)
        {
            // Berada di luar kotak tetapi dalam radius buffer: dorong menjauh sepanjang normal terdekat
            Vector3 localPushDir = new Vector3(
                lossyScale.x > 0.0001f ? dx / lossyScale.x : dx,
                0,
                lossyScale.z > 0.0001f ? dz / lossyScale.z : dz
            ).normalized;

            pushDirectionWorld = transform.TransformDirection(localPushDir).normalized;
            requiredPushDistance = (radius - horizontalDistance) + 0.03f;
        }
        else
        {
            // Pemain berada di dalam kotak rintangan: dorong keluar ke sisi terdekat
            float overlapX = halfWidthX - Mathf.Abs(distMetersX);
            float overlapZ = halfLengthZ - Mathf.Abs(distMetersZ);

            if (overlapX <= overlapZ)
            {
                float signX = distMetersX >= 0 ? 1f : -1f;
                Vector3 localPushDir = new Vector3(signX, 0, 0);
                pushDirectionWorld = transform.TransformDirection(localPushDir).normalized;
                requiredPushDistance = (halfWidthX - Mathf.Abs(distMetersX)) + radius + 0.03f;
            }
            else
            {
                float signZ = distMetersZ >= 0 ? 1f : -1f;
                Vector3 localPushDir = new Vector3(0, 0, signZ);
                pushDirectionWorld = transform.TransformDirection(localPushDir).normalized;
                requiredPushDistance = (halfLengthZ - Mathf.Abs(distMetersZ)) + radius + 0.03f;
            }
        }

        return pushDirectionWorld * requiredPushDistance;
    }

    private void OnDrawGizmosSelected()
    {
        if (boxCollider == null) boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            Gizmos.color = new Color(1f, 0.2f, 0f, 0.35f);
            Vector3 center = transform.TransformPoint(boxCollider.center + new Vector3(0, (verticalMax + verticalMin) * 0.5f, 0));
            Vector3 size = new Vector3(
                boxCollider.size.x * transform.lossyScale.x + headRadius * 2f,
                (verticalMax - verticalMin),
                boxCollider.size.z * transform.lossyScale.z + headRadius * 2f
            );
            Gizmos.matrix = Matrix4x4.TRS(center, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, size);
        }
    }
}
