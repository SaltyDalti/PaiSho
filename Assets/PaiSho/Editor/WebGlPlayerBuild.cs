#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PaiSho.EditorTools
{
    /// <summary>
    /// Batch entry point for a portfolio WebGL player of the existing GamePlay scene.
    /// Invoke with -executeMethod PaiSho.EditorTools.WebGlPlayerBuild.Build
    /// </summary>
    public static class WebGlPlayerBuild
    {
        private const string ScenePath = "Assets/Scenes/GamePlay.unity";
        private const string OutputDirectory = "Builds/WebGL";

        public static void Build()
        {
            EditorSceneManager.OpenScene(ScenePath);

            BuildReadinessValidator.ValidationReport readiness = BuildReadinessValidator.Validate();
            if (readiness.Issues.Count > 0)
            {
                var message = new StringBuilder("Pai Sho build readiness issues:\n");
                foreach (string issue in readiness.Issues)
                    message.AppendLine(" - " + issue);
                Debug.LogError(message.ToString());
                EditorApplication.Exit(2);
                return;
            }

            // Gzip plus the loader's own decompressor, so a static host can serve the
            // files without Content-Encoding headers. Initial memory is above the 32 MB
            // project default so the URP scene can boot; the heap still grows to 2048 MB.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.initialMemorySize = 256;
            PlayerSettings.WebGL.maximumMemorySize = 2048;
            PlayerSettings.WebGL.nameFilesAsHashes = false;

            // Safari stack levers (see WebGlExceptionSupportBuildStep). None does not
            // remove invoke_* on Unity 6 — those are longjmp trampolines (HJ-13).
            WebGlExceptionSupportBuildStep.ApplySafariWebGlPlayerSettings();
            if (PlayerSettings.WebGL.exceptionSupport != WebGLExceptionSupport.None)
            {
                Debug.LogError("WebGL exceptionSupport must be None for portfolio WebGL; refusing build.");
                EditorApplication.Exit(4);
                return;
            }

            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(NamedBuildTarget.WebGL, BuildTarget.WebGL))
            {
                Debug.LogError("Could not switch the active build target to WebGL.");
                EditorApplication.Exit(3);
                return;
            }

            Directory.CreateDirectory(OutputDirectory);
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = OutputDirectory,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"WebGL build failed: {summary.result} errors={summary.totalErrors}");
                EditorApplication.Exit(1);
                return;
            }

            AssetDatabase.SaveAssets();
            LogOutputSizes(OutputDirectory);
            if (!VerifySafariBuildArtifacts(OutputDirectory))
            {
                EditorApplication.Exit(5);
                return;
            }

            Debug.Log($"WebGL build succeeded: {summary.totalSize} bytes at {OutputDirectory}");
            EditorApplication.Exit(0);
        }

        static void LogOutputSizes(string outputDirectory)
        {
            string buildDir = Path.Combine(outputDirectory, "Build");
            if (!Directory.Exists(buildDir))
            {
                Debug.LogWarning($"WebGL size report: missing {buildDir}");
                return;
            }

            long total = 0;
            foreach (string path in Directory.GetFiles(buildDir))
            {
                long size = new FileInfo(path).Length;
                total += size;
                Debug.Log($"WebGL output {Path.GetFileName(path)}: {size} bytes");
            }

            Debug.Log($"WebGL Build/ folder total: {total} bytes");
        }

        /// <summary>
        /// Settings can be logged as applied while the linked framework still looks like the
        /// pre-table dynCall player (live HJ-14 mismatch: framework ~88 KB + dynCall_iii, not
        /// ~83 KB + getWasmTableEntry). Fail the batch build if table mode did not land.
        /// Does NOT require absence of invoke_* (those remain as longjmp under None).
        /// </summary>
        static bool VerifySafariBuildArtifacts(string outputDirectory)
        {
            string buildDir = Path.Combine(outputDirectory, "Build");
            string frameworkPath = FindFrameworkPath(buildDir);
            if (frameworkPath == null)
            {
                Debug.LogError("WebGL Safari verify: missing framework.js(.unityweb) under Build/.");
                return false;
            }

            string text = ReadPossiblyGzipText(frameworkPath);
            if (string.IsNullOrEmpty(text))
            {
                Debug.LogError($"WebGL Safari verify: could not read {frameworkPath}");
                return false;
            }

            bool wantTable = PlayerSettings.WebGL.webAssemblyTable;
            bool hasTableEntry = text.Contains("getWasmTableEntry");
            bool hasDynCallIii = text.Contains("dynCall_iii");
            Debug.Log(
                $"[WebGL] artifact check: framework={Path.GetFileName(frameworkPath)} " +
                $"bytes={new FileInfo(frameworkPath).Length} webAssemblyTable={wantTable} " +
                $"getWasmTableEntry={hasTableEntry} dynCall_iii={hasDynCallIii}");

            if (wantTable && !hasTableEntry)
            {
                Debug.LogError(
                    "WebGL Safari verify FAILED: webAssemblyTable is true but framework.js has no " +
                    "getWasmTableEntry (still looks like dynCall interop). Do not upload — linker " +
                    "settings likely did not apply. Check Bee/emcc logs for -sWASM_TABLE / table flags.");
                return false;
            }

            string args = PlayerSettings.WebGL.emscriptenArgs ?? string.Empty;
            if (!args.Contains("STACK_SIZE"))
            {
                Debug.LogError(
                    "WebGL Safari verify FAILED: emscriptenArgs missing STACK_SIZE after build.");
                return false;
            }

            return true;
        }

        static string FindFrameworkPath(string buildDir)
        {
            if (!Directory.Exists(buildDir))
                return null;

            foreach (string path in Directory.GetFiles(buildDir))
            {
                string name = Path.GetFileName(path);
                if (name.IndexOf("framework", System.StringComparison.OrdinalIgnoreCase) >= 0 &&
                    (name.EndsWith(".js") || name.EndsWith(".js.unityweb") || name.EndsWith(".js.gz")))
                    return path;
            }

            return null;
        }

        static string ReadPossiblyGzipText(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            if (bytes.Length >= 2 && bytes[0] == 0x1f && bytes[1] == 0x8b)
            {
                using (var input = new MemoryStream(bytes))
                using (var gzip = new System.IO.Compression.GZipStream(input, System.IO.Compression.CompressionMode.Decompress))
                using (var reader = new StreamReader(gzip, Encoding.UTF8))
                    return reader.ReadToEnd();
            }

            return Encoding.UTF8.GetString(bytes);
        }
    }
}
#endif
