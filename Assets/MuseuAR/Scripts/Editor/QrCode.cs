using System;
using System.Collections.Generic;
using System.Text;

namespace MuseuAR.EditorTools
{
    /// <summary>
    /// Gerador de QR code (ISO/IEC 18004) sem dependências: modo byte (UTF-8), correção de erro nível M,
    /// versões 1 a 10 (até ~200 caracteres). Baseado no algoritmo de referência de Project Nayuki.
    /// Usado só no editor, para desenhar o QR das placas AR.
    /// </summary>
    public static class QrCode
    {
        const int MinVersion = 1, MaxVersion = 10;

        // Nível M, versões 1..10 (índice 0 = versão 1)
        static readonly int[] EccCodewordsPerBlock = { 10, 16, 26, 18, 24, 16, 18, 22, 22, 26 };
        static readonly int[] NumErrorCorrectionBlocks = { 1, 1, 1, 2, 2, 4, 4, 4, 5, 5 };
        const int FormatBitsM = 0;

        /// <summary>Retorna a matriz do QR: [y, x], true = módulo escuro. Não inclui a zona de silêncio.</summary>
        public static bool[,] Encode(string text)
        {
            byte[] data = Encoding.UTF8.GetBytes(text ?? "");

            int version = MinVersion;
            for (; version <= MaxVersion; version++)
                if (4 + CharCountBits(version) + data.Length * 8 <= DataCodewords(version) * 8) break;
            if (version > MaxVersion)
                throw new ArgumentException($"Texto longo demais para o QR ({data.Length} bytes). Use um link mais curto.");

            // ---------- Bits de dados ----------
            var bits = new List<bool>();
            AppendBits(bits, 0b0100, 4);                          // modo byte
            AppendBits(bits, data.Length, CharCountBits(version));
            foreach (byte b in data) AppendBits(bits, b, 8);

            int capacity = DataCodewords(version) * 8;
            AppendBits(bits, 0, Math.Min(4, capacity - bits.Count)); // terminador
            AppendBits(bits, 0, (8 - bits.Count % 8) % 8);
            for (int pad = 0xEC; bits.Count < capacity; pad ^= 0xEC ^ 0x11)
                AppendBits(bits, pad, 8);

            var codewords = new byte[bits.Count / 8];
            for (int i = 0; i < bits.Count; i++)
                if (bits[i]) codewords[i >> 3] |= (byte)(1 << (7 - (i & 7)));

            // ---------- Matriz ----------
            var qr = new Matrix(version);
            qr.DrawFunctionPatterns();
            qr.DrawCodewords(AddEccAndInterleave(codewords, version));

            int bestMask = 0, bestPenalty = int.MaxValue;
            for (int mask = 0; mask < 8; mask++)
            {
                qr.ApplyMask(mask);
                qr.DrawFormatBits(mask);
                int penalty = qr.Penalty();
                if (penalty < bestPenalty) { bestPenalty = penalty; bestMask = mask; }
                qr.ApplyMask(mask); // desfaz (XOR)
            }
            qr.ApplyMask(bestMask);
            qr.DrawFormatBits(bestMask);
            return qr.Modules;
        }

        static int CharCountBits(int version) => version <= 9 ? 8 : 16;

        static int RawDataModules(int version)
        {
            int result = (16 * version + 128) * version + 64;
            if (version >= 2)
            {
                int numAlign = version / 7 + 2;
                result -= (25 * numAlign - 10) * numAlign - 55;
                if (version >= 7) result -= 36;
            }
            return result;
        }

        static int DataCodewords(int version) =>
            RawDataModules(version) / 8 - EccCodewordsPerBlock[version - 1] * NumErrorCorrectionBlocks[version - 1];

        static void AppendBits(List<bool> bits, int value, int length)
        {
            for (int i = length - 1; i >= 0; i--) bits.Add(((value >> i) & 1) != 0);
        }

        // ---------- Reed-Solomon ----------

        static byte[] AddEccAndInterleave(byte[] data, int version)
        {
            int numBlocks = NumErrorCorrectionBlocks[version - 1];
            int eccLen = EccCodewordsPerBlock[version - 1];
            int rawCodewords = RawDataModules(version) / 8;
            int numShortBlocks = numBlocks - rawCodewords % numBlocks;
            int shortBlockLen = rawCodewords / numBlocks;

            byte[] divisor = ReedSolomonDivisor(eccLen);
            var blocks = new List<byte[]>();
            for (int i = 0, k = 0; i < numBlocks; i++)
            {
                int datLen = shortBlockLen - eccLen + (i < numShortBlocks ? 0 : 1);
                var dat = new byte[datLen];
                Array.Copy(data, k, dat, 0, datLen);
                k += datLen;
                byte[] ecc = ReedSolomonRemainder(dat, divisor);

                var block = new byte[shortBlockLen + 1];
                Array.Copy(dat, block, datLen);
                // blocos curtos ganham um byte "vazio" antes do ECC, ignorado na intercalação
                Array.Copy(ecc, 0, block, shortBlockLen + 1 - eccLen, eccLen);
                blocks.Add(block);
            }

            var result = new List<byte>(rawCodewords);
            for (int i = 0; i < blocks[0].Length; i++)
                for (int j = 0; j < blocks.Count; j++)
                    if (i != shortBlockLen - eccLen || j >= numShortBlocks)
                        result.Add(blocks[j][i]);
            return result.ToArray();
        }

