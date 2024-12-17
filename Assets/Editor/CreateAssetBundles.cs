using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class CreateAssetBundles 
{
    [MenuItem("Build/Build Asset Bundles for Oculus Quest")]
    public static void MenuBuildAllAssetBundles()
    {
        BuildAllAssetBundles();
    }
    [MenuItem("Build/Build Asset Bundles for Editor")]
    public static void MenuBuildAllAssetBundlesForEditor()
    {
        BuildAllAssetBundlesForEditor();
    }
    [MenuItem("Build/Build APK")]
    public static void MenuBuildApk()
    {
        BuildApk();
    }
    [MenuItem("Build/Build All")]
    public static void MenuBuildAll()
    {
        BuildAllAssetBundles();
        BuildApk();
    }

    static void BuildAllAssetBundlesForEditor()
    {
        string assetBundleDirectory = "Assets/StreamingAssets/AssetBundles"; 
        if (!Directory.Exists(assetBundleDirectory))
        {
            Directory.CreateDirectory(assetBundleDirectory);
        }

        AssetBundleBuild sceneAssetBundle = new(), assetBundle = new();
        sceneAssetBundle.assetBundleName = "ScenesEditor";
        sceneAssetBundle.assetNames = Directory.EnumerateFiles("Assets/Uploads", "*.unity", SearchOption.TopDirectoryOnly).ToArray();
        assetBundle.assetBundleName = "AssetsEditor";
        assetBundle.assetNames = GetAssets("Assets/Uploads").ToArray();

        BuildAssetBundlesParameters buildAssetBundlesParameters = new()
        {
            outputPath = assetBundleDirectory,
            bundleDefinitions = new[] { sceneAssetBundle, assetBundle },
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
    }
    static void BuildAllAssetBundles()
    {
        string assetBundleDirectory = "Assets/StreamingAssets/AssetBundles"; 
        if (!Directory.Exists(assetBundleDirectory))
        {
            Directory.CreateDirectory(assetBundleDirectory);
        }

        AssetBundleBuild sceneAssetBundle = new(), assetBundle = new();
        sceneAssetBundle.assetBundleName = "Scenes";
        sceneAssetBundle.assetNames = Directory.EnumerateFiles("Assets/Uploads", "*.unity", SearchOption.TopDirectoryOnly).ToArray();
        assetBundle.assetBundleName = "Assets";
        assetBundle.assetNames = GetAssets("Assets/Uploads").ToArray();

        BuildAssetBundlesParameters buildAssetBundlesParameters = new()
        {
            outputPath = assetBundleDirectory,
            bundleDefinitions = new[] { sceneAssetBundle, assetBundle },
            options = BuildAssetBundleOptions.ChunkBasedCompression,
            targetPlatform = BuildTarget.Android
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
    static void BuildApk()
    {
    }
}
