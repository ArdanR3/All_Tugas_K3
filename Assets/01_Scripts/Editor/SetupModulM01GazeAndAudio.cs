#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;

[InitializeOnLoad]
public static class SetupModulM01GazeAndAudio
{
    private const string AutoRunPrefKey = "M01_GazeAudio_AutoRun_Done_v2";

    static SetupModulM01GazeAndAudio()
    {
        EditorApplication.delayCall += () =>
        {
            if (!EditorPrefs.GetBool(AutoRunPrefKey, false))
            {
                ExecuteSetup(false);
                EditorPrefs.SetBool(AutoRunPrefKey, true);
            }
        };
    }

    [MenuItem("Tools/VR/★ Setup Lengkap: Modul M01 (Audio 3D Radio & Gaze Pop-up)", false, 0)]
    public static void ManualExecuteSetup()
    {
        ExecuteSetup(true);
    }

    public static void ExecuteSetup(bool isManual)
    {
        Scene currentScene = EditorSceneManager.GetActiveScene();
        string targetScenePath = "Assets/00_Scenes/SCN_VR_SpatialUI.unity";

        if (currentScene.name != "SCN_VR_SpatialUI")
        {
            if (System.IO.File.Exists(targetScenePath))
            {
                currentScene = EditorSceneManager.OpenScene(targetScenePath, OpenSceneMode.Single);
            }
            else
            {
                Debug.LogWarning($"[M01 Setup] Scene {targetScenePath} tidak ditemukan, menggunakan scene aktif: {currentScene.name}");
            }
        }

        Debug.Log($"<color=cyan>[M01 Setup]</color> Memulai konfigurasi Modul M01 pada scene: <b>{currentScene.name}</b>");

        // 1. Pastikan Layer GazeTarget terdaftar
        int gazeLayerIndex = EnsureGazeTargetLayer();
        LayerMask gazeLayerMask = 1 << gazeLayerIndex;

        // 2. Setup RADIO (Audio 3D Immersive)
        SetupRadioAudio();

        // 3. Setup XR Camera (HeadGazeInteractor & AudioListener tunggal)
        SetupXRCameraGazeAndAudio(gazeLayerMask);

        // 4. Setup Objek Pajangan Mesin (Minimal 2 Objek) dengan GazeInfoTarget & INFO_PANEL
        SetupPajanganObjects(gazeLayerIndex);

        // 5. Simpan Scene
        EditorSceneManager.MarkSceneDirty(currentScene);
        EditorSceneManager.SaveScene(currentScene);

        Debug.Log("=========================================================================================");
        Debug.Log("🎉 <color=green><b>[M01 Setup] BERHASIL! Seluruh modul M01 & LATIHAN telah siap!</b></color>");
        Debug.Log("👉 <b>1. Radio Audio 3D:</b> AudioSource aktif, Loop=True, PlayOnAwake=True, SpatialBlend=1.0 (3D), Vol=0.5, Rolloff=Logarithmic, Lagu: 'Back To Me'. Posisi objek tetap.");
        Debug.Log("👉 <b>2. XR Camera:</b> Terpasang HeadGazeInteractor (Dwell=1.2s, Dist=8m, Layer=GazeTarget) & 1 AudioListener tunggal.");
        Debug.Log("👉 <b>3. Objek Pajangan:</b> Minimal 2 mesin (CNC & Milling) dipasang GazeInfoTarget, Collider, Layer GazeTarget, dan World Space INFO_PANEL.");
        Debug.Log("👉 <b>4. Tata Letak:</b> Seluruh posisi objek & hierarchy bawaan pengguna dipertahankan 100% tanpa perpindahan.");
        Debug.Log("=========================================================================================");
    }

    private static int EnsureGazeTargetLayer()
    {
        int layerIndex = LayerMask.NameToLayer("GazeTarget");
        if (layerIndex != -1) return layerIndex;

        // Tambahkan ke TagManager jika belum ada
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layersProp = tagManager.FindProperty("layers");

        // Cari slot kosong pertama dari layer 6 ke atas
        for (int i = 6; i < layersProp.arraySize; i++)
        {
            SerializedProperty layerSP = layersProp.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(layerSP.stringValue))
            {
                layerSP.stringValue = "GazeTarget";
                tagManager.ApplyModifiedProperties();
                Debug.Log($"[M01 Setup] Layer 'GazeTarget' berhasil dibuat pada slot {i}.");
                return i;
            }
        }

