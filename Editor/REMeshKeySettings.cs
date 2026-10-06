using System;
using UnityEditor;

namespace REMesh.EditorTools
{
    /// <summary>
    /// Stores the API key in EditorPrefs.
    ///
    /// EditorPrefs is machine-local and outside the project, so a key entered in the
    /// gallery cannot end up in a scene, a prefab, or the repository. Device builds still
    /// use a StreamingAssets key file; this is for working in the editor.
    /// </summary>
    [InitializeOnLoad]
    public static class REMeshKeySettings
    {
        const string PrefKey = "REMesh.ApiKey";

        public const string AccountUrl = "https://remesh.dev/account";
        public const string PricingUrl = "https://remesh.dev/pricing";

        /// <summary>Raised after the stored key changes, so clients can re-arm.</summary>
        public static event Action Changed;

        static REMeshKeySettings()
        {
            // Let runtime code see the editor-entered key without referencing UnityEditor.
            REMeshCredentials.ExternalProvider = Get;
        }

        public static string Get() => EditorPrefs.GetString(PrefKey, string.Empty);

        public static bool HasKey => !string.IsNullOrWhiteSpace(Get());

        public static void Set(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) EditorPrefs.DeleteKey(PrefKey);
            else EditorPrefs.SetString(PrefKey, key.Trim());

            Changed?.Invoke();
        }

        public static void Clear() => Set(null);
    }
}
