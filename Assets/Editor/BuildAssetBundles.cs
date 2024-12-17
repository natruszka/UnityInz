using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class BatchBuild
{
    private static string _assetBundleDirectory = "Assets/StreamingAssets/AssetBundles"; 
    private static readonly string _configPath = "Assets/StreamingAssets/AssetBundles/AssetBundleConfig.json";
    public static void BuildAssetBundles()
    {
        string[] args = Environment.GetCommandLineArgs();
        string assetsPath = null;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-assetsPath" && i + 1 < args.Length)
            {
                assetsPath = args[i + 1];
            }
        }

        if (string.IsNullOrEmpty(assetsPath))
        {
            Debug.LogError("Asset bundle path is not provided. Use -assetsPath <path>.");
            return;
        }
        
        if (!Directory.Exists(assetsPath))
        {
            Debug.LogError("Asset bundle directory does not exist.");
            return;
        }

        AssetBundleBuild assetBundle = new();
        assetBundle.assetBundleName = "Assets";
        assetBundle.assetNames = GetAssets(assetsPath).ToArray();
        BuildAssetBundlesParameters buildAssetBundlesParameters = new()
        {
            outputPath = _assetBundleDirectory,
            bundleDefinitions = new[] { assetBundle },
            options = BuildAssetBundleOptions.None,
            targetPlatform = BuildTarget.StandaloneWindows
        };
        
        AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(buildAssetBundlesParameters);
        if (manifest)
        {
            foreach(var bundleName in manifest.GetAllAssetBundles())
            {
                string projectRelativePath = buildAssetBundlesParameters.outputPath + "/" + bundleName;
                Debug.Log($"Size of AssetBundle {projectRelativePath} is {new FileInfo(projectRelativePath).Length}");
            }
        }
        else
        {
            Debug.Log("Build failed, see Console and Editor log for details");
        }
        
        Debug.Log("AssetBundles built at: " + assetsPath);
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