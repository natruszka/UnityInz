using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;

[Serializable]
public class Component
{
    public string type = string.Empty;
    public string path = string.Empty;
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

public class SceneLoader : MonoBehaviour
{
    public string AssetBundleName = "/AssetsEditor";
    public string SceneBundleName = "/ScenesEditor";
    public string DataLocation = "Assets/Uploads/Data.json";
    private string _bundlePath;
    private AssetBundle _assetBundle;

    void Start()
    {
        if (!File.Exists(DataLocation))
        {
            Debug.LogError("Data file not found");
            return;
        }

        _bundlePath = Path.Combine(Application.streamingAssetsPath, "AssetBundles", AssetBundleName);
        _bundlePath = _bundlePath.Replace("/", @"\");
        Debug.Log(_bundlePath);
        StartCoroutine(LoadAssetBundleAndObjectsToScene());
    }

    IEnumerator LoadAssetBundleAndObjectsToScene()
    {
        AssetBundleCreateRequest bundleLoadRequest = AssetBundle.LoadFromFileAsync(_bundlePath);
        yield return bundleLoadRequest;

        _assetBundle = bundleLoadRequest.assetBundle;
        if (_assetBundle == null)
        {
            Debug.LogError("Failed to load AssetBundle!");
            yield break;
        }

        string data = File.ReadAllText(DataLocation);
        List<GameObjectInfo> gameObjects = JsonConvert.DeserializeObject<List<GameObjectInfo>>(data);
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
                Debug.Log("Loading " + component.path + " of type " + component.type);
                StartCoroutine(LoadAsset(go, component.path, component.type));
            }
        }

        Debug.Log(data);
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