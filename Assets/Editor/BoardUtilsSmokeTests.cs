#if UNITY_EDITOR
using System.Collections.Generic;
using PaiSho.Board;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Lightweight board-encoding smoke checks. Run via:
///   Unity -batchmode -quit -projectPath . -executeMethod BoardUtilsSmokeTests.Run
/// </summary>
public static class BoardUtilsSmokeTests
{
    [MenuItem("PaiSho/Run Board Utils Smoke Tests")]
    public static void Run()
    {
        int failures = 0;

        void Expect(bool condition, string message)
        {
            if (!condition)
            {
                Debug.LogError("[BoardUtilsSmokeTests] FAIL: " + message);
                failures++;
            }
            else
            {
                Debug.Log("[BoardUtilsSmokeTests] PASS: " + message);
            }
        }

        Expect(BoardUtils.ToCoordinate(0, 0) == 189, "Center ToCoordinate(0,0) == 189");
        Expect(BoardUtils.CenterPortCoordinate == 189, "CenterPortCoordinate == 189");
        Expect(BoardUtils.FromCoordinate(189) == new Vector2Int(0, 0), "FromCoordinate(189) == (0,0)");
        Expect(BoardUtils.CoordStride == 20, "CoordStride == 20");
        Expect(BoardUtils.LegalPoints.Contains(189), "Center is a legal point");
        Expect(!BoardUtils.LegalPoints.Contains(BoardUtils.ToCoordinate(9, 9))
               || (Mathf.Abs(9) + Mathf.Abs(9) <= 12),
            "Corner legality matches manhattan rule");

        // Round-trip a ring of cells.
        for (int x = -3; x <= 3; x++)
        {
            for (int z = -3; z <= 3; z++)
            {
                int coord = BoardUtils.ToCoordinate(x, z);
                Vector2Int back = BoardUtils.FromCoordinate(coord);
                Expect(back.x == x && back.y == z, $"Round-trip ({x},{z})");
            }
        }

        List<int> neighbors = BoardUtils.GetNeighbors(189);
        Expect(neighbors.Count > 0, "Center has neighbors");
        Expect(neighbors.Contains(BoardUtils.ToCoordinate(0, 1)), "Center neighbor (0,1)");
        Expect(neighbors.Contains(BoardUtils.ToCoordinate(1, 0)), "Center neighbor (1,0)");

        if (failures == 0)
        {
            Debug.Log("[BoardUtilsSmokeTests] ALL PASSED");
            if (Application.isBatchMode)
                EditorApplication.Exit(0);
        }
        else
        {
            Debug.LogError($"[BoardUtilsSmokeTests] {failures} failure(s)");
            if (Application.isBatchMode)
                EditorApplication.Exit(1);
        }
    }
}
#endif
