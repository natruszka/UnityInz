using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BatchBuild
{
    private static string _assetBundleDirectory = "Assets/StreamingAssets/AssetBundles"; 
    private static readonly string ConfigPath = "Assets/StreamingAssets/ConfigurationData/AssetBundleConfig.json";
    public static void BuildAssetBundles()
    {
        string[] args = Environment.GetCommandLineArgs();
        string buildName = null;
        string assetsPath = null;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-buildName" && i + 1 < args.Length)
            {
                buildName = args[i + 1];
            }
        }
        
        if (string.IsNullOrEmpty(buildName))
        {
            Console.WriteLine("Build name is not provided. Use -buildName <guid>.");
            EditorApplication.Exit(1);
            return;
        }
        assetsPath = Path.Combine("Assets/Uploads", buildName);
        if (!Directory.Exists(assetsPath))
        {
            Console.WriteLine("Asset bundle directory does not exist.");
            EditorApplication.Exit(1);
            return;
        }

        AssetBundleBuild assetBundle = new();
        assetBundle.assetBundleName = buildName;
        assetBundle.assetNames = GetAssets(assetsPath).ToArray();
        BuildAssetBundlesParameters buildAssetBundlesParameters = new()
        {
            outputPath = _assetBundleDirectory,
            bundleDefinitions = new[] { assetBundle },
            options = BuildAssetBundleOptions.None,
            targetPlatform = BuildTarget.Android
        };
        
        string json = "{\"buildName\":\""+buildName+"\"}";
        using (StreamWriter writer = new StreamWriter(ConfigPath, false))
        {
            writer.Write(json);
        }
        
        AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(buildAssetBundlesParameters);
        if (manifest)
        {
            foreach(var bundleName in manifest.GetAllAssetBundles())
            {
                string projectRelativePath = buildAssetBundlesParameters.outputPath + "/" + bundleName;
                Console.WriteLine($"Size of AssetBundle {projectRelativePath} is {new FileInfo(projectRelativePath).Length}");
            }
        }
        else
        {
            Console.WriteLine("Build failed, see Console and Editor log for details");
            EditorApplication.Exit(1);
        }
        
        Console.WriteLine("AssetBundles built at: " + _assetBundleDirectory);
    }

    public static void BuildApk()
    {
        string[] scenes = {
            "Assets/Scenes/SceneWithLoader.unity"
        };
        string[] args = Environment.GetCommandLineArgs();
        string buildName = null;
        string buildPath = null;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-buildName" && i + 1 < args.Length)
            {
                buildName = args[i + 1];
            }
        }
        
        if (string.IsNullOrEmpty(buildName))
        {
            Debug.LogError("Build name is not provided. Use -buildName <guid>.");
            EditorApplication.Exit(1);
            return;
        }
        buildPath = Path.Combine("Builds", "Android", buildName + ".apk");
        
        BuildPlayerOptions buildOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = buildPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };
        
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23; 
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);

        BuildReport report = BuildPipeline.BuildPlayer(buildOptions);

        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log("APK built successfully: " + buildPath);
        }
        else
        {
            Debug.LogError("APK build failed.");
            EditorApplication.Exit(1);
        }
    }
    static List<string> GetAssets(string path)
    {
        List<string> assets = new();
        foreach (var f in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            if (Path.GetExtension(f) != ".meta" &&
                Path.GetExtension(f) != ".cs" && 
                Path.GetExtension(f) != ".unity")
                assets.Add(f);
        return assets;
    }
}