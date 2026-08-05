using System;

namespace PirexZenith
{
    /// <summary>
    /// Marks a field, property, method, or constructor for Dependency Injection by PirexZenith.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Constructor, AllowMultiple = false, Inherited = true)]
    public class InjectAttribute : Attribute
    {
        public InjectAttribute()
        {
        }
    }
}