        static byte[] ReedSolomonDivisor(int degree)
        {
            var result = new byte[degree];
            result[degree - 1] = 1;
            int root = 1;
            for (int i = 0; i < degree; i++)
            {
                for (int j = 0; j < degree; j++)
                {
                    result[j] = Multiply(result[j], root);
                    if (j + 1 < degree) result[j] ^= result[j + 1];
                }
                root = Multiply(root, 0x02);
            }
            return result;
        }

        static byte[] ReedSolomonRemainder(byte[] data, byte[] divisor)
        {
            var result = new byte[divisor.Length];
            foreach (byte b in data)
            {
                int factor = b ^ result[0];
                Array.Copy(result, 1, result, 0, result.Length - 1);
                result[result.Length - 1] = 0;
                for (int i = 0; i < result.Length; i++)
                    result[i] ^= Multiply(divisor[i], factor);
            }
            return result;
        }

        static byte Multiply(int x, int y)
        {
            int z = 0;
            for (int i = 7; i >= 0; i--)
            {
                z = (z << 1) ^ ((z >> 7) * 0x11D);
                z ^= ((y >> i) & 1) * x;
            }
            return (byte)z;
        }

        // ---------- Desenho da matriz ----------

        class Matrix
        {
            public readonly bool[,] Modules;
            readonly bool[,] isFunction;
            readonly int version, size;

            public Matrix(int version)
            {
                this.version = version;
                size = version * 4 + 17;
                Modules = new bool[size, size];
                isFunction = new bool[size, size];
            }

            void Set(int x, int y, bool dark)
            {
                Modules[y, x] = dark;
                isFunction[y, x] = true;
            }

            public void DrawFunctionPatterns()
            {
                for (int i = 0; i < size; i++)
                {
                    Set(6, i, i % 2 == 0);   // linhas de sincronismo
                    Set(i, 6, i % 2 == 0);
                }

                DrawFinder(3, 3);
                DrawFinder(size - 4, 3);
                DrawFinder(3, size - 4);

                int[] align = AlignmentPositions();
                int n = align.Length;
                for (int i = 0; i < n; i++)
                    for (int j = 0; j < n; j++)
                        if (!(i == 0 && j == 0 || i == 0 && j == n - 1 || i == n - 1 && j == 0))
                            DrawAlignment(align[i], align[j]);

                DrawFormatBits(0); // reserva a área; o valor real é escrito depois
                DrawVersion();
            }

            void DrawFinder(int x, int y)
            {
                for (int dy = -4; dy <= 4; dy++)
                for (int dx = -4; dx <= 4; dx++)
                {
                    int dist = Math.Max(Math.Abs(dx), Math.Abs(dy));
                    int xx = x + dx, yy = y + dy;
                    if (xx >= 0 && xx < size && yy >= 0 && yy < size)
                        Set(xx, yy, dist != 2 && dist != 4);
                }
            }

            void DrawAlignment(int x, int y)
            {
                for (int dy = -2; dy <= 2; dy++)
                for (int dx = -2; dx <= 2; dx++)
                    Set(x + dx, y + dy, Math.Max(Math.Abs(dx), Math.Abs(dy)) != 1);
            }

            int[] AlignmentPositions()
            {
                if (version == 1) return new int[0];
                int numAlign = version / 7 + 2;
                int step = (version * 8 + numAlign * 3 + 5) / (numAlign * 4 - 4) * 2;
                var result = new int[numAlign];
                result[0] = 6;
                for (int i = numAlign - 1, pos = size - 7; i >= 1; i--, pos -= step)
                    result[i] = pos;
                return result;
            }

