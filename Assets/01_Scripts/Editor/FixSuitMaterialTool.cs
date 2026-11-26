#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

// [InitializeOnLoad] -- disabled auto execution on startup
public class FixSuitMaterialTool
{
    /* Auto-run constructor disabled to prevent scene modification on domain reload

    //     static FixSuitMaterialTool()
    //     {
    //         EditorApplication.delayCall += () =>
    //         {
    //             ApplySuitMaterialFix();
    //             ApplyElectricalPanelFix();
    //         };
    //     }
    */

    [MenuItem("Tools/K3/Perbaiki Material Protection Suit (URP)", false, 5)]
    public static void ApplySuitMaterialFix()
    {
        string matPath = "Assets/10_K3_Simulation/Protection_suite/Materials/Mat_protective suit.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");

        if (urpLit == null)
        {
            urpLit = Shader.Find("Universal Render Pipeline/Simple Lit");
        }

        if (mat != null && urpLit != null)
        {
            mat.shader = urpLit;

            Texture albedo = AssetDatabase.LoadAssetAtPath<Texture>("Assets/10_K3_Simulation/Protection_suite/Textures/T_Protective suit_AlbedoTransparency.tga");
            Texture normal = AssetDatabase.LoadAssetAtPath<Texture>("Assets/10_K3_Simulation/Protection_suite/Textures/T_Protective suit_Normal.tga");
            Texture metallic = AssetDatabase.LoadAssetAtPath<Texture>("Assets/10_K3_Simulation/Protection_suite/Textures/T_Protective suit_MetallicSmoothness.tga");
            Texture ao = AssetDatabase.LoadAssetAtPath<Texture>("Assets/10_K3_Simulation/Protection_suite/Textures/T_Protective suit_Mixed_AO.tga");

            if (albedo != null)
            {
                mat.SetTexture("_BaseMap", albedo);
                mat.SetTexture("_MainTex", albedo);
            }

            if (normal != null)
            {
                mat.SetTexture("_BumpMap", normal);
                mat.SetFloat("_BumpScale", 1.0f);
                mat.EnableKeyword("_NORMALMAP");
            }

            if (metallic != null)
            {
                mat.SetTexture("_MetallicGlossMap", metallic);
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            }

            if (ao != null)
            {
                mat.SetTexture("_OcclusionMap", ao);
                mat.SetFloat("_OcclusionStrength", 1.0f);
                mat.EnableKeyword("_OCCLUSIONMAP");
            }

            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Smoothness", 0.568f);
            mat.SetFloat("_Cull", 0); // Double-sided (Two-sided)
            mat.SetFloat("_AlphaClip", 1);
            mat.SetFloat("_Cutoff", 0.364f);
            mat.EnableKeyword("_ALPHATEST_ON");

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(matPath, ImportAssetOptions.ForceUpdate);

            // Re-import prefab agar thumbnail dan model di scene langsung refresh
            string prefabPath = "Assets/10_K3_Simulation/Protection_suite/Prefab/Mesh_protective suit.prefab";
            AssetDatabase.ImportAsset(prefabPath, ImportAssetOptions.ForceUpdate);

            Debug.Log("[FixSuit] Sukses! Material 'Mat_protective suit' telah diperbarui ke URP Lit!");
        }
    }

    [MenuItem("Tools/K3/Perbaiki Material Panel Listrik (URP)", false, 6)]
    public static void ApplyElectricalPanelFix()
    {
        string[] prefabs = new string[]
        {
            "Assets/10_K3_Simulation/Electrical_Panel/Prefabs/Shield Metall .prefab",
            "Assets/10_K3_Simulation/Electrical_Panel/Prefabs/Shield Metall rust 2.prefab",
            "Assets/10_K3_Simulation/Electrical_Panel/Prefabs/Shield Metall rust 3.prefab",
            "Assets/10_K3_Simulation/Electrical_Panel/Prefabs/Shield Metall rust 4.prefab",
            "Assets/10_K3_Simulation/Electrical_Panel/Prefabs/Shield Metall rust.prefab",
            "Assets/10_K3_Simulation/Electrical_Panel/Prefabs/Shield Metall Steel.prefab"
        };

        foreach (var p in prefabs)
        {
            AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[FixElectrical] Seluruh Material & Prefab Panel Listrik telah di-refresh ke URP!");
    }
}
#endif
