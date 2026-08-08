#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Headless play-mode smoke test entry point (no GUI / computerUse agents).
///
///   xvfb-run -a Unity -batchmode -projectPath . -executeMethod CoreLoopSmokeTest.Run
///
/// Domain reload clears static event subscriptions, so pending state is stored in
/// SessionState and handlers are re-bound via [InitializeOnLoadMethod].
/// </summary>
public static class CoreLoopSmokeTest
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string ResultPath = CoreLoopSmokeRunner.ResultPath;
    private const string PendingKey = "CoreLoopSmokeTest.Pending";
    private const string StartedAtKey = "CoreLoopSmokeTest.StartedAt";
    private const string RunnerSpawnedKey = "CoreLoopSmokeTest.RunnerSpawned";
    private const string FinishedKey = "CoreLoopSmokeTest.Finished";
    private const float TimeoutSeconds = 90f;

    [MenuItem("PaiSho/Run Core Loop Smoke Test")]
    public static void Run()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ResultPath));
        File.WriteAllText(ResultPath, "STARTING\n");

        SessionState.SetBool(PendingKey, true);
        SessionState.SetBool(RunnerSpawnedKey, false);
        SessionState.SetBool(FinishedKey, false);
        SessionState.SetString(StartedAtKey, EditorApplication.timeSinceStartup.ToString("R"));

        if (!File.Exists(ScenePath))
        {
            FinishFail($"Missing scene {ScenePath}");
            return;
        }

        EditorSceneManager.OpenScene(ScenePath);
        // Handlers are attached in OnLoad (also runs after domain reload).
        EnsureHandlers();
        EditorApplication.EnterPlaymode();
    }

    [InitializeOnLoadMethod]
    private static void OnLoad()
    {
        if (!SessionState.GetBool(PendingKey, false))
            return;

        EnsureHandlers();

        // If we reloaded while already in play mode, spawn immediately.
        if (EditorApplication.isPlaying && !SessionState.GetBool(RunnerSpawnedKey, false))
            SpawnRunner();
    }

    private static void EnsureHandlers()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.update -= Poll;
        EditorApplication.update += Poll;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(PendingKey, false))
            return;

        if (state == PlayModeStateChange.EnteredPlayMode)
            SpawnRunner();
    }

    private static void SpawnRunner()
    {
        if (SessionState.GetBool(RunnerSpawnedKey, false))
            return;

        CoreLoopSmokeRunner.OnSuccess = FinishSuccess;
        CoreLoopSmokeRunner.OnFail = FinishFail;

        var go = new GameObject("~CoreLoopSmokeRunner");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<CoreLoopSmokeRunner>();
        SessionState.SetBool(RunnerSpawnedKey, true);
        Debug.Log("[CoreLoopSmoke] Runtime driver spawned");
    }

    private static void Poll()
    {
        if (!SessionState.GetBool(PendingKey, false))
            return;
        if (SessionState.GetBool(FinishedKey, false))
            return;

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

        if (!double.TryParse(SessionState.GetString(StartedAtKey, "0"), out double startedAt))
            startedAt = EditorApplication.timeSinceStartup;

        if (EditorApplication.timeSinceStartup - startedAt > TimeoutSeconds)
        {
            string hint = SessionState.GetBool(RunnerSpawnedKey, false)
                ? "runner spawned but no result"
                : "never entered play mode / runner not spawned";
            FinishFail($"Timed out after {TimeoutSeconds}s ({hint})");
        }
    }

    private static void FinishSuccess(string detail)
    {
        if (SessionState.GetBool(FinishedKey, false)) return;
        SessionState.SetBool(FinishedKey, true);
        Teardown();
        Debug.Log("[CoreLoopSmoke] SUCCESS " + detail);
        try { File.WriteAllText(ResultPath, "SUCCESS\n" + detail + "\n"); } catch { /* ignore */ }
        if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        EditorApplication.delayCall += () => EditorApplication.Exit(0);
    }

    private static void FinishFail(string message)
    {
        if (SessionState.GetBool(FinishedKey, false)) return;
        SessionState.SetBool(FinishedKey, true);
        Teardown();
        Debug.LogError("[CoreLoopSmoke] FAIL: " + message);
        try { File.WriteAllText(ResultPath, "FAIL\n" + message + "\n"); } catch { /* ignore */ }
        if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        EditorApplication.delayCall += () => EditorApplication.Exit(1);
    }

    private static void Teardown()
    {
        SessionState.SetBool(PendingKey, false);
        EditorApplication.update -= Poll;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        CoreLoopSmokeRunner.OnSuccess = null;
        CoreLoopSmokeRunner.OnFail = null;
    }
}
#endif
