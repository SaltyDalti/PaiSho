using System.Collections.Generic;
using System.IO;
using PaiSho.Board;
using PaiSho.Game;
using PaiSho.Pieces;
using UnityEngine;

/// <summary>
/// Runtime driver for CoreLoopSmokeTest (must live outside Editor/ so Play Mode can load it).
/// Writes /opt/cursor/artifacts/core_loop_smoke.txt so results survive domain reload.
/// </summary>
public class CoreLoopSmokeRunner : MonoBehaviour
{
    public const string ResultPath = "/opt/cursor/artifacts/core_loop_smoke.txt";

    public static System.Action<string> OnSuccess;
    public static System.Action<string> OnFail;

    private float _t0;
    private int _step;
    private bool _done;

    private void Start()
    {
        _t0 = Time.realtimeSinceStartup;
        _step = 1;
        _done = false;
        Debug.Log("[CoreLoopSmoke] Runner Start");
    }

    private void Update()
    {
        if (_done) return;

        try
        {
            float elapsed = Time.realtimeSinceStartup - _t0;

            if (_step == 1)
            {
                if (elapsed < 2f) return;
                if (GameManager.Instance == null || BoardManager.Instance == null || PiecePlacementManager.Instance == null)
                {
                    if (elapsed > 20f) Fail("Managers never became ready");
                    return;
                }

                if (!GameManager.Instance.IsSpringPhase())
                {
                    Fail("Expected Spring Opening at start");
                    return;
                }

                Tile hostTile = BoardManager.Instance.GetTileAt(0, -5);
                if (hostTile == null)
                {
                    Fail("Missing tile (0,-5) — board may not have spawned");
                    return;
                }

                Debug.Log("[CoreLoopSmoke] Host placing Jasmine at (0,-5)");
                PiecePlacementManager.Instance.TryPlacePiece(hostTile);
                _step = 2;
                return;
            }

            if (_step == 2)
            {
                if (elapsed < 3f) return;

                if (GameManager.Instance.GetCurrentPlayer() != Player.Opponent)
                {
                    Fail($"Expected Opponent turn; got {GameManager.Instance.GetCurrentPlayer()}");
                    return;
                }

                Tile oppTile = BoardManager.Instance.GetTileAt(0, 5);
                if (oppTile == null)
                {
                    Fail("Missing tile (0,5)");
                    return;
                }

                Debug.Log("[CoreLoopSmoke] Opponent placing Rose at (0,5)");
                PiecePlacementManager.Instance.TryPlacePiece(oppTile);
                _step = 3;
                return;
            }

            if (_step == 3)
            {
                if (elapsed < 4.5f) return;

                if (GameManager.Instance.IsSpringPhase())
                {
                    Fail("Spring phase did not end");
                    return;
                }

                if (GameManager.Instance.GetCurrentPlayer() != Player.Host)
                {
                    Fail($"Expected Host turn after spring; got {GameManager.Instance.GetCurrentPlayer()}");
                    return;
                }

                Piece jasmine = BoardManager.Instance.GetPieceAt(BoardUtils.ToCoordinate(0, -5));
                if (jasmine == null || jasmine.Type != PieceType.Jasmine)
                {
                    Fail("Host Jasmine missing after spring");
                    return;
                }

                if (MovementManager.Instance == null)
                {
                    Fail("MovementManager missing");
                    return;
                }

                List<int> moves = MovementManager.Instance.GetLegalMoves(jasmine);
                if (moves == null || moves.Count == 0)
                {
                    Fail("Jasmine has zero legal moves");
                    return;
                }

                int dest = moves[0];
                foreach (int m in moves)
                {
                    Vector2Int g = BoardUtils.FromCoordinate(m);
                    if (g.x == 0 && g.y > -5)
                    {
                        dest = m;
                        break;
                    }
                }

                Vector2Int destGrid = BoardUtils.FromCoordinate(dest);
                Tile destTile = BoardManager.Instance.GetTileAt(destGrid.x, destGrid.y);
                if (destTile == null)
                {
                    Fail($"No tile at dest {destGrid}");
                    return;
                }

                int start = jasmine.GetPosition();
                Debug.Log($"[CoreLoopSmoke] Moving Jasmine {BoardUtils.FromCoordinate(start)} -> {destGrid}");

                BoardManager.Instance.MovePiece(jasmine, dest);
                Tile startTile = BoardManager.Instance.GetTileAt(0, -5);
                if (startTile != null) startTile.SetPiece(null);
                destTile.SetPiece(jasmine);
                jasmine.transform.position = destTile.transform.position + Vector3.up * 0.1f;

                MovementManager.Instance.RegisterMove(jasmine);
                if (GameLogManager.Instance != null)
                    GameLogManager.Instance.LogMove(jasmine.Owner, jasmine.Type, start, dest);
                GameManager.Instance.MarkTurnComplete();
                if (CaptureManager.Instance != null)
                    CaptureManager.Instance.CheckForCaptures(jasmine);
                GameManager.Instance.EndTurn();

                _step = 4;
                return;
            }

            if (_step == 4)
            {
                if (elapsed < 5.5f) return;

                Piece jasmine = null;
                foreach (var p in BoardManager.Instance.GetAllPieces())
                {
                    if (p != null && p.Owner == Player.Host && p.Type == PieceType.Jasmine)
                    {
                        jasmine = p;
                        break;
                    }
                }

                if (jasmine == null)
                {
                    Fail("Jasmine missing after move");
                    return;
                }

                if (jasmine.GetPosition() == BoardUtils.ToCoordinate(0, -5))
                {
                    Fail("Jasmine still at start after move");
                    return;
                }

                Piece rose = BoardManager.Instance.GetPieceAt(BoardUtils.ToCoordinate(0, 5));
                if (rose == null)
                {
                    Fail("Rose missing — unexpected capture?");
                    return;
                }

                Succeed($"moved_jasmine_to={BoardUtils.FromCoordinate(jasmine.GetPosition())}; rose_intact=true; legal_moves_ok=true");
                return;
            }

            if (elapsed > 45f)
                Fail($"Timed out at step {_step}");
        }
        catch (System.Exception ex)
        {
            Fail(ex.ToString());
        }
    }

    private void Succeed(string detail)
    {
        if (_done) return;
        _done = true;
        WriteResult("SUCCESS\n" + detail + "\n");
        Debug.Log("[CoreLoopSmoke] SUCCESS " + detail);
        OnSuccess?.Invoke(detail);
        this.enabled = false;
    }

    private void Fail(string message)
    {
        if (_done) return;
        _done = true;
        WriteResult("FAIL\n" + message + "\n");
        Debug.LogError("[CoreLoopSmoke] FAIL: " + message);
        OnFail?.Invoke(message);
        this.enabled = false;
    }

    private static void WriteResult(string contents)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ResultPath));
            File.WriteAllText(ResultPath, contents);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[CoreLoopSmoke] Could not write result file: " + ex.Message);
        }
    }
}
