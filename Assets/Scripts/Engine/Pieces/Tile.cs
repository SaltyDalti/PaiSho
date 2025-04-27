using UnityEngine;
using PaiSho.Pieces;

public class Tile : MonoBehaviour
{
    [SerializeField] private GameObject highlightVisual;
    private Piece occupyingPiece;

    public void SetPiece(Piece piece)
    {
        occupyingPiece = piece;
    }

    public Piece GetPiece()
    {
        return occupyingPiece;
    }

    private int coordinate;

    public void SetCoordinate(int coord)
    {
        coordinate = coord;
    }

    public int GetCoordinate()
    {
        return coordinate;
    }

    public bool HasPiece()
    {
        return occupyingPiece != null;
    }

    public void EnableHighlight()
    {
        if (highlightVisual != null)
            highlightVisual.SetActive(true);
    }

    public void DisableHighlight()
    {
        if (highlightVisual != null)
            highlightVisual.SetActive(false);
    }
}
