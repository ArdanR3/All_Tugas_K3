#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Script Otomasi Pembuatan Scene Lab K3 & Aset Terkait
/// Sesuai Modul Praktikum AR/VR ASTRAtech: M01 UNITY 3D DASAR UNTUK SIMULASI K3
/// Menggunakan Asset Resmi: Industrial Machine Models
/// Mahasiswa: Ardan Rizqi P (NIM: 0920240011)
/// </summary>
// [InitializeOnLoad] -- disabled auto execution on startup
public class LabK3Builder
{
    private const string SCENE_DIR = "Assets/00_Scenes";
    private const string SCENE_PATH = "Assets/00_Scenes/SCN_LabK3.unity";
    private const string K3_ROOT = "Assets/10_K3_Simulation";
    private const string MAT_DIR = "Assets/10_K3_Simulation/Materials";
    private const string PREFAB_DIR = "Assets/10_K3_Simulation/Prefabs";
    private const string MACHINE_DIR = "Assets/10_K3_Simulation/Industrial_Machine_Models/Prefabs";
    /* Auto-run constructor disabled to prevent scene modification on domain reload


    //     static LabK3Builder()
    //     {
    //         EditorApplication.delayCall += () =>
    //         {
    //             // Auto build jika diminta atau scene belum ter-update
    //             if (EditorPrefs.GetBool("Rebuild_LabK3_Industrial", true))
    //             {
    //                 EditorPrefs.SetBool("Rebuild_LabK3_Industrial", false);
    //                 BuildCompleteK3Scene();
    //                 CaptureScreenshots();
    //             }
    //         };
    //     }
    */

    [MenuItem("Tools/K3/1. Bangun Scene SCN_LabK3 (Industrial Machines & 3 Safety Lines)", false, 10)]
    public static void BuildCompleteK3Scene()
    {
        Debug.Log("[LabK3Builder] === Memulai Pembuatan SCN_LabK3 dengan Industrial Machine Models & 3 Safety Lines ===");

        EnsureDirectories();
        CreateAllMaterials();
        BuildSceneHierarchy();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[LabK3Builder] === SUKSES: SCN_LabK3 Berhasil Dibangun Sesuai Modul PDF! ===");
    }

    private static void EnsureDirectories()
    {
        if (!AssetDatabase.IsValidFolder(SCENE_DIR)) AssetDatabase.CreateFolder("Assets", "00_Scenes");
        if (!AssetDatabase.IsValidFolder(K3_ROOT)) AssetDatabase.CreateFolder("Assets", "10_K3_Simulation");
        if (!AssetDatabase.IsValidFolder(MAT_DIR)) AssetDatabase.CreateFolder(K3_ROOT, "Materials");
        if (!AssetDatabase.IsValidFolder(PREFAB_DIR)) AssetDatabase.CreateFolder(K3_ROOT, "Prefabs");
    }

    private static Shader GetURPLitShader()
    {
        Shader s = Shader.Find("Universal Render Pipeline/Lit");
        if (s == null) s = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (s == null) s = Shader.Find("Standard");
        return s;
    }

    private static Material GetOrCreateMaterial(string matName, Color color, float metallic = 0f, float smoothness = 0.5f, string textureName = null, Vector2? tiling = null)
    {
        string path = $"{MAT_DIR}/{matName}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(GetURPLitShader());
            mat.name = matName;
            AssetDatabase.CreateAsset(mat, path);
        }

        mat.color = color;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

