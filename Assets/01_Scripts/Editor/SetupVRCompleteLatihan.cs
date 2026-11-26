#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit;

public static class SetupVRCompleteLatihan
{
    [MenuItem("Tools/VR/★ Bangun Lengkap: Percobaan 1 & Latihan VR Lab K3 (Otomatis)", false, 0)]
    public static void BuildCompleteVRScenario()
    {
        Scene currentScene = EditorSceneManager.GetActiveScene();
        Debug.Log($"<color=cyan>[VR Setup]</color> Memulai setup lengkap Percobaan 1 & Latihan pada scene: <b>{currentScene.name}</b>");

        // 1. Root Hierarchy PLAYER
        GameObject playerRoot = GameObject.Find("07_PLAYER");
        if (playerRoot == null)
        {
            playerRoot = new GameObject("07_PLAYER");
            Undo.RegisterCreatedObjectUndo(playerRoot, "Create 07_PLAYER");
        }
        playerRoot.transform.position = Vector3.zero;
        playerRoot.transform.rotation = Quaternion.identity;
        playerRoot.transform.localScale = Vector3.one;

        // Nonaktifkan Player_Rig lama dan kapsul OBJ_Controller lama agar kamera tidak tertutup/jatuh
        GameObject playerRig = GameObject.Find("Player_Rig");
        if (playerRig != null) playerRig.SetActive(false);

        GameObject objController = GameObject.Find("OBJ_Controller");
        if (objController != null) objController.SetActive(false);

        // 2. Setup XR Origin (Utamakan XR Origin Hands jika ada, fallback ke XR Origin biasa)
        SetupXROrigin(playerRoot);

        // 3. Pastikan XR Interaction Simulator ada di scene
        SetupSimulator();

        // 4. Siapkan Root 08_VR_SCENARIO di Environment
        GameObject env = GameObject.Find("01_ENVIRONMENT");
        GameObject vrRoot = GameObject.Find("08_VR_SCENARIO");
        if (vrRoot == null)
        {
            vrRoot = new GameObject("08_VR_SCENARIO");
            if (env != null) vrRoot.transform.SetParent(env.transform, false);
            Undo.RegisterCreatedObjectUndo(vrRoot, "Create 08_VR_SCENARIO");
        }

        // 5. Bangun 5 Titik/Area Teleport (Kriteria 1, 3, 4, 5, 8 Latihan)
        BuildTeleportAreas(vrRoot);

        // 6. Bangun Rintangan Lingkungan / Barikade K3 (Kriteria 10 Latihan)
        BuildEnvironmentalObstacle(vrRoot);

        // 7. Bangun Storage Area & 5 Objek Grab Berbeda (Kriteria 6, 7, 8 Latihan)
        BuildStorageAreaAndGrabObjects(vrRoot);

        // 8. Bangun Destination Area & Drop Zone (Kriteria 1, 9 Latihan)
        BuildDestinationAreaAndDropZone(vrRoot);

        // 9. Konfigurasi Interaction Layer Mask pada Interactor XR Origin (Kriteria 5 & 8)
        ConfigureXRInteractors();

        // Simpan Scene
        EditorSceneManager.MarkSceneDirty(currentScene);
        EditorSceneManager.SaveScene(currentScene);

        Debug.Log("=========================================================================================");
        Debug.Log("🎉 <color=green><b>[VR Setup] BERHASIL! Seluruh modul Percobaan 1 & LATIHAN M06 telah siap!</b></color>");
        Debug.Log("👉 <b>1. XR Origin:</b> Berada di 07_PLAYER posisi awal (-24.5, 0.05, 2.5) scale normal (1,1,1).");
        Debug.Log("👉 <b>2. 5 Titik Teleport:</b> Starting -> Corridor -> MachineBay -> Storage -> Destination.");
        Debug.Log("👉 <b>3. Rintangan K3:</b> Memblokir teleport langsung dari Starting ke Storage (wajib memutar via Corridor).");
        Debug.Log("👉 <b>4. 5 Objek Grab:</b> ToolBox (Cube), EnergyCore (Sphere), Battery (Cylinder), Chemical (Capsule), Plate.");
        Debug.Log("👉 <b>5. Destination Drop Zone:</b> Di seberang ruangan dengan feedback teks penghitung (target: 3 objek).");
        Debug.Log("👉 <b>6. Interaction Layers:</b> Teleport Interactor hanya ke area 'Teleport', Grab Interactor hanya ke objek 'Grab'.");
        Debug.Log("=========================================================================================");
    }

