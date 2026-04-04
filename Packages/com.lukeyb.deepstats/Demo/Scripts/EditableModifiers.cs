using System.Collections.Generic;
using System.Text;
using LukeyB.DeepStats.User;
using UnityEngine;

namespace LukeyB.DeepStats.Demo
{
    public class EditableModifiers : MonoBehaviour
    {
        public List<EditorDeepModifier> Modifiers;

        private DeepStatsInstance _stats;
        private StringBuilder _rangeSb;
        private StringBuilder _finalSb;

        private GUIStyle _style;

        public void Awake()
        {
            _stats = new DeepStatsInstance();

            _rangeSb = new StringBuilder();
            _finalSb = new StringBuilder();
            _style = new GUIStyle();
        }

        private void OnGUI()
        {
            _style.fontSize = (int)(Screen.width / 40f);

            foreach (var m in _stats.Modifiers.OwnedModifiers)
            {
                _stats.RemoveModifier(m);
            }

            foreach (var m in Modifiers)
            {
                m.ForceReconstruct();   // rebuild the DeepModifier struct in case it's been modified in the editor
                _stats.AddModifier(m);
            }

            _stats.UpdateFinalValues(null);

            _rangeSb.Clear();
            _finalSb.Clear();
            _rangeSb.Append("Add stats to the Modifiers list on the Modifier Collection gameobject. \nExperiment and see the results\n\nRaw Value:\n");
            _finalSb.Append($"\n\n\nFinal Value:\n");

            for (var i = 0; i < DeepStatsConstants.NumStatTypes; i++)
            {
                var statType = (StatType)i;

                var rawValue = _stats.GetRawValue(statType);
                var finalValue = _stats.GetFinalRange(statType);

                var wroteValue = false;
                if (rawValue.x != 0 || rawValue.y != 0)
                {
                    wroteValue = true;

                    _rangeSb.Append(statType.ToString());
                    _rangeSb.Append(": ");

                    if (rawValue.x != rawValue.y)
                    {
                        _rangeSb.Append($"{rawValue.x} - {rawValue.y}");
                    }
                    else
                    {
                        _rangeSb.Append($"{rawValue.x}");
                    }
                }

                if (finalValue.x != 0 || finalValue.y != 0)
                {
                    if (wroteValue)
                    {
                        if (finalValue.x != finalValue.y)
                        {
                            _finalSb.Append($"{finalValue.x} - {finalValue.y}");
                        }
                        else
                        {
                            _finalSb.Append($"{finalValue.x}");
                        }
                    }
                }

                if (wroteValue)
                {
                    _rangeSb.Append("\n");
                    _finalSb.Append("\n");
                }
            }

            GUI.Label(new Rect(5, 40, 300, 50), _rangeSb.ToString(), _style);
            GUI.Label(new Rect(1000, 40, 300, 50), _finalSb.ToString(), _style);
        }

        private void OnDestroy()
        {
            _stats.Dispose();
        }
    }
}