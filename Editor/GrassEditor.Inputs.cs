using Unity.Mathematics;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace ParticleGrass.Editor
{
    public partial class GrassEditor
    {
        private float2 _cursorPosition;
        private float3 _viewDirection;
        private bool _mouseLeftDown, _mouseLeftDrag, _mouseLeftUp;
        private bool _mouseRightDown, _mouseRightDrag, _mouseRightUp;
        private bool _keyF;
        private bool _keyShift;
        private bool _mouseLeft;
        
        private bool _paintStarted;
        private bool _brushOperationStarted;
        
        private enum EditorActionType
        {
            Idle,
            StartPaint,
            ProceedPaint,
            EndPaint,
            StartBrushOperation,
            ProceedBrushOperation,
            ApplyBrushOperation,
            CancelBrushOperation,
        }
        
        private void AddDefaultControls(EditorActionType actionType) 
        {
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            
            if (Event.current.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(0);
            }

            if (Event.current.type == EventType.Repaint || Event.current.type == EventType.Layout)
            {
                return;
            }

            if (_paintStarted || _brushOperationStarted || actionType == EditorActionType.CancelBrushOperation)
            {
                Event.current.Use();
            }
        }
        
        private void HandleInputs()
        {
            _cursorPosition = Event.current.mousePosition;
            
            _keyShift = Event.current.shift;
            _keyF = Event.current.keyCode == KeyCode.F;
            _mouseLeft = Event.current.type == EventType.MouseLeaveWindow;
            
            _mouseLeftDown = Event.current.type == EventType.MouseDown && Event.current.button == 0;
            _mouseLeftDrag = Event.current.type == EventType.MouseDrag && Event.current.button == 0;
            _mouseLeftUp = Event.current.type == EventType.MouseUp && Event.current.button == 0;
            
            _mouseRightDown = Event.current.type == EventType.MouseDown && Event.current.button == 1;
            _mouseRightDrag = Event.current.type == EventType.MouseDrag && Event.current.button == 1;
            _mouseRightUp = Event.current.type == EventType.MouseUp && Event.current.button == 1;
        }

        private EditorActionType GetActiveActionType()
        {
            _viewDirection = GetViewDirection();
            
            if (_keyF)
            {
                _brushOperationStarted = true;
                return EditorActionType.StartBrushOperation;
            }

            if (_brushOperationStarted)
            {
                if (!_mouseLeftDown && !_mouseRightDown)
                {
                    return EditorActionType.ProceedBrushOperation;
                }
                
                _brushOperationStarted = false;
                return _mouseLeftDown
                    ? EditorActionType.ApplyBrushOperation
                    : EditorActionType.CancelBrushOperation;
            }
            
            if (_paintStarted)
            {
                if (!_mouseLeftUp)
                {
                    return EditorActionType.ProceedPaint;
                }
                
                _paintStarted = false;
                return EditorActionType.EndPaint;
            }

            if (_mouseLeftDown)
            {
                _paintStarted = true;
                return EditorActionType.StartPaint;
            }
            
            return EditorActionType.Idle;
        }

        private float3 GetViewDirection()
        {
            return SceneView.lastActiveSceneView.camera.transform.forward;
        }
    }
}
