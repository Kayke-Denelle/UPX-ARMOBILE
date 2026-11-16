using System.IO;
using UnityEditor;
using UnityEngine;

namespace MuseuAR.EditorTools
{
    /// <summary>
    /// Paleta e imagem de fundo da tela inicial (a UI da Unity não tem degradê nativo).
    /// Visual fosco e discreto: degradê suave no fundo, botões e cartões em cores sólidas, sem brilho.
    /// O fundo é recriado a cada "Reconstruir interface", então basta mudar as cores aqui.
    /// </summary>
    public static class StartScreenArt
    {
        const string Folder = "Assets/MuseuAR/Data/UI";

        // ---------- Paleta (tons foscos) ----------
        public static readonly Color BackgroundTop = Hex("232A36");
        public static readonly Color BackgroundBottom = Hex("14181F");
        public static readonly Color Surface = Hex("2C3442");        // emblema
        public static readonly Color Outline = Hex("3A4456");        // aro do emblema
        public static readonly Color Sand = Hex("D4A86A");           // botão Iniciar
        public static readonly Color Sage = Hex("8FB3A8");
        public static readonly Color Rose = Hex("C9999A");
        public static readonly Color Text = Hex("ECE7DE");           // branco quente
        public static readonly Color TextMuted = Hex("A7ADB8");
        public static readonly Color TextDim = Hex("7D8491");
        public static readonly Color Ink = Hex("1B1F27");            // texto sobre cores claras

        public static Sprite Background() => Save("TelaInicial_Fundo", 720, 1280, PaintBackground);

        /// <summary>Degradê vertical suave, levemente diagonal, sem brilhos.</summary>
        static Color PaintBackground(float u, float v, System.Random rng)
        {
            // v = 0 no topo, 1 embaixo
            Color c = Color.Lerp(BackgroundTop, BackgroundBottom, Mathf.Clamp01(v * 0.9f + u * 0.1f));
            float dither = ((float)rng.NextDouble() - 0.5f) / 255f * 2f;   // evita faixas no degradê
            return new Color(c.r + dither, c.g + dither, c.b + dither, 1f);
        }

        // ================= Arquivo =================

        delegate Color Painter(float u, float v, System.Random rng);

        static Sprite Save(string name, int width, int height, Painter paint)
        {
            Directory.CreateDirectory(Folder);
            string path = $"{Folder}/{name}.png";

            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            var rng = new System.Random(7);
            for (int y = 0; y < height; y++)
            {
                float v = 1f - (y + 0.5f) / height;   // as linhas da textura começam embaixo
                for (int x = 0; x < width; x++)
                    pixels[y * width + x] = paint((x + 0.5f) / width, v, rng);
            }
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;   // compressão cria faixas no degradê
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var color);
            return color;
        }
    }
}
