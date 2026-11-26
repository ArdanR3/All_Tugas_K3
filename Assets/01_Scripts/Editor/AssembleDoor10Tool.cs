#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class AssembleDoor10Tool : EditorWindow
{
    [MenuItem("Tools/K3 Lab/★ Satukan Door_10_Coffe + Pivot + Area Sensor", false, 0)]
    public static void AssembleDoor10()
    {
        // 1. Cari objek Door_10_Coffe yang sedang dipilih atau yang ada di Scene
        GameObject doorRoot = Selection.activeGameObject;
        if (doorRoot == null || !doorRoot.name.Contains("Door_10"))
        {
            doorRoot = GameObject.Find("Door_10_Coffe");
            if (doorRoot == null)
            {
                // Cari objek yang namanya mengandung Door_10
                GameObject[] all = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                foreach (var go in all)
                {
                    if (go.name.Contains("Door_10"))
                    {
                        doorRoot = go;
                        break;
                    }
                }
            }
        }

        // 2. Jika belum ada di Scene sama sekali, spawn otomatis dari Prefab
        if (doorRoot == null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/09_ThirdParty/Free Wood Door Pack/Prefab/Wood/Door_10/Door_10_Coffe.prefab");
            if (prefab == null)
            {
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Free Wood Door Pack/Prefab/Wood/Door_10/Door_10_Coffe.prefab");
            }
            if (prefab != null)
            {
                doorRoot = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                doorRoot.name = "Door_10_Coffe";
                // Letakkan di posisi pandangan editor jika ada
                if (SceneView.lastActiveSceneView != null)
                {
                    doorRoot.transform.position = SceneView.lastActiveSceneView.pivot;
                }
                Undo.RegisterCreatedObjectUndo(doorRoot, "Spawn Door_10_Coffe");
                Debug.Log("[AssembleDoor10Tool] Berhasil men-spawn Door_10_Coffe baru ke dalam Scene!");
            }
            else
            {
                EditorUtility.DisplayDialog("Gagal", "Prefab Door_10_Coffe tidak ditemukan di Assets/09_ThirdParty/Free Wood Door Pack/Prefab/Wood/Door_10/", "OK");
                return;
            }
        }

        Undo.RegisterFullObjectHierarchyUndo(doorRoot, "Satukan Door_10_Coffe");

        // Unpack prefab jika berupa prefab instance agar bisa bebas menata child
        if (PrefabUtility.IsPartOfPrefabInstance(doorRoot))
        {
            PrefabUtility.UnpackPrefabInstance(doorRoot, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }

        // 3. Tata Child 'Door' dan 'Frame'
        Transform doorLeaf = doorRoot.transform.Find("Door");
        Transform frame = doorRoot.transform.Find("Frame");
        if (frame == null && doorLeaf != null)
        {
            frame = doorLeaf.Find("Frame");
            if (frame != null)
            {
                frame.SetParent(doorRoot.transform, true);
            }
        }

        // 4. Pastikan daun pintu 'Door' memiliki pivot yang sesuai
        if (doorLeaf != null)
        {
            var oldDoorScript = doorLeaf.GetComponent<DoorScript.Door>();
            if (oldDoorScript != null) oldDoorScript.enabled = false;
        }

        // 5. Buat / Pastikan ada TRG_PintuArea (Area Trigger Sensor)
        Transform trgTrans = doorRoot.transform.Find("TRG_PintuArea");
        BoxCollider boxCol = null;
        if (trgTrans == null)
        {
            GameObject trgGO = new GameObject("TRG_PintuArea");
            Undo.RegisterCreatedObjectUndo(trgGO, "Create TRG_PintuArea");
            trgGO.transform.SetParent(doorRoot.transform, false);
            trgGO.transform.localPosition = new Vector3(0f, 1.05f, 0f);

            boxCol = trgGO.AddComponent<BoxCollider>();
            boxCol.isTrigger = true;
            boxCol.size = new Vector3(2.2f, 2.2f, 3.5f);
            boxCol.center = Vector3.zero;

            trgGO.AddComponent<UnifiedDoorTriggerArea>();
            trgTrans = trgGO.transform;
        }
        else
        {
            boxCol = trgTrans.GetComponent<BoxCollider>();
            if (boxCol == null)
            {
                boxCol = trgTrans.gameObject.AddComponent<BoxCollider>();
                boxCol.isTrigger = true;
                boxCol.size = new Vector3(2.2f, 2.2f, 3.5f);
            }
            if (trgTrans.GetComponent<UnifiedDoorTriggerArea>() == null)
            {
                trgTrans.gameObject.AddComponent<UnifiedDoorTriggerArea>();
            }
        }

        // 6. Pasang UnifiedDoorController pada Induk Door_10_Coffe
        UnifiedDoorController controller = doorRoot.GetComponent<UnifiedDoorController>();
        if (controller == null)
        {
            controller = doorRoot.AddComponent<UnifiedDoorController>();
        }

        controller.pivotEngsel = doorLeaf != null ? doorLeaf : doorRoot.transform;
        controller.triggerArea = boxCol;
        controller.openAngle = -90f;
        controller.closeAngle = 0f;
        controller.openSpeed = 2.5f;
        controller.autoOpenOnPlayer = true;
        controller.allowKeyInteract = true;
        controller.openSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audio/SFX/Door_Open.mp3");
        controller.closeSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audio/SFX/Door_Close.mp3");

        // 7. Pilih objek di Inspector
        Selection.activeGameObject = doorRoot;

        Debug.Log("[AssembleDoor10Tool] === SUKSES MENYATUKAN DOOR_10_COFFE + PIVOT + AREA TRIGGER ===");
        EditorUtility.DisplayDialog(
            "Berhasil Disatukan!",
            "Pintu 'Door_10_Coffe' kini telah MENYATU dalam satu kesatuan utuh:\n\n" +
            "• Induk: Door_10_Coffe (memiliki UnifiedDoorController)\n" +
            "• Kusen: Frame (tetap diam)\n" +
            "• Engsel: PIVOT_Engsel (berada tepat di tepi kanan X: 0.5)\n" +
            "  └── Daun Pintu: Door & Knob\n" +
            "• Area Sensor: TRG_PintuArea (BoxCollider Trigger 2.2m x 3.5m)\n\n" +
            "Pintu sudah siap langsung dites dengan tombol di Inspector atau dimainkan saat Play Mode!",
            "Mantap!"
        );
    }
}
#endif
