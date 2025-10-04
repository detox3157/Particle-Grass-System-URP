using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

using static Unity.Mathematics.math;

namespace ParticleGrass.Editor
{
    internal enum PaintMode
    {
        Additive = 1,
        Multiply = 2,
        Replace = 3,
        ReplaceType = 4
    }
    
    public partial class GrassEditor
    {
        #region Shader ID
        
        private static readonly int PaintingMaskID = Shader.PropertyToID("_PaintingMask");
        private static readonly int PaintActionConfigID = Shader.PropertyToID("_PaintActionConfig");
        private static readonly int BrushConfigID = Shader.PropertyToID("_BrushConfig");
        private static readonly int SurfaceConfigID = Shader.PropertyToID("_SurfaceConfig");
        private static readonly int PointConfigID = Shader.PropertyToID("_PointConfig");
        private static readonly int GrassMapID = Shader.PropertyToID("_GrassMap");
        private static readonly int FalloffMaskID = Shader.PropertyToID("_FalloffMask");
        
        #endregion
        
        private EditorActionType _actionType = EditorActionType.Idle;
        private GrassSurface _surface;
        private RenderTexture _paintingMask;
        
        private void CheckPaintInterrupted(EditorActionType actionType, GrassSurface surface)
        {
            if (_actionType == EditorActionType.ProceedPaint)
            {
                if ((actionType != EditorActionType.ProceedPaint && actionType != EditorActionType.EndPaint) || _mouseLeft)
                {
                    _paintStarted = false;
                    HandlePaintEnd();
                }
                else if (surface != _surface && surface != null)
                {
                    ForceRestartPaint();
                }
            }

            if (surface != null)
            {
                _surface = surface;
            }
            
            _actionType = actionType;
        }

        private void ForceRestartPaint()
        {
            HandlePaintEnd();
            HandlePaintStart();
        }
        
        private void HandlePaintStart()
        {
            _paintingMask = CreatePaintingMask(_surface);
            
            var brushConfig = float4(GrassOverlay.ActiveBrushFalloff.Mask.width, GrassOverlay.ActiveBrushFalloff.Mask.height, 0, 0);
            var paintActionConfig = float4(GrassOverlay.BrushStrength, _keyShift? -1 : 1, (float) GrassOverlay.PaintMode, GrassOverlay.GrassType.GrassMapValue);
            var surfaceConfig = float4(_surface.GrassMapResolution, 0, 0);
            
            _paintCompute.SetVector(BrushConfigID, brushConfig);
            _paintCompute.SetVector(PaintActionConfigID, paintActionConfig);
            _paintCompute.SetVector(SurfaceConfigID, surfaceConfig);
            
            _paintCompute.SetTexture(_paintKernel, PaintingMaskID, _paintingMask);
            _paintCompute.SetTexture(_paintKernel, GrassMapID, _surface.GrassMap);
            _paintCompute.SetTexture(_paintKernel, FalloffMaskID, GrassOverlay.ActiveBrushFalloff.Mask);
        }

        private void HandlePaintEnd()
        {
            _paintingMask?.Release();
        }

        private void HandlePaint()
        {
            DispatchPaint(PointToSurfaceUV(_brushPosition, _surface));
        }

        private void DispatchPaint(float2 pointUV)
        {
            var pixelSize = BrushToPixelSize(_brushRadius * 2, _surface.BoundsSize, _surface.GrassMapResolution);
            var pointConfig = float4(floor(pointUV * _surface.GrassMapResolution), pixelSize);
            
            _paintCompute.SetVector(PointConfigID, pointConfig);
            
            var threads = (pixelSize + int2(7, 7)) / 8;
            _paintCompute.Dispatch(_paintKernel, max(1, threads.x), max(1, threads.y), 1);
        }

        private static int2 BrushToPixelSize(float brushSize, float3 surfaceSize, int2 actionTextureResolution)
        {
            return int2(
                (int) (brushSize / surfaceSize.x * actionTextureResolution.x), 
                (int) (brushSize / surfaceSize.z * actionTextureResolution.y)
            );
        }

        private static float2 PointToSurfaceUV(float3 brushPosition, GrassSurface surface)
        {
            return float3((brushPosition - surface.BoundsMin) / surface.BoundsSize).xz;
        }

        private static RenderTexture CreatePaintingMask(GrassSurface surface)
        {
            var rt = new RenderTexture(new RenderTextureDescriptor
            {
                width = surface.GrassMapResolution.x,
                height = surface.GrassMapResolution.y,
                depthBufferBits = 0,
                volumeDepth = 1,
                msaaSamples = 1,
                dimension = UnityEngine.Rendering.TextureDimension.Tex2D,
                colorFormat = RenderTextureFormat.ARGB32,
                enableRandomWrite = true,
            });
            
            var previousActiveRT = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = previousActiveRT;
            
            return rt;
        }
    }
}
