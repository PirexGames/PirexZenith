using UnityEngine;

namespace PirexZenith.Examples.GameplaySample
{
    /// <summary>
    /// Utility class supporting input polling compatible with both the New Input System and Legacy Input Manager.
    /// Prevents InvalidOperationException when running in Editors switched to the New Input System.
    /// </summary>
    public static class UniversalInput
    {
        private static bool _inputChecked = false;
        private static System.Type _keyboardType;
        private static System.Reflection.PropertyInfo _currentKeyboardProp;
        private static System.Reflection.PropertyInfo _wasPressedProp;

        private static void EnsureInitialized()
        {
            if (_inputChecked) return;
            _inputChecked = true;
            try
            {
                _keyboardType = System.Type.GetType("UnityEngine.InputSystem.Keyboard, Unity.InputSystem");
                if (_keyboardType != null)
                {
                    _currentKeyboardProp = _keyboardType.GetProperty("current");
                    var spaceProp = _keyboardType.GetProperty("spaceKey");
                    if (spaceProp != null && spaceProp.PropertyType != null)
                    {
                        _wasPressedProp = spaceProp.PropertyType.GetProperty("wasPressedThisFrame");
                    }
                }
            }
            catch { }
        }

        public static bool GetKeyDown(KeyCode legacyKey, string inputSystemKeyName)
        {
            EnsureInitialized();

            // 1. Attempt to use the New Input System (via Reflection to avoid hard assembly dependency)
            if (_keyboardType != null && _currentKeyboardProp != null && _wasPressedProp != null)
            {
                try
                {
                    var currentKB = _currentKeyboardProp.GetValue(null);
                    if (currentKB != null)
                    {
                        var keyProp = _keyboardType.GetProperty(inputSystemKeyName);
                        var keyControl = keyProp?.GetValue(currentKB);
                        if (keyControl != null)
                        {
                            return (bool)_wasPressedProp.GetValue(keyControl);
                        }
                    }
                }
                catch { }
            }

            // 2. Fall back to Legacy Input Manager (wrapped in try-catch to avoid exceptions when disabled in Player Settings)
            try
            {
                return Input.GetKeyDown(legacyKey);
            }
            catch
            {
                return false;
            }
        }
    }
}
