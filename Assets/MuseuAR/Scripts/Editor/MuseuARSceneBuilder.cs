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
                "Próximo passo: menu Museu AR > 2. Criar peças de exemplo.");
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

            EnsureEventSystem();

            // ---------- Canvas ----------
            var canvasGO = new GameObject("MuseuAR_UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasGO, "Museu AR");
            canvasGO.layer = 5;
            canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            var safe = NewRect("SafeArea", canvasGO.transform);
            Region(safe, 0, 0, 1, 1, 0, 0);
            safe.gameObject.AddComponent<SafeAreaFitter>();

            // Cabeçalho
            var header = Panel("CabecalhoPeca", safe, PanelColor);
            Region(header.rectTransform, 0f, 0.8f, 1f, 1f, 24, 16);
            var title = Label("Titulo", header.transform, "Nome da peça", 54, FontStyles.Bold);
            Region(title.rectTransform, 0, 0.64f, 1, 1, 32, 8);
            var subtitle = Label("Subtitulo", header.transform, "Período · Origem · Material", 34);
            subtitle.color = MutedText;
            Region(subtitle.rectTransform, 0, 0.47f, 1, 0.64f, 32, 4);
            var progress = Label("Progresso", header.transform, "Curiosidades descobertas: 0/0", 32);
            progress.color = AccentColor;
            Region(progress.rectTransform, 0, 0.31f, 1, 0.47f, 32, 4);
            var infoBtn = MakeButton("BtnSobre", header.transform, "Sobre a peça", ButtonColor, 30);
            Region((RectTransform)infoBtn.transform, 0f, 0.03f, 0.36f, 0.29f, 16, 6);
            var pointsBtn = MakeButton("BtnPontos", header.transform, "Mostrar/ocultar pontos", ButtonColor, 30);
            Region((RectTransform)pointsBtn.transform, 0.36f, 0.03f, 0.74f, 0.29f, 8, 6);
            var resetBtn = MakeButton("BtnResetar", header.transform, "Resetar", ButtonColor, 30);
            Region((RectTransform)resetBtn.transform, 0.74f, 0.03f, 1f, 0.29f, 16, 6);

            // Painel de detalhes
            var detail = Panel("PainelDetalhes", safe, PanelColor);
            Region(detail.rectTransform, 0f, 0.1f, 1f, 0.56f, 24, 8);
            var dTitle = Label("Titulo", detail.transform, "Título da curiosidade", 46, FontStyles.Bold);
            Region(dTitle.rectTransform, 0, 0.85f, 0.76f, 1, 32, 8);
            var closeBtn = MakeButton("BtnFechar", detail.transform, "Fechar", ButtonColor, 30);
            Region((RectTransform)closeBtn.transform, 0.76f, 0.86f, 1, 0.99f, 20, 6);
            var dKind = Label("Categoria", detail.transform, "Curiosidade", 30);
            dKind.color = AccentColor;
            Region(dKind.rectTransform, 0, 0.77f, 1, 0.86f, 32, 4);
            var dScroll = MakeScroll(detail.transform, out var dContent, 0);
            Region((RectTransform)dScroll.transform, 0, 0.2f, 1, 0.77f, 16, 4);
            var dBody = Label("Corpo", dContent, "Texto da curiosidade...", 36, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            var relatedHeader = Label("VejaTambem", dContent, "Veja também", 32, FontStyles.Bold);
            relatedHeader.color = AccentColor;
            relatedHeader.margin = new Vector4(0, 24, 0, 0);
            var relatedTemplate = MakeButton("RelacionadaTemplate", dContent, "Ver em 3D", ButtonColor, 32);
            var relatedLayout = relatedTemplate.gameObject.AddComponent<LayoutElement>();
            relatedLayout.minHeight = 100;
            relatedLayout.preferredHeight = 100;
            var audioBtn = MakeButton("BtnAudio", detail.transform, "Ouvir áudio", AccentColor, 32);
            Region((RectTransform)audioBtn.transform, 0, 0.03f, 0.42f, 0.18f, 24, 6);
            var barBg = Panel("BarraAudio", detail.transform, new Color(1, 1, 1, 0.15f));
            barBg.raycastTarget = false;
            Region(barBg.rectTransform, 0.44f, 0.095f, 1f, 0.115f, 32, 0);
            var barFill = Panel("Preenchimento", barBg.transform, AccentColor);
            barFill.raycastTarget = false;
            barFill.rectTransform.anchorMin = Vector2.zero;
            barFill.rectTransform.anchorMax = new Vector2(0f, 1f);
            barFill.rectTransform.offsetMin = Vector2.zero;
            barFill.rectTransform.offsetMax = Vector2.zero;

            // Menu do acervo
            var catalogPanel = Panel("MenuAcervo", safe, PanelColor);
            Region(catalogPanel.rectTransform, 0f, 0.1f, 1f, 0.78f, 24, 8);
            var cTitle = Label("Titulo", catalogPanel.transform, "Acervo digital", 46, FontStyles.Bold);
            Region(cTitle.rectTransform, 0, 0.89f, 0.74f, 1, 32, 8);
            var cClose = MakeButton("BtnFechar", catalogPanel.transform, "Fechar", ButtonColor, 30);
            Region((RectTransform)cClose.transform, 0.74f, 0.9f, 1, 0.99f, 20, 6);
            var cScroll = MakeScroll(catalogPanel.transform, out var cContent, 16);
            Region((RectTransform)cScroll.transform, 0, 0.02f, 1, 0.89f, 16, 4);
            var template = MakeButton("ItemTemplate", cContent, "Peça", ButtonColor, 36);
            var templateLayout = template.gameObject.AddComponent<LayoutElement>();
            templateLayout.minHeight = 150;
            templateLayout.preferredHeight = 150;
            var thumb = Panel("Thumbnail", template.transform, Color.white);
            thumb.raycastTarget = false;
            thumb.preserveAspect = true;
            var thumbRect = thumb.rectTransform;
            thumbRect.anchorMin = thumbRect.anchorMax = new Vector2(0f, 0.5f);
            thumbRect.pivot = new Vector2(0f, 0.5f);
            thumbRect.anchoredPosition = new Vector2(16f, 0f);
            thumbRect.sizeDelta = new Vector2(118f, 118f);
            var templateLabel = template.GetComponentInChildren<TextMeshProUGUI>();
            templateLabel.alignment = TextAlignmentOptions.MidlineLeft;
            templateLabel.fontStyle = FontStyles.Normal;
            templateLabel.rectTransform.offsetMin = new Vector2(152f, 6f);

            // Dica
            var hint = Panel("Dica", safe, new Color(0f, 0f, 0f, 0.65f));
            hint.raycastTarget = false;
            Region(hint.rectTransform, 0.06f, 0.6f, 0.94f, 0.68f, 0, 0);
            var hintText = Label("Texto", hint.transform, "Aponte a câmera para a placa AR", 34, FontStyles.Normal, TextAlignmentOptions.Center);
            Region(hintText.rectTransform, 0, 0, 1, 1, 24, 8);

            // Barra de modos
            var modeBar = NewRect("BarraModos", safe);
            Region(modeBar, 0f, 0f, 1f, 0.085f, 24, 12);
            var visitBtn = MakeButton("BtnModoVisita", modeBar, "Modo Visita", ButtonColor, 36);
            Region((RectTransform)visitBtn.transform, 0, 0, 0.5f, 1, 8, 0);
            var collectionBtn = MakeButton("BtnModoAcervo", modeBar, "Modo Acervo", ButtonColor, 36);
            Region((RectTransform)collectionBtn.transform, 0.5f, 0, 1, 1, 8, 0);

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

            Wire(modes, "imageManager", imageManager);
            Wire(modes, "planeManager", planeManager);
            Wire(modes, "imageSpawner", spawner);
            Wire(modes, "freePlacement", placement);
            Wire(modes, "catalogMenu", catalogUI);
            Wire(modes, "hintRoot", hint.gameObject);
            Wire(modes, "hintText", hintText);
            Wire(modes, "visitModeButton", visitBtn);
            Wire(modes, "collectionModeButton", collectionBtn);

            var desktop = systems.AddComponent<DesktopSimulator>();
            Wire(desktop, "imageSpawner", spawner);
            Wire(desktop, "modes", modes);
            Wire(desktop, "infoPanel", info);
            Wire(desktop, "arCamera", arCamera);

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

        // ================= Assets =================

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

        static Image Panel(string name, Transform parent, Color color)
        {
            var rect = NewRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
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

        static Button MakeButton(string name, Transform parent, string text, Color color, float fontSize)
        {
            var image = Panel(name, parent, color);
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
