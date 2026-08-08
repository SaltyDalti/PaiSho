#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using PaiSho;

namespace PaiSho.EditorTools
{
    public static class JasminePrefabRepair
    {
        [MenuItem("Pai Sho/Repair Jasmine Tile Prefab")]
        public static void RepairFromMenu()
        {
            TilePrefabBaker.BakeAllFromMenu();
        }
    }
}
#endif
