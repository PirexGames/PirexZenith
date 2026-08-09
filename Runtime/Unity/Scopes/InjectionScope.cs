using UnityEngine;

namespace PirexZenith
{
    /// <summary>
    /// An isolated LifetimeScope designed primarily for Prefabs or Scenes that only need to perform dependency injection on themselves and their children.
    /// It automatically inherits from a parent Scope (like ProjectScope or SceneScope) but does NOT register itself as a potential parent for other dynamic scopes.
    /// </summary>
    [AddComponentMenu("PirexZenith/Injection Scope (Isolated)")]
    public class InjectionScope : LifetimeScope
    {
        /// <summary>
        /// Prevents this scope from being added to the global ActiveScopes list,
        /// ensuring it remains isolated and does not accidentally become the parent of subsequent dynamic instantiations.
        /// </summary>
        protected override bool AutoRegisterToActiveScopes => false;
    }
}
