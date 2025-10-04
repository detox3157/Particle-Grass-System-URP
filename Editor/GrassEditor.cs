using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace ParticleGrass.Editor
{
    [EditorTool("GrassEditor")]
    public partial class GrassEditor : EditorTool
    {
        #region Toolbar Icon
        
        private static GUIContent _toolbarIcon;
        
        public override GUIContent toolbarIcon
        {
            get
            {
                if (_toolbarIcon == null)
                {
                    var iconTex = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath("15e4f0eb86cad4d819458176b52e0638"));

                    _toolbarIcon = iconTex == null
                        ? EditorGUIUtility.IconContent("d_Grid.PaintTool")
                        : new GUIContent(iconTex, "Grass Editor Tool");
                }
                
                return _toolbarIcon;
            }
        }
        
        #endregion

        private const string PaintComputeGUID = "5ea0645ffdbf04f409d1f6d2db4deeb1";
        
        private static readonly GrassOverlay GrassOverlay = new GrassOverlay();

        private ComputeShader _paintCompute;
        private int _paintKernel;

        private void OnEnable()
        {
            _paintCompute = AssetDatabase.LoadAssetAtPath<ComputeShader>(AssetDatabase.GUIDToAssetPath(PaintComputeGUID));
            _paintKernel = _paintCompute.FindKernel("Paint");
        }
        
        public override void OnActivated()
        {
            AddOverlays();
            base.OnActivated();
        }
        
        public override void OnWillBeDeactivated()
        {
            RemoveOverlays();
            base.OnWillBeDeactivated();
        }
        
        private static void AddOverlays()
        {
            GrassType.OnGrassTypesChanged += GrassOverlay.ForceRebuildContent;
            
            SceneView.AddOverlayToActiveView(GrassOverlay);
        }

        private static void RemoveOverlays()
        {
            GrassType.OnGrassTypesChanged -= GrassOverlay.ForceRebuildContent;
            
            SceneView.RemoveOverlayFromActiveView(GrassOverlay);
        }
        
        public override void OnToolGUI(EditorWindow window)
        {
            HandleInputs();
            CheckBrushOperationInterrupted();

            var cursorOnSurface = FindActiveEditedSurface(out var surface, out var hit) && ValidateActiveEditedSurface(surface);
            
            var actionType = GetActiveActionType();
            AddDefaultControls(actionType); 
            
            CheckPaintInterrupted(actionType, surface);
            HandleActionType(actionType);

            if (cursorOnSurface)
            {
                GetBrushProjection(hit);
            }
            
            DrawBrushPreview(cursorOnSurface);
            
            window.Repaint();
        }

        private void HandleActionType(EditorActionType actionType)
        {
            switch (actionType)
            {
                case EditorActionType.StartPaint:
                    HandlePaintStart();
                    break;
                case EditorActionType.EndPaint:
                    HandlePaintEnd();
                    break;
                case EditorActionType.ProceedPaint:
                    HandlePaint();
                    break;
                case EditorActionType.StartBrushOperation:
                    StartBrushOperation();
                    break;
                case EditorActionType.ProceedBrushOperation:
                    ProceedBrushOperation();
                    break;
                case EditorActionType.ApplyBrushOperation:
                    ApplyBrushOperation();
                    break;
                case EditorActionType.CancelBrushOperation:
                    CancelBrushOperation();
                    break;
            }
        }

        private bool FindActiveEditedSurface(out GrassSurface surface, out RaycastHit hit)
        {
            var ray = HandleUtility.GUIPointToWorldRay(_cursorPosition);
            
            if (Physics.Raycast(ray, out hit, 5000f, GrassSurface.SurfaceLayers))
            {
                return hit.collider.gameObject.TryGetComponent(out surface);
            }

            surface = null;
            return false;
        }
        
        private bool ValidateActiveEditedSurface(GrassSurface surface)
        {
            return surface != null && (!GrassOverlay.EditSelectedSurface || surface.gameObject == Selection.activeGameObject);
        }
    }
}
