using UnityEngine;

namespace PaiSho
{
    public static class RenderUtils
    {
        public static Material CreateColoredMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            var material = new Material(shader);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            else
                material.color = color;

            return material;
        }

        public static void ApplyColor(Renderer renderer, Color color)
        {
            if (renderer == null)
                return;

            renderer.material = CreateColoredMaterial(color);
        }
    }
}
