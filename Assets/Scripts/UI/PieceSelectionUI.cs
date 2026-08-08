using UnityEngine;
using UnityEngine.UI;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    public class PieceSelectionUI : MonoBehaviour
    {
        public static PieceSelectionUI Instance;

        [Header("Piece Buttons")]
        public Button jasmineButton;
        public Button roseButton;
        public Button lilyButton;
        public Button jadeButton;
        public Button rhododendronButton;
        public Button chrysanthemumButton;
        public Button boatButton;
        public Button rockButton;
        public Button knotweedButton;
        public Button wheelButton;
        public Button lotusButton;
        public Button orchidButton;

        [Header("UI Panel")]
        public GameObject pieceSelectionPanel;

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }

        private void Start()
        {
            jasmineButton.onClick.AddListener(() => SelectPiece(PieceType.Jasmine));
            roseButton.onClick.AddListener(() => SelectPiece(PieceType.Rose));
            lilyButton.onClick.AddListener(() => SelectPiece(PieceType.Lily));
            jadeButton.onClick.AddListener(() => SelectPiece(PieceType.Jade));
            rhododendronButton.onClick.AddListener(() => SelectPiece(PieceType.Rhododendron));
            chrysanthemumButton.onClick.AddListener(() => SelectPiece(PieceType.Chrysanthemum));
            boatButton.onClick.AddListener(() => SelectPiece(PieceType.Boat));
            rockButton.onClick.AddListener(() => SelectPiece(PieceType.Rock));
            knotweedButton.onClick.AddListener(() => SelectPiece(PieceType.Knotweed));
            wheelButton.onClick.AddListener(() => SelectPiece(PieceType.Wheel));
            lotusButton.onClick.AddListener(() => SelectPiece(PieceType.Lotus));
            orchidButton.onClick.AddListener(() => SelectPiece(PieceType.Orchid));
        }

        private void Update()
        {
            if (pieceSelectionPanel == null || PiecePlacementManager.Instance == null || GameManager.Instance == null)
                return;

            // Hide during Spring Opening; after that, hide only while a placement is armed.
            if (GameManager.Instance.IsSpringPhase())
            {
                if (pieceSelectionPanel.activeSelf)
                    pieceSelectionPanel.SetActive(false);
                return;
            }

            bool shouldShow = !PiecePlacementManager.Instance.IsPlacingPiece();
            if (pieceSelectionPanel.activeSelf != shouldShow)
                pieceSelectionPanel.SetActive(shouldShow);

            // Hotkeys so placement works even when the UI panel is hard to click in Game view.
            HandleHotkeys();
        }

        private void HandleHotkeys()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
                SelectPiece(PieceType.Jasmine);
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
                SelectPiece(PieceType.Rose);
            else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
                SelectPiece(PieceType.Lily);
            else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
                SelectPiece(PieceType.Jade);
            else if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5))
                SelectPiece(PieceType.Rhododendron);
            else if (Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6))
                SelectPiece(PieceType.Chrysanthemum);
            else if (Input.GetKeyDown(KeyCode.Alpha7) || Input.GetKeyDown(KeyCode.Keypad7))
                SelectPiece(PieceType.Boat);
            else if (Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Keypad8))
                SelectPiece(PieceType.Rock);
            else if (Input.GetKeyDown(KeyCode.Alpha9) || Input.GetKeyDown(KeyCode.Keypad9))
                SelectPiece(PieceType.Knotweed);
            else if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0))
                SelectPiece(PieceType.Wheel);
            else if (Input.GetKeyDown(KeyCode.Minus))
                SelectPiece(PieceType.Lotus);
            else if (Input.GetKeyDown(KeyCode.Equals))
                SelectPiece(PieceType.Orchid);
        }

        private void SelectPiece(PieceType type)
        {
            PiecePlacementManager.Instance.SelectPieceToPlace(type);
        }

        public void ShowPanel()
        {
            if (pieceSelectionPanel != null)
                pieceSelectionPanel.SetActive(true);
        }

        public void HidePanel()
        {
            if (pieceSelectionPanel != null)
                pieceSelectionPanel.SetActive(false);
        }

    }
}
