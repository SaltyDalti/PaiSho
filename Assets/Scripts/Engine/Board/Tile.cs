using UnityEngine;
using PaiSho.Board;
using PaiSho.Pieces;

public class Tile : MonoBehaviour
{
    [SerializeField] private GameObject highlightVisual;
    private Piece occupyingPiece;
    private int x;
    private int z;

    public bool IsDecorative { get; private set; } = false;

    public void SetGridPosition(int x, int z)
    {
        this.x = x;
        this.z = z;
    }

    public Vector2Int GetGridPosition()
    {
        return new Vector2Int(x, z);
    }

    public void SetPiece(Piece piece)
    {
        occupyingPiece = piece;
    }

    public Piece GetPiece()
    {
        return occupyingPiece;
    }

    /// <summary>
    /// Kept for callers that still set an explicit coordinate. The authoritative
    /// value always comes from the grid position via <see cref="GetCoordinate"/>.
    /// </summary>
    public void SetCoordinate(int coord)
    {
        Vector2Int grid = BoardUtils.FromCoordinate(coord);
        x = grid.x;
        z = grid.y;
    }

    public int GetCoordinate()
    {
        return BoardUtils.ToCoordinate(x, z);
    }

    public bool HasPiece()
    {
        return occupyingPiece != null;
    }

    public void EnableHighlight()
    {
        if (highlightVisual != null && !IsDecorative)
            highlightVisual.SetActive(true);
    }

    public void DisableHighlight()
    {
        if (highlightVisual != null)
            highlightVisual.SetActive(false);
    }

    public void MarkAsDecorative()
    {
        IsDecorative = true;
    }
}
