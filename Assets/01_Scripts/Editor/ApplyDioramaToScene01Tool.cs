#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

public class ApplyDioramaToScene01Tool
{

    [MenuItem("Tools/K3/Terapkan Diorama Lengkap ke SCN_LabK3_01", false, 0)]
    public static void ManualApply()
    {
        ApplyDiorama(true);
    }

    public static void ApplyDiorama(bool showPrompt = false)
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        if (!activeScene.name.Contains("SCN_LabK3_01"))
        {
            // Jika scene aktif belum SCN_LabK3_01, buka scene tersebut
            string scenePath = "Assets/00_Scenes/SCN_LabK3_01.unity";
            if (File.Exists(scenePath))
            {
                activeScene = EditorSceneManager.OpenScene(scenePath);
            }
            else
            {
                Debug.LogWarning("[ApplyDiorama] Scene SCN_LabK3_01 tidak ditemukan.");
                return;
            }
        }

        Debug.Log("[ApplyDiorama] === MEMULAI PENYEMPURNAAN DIORAMA K3 DI SCN_LabK3_01 ===");

        // 1. Bersihkan monster jika ada
        GameObject monster = GameObject.Find("MonsterRoot");
        if (monster != null)
        {
            Undo.DestroyObjectImmediate(monster);
            Debug.Log("[ApplyDiorama] MonsterRoot berhasil dibersihkan dari scene.");
        }

        // 1b. Bersihkan objek packing dan sampah yang tidak terpakai
        DeleteUnusedClutterTool.ExecuteCleanup(false);

        // 2. Buat / Ambil Kategori Standar Hierarchy
        GameObject envGroup = GetOrCreateCategory("01_ENVIRONMENT");
        GameObject machineGroup = GetOrCreateCategory("02_MACHINES");
        GameObject warehouseGroup = GetOrCreateCategory("03_WAREHOUSE_LOGISTICS");
        GameObject weldingGroup = GetOrCreateCategory("04_FABRICATION_WELDING");
        GameObject safetyGroup = GetOrCreateCategory("05_SAFETY_EVACUATION");
        GameObject elecGroup = GetOrCreateCategory("06_ELECTRICAL");
        GameObject playerGroup = GetOrCreateCategory("07_PLAYER");
        GameObject lightGroup = GetOrCreateCategory("08_LIGHTING");

        // 3. Rapikan objek lama yang sudah ada ke dalam kategori tanpa mengubah posisi/skalanya
        OrganizeExistingObjects(envGroup, machineGroup, warehouseGroup, safetyGroup, elecGroup, playerGroup, lightGroup);

        // 4. Load Materials yang dibutuhkan
        Material matHazardBorder = LoadMaterial("MAT_Hazard_Welding_Border") ?? LoadMaterial("MAT_Hazard_Stripes");
        Material matWeldingScreen = LoadMaterial("MAT_Welding_Screen");
        Material matGasBlue = LoadMaterial("MAT_Gas_Cylinder_Blue");
        Material matGasYellow = LoadMaterial("MAT_Gas_Cylinder_Yellow");
        Material matSafetyYellow = LoadMaterial("MAT_Safety_Yellow_Solid") ?? LoadMaterial("MAT_SafetyYellow");
        Material matSloganWorkSafe = LoadMaterial("MAT_Slogan_WorkSafe");
        Material matSloganWearPPE = LoadMaterial("MAT_Slogan_WearPPE");
        Material matSloganPriority = LoadMaterial("MAT_Slogan_PrioritySafety");
        Material matCautionForklift = LoadMaterial("MAT_Forklift_Caution_Sign");

