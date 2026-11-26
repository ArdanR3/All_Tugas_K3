#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

public class SetupVRScenePercobaan1 : EditorWindow
{
    [MenuItem("Tools/VR/★ Rapikan SCN_LabK3_Interaction (Percobaan 1)", false, 0)]
    [MenuItem("Tools/VR/Pasang XR Origin Hands (Input Modality)", false, 1)]
    public static void ExecuteSetup()
    {
        Scene currentScene = EditorSceneManager.GetActiveScene();
        Debug.Log("[VR Setup] Memulai perbaikan pada scene aktif: " + currentScene.name);

        // 1. Cari atau Buat Root 07_PLAYER
        GameObject playerRoot = GameObject.Find("07_PLAYER");
        if (playerRoot == null)
        {
            playerRoot = new GameObject("07_PLAYER");
            Undo.RegisterCreatedObjectUndo(playerRoot, "Create 07_PLAYER");
        }
        playerRoot.transform.position = Vector3.zero;
        playerRoot.transform.rotation = Quaternion.identity;
        playerRoot.transform.localScale = Vector3.one;

        // 2. Nonaktifkan Player_Rig lama dan OBJ_Controller lama agar tidak menutupi kamera VR
        GameObject playerRig = GameObject.Find("Player_Rig");
        if (playerRig != null)
        {
            playerRig.SetActive(false);
            Debug.Log("[VR Setup] Player_Rig lama dinonaktifkan.");
        }

        GameObject objController = GameObject.Find("OBJ_Controller");
        if (objController != null)
        {
            objController.SetActive(false);
            Debug.Log("[VR Setup] OBJ_Controller (kapsul putih) dinonaktifkan.");
        }

        // 3. Rapikan atau Pasang XR Origin Hands (XR Rig)
        GameObject xrHands = GameObject.Find("XR Origin Hands (XR Rig)");
        GameObject xrOriginOld = GameObject.Find("XR Origin (XR Rig)");

        string handsPrefabPath = "Assets/Samples/XR Interaction Toolkit/3.6.1/Hands Interaction Demo/Prefabs/XR Origin Hands (XR Rig).prefab";
        GameObject handsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(handsPrefabPath);

        GameObject activeOrigin = xrHands;

        if (activeOrigin == null && handsPrefab != null)
        {
            // Buat instance XR Origin Hands
            activeOrigin = (GameObject)PrefabUtility.InstantiatePrefab(handsPrefab);
            activeOrigin.name = "XR Origin Hands (XR Rig)";
            Undo.RegisterCreatedObjectUndo(activeOrigin, "Instantiate XR Origin Hands");

            // Nonaktifkan XR Origin lama jika ada
            if (xrOriginOld != null)
            {
                xrOriginOld.SetActive(false);
                Debug.Log("[VR Setup] XR Origin (XR Rig) lama dinonaktifkan, digantikan oleh XR Origin Hands (XR Rig).");
            }
        }
        else if (activeOrigin == null)
        {
            activeOrigin = xrOriginOld;
            if (activeOrigin == null)
            {
                var allOrigins = Object.FindObjectsByType<Unity.XR.CoreUtils.XROrigin>(FindObjectsSortMode.None);
                if (allOrigins.Length > 0)
                {
                    activeOrigin = allOrigins[0].gameObject;
                }
            }
        }

        if (activeOrigin != null)
        {
            // Pindahkan langsung di bawah 07_PLAYER
            activeOrigin.transform.SetParent(playerRoot.transform, false);

            // Reset Scale ke (1, 1, 1) agar tidak gepeng
            activeOrigin.transform.localScale = Vector3.one;

            // Letakkan di lantai Lab K3 menghadap ke ruangan
            activeOrigin.transform.position = new Vector3(-24.5f, 0.05f, 2.5f);
            activeOrigin.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            activeOrigin.SetActive(true);
            Debug.Log("[VR Setup] " + activeOrigin.name + " berhasil diposisikan di 07_PLAYER dengan scale (1,1,1).");
        }
        else
        {
            Debug.LogWarning("[VR Setup] XR Origin tidak ditemukan!");
        }

        // 4. Pastikan XR Interaction Simulator ada di scene
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
                Debug.Log("[VR Setup] XR Interaction Simulator ditambahkan ke scene.");
            }
        }

        // 5. PERCOBAAN 1 Bagian C: Menyiapkan Teleportation Area
        GameObject env = GameObject.Find("01_ENVIRONMENT");
        GameObject teleportRoot = GameObject.Find("TELEPORTATION_AREAS");
        if (teleportRoot == null)
        {
            teleportRoot = new GameObject("TELEPORTATION_AREAS");
            if (env != null) teleportRoot.transform.SetParent(env.transform, false);
            Undo.RegisterCreatedObjectUndo(teleportRoot, "Create TELEPORTATION_AREAS");
        }

        CreateTeleportArea(teleportRoot, "Teleport_Floor_MachineBay", new Vector3(-22f, 0.02f, 0f), new Vector3(0.5f, 1f, 0.5f));
        CreateTeleportArea(teleportRoot, "Teleport_Floor_Welding", new Vector3(-16f, 0.02f, -2f), new Vector3(0.4f, 1f, 0.4f));
        CreateTeleportArea(teleportRoot, "Teleport_Floor_Warehouse", new Vector3(2f, 0.02f, -2f), new Vector3(0.5f, 1f, 0.5f));

        // 6. PERCOBAAN 1 Bagian D: Menyiapkan Objek Grab (XR Grab Interactable)
        GameObject interactablesRoot = GameObject.Find("INTERACTABLE_OBJECTS");
        if (interactablesRoot == null)
        {
            interactablesRoot = new GameObject("INTERACTABLE_OBJECTS");
            if (env != null) interactablesRoot.transform.SetParent(env.transform, false);
            Undo.RegisterCreatedObjectUndo(interactablesRoot, "Create INTERACTABLE_OBJECTS");
        }

        // Meja inspeksi di depan posisi player
        GameObject table = GameObject.Find("VR_Inspection_Table");
        if (table == null)
        {
            table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name = "VR_Inspection_Table";
            table.transform.SetParent(interactablesRoot.transform, false);
            table.transform.position = new Vector3(-24.5f, 0.45f, 1.2f);
            table.transform.localScale = new Vector3(1.6f, 0.9f, 0.8f);
        }

        CreateGrabObject(interactablesRoot, PrimitiveType.Cube, "Grab_Cube", new Vector3(-24.9f, 0.98f, 1.2f), new Vector3(0.18f, 0.18f, 0.18f), Color.red);
        CreateGrabObject(interactablesRoot, PrimitiveType.Sphere, "Grab_Sphere", new Vector3(-24.5f, 0.98f, 1.2f), new Vector3(0.2f, 0.2f, 0.2f), Color.cyan);
        CreateGrabObject(interactablesRoot, PrimitiveType.Cylinder, "Grab_Cylinder", new Vector3(-24.1f, 0.98f, 1.2f), new Vector3(0.15f, 0.2f, 0.15f), Color.yellow);

        // 7. Simpan Scene
        EditorSceneManager.MarkSceneDirty(currentScene);
        EditorSceneManager.SaveScene(currentScene);

        Debug.Log("==========================================================================");
        Debug.Log("🎉 [VR Setup] SELESAI! Scene SCN_LabK3_Interaction sudah dirapikan!");
        Debug.Log("👉 1. XR Origin sudah langsung di bawah 07_PLAYER dengan scale (1,1,1).");
        Debug.Log("👉 2. Player_Rig dan OBJ_Controller sudah dinonaktifkan (kamera tidak terhalang).");
        Debug.Log("👉 3. XR Interaction Simulator siap aktif saat tombol Play (▶) ditekan.");
        Debug.Log("👉 4. Area Teleportation & 3 Objek Grab (Cube, Sphere, Cylinder) sudah siap diuji.");
        Debug.Log("==========================================================================");
    }

    private static void CreateTeleportArea(GameObject parent, string name, Vector3 pos, Vector3 scale)
    {
        Transform existing = parent.transform.Find(name);
        GameObject plane;
        if (existing == null)
        {
            plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = name;
            plane.transform.SetParent(parent.transform, false);
        }
        else
        {
            plane = existing.gameObject;
        }

        plane.transform.position = pos;
        plane.transform.localScale = scale;

        var teleArea = plane.GetComponent<TeleportationArea>();
        if (teleArea == null)
        {
            teleArea = plane.AddComponent<TeleportationArea>();
        }

        var rend = plane.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }

    private static void CreateGrabObject(GameObject parent, PrimitiveType type, string name, Vector3 pos, Vector3 scale, Color col)
    {
        Transform existing = parent.transform.Find(name);
        GameObject obj;
        if (existing == null)
        {
            obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.SetParent(parent.transform, false);
        }
        else
        {
            obj = existing.gameObject;
        }

        obj.transform.position = pos;
        obj.transform.localScale = scale;

        var rend = obj.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.color = col;
        }

        var rb = obj.GetComponent<Rigidbody>();
        if (rb == null) rb = obj.AddComponent<Rigidbody>();
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.mass = 1.0f;

        var grab = obj.GetComponent<XRGrabInteractable>();
        if (grab == null) grab = obj.AddComponent<XRGrabInteractable>();
        grab.movementType = XRBaseInteractable.MovementType.Kinematic;
    }
}
#endif
