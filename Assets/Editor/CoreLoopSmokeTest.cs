#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Headless play-mode smoke test entry point (no GUI / computerUse agents).
///   xvfb-run -a Unity -batchmode -projectPath . -executeMethod CoreLoopSmokeTest.Run
/// </summary>
public static class CoreLoopSmokeTest
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string ResultPath = CoreLoopSmokeRunner.ResultPath;
    private const float TimeoutSeconds = 90f;

    private static double _startedAt;
    private static bool _runnerSpawned;
    private static bool _finished;

    [MenuItem("PaiSho/Run Core Loop Smoke Test")]
    public static void Run()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ResultPath));
        File.WriteAllText(ResultPath, "STARTING\n");
        _startedAt = EditorApplication.timeSinceStartup;
        _runnerSpawned = false;
        _finished = false;

        if (!File.Exists(ScenePath))
        {
            FinishFail($"Missing scene {ScenePath}");
            return;
        }

        EditorSceneManager.OpenScene(ScenePath);

        // Domain reload clears statics assigned before EnterPlaymode — re-wire in OnPlayModeChanged.
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.update += Poll;
        EditorApplication.EnterPlaymode();
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode)
            return;

        EditorApplication.playModeStateChanged -= OnPlayModeChanged;

        // Re-bind after domain reload so Finish* still runs if callbacks fire.
        CoreLoopSmokeRunner.OnSuccess = FinishSuccess;
        CoreLoopSmokeRunner.OnFail = FinishFail;

        var go = new GameObject("~CoreLoopSmokeRunner");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<CoreLoopSmokeRunner>();
        _runnerSpawned = true;
        Debug.Log("[CoreLoopSmoke] Runtime driver spawned");
    }

    private static void Poll()
    {
        if (_finished) return;

        if (File.Exists(ResultPath))
        {
            string text = File.ReadAllText(ResultPath);
            if (text.StartsWith("SUCCESS"))
            {
                FinishSuccess(text.Replace("SUCCESS\n", "").Trim());
                return;
            }
            if (text.StartsWith("FAIL"))
            {
                FinishFail(text.Replace("FAIL\n", "").Trim());
                return;
            }
        }

        if (EditorApplication.timeSinceStartup - _startedAt > TimeoutSeconds)
        {
            string hint = _runnerSpawned ? "runner spawned but no result" : "never entered play mode / runner not spawned";
            FinishFail($"Timed out after {TimeoutSeconds}s ({hint})");
        }
    }

    private static void FinishSuccess(string detail)
    {
        if (_finished) return;
        _finished = true;
        Teardown();
        Debug.Log("[CoreLoopSmoke] SUCCESS " + detail);
        try { File.WriteAllText(ResultPath, "SUCCESS\n" + detail + "\n"); } catch { /* ignore */ }
        if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        EditorApplication.delayCall += () => EditorApplication.Exit(0);
    }

    private static void FinishFail(string message)
    {
        if (_finished) return;
        _finished = true;
        Teardown();
        Debug.LogError("[CoreLoopSmoke] FAIL: " + message);
        try { File.WriteAllText(ResultPath, "FAIL\n" + message + "\n"); } catch { /* ignore */ }
        if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        EditorApplication.delayCall += () => EditorApplication.Exit(1);
    }

    private static void Teardown()
    {
        EditorApplication.update -= Poll;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        CoreLoopSmokeRunner.OnSuccess = null;
        CoreLoopSmokeRunner.OnFail = null;
    }
}
#endif
