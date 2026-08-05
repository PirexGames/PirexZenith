using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace PirexZenith
{
    /// <summary>
    /// Versatile scope managing the lifecycle of a DIContainer in Unity.
    /// Supports Global Project Scope (DontDestroyOnLoad), Scene Scope, or Sub-object/UI Modal Scope.
    /// Provides 3-tier runtime parent scope resolution without cross-scene Inspector references.
    /// </summary>
    [DefaultExecutionOrder(-5000)] // Ensure initialization before standard scripts
    [DisallowMultipleComponent]
    public class LifetimeScope : MonoBehaviour
    {
        /// <summary>
        /// Global list of active LifetimeScopes ordered by runtime initialization sequence.
        /// </summary>
        public static readonly List<LifetimeScope> ActiveScopes = new List<LifetimeScope>();

        [Header("Installers")]
        [FormerlySerializedAs("installers")]
        [SerializeField]
        private List<MonoInstaller> _installers = new List<MonoInstaller>();

        [Tooltip("Parent LifetimeScope class name (e.g., nameof(CoreSceneScope)) or parent scene name for runtime hierarchy resolution. Can be overridden in code when subclassing LifetimeScope.")]
        [SerializeField]
        private string _parentScopeName = "";

        [Header("Auto-Baked Targets (Do not modify manually)")]
        [SerializeField]
        public List<MonoBehaviour> bakedTargets = new List<MonoBehaviour>();

        /// <summary>
        /// The active DIContainer managed by this LifetimeScope.
        /// </summary>
        public DIContainer Container { get; protected set; }

        /// <summary>
        /// The class name (nameof) of the parent LifetimeScope used for runtime container linking.
        /// Developers can override this property when subclassing LifetimeScope:
        /// <code>
        /// protected override string ParentScopeName => nameof(CoreSceneScope);
        /// </code>
        /// </summary>
        public virtual string ParentScopeName
        {
            get => _parentScopeName;
            set => _parentScopeName = value;
        }

        private DIContainer _runtimeParentContainer;

        // Cached lists for Pure C# Systems (Lifecycle EntryPoints)
        private List<IInitializable> _initializables;
        private List<ITickable> _tickables;
        private List<IFixedTickable> _fixedTickables;
        private List<ILateTickable> _lateTickables;
        private List<IDisposable> _disposables;

        /// <summary>
        /// Manually assigns a parent container to this LifetimeScope before Awake/Initialize.
        /// (Tier 1 Resolution)
        /// </summary>
        public void SetParentContainer(DIContainer parentContainer)
        {
            _runtimeParentContainer = parentContainer;
        }

        protected virtual void Awake()
        {
            if (!ActiveScopes.Contains(this))
            {
                ActiveScopes.Add(this);
            }

            InitializeContainer();
            PerformScopeInjection();
        }

        protected virtual void InitializeContainer()
        {
            var builder = new ContainerBuilder();

            // Resolve Parent Container using 3-Tier Resolution (prioritizing nameof(ScopeClass))
            DIContainer parentContainer = ResolveParentContainer();
            if (parentContainer != null)
            {
                builder.WithParent(parentContainer);
            }

            // If no Installers are manually assigned, automatically discover them in child GameObjects
            if (_installers == null || _installers.Count == 0)
            {
                _installers = new List<MonoInstaller>(GetComponentsInChildren<MonoInstaller>(true));
            }

            foreach (var installer in _installers)
            {
                if (installer != null)
                {
                    installer.InstallBindings(builder);
                }
            }

            // Register this LifetimeScope instance into the container
            builder.RegisterInstance(this);
            builder.RegisterInstance<LifetimeScope>(this);

            Container = builder.Build();
            InitializeSystems();
        }

        /// <summary>
        /// Initializes and links registered Pure C# Systems implementing lifecycle interfaces.
        /// </summary>
        protected virtual void InitializeSystems()
        {
            if (Container == null) return;

            _initializables = Container.ResolveAll<IInitializable>();
            _tickables = Container.ResolveAll<ITickable>();
            _fixedTickables = Container.ResolveAll<IFixedTickable>();
            _lateTickables = Container.ResolveAll<ILateTickable>();
            _disposables = Container.ResolveAll<IDisposable>();

            if (_initializables != null && _initializables.Count > 0)
            {
                for (int i = 0; i < _initializables.Count; i++)
                {
                    try
                    {
                        _initializables[i]?.Initialize();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[PirexZenith] Error while calling Initialize() on Pure C# System '{_initializables[i]?.GetType().Name}': {ex}");
                    }
                }
            }
        }

        protected virtual void Update()
        {
            if (_tickables == null || _tickables.Count == 0) return;
            for (int i = 0; i < _tickables.Count; i++)
            {
                try
                {
                    _tickables[i]?.Tick();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[PirexZenith] Error while calling Tick() on Pure C# System '{_tickables[i]?.GetType().Name}': {ex}");
                }
            }
        }

        protected virtual void FixedUpdate()
        {
            if (_fixedTickables == null || _fixedTickables.Count == 0) return;
            for (int i = 0; i < _fixedTickables.Count; i++)
            {
                try
                {
                    _fixedTickables[i]?.FixedTick();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[PirexZenith] Error while calling FixedTick() on Pure C# System '{_fixedTickables[i]?.GetType().Name}': {ex}");
                }
            }
        }

        protected virtual void LateUpdate()
        {
            if (_lateTickables == null || _lateTickables.Count == 0) return;
            for (int i = 0; i < _lateTickables.Count; i++)
            {
                try
                {
                    _lateTickables[i]?.LateTick();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[PirexZenith] Error while calling LateTick() on Pure C# System '{_lateTickables[i]?.GetType().Name}': {ex}");
                }
            }
        }

        /// <summary>
        /// Resolves the Parent Container:
        /// 1. Programmatic Override (configured via SetParentContainer or CreateChildScope)
        /// 2. Explicit Lookup by nameof(ClassType), GameObject Name, or Scene Name
        /// 3. Automatic Fallback: If ParentScopeName is empty ("") (default), automatically link to the most recent active parent scope (e.g. SceneScope / ProjectScope) in ActiveScopes.
        /// If explicitly set to "None", the Scope operates independently and does NOT inherit any parent container.
        /// </summary>
        protected virtual DIContainer ResolveParentContainer()
        {
            // 1. Programmatic override
            if (_runtimeParentContainer != null)
            {
                return _runtimeParentContainer;
            }

            // 2. Explicit Lookup by nameof(ClassType), GameObject Name, or Scene Name
            string targetName = ParentScopeName;
            if (!string.IsNullOrEmpty(targetName) && !string.Equals(targetName, "None", StringComparison.OrdinalIgnoreCase))
            {
                for (int i = ActiveScopes.Count - 1; i >= 0; i--)
                {
                    var scope = ActiveScopes[i];
                    if (scope != null && scope != this && scope.Container != null)
                    {
                        // Priority 1: Class name (nameof(ClassScope)) - Safe against IDE renaming refactors
                        // Priority 2: GameObject Name or Scene Name
                        if (string.Equals(scope.GetType().Name, targetName, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(scope.name, targetName, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(scope.gameObject.scene.name, targetName, StringComparison.OrdinalIgnoreCase))
                        {
                            return scope.Container;
                        }
                    }
                }
                Debug.LogWarning($"[PirexZenith] Could not find parent LifetimeScope matching class name (nameof) or scene '{targetName}'. Scope will operate independently.");
                return null;
            }

            // 3. Automatic Fallback: If ParentScopeName is empty ("") (default value),
            // automatically link to the most recent active parent scope (e.g., SceneScope / ProjectScope) in ActiveScopes!
            if (!string.Equals(targetName, "None", StringComparison.OrdinalIgnoreCase))
            {
                for (int i = ActiveScopes.Count - 1; i >= 0; i--)
                {
                    var scope = ActiveScopes[i];
                    if (scope != null && scope != this && scope.Container != null)
                    {
                        return scope.Container;
                    }
                }
            }

            // If explicitly set to "None" or no active scope found, return null (standalone)
            return null;
        }

        protected virtual void PerformScopeInjection()
        {
            if (Container == null) return;

            if (bakedTargets == null || bakedTargets.Count == 0)
            {
                // Bulletproof Fallback: If Editor bake data is empty, dynamically discover targets in child GameObjects
                var allMonos = GetComponentsInChildren<MonoBehaviour>(true);
                foreach (var mono in allMonos)
                {
                    if (mono != null && mono != this)
                    {
                        Container.Inject(mono);
                    }
                }
            }
            else
            {
                for (int i = 0; i < bakedTargets.Count; i++)
                {
                    var target = bakedTargets[i];
                    if (target != null && target != this)
                    {
                        Container.Inject(target);
                    }
                }
            }
        }

        /// <summary>
        /// Programmatically creates a child LifetimeScope (ideal for Prefabs, UI Modals, or dynamic Sub-scopes).
        /// </summary>
        public static LifetimeScope CreateChildScope(LifetimeScope parent, GameObject childRoot)
        {
            if (childRoot == null) throw new ArgumentNullException(nameof(childRoot));
            var childScope = childRoot.GetComponent<LifetimeScope>();
            if (childScope == null)
            {
                childScope = childRoot.AddComponent<LifetimeScope>();
            }
            if (parent != null && parent.Container != null)
            {
                childScope.SetParentContainer(parent.Container);
            }
            return childScope;
        }

        protected virtual void OnDestroy()
        {
            ActiveScopes.Remove(this);

            if (_disposables != null)
            {
                for (int i = _disposables.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        _disposables[i]?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[PirexZenith] Error while calling Dispose() on Pure C# System '{_disposables[i]?.GetType().Name}': {ex}");
                    }
                }
                _disposables.Clear();
            }

            Container?.Dispose();
            Container = null;
        }
    }
}
