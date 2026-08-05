using System.Collections.Generic;
using UnityEngine;

namespace PirexZenith
{
    /// <summary>
    /// Component storing a cached list of MonoBehaviour targets requiring injection on a Prefab.
    /// Prevents redundant hierarchy scans when instantiating Prefabs at runtime.
    /// </summary>
    [DisallowMultipleComponent]
    public class DIPrefabCache : MonoBehaviour
    {
        [SerializeField]
        private List<MonoBehaviour> _cachedTargets = new List<MonoBehaviour>();

        public List<MonoBehaviour> CachedTargets => _cachedTargets;

        /// <summary>
        /// Indicates whether this instance has already been injected by the DIContainer (used by Object Pooling).
        /// </summary>
        public bool IsInjected { get; set; }

        /// <summary>
        /// Updates the cached target list from Editor baking systems or runtime fallback.
        /// </summary>
        public void SetCachedTargets(List<MonoBehaviour> targets)
        {
            _cachedTargets = targets ?? new List<MonoBehaviour>();
        }
    }
}
