using UnityEngine;
using PaiSho;

namespace PaiSho.Board
{
    public static class BoardPointMarkerVisual
    {
        private static Material markerMaterial;

        public static GameObject CreateMarker(Transform parent, string name, Color color)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = name;
            marker.transform.SetParent(parent, false);

            var collider = marker.GetComponent<Collider>();
            if (collider != null)
                Object.Destroy(collider);

            var renderer = marker.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = GetMaterial(color);
                renderer.material.renderQueue = 3200;
            }

            return marker;
        }

        public static void UpdateMarker(
            GameObject marker,
            BoardLayout layout,
            int coordinate,
            float diameterScale,
            float heightOffset,
            Color color)
        {
            if (marker == null || layout == null)
                return;

            marker.transform.position = layout.CoordinateToWorld(coordinate);
            float diameter = layout.CellSpacing * diameterScale;
            marker.transform.localScale = new Vector3(diameter, diameter * 0.35f, diameter);
            marker.transform.position += Vector3.up * heightOffset;

            var renderer = marker.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = GetMaterial(color);
        }

        private static Material GetMaterial(Color color)
        {
            if (markerMaterial == null)
                markerMaterial = WoodTheme.CreateWoodMaterial(color, 0.2f);
            else if (markerMaterial.HasProperty("_BaseColor"))
                markerMaterial.SetColor("_BaseColor", color);
            else
                markerMaterial.color = color;

            if (markerMaterial.HasProperty("_EmissionColor"))
            {
                markerMaterial.EnableKeyword("_EMISSION");
                markerMaterial.SetColor("_EmissionColor", color * 1.2f);
            }

            return markerMaterial;
        }
    }
}
