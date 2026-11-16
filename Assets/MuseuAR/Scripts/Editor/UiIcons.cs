using System.IO;
using UnityEditor;
using UnityEngine;

namespace MuseuAR.EditorTools
{
    /// <summary>
    /// Ícones da interface desenhados por código (formas simples, traço uniforme, brancos sobre transparente).
    /// A cor final vem do Image na interface, então o mesmo ícone serve em botões claros e escuros.
    /// Salvos em Assets/MuseuAR/Data/UI/Icones/ e recriados a cada "Reconstruir interface".
    /// </summary>
    public static class UiIcons
    {
        const string Folder = "Assets/MuseuAR/Data/UI/Icones";
        const int Size = 128;
        const float Stroke = 9f;   // espessura do traço em pixels (128x128)

        delegate float Shape(float x, float y);   // retorna a opacidade (0..1) no ponto

        public static Sprite Info() => Save("info", (x, y) => Max(
            Ring(x, y, 64, 64, 50, Stroke),
            Disc(x, y, 64, 89, 7),
            Line(x, y, 64, 40, 64, 70, Stroke)));

        public static Sprite Points() => Save("pontos", (x, y) => Max(
            Ring(x, y, 64, 64, 48, Stroke),
            Disc(x, y, 64, 64, 16)));

        public static Sprite Reset() => Save("resetar", (x, y) =>
        {
            const float cx = 64, cy = 64, r = 44, gapStart = 25f, gapEnd = 95f;   // abertura no topo, à direita
            float ang = Mathf.Atan2(y - cy, x - cx) * Mathf.Rad2Deg;
            if (ang < 0) ang += 360f;
            float arc = ang > gapStart && ang < gapEnd ? 0f : Ring(x, y, cx, cy, r, Stroke);
            // ponta da seta na extremidade do arco (ângulo 95°), apontando no sentido horário
            float a = gapEnd * Mathf.Deg2Rad;
            var p = new Vector2(cx + r * Mathf.Cos(a), cy + r * Mathf.Sin(a));
            var radial = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            var dir = new Vector2(Mathf.Sin(a), -Mathf.Cos(a));
            var tip = p + dir * 22f;
            var b1 = p - dir * 4f + radial * 17f;
            var b2 = p - dir * 4f - radial * 17f;
            return Max(arc, Triangle(x, y, tip, b1, b2));
        });

        public static Sprite Close() => Save("fechar", (x, y) => Max(
            Line(x, y, 36, 36, 92, 92, Stroke + 1),
            Line(x, y, 36, 92, 92, 36, Stroke + 1)));

        public static Sprite Play() => Save("tocar", (x, y) =>
            Triangle(x, y, new Vector2(100, 64), new Vector2(42, 98), new Vector2(42, 30)));

        public static Sprite Pause() => Save("pausar", (x, y) => Max(
            Line(x, y, 46, 34, 46, 94, 16),
            Line(x, y, 82, 34, 82, 94, 16)));

        /// <summary>Cubo em perspectiva isométrica (peça em 3D).</summary>
        public static Sprite Cube() => Save("cubo3d", (x, y) =>
        {
            var top = new Vector2(64, 110); var ur = new Vector2(104, 87); var lr = new Vector2(104, 41);
            var bottom = new Vector2(64, 18); var ll = new Vector2(24, 41); var ul = new Vector2(24, 87);
            var c = new Vector2(64, 64);
            float w = Stroke - 1;
            return Max(Max(L(x, y, top, ur, w), L(x, y, ur, lr, w), L(x, y, lr, bottom, w)),
                       Max(L(x, y, bottom, ll, w), L(x, y, ll, ul, w), L(x, y, ul, top, w)),
                       Max(L(x, y, c, ul, w), L(x, y, c, ur, w), L(x, y, c, bottom, w)));
        });

