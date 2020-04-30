using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Menus for MaTC
/// </summary>
public class MaTCMenus
{

    /// <summary>
    /// Show MeshAndTextureCombiner
    /// </summary>
    [MenuItem("Tools/MaTC/Mesh and Texture Combiner", false, 0)]
    static void ShowMeshAndTextureCombiner()
    {
        (EditorWindow.GetWindow(typeof(MaTC.MeshAndTextureCombiner)) as MaTC.MeshAndTextureCombiner).Show();
    }

    /// <summary>
    /// Show TextureCombiner
    /// </summary>
    [MenuItem("Tools/MaTC/Texture Combiner", false, 0)]
    static void TextureCombiner()
    {
        (EditorWindow.GetWindow(typeof(MaTC.TextureCombiner)) as MaTC.TextureCombiner).Show();
    }

}