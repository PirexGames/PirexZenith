using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using PirexZenith.Core;
using PirexZenith.Unity;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
#if PIREX_VCONTAINER_ENABLED
using VContainer;
using VContainer.Unity;
#endif

namespace PirexZenith.Editor.Benchmark
{
    public interface IBenchSingleton { void DoWork(); }
    public class BenchSingleton : IBenchSingleton { public void DoWork() { } }

    public class BenchServiceA { }
    public class BenchServiceB
    {
        public readonly BenchServiceA A;
        public BenchServiceB(BenchServiceA a) { A = a; }
    }
    public class BenchServiceC
    {
        public readonly BenchServiceB B;
        public BenchServiceC(BenchServiceB b) { B = b; }
    }
    public class BenchServiceD
    {
        public readonly BenchServiceC C;
        public BenchServiceD(BenchServiceC c) { C = c; }
    }
    public class BenchServiceE
    {
        public readonly BenchServiceD D;
        public BenchServiceE(BenchServiceD d) { D = d; }
    }
    public interface IBenchMultiService { void Work(); }
    public class BenchMulti1 : IBenchMultiService { public void Work() { } }
    public class BenchMulti2 : IBenchMultiService { public void Work() { } }
    public class BenchMulti3 : IBenchMultiService { public void Work() { } }
    public class BenchDummyService { }

    public class BenchMonoBehaviour : MonoBehaviour
    {
        [PirexZenith.Core.Inject]
#if PIREX_VCONTAINER_ENABLED
        [VContainer.Inject]
#endif
#if PIREX_REFLEX_ENABLED
        [Reflex.Attributes.Inject]
#endif
        public BenchServiceA A { get; private set; }
    }

    public class DIBenchmarkRunner : EditorWindow
    {
        private struct BenchResult
        {
            public string TestName;
            public double PirexMs;
            public long PirexGc;
            public int PirexGen0;
            public double VContainerMs;
            public long VContainerGc;
            public int VContainerGen0;
            public double ReflexMs;
            public long ReflexGc;
            public int ReflexGen0;
        }

        private List<BenchResult> _results = new List<BenchResult>();
        private Vector2 _scrollPos;
        private bool _isRunning = false;

        [MenuItem("Tools/PirexZenith/🏁 Run 3-Way DI Benchmark")]
        public static void ShowWindow()
        {
            var win = GetWindow<DIBenchmarkRunner>("DI 3-Way Benchmark");
            win.minSize = new Vector2(760, 420);
            win.Show();
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            GUILayout.Label("🏁 PirexZenith vs. VContainer vs. Reflex Performance Benchmark", EditorStyles.boldLabel);
            GUILayout.Label("Scientific comparison measuring Execution Time (ms) & GC Allocation (Bytes).", EditorStyles.miniLabel);
            GUILayout.Space(10);

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label("Libraries Status:", EditorStyles.boldLabel, GUILayout.Width(120));
            GUILayout.Label("👑 PirexZenith: Ready (O(1) Pre-baked)");

#if PIREX_VCONTAINER_ENABLED
            GUILayout.Label("VContainer: Installed", EditorStyles.boldLabel);
#else
            GUILayout.Label("VContainer: Not Installed", EditorStyles.miniLabel);
#endif

#if PIREX_REFLEX_ENABLED
            GUILayout.Label("Reflex: Installed", EditorStyles.boldLabel);
#else
            GUILayout.Label("Reflex: Not Installed", EditorStyles.miniLabel);
#endif
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(15);

            GUI.enabled = !_isRunning;
            if (GUILayout.Button("▶ RUN FULL BENCHMARK SUITE (4 TESTS)", GUILayout.Height(35)))
            {
                RunBenchmarks();
            }
            GUI.enabled = true;

            GUILayout.Space(15);

            if (_results.Count > 0)
            {
                GUILayout.Label("📊 Benchmark Results Summary:", EditorStyles.boldLabel);
                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

                DrawResultHeader();
                foreach (var res in _results)
                {
                    DrawResultRow(res);
                }

                EditorGUILayout.EndScrollView();

                GUILayout.Space(10);
                if (GUILayout.Button("📋 Log Markdown Comparison Table to Console"))
                {
                    LogMarkdownTable();
                }
            }
        }