    private static void SetupXROrigin(GameObject playerRoot)
    {
        string handsPrefabPath = "Assets/Samples/XR Interaction Toolkit/3.6.1/Hands Interaction Demo/Prefabs/XR Origin Hands (XR Rig).prefab";
        string standardPrefabPath = "Assets/Samples/XR Interaction Toolkit/3.6.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";

        GameObject xrHands = GameObject.Find("XR Origin Hands (XR Rig)");
        GameObject xrStandard = GameObject.Find("XR Origin (XR Rig)");

        GameObject targetOrigin = xrHands;

        // Jika belum ada XR Origin Hands di scene, coba load dari prefab
        if (targetOrigin == null)
        {
            GameObject handsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(handsPrefabPath);
            if (handsPrefab != null)
            {
                targetOrigin = (GameObject)PrefabUtility.InstantiatePrefab(handsPrefab);
                targetOrigin.name = "XR Origin Hands (XR Rig)";
                Undo.RegisterCreatedObjectUndo(targetOrigin, "Instantiate XR Origin Hands");

                if (xrStandard != null)
                {
                    xrStandard.SetActive(false);
                }
            }
            else if (xrStandard != null)
            {
                targetOrigin = xrStandard;
            }
            else
            {
                GameObject stdPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(standardPrefabPath);
                if (stdPrefab != null)
                {
                    targetOrigin = (GameObject)PrefabUtility.InstantiatePrefab(stdPrefab);
                    targetOrigin.name = "XR Origin (XR Rig)";
                    Undo.RegisterCreatedObjectUndo(targetOrigin, "Instantiate XR Origin");
                }
            }
        }

        if (targetOrigin != null)
        {
            targetOrigin.transform.SetParent(playerRoot.transform, false);
            targetOrigin.transform.localScale = Vector3.one;
            targetOrigin.transform.position = new Vector3(-24.5f, 0.05f, 2.5f);
            targetOrigin.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            targetOrigin.SetActive(true);
            Debug.Log($"[VR Setup] Menggunakan {targetOrigin.name} di posisi awal.");
        }
    }

    private static void SetupSimulator()
    {
        GameObject simulator = GameObject.Find("XR Interaction Simulator");
        if (simulator == null)
        {
            string simPrefabPath = "Assets/Samples/XR Interaction Toolkit/3.6.1/XR Interaction Simulator/XR Interaction Simulator.prefab";
            GameObject simPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(simPrefabPath);
            if (simPrefab != null)
            {
                simulator = (GameObject)PrefabUtility.InstantiatePrefab(simPrefab);
                simulator.name = "XR Interaction Simulator";
                Undo.RegisterCreatedObjectUndo(simulator, "Instantiate XR Interaction Simulator");
                Debug.Log("[VR Setup] XR Interaction Simulator ditambahkan.");
            }
        }
    }

    private static void BuildTeleportAreas(GameObject vrRoot)
    {
        GameObject tpParent = GetOrCreateChild(vrRoot, "01_TELEPORTATION_AREAS");

        // Layer Teleport (Set ke Everything -1 agar kompatibel dengan bit layer Teleport Starter Assets)
        int teleportLayer = -1;

        // Buat Material Cyan untuk Teleport Pad
        Material padMat = CreateOrGetMaterial("Mat_VR_TeleportPad", new Color(0f, 0.8f, 1f, 0.4f), true);

        // 1. Starting Pad
        CreateTeleportPad(tpParent, "Teleport_Pad_1_Starting", new Vector3(-24.5f, 0.02f, 2.5f), new Vector3(0.2f, 1f, 0.2f), padMat, teleportLayer, "AREA 1: START");

        // 2. Corridor Pad
        CreateTeleportPad(tpParent, "Teleport_Pad_2_Corridor", new Vector3(-18.0f, 0.02f, 2.0f), new Vector3(0.22f, 1f, 0.22f), padMat, teleportLayer, "LORONG TRANSISI");

        // 3. Machine Bay Pad
        CreateTeleportPad(tpParent, "Teleport_Pad_3_MachineBay", new Vector3(-12.0f, 0.02f, -1.0f), new Vector3(0.25f, 1f, 0.25f), padMat, teleportLayer, "MILLING BAY");

        // 4. Storage Area Pad
        CreateTeleportPad(tpParent, "Teleport_Pad_4_Storage", new Vector3(-6.5f, 0.02f, -3.5f), new Vector3(0.25f, 1f, 0.25f), padMat, teleportLayer, "AREA 2: STORAGE");

        // 5. Destination Area Pad
        CreateTeleportPad(tpParent, "Teleport_Pad_5_Destination", new Vector3(3.5f, 0.02f, 1.5f), new Vector3(0.25f, 1f, 0.25f), padMat, teleportLayer, "AREA 3: DESTINATION");
    }

