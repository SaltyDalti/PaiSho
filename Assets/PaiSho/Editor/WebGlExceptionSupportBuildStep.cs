#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PaiSho.EditorTools
{
    /// <summary>
    /// Forces WebGLExceptionSupport.None on every WebGL player build (batch or Editor UI).
    /// Safari turns ExplicitlyThrown invoke_* trampolines into RangeError stack overflows
    /// on this project's board/mesh load path; ProjectSettings alone was still 1 while
    /// WebGlPlayerBuild.Build set None — builds that skipped that method shipped trampolines.
    /// </summary>
    public sealed class WebGlExceptionSupportBuildStep : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.WebGL)
                return;

            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
            Debug.Log(
                "[WebGL] exceptionSupport forced to None (Safari invoke_* stack guard). " +
                "After build, framework.js must not define invoke_iii.");
        }
    }
}
#endif