        private void DrawResultHeader()
        {
            EditorGUILayout.BeginHorizontal("box");
            GUILayout.Label("Test Name", EditorStyles.boldLabel, GUILayout.Width(190));
            GUILayout.Label("👑 PirexZenith", EditorStyles.boldLabel, GUILayout.Width(175));
            GUILayout.Label("VContainer", EditorStyles.boldLabel, GUILayout.Width(175));
            GUILayout.Label("Reflex", EditorStyles.boldLabel, GUILayout.Width(175));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawResultRow(BenchResult res)
        {
            EditorGUILayout.BeginHorizontal("box");
            GUILayout.Label(res.TestName, GUILayout.Width(190));
            GUILayout.Label($"{res.PirexMs:F2}ms | {FormatMemory(res.PirexGc)} | G0:{res.PirexGen0}", EditorStyles.boldLabel, GUILayout.Width(175));
            GUILayout.Label($"{res.VContainerMs:F2}ms | {FormatMemory(res.VContainerGc)} | G0:{res.VContainerGen0}", GUILayout.Width(175));
            GUILayout.Label($"{res.ReflexMs:F2}ms | {FormatMemory(res.ReflexGc)} | G0:{res.ReflexGen0}", GUILayout.Width(175));
            EditorGUILayout.EndHorizontal();
        }

        private string FormatMemory(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / (1024.0 * 1024.0):F2} MB";
        }

        private void RunBenchmarks()
        {
            _isRunning = true;
            _results.Clear();

            try
            {
                _results.Add(RunTest1_ContainerBuild());
                _results.Add(RunTest2_TransientResolve());
                _results.Add(RunTest3_SingletonResolve());
                _results.Add(RunTest4_GameObjectInjection());
                _results.Add(RunTest5_DeepTreeResolve());
                _results.Add(RunTest6_MultiBindingResolve());

                LogMarkdownTable();
            }
            finally
            {
                _isRunning = false;
                Repaint();
            }
        }

        private BenchResult RunTest1_ContainerBuild()
        {
            var res = new BenchResult { TestName = "1. Container Build (100 Svc)" };

            // 👑 PirexZenith
            Measure(() =>
            {
                var builder = new PirexZenith.Core.ContainerBuilder();
                for (int i = 0; i < 100; i++) builder.RegisterTransient<BenchDummyService>();
                builder.Build();
            }, out res.PirexMs, out res.PirexGc, out res.PirexGen0);

            // VContainer
#if PIREX_VCONTAINER_ENABLED
            Measure(() =>
            {
                var builder = new VContainer.ContainerBuilder();
                for (int i = 0; i < 100; i++) builder.Register<BenchDummyService>(VContainer.Lifetime.Transient);
                builder.Build();
            }, out res.VContainerMs, out res.VContainerGc, out res.VContainerGen0);
#endif

            // Reflex
#if PIREX_REFLEX_ENABLED
            Measure(() =>
            {
                var builder = new Reflex.Core.ContainerBuilder();
                for (int i = 0; i < 100; i++) builder.RegisterType(typeof(BenchDummyService), Reflex.Enums.Lifetime.Transient, Reflex.Enums.Resolution.Lazy);
                builder.Build();
            }, out res.ReflexMs, out res.ReflexGc, out res.ReflexGen0);
#endif

            return res;
        }

        private BenchResult RunTest2_TransientResolve()
        {
            var res = new BenchResult { TestName = "2. 50,000x Transient Resolve" };

            // 👑 PirexZenith
            {
                var builder = new PirexZenith.Core.ContainerBuilder();
                builder.RegisterTransient<BenchServiceA>();
                builder.RegisterTransient<BenchServiceB>();
                builder.RegisterTransient<BenchServiceC>();
                var container = builder.Build();
                container.Resolve<BenchServiceC>(); // Warmup

                Measure(() =>
                {
                    for (int i = 0; i < 50000; i++) container.Resolve<BenchServiceC>();
                }, out res.PirexMs, out res.PirexGc, out res.PirexGen0);
            }

            // VContainer
#if PIREX_VCONTAINER_ENABLED
            {
                var builder = new VContainer.ContainerBuilder();
                builder.Register<BenchServiceA>(VContainer.Lifetime.Transient);
                builder.Register<BenchServiceB>(VContainer.Lifetime.Transient);
                builder.Register<BenchServiceC>(VContainer.Lifetime.Transient);
                var container = builder.Build();
                container.Resolve<BenchServiceC>(); // Warmup

                Measure(() =>
                {
                    for (int i = 0; i < 50000; i++) container.Resolve<BenchServiceC>();
                }, out res.VContainerMs, out res.VContainerGc, out res.VContainerGen0);
            }
#endif

            // Reflex
#if PIREX_REFLEX_ENABLED
            {
                var builder = new Reflex.Core.ContainerBuilder();
                builder.RegisterType(typeof(BenchServiceA), Reflex.Enums.Lifetime.Transient, Reflex.Enums.Resolution.Lazy);
                builder.RegisterType(typeof(BenchServiceB), Reflex.Enums.Lifetime.Transient, Reflex.Enums.Resolution.Lazy);
                builder.RegisterType(typeof(BenchServiceC), Reflex.Enums.Lifetime.Transient, Reflex.Enums.Resolution.Lazy);
                var container = builder.Build();
                container.Resolve<BenchServiceC>(); // Warmup

                Measure(() =>
                {
                    for (int i = 0; i < 50000; i++) container.Resolve<BenchServiceC>();
                }, out res.ReflexMs, out res.ReflexGc, out res.ReflexGen0);
            }
#endif

            return res;
        }

