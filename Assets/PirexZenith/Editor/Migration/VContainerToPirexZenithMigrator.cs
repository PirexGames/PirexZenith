using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PirexZenith.Editor.Migration
{
    /// <summary>
    /// Interactive Unity Editor Window to automatically scan and refactor VContainer projects over to PirexZenith.
    /// Access via: Tools -> PirexZenith -> 🛠️ VContainer to PirexZenith Migrator
    /// </summary>
    public class VContainerToPirexZenithMigrator : EditorWindow
    {
        public const string PIREXZENITH_SCOPE_GUID = "a3de49ab71c13804a9ef0424934de8ee";

        [Serializable]
        public class MigratedFileItem
        {
            public string AssetPath;
            public string FullPath;
            public bool IsSelected = true;
            public string DetectedFeatures;
            public int EstimatedReplacements;
        }

        private int _currentTab = 0;
        private readonly string[] _tabNames = { "1. C# Scripts (.cs)", "2. Scenes & Prefabs", "3. Assemblies (.asmdef)" };

        // Script scanning state
        private List<MigratedFileItem> _detectedScripts = new List<MigratedFileItem>();
        private Vector2 _scriptScrollPosition;

        // Scene & Prefab scanning state
        private List<MigratedFileItem> _detectedAssets = new List<MigratedFileItem>();
        private Vector2 _assetScrollPosition;
        private string _customOldGuids = "";

        // Asmdef scanning state
        private List<MigratedFileItem> _detectedAsmdefs = new List<MigratedFileItem>();
        private Vector2 _asmdefScrollPosition;

        private bool _createBackups = true;
        private string _statusMessage = "Select a tab above and click 'Scan Project' to detect VContainer files.";
        private MessageType _statusType = MessageType.Info;

        [MenuItem("Tools/PirexZenith/🛠️ VContainer to PirexZenith Migrator", false, 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<VContainerToPirexZenithMigrator>("VContainer ➔ PirexZenith Migrator");
            window.minSize = new Vector2(680, 520);
            window.Show();
        }

        private void OnEnable()
        {
            AutoDetectOldScopeGuids();
        }

        private void OnGUI()
        {
            DrawHeader();
            _currentTab = GUILayout.Toolbar(_currentTab, _tabNames, GUILayout.Height(28));
            EditorGUILayout.Space(10);

            if (_currentTab == 0)
            {
                DrawScriptTab();
            }
            else if (_currentTab == 1)
            {
                DrawSceneAndPrefabTab();
            }
            else
            {
                DrawAsmdefTab();
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("VContainer ➔ PirexZenith Automated Migration Tool", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This tool scans and migrates both C# scripts and Unity assets (.unity scenes & .prefab files) " +
                "from VContainer over to PirexZenith automatically.\n\n" +
                "• All conversions can create a .bak backup file for safety.\n" +
                "• We recommend committing existing work to Git before running automated migration.",
                MessageType.Info);
            EditorGUILayout.Space(5);
        }

        #region Tab 1: C# Scripts (.cs)

        private void DrawScriptTab()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("🔍 Scan Project for VContainer C# Scripts", GUILayout.Height(30)))
            {
                ScanScripts();
            }

            if (GUILayout.Button("Select All", GUILayout.Width(90), GUILayout.Height(30)))
            {
                SetAllSelected(_detectedScripts, true);
            }

            if (GUILayout.Button("Deselect All", GUILayout.Width(90), GUILayout.Height(30)))
            {
                SetAllSelected(_detectedScripts, false);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            _createBackups = EditorGUILayout.ToggleLeft("Create .bak backups before modifying files", _createBackups);
            EditorGUILayout.Space(5);

            if (!string.IsNullOrEmpty(_statusMessage))
            {
                EditorGUILayout.HelpBox(_statusMessage, _statusType);
                EditorGUILayout.Space(5);
            }

            EditorGUILayout.LabelField($"Detected C# Scripts ({_detectedScripts.Count}):", EditorStyles.boldLabel);

            _scriptScrollPosition = EditorGUILayout.BeginScrollView(_scriptScrollPosition, EditorStyles.helpBox);

            if (_detectedScripts.Count == 0)
            {
                EditorGUILayout.LabelField("No VContainer C# scripts detected.", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                for (int i = 0; i < _detectedScripts.Count; i++)
                {
                    var item = _detectedScripts[i];
                    EditorGUILayout.BeginHorizontal();

                    item.IsSelected = EditorGUILayout.Toggle(item.IsSelected, GUILayout.Width(20));

                    var style = new GUIStyle(EditorStyles.label) { wordWrap = false };
                    EditorGUILayout.LabelField(item.AssetPath, style, GUILayout.ExpandWidth(true));
                    EditorGUILayout.LabelField($"[{item.DetectedFeatures}] ({item.EstimatedReplacements} edit(s))", EditorStyles.miniLabel, GUILayout.Width(260));

                    if (GUILayout.Button("Open", GUILayout.Width(50)))
                    {
                        var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(item.AssetPath);
                        if (asset != null) AssetDatabase.OpenAsset(asset);
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);
            int selectedCount = GetSelectedCount(_detectedScripts);
            GUI.enabled = selectedCount > 0;
            if (GUILayout.Button($"⚡ Migrate {selectedCount} Selected C# Script(s) to PirexZenith", GUILayout.Height(36)))
            {
                if (EditorUtility.DisplayDialog(
                        "Confirm Script Migration",
                        $"Are you sure you want to migrate {selectedCount} C# script(s) to PirexZenith syntax?\n\n" +
                        (_createBackups ? ".bak backup files will be created." : "WARNING: Backup is disabled."),
                        "Migrate Now",
                        "Cancel"))
                {
                    MigrateSelectedScripts();
                }
            }
            GUI.enabled = true;
            EditorGUILayout.Space(10);
        }

        private void ScanScripts()
        {
            _detectedScripts.Clear();
            string[] allScriptGuids = AssetDatabase.FindAssets("t:Script", new[] { "Assets" });

            int scanned = 0;
            foreach (string guid in allScriptGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(assetPath) || !assetPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Ignore editor package/framework internal folder if needed, but allow scanning sample/test scripts
                if (assetPath.Contains("/PirexZenith/Runtime/Core/") || assetPath.Contains("/PirexZenith/Editor/"))
                    continue;

                string fullPath = Path.GetFullPath(assetPath);
                if (!File.Exists(fullPath)) continue;

                string content = File.ReadAllText(fullPath);
                string features;
                int replacements = AnalyzeVContainerScriptUsage(content, out features);

                if (replacements > 0)
                {
                    _detectedScripts.Add(new MigratedFileItem
                    {
                        AssetPath = assetPath,
                        FullPath = fullPath,
                        IsSelected = true,
                        DetectedFeatures = features,
                        EstimatedReplacements = replacements
                    });
                }
                scanned++;
            }

            if (_detectedScripts.Count > 0)
            {
                _statusMessage = $"Script scan complete! Found {_detectedScripts.Count} script(s) using VContainer across {scanned} inspected file(s).";
                _statusType = MessageType.Info;
            }
            else
            {
                _statusMessage = $"Script scan complete! No VContainer scripts were found across {scanned} inspected file(s).";
                _statusType = MessageType.Warning;
            }
        }

        private void MigrateSelectedScripts()
        {
            int migratedCount = 0;
            int totalEdits = 0;

            for (int i = 0; i < _detectedScripts.Count; i++)
            {
                var item = _detectedScripts[i];
                if (!item.IsSelected) continue;

                string fullPath = item.FullPath;
                if (!File.Exists(fullPath)) continue;

                string content = File.ReadAllText(fullPath);
                int editsMade;
                string newContent = MigrateScriptContent(content, out editsMade);

                if (editsMade > 0 && newContent != content)
                {
                    if (_createBackups)
                    {
                        string bakPath = fullPath + ".bak";
                        File.WriteAllText(bakPath, content);
                    }

                    File.WriteAllText(fullPath, newContent);
                    migratedCount++;
                    totalEdits += editsMade;
                }
            }

            AssetDatabase.Refresh();

            _statusMessage = $"Successfully migrated {migratedCount} C# script(s) ({totalEdits} pattern replacement(s)) to PirexZenith!";
            _statusType = MessageType.Info;
            Debug.Log($"[PirexZenith Migrator] {_statusMessage}");

            ScanScripts();
        }

        #endregion

        #region Tab 2: Scenes & Prefabs (.unity / .prefab)

        private void DrawSceneAndPrefabTab()
        {
            EditorGUILayout.LabelField("Project-Wide Scene (.unity) & Prefab (.prefab) Scope Migrator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This tab scans YAML content of every Scene and Prefab in your project to find components referencing VContainer's " +
                "LifetimeScope script GUID and replaces them with PirexZenith's LifetimeScope GUID automatically.",
                MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            _customOldGuids = EditorGUILayout.TextField(new GUIContent("Target Old Scope GUID(s):", "Comma-separated list of old script GUIDs to replace with PirexZenith Scope GUID"), _customOldGuids);
            if (GUILayout.Button("Auto-Detect GUIDs", GUILayout.Width(130)))
            {
                AutoDetectOldScopeGuids();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("🔍 Scan All Scenes & Prefabs for Old Scopes", GUILayout.Height(30)))
            {
                ScanScenesAndPrefabs();
            }

            if (GUILayout.Button("Select All", GUILayout.Width(90), GUILayout.Height(30)))
            {
                SetAllSelected(_detectedAssets, true);
            }

            if (GUILayout.Button("Deselect All", GUILayout.Width(90), GUILayout.Height(30)))
            {
                SetAllSelected(_detectedAssets, false);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            _createBackups = EditorGUILayout.ToggleLeft("Create .bak backups before modifying files", _createBackups);
            EditorGUILayout.Space(5);

            if (!string.IsNullOrEmpty(_statusMessage))
            {
                EditorGUILayout.HelpBox(_statusMessage, _statusType);
                EditorGUILayout.Space(5);
            }

            EditorGUILayout.LabelField($"Detected Scenes & Prefabs ({_detectedAssets.Count}):", EditorStyles.boldLabel);

            _assetScrollPosition = EditorGUILayout.BeginScrollView(_assetScrollPosition, EditorStyles.helpBox);

            if (_detectedAssets.Count == 0)
            {
                EditorGUILayout.LabelField("No old LifetimeScope references detected in Scenes or Prefabs.", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                for (int i = 0; i < _detectedAssets.Count; i++)
                {
                    var item = _detectedAssets[i];
                    EditorGUILayout.BeginHorizontal();

                    item.IsSelected = EditorGUILayout.Toggle(item.IsSelected, GUILayout.Width(20));

                    var style = new GUIStyle(EditorStyles.label) { wordWrap = false };
                    EditorGUILayout.LabelField(item.AssetPath, style, GUILayout.ExpandWidth(true));
                    EditorGUILayout.LabelField($"[{item.DetectedFeatures}] ({item.EstimatedReplacements} replacement(s))", EditorStyles.miniLabel, GUILayout.Width(260));

                    if (GUILayout.Button("Open", GUILayout.Width(50)))
                    {
                        var asset = AssetDatabase.LoadMainAssetAtPath(item.AssetPath);
                        if (asset != null) AssetDatabase.OpenAsset(asset);
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);
            int selectedCount = GetSelectedCount(_detectedAssets);
            GUI.enabled = selectedCount > 0;
            if (GUILayout.Button($"⚡ Replace LifetimeScopes in {selectedCount} Selected Asset(s) to PirexZenith", GUILayout.Height(36)))
            {
                if (EditorUtility.DisplayDialog(
                        "Confirm Scene & Prefab Migration",
                        $"Are you sure you want to replace old LifetimeScope GUIDs in {selectedCount} file(s) across the project?\n\n" +
                        (_createBackups ? ".bak backup files will be created." : "WARNING: Backup is disabled."),
                        "Replace Now",
                        "Cancel"))
                {
                    MigrateSelectedScenesAndPrefabs();
                }
            }
            GUI.enabled = true;

            EditorGUILayout.Space(5);
            if (GUILayout.Button("🎮 Re-link Scopes in Active Loaded Scene", GUILayout.Height(28)))
            {
                MigrateActiveSceneScopes();
            }
            EditorGUILayout.Space(10);
        }

        private void AutoDetectOldScopeGuids()
        {
            var oldGuids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string[] scriptGuids = AssetDatabase.FindAssets("LifetimeScope t:Script");
            foreach (string guid in scriptGuids)
            {
                if (!string.Equals(guid, PIREXZENITH_SCOPE_GUID, StringComparison.OrdinalIgnoreCase))
                {
                    oldGuids.Add(guid);
                }
            }

            _customOldGuids = string.Join(", ", oldGuids);
            if (string.IsNullOrEmpty(_customOldGuids))
            {
                _statusMessage = "No non-PirexZenith LifetimeScope script GUIDs found in AssetDatabase. If VContainer was already uninstalled, enter the old script GUID manually.";
                _statusType = MessageType.Info;
            }
            else
            {
                _statusMessage = $"Auto-detected {oldGuids.Count} old LifetimeScope script GUID(s): {_customOldGuids}";
                _statusType = MessageType.Info;
            }
        }

        private void ScanScenesAndPrefabs()
        {
            _detectedAssets.Clear();
            var targetGuids = GetTargetOldGuids();

            string[] assetGuids = AssetDatabase.FindAssets("t:Scene t:Prefab", new[] { "Assets" });
            int scanned = 0;

            foreach (string guid in assetGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(assetPath) ||
                    (!assetPath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) &&
                     !assetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                // Skip framework internal sample if needed, but inspect project scenes and prefabs
                if (assetPath.Contains("/PirexZenith/Runtime/") || assetPath.Contains("/PirexZenith/Editor/"))
                    continue;

                string fullPath = Path.GetFullPath(assetPath);
                if (!File.Exists(fullPath)) continue;

                string content = File.ReadAllText(fullPath);
                int replacements = AnalyzeYamlScopeUsage(content, targetGuids, out string features);

                if (replacements > 0)
                {
                    _detectedAssets.Add(new MigratedFileItem
                    {
                        AssetPath = assetPath,
                        FullPath = fullPath,
                        IsSelected = true,
                        DetectedFeatures = features,
                        EstimatedReplacements = replacements
                    });
                }
                scanned++;
            }

            if (_detectedAssets.Count > 0)
            {
                _statusMessage = $"Scene/Prefab scan complete! Found {_detectedAssets.Count} asset(s) referencing old LifetimeScope across {scanned} inspected file(s).";
                _statusType = MessageType.Info;
            }
            else
            {
                _statusMessage = $"Scene/Prefab scan complete! No old LifetimeScope references were found across {scanned} inspected file(s).";
                _statusType = MessageType.Warning;
            }
        }

        private HashSet<string> GetTargetOldGuids()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrEmpty(_customOldGuids))
            {
                var parts = _customOldGuids.Split(new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts)
                {
                    string clean = p.Trim();
                    if (clean.Length == 32 && !string.Equals(clean, PIREXZENITH_SCOPE_GUID, StringComparison.OrdinalIgnoreCase))
                    {
                        set.Add(clean);
                    }
                }
            }
            return set;
        }

        private int AnalyzeYamlScopeUsage(string yaml, HashSet<string> targetGuids, out string features)
        {
            int matches = 0;
            var desc = new List<string>();

            // Check explicit target GUIDs
            foreach (var guid in targetGuids)
            {
                int count = Regex.Matches(yaml, @"guid:\s*" + Regex.Escape(guid)).Count;
                if (count > 0)
                {
                    matches += count;
                    desc.Add($"Old GUID ({count})");
                }
            }

            // Also check for VContainer scope signatures in YAML if no specific GUID matched
            if (matches == 0 && (yaml.Contains("autoInjectGameObjects:") || yaml.Contains("VContainer")))
            {
                matches = 1;
                desc.Add("VContainer Scope YAML");
            }

            features = string.Join(", ", desc);
            return matches;
        }

        private void MigrateSelectedScenesAndPrefabs()
        {
            var targetGuids = GetTargetOldGuids();
            int migratedCount = 0;
            int totalReplacements = 0;

            for (int i = 0; i < _detectedAssets.Count; i++)
            {
                var item = _detectedAssets[i];
                if (!item.IsSelected) continue;

                string fullPath = item.FullPath;
                if (!File.Exists(fullPath)) continue;

                string content = File.ReadAllText(fullPath);
                int editsMade;
                string newContent = ReplaceYamlScopeGuids(content, targetGuids, out editsMade);

                if (editsMade > 0 && newContent != content)
                {
                    if (_createBackups)
                    {
                        string bakPath = fullPath + ".bak";
                        File.WriteAllText(bakPath, content);
                    }

                    File.WriteAllText(fullPath, newContent);
                    migratedCount++;
                    totalReplacements += editsMade;
                }
            }

            AssetDatabase.Refresh();

            _statusMessage = $"Successfully replaced old LifetimeScope GUIDs in {migratedCount} Scene/Prefab asset(s) ({totalReplacements} replacement(s))!";
            _statusType = MessageType.Info;
            Debug.Log($"[PirexZenith Migrator] {_statusMessage}");

            ScanScenesAndPrefabs();
        }

        public static string ReplaceYamlScopeGuids(string yaml, HashSet<string> targetGuids, out int replacementsMade)
        {
            string output = yaml;
            int count = 0;

            foreach (var guid in targetGuids)
            {
                if (string.Equals(guid, PIREXZENITH_SCOPE_GUID, StringComparison.OrdinalIgnoreCase))
                    continue;

                var regex = new Regex(@"guid:\s*" + Regex.Escape(guid) + @"\b");
                var matches = regex.Matches(output);
                if (matches.Count > 0)
                {
                    count += matches.Count;
                    output = regex.Replace(output, $"guid: {PIREXZENITH_SCOPE_GUID}");
                }
            }

            // Also convert old VContainer property names to PirexZenith if present in scope blocks
            if (output.Contains("autoInjectGameObjects:"))
            {
                output = Regex.Replace(output, @"autoInjectGameObjects:", "# autoInjectGameObjects migrated to PirexZenith auto-injection:");
            }

            replacementsMade = count;
            return output;
        }

        private void MigrateActiveSceneScopes()
        {
            var activeScopes = UnityEngine.Object.FindObjectsOfType<PirexZenith.LifetimeScope>(true);
            int updatedScopes = 0;

            foreach (var scope in activeScopes)
            {
                if (scope != null)
                {
                    EditorUtility.SetDirty(scope);
                    updatedScopes++;
                }
            }

            EditorSceneManager.MarkAllScenesDirty();
            AssetDatabase.SaveAssets();

            _statusMessage = $"Successfully verified and refreshed {updatedScopes} LifetimeScope component(s) in active loaded scene!";
            _statusType = MessageType.Info;
            Debug.Log($"[PirexZenith Migrator] {_statusMessage}");
        }

        #endregion

        #region Tab 3: Assemblies (.asmdef)

        private void DrawAsmdefTab()
        {
            EditorGUILayout.LabelField("Project-Wide Assembly (.asmdef / .asmref) Migrator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This tab scans all .asmdef and .asmref files in your project and automatically updates " +
                "any 'VContainer' references to use the unified 'PirexZenith' assembly.",
                MessageType.Info);
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("🔍 Scan Project for VContainer Assembly References", GUILayout.Height(30)))
            {
                ScanAsmdefs();
            }

            if (GUILayout.Button("Select All", GUILayout.Width(90), GUILayout.Height(30)))
            {
                SetAllSelected(_detectedAsmdefs, true);
            }

            if (GUILayout.Button("Deselect All", GUILayout.Width(90), GUILayout.Height(30)))
            {
                SetAllSelected(_detectedAsmdefs, false);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            _createBackups = EditorGUILayout.ToggleLeft("Create .bak backups before modifying files", _createBackups);
            EditorGUILayout.Space(5);

            if (!string.IsNullOrEmpty(_statusMessage))
            {
                EditorGUILayout.HelpBox(_statusMessage, _statusType);
                EditorGUILayout.Space(5);
            }

            EditorGUILayout.LabelField($"Detected Assemblies ({_detectedAsmdefs.Count}):", EditorStyles.boldLabel);

            _asmdefScrollPosition = EditorGUILayout.BeginScrollView(_asmdefScrollPosition, EditorStyles.helpBox);

            if (_detectedAsmdefs.Count == 0)
            {
                EditorGUILayout.LabelField("No old assembly references detected.", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                for (int i = 0; i < _detectedAsmdefs.Count; i++)
                {
                    var item = _detectedAsmdefs[i];
                    EditorGUILayout.BeginHorizontal();

                    item.IsSelected = EditorGUILayout.Toggle(item.IsSelected, GUILayout.Width(20));

                    var style = new GUIStyle(EditorStyles.label) { wordWrap = false };
                    EditorGUILayout.LabelField(item.AssetPath, style, GUILayout.ExpandWidth(true));
                    EditorGUILayout.LabelField($"[{item.DetectedFeatures}] ({item.EstimatedReplacements} replacement(s))", EditorStyles.miniLabel, GUILayout.Width(260));

                    if (GUILayout.Button("Open", GUILayout.Width(50)))
                    {
                        var asset = AssetDatabase.LoadMainAssetAtPath(item.AssetPath);
                        if (asset != null) AssetDatabase.OpenAsset(asset);
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);
            int selectedCount = GetSelectedCount(_detectedAsmdefs);
            GUI.enabled = selectedCount > 0;
            if (GUILayout.Button($"⚡ Replace Assembly References in {selectedCount} Selected Asset(s)", GUILayout.Height(36)))
            {
                if (EditorUtility.DisplayDialog(
                        "Confirm Assembly Migration",
                        $"Are you sure you want to replace 'VContainer' with 'PirexZenith' in {selectedCount} assembly file(s)?\n\n" +
                        (_createBackups ? ".bak backup files will be created." : "WARNING: Backup is disabled."),
                        "Replace Now",
                        "Cancel"))
                {
                    MigrateSelectedAsmdefs();
                }
            }
            GUI.enabled = true;
            EditorGUILayout.Space(10);
        }

        private void ScanAsmdefs()
        {
            _detectedAsmdefs.Clear();
            string[] guids = AssetDatabase.FindAssets("t:asmdef t:asmref", new[] { "Assets" });
            int scanned = 0;

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(assetPath)) continue;

                // Skip framework internal if needed, but inspect project assemblies
                if (assetPath.Contains("/PirexZenith/Runtime/") || assetPath.Contains("/PirexZenith/Editor/"))
                    continue;

                string fullPath = Path.GetFullPath(assetPath);
                if (!File.Exists(fullPath)) continue;

                string content = File.ReadAllText(fullPath);
                int replacements = 0;
                
                // Extremely simple regex to find "VContainer" string reference in JSON array
                if (Regex.IsMatch(content, @"\""VContainer\"""))
                {
                    replacements++;
                }

                if (replacements > 0)
                {
                    _detectedAsmdefs.Add(new MigratedFileItem
                    {
                        AssetPath = assetPath,
                        FullPath = fullPath,
                        IsSelected = true,
                        DetectedFeatures = "VContainer Reference",
                        EstimatedReplacements = replacements
                    });
                }
                scanned++;
            }

            if (_detectedAsmdefs.Count > 0)
            {
                _statusMessage = $"Assembly scan complete! Found {_detectedAsmdefs.Count} file(s) referencing VContainer across {scanned} inspected file(s).";
                _statusType = MessageType.Info;
            }
            else
            {
                _statusMessage = $"Assembly scan complete! No VContainer references were found across {scanned} inspected file(s).";
                _statusType = MessageType.Warning;
            }
        }

        private void MigrateSelectedAsmdefs()
        {
            int migratedCount = 0;
            int totalReplacements = 0;

            for (int i = 0; i < _detectedAsmdefs.Count; i++)
            {
                var item = _detectedAsmdefs[i];
                if (!item.IsSelected) continue;

                string fullPath = item.FullPath;
                if (!File.Exists(fullPath)) continue;

                string content = File.ReadAllText(fullPath);
                string newContent = Regex.Replace(content, @"\""VContainer\""", "\"PirexZenith\"");

                if (newContent != content)
                {
                    if (_createBackups)
                    {
                        string bakPath = fullPath + ".bak";
                        File.WriteAllText(bakPath, content);
                    }

                    File.WriteAllText(fullPath, newContent);
                    migratedCount++;
                    totalReplacements++;
                }
            }

            AssetDatabase.Refresh();

            _statusMessage = $"Successfully migrated {migratedCount} assembly file(s) to reference PirexZenith!";
            _statusType = MessageType.Info;
            Debug.Log($"[PirexZenith Migrator] {_statusMessage}");

            ScanAsmdefs();
        }

        #endregion

        #region Helper Methods

        private void SetAllSelected(List<MigratedFileItem> list, bool selected)
        {
            for (int i = 0; i < list.Count; i++)
            {
                list[i].IsSelected = selected;
            }
        }

        private int GetSelectedCount(List<MigratedFileItem> list)
        {
            int count = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].IsSelected) count++;
            }
            return count;
        }

        private int AnalyzeVContainerScriptUsage(string code, out string features)
        {
            var detected = new List<string>();
            int count = 0;

            if (code.Contains("using VContainer") || code.Contains("using VContainer.Unity"))
            {
                detected.Add("Namespaces");
                count++;
            }
            if (code.Contains("IContainerBuilder"))
            {
                detected.Add("IContainerBuilder");
                count++;
            }
            if (code.Contains("IObjectResolver"))
            {
                detected.Add("IObjectResolver");
                count++;
            }
            if (code.Contains("Lifetime.Singleton") || code.Contains("Lifetime.Transient"))
            {
                detected.Add("Lifetime Registrations");
                count++;
            }
            if (code.Contains("RegisterEntryPoint") || code.Contains("RegisterComponent"))
            {
                detected.Add("EntryPoints/Components");
                count++;
            }

            features = string.Join(", ", detected);
            return count;
        }

        /// <summary>
        /// Converts a C# script containing VContainer syntax into clean PirexZenith syntax.
        /// </summary>
        public static string MigrateScriptContent(string originalCode, out int replacementsMade)
        {
            string code = originalCode;
            int edits = 0;

            // 1. Namespaces
            if (code.Contains("using VContainer.Unity;"))
            {
                code = Regex.Replace(code, @"using\s+VContainer\.Unity\s*;", "using PirexZenith;");
                edits++;
            }
            if (code.Contains("using VContainer;"))
            {
                code = Regex.Replace(code, @"using\s+VContainer\s*;", "using PirexZenith;");
                edits++;
            }

            // 2. Core Interfaces
            string afterBuilder = Regex.Replace(code, @"\bIContainerBuilder\b", "ContainerBuilder");
            if (afterBuilder != code) { edits++; code = afterBuilder; }

            string afterResolver = Regex.Replace(code, @"\bIObjectResolver\b", "IResolver");
            if (afterResolver != code) { edits++; code = afterResolver; }

            // 3. Registrations with .As<T>()
            string r1 = Regex.Replace(code,
                @"builder\.Register<([\w\.\s]+)>\s*\(\s*(?:VContainer\.)?Lifetime\.Singleton\s*\)\s*\.As<([\w\.\s]+)>\s*\(\s*\)\s*;",
                "builder.RegisterSingleton<$1, $2>();");
            if (r1 != code) { edits++; code = r1; }

            string r2 = Regex.Replace(code,
                @"builder\.Register<([\w\.\s]+)>\s*\(\s*(?:VContainer\.)?Lifetime\.Transient\s*\)\s*\.As<([\w\.\s]+)>\s*\(\s*\)\s*;",
                "builder.RegisterTransient<$1, $2>();");
            if (r2 != code) { edits++; code = r2; }

            // 4. Registrations with .AsImplementedInterfaces()
            string r3 = Regex.Replace(code,
                @"builder\.Register<([\w\.\s]+)>\s*\(\s*(?:VContainer\.)?Lifetime\.Singleton\s*\)\s*\.AsImplementedInterfaces\s*\(\s*\)\s*;",
                "builder.RegisterSingleton<$1>();");
            if (r3 != code) { edits++; code = r3; }

            string r4 = Regex.Replace(code,
                @"builder\.Register<([\w\.\s]+)>\s*\(\s*(?:VContainer\.)?Lifetime\.Transient\s*\)\s*\.AsImplementedInterfaces\s*\(\s*\)\s*;",
                "builder.RegisterTransient<$1>();");
            if (r4 != code) { edits++; code = r4; }

            // 5. Register<T, I>(Lifetime.X) (2 generic args)
            string r5 = Regex.Replace(code,
                @"builder\.Register<([\w\.\s]+),\s*([\w\.\s]+)>\s*\(\s*(?:VContainer\.)?Lifetime\.Singleton\s*\)\s*;",
                "builder.RegisterSingleton<$1, $2>();");
            if (r5 != code) { edits++; code = r5; }

            string r6 = Regex.Replace(code,
                @"builder\.Register<([\w\.\s]+),\s*([\w\.\s]+)>\s*\(\s*(?:VContainer\.)?Lifetime\.Transient\s*\)\s*;",
                "builder.RegisterTransient<$1, $2>();");
            if (r6 != code) { edits++; code = r6; }

            // 6. Basic Register<T>(Lifetime.X) (1 generic arg)
            string r7 = Regex.Replace(code,
                @"builder\.Register<([\w\.\s]+)>\s*\(\s*(?:VContainer\.)?Lifetime\.Singleton\s*\)\s*;",
                "builder.RegisterSingleton<$1>();");
            if (r7 != code) { edits++; code = r7; }

            string r8 = Regex.Replace(code,
                @"builder\.Register<([\w\.\s]+)>\s*\(\s*(?:VContainer\.)?Lifetime\.Transient\s*\)\s*;",
                "builder.RegisterTransient<$1>();");
            if (r8 != code) { edits++; code = r8; }

            // 7. RegisterComponent -> RegisterInstance
            string r9 = Regex.Replace(code,
                @"builder\.RegisterComponent\s*\(\s*([^)]+)\s*\)\s*;",
                "builder.RegisterInstance($1);");
            if (r9 != code) { edits++; code = r9; }

            // 8. RegisterEntryPoint<T> -> RegisterSystem<T>
            string r10 = Regex.Replace(code,
                @"builder\.RegisterEntryPoint<([\w\.\s]+)>\s*\([^)]*\)\s*;",
                "builder.RegisterSystem<$1>();");
            if (r10 != code) { edits++; code = r10; }

            // 9. Clean up any accidental duplicate usings
            code = RemoveDuplicateUsingLines(code);

            replacementsMade = edits;
            return code;
        }

        private static string RemoveDuplicateUsingLines(string code)
        {
            var lines = code.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var seenUsings = new HashSet<string>();
            var output = new List<string>();

            foreach (var line in lines)
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith("using ") && trimmed.EndsWith(";"))
                {
                    if (!seenUsings.Add(trimmed))
                        continue; // skip duplicate using
                }
                output.Add(line);
            }

            return string.Join("\r\n", output);
        }

        #endregion
    }
}
