#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using PaiSho;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PaiSho.EditorTools
{
    public static class BlenderPathSetup
    {
        private const string SessionSetupKey = "PaiSho.BlenderPathSetupAttempted";

        private static readonly string[] MacBlenderCandidates =
        {
            "/Applications/Blender.app/Contents/MacOS/Blender",
            "/Applications/Blender 4.4.app/Contents/MacOS/Blender",
            "/Applications/Blender 4.3.app/Contents/MacOS/Blender",
            "/Applications/Blender 4.2.app/Contents/MacOS/Blender",
            "/Applications/Blender 4.1.app/Contents/MacOS/Blender",
            "/Applications/Blender 4.0.app/Contents/MacOS/Blender"
        };

        [MenuItem("Pai Sho/Setup Blender for Unity")]
        public static void SetupFromMenu()
        {
            SessionState.EraseBool(SessionSetupKey);
            if (TrySetupBlender(out string message))
                Debug.Log(message);
            else
                Debug.LogError(message);
        }

        [InitializeOnLoadMethod]
        private static void SetupOnLoadIfNeeded()
        {
            EditorApplication.delayCall += () =>
            {
                if (SessionState.GetBool(SessionSetupKey, false))
                    return;

                SessionState.SetBool(SessionSetupKey, true);
                TrySetupBlender(out _);
            };
        }

        public static bool TrySetupBlender(out string message)
        {
            if (!TryFindBlenderExecutable(out string blenderExecutable))
            {
                message =
                    "Blender was not found. Install Blender to /Applications/Blender.app, " +
                    "then run Pai Sho > Setup Blender for Unity.";
                return false;
            }

            EditorPrefs.SetString("BlenderExecutable", blenderExecutable);
            EditorPrefs.SetString("FBXBlenderPath", blenderExecutable);

            if (!TryEnsureCommandLineAlias(blenderExecutable, out string aliasMessage))
            {
                message =
                    $"Found Blender at {blenderExecutable}.\n{aliasMessage}\n" +
                    "After creating the symlink, fully quit Unity (not just Stop Play) and reopen the project.";
                return false;
            }

            if (!TryLaunchBlender(blenderExecutable, out string launchError))
            {
                message = $"Found Blender at {blenderExecutable}, but a test launch failed: {launchError}";
                return false;
            }

            message =
                $"Blender is configured for Unity ({blenderExecutable}).\n" +
                "If .blend imports still fail, fully quit and reopen Unity so it picks up PATH changes.";
            return true;
        }

        private static bool TryFindBlenderExecutable(out string blenderExecutable)
        {
            blenderExecutable = null;

#if UNITY_EDITOR_OSX
            foreach (string candidate in MacBlenderCandidates)
            {
                if (File.Exists(candidate))
                {
                    blenderExecutable = candidate;
                    return true;
                }
            }
#endif

            string fromPath = FindOnPath("blender");
            if (!string.IsNullOrEmpty(fromPath))
            {
                blenderExecutable = fromPath;
                return true;
            }

            return false;
        }

        private static bool TryEnsureCommandLineAlias(string blenderExecutable, out string message)
        {
            message = null;

            string fromPath = FindOnPath("blender");
            if (!string.IsNullOrEmpty(fromPath) && PathsEqual(fromPath, blenderExecutable))
            {
                message = $"`blender` is already on PATH ({fromPath}).";
                return true;
            }

#if UNITY_EDITOR_OSX
            string linkPath = "/usr/local/bin/blender";
            try
            {
                if (File.Exists(linkPath) || Directory.Exists(linkPath))
                    File.Delete(linkPath);

                using var linkProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "/bin/ln",
                        Arguments = $"-sf \"{blenderExecutable}\" \"{linkPath}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                linkProcess.Start();
                linkProcess.WaitForExit();
                if (linkProcess.ExitCode != 0)
                    throw new IOException($"ln exited with code {linkProcess.ExitCode}");

                if (File.Exists(linkPath))
                {
                    message = $"Created symlink {linkPath} -> {blenderExecutable}. Restart Unity if .blend imports still fail.";
                    return true;
                }

                fromPath = FindOnPath("blender");
                if (!string.IsNullOrEmpty(fromPath))
                {
                    message = $"Created symlink {linkPath} -> {blenderExecutable}.";
                    return true;
                }
            }
            catch (Exception ex)
            {
                message =
                    "Create this symlink in Terminal, then restart Unity:\n" +
                    $"ln -sf \"{blenderExecutable}\" /usr/local/bin/blender\n" +
                    $"({ex.Message})";
                return false;
            }
#endif

            message =
                "Unity needs the `blender` command on PATH. Run in Terminal:\n" +
                $"ln -sf \"{blenderExecutable}\" /usr/local/bin/blender\n" +
                "Then fully quit and reopen Unity.";
            return false;
        }

        private static bool TryLaunchBlender(string blenderExecutable, out string error)
        {
            error = null;
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = blenderExecutable,
                        Arguments = "--version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                if (!process.WaitForExit(15000))
                {
                    process.Kill();
                    error = "Timed out waiting for Blender.";
                    return false;
                }

                if (process.ExitCode != 0)
                {
                    error = process.StandardError.ReadToEnd();
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static string FindOnPath(string command)
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "/bin/bash",
                        Arguments = $"-lc \"which {command}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                process.WaitForExit();
                if (process.ExitCode != 0)
                    return null;

                string path = process.StandardOutput.ReadToEnd().Trim();
                return string.IsNullOrEmpty(path) ? null : path;
            }
            catch
            {
                return null;
            }
        }

        private static bool PathsEqual(string left, string right)
        {
            try
            {
                return string.Equals(
                    Path.GetFullPath(left),
                    Path.GetFullPath(right),
                    StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}
#endif
