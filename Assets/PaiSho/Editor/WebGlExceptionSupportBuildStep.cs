#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PaiSho.EditorTools
{
    /// <summary>
    /// Applies portfolio WebGL Safari levers on every WebGL player build (batch or Editor UI).
    ///
    /// HJ-13: On Unity 6000.0.47f1, WebGLExceptionSupport.None still leaves Emscripten
    /// longjmp invoke_* trampolines (including invoke_iii). Absence of invoke_iii is NOT
    /// a valid gate. Keep None anyway (smaller C++ EH), and also raise WASM stack +
    /// prefer WebAssembly.Table interop.
    /// </summary>
    public sealed class WebGlExceptionSupportBuildStep : IPreprocessBuildWithReport
    {
        public const string SafariEmscriptenArgs = "-sSTACK_SIZE=5MB";

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.WebGL)
                return;

            ApplySafariWebGlPlayerSettings();
            Debug.Log(
                "[WebGL] Safari levers: exceptionSupport=None, " +
                $"emscriptenArgs='{PlayerSettings.WebGL.emscriptenArgs}', " +
                $"webAssemblyTable={PlayerSettings.WebGL.webAssemblyTable}, " +
                $"dataCaching={PlayerSettings.WebGL.dataCaching}. " +
                "Note: invoke_* longjmp trampolines remain under None on Unity 6 — do not gate on their absence.");
        }

        public static void ApplySafariWebGlPlayerSettings()
        {
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
            PlayerSettings.WebGL.emscriptenArgs = SafariEmscriptenArgs;
            PlayerSettings.WebGL.webAssemblyTable = true;
            // UnityCache on Safari has repeatedly served stale/corrupt *.unityweb after
            // hosting fixes; portfolio player prefers fresh fetches + productVersion bumps.
            PlayerSettings.WebGL.dataCaching = false;
        }
    }
}
#endif
