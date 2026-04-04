using System.Collections;
using UnityEngine;
using System;

namespace LukeyB.DeepStats.Core
{
    public class ScriptableEnum<T> : ScriptableEnum where T : struct, Enum
    {
        [HideInInspector] public T EnumValue;

        private void OnEnable()
        {
            T parsedEnum;
            if (Enum.TryParse(StringValue, out parsedEnum))
            {
                EnumValue = parsedEnum;
            }
        }

        private void OnValidate()
        {
            if (StringValue != name)
            {
                Debug.LogError($"DeepStats Configuration is out of sync and needs to be regenerated.\nScriptable Object name: {name}, Enum Name: {StringValue}");
            }
        }
    }

    public class ScriptableEnum : ScriptableObject
    {
        // we need to update it only when regenerating configuration, otherwise the enum wont parse and we'll get errors in the console
        /// <summary>
        /// Dont Touch, this is automatically set by the DeepStatsConfiguration
        /// </summary>
        [HideInInspector] public string StringValue;
    }
}