        // 5. Tambahkan STASIUN LOKER APD LENGKAP (Gambar 3) jika belum ada
        if (GameObject.Find("PPE_Locker_Station") == null)
        {
            GameObject lockerStationPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/10_K3_Simulation/Prefabs/Prefab_PPE_Locker_Station_Complete.prefab");
            if (lockerStationPrefab != null)
            {
                // Letakkan di dinding depan kanan (antara lorong evakuasi dan assembly point)
                GameObject ls = PrefabUtility.InstantiatePrefab(lockerStationPrefab, safetyGroup.transform) as GameObject;
                ls.name = "PPE_Locker_Station";
                ls.transform.position = new Vector3(0.5f, 0.0f, -14.5f);
                ls.transform.rotation = Quaternion.Euler(0, 0, 0);
                Debug.Log("[ApplyDiorama] Prefab_PPE_Locker_Station_Complete berhasil dipasang!");
            }
        }

        // 6. Tambahkan TITIK KUMPUL / ASSEMBLY POINT (Gambar 1) jika belum ada
        if (GameObject.Find("Emergency_Assembly_Point") == null)
        {
            GameObject apPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/10_K3_Simulation/Prefabs/Prefab_Assembly_Point.prefab");
            if (apPrefab != null)
            {
                // Di area evakuasi luar/depan samping stasiun loker
                GameObject ap = PrefabUtility.InstantiatePrefab(apPrefab, safetyGroup.transform) as GameObject;
                ap.name = "Emergency_Assembly_Point";
                ap.transform.position = new Vector3(5.5f, 0.005f, -14.5f);
                ap.transform.rotation = Quaternion.identity;
                Debug.Log("[ApplyDiorama] Prefab_Assembly_Point berhasil dipasang!");
            }
        }

        // 7. Tambahkan JALUR EVAKUASI HIJAU BERBELOK (Gambar 2)
        SetupEvacuationWalkwaySystem(safetyGroup.transform);

        // 8. Tambahkan ZONA PENGELASAN / WELDING BAY (Di tengah area kanan)
        SetupWeldingStation(weldingGroup.transform, matHazardBorder, matWeldingScreen, matGasBlue, matGasYellow);

        // 9. Tambahkan AREA PARKIR & RAMBU FORKLIFT (Di sekitar forklift)
        SetupForkliftBay(warehouseGroup.transform, matSafetyYellow, matCautionForklift);

        // 10. Tambahkan SPANDUK SLOGAN K3 DI DINDING WORKSHOP (Work Safe, Wear PPE, Priority Safety)
        SetupWorkshopSlogans(safetyGroup.transform, matSloganWorkSafe, matSloganWearPPE, matSloganPriority);

        // 11. Lengkapi isi MACHINE (Mesin CNC dan Milling jika masih kosong)
        SetupMachinesIfEmpty(machineGroup.transform);

