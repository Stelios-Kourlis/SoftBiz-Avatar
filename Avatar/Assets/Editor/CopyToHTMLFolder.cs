using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;
using UnityEngine;

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
