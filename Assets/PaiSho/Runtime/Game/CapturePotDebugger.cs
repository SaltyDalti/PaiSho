using UnityEngine;
using UnityEngine.InputSystem;
using PaiSho.DevTools;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    /// <summary>Runtime overlay to spawn captured tiles into pots. Press F10 in play mode.</summary>
    public class CapturePotDebugger : MonoBehaviour
    {
        public static CapturePotDebugger Instance;

        private static readonly (PieceType Type, string Label)[] FlowerTypes =
        {
            (PieceType.Jasmine, "Jas"),
            (PieceType.Lily, "Lily"),
            (PieceType.Jade, "Jade"),
            (PieceType.Rose, "Rose"),
            (PieceType.Chrysanthemum, "Chry"),
            (PieceType.Rhododendron, "Rhod")
        };

        private static readonly (PieceType Type, string Label)[] SpecialTypes =
        {
            (PieceType.Lotus, "Lotus"),
            (PieceType.Orchid, "Orch"),
            (PieceType.Boat, "Boat"),
            (PieceType.Wheel, "Wheel"),
            (PieceType.Knotweed, "Knot"),
            (PieceType.Rock, "Rock")
        };

        [SerializeField] private Key toggleKey = Key.F10;
        [SerializeField] private bool panelOpen;

        private Player targetPot = Player.Host;
        private Vector2 scroll;
        private Rect lastContentArea;
        private string statusMessage = "F10 capture pot debugger";
        private float statusClearTime;

        public bool IsPanelOpen => panelOpen;

        private void Awake()
        {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            enabled = false;
            return;
#endif
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (Keyboard.current == null)
                return;

            if (Keyboard.current[toggleKey].wasPressedThisFrame)
            {
                panelOpen = !panelOpen;
                if (panelOpen)
                    SetStatus("Add tiles to test compact pot layout");
            }
        }

        private void OnGUI()
        {
            if (!panelOpen)
            {
                DevTunerGui.DrawClosedHints(
                    (KeyCode.F8, "Board"),
                    (KeyCode.F9, "Hand tray"),
                    (KeyCode.F10, "Capture pot"));
                return;
            }

            DrawPanel();
        }

        private void DrawPanel()
        {
            DevTunerGui.BeginPanel(
                "Capture Pot [F10]",
                DevTunerGui.DefaultPanelWidth,
                DevTunerGui.DefaultMargin,
                DevTunerGui.FooterHeight,
                ref scroll,
                out lastContentArea);

            int potIndex = targetPot == Player.Host ? 0 : 1;
            DevTunerGui.Toolbar(potIndex, new[] { "Host pot", "Opponent pot" }, index =>
            {
                targetPot = index == 0 ? Player.Host : Player.Opponent;
                SetStatus($"Adding to {targetPot} capture pot");
            });

            int count = PotManager.Instance != null ? PotManager.Instance.CountCapturedBy(targetPot) : 0;
            GUILayout.Label($"Tiles in {targetPot} pot: {count}");
            GUILayout.Label("Each click adds one captured tile and relayouts.", GUI.skin.label);

            DrawTypeSection("Flowers (compact lanes 0–5)", FlowerTypes);
            DrawTypeSection("Special & other", SpecialTypes);

            DevTunerGui.Status(statusMessage, statusClearTime);

            DevTunerGui.EndPanelScroll();
            DevTunerGui.DrawFooter(lastContentArea, DevTunerGui.FooterHeight, () =>
            {
                DevTunerGui.ActionBar(
                    ("Clear pot", ClearTargetPot),
                    ("Clear all", ClearAllPots),
                    ("Close", () => panelOpen = false));
            });
        }

        private void DrawTypeSection(string title, (PieceType Type, string Label)[] types)
        {
            GUILayout.Label(title, GUI.skin.box);
            const int columns = 3;
            for (int i = 0; i < types.Length; i += columns)
            {
                GUILayout.BeginHorizontal();
                for (int col = 0; col < columns && i + col < types.Length; col++)
                {
                    (PieceType type, string label) = types[i + col];
                    if (GUILayout.Button(label, GUILayout.Height(28f)))
                        AddTile(type);
                }

                GUILayout.EndHorizontal();
            }
        }

        private void AddTile(PieceType type)
        {
            if (PotVisualManager.Instance == null)
            {
                SetStatus("PotVisualManager missing");
                return;
            }

            if (!PotVisualManager.Instance.TryAddDebugCapture(type, targetPot))
            {
                SetStatus($"Failed to add {type}");
                return;
            }

            int count = PotManager.Instance?.CountCapturedBy(targetPot) ?? 0;
            PotVisualManager.Instance.TryDescribePot(targetPot, out string potInfo);
            SetStatus($"Added {type} ({count} total). {potInfo}");
        }

        private void ClearTargetPot()
        {
            PotVisualManager.Instance?.ClearPot(targetPot);
            SetStatus($"Cleared {targetPot} pot");
        }

        private void ClearAllPots()
        {
            PotVisualManager.Instance?.ClearAll();
            SetStatus("Cleared both pots");
        }

        private void SetStatus(string message)
        {
            statusMessage = message;
            statusClearTime = Time.unscaledTime + 6f;
        }
    }
}
