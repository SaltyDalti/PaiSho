using UnityEngine;
using UnityEngine.Scripting;
using PaiSho.Game;

namespace PaiSho.Presentation
{
    /// <summary>
    /// Safety net after scene Awakes: hollow any remaining hand-tray SampleTile meshes so
    /// Safari title frames are not skinning fourteen authored tile prefabs (HJ-37).
    /// Primary strip is bake-time <c>WebGlSampleTileStripBuildStep</c> + marker Awake.
    /// Pose anchors stay so New Game can <c>ApplySampleTilePose</c>.
    /// Do not deactivate PlayerStand / HandTray* here — <c>IsLiveTrayRoot</c> requires
    /// activeInHierarchy for New Game reattach.
    /// </summary>
    [Preserve]
    internal static class WebGlBootDefer
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        [Preserve]
        private static void HollowHandTraySampleTiles()
        {
            HandTraySlotSampleTile[] samples =
                Object.FindObjectsByType<HandTraySlotSampleTile>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < samples.Length; i++)
            {
                if (samples[i] != null)
                    HandTrayLayoutUtility.HollowSampleTile(samples[i].transform);
            }

            // Also catch SampleTiles that lost the marker component during bake strip.
            HandTraySlotMarker[] markers =
                Object.FindObjectsByType<HandTraySlotMarker>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < markers.Length; i++)
            {
                if (markers[i] == null)
                    continue;

                Transform sample = markers[i].transform.Find(HandTraySlotMarker.SampleTileName);
                if (sample != null && HandTrayLayoutUtility.HasRenderableMesh(sample))
                    HandTrayLayoutUtility.HollowSampleTile(sample);
            }

            // Soften title-frame stand mesh work: scene PrefabInstance stands are replaced on
            // New Game, but their renderers still deserialize with the trays. Disable only —
            // do not destroy (trays parent under these roots until Rebuild).
            DisableScenePlayerStandRenderers();
        }

        private static void DisableScenePlayerStandRenderers()
        {
            GameObject[] roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            for (int r = 0; r < roots.Length; r++)
                DisableStandRenderersUnder(roots[r].transform);
        }

        private static void DisableStandRenderersUnder(Transform root)
        {
            if (root == null)
                return;

            if (root.name == "PlayerStand" || root.name == "PlayerStandOpponent")
            {
                Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] == null)
                        continue;
                    // Keep HandTray* / SampleTile / Slot_* out of this pass — trays already hollowed.
                    if (GameBoardSetup.IsHandTrayAuthoringHierarchy(renderers[i].transform))
                        continue;
                    renderers[i].enabled = false;
                }

                return;
            }

            for (int i = 0; i < root.childCount; i++)
                DisableStandRenderersUnder(root.GetChild(i));
        }
#endif
    }
}
