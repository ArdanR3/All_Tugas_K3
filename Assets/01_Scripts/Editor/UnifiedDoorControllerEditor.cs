#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UnifiedDoorController))]
public class UnifiedDoorControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        UnifiedDoorController door = (UnifiedDoorController)target;

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("Kontrol Cepat Pintu (Bisa di Edit Mode & Play Mode)", EditorStyles.boldLabel);

        // Tombol Buka / Tutup
        GUI.backgroundColor = door.isOpen ? new Color(1f, 0.6f, 0.6f) : new Color(0.6f, 1f, 0.6f);
        string btnText = door.isOpen ? "■ TUTUP PINTU" : "▶ BUKA PINTU";
        if (GUILayout.Button(btnText, GUILayout.Height(34)))
        {
            Undo.RecordObject(door, "Toggle Unified Door");
            if (door.pivotEngsel != null) Undo.RecordObject(door.pivotEngsel, "Rotate Door Pivot");
            door.ToggleDoor();
            EditorUtility.SetDirty(door);
        }
        GUI.backgroundColor = Color.white;

        // Tombol Periksa / Satukan Struktur
        EditorGUILayout.Space(6);
        if (GUILayout.Button("Periksa & Hubungkan Ulang Komponen Otomatis"))
        {
            door.AutoWireComponents();
            EditorUtility.SetDirty(door);
        }
    }
}
#endif
