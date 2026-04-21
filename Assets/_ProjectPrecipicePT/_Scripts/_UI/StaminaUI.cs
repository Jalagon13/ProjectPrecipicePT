using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace ProjectPrecipicePT
{
    [Serializable]
    public struct IntrusionColorMapping
    {
        public StaminaIntrusionType Type;
        public Color Color;
    }

    public class StaminaUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField, Tooltip("The Image representing usable stamina. Should be Filled, Horizontal, Origin Left.")]
        private Image _staminaFillImage;
        
        [SerializeField, Tooltip("Container with a HorizontalLayoutGroup (Middle Right aligned, Control Child Size Width).")]
        private RectTransform _intrusionContainer;

        [Header("Settings")]
        [SerializeField, Tooltip("How fast the UI bars smoothly animate to their target values.")]
        private float _lerpSpeed = 10f;
        
        [SerializeField] private List<IntrusionColorMapping> _intrusionColors;

        private float _targetCurrentStamina;
        private float _targetBaseMaxStamina = 100f; // Prevent division by zero

        private float _visualCurrentStamina;

        // Visual tracking for intrusions
        private Dictionary<StaminaIntrusionType, float> _visualIntrusionAmounts = new();

        private void Start()
        {
            if (StaminaManager.Instance != null)
            {
                StaminaManager.Instance.OnStaminaChanged += HandleStaminaChanged;
                StaminaManager.Instance.OnIntrusionsChanged += HandleIntrusionsChanged;
                    
                // Initialize to current state immediately
                _targetCurrentStamina = StaminaManager.Instance.CurrentStamina;
                _targetBaseMaxStamina = StaminaManager.Instance.CurrentMaxStamina;

                _visualCurrentStamina = _targetCurrentStamina;
                
                HandleIntrusionsChanged();
                UpdateUIBars();
            }
        }

        private void OnDestroy()
        {
            if (StaminaManager.Instance != null)
            {
                StaminaManager.Instance.OnStaminaChanged -= HandleStaminaChanged;
                StaminaManager.Instance.OnIntrusionsChanged -= HandleIntrusionsChanged;
            }
        }

        private void Update()
        {
            if (_targetBaseMaxStamina <= 0f) return;

            // Smoothly interpolate stamina
            _visualCurrentStamina = Mathf.Lerp(_visualCurrentStamina, _targetCurrentStamina, Time.deltaTime * _lerpSpeed);

            // Smoothly interpolate all active intrusions
            bool intrusionsChanged = false;
            foreach (var intrusion in StaminaManager.Instance.Intrusions)
            {
                if (!_visualIntrusionAmounts.ContainsKey(intrusion.Type))
                {
                    _visualIntrusionAmounts[intrusion.Type] = 0f;
                }
                
                float currentVis = _visualIntrusionAmounts[intrusion.Type];
                float newVis = Mathf.Lerp(currentVis, intrusion.Amount, Time.deltaTime * _lerpSpeed);
                _visualIntrusionAmounts[intrusion.Type] = newVis;
                
                if (Mathf.Abs(currentVis - newVis) > 0.01f)
                {
                    intrusionsChanged = true;
                }
            }
            
            // Clean up removed/vanishing intrusions
            List<StaminaIntrusionType> toRemove = new();
            List<StaminaIntrusionType> keys = new List<StaminaIntrusionType>(_visualIntrusionAmounts.Keys);
            foreach (var key in keys)
            {
                bool found = false;
                foreach (var intrusion in StaminaManager.Instance.Intrusions)
                {
                    if (intrusion.Type == key) { found = true; break; }
                }
                
                if (!found)
                {
                    float currentVis = _visualIntrusionAmounts[key];
                    float newVis = Mathf.Lerp(currentVis, 0f, Time.deltaTime * _lerpSpeed);
                    _visualIntrusionAmounts[key] = newVis;
                    intrusionsChanged = true;
                    
                    if (newVis < 0.1f)
                    {
                        toRemove.Add(key);
                    }
                }
            }
            
            foreach (var key in toRemove)
            {
                _visualIntrusionAmounts.Remove(key);
                HandleIntrusionsChanged(); // Rebuild UI hierarchy
            }

            if (intrusionsChanged)
            {
                UpdateIntrusionWidths();
            }

            UpdateUIBars();
        }

        private void HandleStaminaChanged(int currentStamina, int maxStamina, int baseMaxStamina)
        {
            _targetCurrentStamina = currentStamina;
            _targetBaseMaxStamina = baseMaxStamina;
        }

        private void HandleIntrusionsChanged()
        {
            if (_intrusionContainer == null) return;

            // Clear container
            foreach (Transform child in _intrusionContainer)
            {
                Destroy(child.gameObject);
            }

            // Rebuild vanishing ones first so they sit properly
            foreach (var key in _visualIntrusionAmounts.Keys)
            {
                bool found = false;
                foreach (var intrusion in StaminaManager.Instance.Intrusions)
                {
                    if (intrusion.Type == key) { found = true; break; }
                }
                
                if (!found)
                {
                    CreateUIBlock(key, "Vanishing");
                }
            }

            // Rebuild active ones in order
            foreach (var intrusion in StaminaManager.Instance.Intrusions)
            {
                CreateUIBlock(intrusion.Type, "Active");
                
                if (!_visualIntrusionAmounts.ContainsKey(intrusion.Type))
                {
                    _visualIntrusionAmounts[intrusion.Type] = intrusion.Amount; // Snap immediately if you want, or 0f to animate
                }
            }
            
            UpdateIntrusionWidths();
        }

        private void CreateUIBlock(StaminaIntrusionType type, string suffix)
        {
            GameObject block = new GameObject($"Intrusion_{type}_{suffix}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            block.transform.SetParent(_intrusionContainer, false);
            
            RectTransform rt = block.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, _intrusionContainer.rect.height);
            
            Image img = block.GetComponent<Image>();
            img.color = GetColorForType(type);
        }
        
        private void UpdateIntrusionWidths()
        {
            if (_intrusionContainer == null || _targetBaseMaxStamina <= 0f) return;
            
            // We base the percentage width off the entire parent container's width
            float totalWidth = _intrusionContainer.rect.width;

            foreach (Transform child in _intrusionContainer)
            {
                string name = child.name;
                StaminaIntrusionType type = StaminaIntrusionType.Weight;
                foreach (StaminaIntrusionType t in Enum.GetValues(typeof(StaminaIntrusionType)))
                {
                    if (name.Contains(t.ToString()))
                    {
                        type = t;
                        break;
                    }
                }
                
                if (_visualIntrusionAmounts.TryGetValue(type, out float amount))
                {
                    LayoutElement le = child.GetComponent<LayoutElement>();
                    if (le != null)
                    {
                        le.preferredWidth = (amount / _targetBaseMaxStamina) * totalWidth;
                    }
                }
            }
        }

        private Color GetColorForType(StaminaIntrusionType type)
        {
            if (_intrusionColors != null)
            {
                foreach (var mapping in _intrusionColors)
                {
                    if (mapping.Type == type) return mapping.Color;
                }
            }
            return Color.gray; // Default
        }

        private void UpdateUIBars()
        {
            if (_targetBaseMaxStamina <= 0f) return;

            if (_staminaFillImage != null)
            {
                _staminaFillImage.fillAmount = Mathf.Clamp01(_visualCurrentStamina / _targetBaseMaxStamina);
            }
        }
    }
}
