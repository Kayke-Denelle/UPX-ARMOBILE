using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MuseuAR
{
    /// <summary>
    /// Interface principal:
    ///  • Cabeçalho da peça em foco (nome, período, progresso de curiosidades, botões)
    ///  • Painel de detalhes (texto + áudio) aberto ao tocar num hotspot ou em "Sobre a peça"
    ///  • Botões de peças correlacionadas ("Veja também"), que abrem a peça no Modo Acervo
    /// </summary>
    public class InfoPanelUI : MonoBehaviour
    {
        [Header("Cabeçalho da peça")]
        [SerializeField] GameObject headerRoot;
        [SerializeField] TMP_Text itemTitleText;
        [SerializeField] TMP_Text itemSubtitleText;
        [SerializeField] TMP_Text progressText;
        [SerializeField] Button itemInfoButton;
        [SerializeField] Button toggleHotspotsButton;
        [SerializeField] Button resetViewButton;

        [Header("Painel de detalhes")]
        [SerializeField] GameObject detailRoot;
        [SerializeField] TMP_Text detailTitleText;
        [SerializeField] TMP_Text detailKindText;
        [SerializeField] TMP_Text detailBodyText;
        [SerializeField] ScrollRect detailScroll;
        [SerializeField] Button audioButton;
        [SerializeField] TMP_Text audioButtonLabel;
        [Tooltip("Barra de progresso: RectTransform com anchorMin (0,0). O script ajusta anchorMax.x.")]
        [SerializeField] RectTransform audioProgressFill;
        [SerializeField] Button closeButton;

        [Header("Correlacionados")]
        [Tooltip("Título da seção 'Veja também' (fica dentro do conteúdo rolável).")]
        [SerializeField] TMP_Text relatedHeaderText;
        [Tooltip("Botão modelo, no mesmo container do título. Um clone é criado por peça relacionada.")]
        [SerializeField] Button relatedButtonTemplate;

        [Header("Comportamento")]
        [Tooltip("Começa a narração automaticamente ao abrir uma curiosidade.")]
        [SerializeField] bool autoPlayNarration = false;

        /// <summary>Visitante tocou numa peça correlacionada. O AppModeController abre essa peça no Modo Acervo.</summary>
        public static event Action<MuseumItemData> RelatedItemRequested;

        AudioClip currentClip;
        ExhibitController observedExhibit;
        readonly List<Button> relatedButtons = new List<Button>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => RelatedItemRequested = null;

        void Awake()
        {
            if (relatedButtonTemplate) relatedButtonTemplate.gameObject.SetActive(false);
            if (itemInfoButton) itemInfoButton.onClick.AddListener(ShowActiveItemDetails);
            if (toggleHotspotsButton) toggleHotspotsButton.onClick.AddListener(() =>
            {
                if (ExhibitController.Active) ExhibitController.Active.ToggleHotspots();
            });
            if (resetViewButton) resetViewButton.onClick.AddListener(() =>
            {
                if (ExhibitController.Active) ExhibitController.Active.ResetView();
            });
            if (audioButton) audioButton.onClick.AddListener(OnAudioButton);
            if (closeButton) closeButton.onClick.AddListener(CloseDetails);

            SetActive(headerRoot, false);
            SetActive(detailRoot, false);
        }

        void OnEnable()
        {
            ExhibitController.ActiveChanged += OnActiveChanged;
            TouchInputRouter.HotspotTapped += OnHotspotTapped;
            OnActiveChanged(ExhibitController.Active);
        }

        void OnDisable()
        {
            ExhibitController.ActiveChanged -= OnActiveChanged;
            TouchInputRouter.HotspotTapped -= OnHotspotTapped;
            Observe(null);
        }

        // ---------------- Eventos ----------------

        void OnActiveChanged(ExhibitController exhibit)
        {
            Observe(exhibit);
            CloseDetails();

            if (!exhibit)
            {
                SetActive(headerRoot, false);
                return;
            }

            SetActive(headerRoot, true);
            SetText(itemTitleText, exhibit.Data.displayName);
            SetText(itemSubtitleText, exhibit.Data.Subtitle);
            RefreshProgress();
        }

        void OnHotspotTapped(HotspotView hotspot)
        {
            if (!hotspot || !hotspot.Point) return;
            if (hotspot.Owner) hotspot.Owner.Activate();
            hotspot.MarkVisited();

            var point = hotspot.Point;
            var related = point.relatedItem ? new[] { point.relatedItem } : null;
            ShowDetails(point.title, HotspotPoint.KindLabel(point.kind), point.description, point.narration, related);
        }

        void Observe(ExhibitController exhibit)
        {
            if (observedExhibit) observedExhibit.HotspotVisited -= OnHotspotVisited;
            observedExhibit = exhibit;
            if (observedExhibit) observedExhibit.HotspotVisited += OnHotspotVisited;
        }

        void OnHotspotVisited(HotspotView _) => RefreshProgress();

        // ---------------- Painel de detalhes ----------------

        public void ShowActiveItemDetails()
        {
            var exhibit = ExhibitController.Active;
            if (!exhibit) return;

            var data = exhibit.Data;
            ShowDetails(data.displayName, data.Subtitle, data.description, data.introNarration, data.relatedItems);
        }

        void ShowDetails(string title, string kind, string body, AudioClip clip, IEnumerable<MuseumItemData> related)
        {
            if (NarrationPlayer.Instance) NarrationPlayer.Instance.Stop();
            currentClip = clip;

            SetText(detailTitleText, title);
            SetText(detailKindText, kind);
            SetText(detailBodyText, body);
            BuildRelated(related);

            bool hasAudio = clip != null;
            if (audioButton) audioButton.gameObject.SetActive(hasAudio);
            if (audioProgressFill && audioProgressFill.parent) audioProgressFill.parent.gameObject.SetActive(hasAudio);

            SetActive(detailRoot, true);
            if (detailScroll) detailScroll.verticalNormalizedPosition = 1f;

            if (hasAudio && autoPlayNarration && NarrationPlayer.Instance)
                NarrationPlayer.Instance.Play(clip);

            RefreshAudioUI();
        }

        public void CloseDetails()
        {
            SetActive(detailRoot, false);
            if (NarrationPlayer.Instance) NarrationPlayer.Instance.Stop();
            currentClip = null;
        }

        void BuildRelated(IEnumerable<MuseumItemData> related)
        {
            foreach (var button in relatedButtons)
                if (button) Destroy(button.gameObject);
            relatedButtons.Clear();

            var current = ExhibitController.Active ? ExhibitController.Active.Data : null;
            if (related != null && relatedButtonTemplate)
            {
                foreach (var item in related)
                {
                    if (!item || item == current) continue;

                    var button = Instantiate(relatedButtonTemplate, relatedButtonTemplate.transform.parent);
                    button.gameObject.SetActive(true);
                    button.name = $"Relacionada [{item.displayName}]";
                    var label = button.GetComponentInChildren<TMP_Text>(true);
                    if (label) label.text = $"Ver em 3D: <b>{item.displayName}</b>";

                    var target = item;
                    button.onClick.AddListener(() =>
                    {
                        CloseDetails();
                        RelatedItemRequested?.Invoke(target);
                    });
                    relatedButtons.Add(button);
                }
            }

            if (relatedHeaderText) relatedHeaderText.gameObject.SetActive(relatedButtons.Count > 0);
        }

        void OnAudioButton()
        {
            if (!currentClip || !NarrationPlayer.Instance) return;
            NarrationPlayer.Instance.TogglePlayPause(currentClip);
            RefreshAudioUI();
        }

        void Update()
        {
            if (detailRoot && detailRoot.activeSelf && currentClip) RefreshAudioUI();
        }

        void RefreshAudioUI()
        {
            var player = NarrationPlayer.Instance;
            bool isMine = player && currentClip && player.CurrentClip == currentClip;
            bool playing = isMine && player.IsPlaying;

            if (audioButtonLabel) audioButtonLabel.text = playing ? "Pausar áudio" : "Ouvir áudio";

            if (audioProgressFill)
            {
                Vector2 max = audioProgressFill.anchorMax;
                max.x = isMine ? player.Progress : 0f;
                audioProgressFill.anchorMax = max;
            }
        }

        void RefreshProgress()
        {
            if (!progressText) return;
            var exhibit = ExhibitController.Active;
            if (!exhibit || exhibit.Hotspots.Count == 0)
            {
                progressText.text = "";
                return;
            }

            int visited = exhibit.VisitedCount;
            int total = exhibit.Hotspots.Count;
            progressText.text = visited == total
                ? $"Todas as {total} curiosidades descobertas!"
                : $"Curiosidades descobertas: {visited}/{total}";
        }

        static void SetText(TMP_Text text, string value)
        {
            if (text) text.text = value ?? "";
        }

        static void SetActive(GameObject go, bool active)
        {
            if (go) go.SetActive(active);
        }
    }
}