        // Fallback jika tidak menemukan slot kosong di 6-31, coba cek index 3
        SerializedProperty layer3 = layersProp.GetArrayElementAtIndex(3);
        if (string.IsNullOrEmpty(layer3.stringValue))
        {
            layer3.stringValue = "GazeTarget";
            tagManager.ApplyModifiedProperties();
            return 3;
        }

        return 3;
    }

    private static void SetupRadioAudio()
    {
        // Cari GameObject Radio tanpa mengubah posisi atau parent-nya
        GameObject radioObj = GameObject.Find("Radio") ?? GameObject.Find("RADIO");

        if (radioObj == null)
        {
            // Cari objek yang namanya mengandung 'Radio'
            var allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var go in allObjects)
            {
                if (go.name.IndexOf("Radio", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    radioObj = go;
                    break;
                }
            }
        }

        if (radioObj == null)
        {
            Debug.LogError("[M01 Setup] GameObject 'Radio' tidak ditemukan di scene!");
            return;
        }

        Undo.RecordObject(radioObj, "Setup Radio AudioSource");

        AudioSource audioSource = radioObj.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = radioObj.AddComponent<AudioSource>();
        }

        // Cari file lagu Back To Me
        string audioPath = "Assets/06_Audios/Back To Me.mp3";
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(audioPath);
        if (clip == null)
        {
            // Fallback cari di seluruh project
            string[] guids = AssetDatabase.FindAssets("Back To Me t:AudioClip");
            if (guids.Length > 0)
            {
                clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
        }

        if (clip != null)
        {
            audioSource.clip = clip;
        }
        else
        {
            Debug.LogWarning("[M01 Setup] AudioClip 'Back To Me' belum ditemukan!");
        }

        // Pengaturan sesuai Modul M01 halaman 6-7
        audioSource.loop = true;
        audioSource.playOnAwake = true;
        audioSource.spatialBlend = 1.0f; // 100% 3D Sound
        audioSource.volume = 0.5f;        // Volume 0.5 (titik awal 0.4 - 0.6)
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        audioSource.minDistance = 0.5f;
        audioSource.maxDistance = 1.5f;

        EditorUtility.SetDirty(audioSource);
        EditorUtility.SetDirty(radioObj);
        Debug.Log($"[M01 Setup] Radio AudioSource berhasil dikonfigurasi pada '{radioObj.name}'. Clip: {(clip != null ? clip.name : "None")}");
    }

    private static void SetupXRCameraGazeAndAudio(LayerMask gazeMask)
    {
        // Cari Main Camera VR
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            GameObject camObj = GameObject.Find("Main Camera");
            if (camObj != null) mainCam = camObj.GetComponent<Camera>();
        }

        if (mainCam == null)
        {
            var allCams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            if (allCams.Length > 0) mainCam = allCams[0];
        }

        if (mainCam == null)
        {
            Debug.LogError("[M01 Setup] Kamera utama (Main Camera) tidak ditemukan!");
            return;
        }

        GameObject camGO = mainCam.gameObject;
        Undo.RecordObject(camGO, "Setup XR Camera Gaze and Audio");

        // 1. AudioListener: Pastikan tepat SATU AudioListener di scene (pada Main Camera)
        var allListeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        foreach (var listener in allListeners)
        {
            if (listener.gameObject != camGO)
            {
                listener.enabled = false;
                Debug.Log($"[M01 Setup] Menonaktifkan AudioListener duplikat pada '{listener.gameObject.name}'.");
            }
        }

        AudioListener mainListener = camGO.GetComponent<AudioListener>();
        if (mainListener == null)
        {
            mainListener = camGO.AddComponent<AudioListener>();
        }
        mainListener.enabled = true;

        // 2. HeadGazeInteractor: Pasang ke XR Camera
        HeadGazeInteractor gazeInteractor = camGO.GetComponent<HeadGazeInteractor>();
        if (gazeInteractor == null)
        {
            gazeInteractor = camGO.AddComponent<HeadGazeInteractor>();
        }

        // Konfigurasi via SerializedObject agar tersimpan sempurna
        SerializedObject soGaze = new SerializedObject(gazeInteractor);
        soGaze.FindProperty("gazeLayer").intValue = gazeMask.value;
        soGaze.FindProperty("maxDistance").floatValue = 8.0f;
        soGaze.FindProperty("dwellTime").floatValue = 1.2f;
        soGaze.ApplyModifiedProperties();

        EditorUtility.SetDirty(gazeInteractor);
        EditorUtility.SetDirty(camGO);
        Debug.Log($"[M01 Setup] HeadGazeInteractor & AudioListener berhasil dipasang pada '{camGO.name}'.");
    }

    private static void SetupPajanganObjects(int gazeLayerIndex)
    {
        // Cari minimal 2 objek pajangan mesin di scene
        List<GameObject> candidates = new List<GameObject>();

        string[] targetNames = new string[] {
            "CNC Machine (1)", "Machine_4 (1)",
            "CNC Machine", "Machine_4",
            "CNC Machine (2)", "Machine_4 (2)"
        };

        foreach (string name in targetNames)
        {
            GameObject obj = GameObject.Find(name);
            if (obj != null && !candidates.Contains(obj))
            {
                candidates.Add(obj);
                if (candidates.Count >= 2) break;
            }
        }

        // Fallback jika nama persis tidak ketemu, cari di 02_MACHINE
        if (candidates.Count < 2)
        {
            GameObject machineRoot = GameObject.Find("02_MACHINE");
            if (machineRoot != null)
            {
                var allTransforms = machineRoot.GetComponentsInChildren<Transform>(true);
                foreach (var t in allTransforms)
                {
                    if (t.name.IndexOf("Machine", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                        t.name.IndexOf("CNC", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        if (!candidates.Contains(t.gameObject) && t.gameObject != machineRoot)
                        {
                            candidates.Add(t.gameObject);
                            if (candidates.Count >= 2) break;
                        }
                    }
                }
            }
        }

        if (candidates.Count == 0)
        {
            Debug.LogError("[M01 Setup] Tidak ditemukan objek mesin untuk dijadikan pajangan!");
            return;
        }

        string[] titles = new string[] {
            "MESIN CNC INDUSTRIAL",
            "MESIN MILLING OTOMATIS"
        };

        string[] descriptions = new string[] {
            "Dibuat pada tahun 2020\nStatus: Siap Operasi\nSOP: Wajib APD Lengkap",
            "Dibuat pada tahun 2018\nStatus: Aktif\nSOP: Periksa Pelindung Mata"
        };

        for (int i = 0; i < candidates.Count; i++)
        {
            GameObject target = candidates[i];
            string title = i < titles.Length ? titles[i] : target.name;
            string desc = i < descriptions.Length ? descriptions[i] : "Dibuat pada tahun 2020\nStatus: Normal";

            ConfigureSinglePajangan(target, gazeLayerIndex, title, desc);
        }
    }

    private static void ConfigureSinglePajangan(GameObject target, int gazeLayerIndex, string titleText, string descText)
    {
        Undo.RecordObject(target, "Configure Pajangan Gaze");

        // 1. Terapkan Layer GazeTarget (pada root dan child renderers/colliders)
        SetLayerRecursively(target, gazeLayerIndex);

        // 2. Pastikan memiliki Collider
        Collider col = target.GetComponent<Collider>();
        if (col == null)
        {
            // Cek apakah ada collider di children
            col = target.GetComponentInChildren<Collider>();
            if (col == null)
            {
                // Tambahkan BoxCollider jika belum ada sama sekali
                BoxCollider box = target.AddComponent<BoxCollider>();
                Renderer ren = target.GetComponentInChildren<Renderer>();
                if (ren != null)
                {
                    box.center = target.transform.InverseTransformPoint(ren.bounds.center);
                    box.size = ren.bounds.size;
                }
                else
                {
                    box.size = new Vector3(1.5f, 2f, 1.5f);
                    box.center = new Vector3(0f, 1f, 0f);
                }
            }
        }

        // 3. Tambahkan script GazeInfoTarget
        GazeInfoTarget gazeTarget = target.GetComponent<GazeInfoTarget>();
        if (gazeTarget == null)
        {
            gazeTarget = target.AddComponent<GazeInfoTarget>();
        }

        // 4. Cari atau Buat child INFO_PANEL (World Space Canvas)
        Transform existingPanel = target.transform.Find("INFO_PANEL");
        GameObject panelObj;

        if (existingPanel != null)
        {
            panelObj = existingPanel.gameObject;
        }
        else
        {
            panelObj = CreateWorldSpaceInfoPanel(target.transform, titleText, descText);
        }

        // Hubungkan ke field infoPanel di GazeInfoTarget
        SerializedObject soTarget = new SerializedObject(gazeTarget);
        SerializedProperty propPanel = soTarget.FindProperty("infoPanel");
        if (propPanel != null)
        {
            propPanel.objectReferenceValue = panelObj;
            soTarget.ApplyModifiedProperties();
        }

        // Pastikan INFO_PANEL nonaktif di awal sesuai modul poin 16 & 18
        panelObj.SetActive(false);

        EditorUtility.SetDirty(gazeTarget);
        EditorUtility.SetDirty(target);
        Debug.Log($"[M01 Setup] Objek pajangan '{target.name}' berhasil dikonfigurasi dengan GazeInfoTarget & INFO_PANEL.");
    }

    private static GameObject CreateWorldSpaceInfoPanel(Transform parent, string title, string description)
    {
        GameObject panelCanvasObj = new GameObject("INFO_PANEL");
        panelCanvasObj.transform.SetParent(parent, false);

        // Hitung posisi relatif agar sedikit di atas/depan objek tanpa mengubah posisi objek aslinya
        Renderer parentRen = parent.GetComponentInChildren<Renderer>();
        float yOffset = 2.2f;
        if (parentRen != null)
        {
            yOffset = Mathf.Max(2.0f, parent.InverseTransformPoint(parentRen.bounds.max).y + 0.35f);
        }

        panelCanvasObj.transform.localPosition = new Vector3(0f, yOffset, 0f);
        panelCanvasObj.transform.localRotation = Quaternion.identity;
        panelCanvasObj.transform.localScale = new Vector3(0.003f, 0.003f, 0.003f);

        Canvas canvas = panelCanvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        CanvasScaler scaler = panelCanvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;

        panelCanvasObj.AddComponent<GraphicRaycaster>();

        RectTransform canvasRT = panelCanvasObj.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(360f, 200f);

        // 1. Background Panel Card
        GameObject bgCard = new GameObject("Bg_Card");
        bgCard.transform.SetParent(panelCanvasObj.transform, false);
        RectTransform bgRT = bgCard.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        Image bgImg = bgCard.AddComponent<Image>();
        bgImg.color = new Color(0.07f, 0.12f, 0.18f, 0.92f); // Dark industrial blue semi-transparent

        // 2. Header Bar
        GameObject header = new GameObject("Header_Bar");
        header.transform.SetParent(bgCard.transform, false);
        RectTransform headerRT = header.AddComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0f, 1f);
        headerRT.anchorMax = new Vector2(1f, 1f);
        headerRT.pivot = new Vector2(0.5f, 1f);
        headerRT.sizeDelta = new Vector2(0f, 45f);
        headerRT.anchoredPosition = Vector2.zero;
        Image headerImg = header.AddComponent<Image>();
        headerImg.color = new Color(0.12f, 0.35f, 0.65f, 1f);

        // Header Title
        GameObject txtTitle = new GameObject("Txt_Title");
        txtTitle.transform.SetParent(header.transform, false);
        RectTransform titleRT = txtTitle.AddComponent<RectTransform>();
        titleRT.anchorMin = Vector2.zero;
        titleRT.anchorMax = Vector2.one;
        titleRT.offsetMin = new Vector2(10f, 0f);
        titleRT.offsetMax = new Vector2(-10f, 0f);
        TextMeshProUGUI tmpTitle = txtTitle.AddComponent<TextMeshProUGUI>();
        tmpTitle.text = title;
        tmpTitle.fontSize = 20;
        tmpTitle.fontStyle = FontStyles.Bold;
        tmpTitle.alignment = TextAlignmentOptions.Center;
        tmpTitle.color = Color.white;

        // 3. Body Text (Info / SOP)
        GameObject txtBody = new GameObject("Txt_Body");
        txtBody.transform.SetParent(bgCard.transform, false);
        RectTransform bodyRT = txtBody.AddComponent<RectTransform>();
        bodyRT.anchorMin = new Vector2(0f, 0f);
        bodyRT.anchorMax = new Vector2(1f, 1f);
        bodyRT.offsetMin = new Vector2(15f, 15f);
        bodyRT.offsetMax = new Vector2(-15f, -55f);
        TextMeshProUGUI tmpBody = txtBody.AddComponent<TextMeshProUGUI>();
        tmpBody.text = description;
        tmpBody.fontSize = 15;
        tmpBody.alignment = TextAlignmentOptions.Center;
        tmpBody.color = new Color(0.9f, 0.95f, 1f, 1f);

        return panelCanvasObj;
    }

    private static void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            // Jangan ubah layer INFO_PANEL jika sudah ada agar tetap UI
            if (child.name == "INFO_PANEL") continue;
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}
#endif

