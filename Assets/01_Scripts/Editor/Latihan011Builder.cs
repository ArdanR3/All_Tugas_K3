#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

// [InitializeOnLoad] -- disabled auto execution on startup
public static class Latihan011Builder
{
    private const string SCENE_PATH = "Assets/00_Scenes/latihan011.unity";
    private const string MAT_DIR = "Assets/10_K3_Simulation/Materials";
    private const string BUILD_FLAG = "Trigger_Build_Latihan011_v1";
    /* Auto-run constructor disabled to prevent scene modification on domain reload


    //     static Latihan011Builder()
    //     {
    //         EditorApplication.delayCall += () =>
    //         {
    //             if (EditorPrefs.GetBool(BUILD_FLAG, true))
    //             {
    //                 EditorPrefs.SetBool(BUILD_FLAG, false);
    //                 BuildLatihanScene();
    //             }
    //         };
    //     }
    */

    [MenuItem("Tools/K3/2. Bangun Scene Baru 'latihan011' (Diorama K3 Lengkap)", false, 20)]
    public static void BuildLatihanScene()
    {
        Debug.Log("[Latihan011] === Memulai Pembuatan Scene Baru 'latihan011' Sesuai Diorama Referensi ===");

        // 1. Buat scene baru kosong tanpa mengubah SCN_LabK3
        var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 2. Siapkan Material & Tekstur K3
        EnsureTexturesAndMaterials();

        // 3. Bangun Struktur Lingkungan (Lantai & Dinding L-Shape)
        GameObject envRoot = new GameObject("ENVIRONMENT");
        BuildArchitecture(envRoot);

        // 4. Bangun Ruang 1: Workshop Pemesinan Bay 1 (Kiri Atas)
        GameObject bay1Root = new GameObject("RUANG 1 - PEMESINAN BAY 1");
        BuildMachiningBay1(bay1Root);

        // 5. Bangun Ruang 2: Workshop Pemesinan Bay 2 (Kiri Bawah)
        GameObject bay2Root = new GameObject("RUANG 2 - PEMESINAN BAY 2");
        BuildMachiningBay2(bay2Root);

        // 6. Bangun Ruang 3: Gudang Logistik, Pengelasan & Fasilitas K3 (Kanan)
        GameObject bay3Root = new GameObject("RUANG 3 - GUDANG & LOGISTIK K3");
        BuildLogisticsAndSafetyHall(bay3Root);

        // 7. Bangun Pencahayaan & Kamera
        GameObject lightRoot = new GameObject("LIGHTING");
        BuildLighting(lightRoot);

        GameObject playerRoot = new GameObject("PLAYER & CAMERAS");
        BuildCamerasAndPlayer(playerRoot);

        // 8. Simpan ke Assets/00_Scenes/latihan011.unity
        bool saved = EditorSceneManager.SaveScene(newScene, SCENE_PATH);
        if (saved)
        {
            Debug.Log($"[Latihan011] === SUKSES: Scene baru berhasil disimpan di '{SCENE_PATH}' ===");
        }
        else
        {
            Debug.LogError($"[Latihan011] Gagal menyimpan scene ke '{SCENE_PATH}'");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void EnsureTexturesAndMaterials()
    {
        if (!AssetDatabase.IsValidFolder("Assets/10_K3_Simulation"))
            AssetDatabase.CreateFolder("Assets", "10_K3_Simulation");
        if (!AssetDatabase.IsValidFolder(MAT_DIR))
            AssetDatabase.CreateFolder("Assets/10_K3_Simulation", "Materials");

        // 1. Tekstur Titik Kumpul (Assembly Point)
        string apTexPath = $"{MAT_DIR}/IMG_AssemblyPoint.png";
        if (!File.Exists(apTexPath))
        {
            Texture2D tex = CreateAssemblyPointTexture();
            File.WriteAllBytes(apTexPath, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(apTexPath);
        }

        // 2. Tekstur Jalur Evakuasi dengan Panah (Evacuation Walkway)
        string walkTexPath = $"{MAT_DIR}/IMG_Evacuation_Walkway.png";
        if (!File.Exists(walkTexPath))
        {
            Texture2D tex = CreateWalkwayTexture();
            File.WriteAllBytes(walkTexPath, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(walkTexPath);
        }

        // 3. Tekstur Peringatan Forklift
        string fkTexPath = $"{MAT_DIR}/IMG_ForkliftCaution.png";
        if (!File.Exists(fkTexPath))
        {
            Texture2D tex = CreateForkliftCautionTexture();
            File.WriteAllBytes(fkTexPath, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(fkTexPath);
        }

        // 4. Tekstur Garis Hazard Kuning Hitam
        string hzTexPath = $"{MAT_DIR}/IMG_Hazard_Stripes.png";
        if (!File.Exists(hzTexPath))
        {
            Texture2D tex = CreateHazardStripeTexture();
            File.WriteAllBytes(hzTexPath, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(hzTexPath);
        }

        // Buat Material URP
        CreateLitMaterial("MAT_Floor_Epoxy_Grey", new Color(0.72f, 0.74f, 0.76f));
        CreateLitMaterial("MAT_Wall_Industrial_Light", new Color(0.85f, 0.86f, 0.88f));
        CreateLitMaterial("MAT_Wall_Interior_Dividing", new Color(0.80f, 0.82f, 0.84f));
        CreateLitMaterial("MAT_Welding_Partition", new Color(0.12f, 0.28f, 0.18f));
        CreateLitMaterial("MAT_Lockers_Blue", new Color(0.10f, 0.35f, 0.65f));
        CreateLitMaterial("MAT_Gas_Oxygen_Blue", new Color(0.05f, 0.40f, 0.80f));
        CreateLitMaterial("MAT_Gas_Acetylene_Yellow", new Color(0.95f, 0.65f, 0.05f));
        CreateLitMaterial("MAT_Safety_Yellow_Solid", new Color(0.98f, 0.78f, 0.05f));

        CreateTexturedMaterial("MAT_Assembly_Point_Floor", apTexPath);
        CreateTexturedMaterial("MAT_Evacuation_Walkway", walkTexPath);
        CreateTexturedMaterial("MAT_Forklift_Caution_Sign", fkTexPath);
        CreateTexturedMaterial("MAT_Hazard_Welding_Border", hzTexPath);
    }

    private static void BuildArchitecture(GameObject parent)
    {
        Material floorMat = LoadMaterial("MAT_Floor_Epoxy_Grey");
        Material wallMat = LoadMaterial("MAT_Wall_Industrial_Light");
        Material divMat = LoadMaterial("MAT_Wall_Interior_Dividing");

        // Lantai Sayap Kiri (Ruang Pemesinan 1 & 2): 16m x 16m pada X = -8, Z = 0
        GameObject floorLeft = CreatePlane("Floor_Machining_Wing", new Vector3(-8f, 0f, 0f), new Vector3(1.6f, 1f, 1.6f), floorMat, parent);

        // Lantai Sayap Kanan (Gudang & Logistik): 18m x 22m pada X = 9f, Z = -3f
        GameObject floorRight = CreatePlane("Floor_Logistics_Hall", new Vector3(9f, 0f, -3f), new Vector3(1.8f, 1f, 2.2f), floorMat, parent);

        // Dinding Luar Sayap Kiri
        CreateWall("Wall_Left_West", new Vector3(-16f, 1.5f, 0f), new Vector3(0.3f, 3f, 16f), wallMat, parent);
        CreateWall("Wall_Left_North", new Vector3(-8f, 1.5f, 8f), new Vector3(16f, 3f, 0.3f), wallMat, parent);
        CreateWall("Wall_Left_South", new Vector3(-8f, 1.5f, -8f), new Vector3(16f, 3f, 0.3f), wallMat, parent);

        // Dinding Pemisah Antara Ruang 1 dan Ruang 2 (Horizontal di Z = 0) dengan celah lorong
        CreateWall("Wall_Divider_Left_PartA", new Vector3(-11.5f, 1.5f, 0f), new Vector3(9f, 3f, 0.3f), divMat, parent);
        CreateWall("Wall_Divider_Left_PartB", new Vector3(-3f, 1.5f, 0f), new Vector3(4f, 3f, 0.3f), divMat, parent);
        // Celah lorong di X = -6 s.d. -4 untuk akses K3

        // Dinding Luar Sayap Kanan (Gudang Logistik)
        CreateWall("Wall_Right_North", new Vector3(9f, 1.5f, 8f), new Vector3(18f, 3f, 0.3f), wallMat, parent);
        CreateWall("Wall_Right_East", new Vector3(18f, 1.5f, -3f), new Vector3(0.3f, 3f, 22f), wallMat, parent);
        CreateWall("Wall_Right_South", new Vector3(9f, 1.5f, -14f), new Vector3(18f, 3f, 0.3f), wallMat, parent);

        // Dinding Pembatas Antara Sayap Kiri dan Sayap Kanan (dengan bukaan pintu besar selebar 3.5m)
        CreateWall("Wall_Interconnecting_Top", new Vector3(0f, 1.5f, 5f), new Vector3(0.3f, 3f, 6f), divMat, parent);
        CreateWall("Wall_Interconnecting_Bottom", new Vector3(0f, 1.5f, -5.5f), new Vector3(0.3f, 3f, 5f), divMat, parent);
        // Bukaan lorong tembus dari X = 0, Z = -3 s.d. +2
    }

    private static void BuildMachiningBay1(GameObject parent)
    {
        // Jalur Evakuasi Hijau di tengah Bay 1 (Z = 4)
        Material walkMat = LoadMaterial("MAT_Evacuation_Walkway");
        CreatePlane("Walkway_Bay1", new Vector3(-8f, 0.005f, 4f), new Vector3(1.4f, 1f, 0.2f), walkMat, parent);

        // Mesin-mesin Industri di sisi Utara (Z = 6.2)
        InstantiatePrefab("Machine_7", new Vector3(-13f, 0f, 6.2f), Quaternion.identity, parent);
        InstantiatePrefab("Machine_4", new Vector3(-8.5f, 0f, 6.2f), Quaternion.identity, parent);
        InstantiatePrefab("Machine_3", new Vector3(-4f, 0f, 6.2f), Quaternion.identity, parent);

        // Meja Kerja & Drum di sisi Selatan Bay 1 (Z = 1.8)
        InstantiatePrefab("PackingTable", new Vector3(-12.5f, 0f, 1.8f), Quaternion.identity, parent);
        InstantiatePrefab("PackingTable", new Vector3(-5f, 0f, 1.8f), Quaternion.identity, parent);

        // Drum Kimia
        CreateChemicalDrum("Drum_Oil_Blue_Bay1", new Vector3(-2f, 0.5f, 6.2f), LoadMaterial("MAT_OilBarrel_Blue"), parent);

        // APAR & Rambu K3 di dinding
        InstantiatePrefab("Fire Extinguisher", new Vector3(-15.8f, 1.2f, 7.5f), Quaternion.Euler(0, 90, 0), parent);
        InstantiatePrefab("FirstAidKit_Green", new Vector3(-15.8f, 1.5f, 4f), Quaternion.Euler(0, 90, 0), parent);

        // Spanduk Dinding K3
        CreateWallBanner("Banner_Slogan_K3_Bay1", new Vector3(-8.5f, 2.5f, 7.8f), new Vector3(5f, 0.8f, 0.05f),
            "UTAMAKAN KESELAMATAN KERJA", new Color(0f, 0.5f, 0.2f), parent);
    }

    private static void BuildMachiningBay2(GameObject parent)
    {
        // Jalur Evakuasi Hijau di tengah Bay 2 (Z = -4)
        Material walkMat = LoadMaterial("MAT_Evacuation_Walkway");
        CreatePlane("Walkway_Bay2", new Vector3(-8f, 0.005f, -4f), new Vector3(1.4f, 1f, 0.2f), walkMat, parent);

        // Mesin-mesin Industri di sisi Selatan (Z = -6.2)
        InstantiatePrefab("Machine_2", new Vector3(-13.5f, 0f, -6.2f), Quaternion.identity, parent);
        InstantiatePrefab("Machine_6", new Vector3(-8.5f, 0f, -6.2f), Quaternion.identity, parent);
        InstantiatePrefab("Machine_1", new Vector3(-4f, 0f, -6.2f), Quaternion.identity, parent);

        // Barikade Kuning Pengaman Mesin 6 (Robot Arm)
        Material yellowMat = LoadMaterial("MAT_Safety_Yellow_Solid");
        CreateSafetyRailing("Railing_Machine6_Front", new Vector3(-8.5f, 0.5f, -4.9f), new Vector3(3.2f, 1f, 0.08f), yellowMat, parent);
        CreateSafetyRailing("Railing_Machine6_Left", new Vector3(-10.1f, 0.5f, -5.6f), new Vector3(0.08f, 1f, 1.4f), yellowMat, parent);
        CreateSafetyRailing("Railing_Machine6_Right", new Vector3(-6.9f, 0.5f, -5.6f), new Vector3(0.08f, 1f, 1.4f), yellowMat, parent);

        // Meja Kerja di sisi Utara Bay 2 (Z = -1.8)
        InstantiatePrefab("PackingTable", new Vector3(-12.5f, 0f, -1.8f), Quaternion.identity, parent);
        InstantiatePrefab("PackingTable", new Vector3(-4.5f, 0f, -1.8f), Quaternion.identity, parent);

        // APAR di dinding
        InstantiatePrefab("Fire Extinguisher", new Vector3(-15.8f, 1.2f, -4f), Quaternion.Euler(0, 90, 0), parent);

        // Spanduk Dinding K3
        CreateWallBanner("Banner_Slogan_K3_Bay2", new Vector3(-8.5f, 2.5f, -0.2f), new Vector3(5f, 0.8f, 0.05f),
            "KERJA AMAN PRODUKTIVITAS MENINGKAT", new Color(0f, 0.45f, 0.2f), parent);
    }

    private static void BuildLogisticsAndSafetyHall(GameObject parent)
    {
        // ----------------- 1. AREA GUDANG & FORKLIFT (ATAS: Z = 0 s.d. 8) -----------------
        GameObject whGroup = new GameObject("ZONA 1 - GUDANG & FORKLIFT");
        whGroup.transform.SetParent(parent.transform);

        // Rak Palet Bertingkat di Dinding Utara & Timur
        InstantiatePrefab("PalletRack_StartBay", new Vector3(3f, 0f, 6.8f), Quaternion.identity, whGroup);
        InstantiatePrefab("PalletRack_AddOnBay", new Vector3(5.7f, 0f, 6.8f), Quaternion.identity, whGroup);
        InstantiatePrefab("PalletRack_StartBay", new Vector3(11f, 0f, 6.8f), Quaternion.identity, whGroup);
        InstantiatePrefab("PalletRack_AddOnBay", new Vector3(13.7f, 0f, 6.8f), Quaternion.identity, whGroup);

        // Palet Kayu & Kardus di Lantai & di Rak
        InstantiatePrefab("EuroPallet", new Vector3(3f, 0.1f, 4.5f), Quaternion.identity, whGroup);
        InstantiatePrefab("Carton_01", new Vector3(3f, 0.35f, 4.5f), Quaternion.identity, whGroup);
        InstantiatePrefab("EuroPallet", new Vector3(15.5f, 0.1f, 4.5f), Quaternion.identity, whGroup);
        InstantiatePrefab("Carton_02", new Vector3(15.5f, 0.35f, 4.5f), Quaternion.identity, whGroup);
        InstantiatePrefab("Carton_03", new Vector3(15.5f, 0.75f, 4.5f), Quaternion.identity, whGroup);

        // Pelindung Kaki Rak (Post Protectors)
        InstantiatePrefab("RackUprightPostProtector", new Vector3(1.6f, 0f, 6f), Quaternion.identity, whGroup);
        InstantiatePrefab("RackUprightPostProtector", new Vector3(7.1f, 0f, 6f), Quaternion.identity, whGroup);

        // Area Parkir Khusus Forklift (Garis Kotak Kuning)
        Material yellowMat = LoadMaterial("MAT_Safety_Yellow_Solid");
        CreateFloorBoundaryBox("Forklift_Parking_Bay", new Vector3(8.5f, 0.006f, 2.5f), 3.5f, 4.5f, 0.12f, yellowMat, whGroup);

        // Forklift Kuning Terparkir di Kotak
        InstantiatePrefab("Forklift", new Vector3(8.5f, 0f, 2.5f), Quaternion.Euler(0, 180, 0), whGroup);

        // Hand Pallet Jack di dekat palet
        InstantiatePrefab("HandPalletJack", new Vector3(13.5f, 0f, 3f), Quaternion.Euler(0, -45, 0), whGroup);

        // Spanduk Rambu Awas Forklift di dinding
        CreateWallSign("Sign_Awas_Forklift", new Vector3(8.5f, 2.5f, 7.8f), new Vector3(3.2f, 1.2f, 0.05f),
            LoadMaterial("MAT_Forklift_Caution_Sign"), whGroup);

        // ----------------- 2. AREA PENGELASAN / FABRIKASI (TENGAH: Z = -4 s.d. 0) -----------------
        GameObject weldGroup = new GameObject("ZONA 2 - AREA PENGELASAN & MEJA KERJA");
        weldGroup.transform.SetParent(parent.transform);

        // Garis Pembatas Bahaya Belang Kuning-Hitam di Lantai
        Material hazardMat = LoadMaterial("MAT_Hazard_Welding_Border");
        CreateFloorBoundaryBox("Welding_Hazard_Boundary", new Vector3(7f, 0.006f, -3.5f), 6f, 5f, 0.15f, hazardMat, weldGroup);

        // Meja Kerja Pengelasan Besi di dalam kotak
        InstantiatePrefab("PackingTable", new Vector3(5.5f, 0f, -3.5f), Quaternion.identity, weldGroup);
        InstantiatePrefab("PackingTable", new Vector3(8.5f, 0f, -3.5f), Quaternion.identity, weldGroup);

        // Sekat / Tirai Proteksi Las (Welding Screens)
        Material weldScreenMat = LoadMaterial("MAT_Welding_Partition");
        CreateWeldingScreen("WeldingScreen_Back", new Vector3(7f, 1f, -1.2f), new Vector3(5.5f, 2f, 0.08f), weldScreenMat, weldGroup);
        CreateWeldingScreen("WeldingScreen_Left", new Vector3(4.2f, 1f, -3.5f), new Vector3(0.08f, 2f, 4.5f), weldScreenMat, weldGroup);

        // Tabung Silinder Gas Oksigen (Biru) & Asetilin (Kuning)
        CreateGasCylinder("Cylinder_Oxygen_1", new Vector3(9.5f, 0.85f, -1.5f), LoadMaterial("MAT_Gas_Oxygen_Blue"), weldGroup);
        CreateGasCylinder("Cylinder_Acetylene_1", new Vector3(9.9f, 0.75f, -1.5f), LoadMaterial("MAT_Gas_Acetylene_Yellow"), weldGroup);

        // ----------------- 3. STASIUN APD, LOKER & TITIK KUMPUL (BAWAH: Z = -7 s.d. -14) -----------------
        GameObject safetyGroup = new GameObject("ZONA 3 - STASIUN APD, LOKER & EVAKUASI");
        safetyGroup.transform.SetParent(parent.transform);

        // Deretan Loker Karyawan Biru Metalik
        Material lockerMat = LoadMaterial("MAT_Lockers_Blue");
        for (int i = 0; i < 6; i++)
        {
            CreateLockerUnit($"Locker_Unit_{i + 1}", new Vector3(3f + (i * 0.9f), 1f, -13.5f), lockerMat, safetyGroup);
        }

        // Papan / Spanduk "GUNAKAN APD" di atas Loker
        CreateWallBanner("Banner_Gunakan_APD", new Vector3(5.2f, 2.3f, -13.8f), new Vector3(5f, 0.6f, 0.05f),
            "WAJIB GUNAKAN APD LENGKAP: HELM, KACAMATA, SARUNG TANGAN, SEPATU", new Color(0.05f, 0.35f, 0.75f), safetyGroup);

        // Kotak P3K & APAR Dinding di samping Loker
        InstantiatePrefab("FirstAidKit_Green", new Vector3(9f, 1.5f, -13.8f), Quaternion.identity, safetyGroup);
        InstantiatePrefab("Fire Extinguisher", new Vector3(10f, 1.2f, -13.8f), Quaternion.identity, safetyGroup);

        // Marka Lantai Resmi "TITIK KUMPUL / ASSEMBLY POINT" (Kotak Hijau Besar)
        Material apMat = LoadMaterial("MAT_Assembly_Point_Floor");
        CreatePlane("Floor_Assembly_Point", new Vector3(6f, 0.007f, -8.5f), new Vector3(0.35f, 1f, 0.35f), apMat, safetyGroup);

        // Jalur Koridor Pejalan Kaki Hijau Menuju Pintu Darurat
        Material walkMat = LoadMaterial("MAT_Evacuation_Walkway");
        CreatePlane("Walkway_To_Exit", new Vector3(14.5f, 0.005f, -8.5f), new Vector3(0.3f, 1f, 0.9f), walkMat, safetyGroup);

        // Rambu Pintu Darurat (EXIT / KELUAR) di Dinding Timur
        CreateDoorwayExitSign("Sign_Emergency_Exit_Door", new Vector3(17.8f, 2.2f, -8.5f), safetyGroup);
    }

    private static void BuildLighting(GameObject parent)
    {
        // 1. Directional Light Utama
        GameObject dirLightGo = new GameObject("Directional Light");
        dirLightGo.transform.SetParent(parent.transform);
        Light dirLight = dirLightGo.AddComponent<Light>();
        dirLight.type = LightType.Directional;
        dirLight.intensity = 0.95f;
        dirLight.color = new Color(0.98f, 0.96f, 0.92f);
        dirLight.shadows = LightShadows.Soft;
        dirLightGo.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

        // 2. Lampu Darurat Hijau di atas Pintu Exit
        GameObject exitLightGo = new GameObject("PointLight_Exit_Green");
        exitLightGo.transform.SetParent(parent.transform);
        exitLightGo.transform.position = new Vector3(17.5f, 2.5f, -8.5f);
        Light exitLight = exitLightGo.AddComponent<Light>();
        exitLight.type = LightType.Point;
        exitLight.color = new Color(0.1f, 1f, 0.3f);
        exitLight.range = 7f;
        exitLight.intensity = 2f;

        // 3. Lampu Darurat Merah di atas APAR
        GameObject aparLightGo = new GameObject("PointLight_APAR_Red");
        aparLightGo.transform.SetParent(parent.transform);
        aparLightGo.transform.position = new Vector3(10f, 2.2f, -13.5f);
        Light aparLight = aparLightGo.AddComponent<Light>();
        aparLight.type = LightType.Point;
        aparLight.color = new Color(1f, 0.2f, 0.1f);
        aparLight.range = 5f;
        aparLight.intensity = 1.8f;
    }

    private static void BuildCamerasAndPlayer(GameObject parent)
    {
        // 1. Kamera Isometrik Overview Diorama (Persis Sudut Pandang Poster Referensi)
        GameObject isoCamGo = new GameObject("Overview_Diorama_Camera");
        isoCamGo.transform.SetParent(parent.transform);
        isoCamGo.transform.position = new Vector3(25f, 22f, 12f);
        isoCamGo.transform.rotation = Quaternion.Euler(38f, -145f, 0f);
        Camera isoCam = isoCamGo.AddComponent<Camera>();
        isoCam.fieldOfView = 48f;
        isoCam.nearClipPlane = 0.3f;
        isoCam.farClipPlane = 200f;

        // 2. Player FPS untuk Simulasi Jalan & Inspeksi K3
        GameObject player = new GameObject("K3_Player");
        player.transform.SetParent(parent.transform);
        player.transform.position = new Vector3(6f, 0.1f, -8.5f); // Mulai di Titik Kumpul

        CharacterController cc = player.AddComponent<CharacterController>();
        cc.height = 1.7f;
        cc.radius = 0.25f;
        cc.center = new Vector3(0, 0.85f, 0);

        player.AddComponent<K3PlayerController>();

        // Kamera Mata Pemain FPS
        GameObject fpsCamGo = new GameObject("Main Camera");
        fpsCamGo.tag = "MainCamera";
        fpsCamGo.transform.SetParent(player.transform);
        fpsCamGo.transform.localPosition = new Vector3(0, 1.6f, 0);
        Camera fpsCam = fpsCamGo.AddComponent<Camera>();
        fpsCam.fieldOfView = 60f;
        fpsCamGo.AddComponent<AudioListener>();
    }

    // =================================== HELPER METHODS ===================================

    private static GameObject CreatePlane(string name, Vector3 pos, Vector3 scale, Material mat, GameObject parent)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Plane);
        go.name = name;
        go.transform.position = pos;
        go.transform.localScale = scale;
        if (mat != null) go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        if (parent != null) go.transform.SetParent(parent.transform);
        return go;
    }

    private static GameObject CreateWall(string name, Vector3 pos, Vector3 scale, Material mat, GameObject parent)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.position = pos;
        go.transform.localScale = scale;
        if (mat != null) go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        if (parent != null) go.transform.SetParent(parent.transform);
        return go;
    }

    private static void CreateFloorBoundaryBox(string name, Vector3 center, float widthX, float lengthZ, float borderThick, Material mat, GameObject parent)
    {
        GameObject boxRoot = new GameObject(name);
        boxRoot.transform.SetParent(parent.transform);

        // 4 border strips
        CreateWall("Border_North", new Vector3(center.x, center.y, center.z + lengthZ / 2f), new Vector3(widthX, 0.01f, borderThick), mat, boxRoot);
        CreateWall("Border_South", new Vector3(center.x, center.y, center.z - lengthZ / 2f), new Vector3(widthX, 0.01f, borderThick), mat, boxRoot);
        CreateWall("Border_East", new Vector3(center.x + widthX / 2f, center.y, center.z), new Vector3(borderThick, 0.01f, lengthZ), mat, boxRoot);
        CreateWall("Border_West", new Vector3(center.x - widthX / 2f, center.y, center.z), new Vector3(borderThick, 0.01f, lengthZ), mat, boxRoot);
    }

    private static void CreateChemicalDrum(string name, Vector3 pos, Material mat, GameObject parent)
    {
        GameObject drum = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        drum.name = name;
        drum.transform.position = pos;
        drum.transform.localScale = new Vector3(0.6f, 0.5f, 0.6f);
        if (mat != null) drum.GetComponent<MeshRenderer>().sharedMaterial = mat;
        if (parent != null) drum.transform.SetParent(parent.transform);
    }

    private static void CreateGasCylinder(string name, Vector3 pos, Material mat, GameObject parent)
    {
        GameObject cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cyl.name = name;
        cyl.transform.position = pos;
        cyl.transform.localScale = new Vector3(0.25f, 0.8f, 0.25f);
        if (mat != null) cyl.GetComponent<MeshRenderer>().sharedMaterial = mat;
        if (parent != null) cyl.transform.SetParent(parent.transform);
    }

    private static void CreateWeldingScreen(string name, Vector3 pos, Vector3 scale, Material mat, GameObject parent)
    {
        GameObject screen = GameObject.CreatePrimitive(PrimitiveType.Cube);
        screen.name = name;
        screen.transform.position = pos;
        screen.transform.localScale = scale;
        if (mat != null) screen.GetComponent<MeshRenderer>().sharedMaterial = mat;
        if (parent != null) screen.transform.SetParent(parent.transform);
    }

    private static void CreateLockerUnit(string name, Vector3 pos, Material mat, GameObject parent)
    {
        GameObject locker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        locker.name = name;
        locker.transform.position = pos;
        locker.transform.localScale = new Vector3(0.8f, 1.9f, 0.5f);
        if (mat != null) locker.GetComponent<MeshRenderer>().sharedMaterial = mat;
        if (parent != null) locker.transform.SetParent(parent.transform);
    }

    private static void CreateSafetyRailing(string name, Vector3 pos, Vector3 scale, Material mat, GameObject parent)
    {
        GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bar.name = name;
        bar.transform.position = pos;
        bar.transform.localScale = scale;
        if (mat != null) bar.GetComponent<MeshRenderer>().sharedMaterial = mat;
        if (parent != null) bar.transform.SetParent(parent.transform);
    }

    private static void CreateWallBanner(string name, Vector3 pos, Vector3 scale, string text, Color bgColor, GameObject parent)
    {
        GameObject banner = GameObject.CreatePrimitive(PrimitiveType.Cube);
        banner.name = name;
        banner.transform.position = pos;
        banner.transform.localScale = scale;

        Material bannerMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        bannerMat.color = bgColor;
        banner.GetComponent<MeshRenderer>().sharedMaterial = bannerMat;

        if (parent != null) banner.transform.SetParent(parent.transform);
    }

    private static void CreateWallSign(string name, Vector3 pos, Vector3 scale, Material mat, GameObject parent)
    {
        GameObject sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sign.name = name;
        sign.transform.position = pos;
        sign.transform.localScale = scale;
        if (mat != null) sign.GetComponent<MeshRenderer>().sharedMaterial = mat;
        if (parent != null) sign.transform.SetParent(parent.transform);
    }

    private static void CreateDoorwayExitSign(string name, Vector3 pos, GameObject parent)
    {
        GameObject exitSign = GameObject.CreatePrimitive(PrimitiveType.Cube);
        exitSign.name = name;
        exitSign.transform.position = pos;
        exitSign.transform.localScale = new Vector3(0.08f, 0.4f, 1f);

        Material exitMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        exitMat.color = new Color(0f, 0.65f, 0.25f);
        exitSign.GetComponent<MeshRenderer>().sharedMaterial = exitMat;

        if (parent != null) exitSign.transform.SetParent(parent.transform);
    }

    private static void InstantiatePrefab(string prefabName, Vector3 pos, Quaternion rot, GameObject parent)
    {
        string[] guids = AssetDatabase.FindAssets($"{prefabName} t:Prefab");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.transform.position = pos;
                instance.transform.rotation = rot;
                if (parent != null) instance.transform.SetParent(parent.transform);
                return;
            }
        }
        Debug.LogWarning($"[Latihan011] Prefab '{prefabName}' tidak ditemukan.");
    }

    private static Material LoadMaterial(string matName)
    {
        string[] guids = AssetDatabase.FindAssets($"{matName} t:Material");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<Material>(path);
        }
        return null;
    }

