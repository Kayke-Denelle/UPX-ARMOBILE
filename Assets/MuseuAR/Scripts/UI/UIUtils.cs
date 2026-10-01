using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

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
    }
}
