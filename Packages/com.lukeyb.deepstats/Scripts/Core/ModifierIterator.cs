using System;
using System.Collections;
using System.Collections.Generic;
using LukeyB.DeepStats.Enums;
using Unity.Collections;

namespace LukeyB.DeepStats.Core
{
    public class ModifierIterator
    {
        private List<NativeList<DeepModifier>> AllCollections;
        private bool _countsStale = true;
        private int _allCount;
        private int _ownedCount;

        public ModifierEnumerable AllModifiers
        {
            get
            {
                return new ModifierEnumerable(AllCollections, false);
            }
        }

        public ModifierEnumerable OwnedModifiers
        {
            get
            {
                return new ModifierEnumerable(AllCollections, true);
            }
        }

        public int AllCount
        {
            get
            {
                if (_countsStale)
                {
                    UpdateCounts();
                    _countsStale = false;
                }

                return _allCount;
            }
        }

        public int OwnedCount
        {
            get
            {
                if (_countsStale)
                {
                    UpdateCounts();
                    _countsStale = false;
                }

                return _ownedCount;
            }
        }

        public ModifierIterator()
        {
            AllCollections = new List<NativeList<DeepModifier>>();
        }

        public void AddCollection(NativeList<DeepModifier> collection)
        {
            AllCollections.Add(collection);
        }

        public void SetCountsStale()
        {
            _countsStale = true;
        }

        public void UpdateCounts()
        {
            _allCount = 0;
            _ownedCount = 0;

            foreach (var mod in AllModifiers)
            {
                if (mod.ModifierSource == ModifierSource.Self)
                {
                    _ownedCount++;
                }
                else
                {
                    _allCount++;
                }
            }
        }

        public struct ModifierEnumerable
        {
            private readonly List<NativeList<DeepModifier>> AllCollections;
            bool OwnedModifiersOnly;

            public ModifierEnumerable(List<NativeList<DeepModifier>> allCollections, bool ownedModifiersOnly)
            {
                AllCollections = allCollections;
                OwnedModifiersOnly = ownedModifiersOnly;
            }

            public ModifierEnumerator GetEnumerator()
            {
                return new ModifierEnumerator(AllCollections, OwnedModifiersOnly);
            }
        }

        public struct ModifierEnumerator
        {
            private List<NativeList<DeepModifier>> AllCollections;
            private int CurrentCollectionIndex;
            private int CurrentPositionInCollection;
            private bool OwnedModifiersOnly;

            public ModifierEnumerator(List<NativeList<DeepModifier>> allCollections, bool ownedModifiersOnly)
            {
                AllCollections = allCollections;
                OwnedModifiersOnly = ownedModifiersOnly;
                CurrentCollectionIndex = 0;
                CurrentPositionInCollection = AllCollections.Count > 0 ? AllCollections[CurrentCollectionIndex].Length : 0; // iterate lists backwards so we can safely remove from lists
            }

            public DeepModifier Current
            {
                get
                {
                    return AllCollections[CurrentCollectionIndex][CurrentPositionInCollection];
                }
            }

            public bool MoveNext()
            {
                // iterate lists backwards so we can safely remove from lists
                CurrentPositionInCollection--;
                while (CurrentCollectionIndex < AllCollections.Count)
                {
                    if (CurrentPositionInCollection < 0)
                    {
                        CurrentCollectionIndex++;
                        if (CurrentCollectionIndex >= AllCollections.Count)
                        {
                            break;
                        }

                        CurrentPositionInCollection = AllCollections[CurrentCollectionIndex].Length - 1;
                    }
                    else if (
                        (OwnedModifiersOnly && (AllCollections[CurrentCollectionIndex][CurrentPositionInCollection].ModifierSource != ModifierSource.Self)) ||
                        AllCollections[CurrentCollectionIndex][CurrentPositionInCollection].ModifierSource == ModifierSource.FromAddedTagsModifier)
                    {
                        CurrentPositionInCollection--;
                    }
                    else
                    {
                        return true;
                    }
                }

                return false;
            }

            public void Reset()
            {
                CurrentCollectionIndex = 0;
                CurrentPositionInCollection = AllCollections.Count > 0 ? AllCollections[CurrentCollectionIndex].Length : 0; // iterate lists backwards so we can safely remove from lists
            }
        }
    }
}