using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering.Universal;

using static Unity.Mathematics.math;

namespace ParticleGrass
{
    [System.Serializable]
    public struct GrassRendererSettings
    {
        [Range(0f, 1f)] public float windDirection;
        [Range(0f, 1f)] public float windIntensity;

        internal static GrassRendererSettings DefaultSettings => new GrassRendererSettings
        {
            windDirection = 0f,
            windIntensity = 1f,
        };
    }
    
    public class GrassRenderer : ScriptableRendererFeature
    {
        public GrassRendererSettings settings = GrassRendererSettings.DefaultSettings;

        private GrassRenderPass _grassPass;
        
        public override void Create()
        {
            _grassPass = new GrassRenderPass();
            
            _grassPass.Setup(settings);
            
            _grassPass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(_grassPass);
        }

        protected override void Dispose(bool disposing)
        {
            _grassPass.Dispose();
        }
    }
}
