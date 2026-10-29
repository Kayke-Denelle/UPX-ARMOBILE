using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MuseuAR
{
    /// <summary>
    /// Tela inicial: nome do museu, miniaturas das peças, como usar e o botão "Iniciar".
    /// Enquanto ela está aberta, o AppModeController mantém a RA parada.
    /// </summary>
    public class StartScreenUI : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] Button startButton;
        [SerializeField] MuseumCatalog catalog;
        [Tooltip("Recebe o nome do museu (MuseumCatalog.museumName).")]
        [SerializeField] TMP_Text titleText;
        [Tooltip("Modelo de miniatura: um clone por peça do catálogo. A imagem fica no filho \"Foto\".")]
        [SerializeField] GameObject thumbnailTemplate;
        [SerializeField] int maxThumbnails = 4;
        [SerializeField] float fadeDuration = 0.25f;

        Action onStart;

        public bool IsOpen => root && root.activeSelf;

        void Awake()
        {
            if (startButton) startButton.onClick.AddListener(Begin);
            if (titleText && catalog && !string.IsNullOrWhiteSpace(catalog.museumName)) titleText.text = catalog.museumName;
            BuildThumbnails();
        }

        public void Show(Action onStart)
        {
            this.onStart = onStart;
            if (!root) return;
            root.SetActive(true);
            var group = root.GetComponent<CanvasGroup>();
            if (group) group.alpha = 1f;
        }

        /// <summary>Fecha a tela e começa a experiência (botão Iniciar, ou Enter no PC).</summary>
        public void Begin()
        {
            if (!IsOpen) return;
            var callback = onStart;
            onStart = null;
            callback?.Invoke();
            StartCoroutine(FadeOut());
        }

        IEnumerator FadeOut()
        {
            var group = root.GetComponent<CanvasGroup>();
            if (group && fadeDuration > 0f)
            {
                group.interactable = false;
                for (float t = 0f; t < fadeDuration; t += Time.unscaledDeltaTime)
                {
                    group.alpha = 1f - t / fadeDuration;
                    yield return null;
                }
                group.interactable = true;
            }
            root.SetActive(false);
        }

        void BuildThumbnails()
        {
            if (!thumbnailTemplate) return;
            thumbnailTemplate.SetActive(false);
            if (!catalog) return;

            int count = 0;
            foreach (var item in catalog.items)
            {
                if (!item || !item.thumbnail || count >= maxThumbnails) continue;
                var thumb = Instantiate(thumbnailTemplate, thumbnailTemplate.transform.parent);
                thumb.name = $"Miniatura [{item.displayName}]";
                var photo = thumb.transform.Find("Foto");
                var image = photo ? photo.GetComponent<Image>() : null;
                if (image) image.sprite = item.thumbnail;
                thumb.SetActive(true);
                count++;
            }
        }
    }
}
