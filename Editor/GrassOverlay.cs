using System;
using System.Linq;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Mathematics;
using UnityEditor;
using static Unity.Mathematics.math;

namespace ParticleGrass.Editor
{
    [Overlay(defaultDisplay = true, displayName = "Grass Editor")]
    public class GrassOverlay : Overlay
    {
        private const string FloatFormat = "0.0";
        private const float ImageButtonSize = 20f;
        
        private static readonly Color ButtonActiveColor = Color.grey;
        private static readonly Color ButtonInactiveColor = Color.clear;
        
        private const float BrushMinSize = 0.1f, BrushMaxSize = 5f;
        private const float BrushMinStrength = 0f, BrushMaxStrength = 1f;
        private const float BrushMinSizeDisplay = 1f, BrushMaxSizeDisplay = 100f;
        private const float BrushMinStrengthDisplay = 0f, BrushMaxStrengthDisplay = 100f;
        
        private DropdownField _grassTypeDropdownField;
        private DropdownField _paintModeDropdownField;
        private Toggle _editSelectedSurfaceToggle;
        private FloatField _brushSizeField;
        private FloatField _brushStrengthField;
        
        private float _brushSize, _brushStrength;
        private float _brushDisplaySize, _brushDisplayStrength;

        private BrushFalloff[] _falloffs;
        private int _activeFalloffId = 0;
        
        internal BrushFalloff ActiveBrushFalloff => _falloffs[_activeFalloffId];
        
        internal float BrushSize
        {
            get => _brushSize;
            set
            {
                _brushSize = clamp(value, BrushMinSize, BrushMaxSize);
                _brushDisplaySize = remap(BrushMinSize, BrushMaxSize, BrushMinSizeDisplay, BrushMaxSizeDisplay, _brushSize);
                _brushSizeField.SetValueWithoutNotify(_brushDisplaySize);
            }
        }
        
        internal float BrushDisplaySize
        {
            get => _brushDisplaySize;
            set
            {
                _brushDisplaySize = clamp(value, BrushMinSizeDisplay, BrushMaxSizeDisplay);
                _brushSize = remap(BrushMinSizeDisplay, BrushMaxSizeDisplay, BrushMinSize, BrushMaxSize, _brushDisplaySize);
                _brushSizeField.SetValueWithoutNotify(_brushDisplaySize);
            }
        }
        
        internal float BrushStrength
        {
            get => _brushStrength;
            set
            {
                _brushStrength = clamp(value, BrushMinStrength, BrushMaxStrength);
                _brushDisplayStrength = remap(BrushMinStrength, BrushMaxStrength, BrushMinStrengthDisplay, BrushMaxStrengthDisplay, _brushStrength);
                _brushStrengthField.SetValueWithoutNotify(_brushDisplayStrength);
            }
        }
        
        internal float BrushDisplayStrength
        {
            get => _brushDisplayStrength;
            set
            {
                _brushDisplayStrength = clamp(value, BrushMinStrengthDisplay, BrushMaxStrengthDisplay);
                _brushStrength = remap(BrushMinStrengthDisplay, BrushMaxStrengthDisplay, BrushMinStrength, BrushMaxStrength, _brushDisplayStrength);
                _brushStrengthField.SetValueWithoutNotify(_brushDisplayStrength);
            }
        }
        
        internal bool EditSelectedSurface {get; private set;}
        
        internal GrassType GrassType {get; private set;}
        
        internal PaintMode PaintMode { get; private set; } = PaintMode.Additive;
        
        internal float GetStrengthDiscRange => (BrushStrength - BrushMinStrength) / (BrushMaxStrength - BrushMinStrength);
        
