using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine.Networking;

[Serializable]
public class Component
{
    public string type = string.Empty;
    public string path = string.Empty;
    public string relativePath = string.Empty;
}

[Serializable]
public class GameObjectInfo
{
    public string name { get; set; } = string.Empty;
    public float[] position = { 0, 0, 0 };
    public float[] rotation = { 0, 0, 0 };
    public float[] scale = { 1, 1, 1 };
    public List<Component> components { get; set; } = new();
}
[Serializable]
public class BuildData
{
    public string buildName;
}

public class SceneLoader : MonoBehaviour
{
    [CanBeNull] private string _buildName = null;
    private string _bundlePath;
    private AssetBundle _assetBundle;
    private string _assetData;

    async void Start()
    {
        await GetBuildName();
        if (String.IsNullOrEmpty(_buildName))
        {
            Debug.LogError("Build name is not provided.");
            return;
        }
        
        var dataLocation = Path.Combine(Application.streamingAssetsPath, "ConfigurationData", _buildName, "Data.json");
        
        UnityWebRequest request = UnityWebRequest.Get(dataLocation);
        UnityWebRequestAsyncOperation operation = request.SendWebRequest();
        
        while (!operation.isDone)
        {
            await Task.Yield();
        }
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("File exists: " + dataLocation);
            _assetData = request.downloadHandler.text;
        }
        else
        {
            Debug.LogError("File does not exist or cannot be accessed: " + dataLocation);
            return;
        }
#if UNITY_ANDROID && !UNITY_EDITOR
        _bundlePath = $"jar:file://{Application.dataPath}!/assets/AssetBundles/{_buildName}";
#else
        _bundlePath = Path.Combine(Application.streamingAssetsPath, "AssetBundles", _buildName);
#endif
        Debug.Log(_bundlePath);
        StartCoroutine(LoadAssetBundleAndObjectsToScene());
    }

    async Task GetBuildName()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "ConfigurationData", "AssetBundleConfig.json");
        Debug.Log(path);
        UnityWebRequest request = UnityWebRequest.Get(path);
        UnityWebRequestAsyncOperation operation = request.SendWebRequest();
        while (!operation.isDone)
        {
            await Task.Yield();
        }
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log(request.downloadHandler.text);
            var data = JsonConvert.DeserializeObject<BuildData>(request.downloadHandler.text);
            _buildName = data?.buildName;
        }
        else
        {
            Debug.LogError("Cannot load file at " + path);
        }
    }
    IEnumerator LoadAssetBundleAndObjectsToScene()
    {
        UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(_bundlePath);
        Debug.Log(_assetData);
        Debug.Log(_bundlePath);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to load AssetBundle: " + request.error);
            yield break;
        }
        
        _assetBundle = DownloadHandlerAssetBundle.GetContent(request);
        if (_assetBundle == null)
        {
            Debug.LogError("Failed to load AssetBundle!");
            yield break;
        }

        List<GameObjectInfo> gameObjects = JsonConvert.DeserializeObject<List<GameObjectInfo>>(_assetData);
        foreach (var gameObject in gameObjects)
        {
            Debug.Log("Loading " + gameObject.name);
            var go = new GameObject()
            {
                name = gameObject.name,
                transform =
                {
                    position = new Vector3(gameObject.position[0], gameObject.position[1], gameObject.position[2]),
                    rotation = Quaternion.Euler(gameObject.rotation[0], gameObject.rotation[1], gameObject.rotation[2]),
                    localScale = new Vector3(gameObject.scale[0], gameObject.scale[1], gameObject.scale[2])
                }
            };
            foreach (var component in gameObject.components)
            {
                Debug.Log("Loading " + component.relativePath + " of type " + component.type);
                StartCoroutine(LoadAsset(go, component.relativePath, component.type));
            }
        }
        _assetBundle.Unload(false);
    }

    IEnumerator LoadAsset(GameObject obj, string assetName, string assetType)
    {
        AssetBundleRequest assetLoadRequest;
        switch (assetType)
        {
            case "Mesh":
                assetLoadRequest = _assetBundle.LoadAssetAsync<Mesh>(assetName);
                yield return assetLoadRequest;

                Mesh mesh = assetLoadRequest.asset as Mesh;
                if (mesh != null)
                {
                    Instantiate(mesh);
                    obj.AddComponent<MeshRenderer>();
                    obj.AddComponent<MeshFilter>();
                    obj.GetComponent<MeshFilter>().mesh = mesh;
                }
                break;
            
            case "Texture":
                assetLoadRequest = _assetBundle.LoadAssetAsync<Texture>(assetName);
                yield return assetLoadRequest;

                Texture texture = assetLoadRequest.asset as Texture;
                if (texture != null)
                {
                    obj.GetComponent<Renderer>().material.mainTexture = texture;
                }

                break;

            case "Material":
                assetLoadRequest = _assetBundle.LoadAssetAsync<Material>(assetName);
                yield return assetLoadRequest;

                Material material = assetLoadRequest.asset as Material;
                if (material != null)
                {
                    obj.GetComponent<Renderer>().material = material;
                }

                break;
            
            default:
                Debug.LogWarning($"Asset type '{assetType}' is not supported.");
                break;
        }
    }
}