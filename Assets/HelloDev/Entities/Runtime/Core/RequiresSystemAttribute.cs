using System;

namespace HelloDev.Entities
{
    /// <summary>
    /// Declares that a bridge depends on the specified system type(s).
    /// Systems are auto-registered when the bridge initializes.
    /// Replaces the old <c>GetRequiredSystems()</c> virtual override.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class RequiresSystemAttribute : Attribute
    {
        public Type[] SystemTypes { get; }

        public RequiresSystemAttribute(params Type[] systemTypes)
        {
            SystemTypes = systemTypes;
        }
    }
}
