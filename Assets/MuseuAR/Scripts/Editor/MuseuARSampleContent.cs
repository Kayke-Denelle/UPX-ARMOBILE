using System.IO;
using UnityEditor;
using UnityEditor.XR.ARSubsystems;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace MuseuAR.EditorTools
{
    /// <summary>
    /// Menu "Museu AR > 2. Criar peças de exemplo": gera duas peças correlacionadas (ânfora e lamparina)
    /// com modelo provisório feito de primitivas, hotspots, textos e placas AR prontas para imprimir.
    /// Serve de referência de como cadastrar uma peça real — troque o modelo pelo scan/modelo 3D do acervo.
    /// Rodar de novo atualiza os exemplos (não duplica).
    /// </summary>
    public static class MuseuARSampleContent
    {
        const string Root = "Assets/MuseuAR/Exemplos";
        const string DataFolder = "Assets/MuseuAR/Data";
        const float PlateSizeMeters = 0.15f;

        static Material terracotta, dark;

        [MenuItem("Museu AR/2. Criar peças de exemplo", priority = -9)]
        static void CreateMenu()
        {
            Create();
            EditorUtility.DisplayDialog("Museu AR",
                "Peças de exemplo criadas em " + Root + ".\n\n" +
                "• Imprima as placas (pasta Exemplos/Placas) com 15 cm de largura\n" +
                "• Modo Visita: aponte o celular para a placa\n" +
                "• Modo Acervo: escolha a peça e toque numa mesa", "OK");
        }

        public static void Create()
        {
            EnsureFolder($"{Root}/Modelos");
            EnsureFolder($"{Root}/Placas");
            EnsureFolder($"{Root}/Pecas");

            terracotta = SaveMaterial("Terracota", new Color(0.72f, 0.38f, 0.22f), 0.25f);
            dark = SaveMaterial("TerracotaEscura", new Color(0.18f, 0.09f, 0.05f), 0.1f);

            var anfora = LoadOrCreate<MuseumItemData>($"{Root}/Pecas/AnforaRomana.asset");
            var lamparina = LoadOrCreate<MuseumItemData>($"{Root}/Pecas/LamparinaRomana.asset");

            // ---------------- Ânfora ----------------
            anfora.id = "anfora-romana-exemplo";
            anfora.displayName = "Ânfora romana (exemplo)";
            anfora.period = "Séc. I a.C. – II d.C.";
            anfora.origin = "Mediterrâneo";
            anfora.material = "Cerâmica (terracota)";
            anfora.description =
                "Ânforas eram os \"contêineres\" do mundo antigo: transportavam vinho, azeite, grãos e molho de peixe " +
                "por todo o Mediterrâneo. Milhares delas foram encontradas em naufrágios, ainda empilhadas nos porões.\n\n" +
                "Toque nos pontos pulsantes para descobrir os detalhes da peça.";
            anfora.referenceImageName = "placa-anfora";
            anfora.initialScale = 1f;
            anfora.spawnOffset = new Vector3(0f, 0.02f, 0f);
            anfora.modelPrefab = SavePrefab("Anfora", BuildAnfora(lamparina));
            anfora.relatedItems = new System.Collections.Generic.List<MuseumItemData> { lamparina };

            // ---------------- Lamparina ----------------
            lamparina.id = "lamparina-romana-exemplo";
            lamparina.displayName = "Lamparina a óleo (exemplo)";
            lamparina.period = "Séc. I d.C.";
            lamparina.origin = "Império Romano";
            lamparina.material = "Cerâmica moldada";
            lamparina.description =
                "A principal fonte de luz das casas romanas. Queimava azeite de oliva — o mesmo que chegava às cidades " +
                "dentro das ânforas. Peças como esta são frágeis demais para serem manuseadas pelo público.";
            lamparina.referenceImageName = "placa-lamparina";
            lamparina.initialScale = 1.6f;
            lamparina.spawnOffset = new Vector3(0f, 0.02f, 0f);
            lamparina.modelPrefab = SavePrefab("Lamparina", BuildLamparina(anfora));
            lamparina.relatedItems = new System.Collections.Generic.List<MuseumItemData> { anfora };

            EditorUtility.SetDirty(anfora);
            EditorUtility.SetDirty(lamparina);

            // ---------------- Placas AR + catálogo ----------------
            var library = AssetDatabase.LoadAssetAtPath<XRReferenceImageLibrary>($"{DataFolder}/PlacasAR.asset");
            var catalog = AssetDatabase.LoadAssetAtPath<MuseumCatalog>($"{DataFolder}/CatalogoDoMuseu.asset");

            RegisterPlate(library, anfora.referenceImageName, SavePlate(anfora.referenceImageName, 1234, new Color(0.75f, 0.3f, 0.1f)));
            RegisterPlate(library, lamparina.referenceImageName, SavePlate(lamparina.referenceImageName, 98765, new Color(0.1f, 0.35f, 0.7f)));

            if (catalog)
            {
                if (catalog.museumName == "Meu Museu") catalog.museumName = "Museu de Exemplo";
                if (!catalog.items.Contains(anfora)) catalog.items.Add(anfora);
                if (!catalog.items.Contains(lamparina)) catalog.items.Add(lamparina);
                EditorUtility.SetDirty(catalog);
            }
            else Debug.LogWarning("[Museu AR] CatalogoDoMuseu não encontrado. Rode 'Criar cena do museu' antes.");

            AssetDatabase.SaveAssets();
            Debug.Log("[Museu AR] Peças de exemplo criadas/atualizadas em " + Root);
        }

        // ================= Modelos (primitivas provisórias) =================
        // Origem do modelo = base da peça. Unidades em metros. Eixo +Z = lado virado para o visitante.

        static GameObject BuildAnfora(MuseumItemData related)
        {
            var root = new GameObject("Anfora");
            Part(root, PrimitiveType.Capsule, "Ponta", new Vector3(0f, 0.05f, 0f), new Vector3(0.035f, 0.05f, 0.035f), terracotta);
            Part(root, PrimitiveType.Sphere, "Corpo", new Vector3(0f, 0.17f, 0f), new Vector3(0.16f, 0.22f, 0.16f), terracotta);
            Part(root, PrimitiveType.Cylinder, "Pescoco", new Vector3(0f, 0.31f, 0f), new Vector3(0.05f, 0.06f, 0.05f), terracotta);
            Part(root, PrimitiveType.Cylinder, "Borda", new Vector3(0f, 0.372f, 0f), new Vector3(0.068f, 0.008f, 0.068f), terracotta);
            Part(root, PrimitiveType.Cylinder, "Abertura", new Vector3(0f, 0.3805f, 0f), new Vector3(0.04f, 0.001f, 0.04f), dark);
            Part(root, PrimitiveType.Capsule, "AlcaEsq", new Vector3(-0.052f, 0.315f, 0f), new Vector3(0.016f, 0.055f, 0.016f), terracotta, new Vector3(0f, 0f, -12f));
            Part(root, PrimitiveType.Capsule, "AlcaDir", new Vector3(0.052f, 0.315f, 0f), new Vector3(0.016f, 0.055f, 0.016f), terracotta, new Vector3(0f, 0f, 12f));

            Hotspot(root, "Bocal", HotspotKind.Tecnica, new Vector3(0f, 0.38f, 0f), new Vector3(-0.17f, 0.06f, 0f),
                "O bocal estreito e a borda grossa facilitavam a vedação: a ânfora era fechada com uma rolha de cortiça " +
                "ou argila e selada com resina, preservando o conteúdo por meses de viagem.");
            Hotspot(root, "Alças", HotspotKind.Detalhe, new Vector3(0.06f, 0.33f, 0.01f), new Vector3(0.18f, 0.02f, 0f),
                "As duas alças permitiam que duas pessoas carregassem a peça cheia (podia passar de 50 kg) " +
                "e que fosse amarrada no porão dos navios.");
            Hotspot(root, "Corpo", HotspotKind.Correlacao, new Vector3(-0.075f, 0.17f, 0.03f), new Vector3(-0.17f, 0f, 0f),
                "Muitas ânforas levavam azeite de oliva. Nas casas romanas, esse azeite era usado na cozinha, " +
                "na higiene e como combustível das lamparinas — veja a lamparina a óleo em 3D.",
                related);
            Hotspot(root, "Base pontuda", HotspotKind.Curiosidade, new Vector3(0.02f, 0.02f, 0.03f), new Vector3(0.18f, 0.04f, 0f),
                "Por que a base é pontuda? Ela era enterrada na areia ou encaixada entre as outras ânforas, " +
                "formando camadas firmes no porão do navio. A ponta também servia de terceira alça ao despejar o líquido.");
            return root;
        }

        static GameObject BuildLamparina(MuseumItemData related)
        {
            var root = new GameObject("Lamparina");
            Part(root, PrimitiveType.Sphere, "Corpo", new Vector3(0f, 0.02f, 0f), new Vector3(0.1f, 0.04f, 0.1f), terracotta);
            Part(root, PrimitiveType.Cylinder, "Disco", new Vector3(0f, 0.039f, 0f), new Vector3(0.06f, 0.002f, 0.06f), terracotta);
            Part(root, PrimitiveType.Cylinder, "Furo", new Vector3(0f, 0.0405f, 0f), new Vector3(0.012f, 0.001f, 0.012f), dark);
            Part(root, PrimitiveType.Capsule, "Bico", new Vector3(0.065f, 0.02f, 0f), new Vector3(0.03f, 0.03f, 0.03f), terracotta, new Vector3(0f, 0f, 90f));
            Part(root, PrimitiveType.Cylinder, "Pavio", new Vector3(0.085f, 0.035f, 0f), new Vector3(0.01f, 0.001f, 0.01f), dark);
            Part(root, PrimitiveType.Capsule, "Alca", new Vector3(-0.058f, 0.035f, 0f), new Vector3(0.014f, 0.022f, 0.014f), terracotta, new Vector3(0f, 0f, 55f));

            Hotspot(root, "Bico e pavio", HotspotKind.Correlacao, new Vector3(0.086f, 0.036f, 0f), new Vector3(0.09f, 0.05f, 0f),
                "Um pavio de linho saía pelo bico e queimava o azeite do reservatório. " +
                "Esse azeite chegava às cidades transportado em ânforas.",
                related);
            Hotspot(root, "Disco decorado", HotspotKind.Detalhe, new Vector3(0f, 0.041f, 0.02f), new Vector3(-0.02f, 0.08f, 0f),
                "O disco superior costumava ter relevos de deuses, gladiadores ou animais. " +
                "O furo central servia para reabastecer o azeite.");
            Hotspot(root, "Produção em série", HotspotKind.Curiosidade, new Vector3(-0.065f, 0.045f, 0f), new Vector3(-0.1f, 0.03f, 0f),
                "Lamparinas eram feitas com moldes de gesso ou argila, em duas metades — uma das primeiras " +
                "produções em massa da história. Muitas trazem a marca da oficina embaixo.");
            return root;
        }

        static void Part(GameObject root, PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material, Vector3 euler = default)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(root.transform, false);
            part.transform.localPosition = position;
            part.transform.localEulerAngles = euler;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
        }

        static void Hotspot(GameObject root, string title, HotspotKind kind, Vector3 position, Vector3 labelOffset,
            string description, MuseumItemData related = null)
        {
            var go = new GameObject($"Hotspot - {title}");
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = position;
            var point = go.AddComponent<HotspotPoint>();
            point.title = title;
            point.kind = kind;
            point.labelOffset = labelOffset;
            point.description = description;
            point.relatedItem = related;
        }

        static GameObject SavePrefab(string name, GameObject instance)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, $"{Root}/Modelos/{name}.prefab");
            Object.DestroyImmediate(instance);
            return prefab;
        }

        static Material SaveMaterial(string name, Color color, float smoothness)
        {
            string path = $"{Root}/Modelos/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            material.SetColor("_Color", color);
            material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        // ================= Placas AR =================

        /// <summary>
        /// Gera uma imagem com muitos contrastes e cantos (é disso que o rastreamento de imagens precisa).
        /// Em um museu real, a placa pode ser a própria etiqueta da vitrine com uma arte rica em detalhes.
        /// </summary>
        static Texture2D SavePlate(string name, int seed, Color accent)
        {
            const int size = 1024;
            var tex = new Texture2D(size, size, TextureFormat.RGB24, false);
            var pixels = new Color32[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;

            var rng = new System.Random(seed);
            Color[] palette = { Color.black, accent, new Color(0.95f, 0.75f, 0.1f), new Color(0.2f, 0.2f, 0.2f), accent * 0.6f };

            for (int n = 0; n < 140; n++)
            {
                Color32 color = palette[rng.Next(palette.Length)];
                int w = rng.Next(20, 170), h = rng.Next(20, 170);
                int x0 = rng.Next(40, size - 40 - w), y0 = rng.Next(40, size - 40 - h);
                bool circle = rng.NextDouble() < 0.4;
                float cx = x0 + w * 0.5f, cy = y0 + h * 0.5f, r = Mathf.Min(w, h) * 0.5f;

                for (int y = y0; y < y0 + h; y++)
                for (int x = x0; x < x0 + w; x++)
                {
                    if (circle && (x - cx) * (x - cx) + (y - cy) * (y - cy) > r * r) continue;
                    pixels[y * size + x] = color;
                }
            }

            // Moldura preta (ajuda a enquadrar ao imprimir)
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                if (x < 20 || y < 20 || x >= size - 20 || y >= size - 20)
                    pixels[y * size + x] = Color.black;

            tex.SetPixels32(pixels);
            tex.Apply();

            string path = $"{Root}/Placas/{name}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = size;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static void RegisterPlate(XRReferenceImageLibrary library, string imageName, Texture2D texture)
        {
            if (!library)
            {
                Debug.LogWarning("[Museu AR] PlacasAR não encontrada. Rode 'Criar cena do museu' antes.");
                return;
            }

            int index = -1;
            for (int i = 0; i < library.count; i++)
                if (library[i].name == imageName) { index = i; break; }

            if (index < 0)
            {
                library.Add();
                index = library.count - 1;
            }

            library.SetName(index, imageName);
            library.SetTexture(index, texture, true);
            library.SetSpecifySize(index, true);
            library.SetSize(index, new Vector2(PlateSizeMeters, PlateSizeMeters));
            EditorUtility.SetDirty(library);
        }

        // ================= Utilitários =================

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

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
    }
}
