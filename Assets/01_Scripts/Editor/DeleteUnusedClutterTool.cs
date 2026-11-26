#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

public class DeleteUnusedClutterTool
{

    [MenuItem("Tools/K3/Hapus Objek Tidak Terpakai (Packing & Clutter)", false, 1)]
    public static void ManualCleanup()
    {
        ExecuteCleanup(true);
    }

    public static void ExecuteCleanup(bool showDialog = false)
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        if (!activeScene.name.Contains("SCN_LabK3_01"))
        {
            string scenePath = "Assets/00_Scenes/SCN_LabK3_01.unity";
            if (System.IO.File.Exists(scenePath))
            {
                activeScene = EditorSceneManager.OpenScene(scenePath);
            }
            else
            {
                Debug.LogWarning("[DeleteUnusedClutter] Scene SCN_LabK3_01 tidak ditemukan.");
                return;
            }
        }

        Debug.Log("[DeleteUnusedClutter] === MEMULAI PEMBERSIHAN OBJEK TIDAK TERPAKAI DI SCN_LabK3_01 ===");
        int deletedCount = 0;

        // 1. Hapus grup "Packing fixtures" (meja packing, garis putih lantai, palet kayu, kardus di lantai)
        GameObject packing = GameObject.Find("Packing fixtures");
        if (packing != null)
        {
            Undo.DestroyObjectImmediate(packing);
            deletedCount++;
            Debug.Log("[DeleteUnusedClutter] Menghapus 'Packing fixtures' beserta seluruh mejanya, garis putih, dan kardusnya.");
        }

        // 2. Hapus grup "Shipping dock fixtures" (garis hazard pintu, bumper, palet kayu, boks)
        GameObject shipping = GameObject.Find("Shipping dock fixtures");
        if (shipping != null)
        {
            Undo.DestroyObjectImmediate(shipping);
            deletedCount++;
            Debug.Log("[DeleteUnusedClutter] Menghapus 'Shipping dock fixtures' beserta seluruh garis border dan muatannya.");
        }

        // 3. Hapus garis kuning putus-putus lorong tengah "FloorLine_AisleCentreline"
        GameObject aisleLine = GameObject.Find("FloorLine_AisleCentreline");
        if (aisleLine != null)
        {
            Undo.DestroyObjectImmediate(aisleLine);
            deletedCount++;
            Debug.Log("[DeleteUnusedClutter] Menghapus 'FloorLine_AisleCentreline' (garis putus-putus kuning di lantai).");
        }

        // 4. Hapus HandPalletJack (dongkrak / hand pallet kuning) yang tidak terpakai
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        List<GameObject> toDelete = new List<GameObject>();

        foreach (var go in allObjects)
        {
            if (go == null) continue;

            // Hapus HandPalletJack
            if (go.name.Contains("HandPalletJack"))
            {
                // Ambil root paling atas dari instance HandPalletJack
                GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(go) ?? go;
                if (!toDelete.Contains(root)) toDelete.Add(root);
            }

            // Hapus BlankAFrameSign (rambu kerucut kuning) di area lantai kosong tengah/selatan (Z < 0)
            if (go.name.Contains("BlankAFrameSign"))
            {
                GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(go) ?? go;
                // Hanya hapus yang berada di area lantai terbuka (bukan rambu mesin/pintu di Z > 0)
                if (root.transform.position.z < 0f && !toDelete.Contains(root))
                {
                    toDelete.Add(root);
                }
            }

            // Hapus sisa garis kerja outline putih jika masih ada di luar Packing fixtures
            if (go.name.Contains("FloorLine_WorkAreaOutline") || go.name.Contains("FloorLine_DockWalkway"))
            {
                if (!toDelete.Contains(go)) toDelete.Add(go);
            }

            // Hapus sisa cluster Lemari_Barang palsu yang posisinya (0,0,0) dan hanya berisi tumpukan kardus lepas tanpa rak besi
            if (go.name.StartsWith("Lemari_Barang") && go.transform.localPosition == Vector3.zero)
            {
                // Cek apakah punya PalletRack_Run
                if (go.transform.Find("PalletRack_Run") == null)
                {
                    if (!toDelete.Contains(go)) toDelete.Add(go);
                }
            }
        }

        foreach (var go in toDelete)
        {
            if (go != null)
            {
                Debug.Log($"[DeleteUnusedClutter] Menghapus objek tidak terpakai: {go.name} di posisi {go.transform.position}");
                Undo.DestroyObjectImmediate(go);
                deletedCount++;
            }
        }

        // Simpan Scene
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);

        Debug.Log($"[DeleteUnusedClutter] === SUKSES: {deletedCount} OBJEK TIDAK TERPAKAI TELAH DIHAPUS DARI SCN_LabK3_01 ===");

        if (showDialog)
        {
            EditorUtility.DisplayDialog("Sukses Bersihkan Objek", 
                $"Berhasil menghapus {deletedCount} objek yang tidak terpakai dari scene SCN_LabK3_01!\n\n" +
                "Objek yang telah dihapus:\n" +
                "- Meja packing (PackingTable) & garis lantai putih\n" +
                "- Hand pallet jack (dongkrak kuning)\n" +
                "- Rambu segitiga kuning (A-Frame Sign)\n" +
                "- Garis kuning putus-putus di lantai tengah lorong\n" +
                "- Tumpukan palet & kardus yang tidak terpakai\n\n" +
                "Barang-barang utama Anda (mesin, rak lemari barang, forklift, dll) tetap aman dan utuh.", "OK");
        }
    }
}
#endif
