using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

using static Unity.Mathematics.math;

namespace ParticleGrass.Editor
{
    public partial class GrassEditor
    {
        private const float BrushIgnoreSurfaceNormalDistance = 20f;
        
        private const float StrengthDiskMinRadius = 0.5f;
        private const float StrengthDiskMaxRadius = 2.5f;
        private const float MouseMovementRange = 300f;
        private const float MouseSizeSmoothingStrength = 2f;
        private const float MouseStrengthSmoothingStrength = 1.2f;
        private const int GUILabelFontSize = 20;
        
        private static readonly Color TextColor = new Color(1f, 1f, 1f);
        private static readonly Color BrushPreviewColor = new Color(0f, 0.2f, 1f);
        private static readonly Color BrushOperationInitialValueColor = new Color(0f, 0.1f, 0.5f);
        
        private float3 _brushPosition, _brushNormal;
        private float _brushRadius;

        private bool _brushOperationGUI;
        private bool _brushOperationShift;
        private float _brushOperationInitialValue;
        private float2 _brushOperationCursorPosition;
        
        private void GetBrushProjection(RaycastHit hit)
        {
            _brushPosition = hit.point;
            _brushNormal = 
                hit.distance < 0.5f * BrushIgnoreSurfaceNormalDistance? hit.normal : 
                hit.distance > BrushIgnoreSurfaceNormalDistance? float3(0, 1f, 0) : 
                lerp(hit.normal, float3(0, 1f, 0), hit.distance / BrushIgnoreSurfaceNormalDistance * 2f - 1f);
            _brushRadius = GetBrushRadius(hit.point, GrassOverlay.BrushSize * 0.5f);
        }
        
        private void DrawBrushPreview(bool cursorOnSurface)
        {
            if (_brushOperationGUI)
            {
                if (_brushOperationShift)
                {
                    DrawBrushOperationStrengthPreview();
                }
                else
                {
                    DrawBrushOperationSizePreview();
                }
            }
            else if (cursorOnSurface)
            {
                DrawDisc(BrushPreviewColor);
            }
            else
            {
                DrawDiscGUI(GrassOverlay.BrushSize * 0.5f, _cursorPosition, BrushPreviewColor);
            }
        }
        
        private void DrawBrushOperationStrengthPreview()
        {
            DrawDiscGUI(StrengthDiskMinRadius, _brushOperationCursorPosition, BrushOperationInitialValueColor);
            DrawDiscGUI(StrengthDiskMaxRadius, _brushOperationCursorPosition, BrushOperationInitialValueColor);
            
            DrawDiscGUI(lerp(StrengthDiskMinRadius, StrengthDiskMaxRadius, GrassOverlay.GetStrengthDiscRange), _brushOperationCursorPosition, BrushPreviewColor);
            DrawLabelGUI(GrassOverlay.GetBrushStrengthText, _brushOperationCursorPosition, TextColor);
        }

        private void DrawBrushOperationSizePreview()
        {
            DrawDiscGUI(_brushOperationInitialValue * 0.5f, _brushOperationCursorPosition, BrushOperationInitialValueColor);
            DrawDiscGUI(GrassOverlay.BrushSize * 0.5f, _brushOperationCursorPosition, BrushPreviewColor);
        }

        private void DrawDisc(Color discColor)
        {
            Handles.color = discColor;
            Handles.DrawWireDisc(_brushPosition, _brushNormal, _brushRadius);
        }
        
        private void StartBrushOperation()
        {
            _brushOperationGUI = true;
            _brushOperationShift = _keyShift;
            _brushOperationInitialValue = _keyShift? GrassOverlay.BrushStrength : GrassOverlay.BrushSize;
            _brushOperationCursorPosition = _cursorPosition;
        }

        private void CheckBrushOperationInterrupted()
        {
            if (!_brushOperationStarted)
            {
                return;
            }
            
            if (_mouseLeft)
            {
                _brushOperationStarted = false;
                CancelBrushOperation();
            }
        }
        
        private void ProceedBrushOperation()
        {
            var movementX = (_cursorPosition.x - _brushOperationCursorPosition.x) / MouseMovementRange;
            
            if (_brushOperationShift)
            {
                GrassOverlay.SetStrengthOperationValue(
                    _brushOperationInitialValue, 
                    movementX > 0, 
                    pow(saturate(abs(movementX)), MouseStrengthSmoothingStrength)
                );
            }
            else
            {
                GrassOverlay.SetSizeOperationValue(
                    _brushOperationInitialValue, 
                    movementX > 0, 
                    pow(saturate(abs(movementX)), MouseSizeSmoothingStrength)
                );
            }
        }

        private void ApplyBrushOperation()
        {
            _brushOperationGUI = false;
        }

        private void CancelBrushOperation()
        {
            _brushOperationGUI = false;
            
            if (_brushOperationShift)
            {
                GrassOverlay.BrushStrength = _brushOperationInitialValue;
            }
            else
            {
                GrassOverlay.BrushSize = _brushOperationInitialValue;
            }
        }
        
        private static void DrawLabelGUI(string text, float2 center, Color textColor)
        {
            Handles.color = textColor;
            
            var style = new GUIStyle
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = GUILabelFontSize,
                fontStyle = FontStyle.Bold,
                richText = true
            };

            var content = $"<color=#{textColor.ToHexString()}>{text}</color>";
            
            Handles.Label(MousePosToGUIWorldPos(center), content, style);
        }
        
        private static void DrawDiscGUI(float radius, float2 center, Color discColor)
        {
            Handles.color = discColor;
            var point = MousePosToGUIWorldPos(center);
            Handles.DrawWireDisc(point, GetGUIWorldNormal(), GetBrushRadius(point, radius));
        }

        private static float GetBrushRadius(float3 point, float radius)
        {
            return HandleUtility.GetHandleSize(point) * radius;
        }

        private static float3 MousePosToGUIWorldPos(float2 mousePos)
        {
            var offset = SceneView.lastActiveSceneView.camera.nearClipPlane + 0.001f;
            var direction = HandleUtility.GUIPointToWorldRay(mousePos).direction;
            var position = SceneView.lastActiveSceneView.camera.transform.position;
            
            return position + offset * direction;
        }

        private static float3 GetGUIWorldNormal()
        {
            return -SceneView.lastActiveSceneView.camera.transform.forward;
        }
    }
}