    private static void CreateTeleportPad(GameObject parent, string name, Vector3 pos, Vector3 scale, Material mat, int layerMask, string label)
    {
        Transform t = parent.transform.Find(name);
        GameObject pad = t != null ? t.gameObject : GameObject.CreatePrimitive(PrimitiveType.Plane);
        pad.name = name;
        pad.transform.SetParent(parent.transform, false);
        pad.transform.position = pos;
        pad.transform.localScale = scale;

        var rend = pad.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.sharedMaterial = mat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        var col = pad.GetComponent<Collider>();
        if (col == null) col = pad.AddComponent<MeshCollider>();

        var tpArea = pad.GetComponent<TeleportationArea>();
        if (tpArea == null) tpArea = pad.AddComponent<TeleportationArea>();
        tpArea.interactionLayers = layerMask;

        // Label 3D Text
        Transform textT = pad.transform.Find("Pad_Label");
        GameObject textObj = textT != null ? textT.gameObject : new GameObject("Pad_Label");
        textObj.transform.SetParent(pad.transform, false);
        textObj.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        textObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        textObj.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);

        var tm = textObj.GetComponent<TextMesh>();
        if (tm == null) tm = textObj.AddComponent<TextMesh>();
        tm.text = label;
        tm.fontSize = 32;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = Color.white;
    }

    private static void BuildEnvironmentalObstacle(GameObject vrRoot)
    {
        GameObject obsParent = GetOrCreateChild(vrRoot, "02_OBSTACLE_BARRICADE");

        // Barikade K3 penghalang pandang langsung antara Starting Area dan Storage
        Transform bT = obsParent.transform.Find("Safety_Barricade_Wall");
        GameObject barricade = bT != null ? bT.gameObject : GameObject.CreatePrimitive(PrimitiveType.Cube);
        barricade.name = "Safety_Barricade_Wall";
        barricade.transform.SetParent(obsParent.transform, false);
        barricade.transform.position = new Vector3(-21.0f, 1.1f, 0.2f);
        barricade.transform.localScale = new Vector3(0.4f, 2.2f, 4.5f);

        Material barMat = CreateOrGetMaterial("Mat_VR_ObstacleBarricade", new Color(0.9f, 0.5f, 0.1f, 1f), false);
        var rend = barricade.GetComponent<Renderer>();
        if (rend != null) rend.sharedMaterial = barMat;

        // Pasang blocker fisik anti-tembus HMD (Kriteria 10 Modul 6)
        var blocker = barricade.GetComponent<VRObstacleCollisionBlocker>();
        if (blocker == null) blocker = barricade.AddComponent<VRObstacleCollisionBlocker>();
        blocker.headRadius = 0.35f;

        // Papan Tanda Rintangan K3
        Transform signT = barricade.transform.Find("Obstacle_Sign");
        GameObject signObj = signT != null ? signT.gameObject : new GameObject("Obstacle_Sign");
        signObj.transform.SetParent(barricade.transform, false);
        signObj.transform.localPosition = new Vector3(-0.55f, 0.3f, 0f);
        signObj.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
        signObj.transform.localScale = new Vector3(0.06f, 0.06f, 0.06f);

        var tm = signObj.GetComponent<TextMesh>();
        if (tm == null) tm = signObj.AddComponent<TextMesh>();
        tm.text = "[ZONA KESELAMATAN K3]\nAREA TERHALANG!\nGunakan Jalur Lorong Aman\n(Teleport ke Corridor)";
        tm.fontSize = 28;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = Color.black;
    }

    private static void BuildStorageAreaAndGrabObjects(GameObject vrRoot)
    {
        GameObject storageParent = GetOrCreateChild(vrRoot, "03_STORAGE_AREA");

        // Meja Rak Material
        Transform tableT = storageParent.transform.Find("Storage_Material_Rack");
        GameObject table = tableT != null ? tableT.gameObject : GameObject.CreatePrimitive(PrimitiveType.Cube);
        table.name = "Storage_Material_Rack";
        table.transform.SetParent(storageParent.transform, false);
        table.transform.position = new Vector3(-6.5f, 0.45f, -2.2f);
        table.transform.localScale = new Vector3(2.4f, 0.9f, 0.9f);

        Material tableMat = CreateOrGetMaterial("Mat_VR_MetalRack", new Color(0.25f, 0.3f, 0.35f), false);
        var tableRend = table.GetComponent<Renderer>();
        if (tableRend != null) tableRend.sharedMaterial = tableMat;

        // 5 Objek Grab Berbeda (Kriteria 6, 7, 8)
        int grabLayer = InteractionLayerMask.GetMask("Grab");
        if (grabLayer == 0) grabLayer = 1 << 2; // Fallback jika mask 0

        // 1. ToolBox Cube (Merah)
        CreateGrabItem(storageParent, PrimitiveType.Cube, "Grab_1_ToolBox_Cube", 
            new Vector3(-7.3f, 0.98f, -2.2f), new Vector3(0.18f, 0.18f, 0.18f), 
            CreateOrGetMaterial("Mat_Grab_Red", new Color(0.85f, 0.15f, 0.15f), false), grabLayer);

        // 2. EnergyCore Sphere (Cyan)
        CreateGrabItem(storageParent, PrimitiveType.Sphere, "Grab_2_EnergyCore_Sphere", 
            new Vector3(-6.9f, 0.98f, -2.2f), new Vector3(0.2f, 0.2f, 0.2f), 
            CreateOrGetMaterial("Mat_Grab_Cyan", new Color(0.1f, 0.8f, 0.9f), false), grabLayer);

        // 3. PowerCell Cylinder (Kuning)
        CreateGrabItem(storageParent, PrimitiveType.Cylinder, "Grab_3_PowerCell_Cylinder", 
            new Vector3(-6.5f, 1.0f, -2.2f), new Vector3(0.15f, 0.22f, 0.15f), 
            CreateOrGetMaterial("Mat_Grab_Yellow", new Color(0.95f, 0.85f, 0.1f), false), grabLayer);

        // 4. ChemicalFlask Capsule (Hijau)
        CreateGrabItem(storageParent, PrimitiveType.Capsule, "Grab_4_Chemical_Capsule", 
            new Vector3(-6.1f, 1.02f, -2.2f), new Vector3(0.14f, 0.24f, 0.14f), 
            CreateOrGetMaterial("Mat_Grab_Green", new Color(0.15f, 0.85f, 0.25f), false), grabLayer);

        // 5. CircuitBoard Plate (Ungu)
        CreateGrabItem(storageParent, PrimitiveType.Cube, "Grab_5_CircuitBoard_Plate", 
            new Vector3(-5.7f, 0.93f, -2.2f), new Vector3(0.26f, 0.05f, 0.18f), 
            CreateOrGetMaterial("Mat_Grab_Purple", new Color(0.65f, 0.2f, 0.85f), false), grabLayer);
    }

    private static void CreateGrabItem(GameObject parent, PrimitiveType type, string name, Vector3 pos, Vector3 scale, Material mat, int grabLayerMask)
    {
        Transform t = parent.transform.Find(name);
        GameObject obj = t != null ? t.gameObject : GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.SetParent(parent.transform, false);
        obj.transform.position = pos;
        obj.transform.localScale = scale;

        var rend = obj.GetComponent<Renderer>();
        if (rend != null) rend.sharedMaterial = mat;

        // Collider
        var col = obj.GetComponent<Collider>();
        if (col == null)
        {
            if (type == PrimitiveType.Cube) col = obj.AddComponent<BoxCollider>();
            else if (type == PrimitiveType.Sphere) col = obj.AddComponent<SphereCollider>();
            else col = obj.AddComponent<CapsuleCollider>();
        }

        // Rigidbody
        var rb = obj.GetComponent<Rigidbody>();
        if (rb == null) rb = obj.AddComponent<Rigidbody>();
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.mass = 1.0f;
        rb.linearDamping = 0.05f;
        rb.angularDamping = 0.05f;

        // XR Grab Interactable
        var grab = obj.GetComponent<XRGrabInteractable>();
        if (grab == null) grab = obj.AddComponent<XRGrabInteractable>();
        grab.movementType = XRBaseInteractable.MovementType.Kinematic;
        grab.throwOnDetach = true;
        grab.interactionLayers = grabLayerMask;
    }

    private static void BuildDestinationAreaAndDropZone(GameObject vrRoot)
    {
        GameObject destParent = GetOrCreateChild(vrRoot, "04_DESTINATION_AREA");

        // Meja Perakitan / Drop Table
        Transform tableT = destParent.transform.Find("Destination_Assembly_Table");
        GameObject table = tableT != null ? tableT.gameObject : GameObject.CreatePrimitive(PrimitiveType.Cube);
        table.name = "Destination_Assembly_Table";
        table.transform.SetParent(destParent.transform, false);
        table.transform.position = new Vector3(3.5f, 0.45f, 2.7f);
        table.transform.localScale = new Vector3(2.4f, 0.9f, 1.0f);

        Material tableMat = CreateOrGetMaterial("Mat_VR_DestTable", new Color(0.2f, 0.25f, 0.3f), false);
        var tableRend = table.GetComponent<Renderer>();
        if (tableRend != null) tableRend.sharedMaterial = tableMat;

        // Nampan Drop Zone (Trigger)
        Transform trayT = table.transform.Find("Destination_DropZone_Tray");
        GameObject tray = trayT != null ? trayT.gameObject : GameObject.CreatePrimitive(PrimitiveType.Cube);
        tray.name = "Destination_DropZone_Tray";
        tray.transform.SetParent(table.transform, false);
        tray.transform.localPosition = new Vector3(0f, 0.55f, 0f);
        tray.transform.localScale = new Vector3(0.9f, 0.15f, 0.85f);

        Material trayMat = CreateOrGetMaterial("Mat_VR_DropTray", new Color(0f, 0.7f, 0.3f, 0.7f), true);
        var trayRend = tray.GetComponent<Renderer>();
        if (trayRend != null) trayRend.sharedMaterial = trayMat;

        var trayCol = tray.GetComponent<BoxCollider>();
        if (trayCol != null) trayCol.isTrigger = true;

        // Script Detektor & Penghitung Objek
        var detector = tray.GetComponent<DestinationDropZone>();
        if (detector == null) detector = tray.AddComponent<DestinationDropZone>();
        detector.targetCount = 3;

        // Display Teks Papan Status
        Transform displayT = table.transform.Find("Status_Signboard");
        GameObject signboard = displayT != null ? displayT.gameObject : new GameObject("Status_Signboard");
        signboard.transform.SetParent(table.transform, false);
        signboard.transform.localPosition = new Vector3(0f, 1.3f, 0.45f);
        signboard.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        signboard.transform.localScale = new Vector3(0.06f, 0.06f, 0.06f);

        var tm = signboard.GetComponent<TextMesh>();
        if (tm == null) tm = signboard.AddComponent<TextMesh>();
        tm.fontSize = 28;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.text = "[AREA TUJUAN - DROP ZONE]\nPindahkan 3 Objek ke Sini\nObjek Terkumpul: 0 / 3";
        tm.color = Color.yellow;

        detector.statusTextMesh = tm;
    }

    private static void ConfigureXRInteractors()
    {
        int teleportLayer = InteractionLayerMask.GetMask("Teleport") | (1 << 31);
        int grabLayer = InteractionLayerMask.GetMask("Grab", "Default");

        // Cari semua Teleport Interactor di scene dan set layer-nya
        var allRayInteractors = Object.FindObjectsByType<XRRayInteractor>(FindObjectsInactive.Include);
        foreach (var ray in allRayInteractors)
        {
            if (ray.gameObject.name.Contains("Teleport"))
            {
                ray.interactionLayers = teleportLayer;
                Debug.Log($"[VR Setup] {ray.gameObject.name} interactionLayers diset ke Teleport.");
            }
        }

        // Cari semua NearFarInteractor (Direct/Grab)
        var allNearFar = Object.FindObjectsByType<NearFarInteractor>(FindObjectsInactive.Include);
        foreach (var nearFar in allNearFar)
        {
            nearFar.interactionLayers = grabLayer;
        }
    }

    private static GameObject GetOrCreateChild(GameObject parent, string name)
    {
        Transform t = parent.transform.Find(name);
        if (t != null) return t.gameObject;

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        return go;
    }

    private static Material CreateOrGetMaterial(string name, Color color, bool transparent)
    {
        if (!AssetDatabase.IsValidFolder("Assets/04_Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "04_Materials");
        }
        string path = $"Assets/04_Materials/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Standard");
            mat = new Material(s);
            mat.name = name;

            if (transparent)
            {
                mat.SetFloat("_Surface", 1); // Transparent in URP
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = 3000;
            }

            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);

            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        }
        return mat;
    }
}
#endif
