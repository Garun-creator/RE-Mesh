using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Networking;

namespace REMesh.EditorTools
{
    /// <summary>
    /// Pulls the newest version of this package.
    ///
    /// A package installed from a git URL does not update on its own, and it does not update
    /// by asking the Package Manager either: UPM writes the resolved commit into
    /// `Packages/packages-lock.json` and honours it forever after, so re-adding the same URL
    /// re-resolves to the same commit and nothing appears to happen. Unity's own answer is
    /// "remove it and install it again", which loses nothing but is four steps and a
    /// confidence that you are allowed to remove it.
    ///
    /// So this drops the lock entry - the whole of what pins the old commit - and re-adds the
    /// URL, which is the same operation with none of the ceremony.
    /// </summary>
    static class REMeshUpdate
    {
        const string PackageName = "dev.meshra.unity-connector";
        const string GitUrl = "https://github.com/SKYGOD07/MESHRA.git";
        const string ManifestUrl =
            "https://raw.githubusercontent.com/SKYGOD07/MESHRA/main/package.json";

        static AddRequest _request;

        [MenuItem("Tools/MESHRA/Update Package", priority = 4)]
        static void Update() => _ = UpdateAsync();

        /// <summary>The version currently in the project, or null.</summary>
        static string InstalledVersion()
        {
            try
            {
                return UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages()
                    ?.FirstOrDefault(p => p.name == PackageName)?.version;
            }
            catch (Exception)
            {
                return null;
            }
        }

        static async Task UpdateAsync()
        {
            var installed = InstalledVersion();

            string latest = null;
            try
            {
                latest = (string)JObject.Parse(await FetchAsync(ManifestUrl))["version"];
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[REMesh] could not check for updates ({e.Message}).");
            }

            /* Checked rather than assumed, because "update" that reinstalls an identical
             * commit still costs a domain reload, and a reload for nothing is worse than a
             * dialog saying there was nothing to do. */
            if (latest != null && installed != null && latest == installed)
            {
                EditorUtility.DisplayDialog(
                    "REMesh",
                    $"Already on {installed}, which is the latest.",
                    "OK");
                return;
            }

            var what = latest == null
                ? "Re-fetch the package from GitHub?"
                : $"Update from {installed ?? "unknown"} to {latest}?";

            if (!EditorUtility.DisplayDialog(
                    "Update REMesh",
                    $"{what}\n\nUnity will re-resolve the package and recompile. Anything you have " +
                    "imported into your project stays where it is.",
                    "Update", "Cancel"))
            {
                return;
            }

            ForgetLockedCommit();

            _request = Client.Add(GitUrl);
            EditorApplication.update += Poll;
        }

        /// <summary>
        /// Removes this package's entry from packages-lock.json.
        ///
        /// That entry is the whole reason a git package goes stale: it records the exact
        /// commit that was resolved, and UPM keeps using it. Without the entry the next
        /// resolve fetches the branch head, which is the update.
        /// </summary>
        static void ForgetLockedCommit()
        {
            try
            {
                var path = Path.Combine(
                    Path.GetDirectoryName(Application.dataPath) ?? ".", "Packages", "packages-lock.json");

                if (!File.Exists(path)) return;

                var root = JObject.Parse(File.ReadAllText(path));
                if (root["dependencies"] is not JObject deps || deps[PackageName] == null) return;

                deps.Remove(PackageName);
                File.WriteAllText(path, root.ToString());
            }
            catch (Exception e)
            {
                // Not fatal: Add may still pick up a newer commit, and if it does not the
                // user is where they started rather than somewhere worse.
                Debug.LogWarning($"[REMesh] could not clear the package lock ({e.Message}).");
            }
        }

        static void Poll()
        {
            if (_request is not { IsCompleted: true }) return;

            EditorApplication.update -= Poll;

            if (_request.Status == StatusCode.Success)
                Debug.Log($"[REMesh] updated to {_request.Result?.version}.");
            else
                Debug.LogWarning($"[REMesh] update failed: {_request.Error?.message}");

            _request = null;
        }

        /* Its own transport again: REMeshClient attaches the REMesh API key to everything
         * it sends, and this request goes to githubusercontent.com. */
        static async Task<string> FetchAsync(string url)
        {
            using var req = UnityWebRequest.Get(url);
            req.SetRequestHeader("User-Agent", "remesh-unity-connector");
            req.timeout = 30;

            var op = req.SendWebRequest();
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (op.isDone) tcs.TrySetResult(true);
            else op.completed += _ => tcs.TrySetResult(true);
            await tcs.Task;

            if (req.result != UnityWebRequest.Result.Success)
                throw new Exception(req.error);

            return req.downloadHandler.text;
        }
    }
}
