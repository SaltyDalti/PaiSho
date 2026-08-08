using UnityEngine;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    /// <summary>Marks a 3D tile in the physical hand rack for drag-and-drop placement.</summary>
    public class HandTileHandle : MonoBehaviour
    {
        public PieceType PieceType;
        public int SlotIndex;
        public bool IsSpringDraw;
        public bool Locked;
        public Vector3 RestPosition;
        public Quaternion RestRotation;
        public Vector3 RestScale;
    }
}
