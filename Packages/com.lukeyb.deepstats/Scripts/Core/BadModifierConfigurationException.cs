using System;
using System.Collections;
using UnityEngine;

namespace LukeyB.DeepStats.Core
{
    public class BadModifierConfigurationException : Exception
    {
        public BadModifierConfigurationException() : base() { }
        public BadModifierConfigurationException(string message) : base(message) { }
        public BadModifierConfigurationException(string message, Exception innerException) : base(message, innerException) { }

    }
}