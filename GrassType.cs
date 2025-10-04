using System;
using UnityEngine;
using Unity.Mathematics;

using static Unity.Mathematics.math;

namespace ParticleGrass
{
    [Serializable]
    internal struct GrassArtisticParameters
    {
        [Min(0.1f)] public float size;
        public float2 widthRange;
        [Range(0f, 1f)] public float jitterStrength;
        [Range(0f, 1f)] public float clumpingStrength;
        [Min(0.1f)] public float clumpSize;
        public float2 sizeRange;
        public float2 tiltRange;
        public float2 bendRange;
        [Range(0f, 1f)] public float rotationRange;
        [Range(0f, 1f)] public float rotateTowardsCameraStrength;
        [Min(0.1f)] public float followWindDirectionStrength;
        public float2 movementDistanceCutoff;
        public float2 bobbingDistanceCutoff;
        
        [Space(10), Header("Bobbing"), Space(5)]
        public float2 bobbingStrength;
        public float2 bobbingSpeed;
        public float2 bobbingWavelength;
        public float2 bobbingCutoff;

        [HideInInspector] public float3 tintBottomColor;
        [HideInInspector] public float3 tintTopColor;
        [HideInInspector] public float3 tintVariationColorA;
        [HideInInspector] public float3 tintVariationColorB;

        public static GrassArtisticParameters DefaultParameters = new GrassArtisticParameters
        {
            size = 1f,
            widthRange = float2(0.7f, 1f),
            jitterStrength = 0.4f,
            clumpingStrength = 0.3f,
            clumpSize = 0.5f,
            sizeRange = float2(0.5f, 1f),
            tiltRange = float2(0.3f, 0.7f),
            bendRange = float2(0, 1f),
            rotationRange = 0.7f,
            rotateTowardsCameraStrength = 0.2f,
            followWindDirectionStrength = 1.5f,
            movementDistanceCutoff = float2(75f, 100f),
            bobbingDistanceCutoff = float2(40f, 50f),
            bobbingStrength = float2(0.02f, 0.05f),
            bobbingSpeed = float2(15f, 15f),
            bobbingWavelength = float2(0.2f, 1f),
            bobbingCutoff = float2(3f, 2f),
        };
    }
    
    [CreateAssetMenu(fileName = "GrassType", menuName = "ParticleGrass/GrassType")]
    public class GrassType : ScriptableObject
    {
#if UNITY_EDITOR
        public static Action OnGrassTypesChanged = delegate { };
#endif
        
        [field: SerializeField] public string Name { get; private set; } = "Display Name";
        
        [field: SerializeField] internal Color TintBottomColor { get; private set; } = Color.green;
        [field: SerializeField] internal Color TintTopColor { get; private set; } = Color.yellow;
        [field: SerializeField] internal Color TintVariationColorA { get; private set; } = Color.white;
        [field: SerializeField] internal Color TintVariationColorB { get; private set; } = Color.black;
        
        [SerializeField] private GrassArtisticParameters artisticParameters = GrassArtisticParameters.DefaultParameters;
        
        internal GrassArtisticParameters ArtisticParameters => artisticParameters;

        public float GrassMapValue { get; internal set; } = 0f;

#if UNITY_EDITOR
        private void OnValidate()
        {
            artisticParameters.tintBottomColor = float3(TintBottomColor.r, TintBottomColor.g, TintBottomColor.b);
            artisticParameters.tintTopColor = float3(TintTopColor.r, TintTopColor.g, TintTopColor.b);
            artisticParameters.tintVariationColorA = float3(TintVariationColorA.r, TintVariationColorA.g, TintVariationColorA.b);
            artisticParameters.tintVariationColorB = float3(TintVariationColorB.r, TintVariationColorB.g, TintVariationColorB.b);
            
            OnGrassTypesChanged.Invoke();
        }
#endif
    }
}
