/*********************************************************************************************
 * COMPONENT OF: none (editor only: Tools > Rocketee Man > Build Web Game)
 * REQUIRED DEPENDENCIES: Web Build Support module (Unity Hub > Installs > Add modules),
 *                        RocketeeBuildSettings, the Menu and Play scenes
 * DESCRIPTION: Builds the whole game for the web in one click, with the settings from
 *              "Creating a WebGL Build to Google Drive": Company Name San Domenico School,
 *              Product Name Rocketee Man, Version 1.0, Gzip compression, Decompression
 *              Fallback, scenes Menu (0) and Play (1). The build goes in
 *              Builds/RocketeeMan_BenXiao (next to the Assets folder, not inside it), then the
 *              size is checked (must be under 2 GB) and the folder is opened.
 *              "Build and Run Web Game" also opens the game in your browser (localhost).
 * AUTHOR: Ben Xiao (written with Claude)
 * VERSION: 1.0
 *********************************************************************************************/
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RocketeeGameBuilder
{
    public const string CompanyName = "San Domenico School";
    public const string GameName = "Rocketee Man";
    public const string Version = "1.0";
    // GameName_YourName, no spaces or special characters
    public const string BuildName = "RocketeeMan_BenXiao";

    const long MaxBytes = 2L * 1024 * 1024 * 1024;

    [MenuItem("Tools/Rocketee Man/Build Web Game", false, 0)]
    static void BuildWeb()
    {
        Build(false);
    }

    [MenuItem("Tools/Rocketee Man/Build and Run Web Game", false, 1)]
    static void BuildAndRunWeb()
    {
        Build(true);
    }

    [MenuItem("Tools/Rocketee Man/Open Builds Folder", false, 2)]
    static void OpenBuildsFolder()
    {
        string folder = BuildsFolder();
        Directory.CreateDirectory(folder);
        EditorUtility.RevealInFinder(folder);
    }

    static string BuildsFolder()
    {
        return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Builds");
    }

    static void Build(bool run)
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Rocketee Man", "Stop playing first, then build.", "OK");
            return;
        }

        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
        {
            EditorUtility.DisplayDialog("Rocketee Man",
                "Web Build Support is not installed for this Unity version.\n\n" +
                "Unity Hub > Installs > click the gear next to " + Application.unityVersion +
                " > Add modules > Web Build Support > Install. Then open the project again.", "OK");
            return;
        }

        // Asks to save your scene if it has changes
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        // Scenes in the right order and sharp pixel art
        RocketeeBuildSettings.FixSceneList();
        RocketeeBuildSettings.FixBlurrySprites();

        // Player Settings from the class instructions
        PlayerSettings.companyName = CompanyName;
        PlayerSettings.productName = GameName;
        PlayerSettings.bundleVersion = Version;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;

        string output = Path.Combine(BuildsFolder(), BuildName);
        Directory.CreateDirectory(output);

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { RocketeeBuildSettings.MenuPath, RocketeeBuildSettings.PlayPath },
            locationPathName = output,
            target = BuildTarget.WebGL,
            targetGroup = BuildTargetGroup.WebGL,
            options = run ? BuildOptions.AutoRunPlayer : BuildOptions.None,
        };

        Debug.Log("[Rocketee Man] Building the web game into " + output + " (the first build can take 10-30 minutes)...");
        BuildReport report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result != BuildResult.Succeeded)
        {
            EditorUtility.DisplayDialog("Rocketee Man", "The build did not finish (" + report.summary.result + ").\nLook for red errors in the Console.", "OK");
            return;
        }

        long bytes = FolderSize(output);
        string size = (bytes / (1024f * 1024f)).ToString("0.0") + " MB";
        Debug.Log("[Rocketee Man] Build finished: " + output + " (" + size + ")");

        if (bytes > MaxBytes)
        {
            EditorUtility.DisplayDialog("Rocketee Man",
                "The build is " + size + ", which is over 2 GB.\nLower Max Texture Size in Build Profiles (start with 1024) and build again.", "OK");
            return;
        }

        if (!run)
        {
            EditorUtility.RevealInFinder(output);
            EditorUtility.DisplayDialog("Rocketee Man",
                "Build finished! Size: " + size + " (under 2 GB).\n\n" +
                "Next: drag the folder " + BuildName + " into your class folder in Google Drive, " +
                "share it with Mr. Gustin, and submit the link.", "OK");
        }
    }

    static long FolderSize(string folder)
    {
        long total = 0;
        foreach (string file in Directory.GetFiles(folder, "*", SearchOption.AllDirectories))
            total += new FileInfo(file).Length;
        return total;
    }
}
