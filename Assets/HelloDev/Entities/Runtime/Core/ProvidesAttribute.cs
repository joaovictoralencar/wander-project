using System;

namespace HelloDev.Entities
{
    /// <summary>
    /// Declares which component type(s) a bridge adds during <c>OnInitialize</c>.
    /// Used by <see cref="EcsEntityRoot"/> editor validation to detect duplicates
    /// and missing components. Replaces the old <c>GetProvidedComponents()</c> virtual override.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class ProvidesAttribute : Attribute
    {
        public Type[] ComponentTypes { get; }

        public ProvidesAttribute(params Type[] componentTypes)
        {
            ComponentTypes = componentTypes;
        }
    }
}
