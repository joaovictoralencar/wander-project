using System.Collections.Generic;
using System.Text;
using LukeyB.DeepStats.User;
using LukeyB.DeepStats.Core;
using UnityEngine;

namespace LukeyB.DeepStats.Demo
{
    public class StatsLogger : MonoBehaviour
    {
        public DeepStatsMB Stats;
        public DeepModifierCollectionMB RandomModifiers;

        [Space]
        public int NumRandomModifiers = 4;
        public List<EditorDeepModifier> PossibleRandomModifiers;

        [Space]
        public List<DeepStatsMB> EquippableItems;
        private DeepStatsMB EquippedItem;

        [Space]
        public float HorizontalPosition;
        public string Title;
        public string StatsDescription;
        public string ModifiersDescription;
        public string FinalValuesDescription;

        private StringBuilder _sb = new StringBuilder();
        private string _description;
        private List<StatType> _offensiveStats = new List<StatType> { StatType.PhysicalDamage, StatType.ElementalDamage, StatType.AttackSpeed, StatType.CriticalChance };
        private GUIStyle _style;

        private void Awake()
        {
            _style = new GUIStyle();

            AddRandomModifiers();

            if (EquippableItems.Count > 0)
            {
                EquipItem(0);
                Stats.DeepStats.SetTag(ModifierTag.Sword, false);
                Stats.DeepStats.SetTag(ModifierTag.Spear, false);
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                AddRandomModifiers();
            }

            if (EquippableItems.Count == 0)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                EquipItem(0);
                Stats.DeepStats.SetTag(ModifierTag.Sword, false);
                Stats.DeepStats.SetTag(ModifierTag.Spear, false);
            }

            if (Input.GetKeyDown(KeyCode.Alpha2) && EquippableItems.Count > 0)
            {
                EquipItem(1);
                Stats.DeepStats.SetTag(ModifierTag.Sword, true);
                Stats.DeepStats.SetTag(ModifierTag.Spear, false);
            }

            if (Input.GetKeyDown(KeyCode.Alpha3) && EquippableItems.Count > 1)
            {
                EquipItem(2);
                Stats.DeepStats.SetTag(ModifierTag.Sword, false);
                Stats.DeepStats.SetTag(ModifierTag.Spear, true);
            }
        }

        private void AddRandomModifiers()
        {
            if (NumRandomModifiers <= 0)
            {
                return;
            }

            RandomModifiers.ModifierCollection.ClearAllModifiers();

            if (PossibleRandomModifiers.Count == 0)
                return;

            for (var i = 0; i < NumRandomModifiers; i++)
            {
                RandomModifiers.ModifierCollection.AddModifier(PossibleRandomModifiers[Random.Range(0, PossibleRandomModifiers.Count)]);
            }
        }

        private void EquipItem(int itemIndex)
        {
            if (EquippedItem != null)
            {
                EquippedItem.gameObject.SetActive(false);
                Stats.DeepStats.RemoveAddedStatsSource(EquippedItem.DeepStats);
            }

            EquippedItem = EquippableItems[itemIndex];
            EquippedItem.gameObject.SetActive(true);
            Stats.DeepStats.AddAddedStatsSource(EquippedItem.DeepStats);
        }

        private void OnGUI()
        {
            UpdateStatDescription();

            //if (Stats.DeepStats.FinalValuesAreStale)
            //{
            //    UpdateStatDescription();
            //}

            _style.fontSize = (int)(Screen.width / 80f);
            GUI.Label(new Rect(HorizontalPosition, 40, 300, 50), _description, _style);
        }

        private void UpdateStatDescription()
        {
            _sb.Clear();
            _sb.Append(Title + "\n\n\n");

            if (Stats.DeepStats.Modifiers.OwnedCount > 0)
            {
                _sb.Append(StatsDescription + "\n\n");
                foreach (var mod in Stats.DeepStats.Modifiers.OwnedModifiers)
                {
                    _sb.Append(DeepStatsUtils.GetStatModifierDescription(mod));
                    _sb.Append("\n");
                }
                _sb.Append("\n\n");
            }


            if (RandomModifiers != null)
            {
                if (RandomModifiers.ModifierCollection.Modifiers.Count > 0)
                {
                    _sb.Append(ModifiersDescription + "\n\n");
                    foreach (var mod in RandomModifiers.ModifierCollection.Modifiers)
                    {
                        _sb.Append(DeepStatsUtils.GetStatModifierDescription(mod));
                        _sb.Append("\n");
                    }
                    _sb.Append("\n\n");
                }
            }

            _sb.Append(FinalValuesDescription + "\n\n");
            Stats.DeepStats.UpdateFinalValues(null);
            foreach (var statType in _offensiveStats)
            {
                var finalValue = Stats.DeepStats.GetRawValue(statType);
                if (finalValue.x == 0)
                    continue;

                if (finalValue.x == finalValue.y)
                {
                    _sb.Append(statType.ToString() + ": " + finalValue.x);
                    _sb.Append("\n");
                }
                else
                {
                    _sb.Append(statType.ToString() + $": {finalValue.x} - {finalValue.y}");
                    _sb.Append("\n");
                }
            }

            _description = _sb.ToString();
        }
    }
}