        if (!string.IsNullOrEmpty(textureName))
        {
            string texPath = $"{MAT_DIR}/{textureName}";
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex != null)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
                else if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);

                if (tiling.HasValue)
                {
                    if (mat.HasProperty("_BaseMap")) mat.SetTextureScale("_BaseMap", tiling.Value);
                    else if (mat.HasProperty("_MainTex")) mat.SetTextureScale("_MainTex", tiling.Value);
                }
            }
        }

        EditorUtility.SetDirty(mat);
        return mat;
    }

    public static void CreateAllMaterials()
    {
        // 1. MAT_SafetyLine & MAT_SafetyFloor sesuai modul halaman 10-13
        // Tekstur IMG_SafetyLine: belang kuning-hitam di tepi atas & bawah, hijau terang di tengah
        // Tiling (8, 1) agar belang berulang sepanjang garis plane 15 meter
        GetOrCreateMaterial("MAT_SafetyLine", Color.white, 0f, 0.2f, "IMG_SafetyLine.png", new Vector2(8f, 1f));
        GetOrCreateMaterial("MAT_SafetyFloor", Color.white, 0f, 0.2f, "IMG_SafetyLine.png", new Vector2(8f, 1f));

        // 2. Lantai & Dinding Workshop
        GetOrCreateMaterial("MAT_ConcreteFloor", Color.white, 0.05f, 0.25f, "IMG_ConcreteFloor.png", new Vector2(8f, 6f));
        GetOrCreateMaterial("MAT_FactoryWall", new Color(0.85f, 0.86f, 0.85f), 0f, 0.15f);

        // 3. Rambu K3
        GetOrCreateMaterial("MAT_Sign_Hazard", Color.white, 0f, 0.2f, "IMG_Sign_PinchHazard.png");
        GetOrCreateMaterial("MAT_Sign_APD", Color.white, 0f, 0.2f, "IMG_Sign_APD.png");
        GetOrCreateMaterial("MAT_Sign_Exit", Color.white, 0f, 0.2f, "IMG_Sign_Exit.png");
        GetOrCreateMaterial("MAT_Sign_APAR", Color.white, 0f, 0.2f, "IMG_Sign_APAR.png");
        GetOrCreateMaterial("MAT_SafetyYellow", new Color(1.0f, 0.8f, 0.0f), 0.1f, 0.35f);
        GetOrCreateMaterial("MAT_FireExtinguisherRed", new Color(0.85f, 0.1f, 0.15f), 0.3f, 0.6f);

        AssetDatabase.SaveAssets();
    }

    public static void BuildSceneHierarchy()
    {
        var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        newScene.name = "SCN_LabK3";

        Material matFloor = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_DIR}/MAT_ConcreteFloor.mat");
        Material matWall = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_DIR}/MAT_FactoryWall.mat");
        Material matSafetyFloor = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_DIR}/MAT_SafetyFloor.mat");
        Material matSignHazard = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_DIR}/MAT_Sign_Hazard.mat");
        Material matSignAPD = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_DIR}/MAT_Sign_APD.mat");
        Material matSignExit = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_DIR}/MAT_Sign_Exit.mat");
        Material matSignAPAR = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_DIR}/MAT_Sign_APAR.mat");
        Material matYellow = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_DIR}/MAT_SafetyYellow.mat");
        Material matRed = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_DIR}/MAT_FireExtinguisherRed.mat");

        // Load Industrial Machine Prefabs
        GameObject pMachine1 = AssetDatabase.LoadAssetAtPath<GameObject>($"{MACHINE_DIR}/Machine_1.prefab");
        GameObject pMachine2 = AssetDatabase.LoadAssetAtPath<GameObject>($"{MACHINE_DIR}/Machine_2.prefab");
        GameObject pMachine3 = AssetDatabase.LoadAssetAtPath<GameObject>($"{MACHINE_DIR}/Machine_3.prefab");
        GameObject pMachine4 = AssetDatabase.LoadAssetAtPath<GameObject>($"{MACHINE_DIR}/Machine_4.prefab");
        GameObject pMachine5 = AssetDatabase.LoadAssetAtPath<GameObject>($"{MACHINE_DIR}/Machine_5.prefab");
        GameObject pMachine6 = AssetDatabase.LoadAssetAtPath<GameObject>($"{MACHINE_DIR}/Machine_6.prefab");
        GameObject pMachine7 = AssetDatabase.LoadAssetAtPath<GameObject>($"{MACHINE_DIR}/Machine_7.prefab");

        // ==========================================
        // 1. EMPTY GAMEOBJECT: ENVIRONMENT (Halaman 6 Modul)
        // ==========================================
        GameObject env = new GameObject("ENVIRONMENT");

        // Lantai: Sekitar 16m x 12m (Modul langkah 5)
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor_16x12m";
        floor.transform.SetParent(env.transform);
        floor.transform.position = new Vector3(0, -0.1f, 0);
        floor.transform.localScale = new Vector3(16f, 0.2f, 12f);
        floor.GetComponent<Renderer>().sharedMaterial = matFloor;

        // Dinding setinggi sekitar 3m (Modul langkah 5)
        GameObject wallNorth = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallNorth.name = "Wall_North";
        wallNorth.transform.SetParent(env.transform);
        wallNorth.transform.position = new Vector3(0, 1.5f, 6.0f);
        wallNorth.transform.localScale = new Vector3(16.3f, 3.0f, 0.3f);
        wallNorth.GetComponent<Renderer>().sharedMaterial = matWall;

        GameObject wallSouth = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallSouth.name = "Wall_South";
        wallSouth.transform.SetParent(env.transform);
        wallSouth.transform.position = new Vector3(0, 1.5f, -6.0f);
        wallSouth.transform.localScale = new Vector3(16.3f, 3.0f, 0.3f);
        wallSouth.GetComponent<Renderer>().sharedMaterial = matWall;

        GameObject wallEast = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallEast.name = "Wall_East";
        wallEast.transform.SetParent(env.transform);
        wallEast.transform.position = new Vector3(8.0f, 1.5f, 0);
        wallEast.transform.localScale = new Vector3(0.3f, 3.0f, 12.0f);
        wallEast.GetComponent<Renderer>().sharedMaterial = matWall;

        GameObject wallWest = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallWest.name = "Wall_West";
        wallWest.transform.SetParent(env.transform);
        wallWest.transform.position = new Vector3(-8.0f, 1.5f, 0);
        wallWest.transform.localScale = new Vector3(0.3f, 3.0f, 12.0f);
        wallWest.GetComponent<Renderer>().sharedMaterial = matWall;

        // ==========================================
        // 2. EMPTY GAMEOBJECT: MACHINE (Halaman 6 & 10-13 Modul)
        // Menggunakan Model Resmi dari Industrial Machine Models (Tanpa Tabrakan)
        // ==========================================
        GameObject machineGroup = new GameObject("MACHINE");

        // Row 1 (Sisi Dinding Utara / Belakang, Z = 4.2m)
        if (pMachine7 != null)
        {
            GameObject m7 = (GameObject)PrefabUtility.InstantiatePrefab(pMachine7);
            m7.name = "Machine_7 (Drum Machine)";
            m7.transform.SetParent(machineGroup.transform);
            m7.transform.position = new Vector3(-5.2f, 0, 4.2f);
            m7.transform.rotation = Quaternion.Euler(0, 0, 0);
        }

        if (pMachine6 != null)
        {
            GameObject m6 = (GameObject)PrefabUtility.InstantiatePrefab(pMachine6);
            m6.name = "Machine_6 (Robot Arm)";
            m6.transform.SetParent(machineGroup.transform);
            m6.transform.position = new Vector3(0.0f, 0, 4.25f);
            m6.transform.rotation = Quaternion.Euler(0, 0, 0);
        }

        if (pMachine3 != null)
        {
            GameObject m3 = (GameObject)PrefabUtility.InstantiatePrefab(pMachine3);
            m3.name = "Machine_3 (Drill Press)";
            m3.transform.SetParent(machineGroup.transform);
            m3.transform.position = new Vector3(5.3f, 0, 4.2f);
            m3.transform.rotation = Quaternion.Euler(0, 0, 0);
        }

        // Row 2 (Sisi Dinding Selatan / Depan, Z = -4.2m)
        if (pMachine4 != null)
        {
            GameObject m4 = (GameObject)PrefabUtility.InstantiatePrefab(pMachine4);
            m4.name = "Machine_4 (Double Tank)";
            m4.transform.SetParent(machineGroup.transform);
            m4.transform.position = new Vector3(-5.0f, 0, -4.2f);
            m4.transform.rotation = Quaternion.Euler(0, 0, 0);
        }

        if (pMachine2 != null)
        {
            GameObject m2 = (GameObject)PrefabUtility.InstantiatePrefab(pMachine2);
            m2.name = "Machine_2 (Vertical Tank)";
            m2.transform.SetParent(machineGroup.transform);
            m2.transform.position = new Vector3(-0.3f, 0, -4.2f);
            m2.transform.rotation = Quaternion.Euler(0, 180, 0);
        }

        if (pMachine1 != null)
        {
            GameObject m1 = (GameObject)PrefabUtility.InstantiatePrefab(pMachine1);
            m1.name = "Machine_1 (Industrial Unit)";
            m1.transform.SetParent(machineGroup.transform);
            m1.transform.position = new Vector3(4.3f, 0, -4.2f);
            m1.transform.rotation = Quaternion.Euler(0, 180, 0);
        }

        if (pMachine5 != null)
        {
            GameObject m5 = (GameObject)PrefabUtility.InstantiatePrefab(pMachine5);
            m5.name = "Machine_5 (Control Unit)";
            m5.transform.SetParent(machineGroup.transform);
            m5.transform.position = new Vector3(6.9f, 0, -4.2f);
            m5.transform.rotation = Quaternion.Euler(0, 0, 0);
        }

        // ==========================================
        // 3. EMPTY GAMEOBJECT: SAFETY (Halaman 7 & 10-13 Modul)
        // 2 Jalur Safety Line Selebar ~1.5-2m (Sesuai Gambar Halaman 13 Modul)
        // ==========================================
        GameObject safetyGroup = new GameObject("SAFETY");

        // Modul Langkah 7 & 10: "Tambahkan plane baru untuk dijadikan 'Safety Line' selebar kurang lebih 2 m."
        Vector3[] linePositions = new Vector3[] {
            new Vector3(0, 0.005f,  1.1f), // Safety Line 1 (Jalur Area Utara)
            new Vector3(0, 0.005f, -1.1f)  // Safety Line 2 (Jalur Area Selatan)
        };

        for (int i = 0; i < linePositions.Length; i++)
        {
            GameObject sLine = GameObject.CreatePrimitive(PrimitiveType.Plane);
            sLine.name = $"SafetyLine ({i + 1})";
            sLine.transform.SetParent(safetyGroup.transform);
            sLine.transform.position = linePositions[i];
            sLine.transform.localScale = new Vector3(1.45f, 1f, 0.15f); // Panjang 14.5m x Lebar 1.5m
            sLine.GetComponent<Renderer>().sharedMaterial = matSafetyFloor;
            GameObject.DestroyImmediate(sLine.GetComponent<Collider>());
        }

        // Signage & Fasilitas K3 Tambahan untuk Memenuhi Nilai Maksimal Latihan 1
        // Rambu Bahaya Mesin di Area Kerja
        GameObject signHazard = GameObject.CreatePrimitive(PrimitiveType.Cube);
        signHazard.name = "Sign_PinchHazard";
        signHazard.transform.SetParent(safetyGroup.transform);
        signHazard.transform.position = new Vector3(-6.5f, 1.8f, 0f);
        signHazard.transform.localScale = new Vector3(0.8f, 0.8f, 0.04f);
        signHazard.transform.rotation = Quaternion.Euler(0, 90, 0);
        signHazard.GetComponent<Renderer>().sharedMaterial = matSignHazard;

        // Rambu Wajib APD
        GameObject signAPD = GameObject.CreatePrimitive(PrimitiveType.Cube);
        signAPD.name = "Sign_Wajib_APD";
        signAPD.transform.SetParent(safetyGroup.transform);
        signAPD.transform.position = new Vector3(0f, 2.2f, -5.82f);
        signAPD.transform.localScale = new Vector3(1.2f, 1.2f, 0.04f);
        signAPD.GetComponent<Renderer>().sharedMaterial = matSignAPD;

        // Rambu Emergency Exit
        GameObject signExit = GameObject.CreatePrimitive(PrimitiveType.Cube);
        signExit.name = "Sign_Emergency_Exit";
        signExit.transform.SetParent(safetyGroup.transform);
        signExit.transform.position = new Vector3(7.82f, 2.2f, -3.5f);
        signExit.transform.rotation = Quaternion.Euler(0, -90, 0);
        signExit.transform.localScale = new Vector3(1.2f, 0.6f, 0.04f);
        signExit.GetComponent<Renderer>().sharedMaterial = matSignExit;

        // Rambu Titik APAR & Tabung APAR
        GameObject signAPAR = GameObject.CreatePrimitive(PrimitiveType.Cube);
        signAPAR.name = "Sign_Titik_APAR";
        signAPAR.transform.SetParent(safetyGroup.transform);
        signAPAR.transform.position = new Vector3(7.82f, 1.8f, 0f);
        signAPAR.transform.rotation = Quaternion.Euler(0, -90, 0);
        signAPAR.transform.localScale = new Vector3(0.7f, 0.7f, 0.04f);
        signAPAR.GetComponent<Renderer>().sharedMaterial = matSignAPAR;

        GameObject apar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        apar.name = "Fire_Extinguisher_Unit";
        apar.transform.SetParent(safetyGroup.transform);
        apar.transform.position = new Vector3(7.75f, 0.5f, 0f);
        apar.transform.localScale = new Vector3(0.22f, 0.35f, 0.22f);
        apar.GetComponent<Renderer>().sharedMaterial = matRed;

        // ==========================================
        // 4. EMPTY GAMEOBJECT: PLAYER (Halaman 6 Modul)
        // ==========================================
        GameObject playerGroup = new GameObject("PLAYER");
        
        GameObject playerObj = new GameObject("K3_Player");
        playerObj.transform.SetParent(playerGroup.transform);
        playerObj.transform.position = new Vector3(-6.0f, 0.1f, -4.8f);
        playerObj.transform.rotation = Quaternion.Euler(0, 45, 0);

        CharacterController cc = playerObj.AddComponent<CharacterController>();
        cc.height = 1.65f;
        cc.radius = 0.22f;
        cc.stepOffset = 0.65f;
        cc.center = new Vector3(0, 0.825f, 0);

        K3PlayerController playerCtrl = playerObj.AddComponent<K3PlayerController>();

        // Camera sebagai child (tinggi mata 1.6m, FOV 60)
        GameObject camObj = new GameObject("Main Camera");
        camObj.transform.SetParent(playerObj.transform);
        camObj.transform.localPosition = new Vector3(0, 1.6f, 0);
        camObj.transform.localRotation = Quaternion.identity;
        camObj.tag = "MainCamera";

        Camera cam = camObj.AddComponent<Camera>();
        cam.fieldOfView = 60f;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 100f;
        camObj.AddComponent<AudioListener>();

        // ==========================================
        // 5. LIGHTING (Pencahayaan Workshop & Emergency)
        // ==========================================
        GameObject lightingGroup = new GameObject("LIGHTING");

        GameObject dirLightObj = new GameObject("Directional Light");
        dirLightObj.transform.SetParent(lightingGroup.transform);
        dirLightObj.transform.position = new Vector3(0, 5, 0);
        dirLightObj.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
        Light dirLight = dirLightObj.AddComponent<Light>();
        dirLight.type = LightType.Directional;
        dirLight.intensity = 0.9f;
        dirLight.color = new Color(1.0f, 0.98f, 0.94f);
        dirLight.shadows = LightShadows.Soft;

        // Emergency Indicator Lights
        GameObject exitLightObj = new GameObject("PointLight_Exit");
        exitLightObj.transform.SetParent(lightingGroup.transform);
        exitLightObj.transform.position = new Vector3(7.6f, 2.4f, -3.5f);
        Light exitLight = exitLightObj.AddComponent<Light>();
        exitLight.type = LightType.Point;
        exitLight.range = 3.5f;
        exitLight.intensity = 1.5f;
        exitLight.color = new Color(0.1f, 1.0f, 0.4f);

        GameObject aparLightObj = new GameObject("PointLight_APAR");
        aparLightObj.transform.SetParent(lightingGroup.transform);
        aparLightObj.transform.position = new Vector3(7.6f, 2.0f, 0f);
        Light aparLight = aparLightObj.AddComponent<Light>();
        aparLight.type = LightType.Point;
        aparLight.range = 3.0f;
        aparLight.intensity = 1.5f;
        aparLight.color = new Color(1.0f, 0.2f, 0.2f);

        EditorSceneManager.SaveScene(newScene, SCENE_PATH);
        Debug.Log($"[LabK3Builder] Scene SCN_LabK3 tersimpan sukses di: {SCENE_PATH}");
    }

    [MenuItem("Tools/K3/2. Ambil 4 Screenshot Laporan K3", false, 11)]
    public static void CaptureScreenshots()
    {
        string outDir = "Assets/08_UI/Screenshots_K3";
        if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

        var scene = EditorSceneManager.OpenScene(SCENE_PATH);
        if (!scene.IsValid()) return;

        GameObject capCamObj = new GameObject("Screenshot_Cam");
        Camera capCam = capCamObj.AddComponent<Camera>();
        capCam.clearFlags = CameraClearFlags.Skybox;
        capCam.fieldOfView = 50f;

        int width = 1920;
        int height = 1080;
        RenderTexture rt = new RenderTexture(width, height, 24);
        capCam.targetTexture = rt;

        // 1. Overview 3D Sesuai Gambar Halaman 13 Modul (Perspektif Sudut Atas Melihat 3 Safety Lines & Mesin)
        capCamObj.transform.position = new Vector3(10.5f, 9.8f, -9.5f);
        capCamObj.transform.rotation = Quaternion.Euler(38f, -44f, 0f);
        CaptureCameraView(capCam, rt, $"{outDir}/01_Overview_RuangK3.png");

        // 2. Zona Mesin & Safety Line (Closeup Mesin 4, 3, dan Garis Kuning-Hijau-Kuning)
        capCamObj.transform.position = new Vector3(-3.0f, 2.8f, -4.8f);
        capCamObj.transform.rotation = Quaternion.Euler(22f, 15f, 0f);
        CaptureCameraView(capCam, rt, $"{outDir}/02_ZonaMesin_SafetyLine.png");

        // 3. Zona Mesin 2, 6, 7 & Safety Line Tengah
        capCamObj.transform.position = new Vector3(5.5f, 2.8f, -2.5f);
        capCamObj.transform.rotation = Quaternion.Euler(18f, -65f, 0f);
        CaptureCameraView(capCam, rt, $"{outDir}/03_ZonaEvakuasi_APAR.png");

        // 4. Pandangan Jalur Kerja & Signage
        capCamObj.transform.position = new Vector3(-6.8f, 2.2f, -4.5f);
        capCamObj.transform.rotation = Quaternion.Euler(14f, 60f, 0f);
        CaptureCameraView(capCam, rt, $"{outDir}/04_JalurAman_SignageAPD.png");

        capCam.targetTexture = null;
        RenderTexture.active = null;
        GameObject.DestroyImmediate(rt);
        GameObject.DestroyImmediate(capCamObj);

        AssetDatabase.Refresh();
        Debug.Log("[LabK3Builder] 4 Screenshot K3 berhasil diperbarui di: " + outDir);
    }

    private static void CaptureCameraView(Camera cam, RenderTexture rt, string savePath)
    {
        RenderTexture.active = rt;
        cam.Render();

        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(savePath, bytes);
        GameObject.DestroyImmediate(tex);
    }
}
#endif
