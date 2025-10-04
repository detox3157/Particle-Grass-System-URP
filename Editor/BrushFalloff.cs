using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ParticleGrass.Editor
{
    [CreateAssetMenu(fileName = "BrushFalloff", menuName = "ParticleGrass/Editor/BrushFalloff")]
    public class BrushFalloff : ScriptableObject
    {
        private const string FalloffsDirectoryGUID = "0236f6276c7bd488d9a4e9926ba3cd15";

        private static readonly HashSet<BrushFalloff> _brushFalloffs = new HashSet<BrushFalloff>();
        
        internal static BrushFalloff[] BrushFalloffs => _brushFalloffs.ToArray();

        [field:SerializeField] internal string DisplayName {get; private set;}
        [field:SerializeField] internal Texture2D Mask {get; private set;}
        [field:SerializeField] internal Texture2D Icon {get; private set;}

        [InitializeOnLoadMethod]
        private static void OnLoad()
        {
            var falloffFiles = Directory.GetFiles(AssetDatabase.GUIDToAssetPath(FalloffsDirectoryGUID)).
                Where(file => file.EndsWith(".asset")).ToArray();
            
            foreach (var falloffFile in falloffFiles)
            {
                _brushFalloffs.Add(AssetDatabase.LoadAssetAtPath<BrushFalloff>(falloffFile));
            }
        }
        
        private void Awake()
        {
            _brushFalloffs.Add(this);
        }
    }
}