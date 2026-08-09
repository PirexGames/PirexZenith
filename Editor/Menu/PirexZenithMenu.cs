using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PirexZenith.Editor
{
    /// <summary>
    /// Developer utility menu in Unity Editor.
    /// Redirects to the Unified Control Panel.
    /// </summary>
    public static class PirexZenithMenu
    {
        [MenuItem("Tools/UnityPrime/PirexZenith/⚙️ Zenith Control Panel", false, 1)]
        public static void OpenControlPanel()
        {
            PirexZenithEditorWindow.ShowWindow();
        }
    }
}
