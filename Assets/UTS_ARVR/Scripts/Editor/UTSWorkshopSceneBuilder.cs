#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

/// <summary>
/// Builder Otomatis untuk Seluruh Soal UTS Pemrograman Berbasis AR/VR
/// Membangun Scene: Assets/UTS_ARVR/Scenes/test_uts.unity
/// </summary>
public static class UTSWorkshopSceneBuilder
{
    private const string RootFolder = "Assets/UTS_ARVR";
    private const string ScenesFolder = "Assets/UTS_ARVR/Scenes";
    private const string ScriptsFolder = "Assets/UTS_ARVR/Scripts";
    private const string AnimationsFolder = "Assets/UTS_ARVR/Animations";
    private const string PrefabsFolder = "Assets/UTS_ARVR/Prefabs";
    private const string ScenePath = "Assets/UTS_ARVR/Scenes/test_uts.unity";

    [InitializeOnLoadMethod]
    private static void AutoBuildOnLoadIfMissing()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene active = SceneManager.GetActiveScene();
            if (active.IsValid() && active.path == ScenePath)
            {
                PatchActiveUTSSceneInMemory();
            }
        };
    }

    [MenuItem("Tools/UTS ARVR/★ Perbaiki XR Rig & Material Pintu (Tanpa Ubah Layout)", false, 1)]
    public static void PatchActiveUTSSceneInMemory()
    {
        Scene active = SceneManager.GetActiveScene();
        if (!active.IsValid() || active.path != ScenePath) return;

        bool changed = false;

        // Cek jika objek alat masih memakai model 3D lama (Axe, dsb), otomatis ubah jadi primitive
        if (GameObject.Find("HandTool_01_Axe") != null || GameObject.Find("Tool_Box_Red") == null)
        {
            ReplaceToolsWithPrimitives();
            return;
        }

        // 1. Pastikan Main Camera di dalam XR Origin memiliki tag "MainCamera" (dibutuhkan oleh XRInteractionSimulator Camera.main)
        XROrigin xrOrigin = Object.FindFirstObjectByType<XROrigin>();
        if (xrOrigin != null)
        {
            Camera cam = xrOrigin.Camera != null ? xrOrigin.Camera : xrOrigin.GetComponentInChildren<Camera>(true);
            if (cam != null)
            {
                if (cam.gameObject.tag != "MainCamera")
                {
                    cam.gameObject.tag = "MainCamera";
                    changed = true;
                }
                SphereCollider sc = cam.gameObject.GetComponent<SphereCollider>();
                if (sc != null) { Object.DestroyImmediate(sc); changed = true; }
                Rigidbody rb = cam.gameObject.GetComponent<Rigidbody>();
                if (rb != null) { Object.DestroyImmediate(rb); changed = true; }
            }

            // Pastikan Teleport Interactor & NearFarInteractor memiliki layer mask yang sesuai
            int tpMask = (1 << 1) | (1 << 31);
            foreach (var ray in xrOrigin.GetComponentsInChildren<XRRayInteractor>(true))
            {
                if (ray.gameObject.name.Contains("Teleport") && ray.interactionLayers.value != tpMask)
                {
                    ray.interactionLayers = tpMask;
                    EditorUtility.SetDirty(ray);
                    changed = true;
                }
            }
            int grabMask = (1 << 0) | (1 << 2);
            foreach (var nf in xrOrigin.GetComponentsInChildren<NearFarInteractor>(true))
            {
                if (nf.interactionLayers.value != grabMask)
                {
                    nf.interactionLayers = grabMask;
                    EditorUtility.SetDirty(nf);
                    changed = true;
                }
            }
        }

        // 2. Pastikan XR Interaction Simulator berada di root scene (agar DontDestroyOnLoad berfungsi normal)
        GameObject simObj = GameObject.Find("XR Interaction Simulator");
        if (simObj != null && simObj.transform.parent != null)
        {
            simObj.transform.SetParent(null);
            changed = true;
        }

        // 3. Perbaiki TeleportationArea & XRGrabInteractable
        XRInteractionManager mgr = Object.FindFirstObjectByType<XRInteractionManager>();
        TeleportationProvider tpProvider = Object.FindFirstObjectByType<TeleportationProvider>();
        foreach (var tp in Object.FindObjectsByType<TeleportationArea>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (tp.interactionLayers.value != -1 || tp.interactionManager != mgr || (tpProvider != null && tp.teleportationProvider != tpProvider))
            {
                tp.interactionLayers = -1;
                if (mgr != null) tp.interactionManager = mgr;
                if (tpProvider != null) tp.teleportationProvider = tpProvider;
                EditorUtility.SetDirty(tp);
                changed = true;
            }
        }
        foreach (var g in Object.FindObjectsByType<XRGrabInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (g.interactionManager != mgr || g.interactionLayers.value != 5)
            {
                if (mgr != null) g.interactionManager = mgr;
                g.interactionLayers = 5;
                EditorUtility.SetDirty(g);
                changed = true;
            }
        }

        // 4. Perbaiki Material Pintu (Door_Leaf_Mesh) & MeshRenderer yang null dengan material URP dari Assets/04_Materials/
        Material doorMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/04_Materials/M_AH_Puerta.mat");
        Material glassMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/04_Materials/M_AH_Vidrio.mat");
        Material bannerMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/UTS_ARVR/Prefabs/Mat_UTS_ValidBannerBg.mat");
        Material handToolMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/UTS_ARVR/Prefabs/Mat_ToolBase_HandTool.mat");
        Material powerToolMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/UTS_ARVR/Prefabs/Mat_ToolBase_PowerTool.mat");
        Material measureToolMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/UTS_ARVR/Prefabs/Mat_ToolBase_MeasuringTool.mat");

        foreach (var mr in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (mr.GetComponent<TextMesh>() != null) continue;
            string n = mr.gameObject.name;
            if (n == "Door_Leaf_Mesh" && doorMat != null && mr.sharedMaterial != doorMat)
            {
                mr.sharedMaterial = doorMat;
                EditorUtility.SetDirty(mr);
                changed = true;
            }
            else if (n == "Door_Window_Glass" && glassMat != null && mr.sharedMaterial != glassMat)
            {
                mr.sharedMaterial = glassMat;
                EditorUtility.SetDirty(mr);
                changed = true; 
            }
            else if (mr.sharedMaterial == null)
            {
                if (n == "InfoBanner_Background" && bannerMat != null) mr.sharedMaterial = bannerMat;
                else if (n == "Tool_Grip_Base")
                {
                    if (mr.gameObject.CompareTag("PowerTool") && powerToolMat != null) mr.sharedMaterial = powerToolMat;
                    else if (mr.gameObject.CompareTag("MeasuringTool") && measureToolMat != null) mr.sharedMaterial = measureToolMat;
                    else if (handToolMat != null) mr.sharedMaterial = handToolMat;
                }
                else if (doorMat != null) mr.sharedMaterial = doorMat;

                EditorUtility.SetDirty(mr);
                changed = true;
            }
        }

        // 5. Bersihkan teks sign, banner, dan label yang tidak terpakai
        string[] labelsToPurge = new string[] {
            "Tool_Category_Label", "StaticProp_Label", "Workbench_Label",
            "Banner_StartingArea", "Banner_EquipmentArea", "Banner_ToolCheckArea", "Banner_MachineArea",
            "Sign_Text_Front", "Sign_Text_Back"
        };
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (System.Array.IndexOf(labelsToPurge, go.name) >= 0)
            {
                Object.DestroyImmediate(go);
                changed = true;
            }
        }

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active);
            Debug.Log("[UTS ARVR] Konfigurasi XR Origin, XR Interaction Simulator, Teleportation, Grab, dan Material Pintu berhasil diperbaiki tanpa mengubah layout.");
        }
    }

    [MenuItem("Tools/UTS ARVR/Ganti Alat Menjadi Primitive Unity (Cube, Sphere, Capsule, Cylinder)", false, 2)]
    public static void ReplaceToolsWithPrimitives()
    {
        Scene active = SceneManager.GetActiveScene();
        if (!active.IsValid() || active.path != ScenePath)
        {
            EditorSceneManager.OpenScene(ScenePath);
            active = SceneManager.GetActiveScene();
        }

        // 1. Ganti Seluruh Objek Grabbable dengan Primitive Bawaan Unity
        GameObject toolsGroup = GameObject.Find("Interactable_Physical_Tools_6_Items");
        if (toolsGroup == null)
        {
            GameObject area2 = GameObject.Find("Area_02_EquipmentArea");
            if (area2 != null)
            {
                toolsGroup = new GameObject("Interactable_Physical_Tools_6_Items");
                toolsGroup.transform.SetParent(area2.transform, false);
            }
        }

        if (toolsGroup != null)
        {
            List<GameObject> toDestroy = new List<GameObject>();
            for (int i = 0; i < toolsGroup.transform.childCount; i++)
            {
                toDestroy.Add(toolsGroup.transform.GetChild(i).gameObject);
            }
            foreach (var go in toDestroy)
            {
                Object.DestroyImmediate(go);
            }

            // Kategori 1: Hand Tools (Bentuk CUBE / BOX -> Tag: HandTool)
            CreatePrimitiveGrabbableTool(toolsGroup, "Tool_Box_Red", "HandTool", PrimitiveType.Cube,
                new Vector3(-4.0f, 1.0f, 11.2f), new Vector3(0.24f, 0.24f, 0.24f), new Color(0.95f, 0.2f, 0.2f));

            CreatePrimitiveGrabbableTool(toolsGroup, "Tool_Box_Orange", "HandTool", PrimitiveType.Cube,
                new Vector3(-3.5f, 1.0f, 11.2f), new Vector3(0.22f, 0.22f, 0.22f), new Color(1.0f, 0.55f, 0.1f));

            // Kategori 2: Power Tools (Bentuk SPHERE / BOLA -> Tag: PowerTool)
            CreatePrimitiveGrabbableTool(toolsGroup, "Tool_Sphere_Blue", "PowerTool", PrimitiveType.Sphere,
                new Vector3(-3.0f, 1.0f, 11.2f), new Vector3(0.25f, 0.25f, 0.25f), new Color(0.15f, 0.55f, 1.0f));

            CreatePrimitiveGrabbableTool(toolsGroup, "Tool_Sphere_Cyan", "PowerTool", PrimitiveType.Sphere,
                new Vector3(-2.5f, 1.0f, 11.2f), new Vector3(0.22f, 0.22f, 0.22f), new Color(0.1f, 0.9f, 0.9f));

            // Kategori 3: Measuring Tools (Bentuk CAPSULE & CYLINDER -> Tag: MeasuringTool)
            CreatePrimitiveGrabbableTool(toolsGroup, "Tool_Capsule_Yellow", "MeasuringTool", PrimitiveType.Capsule,
                new Vector3(-2.0f, 1.0f, 11.2f), new Vector3(0.18f, 0.24f, 0.18f), new Color(1.0f, 0.85f, 0.1f));

            CreatePrimitiveGrabbableTool(toolsGroup, "Tool_Cylinder_Green", "MeasuringTool", PrimitiveType.Cylinder,
                new Vector3(-1.5f, 1.0f, 11.2f), new Vector3(0.20f, 0.15f, 0.20f), new Color(0.2f, 0.85f, 0.35f));
        }

        // 2. Ganti Objek Lingkungan Statis Non-Grabbable di Meja Kanan dengan Primitive Bawaan Unity
        GameObject nonGrabGroup = GameObject.Find("NonGrabbable_Environment_Objects");
        if (nonGrabGroup != null)
        {
            List<GameObject> toDestroyEnv = new List<GameObject>();
            for (int i = 0; i < nonGrabGroup.transform.childCount; i++)
            {
                toDestroyEnv.Add(nonGrabGroup.transform.GetChild(i).gameObject);
            }
            foreach (var go in toDestroyEnv)
            {
                Object.DestroyImmediate(go);
            }

            CreatePrimitiveStaticEnvObject(nonGrabGroup, "Static_Env_FirstAidBox", PrimitiveType.Cube,
                new Vector3(-4.55f, 0.98f, 11.2f), new Vector3(0.35f, 0.22f, 0.25f), new Color(0.9f, 0.9f, 0.95f));

            CreatePrimitiveStaticEnvObject(nonGrabGroup, "Static_Env_SteelPillar", PrimitiveType.Cylinder,
                new Vector3(2.2f, 1.20f, 11.2f), new Vector3(0.35f, 0.70f, 0.35f), new Color(0.35f, 0.4f, 0.45f));

            CreatePrimitiveStaticEnvObject(nonGrabGroup, "Static_Env_SupplyUnit", PrimitiveType.Cube,
                new Vector3(3.3f, 1.08f, 11.2f), new Vector3(0.50f, 0.45f, 0.45f), new Color(0.45f, 0.32f, 0.22f));
        }

        // 3. Konfigurasi Ulang Meja Inspeksi Soal 3
        UpdateToolCheckStationReferences();

        EditorSceneManager.MarkSceneDirty(active);
        EditorSceneManager.SaveScene(active);
        Debug.Log("[UTS ARVR] Seluruh peralatan berhasil diganti menjadi Primitive Unity murni (Cube/Box, Sphere, Capsule, Cylinder) & meja inspeksi Box siap divalidasi!");
    }

    private static void UpdateToolCheckStationReferences()
    {
        // Meja 1: Hand Tools (Box/Cube)
        SetupStationTrigger("Zone_01_HandTools_CheckStation", "TriggerArea_HandTool", "InfoText_HandTool", "HandTool", "Hand Tools (Box/Cube)");

        // Meja 2: Power Tools (Sphere/Bola)
        SetupStationTrigger("Zone_02_PowerTools_CheckStation", "TriggerArea_PowerTool", "InfoText_PowerTool", "PowerTool", "Power Tools (Sphere/Bola)");

        // Meja 3: Measuring Tools (Capsule/Cylinder)
        SetupStationTrigger("Zone_03_MeasuringTools_CheckStation", "TriggerArea_MeasuringTool", "InfoText_MeasuringTool", "MeasuringTool", "Measuring Tools (Capsule/Cylinder)");
    }

    private static void SetupStationTrigger(string stationName, string triggerName, string infoTextName, string tag, string category)
    {
        GameObject station = GameObject.Find(stationName);
        if (station == null) return;

        Transform triggerT = station.transform.Find(triggerName);
        Transform infoT = station.transform.Find(infoTextName);

        if (triggerT != null)
        {
            ToolCheckArea script = triggerT.GetComponent<ToolCheckArea>();
            if (script == null) script = triggerT.gameObject.AddComponent<ToolCheckArea>();

            script.targetToolTag = tag;
            script.categoryName = category;
            if (infoT != null)
            {
                script.infoText = infoT.gameObject;
                infoT.gameObject.SetActive(false);
            }
            EditorUtility.SetDirty(script);
        }
    }

    [MenuItem("Tools/UTS ARVR/★ Bangun Scene UTS Lengkap (test_uts.unity)", false, 0)]
    public static void BuildUTSScene()
    {
        EnsureFolders();
        EnsureTagsRegistered();

        // 1. Buat Animator Controller & Animation Clips untuk Soal 4
        AnimatorController doorController = CreateDoorAnimationsAndController();

        // 2. Buat Scene Baru yang Bersih
        Scene utsScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        utsScene.name = "test_uts";

        // 3. Root Hierarchy Pengelompokan yang Rapi (Rubrik Umum Poin 8)
        GameObject rootLighting = new GameObject("01_LIGHTING_AND_ENVIRONMENT");
        GameObject rootPlayer = new GameObject("02_PLAYER_VR_SYSTEM");
        GameObject rootWorkshop = new GameObject("03_WORKSHOP_AREAS");
        GameObject rootDoors = new GameObject("04_CONNECTING_DOORS");

        // 4. Bangun Pencahayaan & Struktur Luar Workshop
        BuildLightingAndOuterShell(rootLighting);

        // 5. Bangun Sistem VR Player (XR Origin + CharacterController + Simulator)
        TeleportationProvider teleportProvider = BuildVRPlayerAndSimulator(rootPlayer);

        // 6. Bangun 4 Area Workshop & Teleportation Area (Soal 1, 2, 3)
        BuildStartingArea(rootWorkshop, teleportProvider);
        BuildEquipmentArea(rootWorkshop, teleportProvider);
        BuildToolCheckArea(rootWorkshop, teleportProvider);
        BuildMachineArea(rootWorkshop, teleportProvider);

        // 7. Bangun Dinding Sekat & 3 Pintu Otomatis Penghubung 4 Area (Soal 4)
        BuildConnectingWallsAndDoors(rootDoors, doorController, teleportProvider);

        // 8. Konfigurasi Interaction Layers pada Interactor XR Origin
        ConfigureXRInteractorLayers(teleportProvider);

        // 9. Simpan Scene ke Assets/UTS_ARVR/Scenes/test_uts.unity
        EditorSceneManager.MarkSceneDirty(utsScene);
        EditorSceneManager.SaveScene(utsScene, ScenePath);
        AssetDatabase.SaveAssets();

        Debug.Log("==================================================================================");
        Debug.Log("<color=green><b>[UTS AR/VR] SCENE 'test_uts.unity' BERHASIL DIBANGUN DENGAN LENGKAP!</b></color>");
        Debug.Log($"Scene tersimpan di: <b>{ScenePath}</b>");
        Debug.Log("1. [SOAL 1] 4 Area Workshop (Starting, Equipment, Tool Check 3 Kategori, Machine) + Teleportation khusus lantai jalan + CharacterController anti-tembus.");
        Debug.Log("2. [SOAL 2] 6 Objek Peralatan Fisik (XRGrabInteractable + Rigidbody + Collider) + 3 Objek Lingkungan Statis (Tanpa XRGrabInteractable).");
        Debug.Log("3. [SOAL 3] 3 Zona Pemeriksaan ToolCheckArea (HandTool, PowerTool, MeasuringTool) dengan UI infoText (awal tersembunyi, muncul otomatis saat alat sesuai).");
        Debug.Log("4. [SOAL 4] Pintu Otomatis Penghubung 4 Area dengan 2-State Animator (Door_Closed <-> Door_Open via parameter 'isOpen') & DoorAutoTrigger.");
        Debug.Log("==================================================================================");
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder(RootFolder))
            AssetDatabase.CreateFolder("Assets", "UTS_ARVR");
        if (!AssetDatabase.IsValidFolder(ScenesFolder))
            AssetDatabase.CreateFolder(RootFolder, "Scenes");
        if (!AssetDatabase.IsValidFolder(ScriptsFolder))
            AssetDatabase.CreateFolder(RootFolder, "Scripts");
        if (!AssetDatabase.IsValidFolder(AnimationsFolder))
            AssetDatabase.CreateFolder(RootFolder, "Animations");
        if (!AssetDatabase.IsValidFolder(PrefabsFolder))
            AssetDatabase.CreateFolder(RootFolder, "Prefabs");
    }

    private static void EnsureTagsRegistered()
    {
        Object[] tagAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (tagAssets == null || tagAssets.Length == 0) return;

        SerializedObject tagManager = new SerializedObject(tagAssets[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        string[] requiredTags = { "HandTool", "PowerTool", "MeasuringTool" };
        foreach (string reqTag in requiredTags)
        {
            bool found = false;
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == reqTag)
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                int index = tagsProp.arraySize;
                tagsProp.InsertArrayElementAtIndex(index);
                tagsProp.GetArrayElementAtIndex(index).stringValue = reqTag;
            }
        }
        tagManager.ApplyModifiedProperties();
    }

    // ========================================================================================
    // SOAL 4: PEMBUATAN ANIMATION CLIPS & 2-STATE ANIMATOR CONTROLLER
    // ========================================================================================
    private static AnimatorController CreateDoorAnimationsAndController()
    {
        string controllerPath = $"{AnimationsFolder}/UTS_DoorAnimator.controller";
        AnimatorController existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (existing != null && existing.layers.Length > 0)
        {
            return existing;
        }

        string closedClipPath = $"{AnimationsFolder}/UTS_Door_Closed.anim";
        string openClipPath = $"{AnimationsFolder}/UTS_Door_Open.anim";

        AnimationClip closedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(closedClipPath);
        if (closedClip == null)
        {
            closedClip = new AnimationClip { name = "UTS_Door_Closed" };
            AnimationUtility.SetEditorCurve(closedClip, EditorCurveBinding.FloatCurve("", typeof(Transform), "localEulerAnglesRaw.x"), AnimationCurve.Constant(0f, 1f, 0f));
            AnimationUtility.SetEditorCurve(closedClip, EditorCurveBinding.FloatCurve("", typeof(Transform), "localEulerAnglesRaw.y"), AnimationCurve.Constant(0f, 1f, 0f));
            AnimationUtility.SetEditorCurve(closedClip, EditorCurveBinding.FloatCurve("", typeof(Transform), "localEulerAnglesRaw.z"), AnimationCurve.Constant(0f, 1f, 0f));
            AssetDatabase.CreateAsset(closedClip, closedClipPath);
        }

        AnimationClip openClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(openClipPath);
        if (openClip == null)
        {
            openClip = new AnimationClip { name = "UTS_Door_Open" };
            AnimationUtility.SetEditorCurve(openClip, EditorCurveBinding.FloatCurve("", typeof(Transform), "localEulerAnglesRaw.x"), AnimationCurve.Constant(0f, 1f, 0f));
            AnimationUtility.SetEditorCurve(openClip, EditorCurveBinding.FloatCurve("", typeof(Transform), "localEulerAnglesRaw.y"), AnimationCurve.Constant(0f, 1f, -90f));
            AnimationUtility.SetEditorCurve(openClip, EditorCurveBinding.FloatCurve("", typeof(Transform), "localEulerAnglesRaw.z"), AnimationCurve.Constant(0f, 1f, 0f));
            AssetDatabase.CreateAsset(openClip, openClipPath);
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        controller.AddParameter("isOpen", AnimatorControllerParameterType.Bool);

        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        AnimatorState stateClosed = sm.AddState("Door_Closed", new Vector3(280f, 60f, 0f));
        stateClosed.motion = closedClip;
        sm.defaultState = stateClosed;

        AnimatorState stateOpen = sm.AddState("Door_Open", new Vector3(280f, 200f, 0f));
        stateOpen.motion = openClip;

        AnimatorStateTransition toOpen = stateClosed.AddTransition(stateOpen);
        toOpen.hasExitTime = false;
        toOpen.duration = 0.4f;
        toOpen.AddCondition(AnimatorConditionMode.If, 0f, "isOpen");

        AnimatorStateTransition toClosed = stateOpen.AddTransition(stateClosed);
        toClosed.hasExitTime = false;
        toClosed.duration = 0.4f;
        toClosed.AddCondition(AnimatorConditionMode.IfNot, 0f, "isOpen");

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return controller;
    }

    private static void SetQuaternionCurve(AnimationClip clip, string relativePath, Quaternion startQ, Quaternion endQ, float duration)
    {
        AnimationCurve curveX = new AnimationCurve(new Keyframe(0f, startQ.x), new Keyframe(duration, endQ.x));
        AnimationCurve curveY = new AnimationCurve(new Keyframe(0f, startQ.y), new Keyframe(duration, endQ.y));
        AnimationCurve curveZ = new AnimationCurve(new Keyframe(0f, startQ.z), new Keyframe(duration, endQ.z));
        AnimationCurve curveW = new AnimationCurve(new Keyframe(0f, startQ.w), new Keyframe(duration, endQ.w));

        clip.SetCurve(relativePath, typeof(Transform), "m_LocalRotation.x", curveX);
        clip.SetCurve(relativePath, typeof(Transform), "m_LocalRotation.y", curveY);
        clip.SetCurve(relativePath, typeof(Transform), "m_LocalRotation.z", curveZ);
        clip.SetCurve(relativePath, typeof(Transform), "m_LocalRotation.w", curveW);
    }

    // ========================================================================================
    // PENCAHAYAAN & STRUKTUR BANGUNAN WORKSHOP (NON-TELEPORTABLE COLLIDERS)
    // ========================================================================================
    private static void BuildLightingAndOuterShell(GameObject rootLighting)
    {
        // Directional Light
        GameObject sunObj = new GameObject("Directional_Light_Workshop");
        sunObj.transform.SetParent(rootLighting.transform, false);
        sunObj.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
        Light sunLight = sunObj.AddComponent<Light>();
        sunLight.type = LightType.Directional;
        sunLight.color = new Color(1f, 0.97f, 0.92f);
        sunLight.intensity = 1.2f;
        sunLight.shadows = LightShadows.Soft;

        // Point Lights di setiap Area (Starting, Equipment, Tool Check, Machine)
        float[] areaCentersZ = { 1.0f, 11.0f, 21.0f, 31.0f };
        string[] areaNames = { "StartingArea", "EquipmentArea", "ToolCheckArea", "MachineArea" };
        GameObject lightsGroup = new GameObject("Workshop_Ceiling_Lights");
        lightsGroup.transform.SetParent(rootLighting.transform, false);

        GameObject highBayPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/10_K3_Simulation/WarehousePack/Prefabs/URP/HighBayLight.prefab");

        for (int i = 0; i < areaCentersZ.Length; i++)
        {
            GameObject ptObj = new GameObject($"PointLight_{areaNames[i]}");
            ptObj.transform.SetParent(lightsGroup.transform, false);
            ptObj.transform.position = new Vector3(0f, 3.6f, areaCentersZ[i]);
            Light pt = ptObj.AddComponent<Light>();
            pt.type = LightType.Point;
            pt.range = 14f;
            pt.intensity = 2.0f;
            pt.color = new Color(0.95f, 0.98f, 1f);

            if (highBayPrefab != null)
            {
                GameObject lampVisual = (GameObject)PrefabUtility.InstantiatePrefab(highBayPrefab);
                lampVisual.name = $"HighBayLight_{areaNames[i]}";
                lampVisual.transform.SetParent(ptObj.transform, false);
                lampVisual.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            }
        }

        // Struktur Bangunan Luar (Lantai Dasar Beton, Dinding Luar Kiri, Kanan, Belakang, Depan)
        // PENTING SOAL 1: Semua dinding & lantai dasar pinggir TIDAK memiliki TeleportationArea!
        GameObject shellGroup = new GameObject("Workshop_Building_Structure_NonTeleportable");
        shellGroup.transform.SetParent(rootLighting.transform, false);

        Material floorMat = LoadMat("Assets/10_K3_Simulation/Materials/MAT_Floor_Epoxy_Grey.mat", new Color(0.28f, 0.3f, 0.33f));
        Material wallMat = LoadMat("Assets/10_K3_Simulation/Materials/MAT_FactoryWall.mat", new Color(0.75f, 0.77f, 0.8f));
        Material hazardMat = LoadMat("Assets/10_K3_Simulation/Materials/MAT_SafetyYellow.mat", new Color(0.9f, 0.7f, 0.1f));

        // Lantai Dasar Keseluruhan (Non-Teleportable Base Floor di bawah lantai jalan)
        CreateBoxStructure(shellGroup, "BaseFloor_Concrete_NonTeleportable", new Vector3(0f, -0.05f, 16f), new Vector3(12.4f, 0.1f, 40.4f), floorMat);

        // Dinding Luar Barat (X = -6.0)
        CreateBoxStructure(shellGroup, "OuterWall_West_NonTeleportable", new Vector3(-6.0f, 2.0f, 16f), new Vector3(0.3f, 4.0f, 40.2f), wallMat);

        // Dinding Luar Timur (X = +6.0)
        CreateBoxStructure(shellGroup, "OuterWall_East_NonTeleportable", new Vector3(6.0f, 2.0f, 16f), new Vector3(0.3f, 4.0f, 40.2f), wallMat);

        // Dinding Luar Selatan / Belakang Starting Area (Z = -4.0)
        CreateBoxStructure(shellGroup, "OuterWall_South_NonTeleportable", new Vector3(0f, 2.0f, -4.0f), new Vector3(12.2f, 4.0f, 0.3f), wallMat);

        // Dinding Luar Utara / Ujung Machine Area (Z = +36.0)
        CreateBoxStructure(shellGroup, "OuterWall_North_NonTeleportable", new Vector3(0f, 2.0f, 36.0f), new Vector3(12.2f, 4.0f, 0.3f), wallMat);

        // Lis Kuning K3 di kaki dinding luar
        CreateBoxStructure(shellGroup, "SafetySkirt_West", new Vector3(-5.82f, 0.15f, 16f), new Vector3(0.05f, 0.3f, 40f), hazardMat);
        CreateBoxStructure(shellGroup, "SafetySkirt_East", new Vector3(5.82f, 0.15f, 16f), new Vector3(0.05f, 0.3f, 40f), hazardMat);
    }

    // ========================================================================================
    // SISTEM PLAYER VR & SIMULATOR (SOAL 1)
    // ========================================================================================
    private static TeleportationProvider BuildVRPlayerAndSimulator(GameObject rootPlayer)
    {
        string xrHandsPath = "Assets/Samples/XR Interaction Toolkit/3.6.1/Hands Interaction Demo/Prefabs/XR Origin Hands (XR Rig).prefab";
        string xrStdPath = "Assets/Samples/XR Interaction Toolkit/3.6.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";

        GameObject xrPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(xrHandsPath);
        if (xrPrefab == null) xrPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(xrStdPath);

        GameObject xrOriginObj = (GameObject)PrefabUtility.InstantiatePrefab(xrPrefab);
        xrOriginObj.name = "XR Origin (VR Player)";
        xrOriginObj.tag = "Player";
        xrOriginObj.transform.SetParent(rootPlayer.transform, false);
        xrOriginObj.transform.position = new Vector3(0f, 0.05f, -1.8f);
        xrOriginObj.transform.rotation = Quaternion.identity;
        xrOriginObj.transform.localScale = Vector3.one;

        // Pastikan CharacterController terpasang dan dikonfigurasi agar tidak menembus objek/dinding
        CharacterController cc = xrOriginObj.GetComponent<CharacterController>();
        if (cc == null) cc = xrOriginObj.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.3f;
        cc.center = new Vector3(0f, 0.9f, 0f);
        cc.stepOffset = 0.25f;
        cc.slopeLimit = 45f;

        // Pasang VRWorkshopBoundaryGuard untuk proteksi ekstra anti-tembus saat menggerakkan HMD di Simulator
        if (xrOriginObj.GetComponent<VRWorkshopBoundaryGuard>() == null)
        {
            xrOriginObj.AddComponent<VRWorkshopBoundaryGuard>();
        }

        // Pastikan Main Camera mempertahankan tag "MainCamera" agar Camera.main pada XR Interaction Simulator berfungsi
        XROrigin xrOriginComp = xrOriginObj.GetComponent<XROrigin>();
        if (xrOriginComp != null && xrOriginComp.Camera != null)
        {
            GameObject camObj = xrOriginComp.Camera.gameObject;
            camObj.tag = "MainCamera";
        }

        // Instantiate XR Interaction Simulator di root scene (agar DontDestroyOnLoad tidak memunculkan warning)
        string simPath = "Assets/Samples/XR Interaction Toolkit/3.6.1/XR Interaction Simulator/XR Interaction Simulator.prefab";
        GameObject simPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(simPath);
        if (simPrefab != null)
        {
            GameObject simObj = (GameObject)PrefabUtility.InstantiatePrefab(simPrefab);
            simObj.name = "XR Interaction Simulator";
            simObj.transform.SetParent(null);
        }

        return xrOriginObj.GetComponentInChildren<TeleportationProvider>(true);
    }

    // ========================================================================================
    // AREA 1: STARTING AREA (Z: -4 s/d +6)
    // ========================================================================================
    private static void BuildStartingArea(GameObject rootWorkshop, TeleportationProvider tpProvider)
    {
        GameObject area1 = new GameObject("Area_01_StartingArea");
        area1.transform.SetParent(rootWorkshop.transform, false);

        Material walkMat = LoadMat("Assets/10_K3_Simulation/Materials/MAT_GreenWalkway.mat", new Color(0.15f, 0.55f, 0.3f));

        // Lantai Jalan yang DAPAT Diteleport (Hanya jalur tengah & area berdiri aman)
        CreateTeleportableFloor(area1, "TeleportFloor_StartingArea", new Vector3(0f, 0.01f, 1.0f), new Vector3(6.5f, 0.02f, 8.8f), walkMat, tpProvider);

        // Papan Nama Area 1
        CreateAreaHeaderBanner(area1, "Banner_StartingArea", new Vector3(0f, 3.1f, 5.6f),
            "AREA 1: STARTING AREA\n[Gunakan Teleport pada Jalur Hijau | Dekati Pintu untuk Membuka]", new Color(0.1f, 0.45f, 0.8f));

        // Objek Lingkungan Statis di Starting Area (Memiliki Collider, TANPA TeleportationArea, TANPA XRGrabInteractable)
        GameObject propsGroup = new GameObject("Environment_Props_StartingArea");
        propsGroup.transform.SetParent(area1.transform, false);

        SpawnStaticEnvironmentPrefab(propsGroup, "Assets/10_K3_Simulation/Prefabs/Prefab_Locker_Bank_6.prefab",
            "Locker_Station_Left", new Vector3(-4.8f, 0f, 0.5f), Quaternion.Euler(0f, 90f, 0f), new Vector3(2.2f, 2.0f, 0.8f));

        SpawnStaticEnvironmentPrefab(propsGroup, "Assets/10_K3_Simulation/Protection_suite/Prefab/Mesh_protective suit.prefab",
            "APD_Hazmat_Display_Right", new Vector3(4.5f, 0f, 0.5f), Quaternion.Euler(0f, -90f, 0f), new Vector3(1.0f, 2.0f, 1.0f));

        SpawnStaticEnvironmentPrefab(propsGroup, "Assets/10_K3_Simulation/WarehousePack/Prefabs/URP/BlankAFrameSign.prefab",
            "CautionSign_Start", new Vector3(-2.4f, 0f, 2.5f), Quaternion.Euler(0f, 25f, 0f), new Vector3(0.7f, 1.1f, 0.7f));

        SpawnStaticEnvironmentPrefab(propsGroup, "Assets/10_K3_Simulation/Prefabs/Prefab_WarningSignPost.prefab",
            "WarningSignPost_Start", new Vector3(2.4f, 0f, 2.5f), Quaternion.Euler(0f, -25f, 0f), new Vector3(0.7f, 1.6f, 0.7f));
    }

    // ========================================================================================
    // AREA 2: EQUIPMENT AREA (Z: +6 s/d +16) - SOAL 2 (MINIMAL 5 OBJEK GRAB + OBJEK NON-GRAB)
    // ========================================================================================
    private static void BuildEquipmentArea(GameObject rootWorkshop, TeleportationProvider tpProvider)
    {
        GameObject area2 = new GameObject("Area_02_EquipmentArea");
        area2.transform.SetParent(rootWorkshop.transform, false);

        Material walkMat = LoadMat("Assets/10_K3_Simulation/Materials/MAT_GreenWalkway.mat", new Color(0.15f, 0.55f, 0.3f));

        // Lantai Jalan yang DAPAT Diteleport di Equipment Area
        CreateTeleportableFloor(area2, "TeleportFloor_EquipmentArea_Center", new Vector3(0f, 0.01f, 11.0f), new Vector3(3.2f, 0.02f, 8.8f), walkMat, tpProvider);
        CreateTeleportableFloor(area2, "TeleportFloor_EquipmentArea_LeftBay", new Vector3(-2.6f, 0.01f, 9.2f), new Vector3(2.4f, 0.02f, 2.0f), walkMat, tpProvider);
        CreateTeleportableFloor(area2, "TeleportFloor_EquipmentArea_RightBay", new Vector3(2.6f, 0.01f, 9.2f), new Vector3(2.4f, 0.02f, 2.0f), walkMat, tpProvider);

        // Papan Nama Area 2
        CreateAreaHeaderBanner(area2, "Banner_EquipmentArea", new Vector3(0f, 3.1f, 15.6f),
            "AREA 2: EQUIPMENT AREA\n[Meja Kiri: 6 Peralatan Dapat Diambil (Grab) | Meja Kanan: Objek Lingkungan (Tidak Dapat Diambil)]", new Color(0.8f, 0.45f, 0.1f));

        // 1. Meja Peralatan & Rak (Memiliki Collider, TIDAK memiliki TeleportationArea)
        GameObject furnitureGroup = new GameObject("Workbenches_And_Racks_NonTeleportable");
        furnitureGroup.transform.SetParent(area2.transform, false);

        Material tableMat = LoadMat("Assets/10_K3_Simulation/Materials/MAT_MachineMetal_Dark.mat", new Color(0.22f, 0.26f, 0.3f));

        // Meja Utama Peralatan (Kiri: X = -2.8, Z = 11.2) -> Permukaan meja di Y = 0.85f
        GameObject mainToolTable = CreateIndustrialWorkbench(furnitureGroup, "Workbench_01_InteractableTools",
            new Vector3(-2.8f, 0f, 11.2f), new Vector3(2.8f, 0.85f, 1.1f), tableMat,
            "MEJA PERALATAN KERJA (DAPAT DIAMBIL / GRAB)\nAmbil & Bawa Alat ke Tool Check Area (Area 3)");

        // Meja Kedua untuk Objek Lingkungan yang TIDAK Dapat Diambil (Kanan: X = +2.8, Z = 11.2)
        GameObject staticPropTable = CreateIndustrialWorkbench(furnitureGroup, "Workbench_02_StaticEnvironmentProps",
            new Vector3(2.8f, 0f, 11.2f), new Vector3(2.4f, 0.85f, 1.1f), tableMat,
            "OBJEK LINGKUNGAN (TIDAK DAPAT DIAMBIL)\nMemiliki Collider Fisik Tanpa XRGrabInteractable");

        // Rak Besi Gudang di Sisi Dinding
        SpawnStaticEnvironmentPrefab(furnitureGroup, "Assets/10_K3_Simulation/WarehousePack/Prefabs/URP/BoltlessShelvingUnit.prefab",
            "StorageRack_LeftWall", new Vector3(-5.1f, 0f, 11.2f), Quaternion.Euler(0f, 90f, 0f), new Vector3(1.8f, 2.0f, 0.8f));

        // 2. SOAL 2: Minimal 5 Objek Peralatan yang Dapat Diambil, Dipindahkan, dan Dilepaskan (Primitive Unity Murni)
        GameObject toolsGroup = new GameObject("Interactable_Physical_Tools_6_Items");
        toolsGroup.transform.SetParent(area2.transform, false);

        // Kategori 1: Hand Tools (Bentuk CUBE / BOX -> Tag: HandTool)
        CreatePrimitiveGrabbableTool(toolsGroup, "Tool_Box_Red", "HandTool", PrimitiveType.Cube,
            new Vector3(-4.0f, 1.0f, 11.2f), new Vector3(0.24f, 0.24f, 0.24f), new Color(0.95f, 0.2f, 0.2f));

        CreatePrimitiveGrabbableTool(toolsGroup, "Tool_Box_Orange", "HandTool", PrimitiveType.Cube,
            new Vector3(-3.5f, 1.0f, 11.2f), new Vector3(0.22f, 0.22f, 0.22f), new Color(1.0f, 0.55f, 0.1f));

        // Kategori 2: Power Tools (Bentuk SPHERE / BOLA -> Tag: PowerTool)
        CreatePrimitiveGrabbableTool(toolsGroup, "Tool_Sphere_Blue", "PowerTool", PrimitiveType.Sphere,
            new Vector3(-3.0f, 1.0f, 11.2f), new Vector3(0.25f, 0.25f, 0.25f), new Color(0.15f, 0.55f, 1.0f));

        CreatePrimitiveGrabbableTool(toolsGroup, "Tool_Sphere_Cyan", "PowerTool", PrimitiveType.Sphere,
            new Vector3(-2.5f, 1.0f, 11.2f), new Vector3(0.22f, 0.22f, 0.22f), new Color(0.1f, 0.9f, 0.9f));

        // Kategori 3: Measuring Tools (Bentuk CAPSULE & CYLINDER -> Tag: MeasuringTool)
        CreatePrimitiveGrabbableTool(toolsGroup, "Tool_Capsule_Yellow", "MeasuringTool", PrimitiveType.Capsule,
            new Vector3(-2.0f, 1.0f, 11.2f), new Vector3(0.18f, 0.24f, 0.18f), new Color(1.0f, 0.85f, 0.1f));

        CreatePrimitiveGrabbableTool(toolsGroup, "Tool_Cylinder_Green", "MeasuringTool", PrimitiveType.Cylinder,
            new Vector3(-1.5f, 1.0f, 11.2f), new Vector3(0.20f, 0.15f, 0.20f), new Color(0.2f, 0.85f, 0.35f));

        // 3. SOAL 2: Minimal 1 Objek Lingkungan dalam Jangkauan Controller yang TIDAK Dapat Diambil (Primitive Unity Statis)
        GameObject nonGrabGroup = new GameObject("NonGrabbable_Environment_Objects");
        nonGrabGroup.transform.SetParent(area2.transform, false);

        // Tempatkan 1 objek lingkungan statis di ujung meja kiri (dalam jangkauan langsung tangan/controller)
        CreatePrimitiveStaticEnvObject(nonGrabGroup, "Static_Env_FirstAidBox", PrimitiveType.Cube,
            new Vector3(-4.55f, 0.98f, 11.2f), new Vector3(0.35f, 0.22f, 0.25f), new Color(0.9f, 0.9f, 0.95f));

        // Tempatkan 2 objek lingkungan statis tambahan di meja kanan (juga dalam jangkauan controller)
        CreatePrimitiveStaticEnvObject(nonGrabGroup, "Static_Env_SteelPillar", PrimitiveType.Cylinder,
            new Vector3(2.2f, 1.20f, 11.2f), new Vector3(0.35f, 0.70f, 0.35f), new Color(0.35f, 0.4f, 0.45f));

        CreatePrimitiveStaticEnvObject(nonGrabGroup, "Static_Env_SupplyUnit", PrimitiveType.Cube,
            new Vector3(3.3f, 1.08f, 11.2f), new Vector3(0.50f, 0.45f, 0.45f), new Color(0.45f, 0.32f, 0.22f));
    }

    // ========================================================================================
    // AREA 3: TOOL CHECK AREA (Z: +16 s/d +26) - SOAL 1 & SOAL 3 (3 KATEGORI TOOLS)
    // ========================================================================================
    private static void BuildToolCheckArea(GameObject rootWorkshop, TeleportationProvider tpProvider)
    {
        GameObject area3 = new GameObject("Area_03_ToolCheckArea");
        area3.transform.SetParent(rootWorkshop.transform, false);

        Material walkMat = LoadMat("Assets/10_K3_Simulation/Materials/MAT_GreenWalkway.mat", new Color(0.15f, 0.55f, 0.3f));

        // Lantai Jalan yang DAPAT Diteleport di Tool Check Area (Mengelilingi 3 meja inspeksi)
        CreateTeleportableFloor(area3, "TeleportFloor_ToolCheck_SouthWalkway", new Vector3(0f, 0.01f, 18.2f), new Vector3(9.5f, 0.02f, 3.2f), walkMat, tpProvider);
        CreateTeleportableFloor(area3, "TeleportFloor_ToolCheck_NorthWalkway", new Vector3(0f, 0.01f, 23.8f), new Vector3(9.5f, 0.02f, 3.2f), walkMat, tpProvider);
        CreateTeleportableFloor(area3, "TeleportFloor_ToolCheck_LeftPass", new Vector3(-1.8f, 0.01f, 21.0f), new Vector3(1.4f, 0.02f, 2.6f), walkMat, tpProvider);
        CreateTeleportableFloor(area3, "TeleportFloor_ToolCheck_RightPass", new Vector3(1.8f, 0.01f, 21.0f), new Vector3(1.4f, 0.02f, 2.6f), walkMat, tpProvider);

        // Papan Nama Area 3
        CreateAreaHeaderBanner(area3, "Banner_ToolCheckArea", new Vector3(0f, 3.1f, 25.6f),
            "AREA 3: TOOL CHECK AREA\n[Letakkan Alat Sesuai Kategori untuk Menampilkan Teks Verifikasi]", new Color(0.15f, 0.6f, 0.45f));

        // 3 Zona / Meja Kategori Pemeriksaan (Hand Tools, Power Tools, Measuring Tools)
        BuildToolCheckStation(area3,
            stationName: "Zone_01_HandTools_CheckStation",
            categoryTitle: "KATEGORI 1: HAND TOOLS",
            requiredTag: "HandTool",
            stationPos: new Vector3(-3.6f, 0f, 21.0f),
            padColor: new Color(0.95f, 0.35f, 0.15f),
            verifiedMessage: "✓ [INFORMASI PEMERIKSAAN]\nPERALATAN TELAH SESUAI!\nKategori: HAND TOOLS (Valid)");

        BuildToolCheckStation(area3,
            stationName: "Zone_02_PowerTools_CheckStation",
            categoryTitle: "KATEGORI 2: POWER TOOLS",
            requiredTag: "PowerTool",
            stationPos: new Vector3(0.0f, 0f, 21.0f),
            padColor: new Color(0.15f, 0.65f, 0.95f),
            verifiedMessage: "✓ [INFORMASI PEMERIKSAAN]\nPERALATAN TELAH SESUAI!\nKategori: POWER TOOLS (Valid)");

        GameObject station3 = BuildToolCheckStation(area3,
            stationName: "Zone_03_MeasuringTools_CheckStation",
            categoryTitle: "KATEGORI 3: MEASURING TOOLS",
            requiredTag: "MeasuringTool",
            stationPos: new Vector3(3.6f, 0f, 21.0f),
            padColor: new Color(0.2f, 0.85f, 0.35f),
            verifiedMessage: "✓ [INFORMASI PEMERIKSAAN]\nPERALATAN TELAH SESUAI!\nKategori: MEASURING TOOLS (Valid)");

        // Simpan salah satu station sebagai Prefab di Assets/UTS_ARVR/Prefabs/
        PrefabUtility.SaveAsPrefabAsset(station3, $"{PrefabsFolder}/Prefab_UTS_ToolCheckStation.prefab");
    }

    private static GameObject BuildToolCheckStation(
        GameObject parent,
        string stationName,
        string categoryTitle,
        string requiredTag,
        Vector3 stationPos,
        Color padColor,
        string verifiedMessage)
    {
        GameObject station = new GameObject(stationName);
        station.transform.SetParent(parent.transform, false);
        station.transform.position = stationPos;

        Material tableMat = LoadMat("Assets/10_K3_Simulation/Materials/MAT_MachineMetal_Dark.mat", new Color(0.2f, 0.23f, 0.27f));
        Material padMat = CreateCustomMaterial($"Mat_UTS_Pad_{requiredTag}", padColor);

        // 1. Meja Fisik (Memiliki BoxCollider solid, TANPA TeleportationArea)
        GameObject table = CreateBoxStructure(station, "Inspection_Table_Body", new Vector3(0f, 0.425f, 0f), new Vector3(1.8f, 0.85f, 1.0f), tableMat);

        // 2. Alas Visual Kategori di Atas Meja
        GameObject pad = CreateBoxStructure(station, "Inspection_Pad_Surface", new Vector3(0f, 0.86f, 0f), new Vector3(1.5f, 0.02f, 0.8f), padMat);

        // 3. Papan Label Kategori (Selalu Tampil agar Pengguna Tahu Meja Kategori Apa)
        CreateFloatingSign(station, "Category_Header_Sign", new Vector3(0f, 1.32f, 0.42f),
            $"{categoryTitle}\nLetakkan Alat Ber-Tag '{requiredTag}' di Meja Ini", Color.white, padColor, 0.045f);

        // 4. Objek UI Teks Informasi Pemeriksaan (infoText) -> PADA KONDISI AWAL TIDAK DITAMPILKAN (SetActive(false))
        GameObject infoTextObj = new GameObject($"InfoText_{requiredTag}");
        infoTextObj.transform.SetParent(station.transform, false);
        infoTextObj.transform.localPosition = new Vector3(0f, 1.85f, 0.42f);

        // Panel Background Hijau Terang untuk InfoText
        Material validBgMat = CreateCustomMaterial("Mat_UTS_ValidBannerBg", new Color(0.05f, 0.45f, 0.15f));
        GameObject bgPanel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bgPanel.name = "InfoBanner_Background";
        bgPanel.transform.SetParent(infoTextObj.transform, false);
        bgPanel.transform.localPosition = Vector3.zero;
        bgPanel.transform.localScale = new Vector3(1.75f, 0.42f, 0.04f);
        Renderer bgRend = bgPanel.GetComponent<Renderer>();
        if (bgRend != null) bgRend.sharedMaterial = validBgMat;
        Object.DestroyImmediate(bgPanel.GetComponent<Collider>());

        // Teks Depan & Belakang agar terbaca dari segala arah
        Create3DTextChild(infoTextObj, "Text_Front", new Vector3(0f, 0f, -0.03f), Quaternion.identity, verifiedMessage, Color.yellow, 0.042f);
        Create3DTextChild(infoTextObj, "Text_Back", new Vector3(0f, 0f, 0.03f), Quaternion.Euler(0f, 180f, 0f), verifiedMessage, Color.yellow, 0.042f);

        // WAJIB SOAL 3: Pada kondisi awal, infoText tidak ditampilkan (nonaktif)
        infoTextObj.SetActive(false);

        // 5. Trigger Area Pemeriksaan (BoxCollider isTrigger = true + Skrip ToolCheckArea.cs)
        GameObject triggerObj = new GameObject($"TriggerArea_{requiredTag}");
        triggerObj.transform.SetParent(station.transform, false);
        triggerObj.transform.localPosition = new Vector3(0f, 1.12f, 0f);

        BoxCollider triggerCol = triggerObj.AddComponent<BoxCollider>();
        triggerCol.isTrigger = true;
        triggerCol.size = new Vector3(1.55f, 0.55f, 0.85f);

        ToolCheckArea checkScript = triggerObj.AddComponent<ToolCheckArea>();
        checkScript.infoText = infoTextObj;
        checkScript.targetToolTag = requiredTag;
        checkScript.categoryName = categoryTitle;

        return station;
    }

    // ========================================================================================
    // AREA 4: MACHINE AREA (Z: +26 s/d +36) - SOAL 1
    // ========================================================================================
    private static void BuildMachineArea(GameObject rootWorkshop, TeleportationProvider tpProvider)
    {
        GameObject area4 = new GameObject("Area_04_MachineArea");
        area4.transform.SetParent(rootWorkshop.transform, false);

        Material walkMat = LoadMat("Assets/10_K3_Simulation/Materials/MAT_GreenWalkway.mat", new Color(0.15f, 0.55f, 0.3f));
        Material hazardFloorMat = LoadMat("Assets/10_K3_Simulation/Materials/MAT_KeepClear.mat", new Color(0.8f, 0.6f, 0.1f));

        // Lantai Jalan yang DAPAT Diteleport (Hanya Koridor Aman di Tengah Machine Area)
        CreateTeleportableFloor(area4, "TeleportFloor_MachineArea_SafeWalkway", new Vector3(0f, 0.01f, 30.8f), new Vector3(3.4f, 0.02f, 8.4f), walkMat, tpProvider);

        // Lantai Zona Mesin Kiri & Kanan (Memiliki Collider, TIDAK memiliki TeleportationArea!)
        CreateBoxStructure(area4, "MachineBayFloor_Left_NonTeleportable", new Vector3(-3.8f, 0.01f, 31.0f), new Vector3(3.8f, 0.02f, 8.8f), hazardFloorMat);
        CreateBoxStructure(area4, "MachineBayFloor_Right_NonTeleportable", new Vector3(3.8f, 0.01f, 31.0f), new Vector3(3.8f, 0.02f, 8.8f), hazardFloorMat);

        // Papan Nama Area 4
        CreateAreaHeaderBanner(area4, "Banner_MachineArea", new Vector3(0f, 3.1f, 35.4f),
            "AREA 4: MACHINE AREA\n[Zona Mesin Industri Berat - Teleportasi Hanya Diizinkan di Jalur Hijau Tengah]", new Color(0.8f, 0.2f, 0.2f));

        // Mesin-mesin Industri (Semua memiliki BoxCollider solid, TANPA TeleportationArea)
        GameObject machinesGroup = new GameObject("Industrial_Machines_NonTeleportable");
        machinesGroup.transform.SetParent(area4.transform, false);

        SpawnStaticEnvironmentPrefab(machinesGroup, "Assets/10_K3_Simulation/Industrial_Machine_Models/Prefabs/Machine_1.prefab",
            "Machine_01_CNC_Milling", new Vector3(-3.8f, 0f, 28.8f), Quaternion.Euler(0f, 90f, 0f), new Vector3(2.2f, 2.2f, 1.8f));

        SpawnStaticEnvironmentPrefab(machinesGroup, "Assets/10_K3_Simulation/Industrial_Machine_Models/Prefabs/Machine_2.prefab",
            "Machine_02_Industrial_Lathe", new Vector3(-3.8f, 0f, 33.0f), Quaternion.Euler(0f, 90f, 0f), new Vector3(2.2f, 2.2f, 1.8f));

        SpawnStaticEnvironmentPrefab(machinesGroup, "Assets/10_K3_Simulation/Prefabs/Prefab_MachinePress.prefab",
            "Machine_03_Hydraulic_Press", new Vector3(3.8f, 0f, 28.8f), Quaternion.Euler(0f, -90f, 0f), new Vector3(2.0f, 2.4f, 1.8f));

        SpawnStaticEnvironmentPrefab(machinesGroup, "Assets/10_K3_Simulation/Industrial_Machine_Models/Prefabs/Machine_4.prefab",
            "Machine_04_Turbine_Generator", new Vector3(3.8f, 0f, 33.0f), Quaternion.Euler(0f, -90f, 0f), new Vector3(2.2f, 2.2f, 1.8f));

        SpawnStaticEnvironmentPrefab(machinesGroup, "Assets/10_K3_Simulation/Low Poly Forklift/Prefabs/Forklift.prefab",
            "Machine_05_Heavy_Forklift", new Vector3(0f, 0f, 34.3f), Quaternion.Euler(0f, 180f, 0f), new Vector3(1.6f, 2.1f, 2.4f));

        // Pagar Pembatas K3 di Sisi Jalur Mesin
        SpawnStaticEnvironmentPrefab(machinesGroup, "Assets/10_K3_Simulation/Prefabs/Prefab_SafetyBarrier.prefab",
            "SafetyBarrier_Left_1", new Vector3(-2.0f, 0f, 28.8f), Quaternion.Euler(0f, 90f, 0f), new Vector3(2.0f, 1.1f, 0.2f));
        SpawnStaticEnvironmentPrefab(machinesGroup, "Assets/10_K3_Simulation/Prefabs/Prefab_SafetyBarrier.prefab",
            "SafetyBarrier_Right_1", new Vector3(2.0f, 0f, 28.8f), Quaternion.Euler(0f, 90f, 0f), new Vector3(2.0f, 1.1f, 0.2f));
    }

    // ========================================================================================
    // SOAL 4: DINDING SEKAT & 3 PINTU OTOMATIS PENGHUBUNG 4 AREA WORKSHOP
    // ========================================================================================
    private static void BuildConnectingWallsAndDoors(
        GameObject rootDoors,
        AnimatorController doorController,
        TeleportationProvider tpProvider)
    {
        // 3 Sekat Pembatas untuk 4 Area:
        // Pintu 1 (Z = 6.0): Menyambungkan Area 1 (Starting Area) <-> Area 2 (Equipment Area)
        // Pintu 2 (Z = 16.0): Menyambungkan Area 2 (Equipment Area) <-> Area 3 (Tool Check Area)
        // Pintu 3 (Z = 26.0): Menyambungkan Area 3 (Tool Check Area) <-> Area 4 (Machine Area)

        BuildPartitionWallWithAutoDoor(rootDoors, "DoorSystem_01_Starting_To_Equipment", 6.0f,
            "PINTU OTOMATIS 1\nStarting Area <-> Equipment Area", doorController, tpProvider, false);

        BuildPartitionWallWithAutoDoor(rootDoors, "DoorSystem_02_Equipment_To_ToolCheck", 16.0f,
            "PINTU OTOMATIS 2\nEquipment Area <-> Tool Check Area", doorController, tpProvider, false);

        GameObject door3 = BuildPartitionWallWithAutoDoor(rootDoors, "DoorSystem_03_ToolCheck_To_Machine", 26.0f,
            "PINTU OTOMATIS 3\nTool Check Area <-> Machine Area", doorController, tpProvider, true);
    }

    private static GameObject BuildPartitionWallWithAutoDoor(
        GameObject parent,
        string systemName,
        float zPos,
        string doorTitle,
        AnimatorController doorController,
        TeleportationProvider tpProvider,
        bool saveAsPrefab)
    {
        GameObject doorSystem = new GameObject(systemName);
        doorSystem.transform.SetParent(parent.transform, false);
        doorSystem.transform.position = new Vector3(0f, 0f, zPos);

        Material wallMat = LoadMat("Assets/10_K3_Simulation/Materials/MAT_Wall_Interior_Dividing.mat", new Color(0.68f, 0.72f, 0.75f));
        Material frameMat = LoadMat("Assets/10_K3_Simulation/Materials/MAT_MachineMetal_Dark.mat", new Color(0.18f, 0.2f, 0.22f));
        Material doorLeafMat = LoadMat("Assets/04_Materials/M_AH_Puerta.mat", new Color(0.48f, 0.3f, 0.16f));
        Material walkMat = LoadMat("Assets/10_K3_Simulation/Materials/MAT_GreenWalkway.mat", new Color(0.15f, 0.55f, 0.3f));

        // 1. Dinding Sekat Kiri, Kanan, dan Atas Pintu (Lebar bukaan pintu = 1.6m, dari X = -0.8 s/d +0.8)
        // Semua segmen dinding memiliki BoxCollider solid TANPA TeleportationArea
        CreateBoxStructure(doorSystem, "PartitionWall_Left_NonTeleportable", new Vector3(-3.4f, 2.0f, 0f), new Vector3(5.2f, 4.0f, 0.28f), wallMat);
        CreateBoxStructure(doorSystem, "PartitionWall_Right_NonTeleportable", new Vector3(3.4f, 2.0f, 0f), new Vector3(5.2f, 4.0f, 0.28f), wallMat);
        CreateBoxStructure(doorSystem, "PartitionWall_Header_NonTeleportable", new Vector3(0f, 3.2f, 0f), new Vector3(1.6f, 1.6f, 0.28f), wallMat);

        // Kusen Pintu Kiri & Kanan
        CreateBoxStructure(doorSystem, "DoorFrame_PostLeft", new Vector3(-0.78f, 1.2f, 0f), new Vector3(0.08f, 2.4f, 0.32f), frameMat);
        CreateBoxStructure(doorSystem, "DoorFrame_PostRight", new Vector3(0.78f, 1.2f, 0f), new Vector3(0.08f, 2.4f, 0.32f), frameMat);
        CreateBoxStructure(doorSystem, "DoorFrame_TopBar", new Vector3(0f, 2.42f, 0f), new Vector3(1.64f, 0.08f, 0.32f), frameMat);

        // Lantai Ambang Pintu (Teleportable supaya perpindahan antar ruangan mulus)
        GameObject thresholdFloor = CreateTeleportableFloor(doorSystem, "TeleportFloor_DoorThreshold",
            new Vector3(0f, 0.01f, 0f), new Vector3(1.5f, 0.02f, 1.4f), walkMat, tpProvider);
        thresholdFloor.transform.localPosition = new Vector3(0f, 0.01f, 0f);

        // 2. Engsel Pintu (Door_Hinge_Pivot) di sisi kiri kusen (X = -0.74f) -> Memiliki Animator (2 State: Door_Closed & Door_Open)
        GameObject hingePivot = new GameObject("Door_Hinge_Pivot");
        hingePivot.transform.SetParent(doorSystem.transform, false);
        hingePivot.transform.localPosition = new Vector3(-0.74f, 0f, 0f);
        hingePivot.transform.localRotation = Quaternion.identity;

        Animator animator = hingePivot.AddComponent<Animator>();
        animator.runtimeAnimatorController = doorController;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        // Daun Pintu (Door_Leaf) di bawah Door_Hinge_Pivot
        GameObject doorLeaf = CreateBoxStructure(hingePivot, "Door_Leaf_Mesh",
            new Vector3(0.74f, 1.18f, 0f), new Vector3(1.46f, 2.36f, 0.08f), doorLeafMat);

        // Gagang Pintu (Handle)
        CreateBoxStructure(doorLeaf, "Door_Handle_Front", new Vector3(0.38f, 0f, -0.8f), new Vector3(0.04f, 0.12f, 0.8f), frameMat);
        CreateBoxStructure(doorLeaf, "Door_Handle_Back", new Vector3(0.38f, 0f, 0.8f), new Vector3(0.04f, 0.12f, 0.8f), frameMat);

        // Jendela Kaca Kecil pada Daun Pintu
        Material glassMat = LoadMat("Assets/10_K3_Simulation/Protection_suite/Materials/Mat_glass.mat", new Color(0.6f, 0.85f, 0.95f, 0.5f));
        GameObject windowPane = GameObject.CreatePrimitive(PrimitiveType.Cube);
        windowPane.name = "Door_Window_Glass";
        windowPane.transform.SetParent(doorLeaf.transform, false);
        windowPane.transform.localPosition = new Vector3(0f, 0.22f, 0f);
        windowPane.transform.localScale = new Vector3(0.45f, 0.3f, 1.25f);
        Renderer winRend = windowPane.GetComponent<Renderer>();
        if (winRend != null) winRend.sharedMaterial = glassMat;
        Object.DestroyImmediate(windowPane.GetComponent<Collider>());

        // 3. Papan Informasi Pintu
        CreateFloatingSign(doorSystem, "Door_Signboard", new Vector3(0f, 2.72f, -0.18f),
            doorTitle, Color.white, new Color(0.15f, 0.25f, 0.4f), 0.045f);

        // 4. Area Sensor Trigger Pintu Otomatis (BoxCollider isTrigger = true + DoorAutoTrigger.cs)
        GameObject triggerZone = new GameObject("Door_AutoTrigger_Area");
        triggerZone.transform.SetParent(doorSystem.transform, false);
        triggerZone.transform.localPosition = new Vector3(0f, 1.2f, 0f);

        BoxCollider triggerCol = triggerZone.AddComponent<BoxCollider>();
        triggerCol.isTrigger = true;
        triggerCol.size = new Vector3(2.8f, 2.4f, 4.4f);

        DoorAutoTrigger autoTrigger = triggerZone.AddComponent<DoorAutoTrigger>();
        autoTrigger.doorAnimator = animator;
        autoTrigger.boolParameterName = "isOpen";
        autoTrigger.proximityRadius = 2.6f;

        if (saveAsPrefab)
        {
            PrefabUtility.SaveAsPrefabAsset(doorSystem, $"{PrefabsFolder}/Prefab_UTS_AutoDoor.prefab");
        }

        return doorSystem;
    }

    // ========================================================================================
    // HELPER PEMBUATAN KOMPONEN WORKSHOP
    // ========================================================================================
    private static GameObject CreateTeleportableFloor(
        GameObject parent,
        string name,
        Vector3 worldPos,
        Vector3 size,
        Material mat,
        TeleportationProvider tpProvider)
    {
        GameObject floorObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floorObj.name = name;
        floorObj.transform.SetParent(parent.transform, false);
        floorObj.transform.position = worldPos;
        floorObj.transform.localScale = size;

        Renderer rend = floorObj.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.sharedMaterial = mat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        BoxCollider col = floorObj.GetComponent<BoxCollider>();
        if (col == null) col = floorObj.AddComponent<BoxCollider>();
        col.isTrigger = false;

        // SOAL 1: Pasang TeleportationArea HANYA pada lantai jalan
        TeleportationArea tpArea = floorObj.AddComponent<TeleportationArea>();
        tpArea.interactionLayers = -1; // Kompatibel dengan bit layer Teleport Interactor
        if (tpProvider != null)
        {
            tpArea.teleportationProvider = tpProvider;
        }

        return floorObj;
    }

    private static GameObject CreateBoxStructure(
        GameObject parent,
        string name,
        Vector3 localPos,
        Vector3 scale,
        Material mat)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent.transform, false);
        obj.transform.localPosition = localPos;
        obj.transform.localScale = scale;

        Renderer rend = obj.GetComponent<Renderer>();
        if (rend != null && mat != null)
        {
            rend.sharedMaterial = mat;
        }

        // Memiliki BoxCollider biasa TANPA TeleportationArea
        BoxCollider col = obj.GetComponent<BoxCollider>();
        if (col == null) col = obj.AddComponent<BoxCollider>();
        col.isTrigger = false;

        return obj;
    }

    private static GameObject CreateIndustrialWorkbench(
        GameObject parent,
        string name,
        Vector3 worldPos,
        Vector3 size,
        Material mat,
        string headerTitle)
    {
        GameObject bench = new GameObject(name);
        bench.transform.SetParent(parent.transform, false);
        bench.transform.position = worldPos;

        // Meja Fisik (Collider Solid, TANPA TeleportationArea)
        CreateBoxStructure(bench, "Workbench_Body_Collider", new Vector3(0f, size.y * 0.5f, 0f), size, mat);

        return bench;
    }

    private static GameObject CreatePrimitiveGrabbableTool(
        GameObject parent,
        string toolName,
        string toolTag,
        PrimitiveType primitiveType,
        Vector3 worldPos,
        Vector3 localScale,
        Color color)
    {
        // Objek Primitif Murni Bawaan Unity (Cube, Sphere, Capsule, Cylinder)
        GameObject tool = GameObject.CreatePrimitive(primitiveType);
        tool.name = toolName;
        tool.tag = toolTag;
        tool.transform.SetParent(parent.transform, false);
        tool.transform.position = worldPos;
        tool.transform.localScale = localScale;

        Material mat = CreateCustomMaterial($"Mat_{toolName}", color);
        Renderer rend = tool.GetComponent<Renderer>();
        if (rend != null) rend.sharedMaterial = mat;

        // Pastikan Collider bawaan solid (bukan trigger)
        Collider col = tool.GetComponent<Collider>();
        if (col != null) col.isTrigger = false;

        // SOAL 2: Rigidbody (Use Gravity = true, Is Kinematic = false)
        Rigidbody rb = tool.AddComponent<Rigidbody>();
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.mass = 1.0f;
        rb.linearDamping = 0.15f;
        rb.angularDamping = 0.2f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // SOAL 2: XRGrabInteractable (Dapat diambil, dipindahkan, dilepaskan)
        XRGrabInteractable grab = tool.AddComponent<XRGrabInteractable>();
        grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
        grab.throwOnDetach = true;
        grab.useDynamicAttach = true;

        XRInteractionManager mgr = Object.FindFirstObjectByType<XRInteractionManager>();
        if (mgr != null) grab.interactionManager = mgr;

        int grabLayerMask = InteractionLayerMask.GetMask("Default", "Grab");
        if (grabLayerMask == 0) grabLayerMask = 5;
        grab.interactionLayers = grabLayerMask;

        return tool;
    }

    private static GameObject CreatePrimitiveStaticEnvObject(
        GameObject parent,
        string objName,
        PrimitiveType primitiveType,
        Vector3 worldPos,
        Vector3 localScale,
        Color color)
    {
        GameObject obj = GameObject.CreatePrimitive(primitiveType);
        obj.name = objName;
        obj.transform.SetParent(parent.transform, false);
        obj.transform.position = worldPos;
        obj.transform.localScale = localScale;

        Material mat = CreateCustomMaterial($"Mat_{objName}", color);
        Renderer rend = obj.GetComponent<Renderer>();
        if (rend != null) rend.sharedMaterial = mat;

        Collider col = obj.GetComponent<Collider>();
        if (col != null) col.isTrigger = false;

        // Tanpa XRGrabInteractable & Rigidbody (Statis / Tidak dapat diambil)
        return obj;
    }

    private static GameObject SpawnStaticEnvironmentPrefab(
        GameObject parent,
        string prefabPath,
        string objName,
        Vector3 worldPos,
        Quaternion worldRot,
        Vector3 fallbackColliderSize,
        string optionalLabel = null)
    {
        GameObject container = new GameObject(objName);
        container.transform.SetParent(parent.transform, false);
        container.transform.position = worldPos;
        container.transform.rotation = worldRot;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab != null)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "Visual_Mesh";
            instance.transform.SetParent(container.transform, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;

            // Pastikan TIDAK ADA XRGrabInteractable atau TeleportationArea pada objek lingkungan
            foreach (var grab in instance.GetComponentsInChildren<XRBaseInteractable>(true))
                Object.DestroyImmediate(grab);
            foreach (var rb in instance.GetComponentsInChildren<Rigidbody>(true))
                Object.DestroyImmediate(rb);
        }

        // Pasang BoxCollider solid pada container agar tidak dapat ditembus pemain & memblokir ray teleport
        BoxCollider col = container.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, fallbackColliderSize.y * 0.5f, 0f);
        col.size = fallbackColliderSize;
        col.isTrigger = false;

        return container;
    }

    private static void ConfigureXRInteractorLayers(TeleportationProvider tpProvider)
    {
        XRInteractionManager mgr = Object.FindFirstObjectByType<XRInteractionManager>();

        // Pastikan semua TeleportationArea kompatibel dengan layer Teleport Interactor prefab
        var allTpAreas = Object.FindObjectsByType<TeleportationArea>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var tp in allTpAreas)
        {
            tp.interactionLayers = -1;
            if (mgr != null) tp.interactionManager = mgr;
            if (tpProvider != null)
            {
                tp.teleportationProvider = tpProvider;
            }
        }

        var allGrabs = Object.FindObjectsByType<XRGrabInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var g in allGrabs)
        {
            if (mgr != null) g.interactionManager = mgr;
        }
    }

    private static void CreateAreaHeaderBanner(GameObject parent, string name, Vector3 worldPos, string text, Color bannerColor)
    {
        CreateFloatingSign(parent, name, worldPos, text, Color.white, bannerColor, 0.065f);
    }

    private static GameObject CreateFloatingSign(
        GameObject parent,
        string name,
        Vector3 localPos,
        string text,
        Color textColor,
        Color panelColor,
        float textScale)
    {
        GameObject signRoot = new GameObject(name);
        signRoot.transform.SetParent(parent.transform, false);
        signRoot.transform.localPosition = localPos;

        Create3DTextChild(signRoot, "Sign_Text_Front", new Vector3(0f, 0f, -0.02f), Quaternion.identity, text, textColor, textScale);
        Create3DTextChild(signRoot, "Sign_Text_Back", new Vector3(0f, 0f, 0.02f), Quaternion.Euler(0f, 180f, 0f), text, textColor, textScale);

        return signRoot;
    }

    private static GameObject Create3DTextChild(
        GameObject parent,
        string name,
        Vector3 localPos,
        Quaternion localRot,
        string content,
        Color color,
        float scale)
    {
        GameObject txtObj = new GameObject(name);
        txtObj.transform.SetParent(parent.transform, false);
        txtObj.transform.localPosition = localPos;
        txtObj.transform.localRotation = localRot;
        txtObj.transform.localScale = new Vector3(scale, scale, scale);

        TextMesh tm = txtObj.AddComponent<TextMesh>();
        tm.text = content;
        tm.fontSize = 36;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = color;

        return txtObj;
    }

    private static void SetTagRecursively(GameObject obj, string tag)
    {
        if (obj == null) return;
        obj.tag = tag;
        foreach (Transform child in obj.transform)
        {
            SetTagRecursively(child.gameObject, tag);
        }
    }

    private static Material LoadMat(string path, Color fallbackColor)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;
        return CreateCustomMaterial(System.IO.Path.GetFileNameWithoutExtension(path), fallbackColor);
    }

    private static Material CreateCustomMaterial(string matName, Color color)
    {
        string path = $"{PrefabsFolder}/{matName}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Standard");
            mat = new Material(s) { name = matName, color = color };
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
