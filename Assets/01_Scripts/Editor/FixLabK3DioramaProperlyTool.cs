#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

public class FixLabK3DioramaProperlyTool
{
    [MenuItem("Tools/K3/Perbaiki Total Koridor APD & Assembly Point (Sesuai Gambar 3, 4, 5)", false, 0)]
    public static void ExecuteFix()
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        if (!activeScene.name.Contains("SCN_LabK3_01"))
        {
            string scenePath = "Assets/00_Scenes/SCN_LabK3_01.unity";
            if (File.Exists(scenePath))
            {
                activeScene = EditorSceneManager.OpenScene(scenePath);
            }
            else
            {
                Debug.LogError("[FixLabK3] Scene SCN_LabK3_01.unity tidak ditemukan!");
                return;
            }
        }

        Debug.Log("[FixLabK3] === MEMULAI PERBAIKAN SCN_LabK3_01 SESUAI GAMBAR RUJUKAN ===");

        // 1. HAPUS PREFAB_ABANDONEDHOUSE DARI SCENE
        int deletedHouseCount = 0;
        foreach (var root in activeScene.GetRootGameObjects())
        {
            if (root.name.Contains("AbandonedHouse") || root.name.Contains("Abandoned_House") || root.name.Contains("House_Structure"))
            {
                Undo.DestroyObjectImmediate(root);
                deletedHouseCount++;
            }
        }
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go != null && (go.name.Contains("Prefab_AbandonedHouse") || go.name.Contains("HouseRoot") || go.name == "Abandoned_House"))
            {
                Undo.DestroyObjectImmediate(go);
                deletedHouseCount++;
            }
        }
        Debug.Log($"[FixLabK3] Objek Prefab_AbandonedHouse berhasil dihapus ({deletedHouseCount} objek dibersihkan).");

        // 2. AMBIL ATAU BUAT KATEGORI UTAMA HIERARCHY
        GameObject safetyGroup = GameObject.Find("05_SAFETY_EVACUATION");
        if (safetyGroup == null)
        {
            safetyGroup = new GameObject("05_SAFETY_EVACUATION");
            Undo.RegisterCreatedObjectUndo(safetyGroup, "Create 05_SAFETY_EVACUATION");
        }
        safetyGroup.transform.position = Vector3.zero;
        safetyGroup.transform.rotation = Quaternion.identity;
        safetyGroup.transform.localScale = Vector3.one;

        // 3. HAPUS SEMUA LOKER LAMA, ASSEMBLY POINT LAMA, DAN WALKWAY CORRIDOR LAMA YANG SALAH
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go == null) continue;
            string n = go.name;
            if (n.StartsWith("PPE_Locker_Station") || n.StartsWith("Locker_Station_") || n == "Emergency_Assembly_Point" || n == "Evacuation_Walkway_Corridor")
            {
                Undo.DestroyObjectImmediate(go);
            }
        }

        // 4. LOAD PREFAB APD & ASSEMBLY POINT
        GameObject lockerStationPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/10_K3_Simulation/Prefabs/Prefab_PPE_Locker_Station_Complete.prefab");
        GameObject assemblyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/10_K3_Simulation/Prefabs/Prefab_Assembly_Point.prefab");

        if (lockerStationPrefab == null)
        {
            Debug.LogError("[FixLabK3] Prefab_PPE_Locker_Station_Complete tidak ditemukan!");
            return;
        }
        if (assemblyPrefab == null)
        {
            Debug.LogError("[FixLabK3] Prefab_Assembly_Point tidak ditemukan!");
            return;
        }

        // 5. PASANG 3 STASIUN LOKER APD DI DINDING KORIDOR (Gambar 3, 4, 5)
        // Koordinat tepat di dinding dalam koridor (X = -23.5f), menghadap ke koridor (Euler Y = -90)
        Vector3[] lockerPositions = new Vector3[]
        {
            new Vector3(-23.5f, 0.0f, -1.78f), // Station 1 (Utara / dekat belokan)
            new Vector3(-23.5f, 0.0f, -6.09f), // Station 2 (Tengah)
            new Vector3(-23.5f, 0.0f, -10.10f) // Station 3 (Selatan / dekat assembly point)
        };

        for (int i = 0; i < lockerPositions.Length; i++)
        {
            GameObject ls = PrefabUtility.InstantiatePrefab(lockerStationPrefab, safetyGroup.transform) as GameObject;
            ls.name = i == 0 ? "PPE_Locker_Station (2)" : (i == 1 ? "PPE_Locker_Station (1)" : "PPE_Locker_Station");
            ls.transform.position = lockerPositions[i];
            ls.transform.rotation = Quaternion.Euler(0, -90, 0);
            Debug.Log($"[FixLabK3] Dipasang {ls.name} di {ls.transform.position}");
        }

        // 6. PASANG EMERGENCY ASSEMBLY POINT DI UJUNG KORIDOR (Gambar 3, 4)
        // Koordinat: X = -26.2f, Y = 0.005f, Z = -16.1f, tepat di depan dinding buntu selatan
        GameObject ap = PrefabUtility.InstantiatePrefab(assemblyPrefab, safetyGroup.transform) as GameObject;
        ap.name = "Emergency_Assembly_Point";
        ap.transform.position = new Vector3(-26.2f, 0.005f, -16.1f);
        ap.transform.rotation = Quaternion.Euler(0, 90, 0); // Teks 'ASSEMBLY POINT' menghadap ke arah datangnya jalur hijau!
        Debug.Log($"[FixLabK3] Dipasang Emergency_Assembly_Point di {ap.transform.position}");

        // 7. PASANG JALUR EVAKUASI HIJAU DENGAN GARIS HAZARD (Evacuation_Walkway_Corridor)
        // Jalur hijau lurus selebar 2.4m di lantai koridor dari Z = 1.4f s.d Z = -14.6f (menyambung pas ke Assembly Point)
        GameObject walkwayRoot = new GameObject("Evacuation_Walkway_Corridor");
        walkwayRoot.transform.SetParent(safetyGroup.transform, false);

        Material matStraight = AssetDatabase.LoadAssetAtPath<Material>("Assets/10_K3_Simulation/Materials/MAT_Walkway_Straight.mat");

        // 4 segmen @ 4 meter
        for (int s = 0; s < 4; s++)
        {
            float zCenter = -0.6f - (s * 4.0f); // s=0: -0.6 (covers 1.4 to -2.6), s=1: -4.6, s=2: -8.6, s=3: -12.6 (covers -10.6 to -14.6)
            GameObject seg = CreateFloorQuad("Corridor_Walkway_4m_" + (s + 1), new Vector3(-26.2f, 0.005f, zCenter), new Vector3(2.4f, 4.0f, 1f), matStraight, walkwayRoot.transform);
            seg.transform.rotation = Quaternion.Euler(90, 0, 0);
        }

        // Segmen penghubung di ujung belokan utara (Z = 1.4f s.d 3.8f) menuju area CNC machining
        GameObject turnSeg = CreateFloorQuad("Corridor_Walkway_TurnConnection", new Vector3(-26.2f, 0.005f, 2.6f), new Vector3(2.4f, 2.4f, 1f), matStraight, walkwayRoot.transform);
        turnSeg.transform.rotation = Quaternion.Euler(90, 0, 0);

        // Sambungan siku 90 derajat ke jalur CNC (menghubungkan dari X = -26.2f ke X = -23.8f pada Z = 3.02f)
        GameObject cornerTurn = CreateFloorQuad("Corridor_Corner_To_Machining", new Vector3(-24.5f, 0.0055f, 3.02f), new Vector3(2.4f, 1.6f, 1f), matStraight, walkwayRoot.transform);
        cornerTurn.transform.rotation = Quaternion.Euler(90, 90, 0);

        // 8. PASANG 3 SPOTLIGHT PENERANGAN DI ATAS STASIUN LOKER (Gambar 5)
        GameObject lightRoot = GameObject.Find("05_SAFETY_EVACUATION/Lighting_PPE_Corridor");
        if (lightRoot != null) Undo.DestroyObjectImmediate(lightRoot);
        lightRoot = new GameObject("Lighting_PPE_Corridor");
        lightRoot.transform.SetParent(safetyGroup.transform, false);

        for (int i = 0; i < lockerPositions.Length; i++)
        {
            GameObject spotGO = new GameObject($"SpotLight_Locker_Station_{i + 1}");
            spotGO.transform.SetParent(lightRoot.transform, false);
            spotGO.transform.position = new Vector3(-24.2f, 3.2f, lockerPositions[i].z);
            spotGO.transform.rotation = Quaternion.Euler(55f, 90f, 0f);

            Light light = spotGO.AddComponent<Light>();
            light.type = LightType.Spot;
            light.spotAngle = 75f;
            light.innerSpotAngle = 45f;
            light.range = 7.0f;
            light.intensity = 4.0f;
            light.color = new Color(1.0f, 0.95f, 0.88f); // Warm white
            light.shadows = LightShadows.Soft;
        }

        // 9. SIMPAN PERUBAHAN KE SCENE
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);

        Debug.Log("[FixLabK3] === SUKSES: SCN_LabK3_01 TELAH DIPERBAIKI TOTAL SESUAI GAMBAR RUJUKAN 3, 4, 5 ===");
    }

    private static GameObject CreateFloorQuad(string name, Vector3 pos, Vector3 size, Material mat, Transform parent)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = name;
        quad.transform.SetParent(parent, false);
        quad.transform.position = pos;
        quad.transform.localScale = size;
        if (mat != null)
        {
            quad.GetComponent<Renderer>().sharedMaterial = mat;
        }
        return quad;
    }
}
#endif
