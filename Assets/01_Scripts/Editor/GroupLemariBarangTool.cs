#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

// [InitializeOnLoad] -- disabled auto execution on startup
public class GroupLemariBarangTool
{
    /* Auto-run constructor disabled to prevent scene modification on domain reload

    //     static GroupLemariBarangTool()
    //     {
    //         EditorApplication.delayCall += () =>
    //         {
    //             // Auto group saat script pertama kali di-compile jika belum di-group
    //             AutoGroupLemariBarangIfFound();
    //         };
    //     }
    */

    [MenuItem("Tools/K3/Group Lemari Barang (Rak + Palet + Muatan)", false, 1)]
    [MenuItem("GameObject/K3/Group Lemari Barang", false, 10)]
    public static void GroupLemariBarangMenu()
    {
        ExecuteGroupLemariBarang(true);
    }

    [MenuItem("Tools/K3/Group Objek Terpilih (Selection) %g", false, 2)]
    [MenuItem("GameObject/Group Selected %g", false, 0)]
    public static void GroupSelectedObjects()
    {
        Transform[] selected = Selection.transforms;
        if (selected == null || selected.Length == 0)
        {
            EditorUtility.DisplayDialog("Group Objek", "Pilih minimal 1 objek di Hierarchy atau Scene view terlebih dahulu sebelum melakukan Group.", "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Group Selected Objects");

        // Hitung rata-rata posisi / center bounding
        Vector3 center = Vector3.zero;
        float minY = float.MaxValue;
        foreach (var t in selected)
        {
            center += t.position;
            if (t.position.y < minY) minY = t.position.y;
        }
        center /= selected.Length;
        center.y = minY; // Gunakan lantai terendah sebagai pivot

        // Ambil parent dari objek pertama jika ada
        Transform commonParent = selected[0].parent;
        for (int i = 1; i < selected.Length; i++)
        {
            if (selected[i].parent != commonParent)
            {
                commonParent = null; // Tidak punya parent yang sama
                break;
            }
        }

        // Tentukan nama group
        string groupName = "Lemari_Barang";
        if (selected.Length == 1)
        {
            groupName = $"[GROUP] {selected[0].name}";
        }
        else
        {
            foreach (var t in selected)
            {
                if (t.name.ToLower().Contains("rack") || t.name.ToLower().Contains("pallet") || t.name.ToLower().Contains("lemari"))
                {
                    groupName = "Lemari_Barang (Pallet Rack)";
                    break;
                }
            }
        }

        GameObject groupGo = new GameObject(groupName);
        Undo.RegisterCreatedObjectUndo(groupGo, "Create Group Parent");
        groupGo.transform.position = center;
        groupGo.transform.rotation = Quaternion.identity;

        if (commonParent != null)
        {
            Undo.SetTransformParent(groupGo.transform, commonParent, "Set Group Parent");
        }

        foreach (var t in selected)
        {
            Undo.SetTransformParent(t, groupGo.transform, "Reparent to Group");
        }

        Selection.activeGameObject = groupGo;
        EditorGUIUtility.PingObject(groupGo);

        Undo.CollapseUndoOperations(undoGroup);
        Debug.Log($"[GroupTool] Berhasil mengelompokkan {selected.Length} objek ke dalam '{groupName}'!");
    }

    [MenuItem("Tools/K3/Rapikan Objek Logistik Lepas di Root", false, 3)]
    public static void GroupLooseRootLogistics()
    {
        var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        if (!activeScene.isLoaded) return;

        List<Transform> looseLogistics = new List<Transform>();
        foreach (var root in activeScene.GetRootGameObjects())
        {
            string n = root.name.ToLower();
            if (n.StartsWith("carton_") || n.StartsWith("barrel_") || n.StartsWith("palett") || n.StartsWith("europallet") || n == "forklift")
            {
                looseLogistics.Add(root.transform);
            }
        }

        if (looseLogistics.Count == 0)
        {
            Debug.Log("[GroupTool] Tidak ada objek logistik lepas di root scene.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Group Loose Logistics");

        GameObject logGroup = new GameObject("AREA_LOGISTIK_DAN_PENYIMPANAN");
        Undo.RegisterCreatedObjectUndo(logGroup, "Create Logistics Group");
        logGroup.transform.position = looseLogistics[0].position;

        foreach (var t in looseLogistics)
        {
            Undo.SetTransformParent(t, logGroup.transform, "Reparent to Logistics Group");
        }

        Selection.activeGameObject = logGroup;
        EditorGUIUtility.PingObject(logGroup);
        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log($"[GroupTool] Sukses merapikan {looseLogistics.Count} objek logistik lepas ke '{logGroup.name}'!");
    }

    public static bool ExecuteGroupLemariBarang(bool showFeedback)
    {
        var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        if (!activeScene.isLoaded) return false;

        // Cek apakah sudah ada group Lemari_Barang
        foreach (var go in activeScene.GetRootGameObjects())
        {
            if (go.name.Contains("Lemari_Barang") || go.name.Contains("LEMARI_BARANG"))
            {
                if (showFeedback)
                {
                    Selection.activeGameObject = go;
                    EditorGUIUtility.PingObject(go);
                    Debug.Log($"[GroupLemariBarang] Group lemari barang sudah ada: '{go.name}'.");
                }
                return true;
            }
        }

        // Cari elemen-elemen rak lemari barang
        Transform palletRackRun = null;
        Transform palletRackLoads = null;
        Transform postProtectors = null;
        Transform euroPallets = null;
        Transform stackingTotes = null;
        Transform assembledBoxes = null;
        Transform storageAisleFixtures = null;

        List<Transform> standaloneRackParts = new List<Transform>();

        // Cari di seluruh scene
        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (var t in allTransforms)
        {
            if (t.gameObject.scene != activeScene) continue;

            string n = t.name;
            if (n == "PalletRack_Run") palletRackRun = t;
            else if (n == "PalletRack_Loads") palletRackLoads = t;
            else if (n == "PostProtectors") postProtectors = t;
            else if (n == "EuroPallets" && t.parent != null && t.parent.name.Contains("Storage")) euroPallets = t;
            else if (n == "StackingTotes") stackingTotes = t;
            else if (n == "AssembledCardboardBoxes") assembledBoxes = t;
            else if (n == "Storage aisle fixtures") storageAisleFixtures = t;
            else if (n.StartsWith("PalletRack_StartBay") || n.StartsWith("PalletRack_AddOnBay") || n.StartsWith("RackUprightPostProtector"))
            {
                if (!standaloneRackParts.Contains(t)) standaloneRackParts.Add(t);
            }
        }

        List<Transform> toGroup = new List<Transform>();
        if (palletRackRun != null) toGroup.Add(palletRackRun);
        if (palletRackLoads != null) toGroup.Add(palletRackLoads);
        if (postProtectors != null) toGroup.Add(postProtectors);
        if (stackingTotes != null) toGroup.Add(stackingTotes);
        if (assembledBoxes != null) toGroup.Add(assembledBoxes);
        if (euroPallets != null) toGroup.Add(euroPallets);

        if (toGroup.Count == 0 && standaloneRackParts.Count > 0)
        {
            toGroup.AddRange(standaloneRackParts);
        }

        if (toGroup.Count == 0)
        {
            if (showFeedback)
            {
                Debug.LogWarning("[GroupLemariBarang] Tidak menemukan elemen rak lemari barang di scene aktif.");
            }
            return false;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Group Lemari Barang");

        // Buat GameObject Group Utama
        GameObject groupGo = new GameObject("Lemari_Barang (Pallet Rack & Muatan)");
        Undo.RegisterCreatedObjectUndo(groupGo, "Create Lemari Barang Group");

        // Hitung pivot di lantai bawah rak
        Vector3 basePos = toGroup[0].position;
        groupGo.transform.position = new Vector3(basePos.x, 0f, basePos.z);
        groupGo.transform.rotation = Quaternion.identity;

        // Jika toGroup ada di dalam Storage aisle fixtures, taruh groupGo di dalamnya agar struktur hierarki tetap rapi
        Transform targetParent = null;
        if (storageAisleFixtures != null)
        {
            targetParent = storageAisleFixtures;
        }
        else if (toGroup[0].parent != null)
        {
            targetParent = toGroup[0].parent;
        }

        if (targetParent != null)
        {
            Undo.SetTransformParent(groupGo.transform, targetParent, "Set Group Parent");
        }

        // Masukkan semua elemen ke dalam group
        foreach (var elem in toGroup)
        {
            Undo.SetTransformParent(elem, groupGo.transform, "Reparent to Lemari Barang Group");
        }

        Selection.activeGameObject = groupGo;
        EditorGUIUtility.PingObject(groupGo);
        Undo.CollapseUndoOperations(undoGroup);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(activeScene);

        Debug.Log($"[GroupLemariBarang] SUKSES! Seluruh komponen rak lemari barang ({toGroup.Count} kategori elemen) telah digroup ke dalam '{groupGo.name}'.");
        return true;
    }

    private static void AutoGroupLemariBarangIfFound()
    {
        // Jalankan otomatis sekali jika menemukan PalletRack_Run yang belum digroup
        var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        if (activeScene.isLoaded)
        {
            ExecuteGroupLemariBarang(false);
        }
    }

    [MenuItem("Tools/K3/Hapus Monster dari Scene Ini", false, 99)]
    public static void RemoveMonsterFromCurrentScene()
    {
        GameObject monster = GameObject.Find("MonsterRoot");
        if (monster != null)
        {
            Undo.DestroyObjectImmediate(monster);
            Debug.Log("[Fix] Objek MonsterRoot berhasil dihapus dari scene aktif!");
        }
        else
        {
            Debug.Log("[Fix] Tidak ada objek MonsterRoot di scene aktif.");
        }
    }
}
#endif
