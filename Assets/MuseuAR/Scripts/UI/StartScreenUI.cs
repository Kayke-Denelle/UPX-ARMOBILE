using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MuseuAR
{
    /// <summary>
    /// Tela inicial: nome do museu, como usar e o botão "Iniciar" (emblema com leve flutuação).
    /// Enquanto ela está aberta, o AppModeController mantém a RA parada.
    /// </summary>
    public class StartScreenUI : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] Button startButton;
        [SerializeField] MuseumCatalog catalog;
        [Tooltip("Recebe o nome do museu (MuseumCatalog.museumName).")]
        [SerializeField] TMP_Text titleText;
        [SerializeField] float fadeDuration = 0.25f;

        [Header("Animação")]
        [Tooltip("Sobe e desce devagar (emblema). 0 = parado.")]
        [SerializeField] RectTransform floatTarget;
        [SerializeField] float floatDistance = 5f;

        Action onStart;
        Vector2 floatBasePosition;

        public bool IsOpen => root && root.activeSelf;

        void Awake()
        {
            if (startButton) startButton.onClick.AddListener(Begin);
            if (titleText && catalog && !string.IsNullOrWhiteSpace(catalog.museumName)) titleText.text = catalog.museumName;
            if (floatTarget) floatBasePosition = floatTarget.anchoredPosition;
        }

        void Update()
        {
            if (!IsOpen || !floatTarget) return;
            floatTarget.anchoredPosition = floatBasePosition + Vector2.up * (Mathf.Sin(Time.unscaledTime * 1.2f) * floatDistance);
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
    }
}
