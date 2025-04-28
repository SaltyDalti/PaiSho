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
            if (pieceSelectionPanel != null && PiecePlacementManager.Instance != null)
            {
                pieceSelectionPanel.SetActive(!PiecePlacementManager.Instance.IsPlacingPiece());
            }
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
