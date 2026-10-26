using UnityEngine;

namespace FluxLab
{
    /// <summary>
    /// Dibuja a Mishi, un gatito negro sentado, con varias caras.
    /// </summary>
    public static class RetratoMishi
    {
        public enum Cara
        {
            Quieto,
            Curioso,
            Habla,
            Feliz,
            Sorpresa,
            Guino
        }

        static readonly Color32 Transparente = new Color32(0, 0, 0, 0);
        static readonly Color32 Pelaje = new Color32(32, 30, 36, 255);
        static readonly Color32 Sombra = new Color32(18, 16, 22, 255);
        static readonly Color32 Rosa = new Color32(236, 156, 170, 255);
        static readonly Color32 Hocico = new Color32(246, 228, 214, 255);
        static readonly Color32 Ojo = new Color32(255, 250, 236, 255);
        static readonly Color32 Pupila = new Color32(22, 18, 28, 255);
        static readonly Color32 Brillo = new Color32(255, 255, 255, 255);
        static readonly Color32 Bigote = new Color32(232, 220, 206, 255);

        public static void Pintar(Texture2D textura, Cara cara)
        {
            int lado = textura.width;
            var pixeles = new Color32[lado * lado];
            for (int i = 0; i < pixeles.Length; i++)
                pixeles[i] = Transparente;

            Elipse(pixeles, lado, 168, 108, 22, 16, Pelaje);
            Elipse(pixeles, lado, 188, 132, 18, 14, Pelaje);
            Elipse(pixeles, lado, 204, 154, 14, 12, Pelaje);

            Elipse(pixeles, lado, 128, 96, 74, 48, Pelaje);
            Elipse(pixeles, lado, 128, 78, 78, 22, Sombra);
            Elipse(pixeles, lado, 96, 58, 22, 14, Pelaje);
            Elipse(pixeles, lado, 160, 58, 22, 14, Pelaje);

            Elipse(pixeles, lado, 128, 168, 62, 56, Pelaje);
            Triangulo(pixeles, lado, 78, 196, 108, 196, 86, 246, Pelaje);
            Triangulo(pixeles, lado, 148, 196, 178, 196, 170, 246, Pelaje);
            Triangulo(pixeles, lado, 86, 198, 104, 198, 92, 232, Rosa);
            Triangulo(pixeles, lado, 154, 198, 172, 198, 166, 232, Rosa);

            Elipse(pixeles, lado, 128, 150, 34, 26, Hocico);

            int altoOjo = cara == Cara.Feliz ? 6 : cara == Cara.Sorpresa ? 20 : 16;
            int anchoOjo = cara == Cara.Sorpresa ? 16 : 14;
            int mirada = cara == Cara.Curioso ? 4 : 0;
            bool guiño = cara == Cara.Guino;

            OjoDe(pixeles, lado, 108, 172, anchoOjo, guiño ? 4 : altoOjo, mirada, !guiño);
            OjoDe(pixeles, lado, 148, 172, anchoOjo, altoOjo, mirada, true);

            Elipse(pixeles, lado, 128, 156, 7, 5, Rosa);

            if (cara == Cara.Habla)
                Elipse(pixeles, lado, 128, 142, 8, 7, Sombra);
            else if (cara == Cara.Sorpresa)
                Elipse(pixeles, lado, 128, 142, 6, 6, Sombra);
            else
            {
                Linea(pixeles, lado, 120, 146, 128, 140, Rosa);
                Linea(pixeles, lado, 136, 146, 128, 140, Rosa);
            }

            Bigotes(pixeles, lado);
            textura.SetPixels32(pixeles);
            textura.Apply();
        }

        static void OjoDe(Color32[] pixeles, int lado, int x, int y, int ancho, int alto, int mirada, bool abierto)
        {
            if (!abierto || alto < 8)
            {
                Linea(pixeles, lado, x - 12, y, x + 12, y + 3, Ojo);
                return;
            }

            Elipse(pixeles, lado, x, y, ancho, alto, Ojo);
            Elipse(pixeles, lado, x + mirada, y - 2, 6, 8, Pupila);
            Elipse(pixeles, lado, x + mirada + 3, y + 3, 2, 2, Brillo);
        }

        static void Bigotes(Color32[] pixeles, int lado)
        {
            Linea(pixeles, lado, 96, 154, 58, 160, Bigote);
            Linea(pixeles, lado, 96, 148, 54, 148, Bigote);
            Linea(pixeles, lado, 160, 154, 198, 160, Bigote);
            Linea(pixeles, lado, 160, 148, 202, 148, Bigote);
        }

        static void Elipse(Color32[] pixeles, int lado, int cx, int cy, int rx, int ry, Color32 color)
        {
            int x0 = Mathf.Max(0, cx - rx);
            int x1 = Mathf.Min(lado - 1, cx + rx);
            int y0 = Mathf.Max(0, cy - ry);
            int y1 = Mathf.Min(lado - 1, cy + ry);
            float rx2 = rx * rx;
            float ry2 = ry * ry;
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    if (dx * dx / rx2 + dy * dy / ry2 <= 1f)
                        pixeles[y * lado + x] = color;
                }
            }
        }

        static void Triangulo(Color32[] pixeles, int lado, int x0, int y0, int x1, int y1, int x2, int y2, Color32 color)
        {
            int minX = Mathf.Clamp(Mathf.Min(x0, Mathf.Min(x1, x2)), 0, lado - 1);
            int maxX = Mathf.Clamp(Mathf.Max(x0, Mathf.Max(x1, x2)), 0, lado - 1);
            int minY = Mathf.Clamp(Mathf.Min(y0, Mathf.Min(y1, y2)), 0, lado - 1);
            int maxY = Mathf.Clamp(Mathf.Max(y0, Mathf.Max(y1, y2)), 0, lado - 1);
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (Dentro(x, y, x0, y0, x1, y1, x2, y2))
                        pixeles[y * lado + x] = color;
                }
            }
        }

        static bool Dentro(int x, int y, int x0, int y0, int x1, int y1, int x2, int y2)
        {
            int a = (x1 - x0) * (y - y0) - (y1 - y0) * (x - x0);
            int b = (x2 - x1) * (y - y1) - (y2 - y1) * (x - x1);
            int c = (x0 - x2) * (y - y2) - (y0 - y2) * (x - x2);
            return (a >= 0 && b >= 0 && c >= 0) || (a <= 0 && b <= 0 && c <= 0);
        }

        static void Linea(Color32[] pixeles, int lado, int x0, int y0, int x1, int y1, Color32 color)
        {
            int pasos = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
            for (int i = 0; i <= pasos; i++)
            {
                float t = pasos == 0 ? 0f : i / (float)pasos;
                int x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t));
                int y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));
                Elipse(pixeles, lado, x, y, 1, 1, color);
            }
        }
    }
}
