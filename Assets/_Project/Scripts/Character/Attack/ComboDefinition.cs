using UnityEngine;

namespace Wander.Character.Attack
{
    [CreateAssetMenu(fileName = "NewCombo", menuName = "Wander/Combo Definition")]
    public class ComboDefinition : ScriptableObject
    {
        [Tooltip("Display name (debug / UI)")]
        public string ComboName;

        [Tooltip("Sequence of inputs that selects this combo, e.g. [Light, Light, Heavy]")]
        public AttackInputType[] InputPattern;

        [Tooltip("One entry per step — clip, damage, timing windows")]
        public ComboStep[] Steps;
    }
}