        private BenchResult RunTest3_SingletonResolve()
        {
            var res = new BenchResult { TestName = "3. 100,000x Singleton Resolve" };

            // 👑 PirexZenith
            {
                var builder = new PirexZenith.Core.ContainerBuilder();
                builder.RegisterSingleton<BenchSingleton, IBenchSingleton>();
                var container = builder.Build();
                container.Resolve<IBenchSingleton>(); // Warmup

                Measure(() =>
                {
                    for (int i = 0; i < 100000; i++) container.Resolve<IBenchSingleton>();
                }, out res.PirexMs, out res.PirexGc, out res.PirexGen0);
            }

            // VContainer
#if PIREX_VCONTAINER_ENABLED
            {
                var builder = new VContainer.ContainerBuilder();
                builder.Register<BenchSingleton>(VContainer.Lifetime.Singleton).As<IBenchSingleton>();
                var container = builder.Build();
                container.Resolve<IBenchSingleton>(); // Warmup

                Measure(() =>
                {
                    for (int i = 0; i < 100000; i++) container.Resolve<IBenchSingleton>();
                }, out res.VContainerMs, out res.VContainerGc, out res.VContainerGen0);
            }
#endif

            // Reflex
#if PIREX_REFLEX_ENABLED
            {
                var builder = new Reflex.Core.ContainerBuilder();
                builder.RegisterType(typeof(BenchSingleton), new[] { typeof(IBenchSingleton) }, Reflex.Enums.Lifetime.Singleton, Reflex.Enums.Resolution.Lazy);
                var container = builder.Build();
                container.Resolve<IBenchSingleton>(); // Warmup

                Measure(() =>
                {
                    for (int i = 0; i < 100000; i++) container.Resolve<IBenchSingleton>();
                }, out res.ReflexMs, out res.ReflexGc, out res.ReflexGen0);
            }
#endif

            return res;
        }

        private BenchResult RunTest4_GameObjectInjection()
        {
            var res = new BenchResult { TestName = "4. 5,000x GameObject DI Inject" };

            GameObject prefab = new GameObject("BenchPrefab");
            prefab.AddComponent<BenchMonoBehaviour>();
            var cache = prefab.AddComponent<DIPrefabCache>();
            cache.SetCachedTargets(new List<UnityEngine.MonoBehaviour> { prefab.GetComponent<BenchMonoBehaviour>() });
            prefab.SetActive(false);

            List<GameObject> created = new List<GameObject>(5000);

            // 👑 PirexZenith
            {
                var builder = new PirexZenith.Core.ContainerBuilder();
                builder.RegisterSingleton<BenchServiceA>();
                var container = builder.Build();
                var first = UnityContainerExtensions.Instantiate(container, prefab);
                DestroyImmediate(first); // Warmup

                Measure(() =>
                {
                    for (int i = 0; i < 5000; i++)
                    {
                        var go = UnityContainerExtensions.Instantiate(container, prefab);
                        created.Add(go);
                    }
                }, out res.PirexMs, out res.PirexGc, out res.PirexGen0);

                foreach (var go in created) DestroyImmediate(go);
                created.Clear();
            }

            // VContainer
#if PIREX_VCONTAINER_ENABLED
            {
                var builder = new VContainer.ContainerBuilder();
                builder.Register<BenchServiceA>(VContainer.Lifetime.Singleton);
                var container = builder.Build();
                var first = UnityEngine.Object.Instantiate(prefab);
                IObjectResolver resolver = container;
                resolver.InjectGameObject(first);
                DestroyImmediate(first); // Warmup

                Measure(() =>
                {
                    for (int i = 0; i < 5000; i++)
                    {
                        var go = UnityEngine.Object.Instantiate(prefab);
                        resolver.InjectGameObject(go);
                        created.Add(go);
                    }
                }, out res.VContainerMs, out res.VContainerGc, out res.VContainerGen0);

                foreach (var go in created) DestroyImmediate(go);
                created.Clear();
            }
#endif

            // Reflex
#if PIREX_REFLEX_ENABLED
            {
                var builder = new Reflex.Core.ContainerBuilder();
                builder.RegisterType(typeof(BenchServiceA), Reflex.Enums.Lifetime.Singleton, Reflex.Enums.Resolution.Lazy);
                var container = builder.Build();
                var first = UnityEngine.Object.Instantiate(prefab);
                Reflex.Injectors.AttributeInjector.Inject(first.GetComponent<BenchMonoBehaviour>(), container);
                DestroyImmediate(first); // Warmup

                Measure(() =>
                {
                    for (int i = 0; i < 5000; i++)
                    {
                        var go = UnityEngine.Object.Instantiate(prefab);
                        Reflex.Injectors.AttributeInjector.Inject(go.GetComponent<BenchMonoBehaviour>(), container);
                        created.Add(go);
                    }
                }, out res.ReflexMs, out res.ReflexGc, out res.ReflexGen0);

                foreach (var go in created) DestroyImmediate(go);
                created.Clear();
            }
#endif

            DestroyImmediate(prefab);
            return res;
        }

