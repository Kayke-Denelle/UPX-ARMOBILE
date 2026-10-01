using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MuseuAR
{
    /// <summary>
    /// Lista de peças do catálogo (Modo Acervo). Gera um botão por peça a partir de um "template".
    /// Se o template tiver um filho chamado "Thumbnail" com Image, ele recebe a miniatura da peça.
    /// </summary>
    public class CatalogMenuUI : MonoBehaviour
    {
        [SerializeField] MuseumCatalog catalog;
        [SerializeField] FreePlacementController placement;
        [SerializeField] GameObject menuRoot;
        [SerializeField] TMP_Text titleText;
        [SerializeField] Transform listContainer;
        [SerializeField] Button itemButtonTemplate;
        [SerializeField] Button closeButton;

        bool built;

        public bool IsOpen => menuRoot && menuRoot.activeSelf;

        void Awake()
        {
            if (itemButtonTemplate) itemButtonTemplate.gameObject.SetActive(false);
            if (closeButton) closeButton.onClick.AddListener(Hide);
        }

        public void Show()
        {
            if (!built) Build();
            if (menuRoot) menuRoot.SetActive(true);
        }

        public void Hide()
        {
            if (menuRoot) menuRoot.SetActive(false);
        }

        void Build()
        {
            built = true;
            if (!catalog || !itemButtonTemplate || !listContainer)
            {
                Debug.LogWarning("[Museu AR] CatalogMenuUI: faltam referências (catalog, template ou container).");
                return;
            }

            if (titleText) titleText.text = $"Acervo digital — {catalog.museumName}";

            foreach (var item in catalog.items)
            {
                if (!item) continue;

                var button = Instantiate(itemButtonTemplate, listContainer);
                button.gameObject.SetActive(true);
                button.name = $"Item [{item.displayName}]";

                var label = button.GetComponentInChildren<TMP_Text>(true);
                if (label)
                {
                    string subtitle = item.Subtitle;
                    label.text = string.IsNullOrEmpty(subtitle)
                        ? $"<b>{item.displayName}</b>"
                        : $"<b>{item.displayName}</b>\n<size=75%>{subtitle}</size>";
                }

                var thumbTransform = button.transform.Find("Thumbnail");
                var thumb = thumbTransform ? thumbTransform.GetComponent<Image>() : null;
                if (thumb)
                {
                    thumb.sprite = item.thumbnail;
                    thumb.enabled = item.thumbnail != null;
                }

                var selected = item;
                button.onClick.AddListener(() =>
                {
                    if (placement) placement.SelectItem(selected);
                    Hide();
                });
            }
        }
    }
}
