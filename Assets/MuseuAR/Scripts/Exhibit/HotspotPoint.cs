using UnityEngine;

namespace MuseuAR
{
    public enum HotspotKind { Curiosidade, Detalhe, Tecnica, Historia, Correlacao }

    /// <summary>
    /// Coloque em objetos vazios FILHOS do prefab do modelo, exatamente no ponto que quer destacar.
    /// Em runtime, cada HotspotPoint vira um marcador pulsante + seta + rótulo 3D (ver HotspotView).
    /// No editor, a linha amarela mostra onde o rótulo vai ficar.
    /// </summary>
    [DisallowMultipleComponent]
    public class HotspotPoint : MonoBehaviour
    {
        [Tooltip("Texto curto que flutua no 3D (ideal: até ~25 caracteres).")]
        public string title = "Curiosidade";
        public HotspotKind kind = HotspotKind.Curiosidade;
        [TextArea(4, 12)]
        public string description;
        [Tooltip("Áudio opcional com a narração desta curiosidade.")]
        public AudioClip narration;
        [Tooltip("Posição do rótulo relativa a este ponto (espaço local). A seta sai do rótulo e aponta para o ponto.")]
        public Vector3 labelOffset = new Vector3(0.08f, 0.08f, 0f);
        [Tooltip("Peça relacionada (opcional). Aparece como 'Veja também' no painel.")]
        public MuseumItemData relatedItem;

        public static string KindLabel(HotspotKind kind)
        {
            switch (kind)
            {
                case HotspotKind.Detalhe: return "Detalhe";
                case HotspotKind.Tecnica: return "Técnica";
                case HotspotKind.Historia: return "História";
                case HotspotKind.Correlacao: return "Correlação";
                default: return "Curiosidade";
            }
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Vector3 point = transform.position;
            Vector3 labelPos = transform.TransformPoint(labelOffset);
            float size = UnityEditor.HandleUtility.GetHandleSize(point) * 0.06f;

            Gizmos.color = new Color(1f, 0.78f, 0.2f);
            Gizmos.DrawSphere(point, size);
            Gizmos.DrawLine(labelPos, point);
            Gizmos.DrawWireCube(labelPos, Vector3.one * size * 1.5f);
            UnityEditor.Handles.Label(labelPos, title);
        }
#endif
    }
}
