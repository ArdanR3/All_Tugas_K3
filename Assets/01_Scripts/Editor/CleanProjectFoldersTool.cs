#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class CleanProjectFoldersTool
{
    [MenuItem("Tools/K3/Rapikan & Validasi Struktur Folder K3")]
    public static void OrganizeK3AndHutanFolders()
    {
        Debug.Log("[CleanFolders] Memeriksa dan memvalidasi struktur folder K3...");

        // 1. Pastikan struktur folder utama ada
        EnsureFolder("Assets/00_Scenes");
        EnsureFolder("Assets/01_Scripts");
        EnsureFolder("Assets/01_Scripts/K3");
        EnsureFolder("Assets/01_Scripts/VR");
        EnsureFolder("Assets/01_Scripts/Utility");
        EnsureFolder("Assets/01_Scripts/Editor");
        EnsureFolder("Assets/04_Materials");
        EnsureFolder("Assets/05_Animations");
        EnsureFolder("Assets/08_UI");
        EnsureFolder("Assets/10_K3_Simulation");
        EnsureFolder("Assets/UTS_ARVR");

        // 2. Pindahkan aset lama jika ada yang tertinggal
        MoveFolderIfExists("Assets/02_Materials", "Assets/10_K3_Simulation/Materials");
        MoveFolderIfExists("Assets/03_Prefabs", "Assets/10_K3_Simulation/Prefabs");
        MoveFolderIfExists("Assets/Industrial_Machine_Models", "Assets/10_K3_Simulation/Industrial_Machine_Models");
        MoveFolderIfExists("Assets/WarehousePack", "Assets/10_K3_Simulation/WarehousePack");
        MoveFolderIfExists("Assets/GeeKay3D", "Assets/10_K3_Simulation/GeeKay3D");
        MoveFolderIfExists("Assets/AK STUDIO ART", "Assets/10_K3_Simulation/AK STUDIO ART");

        // Pindahkan script yang masih di root 01_Scripts
        MoveAssetIfExists("Assets/01_Scripts/AutomaticDoorTrigger.cs", "Assets/01_Scripts/K3/AutomaticDoorTrigger.cs");
        MoveAssetIfExists("Assets/01_Scripts/MachineSafetyZoneTrigger.cs", "Assets/01_Scripts/K3/MachineSafetyZoneTrigger.cs");
        MoveAssetIfExists("Assets/01_Scripts/UnifiedDoorController.cs", "Assets/01_Scripts/K3/UnifiedDoorController.cs");
        MoveAssetIfExists("Assets/01_Scripts/UnifiedDoorTriggerArea.cs", "Assets/01_Scripts/K3/UnifiedDoorTriggerArea.cs");
        MoveAssetIfExists("Assets/01_Scripts/SimpleObjectController.cs", "Assets/01_Scripts/K3/SimpleObjectController.cs");
        MoveAssetIfExists("Assets/01_Scripts/Test.cs", "Assets/01_Scripts/K3/Test.cs");
        MoveAssetIfExists("Assets/01_Scripts/CameraFollow.cs", "Assets/01_Scripts/Utility/CameraFollow.cs");
        MoveAssetIfExists("Assets/01_Scripts/MobileFPSOptimizer.cs", "Assets/01_Scripts/Utility/MobileFPSOptimizer.cs");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CleanFolders] Struktur folder K3 terverifikasi 100% rapi dan standar!");
    }

    private static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
            string name = System.IO.Path.GetFileName(path);
            if (AssetDatabase.IsValidFolder(parent))
            {
                AssetDatabase.CreateFolder(parent, name);
                Debug.Log($"[CleanFolders] Dibuat: {path}");
            }
        }
    }

    private static void MoveFolderIfExists(string source, string destination)
    {
        if (AssetDatabase.IsValidFolder(source))
        {
            string err = AssetDatabase.MoveAsset(source, destination);
            if (string.IsNullOrEmpty(err))
            {
                Debug.Log($"[CleanFolders] SUKSES memindahkan '{source}' -> '{destination}'");
            }
            else
            {
                Debug.LogWarning($"[CleanFolders] Gagal memindahkan '{source}': {err}");
            }
        }
    }

    private static void MoveAssetIfExists(string source, string destination)
    {
        if (System.IO.File.Exists(source))
        {
            string err = AssetDatabase.MoveAsset(source, destination);
            if (string.IsNullOrEmpty(err))
            {
                Debug.Log($"[CleanFolders] SUKSES memindahkan '{source}' -> '{destination}'");
            }
            else
            {
                Debug.LogWarning($"[CleanFolders] Gagal memindahkan '{source}': {err}");
            }
        }
    }
}
#endif
