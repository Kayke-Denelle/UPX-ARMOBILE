using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MuseuAR
{
    public static class UIUtils
    {
        static readonly List<RaycastResult> results = new List<RaycastResult>();

        /// <summary>True se a posição da tela está sobre algum elemento de UI (botão, painel...).</summary>
        public static bool IsPointerOverUI(Vector2 screenPosition)
        {
            var eventSystem = EventSystem.current;
            if (!eventSystem) return false;

            var data = new PointerEventData(eventSystem) { position = screenPosition };
            results.Clear();
            eventSystem.RaycastAll(data, results);
            return results.Count > 0;
        }

        /// <summary>
        /// Ajusta a altura de um painel para "abraçar" o conteúdo do seu ScrollRect, até maxHeight
        /// (a partir dele o conteúdo rola). O painel precisa ter altura fixa (âncoras num ponto)
        /// e o ScrollRect precisa esticar com o painel.
        /// </summary>
        public static void FitPanelToScroll(RectTransform panel, ScrollRect scroll, float maxHeight)
        {
            if (!panel || !scroll || !scroll.content || maxHeight <= 0f) return;

            LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
            var scrollRect = (RectTransform)scroll.transform;
            float chrome = scrollRect.offsetMin.y - scrollRect.offsetMax.y;   // espaço acima e abaixo do conteúdo
            float wanted = chrome + LayoutUtility.GetPreferredHeight(scroll.content);
            panel.sizeDelta = new Vector2(panel.sizeDelta.x, Mathf.Min(wanted, maxHeight));
        }
    }
}
