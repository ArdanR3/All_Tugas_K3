using UnityEngine;

/// <summary>
/// Mengatur target frame rate ke 60 FPS pada perangkat Android
/// dan menjaga performa rendering tetap stabil dan mulus.
/// </summary>
public class MobileFPSOptimizer : MonoBehaviour
{
    private void Awake()
    {
        // Kunci di 60 FPS agar pergerakan AR mulus dan responsif
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        
        // Mencegah layar HP mati/tidur otomatis saat aplikasi AR aktif
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }
}
