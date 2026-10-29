using UnityEngine;
using UnityEngine.InputSystem;

namespace MuseuAR
{
    /// <summary>
    /// MODO PC: permite testar tudo no editor (XR Simulation) sem celular.
    ///  • Teclas 1–9 = "a câmera viu o QR code" da peça 1–9 do catálogo (Modo Visita)
    ///  • Mesa virtual = posiciona peças no Modo Acervo mesmo sem superfície detectada
    ///  • Atalhos de teclado e painel de ajuda na tela (H)
    /// Desliga sozinho no celular.
    /// </summary>
    public class DesktopSimulator : MonoBehaviour
    {
        [SerializeField] ImageTrackingSpawner imageSpawner;
        [SerializeField] AppModeController modes;
        [SerializeField] InfoPanelUI infoPanel;
        [SerializeField] Camera arCamera;
        [SerializeField] bool showHelpOnStart = false;
        [Tooltip("Graus por segundo ao girar a peça com as setas do teclado.")]
        [SerializeField] float keyRotateSpeed = 90f;

        bool showHelp;
        string pendingHint;
        GUIStyle boxStyle, textStyle;

        void Awake()
        {
            if (Application.isMobilePlatform && !Application.isEditor)
            {
                enabled = false;
                return;
            }
            showHelp = showHelpOnStart;
            FreePlacementController.UseVirtualTable = true;
        }

        void OnEnable()
        {
            if (modes) modes.ModeChanged += OnModeChanged;
            FreePlacementController.ItemSelected += OnItemSelected;
            ImageTrackingSpawner.ItemRecognized += ClearPendingHint;
        }

        void OnDisable()
        {
            if (modes) modes.ModeChanged -= OnModeChanged;
            FreePlacementController.ItemSelected -= OnItemSelected;
            ImageTrackingSpawner.ItemRecognized -= ClearPendingHint;
            FreePlacementController.UseVirtualTable = false;
        }

        // As dicas padrão falam de "celular"; no PC trocamos pelo atalho certo.
        void OnModeChanged(ARMode mode)
        {
            pendingHint = mode == ARMode.Visita
                ? "PC: aperte 1, 2, 3... para simular o QR code de uma peça"
                : "PC: escolha uma peça no menu";
        }

        void OnItemSelected(MuseumItemData item) =>
            pendingHint = $"PC: clique no chão/mesa à sua frente para posicionar: {item.displayName}";

        void ClearPendingHint(MuseumItemData _) => pendingHint = null;

        void LateUpdate()
        {
            if (pendingHint != null && modes) modes.ShowHint(pendingHint);
            pendingHint = null;
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (Mouse.current != null && Mouse.current.rightButton.isPressed) return; // navegando

            if (keyboard.hKey.wasPressedThisFrame || keyboard.f1Key.wasPressedThisFrame) showHelp = !showHelp;

            // Tela inicial aberta: só Enter/Espaço (= botão Iniciar)
            if (modes && !modes.Started)
            {
                if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
                    modes.RequestStart();
                return;
            }

            for (int i = 0; i < 9; i++)
            {
                if (keyboard[Key.Digit1 + i].wasPressedThisFrame || keyboard[Key.Numpad1 + i].wasPressedThisFrame)
                    SimulatePlate(i);
            }

            if (keyboard.mKey.wasPressedThisFrame && modes)
                modes.SetMode(modes.Mode == ARMode.Visita ? ARMode.Acervo : ARMode.Visita);

            if (keyboard.iKey.wasPressedThisFrame && infoPanel) infoPanel.ShowActiveItemDetails();
            if (keyboard.escapeKey.wasPressedThisFrame && infoPanel) infoPanel.CloseDetails();

            var active = ExhibitController.Active;
            if (!active) return;

            if (keyboard.rKey.wasPressedThisFrame) active.ResetView();
            if (keyboard.pKey.wasPressedThisFrame) active.ToggleHotspots();

            float rotate = 0f;
            if (keyboard.leftArrowKey.isPressed) rotate += 1f;
            if (keyboard.rightArrowKey.isPressed) rotate -= 1f;
            if (rotate != 0f) active.RotateBy(rotate * keyRotateSpeed * Time.deltaTime);

            if (keyboard.equalsKey.wasPressedThisFrame || keyboard.numpadPlusKey.wasPressedThisFrame) active.ScaleBy(1.15f);
            if (keyboard.minusKey.wasPressedThisFrame || keyboard.numpadMinusKey.wasPressedThisFrame) active.ScaleBy(1f / 1.15f);
        }

        void SimulatePlate(int index)
        {
            var catalog = imageSpawner ? imageSpawner.Catalog : null;
            if (!catalog || index >= catalog.items.Count || !catalog.items[index]) return;

            if (modes && modes.Mode != ARMode.Visita) modes.SetMode(ARMode.Visita);
            imageSpawner.SimulateRecognition(catalog.items[index], arCamera ? arCamera : Camera.main);
        }

        // ---------------- Painel de ajuda ----------------

        void OnGUI()
        {
            float scale = Mathf.Max(1f, Screen.height / 900f);
            if (boxStyle == null)
            {
                boxStyle = new GUIStyle(GUI.skin.box) { normal = { background = Texture2D.whiteTexture } };
                textStyle = new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true };
            }
            textStyle.fontSize = Mathf.RoundToInt(13 * scale);

            if (!showHelp)
            {
                GUI.Label(new Rect(8 * scale, Screen.height - 28 * scale, 400 * scale, 24 * scale),
                    "<color=#FFD24D><b>[H] Ajuda do modo PC</b></color>", textStyle);
                return;
            }

            string catalogLines = "";
            var catalog = imageSpawner ? imageSpawner.Catalog : null;
            if (catalog)
                for (int i = 0; i < catalog.items.Count && i < 9; i++)
                    if (catalog.items[i]) catalogLines += $"\n  <b>{i + 1}</b> = QR da {catalog.items[i].displayName}";

            string text =
                "<color=#FFD24D><b>MODO PC  (H fecha)</b></color>\n" +
                "<b>Enter</b> = botão Iniciar (tela inicial)\n" +
                "<b>Andar:</b> segure o <b>botão DIREITO</b> do mouse\n" +
                "  + W A S D (Q desce, E sobe, Shift corre)\n" +
                "  e mova o mouse para olhar\n\n" +
                "<b>Modo Visita</b> (simular o QR code):" + catalogLines + "\n\n" +
                "<b>Clique</b> = tocar (pontos, botões, mesa)\n" +
                "<b>Arrastar (esquerdo)</b> ou <b>← →</b> = girar peça\n" +
                "<b>Roda do mouse</b> ou <b>+ −</b> = zoom\n" +
                "<b>I</b> = sobre a peça   <b>Esc</b> = fechar painel\n" +
                "<b>P</b> = mostrar/ocultar pontos   <b>R</b> = resetar\n" +
                "<b>M</b> = trocar Modo Visita / Acervo\n\n" +
                "<i>Dica: na Game view, use resolução 1080x1920 (retrato).</i>";

            float width = 330 * scale;
            float height = textStyle.CalcHeight(new GUIContent(text), width - 20 * scale) + 20 * scale;
            var rect = new Rect(8 * scale, Screen.height * 0.5f - height * 0.5f, width, height);

            var previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.8f);
            GUI.Box(rect, GUIContent.none, boxStyle);
            GUI.color = previous;
            GUI.Label(new Rect(rect.x + 10 * scale, rect.y + 10 * scale, width - 20 * scale, height - 20 * scale), text, textStyle);
        }
    }
}