        private BenchResult RunTest5_DeepTreeResolve()
        {
            var res = new BenchResult { TestName = "5. 20,000x Deep Tree (5-Level A->E)" };

            // 👑 PirexZenith
            {
                var builder = new PirexZenith.Core.ContainerBuilder();
                builder.RegisterTransient<BenchServiceA>();
                builder.RegisterTransient<BenchServiceB>();
                builder.RegisterTransient<BenchServiceC>();
                builder.RegisterTransient<BenchServiceD>();
                builder.RegisterTransient<BenchServiceE>();
                var container = builder.Build();
                container.Resolve<BenchServiceE>(); // Warmup

                Measure(() =>
                {
                    for (int i = 0; i < 20000; i++) container.Resolve<BenchServiceE>();
                }, out res.PirexMs, out res.PirexGc, out res.PirexGen0);
            }

            // VContainer
#if PIREX_VCONTAINER_ENABLED
            {
                var builder = new VContainer.ContainerBuilder();
                builder.Register<BenchServiceA>(VContainer.Lifetime.Transient);
                builder.Register<BenchServiceB>(VContainer.Lifetime.Transient);
                builder.Register<BenchServiceC>(VContainer.Lifetime.Transient);
                builder.Register<BenchServiceD>(VContainer.Lifetime.Transient);
                builder.Register<BenchServiceE>(VContainer.Lifetime.Transient);
                var container = builder.Build();
                container.Resolve<BenchServiceE>(); // Warmup

                Measure(() =>
                {
                    for (int i = 0; i < 20000; i++) container.Resolve<BenchServiceE>();
                }, out res.VContainerMs, out res.VContainerGc, out res.VContainerGen0);
            }
#endif

            // Reflex
#if PIREX_REFLEX_ENABLED
            {
                var builder = new Reflex.Core.ContainerBuilder();
                builder.RegisterType(typeof(BenchServiceA), Reflex.Enums.Lifetime.Transient, Reflex.Enums.Resolution.Lazy);
                builder.RegisterType(typeof(BenchServiceB), Reflex.Enums.Lifetime.Transient, Reflex.Enums.Resolution.Lazy);
                builder.RegisterType(typeof(BenchServiceC), Reflex.Enums.Lifetime.Transient, Reflex.Enums.Resolution.Lazy);
                builder.RegisterType(typeof(BenchServiceD), Reflex.Enums.Lifetime.Transient, Reflex.Enums.Resolution.Lazy);
                builder.RegisterType(typeof(BenchServiceE), Reflex.Enums.Lifetime.Transient, Reflex.Enums.Resolution.Lazy);
                var container = builder.Build();
                container.Resolve<BenchServiceE>(); // Warmup

                Measure(() =>
                {
                    for (int i = 0; i < 20000; i++) container.Resolve<BenchServiceE>();
                }, out res.ReflexMs, out res.ReflexGc, out res.ReflexGen0);
            }
#endif

            return res;
        }