    private static void CreateLitMaterial(string matName, Color col)
    {
        string path = $"{MAT_DIR}/{matName}.mat";
        if (File.Exists(path)) return;
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = col;
        mat.SetFloat("_Smoothness", 0.35f);
        AssetDatabase.CreateAsset(mat, path);
    }

    private static void CreateTexturedMaterial(string matName, string texPath)
    {
        string path = $"{MAT_DIR}/{matName}.mat";
        if (File.Exists(path)) return;
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if (tex != null) mat.mainTexture = tex;
        mat.SetFloat("_Smoothness", 0.4f);
        AssetDatabase.CreateAsset(mat, path);
    }

    // =================================== TEKSTUR PROCEDURAL ===================================

    private static Texture2D CreateAssemblyPointTexture()
    {
        Texture2D tex = new Texture2D(256, 256, TextureFormat.RGBA32, false);
        Color green = new Color(0f, 0.55f, 0.25f, 1f);
        Color white = Color.white;

        for (int y = 0; y < 256; y++)
        {
            for (int x = 0; x < 256; x++)
            {
                // Border putih
                if (x < 12 || x > 243 || y < 12 || y > 243)
                    tex.SetPixel(x, y, white);
                // Panah sudut mengarah ke dalam
                else if ((x < 60 && y < 60 && Mathf.Abs(x - y) < 8) ||
                         (x > 196 && y < 60 && Mathf.Abs((256 - x) - y) < 8) ||
                         (x < 60 && y > 196 && Mathf.Abs(x - (256 - y)) < 8) ||
                         (x > 196 && y > 196 && Mathf.Abs((256 - x) - (256 - y)) < 8))
                    tex.SetPixel(x, y, white);
                // Simbol orang tengah
                else if (Vector2.Distance(new Vector2(x, y), new Vector2(128, 145)) < 16)
                    tex.SetPixel(x, y, white); // Kepala
                else if (x >= 114 && x <= 142 && y >= 85 && y <= 125)
                    tex.SetPixel(x, y, white); // Badan
                else
                    tex.SetPixel(x, y, green);
            }
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D CreateWalkwayTexture()
    {
        Texture2D tex = new Texture2D(512, 128, TextureFormat.RGBA32, false);
        Color green = new Color(0f, 0.52f, 0.22f, 1f);
        Color yellow = new Color(1f, 0.8f, 0f, 1f);
        Color black = Color.black;
        Color white = Color.white;

        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 512; x++)
            {
                // Tepi hazard atas (y > 108) dan bawah (y < 20)
                if (y < 20 || y > 108)
                {
                    bool stripe = ((x + y) / 16) % 2 == 0;
                    tex.SetPixel(x, y, stripe ? yellow : black);
                }
                else
                {
                    // Tengah hijau dengan panah putih
                    int block = x % 128;
                    if (block >= 50 && block <= 90 && y >= 60 && y <= 68)
                        tex.SetPixel(x, y, white); // Batang panah
                    else if (block >= 80 && block <= 100 && Mathf.Abs(y - 64) <= (100 - block))
                        tex.SetPixel(x, y, white); // Ujung panah
                    else
                        tex.SetPixel(x, y, green);
                }
            }
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D CreateForkliftCautionTexture()
    {
        Texture2D tex = new Texture2D(256, 128, TextureFormat.RGBA32, false);
        Color yellow = new Color(1f, 0.82f, 0.05f, 1f);
        Color black = Color.black;

        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 256; x++)
            {
                // Frame border hitam
                if (x < 8 || x > 247 || y < 8 || y > 119)
                    tex.SetPixel(x, y, black);
                // Header hitam atas
                else if (y >= 88 && y <= 112)
                    tex.SetPixel(x, y, black);
                // Siluet Forklift sederhana
                else if (x >= 40 && x <= 110 && y >= 30 && y <= 65)
                    tex.SetPixel(x, y, black); // Badan forklift
                else if (x >= 110 && x <= 118 && y >= 30 && y <= 80)
                    tex.SetPixel(x, y, black); // Tiang garpu
                else if (x >= 118 && x <= 140 && y >= 30 && y <= 38)
                    tex.SetPixel(x, y, black); // Garpu
                else
                    tex.SetPixel(x, y, yellow);
            }
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D CreateHazardStripeTexture()
    {
        Texture2D tex = new Texture2D(256, 64, TextureFormat.RGBA32, false);
        Color yellow = new Color(1f, 0.8f, 0f, 1f);
        Color black = Color.black;

        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 256; x++)
            {
                bool stripe = ((x + y) / 16) % 2 == 0;
                tex.SetPixel(x, y, stripe ? yellow : black);
            }
        }
        tex.Apply();
        return tex;
    }
}
#endif
