#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// Builder Otomatis untuk Modul Praktikum AR/VR:
/// M07 / M01 - Sistem Menu Spasial (Spatial UI) 3D di Lab K3
/// Sesuai Kriteria Percobaan & Latihan:
/// 1. World Space & Pencegahan Motion Sickness (World-locked, skala 0.002, jarak 1.5m - 2m).
/// 2. Desain Aksesibilitas (TextMeshPro, kontras tinggi, hit target besar, visual Color Tint feedback).
/// 3. Navigasi Multi-Panel (Minimal 5 panel informasi mesin dengan navigasi 2 halaman: Utama & SOP).
/// 4. Penempatan Bebas Clipping (Posisinya ergonomis, tidak memotong mesin/dinding).
/// 5. Dukungan Interaksi XR & Proximity UI (Ray/Poke Interactor & Proximity Trigger otomatis).
/// </summary>
public static class BuildSpatialUIK3Lab
{
    private const string SCENE_PATH = "Assets/00_Scenes/SCN_LabK3_Interaction.unity";

    public struct MachineStationConfig
    {
        public string id;
        public string machineTitle;
        public string hazardCategory;
        public string areaInfo;
        public string sopText;
        public Vector3 triggerCenter;
        public Vector3 triggerSize;
        public Vector3 uiPosition;
        public Vector3 uiEulerAngles;
    }

    [MenuItem("Tools/K3/★ Bangun Lengkap: Spatial UI & Proximity K3 (Otomatis)", false, 0)]
    public static void BuildCompleteSpatialUIScenario()
    {
        Debug.Log("<color=cyan>[Spatial UI Builder]</color> Memulai pembangunan Sistem Menu Spasial 3D Lab K3...");

        // 1. Pastikan Scene SCN_LabK3_Interaction terbuka
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != SCENE_PATH)
        {
            scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
        }

        // 2. Setup XR Origin: Camera Tag, SphereCollider & Rigidbody untuk Proximity
        Camera mainCam = SetupXROriginMainCamera();

        // 3. Setup EventSystem dengan XRUIInputModule
        SetupEventSystem();

        // 4. Siapkan Root Container UI Spasial di Environment
        GameObject uiRoot = GameObject.Find("09_SPATIAL_UI_SYSTEM");
        if (uiRoot != null)
        {
            Undo.DestroyObjectImmediate(uiRoot);
        }
        uiRoot = new GameObject("09_SPATIAL_UI_SYSTEM");
        Undo.RegisterCreatedObjectUndo(uiRoot, "Create 09_SPATIAL_UI_SYSTEM");

        GameObject env = GameObject.Find("01_ENVIRONMENT");
        if (env != null)
        {
            uiRoot.transform.SetParent(env.transform, false);
        }

        // 5. Muat Font TMP resmi (LiberationSans SDF)
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if (fontAsset == null)
        {
            fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        }

        // 6. Konfigurasi 5 Stasiun Mesin K3 Lengkap
        List<MachineStationConfig> stations = GetStationConfigs();

        int builtCount = 0;
        foreach (var cfg in stations)
        {
            BuildStationSpatialUI(cfg, uiRoot, mainCam, fontAsset);
            builtCount++;
        }

