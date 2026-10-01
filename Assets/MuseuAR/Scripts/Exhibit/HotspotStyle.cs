using TMPro;
using UnityEngine;

namespace MuseuAR
{
    /// <summary>
    /// Aparência de todos os hotspots (setas, marcadores e rótulos).
    /// Criar em: Assets > Create > Museu AR > Estilo dos Hotspots
    /// </summary>
    [CreateAssetMenu(fileName = "EstiloHotspots", menuName = "Museu AR/Estilo dos Hotspots", order = 2)]
    public class HotspotStyle : ScriptableObject
    {
        [Header("Materiais (URP)")]
        [Tooltip("Shader recomendado: Universal Render Pipeline/Particles/Unlit (respeita a cor da linha).")]
        public Material lineMaterial;
        [Tooltip("Shader recomendado: Universal Render Pipeline/Unlit.")]
        public Material markerMaterial;
        [Tooltip("Opcional. Vazio = fonte padrão do TextMeshPro.")]
        public TMP_FontAsset font;

        [Header("Cores")]
        public Color arrowColor = new Color(1f, 0.78f, 0.2f);
        public Color visitedColor = new Color(0.35f, 0.9f, 0.6f);
        public Color labelColor = Color.white;
        public Color labelOutlineColor = new Color(0f, 0f, 0f, 0.9f);
        [Range(0f, 0.5f)] public float labelOutlineWidth = 0.25f;

        [Header("Tamanhos no mundo real (metros) — não mudam com o zoom da peça")]
        public float markerSize = 0.018f;
        [Tooltip("Raio da área de toque em volta do marcador (maior = mais fácil de tocar).")]
        public float tapRadius = 0.035f;
        public float lineWidth = 0.0025f;
        public float arrowHeadWidth = 0.012f;
        public float arrowHeadLength = 0.018f;
        [Tooltip("Espaço entre o rótulo e o início da seta.")]
        public float labelGap = 0.015f;
        [Tooltip("Tamanho da fonte do TextMeshPro 3D. 0.3 ≈ letras de 3 cm.")]
        public float labelFontSize = 0.3f;
        public Vector2 labelBoxSize = new Vector2(0.35f, 0.07f);

        [Header("Animação")]
        public bool pulseUnvisited = true;

        // ---- Fallbacks caso os materiais não tenham sido atribuídos ----
        static Material lineFallback, markerFallback;

        public static Material FallbackMaterial(bool forLine)
        {
            if (forLine)
            {
                if (!lineFallback) lineFallback = CreateMaterial("Universal Render Pipeline/Particles/Unlit", "Sprites/Default");
                return lineFallback;
            }
            if (!markerFallback) markerFallback = CreateMaterial("Universal Render Pipeline/Unlit", "Sprites/Default");
            return markerFallback;
        }

        static Material CreateMaterial(params string[] shaderNames)
        {
            foreach (var shaderName in shaderNames)
            {
                var shader = Shader.Find(shaderName);
                if (shader) return new Material(shader);
            }
            Debug.LogWarning("[Museu AR] Nenhum shader encontrado para os hotspots. Atribua materiais no HotspotStyle.");
            return null;
        }
    }
}
