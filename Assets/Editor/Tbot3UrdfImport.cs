using System.IO;
using UnityEditor;
using UnityEngine;
using RosSharp.Urdf.Editor;

// Editor hook so we can run ros-sharp's URDF importer headlessly via the
// MCP "execute menu item" path (the built-in importer uses a file dialog).
public static class Tbot3UrdfImport
{
    [MenuItem("Tools/Import TurtleBot3 Burger")]
    public static void ImportBurger()
    {
        string rel = "Assets/Tbot3/turtlebot3_burger.urdf";
        string full = Path.GetFullPath(rel);
        if (!File.Exists(full))
        {
            Debug.LogError("[Tbot3] URDF not found at " + full);
            return;
        }
        UrdfRobotExtensions.Create(full);
        Debug.Log("[Tbot3] Imported TurtleBot3 Burger from " + rel);
    }
}