        // 7. Simpan Scene
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("=========================================================================================");
        Debug.Log($"🎉 <color=green><b>[Spatial UI Builder] SUKSES! {builtCount} Panel Menu Spasial 3D Berhasil Dibangun!</b></color>");
        Debug.Log("👉 <b>1. Render Mode:</b> Seluruh Canvas menggunakan World Space, world-locked (tidak child kamera).");
        Debug.Log("👉 <b>2. Skala Ergonomis:</b> Skala Canvas (0.002, 0.002, 0.002) pada ketinggian Y = 1.45m bebas clipping.");
        Debug.Log("👉 <b>3. Multi-Panel (5 Stasiun):</b> CNC Milling, CNC Lathe, Welding Station, Panel Listrik, Forklift Unit.");
        Debug.Log("👉 <b>4. Navigasi 2 Halaman:</b> Halaman Utama (Buka SOP) & Halaman Detail SOP (Kembali ke Utama) dengan Color Tint.");
        Debug.Log("👉 <b>5. Proximity Trigger:</b> Box Collider IsTrigger + ProximityUI.cs aktif otomatis saat pemain mendekat.");
        Debug.Log("👉 <b>6. XR Interaction:</b> TrackedDeviceGraphicRaycaster & XRUIInputModule siap untuk Ray/Poke Interactor.");
        Debug.Log("=========================================================================================");
    }

    private static Camera SetupXROriginMainCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            XROrigin xrOrigin = Object.FindFirstObjectByType<XROrigin>();
            if (xrOrigin != null && xrOrigin.Camera != null)
            {
                cam = xrOrigin.Camera;
            }
            else
            {
                GameObject camGO = GameObject.Find("Main Camera");
                if (camGO != null) cam = camGO.GetComponent<Camera>();
            }
        }

        if (cam != null)
        {
            // Pastikan Tag "MainCamera" terpasang
            if (!cam.CompareTag("MainCamera"))
            {
                cam.gameObject.tag = "MainCamera";
                Debug.Log("[Spatial UI Setup] Tag MainCamera diterapkan pada: " + cam.name);
            }

            // Pastikan Sphere Collider terpasang (Radius = 0.2)
            SphereCollider sc = cam.GetComponent<SphereCollider>();
            if (sc == null)
            {
                sc = Undo.AddComponent<SphereCollider>(cam.gameObject);
            }
            sc.radius = 0.2f;
            sc.isTrigger = false;

            // Pastikan Rigidbody terpasang (isKinematic = true, useGravity = false)
            Rigidbody rb = cam.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = Undo.AddComponent<Rigidbody>(cam.gameObject);
            }
            rb.isKinematic = true;
            rb.useGravity = false;

            Debug.Log("[Spatial UI Setup] Collider & Rigidbody terverifikasi pada Main Camera.");
        }
        else
        {
            Debug.LogWarning("[Spatial UI Setup] Main Camera tidak ditemukan di scene!");
        }

        return cam;
    }

    private static void SetupEventSystem()
    {
        EventSystem es = Object.FindFirstObjectByType<EventSystem>();
        if (es == null)
        {
            GameObject esGO = new GameObject("EventSystem");
            es = esGO.AddComponent<EventSystem>();
            Undo.RegisterCreatedObjectUndo(esGO, "Create EventSystem");
        }

        // Tambahkan XRUIInputModule jika belum ada
        XRUIInputModule xrInput = es.GetComponent<XRUIInputModule>();
        if (xrInput == null)
        {
            xrInput = Undo.AddComponent<XRUIInputModule>(es.gameObject);
            Debug.Log("[Spatial UI Setup] XRUIInputModule ditambahkan ke EventSystem.");
        }

        // Nonaktifkan StandaloneInputModule lama jika ada agar tidak bentrok
        StandaloneInputModule sim = es.GetComponent<StandaloneInputModule>();
        if (sim != null)
        {
            sim.enabled = false;
        }
    }

    private static List<MachineStationConfig> GetStationConfigs()
    {
        return new List<MachineStationConfig>
        {
            // 1. MESIN CNC MILLING
            new MachineStationConfig
            {
                id = "MillingMachine",
                machineTitle = "MESIN CNC MILLING 3-AXIS (MILLING BAY)",
                hazardCategory = "BAHAYA TINGGI: PUTARAN SPINDEL & SERPIHAN (CHIPS)",
                areaInfo = "LOKASI: BENGKEL PEMESINAN BAY 1 | SUPERVISI: INSTRUKTUR K3",
                triggerCenter = new Vector3(-12.0f, 1.5f, 2.0f),
                triggerSize = new Vector3(5.5f, 3.5f, 5.5f),
                uiPosition = new Vector3(-12.0f, 1.45f, 0.8f),
                uiEulerAngles = new Vector3(0f, 0f, 0f),
                sopText = "1. Wajib mengenakan Safety Glasses (ANSI Z87.1), Safety Shoes, dan baju kerja pas badan.\n" +
                          "2. Dilarang memakai sarung tangan kain longgar saat spindel mesin sedang berputar.\n" +
                          "3. Periksa pencekaman benda kerja (vise/clamp) dan pastikan kunci chuck sudah dilepas.\n" +
                          "4. Tutup pintu enclosure kaca pengaman sebelum menekan tombol Cycle Start.\n" +
                          "5. Jika terjadi anomali, getaran hebat, atau pahat patah, tekan tombol EMERGENCY STOP."
            },

            // 2. MESIN CNC TURNING / LATHE
            new MachineStationConfig
            {
                id = "CNCLathe",
                machineTitle = "MESIN BUBUT CNC / TURNING CENTER",
                hazardCategory = "BAHAYA TINGGI: CHUCK ROTASI TINGGI & PANAS GESEKAN",
                areaInfo = "LOKASI: BENGKEL PEMESINAN BAY 2 | SUPERVISI: INSTRUKTUR K3",
                triggerCenter = new Vector3(-7.0f, 1.5f, 7.5f),
                triggerSize = new Vector3(5.5f, 3.5f, 5.5f),
                uiPosition = new Vector3(-7.0f, 1.45f, 6.2f),
                uiEulerAngles = new Vector3(0f, 0f, 0f),
                sopText = "1. Gunakan pelindung mata dan penutup telinga (earplug) saat pembubutan putaran tinggi.\n" +
                          "2. Pastikan benda kerja silindris dicekam seimbang dan tailstock terkunci kokoh.\n" +
                          "3. Jangan membersihkan serpihan geram logam dengan tangan; gunakan kuas atau kait khusus.\n" +
                          "4. Pastikan semprotan cairan pendingin (coolant) terarah tepat ke mata pahat.\n" +
                          "5. Matikan daya utama dan pasang gembok pengaman (LOTO) saat mengganti insert pahat."
            },

            // 3. STASIUN PENGELASAN / WELDING
            new MachineStationConfig
            {
                id = "WeldingStation",
                machineTitle = "STASIUN FABRIKASI & PENGELASAN SMAW/GMAW",
                hazardCategory = "BAHAYA EKSTREM: RADIASI UV/IR, SUHU TINGGI & ASAP TOKSIK",
                areaInfo = "LOKASI: AREA FABRIKASI LAS | SUPERVISI: INSTRUKTUR PENGELASAN",
                triggerCenter = new Vector3(-16.3f, 1.5f, -2.04f),
                triggerSize = new Vector3(6.0f, 3.5f, 6.0f),
                uiPosition = new Vector3(-16.3f, 1.45f, -3.5f),
                uiEulerAngles = new Vector3(0f, 0f, 0f),
                sopText = "1. Wajib memakai Helm Las (Filter Shade 10-12), Apron Kulit, dan Sarung Tangan Las tebal.\n" +
                          "2. Hidupkan sistem exhaust ventilasi penghisap asap sebelum menyalakan busur las.\n" +
                          "3. Tutup tirai pelindung las (Welding Screen) agar radiasi kilatan tidak mengenai pekerja lain.\n" +
                          "4. Pastikan tabung gas pelindung terikat rantai kokoh dan regulator tidak bocor.\n" +
                          "5. Sediakan APAR jenis Powder / CO2 siap pakai di dekat area pengelasan."
            },

            // 4. PANEL DISTRIBUSI LISTRIK
            new MachineStationConfig
            {
                id = "ElectricalPanel",
                machineTitle = "PANEL DISTRIBUSI KELISTRIKAN TEGANGAN MENENGAH",
                hazardCategory = "BAHAYA TINGGI: TEGANGAN LISTRIK & BUSUR API (ARC FLASH)",
                areaInfo = "LOKASI: RUANG DISTRIBUSI DAYA | SUPERVISI: AHLI K3 LISTRIK",
                triggerCenter = new Vector3(8.82f, 1.5f, 8.58f),
                triggerSize = new Vector3(5.0f, 3.5f, 5.0f),
                uiPosition = new Vector3(8.82f, 1.45f, 7.2f),
                uiEulerAngles = new Vector3(0f, 0f, 0f),
                sopText = "1. Hanya teknisi tersertifikasi K3 Listrik yang berwenang membuka pintu panel ini.\n" +
                          "2. Terapkan prosedur LOTO (Lockout / Tagout) sebelum melakukan pemeliharaan sirkuit.\n" +
                          "3. Gunakan sarung tangan isolasi listrik (1000V) dan sepatu dielectric berinsulasi.\n" +
                          "4. Dilarang keras menaruh cairan, benda logam, atau kain lap di atas kabinet panel.\n" +
                          "5. Dalam darurat kebakaran listrik, dilarang menyiram air; gunakan APAR Kelas C (CO2)."
            },

            // 5. UNIT LOGISTIK & FORKLIFT
            new MachineStationConfig
            {
                id = "ForkliftUnit",
                machineTitle = "UNIT LOGISTIK FORKLIFT & MATERIAL HANDLING",
                hazardCategory = "BAHAYA SEDANG: TABRAKAN, TITIK JEPIT & BEBAN JATUH",
                areaInfo = "LOKASI: GUDANG LOGISTIK & PARKING BAY | SUPERVISI: PENGAWAS WAREHOUSE",
                triggerCenter = new Vector3(2.78f, 1.5f, -2.06f),
                triggerSize = new Vector3(6.0f, 3.5f, 6.0f),
                uiPosition = new Vector3(2.78f, 1.45f, -3.5f),
                uiEulerAngles = new Vector3(0f, 0f, 0f),
                sopText = "1. Operator wajib memiliki SIO (Surat Izin Operator) Forklift yang masih aktif.\n" +
                          "2. Bunyikan klakson saat melintasi persimpangan, blind spot, dan dekat pejalan kaki.\n" +
                          "3. Batas kecepatan maksimum di dalam bengkel/gudang adalah 5 km/jam.\n" +
                          "4. Ketinggian garpu angkat saat membawa beban berjalan maksimal 15-20 cm dari lantai.\n" +
                          "5. Parkir di garis kuning khusus, turunkan garpu rata lantai, pasang rem tangan & cabut kunci."
            }
        };
    }

    private static void BuildStationSpatialUI(MachineStationConfig cfg, GameObject parentRoot, Camera eventCamera, TMP_FontAsset font)
    {
        // 1. Buat Objek Station Anchor dengan Trigger Box Collider & ProximityUI
        GameObject stationTriggerGO = new GameObject($"STATION_TRIGGER_{cfg.id}");
        stationTriggerGO.transform.SetParent(parentRoot.transform, false);
        stationTriggerGO.transform.position = cfg.triggerCenter;

        BoxCollider bc = stationTriggerGO.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.center = Vector3.zero;
        bc.size = cfg.triggerSize;

        ProximityUI proximityScript = stationTriggerGO.AddComponent<ProximityUI>();

        // 2. Buat Canvas Spasial Khusus VR
        GameObject canvasGO = new GameObject($"UI_Spatial_{cfg.id}");
        canvasGO.transform.SetParent(parentRoot.transform, false);
        canvasGO.transform.position = cfg.uiPosition;
        canvasGO.transform.rotation = Quaternion.Euler(cfg.uiEulerAngles);
        canvasGO.transform.localScale = new Vector3(0.002f, 0.002f, 0.002f);

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        if (eventCamera != null)
        {
            canvas.worldCamera = eventCamera;
        }

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 4f;

        canvasGO.AddComponent<GraphicRaycaster>();
        canvasGO.AddComponent<TrackedDeviceGraphicRaycaster>();

        RectTransform canvasRT = canvasGO.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(700f, 460f);
        canvasRT.anchoredPosition = Vector2.zero;
        canvasRT.localPosition = cfg.uiPosition;

        // Hubungkan ke ProximityUI agar panel otomatis sembunyi/muncul
        proximityScript.panelUI = canvasGO;

        // 3. Bangun Halaman 1: Panel_Utama
        GameObject panelUtamaGO = CreateUIPanel("Panel_Utama", canvasGO.transform, new Color(0.06f, 0.09f, 0.15f, 0.94f));
        BuildPanelUtamaContent(panelUtamaGO, cfg, font);

        // 4. Bangun Halaman 2: Panel_SOP (Initially Inactive)
        GameObject panelSOPGO = CreateUIPanel("Panel_SOP", canvasGO.transform, new Color(0.07f, 0.10f, 0.16f, 0.95f));
        BuildPanelSOPContent(panelSOPGO, cfg, font);

        // 5. Hubungkan Navigasi Tombol OnClick (SOP <-> Utama)
        Button btnBukaSOP = panelUtamaGO.transform.Find("Btn_InfoSOP")?.GetComponent<Button>();
        Button btnKembali = panelSOPGO.transform.Find("Btn_Kembali")?.GetComponent<Button>();

        if (btnBukaSOP != null && btnKembali != null)
        {
            // Klik "Buka SOP Mesin": Aktifkan Panel_SOP, nonaktifkan Panel_Utama
            UnityEventTools.AddBoolPersistentListener(btnBukaSOP.onClick, panelSOPGO.SetActive, true);
            UnityEventTools.AddBoolPersistentListener(btnBukaSOP.onClick, panelUtamaGO.SetActive, false);

            // Klik "Kembali ke Menu Utama": Aktifkan Panel_Utama, nonaktifkan Panel_SOP
            UnityEventTools.AddBoolPersistentListener(btnKembali.onClick, panelUtamaGO.SetActive, true);
            UnityEventTools.AddBoolPersistentListener(btnKembali.onClick, panelSOPGO.SetActive, false);
        }

        // Awalnya: Panel_SOP nonaktif, Panel_Utama aktif
        panelSOPGO.SetActive(false);
        panelUtamaGO.SetActive(true);

        // Canvas awal mulai disembunyikan oleh ProximityUI saat PlayMode
        // (Tetap aktif di editor agar terlihat saat editing)
    }

    private static GameObject CreateUIPanel(string name, Transform parent, Color bgColor)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);

        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = panel.AddComponent<Image>();
        img.color = bgColor;

        return panel;
    }

    private static void BuildPanelUtamaContent(GameObject panel, MachineStationConfig cfg, TMP_FontAsset font)
    {
        // A. Header Bar
        GameObject headerBar = new GameObject("Header_Bar");
        headerBar.transform.SetParent(panel.transform, false);
        RectTransform headerRT = headerBar.AddComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0f, 1f);
        headerRT.anchorMax = new Vector2(1f, 1f);
        headerRT.pivot = new Vector2(0.5f, 1f);
        headerRT.sizeDelta = new Vector2(0f, 65f);
        headerRT.anchoredPosition = Vector2.zero;
        Image headerImg = headerBar.AddComponent<Image>();
        headerImg.color = new Color(0.12f, 0.35f, 0.65f, 1f);

        // Judul Utama
        GameObject txtJudul = new GameObject("Txt_Judul");
        txtJudul.transform.SetParent(headerBar.transform, false);
        RectTransform tjRT = txtJudul.AddComponent<RectTransform>();
        tjRT.anchorMin = Vector2.zero;
        tjRT.anchorMax = Vector2.one;
        tjRT.offsetMin = new Vector2(15f, 0f);
        tjRT.offsetMax = new Vector2(-15f, 0f);
        TextMeshProUGUI tmpJudul = txtJudul.AddComponent<TextMeshProUGUI>();
        if (font != null) tmpJudul.font = font;
        tmpJudul.text = cfg.machineTitle;
        tmpJudul.fontSize = 24;
        tmpJudul.fontStyle = FontStyles.Bold;
        tmpJudul.alignment = TextAlignmentOptions.Center;
        tmpJudul.color = Color.white;

        // B. Sub Judul K3
        GameObject txtSub = new GameObject("Txt_SubJudul");
        txtSub.transform.SetParent(panel.transform, false);
        RectTransform tsRT = txtSub.AddComponent<RectTransform>();
        tsRT.anchorMin = new Vector2(0f, 1f);
        tsRT.anchorMax = new Vector2(1f, 1f);
        tsRT.pivot = new Vector2(0.5f, 1f);
        tsRT.sizeDelta = new Vector2(-40f, 30f);
        tsRT.anchoredPosition = new Vector2(0f, -75f);
        TextMeshProUGUI tmpSub = txtSub.AddComponent<TextMeshProUGUI>();
        if (font != null) tmpSub.font = font;
        tmpSub.text = "SISTEM KONTROL & INFORMASI LAB K3 - ASTRATECH";
        tmpSub.fontSize = 15;
        tmpSub.fontStyle = FontStyles.Bold;
        tmpSub.alignment = TextAlignmentOptions.Center;
        tmpSub.color = new Color(1f, 0.85f, 0.2f, 1f);

        // C. Box Status & Informasi Bahaya
        GameObject infoBox = new GameObject("Info_Box");
        infoBox.transform.SetParent(panel.transform, false);
        RectTransform ibRT = infoBox.AddComponent<RectTransform>();
        ibRT.anchorMin = new Vector2(0.05f, 0.32f);
        ibRT.anchorMax = new Vector2(0.95f, 0.80f);
        ibRT.offsetMin = Vector2.zero;
        ibRT.offsetMax = Vector2.zero;
        Image ibImg = infoBox.AddComponent<Image>();
        ibImg.color = new Color(0.04f, 0.07f, 0.12f, 0.8f);

        GameObject txtContent = new GameObject("Txt_StatusInfo");
        txtContent.transform.SetParent(infoBox.transform, false);
        RectTransform tcRT = txtContent.AddComponent<RectTransform>();
        tcRT.anchorMin = Vector2.zero;
        tcRT.anchorMax = Vector2.one;
        tcRT.offsetMin = new Vector2(20f, 15f);
        tcRT.offsetMax = new Vector2(-20f, -15f);
        TextMeshProUGUI tmpContent = txtContent.AddComponent<TextMeshProUGUI>();
        if (font != null) tmpContent.font = font;
        tmpContent.text = $"<b>● STATUS SISTEM :</b> <color=#10B981>AKTIF & SIAP OPERASIONAL</color>\n" +
                          $"<b>● TINGKAT RISIKO :</b> <color=#EF4444>{cfg.hazardCategory}</color>\n" +
                          $"<b>● DETAIL AREA    :</b> {cfg.areaInfo}\n\n" +
                          $"<color=#9CA3AF><i>Petunjuk: Pastikan selalu membaca dan mematuhi Prosedur K3 sebelum mengoperasikan mesin ini.</i></color>";
        tmpContent.fontSize = 16;
        tmpContent.lineSpacing = 15;
        tmpContent.color = new Color(0.9f, 0.92f, 0.95f);

        // D. Tombol Buka SOP Mesin (Btn_InfoSOP)
        GameObject btnGO = new GameObject("Btn_InfoSOP");
        btnGO.transform.SetParent(panel.transform, false);
        RectTransform bRT = btnGO.AddComponent<RectTransform>();
        bRT.anchorMin = new Vector2(0.5f, 0.15f);
        bRT.anchorMax = new Vector2(0.5f, 0.15f);
        bRT.pivot = new Vector2(0.5f, 0.5f);
        bRT.sizeDelta = new Vector2(280f, 55f);

        Image btnImg = btnGO.AddComponent<Image>();
        btnImg.color = new Color(0.15f, 0.5f, 0.85f, 1f);

        Button btn = btnGO.AddComponent<Button>();
        btn.transition = Selectable.Transition.ColorTint;
        ColorBlock cb = btn.colors;
        cb.normalColor = new Color(0.15f, 0.5f, 0.85f, 1f);
        cb.highlightedColor = new Color(0.3f, 0.85f, 1f, 1f);
        cb.pressedColor = new Color(0.08f, 0.3f, 0.55f, 1f);
        cb.selectedColor = new Color(0.15f, 0.5f, 0.85f, 1f);
        btn.colors = cb;

        GameObject btnTxt = new GameObject("Text (TMP)");
        btnTxt.transform.SetParent(btnGO.transform, false);
        RectTransform btRT = btnTxt.AddComponent<RectTransform>();
        btRT.anchorMin = Vector2.zero;
        btRT.anchorMax = Vector2.one;
        btRT.offsetMin = Vector2.zero;
        btRT.offsetMax = Vector2.zero;
        TextMeshProUGUI tmpBtn = btnTxt.AddComponent<TextMeshProUGUI>();
        if (font != null) tmpBtn.font = font;
        tmpBtn.text = "Buka SOP Mesin";
        tmpBtn.fontSize = 20;
        tmpBtn.fontStyle = FontStyles.Bold;
        tmpBtn.alignment = TextAlignmentOptions.Center;
        tmpBtn.color = Color.white;
    }

    private static void BuildPanelSOPContent(GameObject panel, MachineStationConfig cfg, TMP_FontAsset font)
    {
        // A. Header Bar SOP (Warna Amber Keselamatan)
        GameObject headerBar = new GameObject("Header_Bar_SOP");
        headerBar.transform.SetParent(panel.transform, false);
        RectTransform headerRT = headerBar.AddComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0f, 1f);
        headerRT.anchorMax = new Vector2(1f, 1f);
        headerRT.pivot = new Vector2(0.5f, 1f);
        headerRT.sizeDelta = new Vector2(0f, 60f);
        headerRT.anchoredPosition = Vector2.zero;
        Image headerImg = headerBar.AddComponent<Image>();
        headerImg.color = new Color(0.85f, 0.45f, 0.1f, 1f);

        GameObject txtJudul = new GameObject("Txt_Judul_SOP");
        txtJudul.transform.SetParent(headerBar.transform, false);
        RectTransform tjRT = txtJudul.AddComponent<RectTransform>();
        tjRT.anchorMin = Vector2.zero;
        tjRT.anchorMax = Vector2.one;
        tjRT.offsetMin = new Vector2(15f, 0f);
        tjRT.offsetMax = new Vector2(-15f, 0f);
        TextMeshProUGUI tmpJudul = txtJudul.AddComponent<TextMeshProUGUI>();
        if (font != null) tmpJudul.font = font;
        tmpJudul.text = "PROSEDUR STANDAR KESELAMATAN KERJA (SOP)";
        tmpJudul.fontSize = 22;
        tmpJudul.fontStyle = FontStyles.Bold;
        tmpJudul.alignment = TextAlignmentOptions.Center;
        tmpJudul.color = Color.white;

        // B. Box Teks SOP Lengkap
        GameObject sopBox = new GameObject("SOP_Content_Box");
        sopBox.transform.SetParent(panel.transform, false);
        RectTransform sbRT = sopBox.AddComponent<RectTransform>();
        sbRT.anchorMin = new Vector2(0.04f, 0.22f);
        sbRT.anchorMax = new Vector2(0.96f, 0.85f);
        sbRT.offsetMin = Vector2.zero;
        sbRT.offsetMax = Vector2.zero;
        Image sbImg = sopBox.AddComponent<Image>();
        sbImg.color = new Color(0.04f, 0.07f, 0.12f, 0.8f);

        GameObject txtSOP = new GameObject("Txt_Prosedur_K3");
        txtSOP.transform.SetParent(sopBox.transform, false);
        RectTransform tsRT = txtSOP.AddComponent<RectTransform>();
        tsRT.anchorMin = Vector2.zero;
        tsRT.anchorMax = Vector2.one;
        tsRT.offsetMin = new Vector2(20f, 12f);
        tsRT.offsetMax = new Vector2(-20f, -12f);
        TextMeshProUGUI tmpSOP = txtSOP.AddComponent<TextMeshProUGUI>();
        if (font != null) tmpSOP.font = font;
        tmpSOP.text = cfg.sopText;
        tmpSOP.fontSize = 16;
        tmpSOP.lineSpacing = 14;
        tmpSOP.color = new Color(0.95f, 0.96f, 0.98f);

        // C. Tombol Kembali ke Menu Utama (Btn_Kembali)
        GameObject btnGO = new GameObject("Btn_Kembali");
        btnGO.transform.SetParent(panel.transform, false);
        RectTransform bRT = btnGO.AddComponent<RectTransform>();
        bRT.anchorMin = new Vector2(0.5f, 0.11f);
        bRT.anchorMax = new Vector2(0.5f, 0.11f);
        bRT.pivot = new Vector2(0.5f, 0.5f);
        bRT.sizeDelta = new Vector2(280f, 50f);

        Image btnImg = btnGO.AddComponent<Image>();
        btnImg.color = new Color(0.25f, 0.3f, 0.38f, 1f);

        Button btn = btnGO.AddComponent<Button>();
        btn.transition = Selectable.Transition.ColorTint;
        ColorBlock cb = btn.colors;
        cb.normalColor = new Color(0.25f, 0.3f, 0.38f, 1f);
        cb.highlightedColor = new Color(0.45f, 0.75f, 0.95f, 1f);
        cb.pressedColor = new Color(0.15f, 0.2f, 0.25f, 1f);
        cb.selectedColor = new Color(0.25f, 0.3f, 0.38f, 1f);
        btn.colors = cb;

        GameObject btnTxt = new GameObject("Text (TMP)");
        btnTxt.transform.SetParent(btnGO.transform, false);
        RectTransform btRT = btnTxt.AddComponent<RectTransform>();
        btRT.anchorMin = Vector2.zero;
        btRT.anchorMax = Vector2.one;
        btRT.offsetMin = Vector2.zero;
        btRT.offsetMax = Vector2.zero;
        TextMeshProUGUI tmpBtn = btnTxt.AddComponent<TextMeshProUGUI>();
        if (font != null) tmpBtn.font = font;
        tmpBtn.text = "Kembali ke Menu Utama";
        tmpBtn.fontSize = 18;
        tmpBtn.fontStyle = FontStyles.Bold;
        tmpBtn.alignment = TextAlignmentOptions.Center;
        tmpBtn.color = Color.white;
    }
}
#endif