            public void DrawFormatBits(int mask)
            {
                int data = FormatBitsM << 3 | mask;
                int rem = data;
                for (int i = 0; i < 10; i++) rem = (rem << 1) ^ ((rem >> 9) * 0x537);
                int bits = (data << 10 | rem) ^ 0x5412;

                for (int i = 0; i <= 5; i++) Set(8, i, Bit(bits, i));
                Set(8, 7, Bit(bits, 6));
                Set(8, 8, Bit(bits, 7));
                Set(7, 8, Bit(bits, 8));
                for (int i = 9; i < 15; i++) Set(14 - i, 8, Bit(bits, i));

                for (int i = 0; i < 8; i++) Set(size - 1 - i, 8, Bit(bits, i));
                for (int i = 8; i < 15; i++) Set(8, size - 15 + i, Bit(bits, i));
                Set(8, size - 8, true); // módulo escuro fixo
            }

            void DrawVersion()
            {
                if (version < 7) return;
                int rem = version;
                for (int i = 0; i < 12; i++) rem = (rem << 1) ^ ((rem >> 11) * 0x1F25);
                int bits = version << 12 | rem;
                for (int i = 0; i < 18; i++)
                {
                    bool bit = Bit(bits, i);
                    int a = size - 11 + i % 3, b = i / 3;
                    Set(a, b, bit);
                    Set(b, a, bit);
                }
            }

            public void DrawCodewords(byte[] data)
            {
                int i = 0;
                for (int right = size - 1; right >= 1; right -= 2)
                {
                    if (right == 6) right = 5;
                    for (int vert = 0; vert < size; vert++)
                    for (int j = 0; j < 2; j++)
                    {
                        int x = right - j;
                        bool upward = ((right + 1) & 2) == 0;
                        int y = upward ? size - 1 - vert : vert;
                        if (!isFunction[y, x] && i < data.Length * 8)
                        {
                            Modules[y, x] = Bit(data[i >> 3], 7 - (i & 7));
                            i++;
                        }
                    }
                }
            }

            public void ApplyMask(int mask)
            {
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    if (isFunction[y, x]) continue;
                    bool invert;
                    switch (mask)
                    {
                        case 0: invert = (x + y) % 2 == 0; break;
                        case 1: invert = y % 2 == 0; break;
                        case 2: invert = x % 3 == 0; break;
                        case 3: invert = (x + y) % 3 == 0; break;
                        case 4: invert = (x / 3 + y / 2) % 2 == 0; break;
                        case 5: invert = x * y % 2 + x * y % 3 == 0; break;
                        case 6: invert = (x * y % 2 + x * y % 3) % 2 == 0; break;
                        default: invert = ((x + y) % 2 + x * y % 3) % 2 == 0; break;
                    }
                    if (invert) Modules[y, x] = !Modules[y, x];
                }
            }

            /// <summary>Penalidade da norma (regras 1–4), usada para escolher a máscara mais legível.</summary>
            public int Penalty()
            {
                int result = 0;

                // Regra 1: sequências de 5+ módulos iguais em linha/coluna
                for (int pass = 0; pass < 2; pass++)
                for (int a = 0; a < size; a++)
                {
                    int run = 1;
                    for (int b = 1; b < size; b++)
                    {
                        bool prev = pass == 0 ? Modules[a, b - 1] : Modules[b - 1, a];
                        bool cur = pass == 0 ? Modules[a, b] : Modules[b, a];
                        if (cur == prev)
                        {
                            run++;
                            if (run == 5) result += 3;
                            else if (run > 5) result++;
                        }
                        else run = 1;
                    }
                }

                // Regra 2: blocos 2x2 da mesma cor
                for (int y = 0; y < size - 1; y++)
                for (int x = 0; x < size - 1; x++)
                {
                    bool c = Modules[y, x];
                    if (c == Modules[y, x + 1] && c == Modules[y + 1, x] && c == Modules[y + 1, x + 1]) result += 3;
                }

                // Regra 3: padrões parecidos com o localizador (1011101 + 4 claros)
                for (int pass = 0; pass < 2; pass++)
                for (int a = 0; a < size; a++)
                for (int b = 0; b + 11 <= size; b++)
                {
                    if (MatchesFinderLike(pass, a, b, false) || MatchesFinderLike(pass, a, b, true)) result += 40;
                }

                // Regra 4: equilíbrio entre claros e escuros
                int dark = 0;
                foreach (bool m in Modules) if (m) dark++;
                int total = size * size;
                int k = (Math.Abs(dark * 20 - total * 10) + total - 1) / total - 1;
                result += Math.Max(0, k) * 10;
                return result;
            }

            static readonly bool[] FinderLike = { true, false, true, true, true, false, true, false, false, false, false };

            bool MatchesFinderLike(int pass, int a, int b, bool reversed)
            {
                for (int i = 0; i < 11; i++)
                {
                    bool expected = FinderLike[reversed ? 10 - i : i];
                    bool m = pass == 0 ? Modules[a, b + i] : Modules[b + i, a];
                    if (m != expected) return false;
                }
                return true;
            }

            static bool Bit(int value, int i) => ((value >> i) & 1) != 0;
        }
    }
}
