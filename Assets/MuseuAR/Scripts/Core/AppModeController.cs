using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

namespace MuseuAR
{
    public enum ARMode
    {
        /// <summary>Aponta para a placa AR da vitrine e a peça aparece.</summary>
        Visita,
        /// <summary>Escolhe uma peça do catálogo e posiciona numa mesa/chão.</summary>
        Acervo
    }

    /// <summary>Alterna entre os modos e mostra as dicas de uso na tela.</summary>
    public class AppModeController : MonoBehaviour
    {
        [Header("AR")]
        [SerializeField] ARTrackedImageManager imageManager;
        [SerializeField] ARPlaneManager planeManager;
        [SerializeField] ImageTrackingSpawner imageSpawner;
        [SerializeField] FreePlacementController freePlacement;

        [Header("UI")]
        [SerializeField] CatalogMenuUI catalogMenu;
        [SerializeField] GameObject hintRoot;
        [SerializeField] TMP_Text hintText;
        [SerializeField] Button visitModeButton;
        [SerializeField] Button collectionModeButton;
        [SerializeField] Color selectedButtonColor = new Color(0.95f, 0.62f, 0.1f, 1f);
        [SerializeField] Color normalButtonColor = new Color(0.18f, 0.18f, 0.22f, 0.95f);

        [Header("Início")]
        [SerializeField] ARMode startMode = ARMode.Visita;
        [Tooltip("Opcional. Com tela inicial, a experiência só começa ao tocar em \"Iniciar\".")]
        [SerializeField] StartScreenUI startScreen;

        public ARMode Mode { get; private set; }
        public event System.Action<ARMode> ModeChanged;

        void Awake()
        {
            if (visitModeButton) visitModeButton.onClick.AddListener(() => SetMode(ARMode.Visita));
            if (collectionModeButton) collectionModeButton.onClick.AddListener(() => SetMode(ARMode.Acervo));
        }

        void OnEnable()
        {
            ImageTrackingSpawner.ItemRecognized += OnItemRecognized;
            FreePlacementController.ItemSelected += OnItemSelected;
            FreePlacementController.ItemPlaced += OnItemPlaced;
            InfoPanelUI.RelatedItemRequested += OpenInCollection;
        }

        void OnDisable()
        {
            ImageTrackingSpawner.ItemRecognized -= OnItemRecognized;
            FreePlacementController.ItemSelected -= OnItemSelected;
            FreePlacementController.ItemPlaced -= OnItemPlaced;
            InfoPanelUI.RelatedItemRequested -= OpenInCollection;
        }

        /// <summary>A experiência já começou (o visitante passou da tela inicial).</summary>
        public bool Started { get; private set; }

        void Start()
        {
            if (startScreen)
            {
                PauseUntilStart();
                startScreen.Show(Begin);
            }
            else Begin();
        }

        /// <summary>Começa a experiência no modo inicial. Chamado pelo botão "Iniciar".</summary>
        public void Begin()
        {
            if (Started) return;
            Started = true;
            SetMode(startMode);
        }

        /// <summary>Equivale a tocar em "Iniciar" (usado pelo atalho Enter no PC).</summary>
        public void RequestStart()
        {
            if (startScreen && startScreen.IsOpen) startScreen.Begin();
            else Begin();
        }

        // Com a tela inicial aberta, nenhuma peça aparece por trás dela. O rastreamento de imagens
        // continua ligado (desligar no primeiro frame dessincroniza o AR Foundation); as peças só ficam ocultas.
        void PauseUntilStart()
        {
            if (imageSpawner) imageSpawner.SetExhibitsVisible(false);
            if (planeManager) planeManager.enabled = false;
            if (freePlacement) freePlacement.enabled = false;
            if (catalogMenu) catalogMenu.Hide();
            HideHint();
        }

        public void SetVisitMode() => SetMode(ARMode.Visita);
        public void SetCollectionMode() => SetMode(ARMode.Acervo);

        public void SetMode(ARMode mode)
        {
            Mode = mode;
            bool visit = mode == ARMode.Visita;

            if (NarrationPlayer.Instance) NarrationPlayer.Instance.Stop();
            ExhibitController.ClearActive();

            // Modo Visita: rastreamento de imagens
            if (imageManager) imageManager.enabled = visit;
            if (imageSpawner) imageSpawner.SetExhibitsVisible(visit);

            // Modo Acervo: detecção de planos + posicionamento livre
            if (planeManager)
            {
                planeManager.enabled = !visit;
                foreach (var plane in planeManager.trackables)
                    plane.gameObject.SetActive(!visit);
            }
            if (freePlacement)
            {
                freePlacement.enabled = !visit;
                if (visit) freePlacement.ResetSelection();
            }

            if (catalogMenu)
            {
                if (visit) catalogMenu.Hide();
                else catalogMenu.Show();
            }

            ShowHint(visit
                ? "Aponte a câmera para o QR code ao lado da peça"
                : "Escolha uma peça do acervo");

            Tint(visitModeButton, visit);
            Tint(collectionModeButton, !visit);
            ModeChanged?.Invoke(mode);
        }

        /// <summary>Abre uma peça direto no Modo Acervo, pronta para ser posicionada (usado pelos correlacionados).</summary>
        public void OpenInCollection(MuseumItemData item)
        {
            if (!item) return;
            if (Mode != ARMode.Acervo) SetMode(ARMode.Acervo);
            if (catalogMenu) catalogMenu.Hide();
            if (freePlacement) freePlacement.SelectItem(item);
        }

        public void ShowHint(string message)
        {
            if (hintText) hintText.text = message ?? "";
            if (hintRoot) hintRoot.SetActive(!string.IsNullOrEmpty(message));
        }

        public void HideHint() => ShowHint(null);

        void OnItemRecognized(MuseumItemData _)
        {
            if (Mode == ARMode.Visita) HideHint();
        }

        void OnItemSelected(MuseumItemData item)
        {
            ShowHint($"Mova o celular devagar e toque numa superfície plana para posicionar: {item.displayName}");
        }

        void OnItemPlaced(MuseumItemData _) => HideHint();

        void Tint(Button button, bool selected)
        {
            if (button && button.targetGraphic)
                button.targetGraphic.color = selected ? selectedButtonColor : normalButtonColor;
        }
    }
}