        internal string GetBrushStrengthText => BrushDisplayStrength.ToString(FloatFormat);

        
        public override VisualElement CreatePanelContent()
        {
            _grassTypeDropdownField = new DropdownField();
            _grassTypeDropdownField.choices = GrassResources.GrassTypes.Select(type => type.Name).ToList();
            _grassTypeDropdownField.value = GrassResources.GrassTypes[0].Name;
            GrassType = GrassResources.GrassTypes[0];
            
            _paintModeDropdownField = new DropdownField();
            _paintModeDropdownField.choices = Enum.GetNames(typeof(PaintMode)).ToList();
            _paintModeDropdownField.value = Enum.GetName(typeof(PaintMode), PaintMode);
            
            _editSelectedSurfaceToggle = new Toggle("Only edit selected surface");
            _brushSizeField = new FloatField("Brush Size");
            _brushStrengthField = new FloatField("Brush Strength");
            
            _brushSizeField.formatString = _brushStrengthField.formatString = FloatFormat;
            
            _editSelectedSurfaceToggle.value = EditSelectedSurface;
            BrushDisplaySize = 20f;
            BrushDisplayStrength = 100f;

            _grassTypeDropdownField.RegisterValueChangedCallback(UpdateGrassType);
            _paintModeDropdownField.RegisterValueChangedCallback(UpdatePaintMode);
            _editSelectedSurfaceToggle.RegisterValueChangedCallback(UpdateEditSelectedSurface);
            _brushSizeField.RegisterValueChangedCallback(UpdateBrushSize);
            _brushStrengthField.RegisterValueChangedCallback(UpdateBrushStrength);
            
            _falloffs = BrushFalloff.BrushFalloffs;
            
            var content = new VisualElement();
            content.Add(CreateBrushFalloffContainer());
            content.Add(_grassTypeDropdownField);
            content.Add(_paintModeDropdownField);
            content.Add(_editSelectedSurfaceToggle);
            content.Add(_brushSizeField);
            content.Add(_brushStrengthField);
            return content;
        }
        
        internal void SetStrengthOperationValue(float operationValue, bool positive, float movementMagnitude)
        {
            BrushStrength = lerp(operationValue, positive ? BrushMaxStrength : BrushMinStrength, movementMagnitude);
        }
        
        internal void SetSizeOperationValue(float operationValue, bool positive, float movementMagnitude)
        {
            BrushSize = lerp(operationValue, positive ? BrushMaxSize : BrushMinSize, movementMagnitude);
        }
        
        internal void UpdateGrassType(ChangeEvent<string> evt)
        {
            GrassType = GrassResources.GrassTypes.FirstOrDefault(type => type.Name.Equals(evt.newValue));
        }
        
        internal void UpdatePaintMode(ChangeEvent<string> evt)
        {
            PaintMode = (PaintMode)Enum.Parse(typeof(PaintMode), evt.newValue);
        }
        
        private void UpdateEditSelectedSurface(ChangeEvent<bool> evt)
        {
            EditSelectedSurface = evt.newValue;
            _editSelectedSurfaceToggle.value = EditSelectedSurface;
        }

        private void UpdateBrushSize(ChangeEvent<float> evt)
        {
           var newSize = clamp(evt.newValue, BrushMinSizeDisplay, BrushMaxSizeDisplay);
            BrushDisplaySize = newSize;
        }
        
        private void UpdateBrushStrength(ChangeEvent<float> evt)
        {
            var newStrength = clamp(evt.newValue, BrushMinStrength, BrushMaxStrength);
            BrushDisplayStrength = newStrength;
        }
        
        private VisualElement CreateBrushFalloffContainer()
        {
            var scrollContainer = new ScrollView();
            
            var contentContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    maxHeight = ImageButtonSize + 4,
                    maxWidth = (ImageButtonSize + 4) * 5
                }
            };
            
            for (var falloffId = 0; falloffId < _falloffs.Length; falloffId++)
            {
                var brushButton = CreateFalloffButton(_falloffs[falloffId], falloffId);
                
                contentContainer.Add(brushButton);
            }
            
            scrollContainer.Add(contentContainer);
            return scrollContainer;
        }
        
        private Button CreateFalloffButton(BrushFalloff falloff, int falloffId)
        {
            var imageButton = CreateImageButton(falloff.Icon, falloffId == _activeFalloffId);

            imageButton.clicked += () => SetActiveFalloff(falloffId);
            
            return imageButton;
        }
        
        private void SetActiveFalloff(int falloffId)
        {
            _activeFalloffId = falloffId;
            ForceRebuildContent();
        }

        internal void ForceRebuildContent()
        {
            displayed = false;
            displayed = true;
        }
        
        private static Button CreateImageButton(Background icon, bool active)
        {
            return new Button
            {
                iconImage = icon,
                style = { 
                    width = ImageButtonSize, height = ImageButtonSize,
                    borderBottomWidth = 0, borderLeftWidth = 0, borderTopWidth = 0, borderRightWidth = 0,
                    marginBottom = 2, marginLeft = 2, marginTop = 2, marginRight = 2,
                    paddingBottom = 0, paddingLeft = 0, paddingTop = 0, paddingRight = 0,
                    backgroundColor = active? ButtonActiveColor : ButtonInactiveColor,
                }
            };
        }
    }
}
