using System.Collections.Generic;
using UnityEngine;
using PaiSho.Pieces;
using PaiSho.Board;

namespace PaiSho.Game
{
    public class EchoTileManager : MonoBehaviour
    {
        public static EchoTileManager Instance;

        private readonly Dictionary<Player, int> revivalPoints = new Dictionary<Player, int>();
        private readonly Dictionary<Player, int> echoCounts = new Dictionary<Player, int>();
        private int totalEchoesSummoned;

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;

            revivalPoints[Player.Host] = 0;
            revivalPoints[Player.Opponent] = 0;
            echoCounts[Player.Host] = 0;
            echoCounts[Player.Opponent] = 0;
        }

        public void AddRevivalPoints(Player player, int amount)
        {
            if (amount <= 0) return;

            revivalPoints[player] += amount;
            DebugLogger.Log($">>> {player} earned {amount} revival point(s). Total: {revivalPoints[player]}");

            while (revivalPoints[player] >= 10)
            {
                if (!TrySummonEcho(player))
                    break;
                revivalPoints[player] -= 10;
            }
        }

        private bool TrySummonEcho(Player player)
        {
            List<PotManager.CapturedPieceInfo> pot = PotManager.Instance.GetAllCapturedPieces();
            List<PotManager.CapturedPieceInfo> eligible = pot.FindAll(p =>
                p.Owner != player && // echo of captured enemy flowers
                p.Type != PieceType.Lotus &&
                p.Type != PieceType.Orchid &&
                Piece.IsFlowerType(p.Type));

            // Also allow own flowers that were somehow recorded; prefer any flower type.
            if (eligible.Count == 0)
            {
                eligible = pot.FindAll(p =>
                    p.Type != PieceType.Lotus &&
                    p.Type != PieceType.Orchid &&
                    Piece.IsFlowerType(p.Type));
            }

            if (eligible.Count == 0)
            {
                DebugLogger.Log($">>> {player} has revival points but no eligible pot flowers for an Echo.");
                return false;
            }

            var rand = new System.Random();
            var candidate = eligible[rand.Next(eligible.Count)];

            int targetPos = candidate.Coordinate;
            if (!BoardUtils.LegalPoints.Contains(targetPos) || BoardManager.Instance.IsOccupied(targetPos))
            {
                // Fall back to nearest empty legal tile to center.
                targetPos = FindEmptyLegalNearCenter();
                if (targetPos < 0)
                {
                    DebugLogger.Log($">>> {player} could not place Echo — board full.");
                    return false;
                }
            }

            Piece echo = SpawnEchoPiece(player, candidate.Type, targetPos);
            if (echo == null)
                return false;

            echo.IsNewThisTurn = true;
            echo.IsGhost = true;
            echo.PointValue *= 2;
            echo.SetVisualState("ghost");

            echoCounts[player]++;
            totalEchoesSummoned++;

            DebugLogger.Log($">>> {player} summoned Ghost Echo {candidate.Type} at {targetPos}.");
            return true;
        }

        private static Piece SpawnEchoPiece(Player player, PieceType type, int coordinate)
        {
            Vector2Int grid = BoardUtils.FromCoordinate(coordinate);
            Tile tile = BoardManager.Instance.GetTileAt(grid.x, grid.y);
            Vector3 pos = tile != null
                ? tile.transform.position + Vector3.up * 0.1f
                : new Vector3(grid.x * 1.5f, 0.1f, grid.y * 1.5f);

            // Prefer placement manager prefabs / procedural path.
            GameObject go = null;
            if (PiecePlacementManager.Instance != null)
            {
                // Use a tiny reflective approach: create procedural via public placement isn't available.
                // Fall back to BoardManager placeholder + visual cube.
            }

            go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = $"Echo_{type}";
            go.transform.position = pos;
            go.transform.localScale = new Vector3(1.0f, 0.1f, 1.0f);
            Object.Destroy(go.GetComponent<Collider>());

            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mr.material.color = new Color(0.7f, 0.85f, 1f, 0.65f);
            }

            Piece piece = go.AddComponent<Piece>();
            piece.Initialize(player, type);
            BoardManager.Instance.PlacePiece(piece, coordinate);
            if (tile != null)
                tile.SetPiece(piece);
            return piece;
        }

        private static int FindEmptyLegalNearCenter()
        {
            if (!BoardManager.Instance.IsOccupied(BoardUtils.CenterPortCoordinate))
                return BoardUtils.CenterPortCoordinate;

            for (int radius = 1; radius <= 9; radius++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    for (int z = -radius; z <= radius; z++)
                    {
                        if (Mathf.Abs(x) != radius && Mathf.Abs(z) != radius)
                            continue;
                        int coord = BoardUtils.ToCoordinate(x, z);
                        if (BoardUtils.LegalPoints.Contains(coord) && !BoardManager.Instance.IsOccupied(coord))
                            return coord;
                    }
                }
            }
            return -1;
        }

        public int GetEchoCount(Player player)
        {
            return echoCounts.TryGetValue(player, out int count) ? count : 0;
        }

        public void OnEchoMoved(Piece piece)
        {
            if (piece == null || !piece.IsGhost) return;

            piece.IsGhost = false;
            piece.SetVisualState("vibrant");
            DebugLogger.Log($">>> Echo Tile {piece.Type} has entered play.");
        }

        public int GetEchoCount()
        {
            return totalEchoesSummoned;
        }
    }
}
