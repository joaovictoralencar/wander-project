using System;
using System.Collections;
using LukeyB.DeepStats.User;
using Unity.Collections;
using UnityEngine;

namespace LukeyB.DeepStats.Core
{
    public class ModifierTags
    {
        public ModifierTagLookup Tags;
        public Action TagsChanged;

        public bool this[ModifierTag tag]
        {
            set
            {
                if (Tags.IsTagSet(tag) != value)
                {
                    Tags.SetTag(tag, value);
                    TagsChanged?.Invoke();
                }
            }
        }
    }
}