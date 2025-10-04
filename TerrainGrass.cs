using UnityEditor;
using UnityEngine;

namespace ParticleGrass
{
    [RequireComponent(typeof(Terrain))]
    internal class TerrainGrass : GrassSurface
    {
        private TerrainData _terrainData;

        protected override void Initialize(ref string surfaceName)
        {
            _terrainData = GetComponent<Terrain>().terrainData;
            
            surfaceName = _terrainData.name;
        }

        protected override void Dispose()
        {
            
        }
        
        protected override RenderTexture GetHeightmap(out float heightmapHeight)
        {
            heightmapHeight = _terrainData.heightmapScale.y * 2f;
            return _terrainData.heightmapTexture;
        }

        protected override Bounds GetBounds()
        {
            return new Bounds(transform.position + _terrainData.bounds.size * 0.5f, _terrainData.bounds.size);
        }
        
#if UNITY_EDITOR
    
        [InitializeOnLoadMethod]
        private static void OnLoad()
        {
            TerrainCallbacks.heightmapChanged += OnHeightmapChanged;
        }
        
        private static void OnHeightmapChanged(Terrain terrain, RectInt region, bool sync)
        {
            if (terrain.TryGetComponent(out TerrainGrass terrainGrass))
            {
                terrainGrass.ForceUpdateBounds(terrainGrass.GetBounds());
            }
        }
    
#endif
    }
}
