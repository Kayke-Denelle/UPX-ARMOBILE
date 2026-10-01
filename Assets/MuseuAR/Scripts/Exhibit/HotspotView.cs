using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace MuseuAR
{
    /// <summary>
    /// Visual gerado em runtime para cada HotspotPoint:
    ///  • marcador (esfera pulsante) no ponto de interesse
    ///  • seta (LineRenderer com ponta) saindo do rótulo e apontando para o ponto
    ///  • rótulo 3D (TextMeshPro) sempre virado para o visitante
    /// Marcador e rótulo mantêm tamanho constante no mundo, mesmo quando o visitante dá zoom na peça.
    /// </summary>
    public class HotspotView : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        public HotspotPoint Point { get; private set; }
        public ExhibitController Owner { get; internal set; }
        public bool Visited { get; private set; }

        HotspotStyle style;
        LineRenderer line;
        Transform marker;
        Renderer markerRenderer;
        Transform labelRoot;
        TextMeshPro label;
        MaterialPropertyBlock block;
        float pulsePhase;
        float lastHeadFraction = -1f;

        public static HotspotView Create(HotspotPoint point, HotspotStyle style)
        {
            var go = new GameObject($"HotspotView ({point.title})");
            go.transform.SetParent(point.transform, false);
            var view = go.AddComponent<HotspotView>();
            view.Build(point, style);
            return view;
        }

        void Build(HotspotPoint point, HotspotStyle s)
        {
            Point = point;
            style = s ? s : ScriptableObject.CreateInstance<HotspotStyle>();
            pulsePhase = Random.value * Mathf.PI * 2f;
            block = new MaterialPropertyBlock();

            // ---------- Marcador ----------
            var markerGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            markerGO.name = "Marker";
            marker = markerGO.transform;
            marker.SetParent(transform, false);
            markerRenderer = markerGO.GetComponent<Renderer>();
            markerRenderer.sharedMaterial = style.markerMaterial ? style.markerMaterial : HotspotStyle.FallbackMaterial(false);
            markerRenderer.shadowCastingMode = ShadowCastingMode.Off;
            markerRenderer.receiveShadows = false;
            // Área de toque maior que o marcador visível
            var markerCollider = markerGO.GetComponent<SphereCollider>();
            markerCollider.radius = Mathf.Max(0.5f, style.tapRadius / Mathf.Max(0.0001f, style.markerSize));

            // ---------- Seta ----------
            line = gameObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 4;
            line.numCapVertices = 0;
            line.widthMultiplier = 1f;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.sharedMaterial = style.lineMaterial ? style.lineMaterial : HotspotStyle.FallbackMaterial(true);

            // ---------- Rótulo ----------
            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelRoot = labelGO.transform;
            labelRoot.SetParent(transform, false);
            labelRoot.localPosition = point.labelOffset;

            label = labelGO.AddComponent<TextMeshPro>();
            if (style.font) label.font = style.font;
            label.text = point.title;
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta = style.labelBoxSize;
            label.enableAutoSizing = true;
            label.fontSizeMax = style.labelFontSize;
            label.fontSizeMin = style.labelFontSize * 0.5f;
            label.fontSize = style.labelFontSize;
            label.fontStyle = FontStyles.Bold;
            label.outlineWidth = style.labelOutlineWidth;
            label.outlineColor = style.labelOutlineColor;

            var labelCollider = labelGO.AddComponent<BoxCollider>();
            labelCollider.size = new Vector3(style.labelBoxSize.x, style.labelBoxSize.y, 0.01f);

            var billboard = labelGO.AddComponent<Billboard>();
            billboard.lockVertical = false;

            ApplyColors();
        }

        public void MarkVisited()
        {
            if (Visited) return;
            Visited = true;
            ApplyColors();
            if (Owner) Owner.NotifyHotspotVisited(this);
        }

        void ApplyColors()
        {
            Color color = Visited ? style.visitedColor : style.arrowColor;
            line.startColor = color;
            line.endColor = color;

            markerRenderer.GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            block.SetColor(ColorId, color);
            markerRenderer.SetPropertyBlock(block);

            label.color = Visited ? Color.Lerp(style.labelColor, style.visitedColor, 0.6f) : style.labelColor;
        }

        void LateUpdate()
        {
            if (!Point) return;

            // Escala acumulada do modelo (pivot * zoom do visitante)
            float parentScale = Mathf.Max(1e-5f, transform.lossyScale.x);

            float pulse = (!Visited && style.pulseUnvisited)
                ? 1f + 0.18f * Mathf.Sin(Time.time * 4f + pulsePhase)
                : 1f;

            marker.localScale = Vector3.one * (style.markerSize * pulse / parentScale);
            labelRoot.localPosition = Point.labelOffset;
            labelRoot.localScale = Vector3.one / parentScale;

            UpdateArrow(parentScale);
        }

        void UpdateArrow(float parentScale)
        {
            // Tudo em espaço local deste objeto (origem = ponto de interesse)
            Vector3 labelPos = Point.labelOffset;
            float length = labelPos.magnitude;
            if (length < 1e-5f) { line.enabled = false; return; }

            Vector3 dir = -labelPos / length;                       // do rótulo para o ponto
            float gapStart = style.labelGap / parentScale;
            float gapEnd = style.markerSize * 0.75f / parentScale;
            if (gapStart + gapEnd >= length) { line.enabled = false; return; }
            line.enabled = true;

            Vector3 a = labelPos + dir * gapStart;                   // início (perto do rótulo)
            Vector3 b = -dir * gapEnd;                               // fim (encostando no marcador)
            float segment = Vector3.Distance(a, b);

            float head = Mathf.Clamp((style.arrowHeadLength / parentScale) / segment, 0.05f, 0.5f);
            float headStart = 1f - head;

            line.SetPosition(0, a);
            line.SetPosition(1, Vector3.Lerp(a, b, headStart - 0.001f));
            line.SetPosition(2, Vector3.Lerp(a, b, headStart));
            line.SetPosition(3, b);

            // A curva de largura cria a ponta da seta (fina -> larga -> zero)
            if (Mathf.Abs(head - lastHeadFraction) > 0.002f)
            {
                lastHeadFraction = head;
                line.widthCurve = new AnimationCurve(
                    new Keyframe(0f, style.lineWidth),
                    new Keyframe(headStart - 0.001f, style.lineWidth),
                    new Keyframe(headStart, style.arrowHeadWidth),
                    new Keyframe(1f, 0f));
            }
        }
    }
}