        // Simpan Scene
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);

        Debug.Log("[ApplyDiorama] === SUKSES: DIORAMA K3 LENGKAP TELAH DITERAPKAN DI SCN_LabK3_01 ===");

        if (showPrompt)
        {
            EditorUtility.DisplayDialog("Sukses", "Diorama K3 lengkap telah berhasil diterapkan ke SCN_LabK3_01!\n\nSemua barang bawaan Anda tetap utuh, aset baru (Assembly Point, Jalur Belok, Loker APD, Zona Las) telah ditambahkan, dan Hierarchy telah dirapikan secara standar.", "OK");
        }
    }

    private static GameObject GetOrCreateCategory(string catName)
    {
        GameObject go = GameObject.Find(catName);
        if (go == null)
        {
            go = new GameObject(catName);
            Undo.RegisterCreatedObjectUndo(go, "Create Category " + catName);
        }
        go.transform.position = Vector3.zero;
        go.transform.rotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        return go;
    }

    private static void OrganizeExistingObjects(GameObject env, GameObject mach, GameObject wh, GameObject safe, GameObject elec, GameObject ply, GameObject lgt)
    {
        // Pindahkan objek lama dengan tetap menjaga transform dunianya
        MoveToParentIfFound("ENVIRONMENT", env.transform);
        MoveToParentIfFound("MACHINE", mach.transform);
        MoveToParentIfFound("SAFETY", safe.transform);
        MoveToParentIfFound("LIGHTING", lgt.transform);
        MoveToParentIfFound("PLAYER", ply.transform);

        MoveToParentIfFound("electrical", elec.transform);
        MoveToParentIfFound("electrical (1)", elec.transform);

        MoveToParentIfFound("for klift", wh.transform);
        MoveToParentIfFound("box", wh.transform);
    }

    private static void MoveToParentIfFound(string objName, Transform newParent)
    {
        GameObject go = GameObject.Find(objName);
        if (go != null && go.transform.parent != newParent)
        {
            Undo.SetTransformParent(go.transform, newParent, "Organize " + objName);
        }
    }

    private static void SetupEvacuationWalkwaySystem(Transform parent)
    {
        GameObject walkwayRoot = GameObject.Find("Evacuation_Walkway_Corridor");
        if (walkwayRoot != null) return; // Sudah terpasang

        walkwayRoot = new GameObject("Evacuation_Walkway_Corridor");
        walkwayRoot.transform.SetParent(parent, false);

        GameObject str2m = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/10_K3_Simulation/Prefabs/Prefab_Walkway_Straight_2m.prefab");
        GameObject str4m = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/10_K3_Simulation/Prefabs/Prefab_Walkway_Straight_4m.prefab");
        GameObject corner = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/10_K3_Simulation/Prefabs/Prefab_Walkway_Corner_90L.prefab");

        if (str2m == null || corner == null) return;

        // 1. Lorong Machining (X = -21 s.d. -5, Z = -2.0)
        // Jalan lurus melintasi mesin-mesin workshop menuju pintu lorong
        InstantiateChild(str4m, new Vector3(-19.0f, 0.005f, -2.0f), Quaternion.Euler(0, 90, 0), walkwayRoot.transform);
        InstantiateChild(str4m, new Vector3(-15.0f, 0.005f, -2.0f), Quaternion.Euler(0, 90, 0), walkwayRoot.transform);
        InstantiateChild(str4m, new Vector3(-11.0f, 0.005f, -2.0f), Quaternion.Euler(0, 90, 0), walkwayRoot.transform);
        InstantiateChild(str4m, new Vector3(-7.0f, 0.005f, -2.0f), Quaternion.Euler(0, 90, 0), walkwayRoot.transform);
        InstantiateChild(str2m, new Vector3(-4.0f, 0.005f, -2.0f), Quaternion.Euler(0, 90, 0), walkwayRoot.transform);

        // 2. BELOKAN SIKU 90 DERAJAT (Sudut L) di X = -2.0, Z = -2.0
        // Membelokkan jalur dari arah kiri menuju ke arah depan (selatan)
        InstantiateChild(corner, new Vector3(-2.0f, 0.005f, -2.0f), Quaternion.Euler(0, 0, 0), walkwayRoot.transform);

        // 3. Lorong Utama Menuju Stasiun APD & Assembly Point (Z = -3.5 s.d. -13.0)
        InstantiateChild(str4m, new Vector3(-2.0f, 0.005f, -5.5f), Quaternion.identity, walkwayRoot.transform);
        InstantiateChild(str4m, new Vector3(-2.0f, 0.005f, -9.5f), Quaternion.identity, walkwayRoot.transform);
        InstantiateChild(str4m, new Vector3(-2.0f, 0.005f, -13.5f), Quaternion.identity, walkwayRoot.transform);

        // 4. BELOKAN SIKU 90 DERAJAT ke arah Titik Kumpul (Assembly Point) di depan
        InstantiateChild(corner, new Vector3(-2.0f, 0.005f, -15.5f), Quaternion.Euler(0, 270, 0), walkwayRoot.transform);

        // 5. Segmen Lurus Menghubungkan langsung ke pintu Assembly Point
        InstantiateChild(str4m, new Vector3(1.5f, 0.005f, -15.5f), Quaternion.Euler(0, 90, 0), walkwayRoot.transform);

        Debug.Log("[ApplyDiorama] Jalur Evakuasi Hijau Modular Berbelok berhasil dipasang!");
    }

    private static void SetupWeldingStation(Transform parent, Material matHazard, Material matScreen, Material matBlue, Material matYellow)
    {
        if (GameObject.Find("Welding_Fabrication_Station") != null) return;

        GameObject station = new GameObject("Welding_Fabrication_Station");
        station.transform.SetParent(parent, false);
        station.transform.position = new Vector3(0.5f, 0.0f, -7.5f);

        // 1. Kotak Garis Bahaya Kuning-Hitam di Lantai (4.2m x 4.2m)
        CreateFloorQuad("Hazard_Boundary_Floor", new Vector3(0, 0.006f, 0), new Vector3(4.2f, 4.2f, 1), matHazard, station.transform);

        // 2. 4 Partisi Layar Las Hijau Berbentuk Salib / Kuadran (Pelindung Percikan Api)
        CreateBox("Welding_Screen_North", new Vector3(0, 1.0f, 1.0f), new Vector3(0.08f, 2.0f, 2.0f), matScreen, station.transform);
        CreateBox("Welding_Screen_South", new Vector3(0, 1.0f, -1.0f), new Vector3(0.08f, 2.0f, 2.0f), matScreen, station.transform);
        CreateBox("Welding_Screen_East", new Vector3(1.0f, 1.0f, 0), new Vector3(2.0f, 2.0f, 0.08f), matScreen, station.transform);
        CreateBox("Welding_Screen_West", new Vector3(-1.0f, 1.0f, 0), new Vector3(2.0f, 2.0f, 0.08f), matScreen, station.transform);

        // 3. Meja Kerja Fabrikasi Besi di dalam bay
        GameObject tablePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/10_K3_Simulation/WarehousePack/Prefabs/URP/PackingTable.prefab");
        if (tablePrefab != null)
        {
            GameObject tbl = PrefabUtility.InstantiatePrefab(tablePrefab, station.transform) as GameObject;
            tbl.name = "Welding_Workbench";
            tbl.transform.localPosition = new Vector3(1.0f, 0, 1.0f);
            tbl.transform.localRotation = Quaternion.Euler(0, 90, 0);
        }

        // 4. Tabung Gas Oksigen (Biru) & Asetilen (Kuning)
        CreateCylinder("Gas_Cylinder_Oxygen_Blue", new Vector3(-1.4f, 0.8f, 1.4f), new Vector3(0.28f, 0.8f, 0.28f), matBlue, station.transform);
        CreateCylinder("Gas_Cylinder_Acetylene_Yellow", new Vector3(-1.4f, 0.7f, 1.0f), new Vector3(0.28f, 0.7f, 0.28f), matYellow, station.transform);

        Debug.Log("[ApplyDiorama] Zona Pengelasan & Fabrikasi berhasil dibangun!");
    }

    private static void SetupForkliftBay(Transform parent, Material matYellow, Material matCaution)
    {
        if (GameObject.Find("Forklift_Safety_Zone") != null) return;

        GameObject bay = new GameObject("Forklift_Safety_Zone");
        bay.transform.SetParent(parent, false);

        // Cari forklift
        GameObject fk = GameObject.Find("for klift");
        Vector3 fkPos = new Vector3(2.78f, 0.0f, -2.06f);
        if (fk != null)
        {
            fkPos = new Vector3(fk.transform.position.x, 0.0f, fk.transform.position.z);
            // Pastikan forklift menapak lantai dengan benar
            fk.transform.position = new Vector3(fkPos.x, 0.0f, fkPos.z);
        }

        // Garis batas parkir kuning di lantai
        CreateFloorBoundaryBox("Forklift_Parking_Box", fkPos, 3.2f, 4.0f, 0.12f, matYellow, bay.transform);

        // Rambu Kerucut / A-Frame Kuning "Awas Forklift"
        GameObject aframePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/10_K3_Simulation/WarehousePack/Prefabs/URP/BlankAFrameSign.prefab");
        if (aframePrefab != null)
        {
            GameObject s1 = PrefabUtility.InstantiatePrefab(aframePrefab, bay.transform) as GameObject;
            s1.name = "Caution_Sign_Forklift_1";
            s1.transform.position = new Vector3(fkPos.x - 1.8f, 0.0f, fkPos.z - 1.8f);
            s1.transform.rotation = Quaternion.Euler(0, 45, 0);

            GameObject s2 = PrefabUtility.InstantiatePrefab(aframePrefab, bay.transform) as GameObject;
            s2.name = "Caution_Sign_Forklift_2";
            s2.transform.position = new Vector3(fkPos.x + 1.8f, 0.0f, fkPos.z - 1.8f);
            s2.transform.rotation = Quaternion.Euler(0, -45, 0);
        }

        Debug.Log("[ApplyDiorama] Area Parkir & Rambu Keselamatan Forklift berhasil dipasang!");
    }

    private static void SetupWorkshopSlogans(Transform parent, Material matWorkSafe, Material matWearPPE, Material matPriority)
    {
        if (GameObject.Find("Safety_Slogans_Wall") != null) return;

        GameObject slogansRoot = new GameObject("Safety_Slogans_Wall");
        slogansRoot.transform.SetParent(parent, false);

        // 1. WORK SAFE, HOME SAFE (Dinding Barat Workshop)
        CreateWallSign("Slogan_Work_Safe", new Vector3(-28.05f, 2.4f, 5.0f), new Vector3(0.04f, 0.8f, 3.2f), matWorkSafe, slogansRoot.transform);

        // 2. WEAR PPE (Dinding Utara Workshop)
        CreateWallSign("Slogan_Wear_PPE", new Vector3(-20.0f, 2.4f, 11.75f), new Vector3(2.6f, 0.8f, 0.04f), matWearPPE, slogansRoot.transform);

        // 3. PRIORITY: SAFETY (Dinding Utara Sebelah Kanan)
        CreateWallSign("Slogan_Priority_Safety", new Vector3(-13.5f, 2.4f, 11.75f), new Vector3(3.2f, 0.8f, 0.04f), matPriority, slogansRoot.transform);

        Debug.Log("[ApplyDiorama] Spanduk Slogan K3 Dinding Workshop berhasil dipasang!");
    }

    private static void SetupMachinesIfEmpty(Transform machineGroup)
    {
        // 1. Isi CNC jika kosong
        Transform cncTr = machineGroup.Find("cnc");
        if (cncTr != null && cncTr.childCount == 0)
        {
            GameObject cncObj = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/10_K3_Simulation/cnc-machine/source/CNC Machine.obj");
            if (cncObj != null)
            {
                GameObject inst = PrefabUtility.InstantiatePrefab(cncObj, cncTr) as GameObject;
                inst.name = "CNC_Milling_Center";
                inst.transform.localPosition = new Vector3(-13.5f, 0.0f, 9.8f);
                inst.transform.localRotation = Quaternion.Euler(0, 180, 0);
                inst.transform.localScale = Vector3.one * 1.1f;
                Debug.Log("[ApplyDiorama] Mesin CNC berhasil dipasang di slot cnc!");
            }
        }

        // 2. Isi Milling jika kosong
        Transform millTr = machineGroup.Find("milling");
        if (millTr != null && millTr.childCount == 0)
        {
            GameObject m1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/10_K3_Simulation/Industrial_Machine_Models/Prefabs/Machine_1.prefab");
            GameObject m4 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/10_K3_Simulation/Industrial_Machine_Models/Prefabs/Machine_4.prefab");

            if (m1 != null)
            {
                GameObject inst1 = PrefabUtility.InstantiatePrefab(m1, millTr) as GameObject;
                inst1.name = "Milling_Machine_1";
                inst1.transform.localPosition = new Vector3(-18.5f, 0.0f, 9.8f);
                inst1.transform.localRotation = Quaternion.Euler(0, 180, 0);
            }
            if (m4 != null)
            {
                GameObject inst4 = PrefabUtility.InstantiatePrefab(m4, millTr) as GameObject;
                inst4.name = "Milling_Machine_2";
                inst4.transform.localPosition = new Vector3(-23.5f, 0.0f, 9.8f);
                inst4.transform.localRotation = Quaternion.Euler(0, 180, 0);
            }

            // 2 Meja Kerja Logam (Workbenches) di baris tengah workshop
            GameObject tablePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/10_K3_Simulation/WarehousePack/Prefabs/URP/PackingTable.prefab");
            if (tablePrefab != null)
            {
                GameObject t1 = PrefabUtility.InstantiatePrefab(tablePrefab, millTr) as GameObject;
                t1.name = "Machining_Workbench_1";
                t1.transform.localPosition = new Vector3(-16.5f, 0.0f, 4.5f);
                t1.transform.localRotation = Quaternion.Euler(0, 90, 0);

                GameObject t2 = PrefabUtility.InstantiatePrefab(tablePrefab, millTr) as GameObject;
                t2.name = "Machining_Workbench_2";
                t2.transform.localPosition = new Vector3(-21.5f, 0.0f, 4.5f);
                t2.transform.localRotation = Quaternion.Euler(0, 90, 0);
            }
            Debug.Log("[ApplyDiorama] Mesin Milling & Meja Fabrikasi berhasil dipasang di slot milling!");
        }
    }

    private static void InstantiateChild(GameObject prefab, Vector3 pos, Quaternion rot, Transform parent)
    {
        if (prefab == null) return;
        GameObject inst = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
        inst.transform.position = pos;
        inst.transform.rotation = rot;
    }

    private static void CreateBox(string name, Vector3 pos, Vector3 scale, Material mat, Transform parent)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
    }

    private static void CreateCylinder(string name, Vector3 pos, Vector3 scale, Material mat, Transform parent)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
    }

    private static void CreateWallSign(string name, Vector3 pos, Vector3 scale, Material mat, Transform parent)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = scale;
        if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(go.GetComponent<Collider>());
    }

    private static void CreateFloorQuad(string name, Vector3 localPos, Vector3 size, Material mat, Transform parent)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = name;
        quad.transform.SetParent(parent, false);
        quad.transform.localPosition = localPos;
        quad.transform.localRotation = Quaternion.Euler(90, 0, 0);
        quad.transform.localScale = size;
        if (mat != null) quad.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(quad.GetComponent<Collider>());
    }

    private static void CreateFloorBoundaryBox(string name, Vector3 center, float width, float length, float thickness, Material mat, Transform parent)
    {
        GameObject box = new GameObject(name);
        box.transform.SetParent(parent, false);
        box.transform.position = center;

        float halfW = width * 0.5f;
        float halfL = length * 0.5f;

        CreateSubLine("Line_North", new Vector3(0, 0.005f, halfL), new Vector3(width, 0.005f, thickness), mat, box.transform);
        CreateSubLine("Line_South", new Vector3(0, 0.005f, -halfL), new Vector3(width, 0.005f, thickness), mat, box.transform);
        CreateSubLine("Line_East", new Vector3(halfW, 0.005f, 0), new Vector3(thickness, 0.005f, length), mat, box.transform);
        CreateSubLine("Line_West", new Vector3(-halfW, 0.005f, 0), new Vector3(thickness, 0.005f, length), mat, box.transform);
    }

    private static void CreateSubLine(string name, Vector3 localPos, Vector3 scale, Material mat, Transform parent)
    {
        GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
        line.name = name;
        line.transform.SetParent(parent, false);
        line.transform.localPosition = localPos;
        line.transform.localScale = scale;
        if (mat != null) line.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(line.GetComponent<Collider>());
    }

    private static Material LoadMaterial(string matName)
    {
        string path = $"Assets/10_K3_Simulation/Materials/{matName}.mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;

        string[] guids = AssetDatabase.FindAssets(matName + " t:Material");
        if (guids.Length > 0)
        {
            return AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
        return null;
    }
}
#endif