        /// <summary>Moldura de leitura (cantos) com um quadrado no centro: apontar a câmera para a placa.</summary>
        public static Sprite Scan() => Save("escanear", (x, y) =>
        {
            const float a = 22, b = 106, len = 26, w = Stroke;
            float top = Max(Max(Line(x, y, a, b, a + len, b, w), Line(x, y, a, b, a, b - len, w)),
                            Max(Line(x, y, b, b, b - len, b, w), Line(x, y, b, b, b, b - len, w)));
            float bottom = Max(Max(Line(x, y, a, a, a + len, a, w), Line(x, y, a, a, a, a + len, w)),
                               Max(Line(x, y, b, a, b - len, a, w), Line(x, y, b, a, b, a + len, w)));
            return Max(top, bottom, Box(x, y, 64, 64, 15, 15, 4));
        });

        /// <summary>Grade 2x2: catálogo do acervo.</summary>
        public static Sprite Grid() => Save("grade", (x, y) => Max(
            Max(Box(x, y, 42, 86, 18, 18, 6), Box(x, y, 86, 86, 18, 18, 6)),
            Max(Box(x, y, 42, 42, 18, 18, 6), Box(x, y, 86, 42, 18, 18, 6))));

        public static Sprite Chevron() => Save("seta", (x, y) => Max(
            Line(x, y, 50, 98, 82, 64, Stroke + 1),
            Line(x, y, 82, 64, 50, 30, Stroke + 1)));

        // ================= Formas (distância com borda suavizada) =================

        static float Max(float a, float b) => Mathf.Max(a, b);
        static float Max(float a, float b, float c) => Mathf.Max(a, Mathf.Max(b, c));

        static float Edge(float distanceOutside) => Mathf.Clamp01(0.5f - distanceOutside);

        static float Disc(float x, float y, float cx, float cy, float r) =>
            Edge(new Vector2(x - cx, y - cy).magnitude - r);

        static float Ring(float x, float y, float cx, float cy, float r, float width) =>
            Edge(Mathf.Abs(new Vector2(x - cx, y - cy).magnitude - r) - width * 0.5f);

        static float Line(float x, float y, float x0, float y0, float x1, float y1, float width) =>
            L(x, y, new Vector2(x0, y0), new Vector2(x1, y1), width);

        static float L(float x, float y, Vector2 a, Vector2 b, float width)
        {
            var p = new Vector2(x, y);
            var ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Edge((p - (a + ab * t)).magnitude - width * 0.5f);
        }

        static float Box(float x, float y, float cx, float cy, float hx, float hy, float radius)
        {
            float qx = Mathf.Abs(x - cx) - (hx - radius), qy = Mathf.Abs(y - cy) - (hy - radius);
            float d = new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0) - radius;
            return Edge(d);
        }

        static float Triangle(float x, float y, Vector2 a, Vector2 b, Vector2 c)
        {
            var p = new Vector2(x, y);
            float d = Mathf.Max(EdgeDistance(p, a, b), Mathf.Max(EdgeDistance(p, b, c), EdgeDistance(p, c, a)));
            // a orientação dos vértices não importa: testa também com o sinal invertido
            float d2 = Mathf.Max(EdgeDistance(p, b, a), Mathf.Max(EdgeDistance(p, c, b), EdgeDistance(p, a, c)));
            return Edge(Mathf.Min(d, d2));
        }

        /// <summary>Distância com sinal até a reta AB (positiva de um dos lados).</summary>
        static float EdgeDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            var e = b - a;
            var n = new Vector2(e.y, -e.x).normalized;
            return Vector2.Dot(p - a, n);
        }

        // ================= Arquivo =================

        static Sprite Save(string name, Shape shape)
        {
            Directory.CreateDirectory(Folder);
            string path = $"{Folder}/{name}.png";
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(shape(x + 0.5f, y + 0.5f)) * 255f);
                pixels[y * Size + x] = new Color32(255, 255, 255, a);
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