        private BenchResult RunTest6_MultiBindingResolve()
        {
            var res = new BenchResult { TestName = "6. 10,000x ResolveAll<T> (Multi-Bind)" };

            // 👑 PirexZenith
            {
                var builder = new PirexZenith.Core.ContainerBuilder();
                builder.RegisterTransient<BenchMulti1, IBenchMultiService>();
                builder.RegisterTransient<BenchMulti2, IBenchMultiService>();
                builder.RegisterTransient<BenchMulti3, IBenchMultiService>();
                var container = builder.Build();
                container.ResolveAll<IBenchMultiService>(); // Warmup

                Measure(() =>
                {
                    for (int i = 0; i < 10000; i++)
                    {
                        var list = container.ResolveAll<IBenchMultiService>();
                    }
                }, out res.PirexMs, out res.PirexGc, out res.PirexGen0);
            }

            // VContainer
#if PIREX_VCONTAINER_ENABLED
            {
                var builder = new VContainer.ContainerBuilder();
                builder.Register<BenchMulti1>(VContainer.Lifetime.Transient).As<IBenchMultiService>();
                builder.Register<BenchMulti2>(VContainer.Lifetime.Transient).As<IBenchMultiService>();
                builder.Register<BenchMulti3>(VContainer.Lifetime.Transient).As<IBenchMultiService>();
                var container = builder.Build();
                container.Resolve<IEnumerable<IBenchMultiService>>(); // Warmup

                Measure(() =>
                {
                    for (int i = 0; i < 10000; i++)
                    {
                        var list = container.Resolve<IEnumerable<IBenchMultiService>>();
                    }
                }, out res.VContainerMs, out res.VContainerGc, out res.VContainerGen0);
            }
#endif

            // Reflex
#if PIREX_REFLEX_ENABLED
            {
                var builder = new Reflex.Core.ContainerBuilder();
                builder.RegisterType(typeof(BenchMulti1), new[] { typeof(IBenchMultiService) }, Reflex.Enums.Lifetime.Transient, Reflex.Enums.Resolution.Lazy);
                builder.RegisterType(typeof(BenchMulti2), new[] { typeof(IBenchMultiService) }, Reflex.Enums.Lifetime.Transient, Reflex.Enums.Resolution.Lazy);
                builder.RegisterType(typeof(BenchMulti3), new[] { typeof(IBenchMultiService) }, Reflex.Enums.Lifetime.Transient, Reflex.Enums.Resolution.Lazy);
                var container = builder.Build();
                container.All<IBenchMultiService>(); // Warmup

                Measure(() =>
                {
                    for (int i = 0; i < 10000; i++)
                    {
                        var list = container.All<IBenchMultiService>();
                    }
                }, out res.ReflexMs, out res.ReflexGc, out res.ReflexGen0);
            }
#endif

            return res;
        }

        private void Measure(Action action, out double ms, out long gcAlloc, out int gen0Collections)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long startMem = GC.GetAllocatedBytesForCurrentThread();
            int startGen0 = GC.CollectionCount(0);
            Stopwatch sw = Stopwatch.StartNew();

            action();

            sw.Stop();
            int endGen0 = GC.CollectionCount(0);
            long endMem = GC.GetAllocatedBytesForCurrentThread();

            ms = sw.Elapsed.TotalMilliseconds;
            gcAlloc = Math.Max(0, endMem - startMem);
            gen0Collections = Math.Max(0, endGen0 - startGen0);
        }

        private void LogMarkdownTable()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("### 🏁 3-Way DI Performance Benchmark Results (PirexZenith vs VContainer vs Reflex)");
            sb.AppendLine();
            sb.AppendLine("| Test Name | 👑 PirexZenith (ms / GC / Gen0) | VContainer (ms / GC / Gen0) | Reflex (ms / GC / Gen0) |");
            sb.AppendLine("| :--- | :---: | :---: | :---: |");

            foreach (var res in _results)
            {
                sb.AppendLine($"| **{res.TestName}** | **{res.PirexMs:F2} ms** ({FormatMemory(res.PirexGc)} - Gen0: {res.PirexGen0}) | {res.VContainerMs:F2} ms ({FormatMemory(res.VContainerGc)} - Gen0: {res.VContainerGen0}) | {res.ReflexMs:F2} ms ({FormatMemory(res.ReflexGc)} - Gen0: {res.ReflexGen0}) |");
            }

            sb.AppendLine();
            sb.AppendLine("*Benchmark executed via `DIBenchmarkRunner` in Unity Editor measuring execution time (ms), GC Allocations (Bytes), and GC Gen 0 collections.*");
            Debug.Log(sb.ToString());
        }
    }
}
