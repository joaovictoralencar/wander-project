using System.Collections.Generic;
using LukeyB.DeepStats.Core;
using LukeyB.DeepStats.Enums;

namespace LukeyB.DeepStats.User
{
    public class DeepModifierCollection
    {
        public IReadOnlyList<DeepModifier> Modifiers { get { return _modifiers; } }

        private List<DeepModifier> _modifiers = new List<DeepModifier>();
        private List<DeepStatsInstance> _statsChildren = new List<DeepStatsInstance>();

        public void AddModifier(EditorDeepModifier mod)
        {
            AddModifier(mod.Value);
        }

        public void AddModifier(DeepModifier mod)
        {
            AddModifierInternal(mod, ModifierSource.Self);
        }

        private void AddModifierInternal(DeepModifier mod, ModifierSource source)
        {
            mod.ModifierSource = source;

            _modifiers.Add(mod);
            foreach (var s in _statsChildren)
            {
                s.AddModifierInternal(mod, ModifierSource.FromParent);
            }
        }

        public void RemoveModifier(EditorDeepModifier mod)
        {
            RemoveModifier(mod.Value);
        }

        public void RemoveModifier(DeepModifier mod)
        {
            RemoveModifierInternal(mod);
        }

        private void RemoveModifierInternal(DeepModifier mod)
        {
            _modifiers.Remove(mod);
            foreach (var s in _statsChildren)
            {
                s.RemoveModifierInternal(mod, ModifierSource.FromParent);
            }
        }

        public void ClearAllModifiers()
        {
            for (var i = _modifiers.Count - 1; i >= 0; i--)
            {
                RemoveModifierInternal(_modifiers[i]);
            }
        }

        public void AddStatsChild(DeepStatsInstance child)
        {
            _statsChildren.Add(child);
            foreach (var m in _modifiers)
            {
                child.AddModifierInternal(m, ModifierSource.FromParent);
            }
        }

        public void RemoveStatsChild(DeepStatsInstance child)
        {
            _statsChildren.Remove(child);
            foreach (var m in _modifiers)
            {
                child.RemoveModifierInternal(m, ModifierSource.FromParent);
            }
        }
    }
}