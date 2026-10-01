using System.Collections.Generic;
using UnityEngine;

namespace MuseuAR
{
    /// <summary>
    /// Ficha de uma peça do acervo.
    /// Criar em: Assets > Create > Museu AR > Peça do Acervo
    /// </summary>
    [CreateAssetMenu(fileName = "NovaPeca", menuName = "Museu AR/Peça do Acervo", order = 1)]
    public class MuseumItemData : ScriptableObject
    {
        [Header("Identificação")]
        [Tooltip("Identificador único, sem espaços. Ex.: anfora-romana-01")]
        public string id;
        public string displayName = "Nome da peça";
        [Tooltip("Ex.: Século II d.C.")]
        public string period;
        [Tooltip("Ex.: Península Itálica")]
        public string origin;
        [Tooltip("Ex.: Cerâmica, bronze, madeira...")]
        public string material;
        [TextArea(4, 12)]
        public string description;

        [Header("Realidade Aumentada")]
        [Tooltip("Nome EXATO da imagem na Reference Image Library (a placa AR ao lado da vitrine).")]
        public string referenceImageName;
        [Tooltip("Prefab do modelo 3D. Pontos de interesse = objetos filhos com o componente HotspotPoint.")]
        public GameObject modelPrefab;
        [Min(0.0001f)]
        [Tooltip("Escala inicial do modelo. Use para corrigir modelos importados em cm (0.01) ou reduzir peças grandes.")]
        public float initialScale = 1f;
        [Tooltip("Deslocamento do modelo em relação à placa/superfície, em metros (Y = para cima).")]
        public Vector3 spawnOffset = new Vector3(0f, 0.05f, 0f);
        [Tooltip("Rotação inicial do modelo (graus). Útil se o modelo foi exportado 'deitado' ou de costas.")]
        public Vector3 spawnRotation;

        [Header("Mídia")]
        [Tooltip("Áudio de apresentação da peça (botão 'Sobre a peça').")]
        public AudioClip introNarration;
        public Sprite thumbnail;

        [Header("Conteúdo correlacionado")]
        [Tooltip("Peças relacionadas: mesmo período, mesma técnica, mesma coleção, mesmo artista...")]
        public List<MuseumItemData> relatedItems = new List<MuseumItemData>();

        /// <summary>"Período · Origem · Material" (ignora campos vazios).</summary>
        public string Subtitle
        {
            get
            {
                var parts = new List<string>(3);
                if (!string.IsNullOrWhiteSpace(period)) parts.Add(period);
                if (!string.IsNullOrWhiteSpace(origin)) parts.Add(origin);
                if (!string.IsNullOrWhiteSpace(material)) parts.Add(material);
                return string.Join("  ·  ", parts);
            }
        }

        void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id)) id = name;
        }
    }
}
