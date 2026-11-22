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
            // ExplicitlyThrown is enough for gameplay; Full With Stacktrace makes
            // Safari's invoke_* trampolines deep enough to RangeError on boot.
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.nameFilesAsHashes = false;

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
    }
}
#endif
