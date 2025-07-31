using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;
using UnityEngine;

/// <summary>
/// Copy the WebGL build to Website/UnityWebGL so the site always has the most recent build
/// </summary>
public class CopyToHTMLFolder
{
    [PostProcessBuild]
    public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.WebGL) return;

        // var buildFolder = Path.GetDirectoryName(pathToBuiltProject);
        // Debug.Log($"Build folder: {buildFolder}");
        Debug.Log($"Path To Build: {pathToBuiltProject}");
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        var destFolder = Path.Combine(projectRoot, "Website", "UnityWebGL");

        if (!Directory.Exists(destFolder))
            Directory.CreateDirectory(destFolder);

        if (Directory.Exists(destFolder)) //Clear dest folder contents
        {
            foreach (var file in Directory.GetFiles(destFolder))
            {
                if (file.Equals(".gitkeep")) continue; // Keep .gitkeep file
                File.Delete(file);
            }

            foreach (var dir in Directory.GetDirectories(destFolder))
                Directory.Delete(dir, true); // Delete subdirectories and their contents
        }

        CopyDirectory(pathToBuiltProject, destFolder);

        UnityEngine.Debug.Log($"Copied WebGL build to {destFolder}");
    }

    // Helper method to copy directory recursively
    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        // Directory.CreateDirectory(destinationDir);

        foreach (var filePath in Directory.GetFiles(sourceDir))
        {
            var destFile = Path.Combine(destinationDir, Path.GetFileName(filePath));
            File.Copy(filePath, destFile, true); // overwrite = true
        }

        foreach (var dirPath in Directory.GetDirectories(sourceDir))
        {
            var destSubDir = Path.Combine(destinationDir, Path.GetFileName(dirPath));
            Directory.CreateDirectory(destSubDir);
            CopyDirectory(dirPath, destSubDir);
        }
    }
}
