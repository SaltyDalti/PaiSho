using UnityEngine;
using UnityEngine.Scripting;
using PaiSho.Game;

namespace PaiSho.Presentation
{
    /// <summary>
    /// Safety net after scene Awakes: destroy any remaining hand-tray SampleTile meshes so
    /// Safari title frames are not skinning fourteen authored tile prefabs (HJ-32).
    /// Primary strip is <see cref="HandTraySlotMarker"/> Awake (early execution order).
    /// Do not deactivate PlayerStand / HandTray* here — <c>IsLiveTrayRoot</c> requires
    /// activeInHierarchy for New Game reattach.
    /// </summary>
    [Preserve]
    internal static class WebGlBootDefer
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        [Preserve]
        private static void StripHandTraySampleTiles()
        {
            HandTraySlotSampleTile[] samples =
                Object.FindObjectsByType<HandTraySlotSampleTile>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < samples.Length; i++)
            {
                if (samples[i] != null)
                    Object.Destroy(samples[i].gameObject);
            }
        }
#endif
    }
}
