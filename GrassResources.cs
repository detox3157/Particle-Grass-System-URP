using System;
using System.IO;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace ParticleGrass
{

    public class GrassResources : MonoBehaviour
    {
        internal const int MaxTextureResolution = 8192;

        private const string ResourcesDirectory = "ParticleGrassSystem";

        private static readonly string PathToConfigs = Path.Combine(ResourcesDirectory, "Configs");
        private static readonly string PathToGrassMaps = Path.Combine(ResourcesDirectory, "GrassMaps");
        private static readonly string PathToGrassTypes = Path.Combine(ResourcesDirectory, "GrassTypes");

        private static GrassConfig[] _configs;
        private static int _activeConfigId;
        
        public static GrassType[] GrassTypes {get; private set;}
        public static GrassConfig Config => _configs[_activeConfigId];
        
        #region On Load
        
        [InitializeOnLoadMethod, RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnLoad()
        {
#if UNITY_EDITOR
            CheckDirectories();
            GrassType.OnGrassTypesChanged += LoadGrassTypes;
#endif

            LoadConfigs();
            LoadGrassTypes();
        }
        
#if UNITY_EDITOR
        
        private static void CheckDirectories()
        {
            CheckResourcesDirectory(PathToConfigs);
            CheckResourcesDirectory(PathToGrassMaps);
            CheckResourcesDirectory(PathToGrassTypes);
        }

        private static void CheckResourcesDirectory(string path)
        {
            var fullPath = ResourceToFullPath(path);

            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }
        }
#endif
        
        #endregion
        
        #region Grass Types

        private static void LoadGrassTypes()
        {
            GrassTypes = Resources.LoadAll(PathToGrassTypes).OfType<GrassType>().ToArray();

#if UNITY_EDITOR
            
            if (GrassTypes.Length == 0)
            {
                CreateDefaultGrassTypeAsset();
            }
            
#endif

            for (var type = 0; type < GrassTypes.Length; type++)
            {
                GrassTypes[type].GrassMapValue = type / 256f;
            }
        }
        
#if UNITY_EDITOR
        
        private static void CreateDefaultGrassTypeAsset()
        {
            var instance = ScriptableObject.CreateInstance<GrassType>();
            
            AssetDatabase.CreateAsset(instance, Path.Combine(ResourceToFullPath(PathToGrassTypes), "DefaultGrass.asset"));
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
#endif
        
        #endregion
        
        #region Config

        public static void SetConfig(int configId)
        {
            if (configId < 0 || configId >= _configs.Length)
            {
                throw new ArgumentOutOfRangeException();
            }
            
            _activeConfigId = configId;
        }
        
        private static void LoadConfigs()
        {
            _configs = Resources.LoadAll(PathToConfigs).OfType<GrassConfig>().ToArray();
            
            InitializeConfigs();
        }
        
        private static void InitializeConfigs()
        {
#if UNITY_EDITOR
            if (_configs.Length == 0)
            {
                CreateDefaultConfigInstance();
            }

            foreach (var config in _configs)
            {
                config.Validate();
            }
#endif
            
            foreach (var config in _configs)
            {
                config.InitializeComputeKernels();
            }
        }
        
#if UNITY_EDITOR
        
        private static void CreateDefaultConfigInstance()
        {
            var instance = ScriptableObject.CreateInstance<GrassConfig>();
            
            AssetDatabase.CreateAsset(instance, Path.Combine(ResourceToFullPath(PathToConfigs), "Config.asset"));
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
#endif
        
        #endregion
        
        #region Textures

#if UNITY_EDITOR
        internal static Texture2D CreateGrassMapAsset(int resolution, string name)
        {
            return CreateTextureAsset(Path.Combine(PathToGrassMaps, name), resolution, Color.clear);
        }
        
        private static Texture2D CreateTextureAsset(string path, int resolution, Color color)
        {
            var fullPath = ResourceToFullPath(path, ".png");
            
            var texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);

            FillTextureColor(texture, color);
            
            var bytes = texture.EncodeToPNG();
            File.WriteAllBytes(fullPath, bytes);

            AssetDatabase.Refresh();
            DestroyImmediate(texture);
            return ImportTextureAsset(fullPath);
        }
        
        private static Texture2D ImportTextureAsset(string fullPath)
        {
            var importer = AssetImporter.GetAtPath(fullPath) as TextureImporter;
            
            if (importer == null)
            {
                return null;
            }

            importer.mipmapEnabled = false;
            importer.streamingMipmaps = false;
            importer.filterMode = FilterMode.Point;
            importer.sRGBTexture = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.isReadable = true;
            
            importer.SetPlatformTextureSettings(
                new TextureImporterPlatformSettings
                {
                    name = "DefaultTexturePlatform",
                    format = TextureImporterFormat.RGBA32,
                    maxTextureSize = MaxTextureResolution,
                }
            );
            
            importer.SaveAndReimport();
            
            return AssetDatabase.LoadAssetAtPath<Texture2D>(fullPath);
        }
        
#endif
        
        #endregion
        
        #region Utils
        
        private static void FillTextureColor(Texture2D texture, Color color)
        {
            var pixels = texture.GetPixels();

            for (var pixelId = 0; pixelId < pixels.Length; pixelId++)
            {
                pixels[pixelId] = color;
            }
            
            texture.SetPixels(pixels);
        }
        
        private static string ResourceToFullPath(string path, string extension = "")
        {
            return $"{Path.Combine("Assets", "Resources", path)}{extension}";
        }
        
        #endregion
    }
}