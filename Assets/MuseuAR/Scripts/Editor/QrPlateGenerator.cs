using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.XR.ARSubsystems;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace MuseuAR.EditorTools
{
    /// <summary>
    /// Menu "Museu AR > 3. Gerar placas com QR code": cria uma placa por peça do catálogo.
    ///  • Moldura ilustrada (formas únicas por peça): é o que o ARCore/ARKit rastreia, por isso a peça fica estável.
    ///  • QR code no centro: abre o link do catálogo (MuseumCatalog.qrContent) na câmera comum do celular.
    /// As placas são registradas na biblioteca PlacasAR com o tamanho impresso (15 cm).
    /// Rodar de novo atualiza as placas (o desenho da moldura é sempre o mesmo para cada peça).
    /// </summary>
    public static class QrPlateGenerator
    {
        const string CatalogPath = "Assets/MuseuAR/Data/CatalogoDoMuseu.asset";
        const string LibraryPath = "Assets/MuseuAR/Data/PlacasAR.asset";
        public const float PlateSizeMeters = 0.15f;
        const int Size = 1024;
        const int Border = 20;
        const float QrBoxFraction = 0.46f;   // largura do quadro branco do QR em relação à placa

        static readonly Color[] Accents =
        {
            new Color(0.75f, 0.30f, 0.10f), new Color(0.10f, 0.35f, 0.70f), new Color(0.15f, 0.45f, 0.40f),
            new Color(0.45f, 0.20f, 0.55f), new Color(0.70f, 0.15f, 0.25f), new Color(0.20f, 0.50f, 0.15f),
        };

        [MenuItem("Museu AR/3. Gerar placas com QR code", priority = -8)]
        static void GenerateMenu()
        {
            string report = GenerateAll();
            if (report != null)
                EditorUtility.DisplayDialog("Museu AR", report + "\nImprima cada placa com 15 cm de largura.", "OK");
        }

        /// <summary>Gera as placas de todas as peças do catálogo. Retorna um resumo (ou null se faltar algo).</summary>
        public static string GenerateAll()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<MuseumCatalog>(CatalogPath);
            var library = AssetDatabase.LoadAssetAtPath<XRReferenceImageLibrary>(LibraryPath);
            if (!catalog || !library)
            {
                EditorUtility.DisplayDialog("Museu AR", $"Não encontrei {CatalogPath} ou {LibraryPath}.", "OK");
                return null;
            }

            var report = new StringBuilder("Placas geradas:\n");
            foreach (var item in catalog.items)
            {
                if (!item) continue;
                if (string.IsNullOrWhiteSpace(item.referenceImageName))
                {
                    item.referenceImageName = "placa-" + item.id;
                    EditorUtility.SetDirty(item);
                }

                string content = catalog.QrContentFor(item);
                string path = PlatePath(library, item);
                var texture = SavePlate(path, item.id, content);
                RegisterPlate(library, item.referenceImageName, texture);
                report.AppendLine($"• {item.displayName}: {path}\n   QR = {content}");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[Museu AR] " + report);
            return report.ToString();
        }

        /// <summary>Reaproveita o arquivo da placa já registrada; senão cria ao lado da ficha da peça.</summary>
        static string PlatePath(XRReferenceImageLibrary library, MuseumItemData item)
        {
            for (int i = 0; i < library.count; i++)
            {
                if (library[i].name != item.referenceImageName || !library[i].texture) continue;
                string existing = AssetDatabase.GetAssetPath(library[i].texture);
                if (!string.IsNullOrEmpty(existing)) return existing;
            }
            string folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(item)).Replace('\\', '/');
            return $"{folder}/{item.referenceImageName}.png";
        }

        // ================= Desenho =================

        static Texture2D SavePlate(string path, string id, string qrContent)
        {
            var pixels = new Color32[Size * Size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;

            uint seed = Fnv1a(id);
            var rng = new System.Random((int)seed);
            Color accent = Accents[seed % (uint)Accents.Length];
            Color[] palette = { Color.black, accent, new Color(0.95f, 0.75f, 0.1f), new Color(0.2f, 0.2f, 0.2f), accent * 0.6f };

            // Moldura: muitas formas de alto contraste (cantos e bordas = o que o rastreamento procura)
            for (int n = 0; n < 170; n++)
            {
                Color32 color = palette[rng.Next(palette.Length)];
                int w = rng.Next(20, 150), h = rng.Next(20, 150);
                int x0 = rng.Next(Border + 10, Size - Border - 10 - w), y0 = rng.Next(Border + 10, Size - Border - 10 - h);
                bool circle = rng.NextDouble() < 0.4;
                float cx = x0 + w * 0.5f, cy = y0 + h * 0.5f, r = Mathf.Min(w, h) * 0.5f;
                for (int y = y0; y < y0 + h; y++)
                for (int x = x0; x < x0 + w; x++)
                {
                    if (circle && (x - cx) * (x - cx) + (y - cy) * (y - cy) > r * r) continue;
                    pixels[y * Size + x] = color;
                }
            }

            // QR no centro, com zona de silêncio branca de 4 módulos
            bool[,] qr = QrCode.Encode(qrContent);
            int modules = qr.GetLength(0);
            int modulePx = Mathf.FloorToInt(Size * QrBoxFraction / (modules + 8));
            int box = (modules + 8) * modulePx;
            int bx = (Size - box) / 2, by = (Size - box) / 2;

            FillRect(pixels, bx - 14, by - 14, box + 28, box + 28, accent);       // aro colorido
            FillRect(pixels, bx - 6, by - 6, box + 12, box + 12, Color.black);   // contorno
            FillRect(pixels, bx, by, box, box, Color.white);
            for (int my = 0; my < modules; my++)
            for (int mx = 0; mx < modules; mx++)
            {
                if (!qr[my, mx]) continue;
                // linha 0 do QR = topo da imagem (as texturas começam embaixo)
                int px = bx + (mx + 4) * modulePx;
                int py = by + box - (my + 5) * modulePx;
                FillRect(pixels, px, py, modulePx, modulePx, Color.black);
            }

            // Borda preta externa (ajuda a recortar e enquadrar)
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
                if (x < Border || y < Border || x >= Size - Border || y >= Size - Border)
                    pixels[y * Size + x] = Color.black;

            var tex = new Texture2D(Size, Size, TextureFormat.RGB24, false);
            tex.SetPixels32(pixels);
            tex.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = Size;
            importer.isReadable = false;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static void FillRect(Color32[] pixels, int x0, int y0, int w, int h, Color32 color)
        {
            for (int y = Mathf.Max(0, y0); y < Mathf.Min(Size, y0 + h); y++)
            for (int x = Mathf.Max(0, x0); x < Mathf.Min(Size, x0 + w); x++)
                pixels[y * Size + x] = color;
        }

        /// <summary>Hash estável (o desenho da moldura não muda entre execuções).</summary>
        static uint Fnv1a(string s)
        {
            uint hash = 2166136261;
            foreach (char c in s ?? "") { hash ^= c; hash *= 16777619; }
            return hash;
        }

        // ================= Biblioteca de imagens =================

        static void RegisterPlate(XRReferenceImageLibrary library, string imageName, Texture2D texture)
        {
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
    }
}
