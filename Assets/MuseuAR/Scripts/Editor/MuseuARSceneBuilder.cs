using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace MuseuAR.EditorTools
{
    /// <summary>
    /// Menu "Museu AR > Montar cena base": cria os sistemas, a interface e os assets iniciais,
    /// já com todas as referências ligadas. Rode UMA vez numa cena que já tenha
    /// AR Session + XR Origin (Mobile AR).
    /// </summary>
    public static class MuseuARSceneBuilder
    {
        const string DataFolder = "Assets/MuseuAR/Data";

        static readonly Color PanelColor = new Color(0.07f, 0.07f, 0.09f, 0.9f);
        static readonly Color ButtonColor = new Color(0.18f, 0.18f, 0.22f, 0.95f);
        static readonly Color AccentColor = new Color(0.95f, 0.62f, 0.1f, 1f);
        static readonly Color MutedText = new Color(0.8f, 0.8f, 0.85f, 1f);

        public const string MuseumScenePath = "Assets/MuseuAR/Scenes/MuseuAR.unity";
        const string TemplateScenePath = "Assets/Scenes/SampleScene.unity";

        // Objetos do template Mobile AR que conflitam com o app (criam cubos ao tocar, menu próprio etc.)
        static readonly string[] TemplateObjectsToRemove = { "Object Spawner", "Screen Space Ray Interactor" };
        const string TemplateMenuComponent = "ARTemplateMenuManager";

        [MenuItem("Museu AR/1. Criar cena do museu (a partir do template)", priority = -10)]
        static void CreateMuseumSceneMenu() => CreateMuseumScene(true);

        /// <summary>
        /// Copia a SampleScene do template (que já tem AR Session + XR Origin configurados),
        /// remove o que é só do template, monta o sistema do museu e coloca a cena em primeiro no Build.
        /// </summary>
        public static bool CreateMuseumScene(bool interactive)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MuseumScenePath))
            {
                if (interactive && EditorUtility.DisplayDialog("Museu AR", $"A cena {MuseumScenePath} já existe. Abrir?", "Abrir", "Cancelar"))
                    EditorSceneManager.OpenScene(MuseumScenePath);
                return false;
            }
            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(TemplateScenePath))
            {
                Notify(interactive, $"Não encontrei {TemplateScenePath}. Crie uma cena com AR Session + XR Origin (Mobile AR) e use 'Montar cena base'.");
                return false;
            }
            if (interactive && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;

            EnsureFolder("Assets/MuseuAR/Scenes");
            AssetDatabase.CopyAsset(TemplateScenePath, MuseumScenePath);
            var scene = EditorSceneManager.OpenScene(MuseumScenePath);

            foreach (var root in scene.GetRootGameObjects())
            {
                if (HasComponentNamed(root, TemplateMenuComponent))
                {
                    Object.DestroyImmediate(root);
                    continue;
                }
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t && System.Array.IndexOf(TemplateObjectsToRemove, t.name) >= 0)
                        Object.DestroyImmediate(t.gameObject);
            }

            if (!Build(interactive: false)) return false;
            EditorSceneManager.SaveScene(scene);

            // Cena do museu em primeiro na lista de Build
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == MuseumScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(MuseumScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();

            Notify(interactive, "Cena do museu criada em " + MuseumScenePath + "\n\n" +
                "Próximo passo: cadastre as peças (veja docs/documents/guia-de-conteudo.md).");
            return true;
        }

        static bool HasComponentNamed(GameObject go, string typeName)
        {
            foreach (var c in go.GetComponents<Component>())
                if (c && c.GetType().Name == typeName) return true;
            return false;
        }

        static void Notify(bool interactive, string message)
        {
            if (interactive) EditorUtility.DisplayDialog("Museu AR", message, "OK");
            else Debug.Log("[Museu AR] " + message);
        }

        [MenuItem("Museu AR/Montar cena base (na cena aberta)", priority = 50)]
        public static void BuildScene() => Build(interactive: true);

        static bool Build(bool interactive)
        {
            var origin = Object.FindAnyObjectByType<XROrigin>();
            if (!origin)
            {
                Notify(interactive,
                    "Nenhum XR Origin na cena.\n\nAntes, adicione:\n• GameObject > XR > AR Session\n• GameObject > XR > XR Origin (Mobile AR)");
                return false;
            }
            if (!Object.FindAnyObjectByType<ARSession>())
                Debug.LogWarning("[Museu AR] Não há AR Session na cena. Adicione via GameObject > XR > AR Session.");

            if (Object.FindAnyObjectByType<AppModeController>() &&
                (!interactive || !EditorUtility.DisplayDialog("Museu AR", "Já existe um AppModeController na cena. Criar outro mesmo assim?", "Criar", "Cancelar")))
                return false;

            // ---------- Assets ----------
            EnsureFolder(DataFolder);
            var style = LoadOrCreateStyle();
            var catalog = LoadOrCreate<MuseumCatalog>($"{DataFolder}/CatalogoDoMuseu.asset");
            var library = LoadOrCreate<XRReferenceImageLibrary>($"{DataFolder}/PlacasAR.asset");

            // ---------- Managers AR no XR Origin ----------
            var originGO = origin.gameObject;
            var imageManager = GetOrAdd<ARTrackedImageManager>(originGO);
            if (imageManager.referenceLibrary == null) imageManager.referenceLibrary = library;
            EditorUtility.SetDirty(imageManager);

            var planeManager = GetOrAdd<ARPlaneManager>(originGO);
            planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
            EditorUtility.SetDirty(planeManager);

            var raycastManager = GetOrAdd<ARRaycastManager>(originGO);
            GetOrAdd<ARAnchorManager>(originGO);
            var arCamera = origin.Camera;

            // ---------- Sistemas ----------
            var systems = new GameObject("MuseuAR_Systems");
            Undo.RegisterCreatedObjectUndo(systems, "Museu AR");
            var spawner = systems.AddComponent<ImageTrackingSpawner>();
            var placement = systems.AddComponent<FreePlacementController>();
            var input = systems.AddComponent<TouchInputRouter>();
            var modes = systems.AddComponent<AppModeController>();
            systems.AddComponent<NarrationPlayer>();

            Wire(spawner, "imageManager", imageManager);
            Wire(spawner, "catalog", catalog);
            Wire(spawner, "hotspotStyle", style);
            Wire(placement, "raycastManager", raycastManager);
            Wire(placement, "hotspotStyle", style);
            Wire(placement, "arCamera", arCamera);
            Wire(input, "arCamera", arCamera);
            Wire(modes, "imageManager", imageManager);
            Wire(modes, "planeManager", planeManager);
            Wire(modes, "imageSpawner", spawner);

            EnsureEventSystem();

            var desktop = systems.AddComponent<DesktopSimulator>();
            Wire(desktop, "imageSpawner", spawner);
            Wire(desktop, "modes", modes);
            Wire(desktop, "arCamera", arCamera);

            BuildUI(catalog, placement, modes, desktop);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(systems.scene);
            Selection.activeGameObject = systems;

            if (interactive)
                EditorUtility.DisplayDialog("Museu AR", "Cena base criada!\n\nPróximos passos:\n" +
                    "1. Adicione imagens (placas) em Assets/MuseuAR/Data/PlacasAR\n" +
                    "2. Crie as peças (Create > Museu AR > Peça do Acervo)\n" +
                    "3. Adicione as peças no CatalogoDoMuseu\n" +
                    "4. (Opcional) Plane Prefab no ARPlaneManager: GameObject > XR > AR Default Plane", "OK");
            return true;
        }

        [MenuItem("Museu AR/Reconstruir interface (na cena aberta)", priority = 51)]
        static void RebuildUIMenu()
        {
            if (RebuildUI())
                EditorUtility.DisplayDialog("Museu AR", "Interface reconstruída. Salve a cena (Ctrl+S).", "OK");
        }

        /// <summary>
        /// Apaga o Canvas "MuseuAR_UI" da cena aberta e cria de novo com o layout atual,
        /// religando AppModeController e DesktopSimulator. Use depois de mudar o layout neste arquivo.
        /// </summary>
        public static bool RebuildUI()
        {
            var modes = Object.FindAnyObjectByType<AppModeController>();
            var placement = Object.FindAnyObjectByType<FreePlacementController>();
            if (!modes || !placement)
            {
                Notify(true, "Abra a cena do museu (com MuseuAR_Systems) antes de reconstruir a interface.");
                return false;
            }

            var old = GameObject.Find("MuseuAR_UI");
            if (old) Undo.DestroyObjectImmediate(old);

            var catalog = AssetDatabase.LoadAssetAtPath<MuseumCatalog>($"{DataFolder}/CatalogoDoMuseu.asset");
            BuildUI(catalog, placement, modes, Object.FindAnyObjectByType<DesktopSimulator>());
            EditorSceneManager.MarkSceneDirty(modes.gameObject.scene);
            return true;
        }

        // ================= Interface =================
        // Layout em unidades de referência (tela de 1080 x 1920 em pé). Os painéis têm tamanho fixo
        // e ficam presos às bordas: o centro da tela fica livre para a peça em AR.

        const float Margin = 24f;
        const float CardWidth = 1032f;
        const float ModeBarHeight = 88f;
        const float SheetBottom = Margin + ModeBarHeight + 16f;   // painéis de baixo ficam acima da barra de modos

        static void BuildUI(MuseumCatalog catalog, FreePlacementController placement, AppModeController modes, DesktopSimulator desktop)
        {
            rounded = LoadOrCreateRoundedSprite();

            var canvasGO = new GameObject("MuseuAR_UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasGO, "Museu AR");
            canvasGO.layer = 5;
            canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            // Expand: a área de 1080x1920 sempre cabe inteira na tela (em pé, deitado ou tablet)
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            var safe = NewRect("SafeArea", canvasGO.transform);
            Region(safe, 0, 0, 1, 1, 0, 0);
            safe.gameObject.AddComponent<SafeAreaFitter>();

            // ---------- Cabeçalho (topo) ----------
            var header = Panel("CabecalhoPeca", safe, PanelColor, 28);
            Place(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -Margin), new Vector2(CardWidth, 196));
            var title = Label("Titulo", header.transform, "Nome da peça", 40, FontStyles.Bold);
            StretchTop(title.rectTransform, 18, 54, 32, 32);
            title.overflowMode = TextOverflowModes.Ellipsis;
            var subtitle = Label("Subtitulo", header.transform, "Período · Origem · Material", 26);
            subtitle.color = MutedText;
            subtitle.overflowMode = TextOverflowModes.Ellipsis;
            StretchTop(subtitle.rectTransform, 72, 36, 32, 32);
            var infoBtn = MakeButton("BtnSobre", header.transform, "Sobre a peça", ButtonColor, 24, 28);
            Place((RectTransform)infoBtn.transform, new Vector2(0f, 1f), new Vector2(24, -124), new Vector2(200, 56));
            var pointsBtn = MakeButton("BtnPontos", header.transform, "Pontos", ButtonColor, 24, 28);
            Place((RectTransform)pointsBtn.transform, new Vector2(0f, 1f), new Vector2(236, -124), new Vector2(140, 56));
            var resetBtn = MakeButton("BtnResetar", header.transform, "Resetar", ButtonColor, 24, 28);
            Place((RectTransform)resetBtn.transform, new Vector2(0f, 1f), new Vector2(388, -124), new Vector2(140, 56));
            var progress = Label("Progresso", header.transform, "Descobertas: 0/0", 24, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
            progress.color = AccentColor;
            Place(progress.rectTransform, new Vector2(1f, 1f), new Vector2(-32, -124), new Vector2(440, 56));

            // ---------- Painel de detalhes (embaixo) ----------
            var detail = Panel("PainelDetalhes", safe, PanelColor, 28);
            Place(detail.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, SheetBottom), new Vector2(CardWidth, 600));
            var dKind = Label("Categoria", detail.transform, "Curiosidade", 22, FontStyles.Bold | FontStyles.UpperCase);
            dKind.color = AccentColor;
            dKind.overflowMode = TextOverflowModes.Ellipsis;
            StretchTop(dKind.rectTransform, 24, 32, 32, 120);
            var dTitle = Label("Titulo", detail.transform, "Título da curiosidade", 36, FontStyles.Bold);
            dTitle.overflowMode = TextOverflowModes.Ellipsis;
            StretchTop(dTitle.rectTransform, 56, 52, 32, 120);
            var closeBtn = MakeButton("BtnFechar", detail.transform, "×", ButtonColor, 44, 36);
            Place((RectTransform)closeBtn.transform, new Vector2(1f, 1f), new Vector2(-20, -20), new Vector2(72, 72));
            var dScroll = MakeScroll(detail.transform, out var dContent, 8);
            var dScrollRect = (RectTransform)dScroll.transform;
            dScrollRect.anchorMin = Vector2.zero;
            dScrollRect.anchorMax = Vector2.one;
            dScrollRect.offsetMin = new Vector2(16, 104);   // espaço para o botão de áudio
            dScrollRect.offsetMax = new Vector2(-16, -116);
            var dBody = Label("Corpo", dContent, "Texto da curiosidade...", 28, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            dBody.lineSpacing = 8;
            var relatedHeader = Label("VejaTambem", dContent, "Veja também", 24, FontStyles.Bold | FontStyles.UpperCase);
            relatedHeader.color = AccentColor;
            relatedHeader.margin = new Vector4(0, 16, 0, 0);
            var relatedTemplate = MakeButton("RelacionadaTemplate", dContent, "Ver em 3D", ButtonColor, 26, 32);
            var relatedLayout = relatedTemplate.gameObject.AddComponent<LayoutElement>();
            relatedLayout.minHeight = 72;
            relatedLayout.preferredHeight = 72;
            var audioBtn = MakeButton("BtnAudio", detail.transform, "Ouvir áudio", AccentColor, 24, 32);
            audioBtn.GetComponentInChildren<TextMeshProUGUI>().color = new Color(0.1f, 0.08f, 0.05f);
            Place((RectTransform)audioBtn.transform, new Vector2(0f, 0f), new Vector2(24, 24), new Vector2(240, 64));
            var barBg = Panel("BarraAudio", detail.transform, new Color(1, 1, 1, 0.15f), 4);
            barBg.raycastTarget = false;
            barBg.rectTransform.anchorMin = new Vector2(0f, 0f);
            barBg.rectTransform.anchorMax = new Vector2(1f, 0f);
            barBg.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            barBg.rectTransform.offsetMin = new Vector2(288, 52);
            barBg.rectTransform.offsetMax = new Vector2(-32, 60);
            var barFill = Panel("Preenchimento", barBg.transform, AccentColor, 4);
            barFill.raycastTarget = false;
            barFill.rectTransform.anchorMin = Vector2.zero;
            barFill.rectTransform.anchorMax = new Vector2(0f, 1f);
            barFill.rectTransform.offsetMin = Vector2.zero;
            barFill.rectTransform.offsetMax = Vector2.zero;

            // ---------- Menu do acervo (embaixo) ----------
            var catalogPanel = Panel("MenuAcervo", safe, PanelColor, 28);
            Place(catalogPanel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, SheetBottom), new Vector2(CardWidth, 760));
            var cTitle = Label("Titulo", catalogPanel.transform, "Acervo digital", 34, FontStyles.Bold);
            cTitle.overflowMode = TextOverflowModes.Ellipsis;
            StretchTop(cTitle.rectTransform, 28, 52, 32, 120);
            var cClose = MakeButton("BtnFechar", catalogPanel.transform, "×", ButtonColor, 44, 36);
            Place((RectTransform)cClose.transform, new Vector2(1f, 1f), new Vector2(-20, -20), new Vector2(72, 72));
            var cScroll = MakeScroll(catalogPanel.transform, out var cContent, 12);
            Region((RectTransform)cScroll.transform, 0, 0, 1, 1, 8, 0);
            ((RectTransform)cScroll.transform).offsetMin = new Vector2(8, 16);
            ((RectTransform)cScroll.transform).offsetMax = new Vector2(-8, -100);
            var template = MakeButton("ItemTemplate", cContent, "Peça", ButtonColor, 28, 20);
            var templateLayout = template.gameObject.AddComponent<LayoutElement>();
            templateLayout.minHeight = 120;
            templateLayout.preferredHeight = 120;
            var thumb = Panel("Thumbnail", template.transform, Color.white);
            thumb.raycastTarget = false;
            thumb.preserveAspect = true;
            Place(thumb.rectTransform, new Vector2(0f, 0.5f), new Vector2(12, 0), new Vector2(96, 96));
            var templateLabel = template.GetComponentInChildren<TextMeshProUGUI>();
            templateLabel.alignment = TextAlignmentOptions.MidlineLeft;
            templateLabel.fontStyle = FontStyles.Normal;
            templateLabel.rectTransform.offsetMin = new Vector2(128f, 6f);

            // ---------- Dica (flutua abaixo da área do cabeçalho) ----------
            var hint = Panel("Dica", safe, new Color(0f, 0f, 0f, 0.7f), 40);
            hint.raycastTarget = false;
            Place(hint.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -244), new Vector2(920, 96));
            var hintText = Label("Texto", hint.transform, "Aponte a câmera para o QR code", 26, FontStyles.Normal, TextAlignmentOptions.Center);
            hintText.enableAutoSizing = true;
            hintText.fontSizeMin = 18;
            hintText.fontSizeMax = 26;
            Region(hintText.rectTransform, 0, 0, 1, 1, 32, 8);

            // ---------- Barra de modos (rodapé, controle segmentado) ----------
            var modeBar = Panel("BarraModos", safe, PanelColor, ModeBarHeight * 0.5f);
            Place(modeBar.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, Margin), new Vector2(640, ModeBarHeight));
            var visitBtn = MakeButton("BtnModoVisita", modeBar.transform, "Visita", ButtonColor, 28, 36);
            Region((RectTransform)visitBtn.transform, 0, 0, 0.5f, 1, 0, 0);
            ((RectTransform)visitBtn.transform).offsetMin = new Vector2(8, 8);
            ((RectTransform)visitBtn.transform).offsetMax = new Vector2(-4, -8);
            var collectionBtn = MakeButton("BtnModoAcervo", modeBar.transform, "Acervo", ButtonColor, 28, 36);
            Region((RectTransform)collectionBtn.transform, 0.5f, 0, 1, 1, 0, 0);
            ((RectTransform)collectionBtn.transform).offsetMin = new Vector2(4, 8);
            ((RectTransform)collectionBtn.transform).offsetMax = new Vector2(-8, -8);

            // ---------- Tela inicial (por cima de tudo) ----------
            var start = BuildStartScreen(canvasGO.transform, out var startButton, out var startTitle, out var startThumb);

            // ---------- Scripts de UI + ligações ----------
            var info = canvasGO.AddComponent<InfoPanelUI>();
            Wire(info, "headerRoot", header.gameObject);
            Wire(info, "itemTitleText", title);
            Wire(info, "itemSubtitleText", subtitle);
            Wire(info, "progressText", progress);
            Wire(info, "itemInfoButton", infoBtn);
            Wire(info, "toggleHotspotsButton", pointsBtn);
            Wire(info, "resetViewButton", resetBtn);
            Wire(info, "detailRoot", detail.gameObject);
            Wire(info, "detailTitleText", dTitle);
            Wire(info, "detailKindText", dKind);
            Wire(info, "detailBodyText", dBody);
            Wire(info, "detailScroll", dScroll);
            Wire(info, "audioButton", audioBtn);
            Wire(info, "audioButtonLabel", audioBtn.GetComponentInChildren<TextMeshProUGUI>());
            Wire(info, "audioProgressFill", barFill.rectTransform);
            Wire(info, "closeButton", closeBtn);
            Wire(info, "relatedHeaderText", relatedHeader);
            Wire(info, "relatedButtonTemplate", relatedTemplate);

            var catalogUI = canvasGO.AddComponent<CatalogMenuUI>();
            Wire(catalogUI, "catalog", catalog);
            Wire(catalogUI, "placement", placement);
            Wire(catalogUI, "menuRoot", catalogPanel.gameObject);
            Wire(catalogUI, "titleText", cTitle);
            Wire(catalogUI, "listContainer", cContent);
            Wire(catalogUI, "itemButtonTemplate", template);
            Wire(catalogUI, "closeButton", cClose);

            Wire(modes, "freePlacement", placement);
            Wire(modes, "catalogMenu", catalogUI);
            Wire(modes, "hintRoot", hint.gameObject);
            Wire(modes, "hintText", hintText);
            Wire(modes, "visitModeButton", visitBtn);
            Wire(modes, "collectionModeButton", collectionBtn);

            var startUI = canvasGO.AddComponent<StartScreenUI>();
            Wire(startUI, "root", start);
            Wire(startUI, "startButton", startButton);
            Wire(startUI, "catalog", catalog);
            Wire(startUI, "titleText", startTitle);
            Wire(startUI, "thumbnailTemplate", startThumb);
            Wire(modes, "startScreen", startUI);

            if (desktop) Wire(desktop, "infoPanel", info);
        }

        static readonly string[] StartSteps =
        {
            "Aponte a câmera para o QR code ao lado da peça",
            "Arraste para girar e faça pinça para ampliar",
            "Toque nos pontos para descobrir curiosidades",
        };

        /// <summary>Tela cheia com nome do museu, miniaturas das peças, passos de uso e botão "Iniciar".</summary>
        static GameObject BuildStartScreen(Transform canvas, out Button startButton, out TextMeshProUGUI title, out GameObject thumbTemplate)
        {
            var background = Panel("TelaInicial", canvas, new Color(0.06f, 0.06f, 0.08f, 1f));
            Region(background.rectTransform, 0, 0, 1, 1, 0, 0);
            background.gameObject.AddComponent<CanvasGroup>();

            var content = NewRect("Conteudo", background.transform);
            Region(content, 0, 0, 1, 1, 0, 0);
            content.gameObject.AddComponent<SafeAreaFitter>();

            var eyebrow = Label("Chamada", content, "Realidade aumentada", 26, FontStyles.Bold | FontStyles.UpperCase, TextAlignmentOptions.Center);
            eyebrow.color = AccentColor;
            eyebrow.characterSpacing = 8;
            StretchTop(eyebrow.rectTransform, 220, 40, 80, 80);

            title = Label("Titulo", content, "Museu AR", 96, FontStyles.Bold, TextAlignmentOptions.Center);
            title.enableAutoSizing = true;
            title.fontSizeMin = 56;
            title.fontSizeMax = 96;
            StretchTop(title.rectTransform, 268, 124, 60, 60);

            var subtitle = Label("Subtitulo", content, "Veja em 3D as peças que não podem ser tocadas", 34, FontStyles.Normal, TextAlignmentOptions.Center);
            subtitle.color = MutedText;
            StretchTop(subtitle.rectTransform, 400, 100, 120, 120);

            // Miniaturas das peças (preenchidas pelo StartScreenUI a partir do catálogo)
            var row = NewRect("Miniaturas", content);
            StretchTop(row, 548, 200, 40, 40);
            var rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.spacing = 24;
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = false;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;
            var thumbFrame = Panel("MiniaturaTemplate", row, ButtonColor, 32);
            thumbFrame.rectTransform.sizeDelta = new Vector2(200, 200);
            thumbFrame.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var photo = Panel("Foto", thumbFrame.transform, Color.white);
            photo.raycastTarget = false;
            photo.preserveAspect = true;
            Region(photo.rectTransform, 0, 0, 1, 1, 0, 0);
            thumbTemplate = thumbFrame.gameObject;

            // Como usar: 3 passos numerados
            for (int i = 0; i < StartSteps.Length; i++)
            {
                var card = Panel($"Passo{i + 1}", content, new Color(1f, 1f, 1f, 0.06f), 28);
                Place(card.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -(820 + i * 144)), new Vector2(CardWidth - 48, 124));
                var badge = Panel("Numero", card.transform, AccentColor, 36);
                Place(badge.rectTransform, new Vector2(0f, 0.5f), new Vector2(26, 0), new Vector2(72, 72));
                var number = Label("Texto", badge.transform, (i + 1).ToString(), 36, FontStyles.Bold, TextAlignmentOptions.Center);
                number.color = new Color(0.1f, 0.08f, 0.05f);
                Region(number.rectTransform, 0, 0, 1, 1, 0, 0);
                var text = Label("Texto", card.transform, StartSteps[i], 30);
                Region(text.rectTransform, 0, 0, 1, 1, 0, 0);
                text.rectTransform.offsetMin = new Vector2(124, 8);
                text.rectTransform.offsetMax = new Vector2(-28, -8);
            }

            startButton = MakeButton("BtnIniciar", content, "Iniciar", AccentColor, 44, 64);
            startButton.GetComponentInChildren<TextMeshProUGUI>().color = new Color(0.1f, 0.08f, 0.05f);
            Place((RectTransform)startButton.transform, new Vector2(0.5f, 0f), new Vector2(0, 150), new Vector2(640, 128));

            var note = Label("Aviso", content, "Permita o acesso à câmera quando o app pedir.", 24, FontStyles.Normal, TextAlignmentOptions.Center);
            note.color = MutedText;
            Place(note.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 80), new Vector2(CardWidth, 44));

            return background.gameObject;
        }

        // ================= Assets =================

        static Sprite rounded;

        /// <summary>
        /// Círculo branco 128x128 fatiado (9-slice): vira cartão de cantos arredondados ou "pílula".
        /// O raio de cada elemento é controlado por Image.pixelsPerUnitMultiplier (ver Panel).
        /// </summary>
        static Sprite LoadOrCreateRoundedSprite()
        {
            string path = $"{DataFolder}/UI_Arredondado.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing) return existing;

            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float c = (size - 1) * 0.5f, r = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(r - d)));   // borda suavizada
            }
            tex.Apply();
            EnsureFolder(DataFolder);
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = new Vector4(RoundedBorder, RoundedBorder, RoundedBorder, RoundedBorder);
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        const float RoundedBorder = 63f;

        static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static HotspotStyle LoadOrCreateStyle()
        {
            string path = $"{DataFolder}/EstiloHotspots.asset";
            var style = AssetDatabase.LoadAssetAtPath<HotspotStyle>(path);
            if (style) return style;

            style = ScriptableObject.CreateInstance<HotspotStyle>();
            style.lineMaterial = CreateMaterial($"{DataFolder}/Hotspot_Seta.mat", "Universal Render Pipeline/Particles/Unlit");
            style.markerMaterial = CreateMaterial($"{DataFolder}/Hotspot_Marcador.mat", "Universal Render Pipeline/Unlit");
            AssetDatabase.CreateAsset(style, path);
            return style;
        }

        static Material CreateMaterial(string path, string shaderName)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing) return existing;

            var shader = Shader.Find(shaderName);
            if (!shader) shader = Shader.Find("Sprites/Default");
            if (!shader) return null;

            var material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        // ================= Cena =================

        static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            return component ? component : Undo.AddComponent<T>(go);
        }

        static void Wire(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogWarning($"[Museu AR] Campo '{field}' não encontrado em {target.GetType().Name}.");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void EnsureEventSystem()
        {
            var eventSystem = Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem)
            {
                // Qualquer módulo do Input System (inclusive o XRUIInputModule do template) serve.
                // Só troca o StandaloneInputModule antigo, que não funciona com o Input System.
                var legacy = eventSystem.GetComponent<StandaloneInputModule>();
                if (legacy) Undo.DestroyObjectImmediate(legacy);
                if (!eventSystem.GetComponent<BaseInputModule>())
                    Undo.AddComponent<InputSystemUIInputModule>(eventSystem.gameObject);
                return;
            }
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Undo.RegisterCreatedObjectUndo(go, "Museu AR");
        }

        // ================= UI helpers =================

        static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5; // UI
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        /// <summary>Posiciona por âncoras (0–1) com margens em pixels.</summary>
        static void Region(RectTransform rect, float x0, float y0, float x1, float y1, float padX, float padY)
        {
            rect.anchorMin = new Vector2(x0, y0);
            rect.anchorMax = new Vector2(x1, y1);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(padX, padY);
            rect.offsetMax = new Vector2(-padX, -padY);
        }

        /// <summary>Tamanho fixo, preso a uma âncora do pai (ex.: (0.5, 1) = topo centro). A posição conta a partir da âncora.</summary>
        static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        /// <summary>Faixa que ocupa a largura do pai (menos as margens), a "top" unidades do topo.</summary>
        static void StretchTop(RectTransform rect, float top, float height, float left, float right)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, -top - height);
            rect.offsetMax = new Vector2(-right, -top);
        }

        /// <param name="radius">Raio dos cantos em unidades de referência (0 = retângulo).</param>
        static Image Panel(string name, Transform parent, Color color, float radius = 0f)
        {
            var rect = NewRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            if (radius > 0f && rounded)
            {
                image.sprite = rounded;
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = RoundedBorder / radius;
            }
            return image;
        }

        static TextMeshProUGUI Label(string name, Transform parent, string text, float size,
            FontStyles style = FontStyles.Normal, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
        {
            var rect = NewRect(name, parent);
            var tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.alignment = align;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            return tmp;
        }

        static Button MakeButton(string name, Transform parent, string text, Color color, float fontSize, float radius = 0f)
        {
            var image = Panel(name, parent, color, radius);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var label = Label("Texto", image.transform, text, fontSize, FontStyles.Bold, TextAlignmentOptions.Center);
            Region(label.rectTransform, 0, 0, 1, 1, 12, 6);
            return button;
        }

        static ScrollRect MakeScroll(Transform parent, out RectTransform content, float spacing)
        {
            var root = NewRect("Scroll", parent);
            var scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 20f;

            var viewport = NewRect("Viewport", root);
            Region(viewport, 0, 0, 1, 1, 0, 0);
            viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0); // recebe o arrasto
            viewport.gameObject.AddComponent<RectMask2D>();

            content = NewRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = spacing;
            layout.padding = new RectOffset(16, 16, 8, 8);

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;
            return scroll;
        }
    }
}
