using System.IO;
using UnityEngine;

namespace FluxLab
{
    /// <summary>
    /// Dibuja a Magnus con el pelo y la túnica guardados en el vestidor.
    /// </summary>
    public static class AspectoMagnus
    {
        static readonly Color[] Pelos =
        {
            new Color(0f, 0f, 0f, 0f),
            Hex("#8a5a32"),
            Hex("#e2b34a"),
            Hex("#c4483a"),
            Hex("#e4ddd4")
        };

        static readonly Color[] Tunicas =
        {
            Hex("#3c3842"),
            Hex("#d64545"),
            Hex("#3a78d8"),
            Hex("#e2b53a"),
            Hex("#3ea86a"),
            Hex("#e07a2a")
        };

        public static Texture2D Crear()
        {
            Texture2D origen = Leer("Personaje/magnus.png");
            if (origen == null)
                return null;

            int pelo = Mathf.Clamp(AjustesJuego.Pelo, 0, Pelos.Length - 1);
            int color = Mathf.Clamp(AjustesJuego.ColorTunica, 0, Tunicas.Length - 1);
            bool teñirPelo = Pelos[pelo].a > 0.2f;
            bool teñirRopa = color > 0;
            if (!teñirPelo && !teñirRopa)
                return origen;

            return Recolorear(origen, Pelos[pelo], teñirPelo, Tunicas[color], teñirRopa);
        }

        static Texture2D Leer(string relativo)
        {
            string ruta = Path.Combine(Application.streamingAssetsPath, relativo);
            if (!File.Exists(ruta))
                return null;

            var textura = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            textura.LoadImage(File.ReadAllBytes(ruta));
            textura.filterMode = FilterMode.Bilinear;
            return textura;
        }

        static Texture2D Recolorear(Texture2D origen, Color peloColor, bool teñirPelo, Color tunicaColor, bool teñirRopa)
        {
            Color[] pixeles = origen.GetPixels();
            int ancho = origen.width;
            int alto = origen.height;
            int total = pixeles.Length;
            var tela = new float[total];
            var cabello = new float[total];
            var libre = new bool[total];

            for (int i = 0; i < total; i++)
            {
                Color pixel = pixeles[i];
                if (pixel.a < 0.12f)
                    continue;
                float luma = pixel.r * 0.3f + pixel.g * 0.59f + pixel.b * 0.11f;
                float max = Mathf.Max(pixel.r, Mathf.Max(pixel.g, pixel.b));
                float min = Mathf.Min(pixel.r, Mathf.Min(pixel.g, pixel.b));
                float sat = max <= 0.001f ? 0f : (max - min) / max;
                float desdeAbajo = ((i / ancho) + 0.5f) / alto;
                bool piel = pixel.r > 0.62f && pixel.g > 0.45f && pixel.r > pixel.b + 0.04f;
                bool gafas = pixel.b > pixel.r + 0.05f && pixel.g > 0.25f;
                bool zapatos = desdeAbajo < 0.11f && pixel.r > pixel.g + 0.03f && pixel.r > pixel.b;
                if (piel || gafas || zapatos || luma < 0.08f || sat > 0.34f)
                    continue;

                libre[i] = true;
                if (teñirPelo && desdeAbajo > 0.68f)
                    cabello[i] = 1f;
                else if (teñirRopa && desdeAbajo > 0.10f && desdeAbajo <= 0.68f)
                    tela[i] = 1f;
            }

            for (int paso = 0; paso < 2; paso++)
            {
                float[] telaSuave = (float[])tela.Clone();
                float[] peloSuave = (float[])cabello.Clone();
                for (int y = 1; y < alto - 1; y++)
                {
                    for (int x = 1; x < ancho - 1; x++)
                    {
                        int i = y * ancho + x;
                        if (!libre[i])
                            continue;
                        float sumaTela = 0f;
                        float sumaPelo = 0f;
                        int cuenta = 0;
                        for (int oy = -1; oy <= 1; oy++)
                        {
                            for (int ox = -1; ox <= 1; ox++)
                            {
                                int j = (y + oy) * ancho + (x + ox);
                                if (!libre[j])
                                    continue;
                                sumaTela += tela[j];
                                sumaPelo += cabello[j];
                                cuenta++;
                            }
                        }

                        if (cuenta == 0)
                            continue;
                        telaSuave[i] = sumaTela / cuenta;
                        peloSuave[i] = sumaPelo / cuenta;
                    }
                }

                tela = telaSuave;
                cabello = peloSuave;
            }

            for (int i = 0; i < total; i++)
            {
                float peso = Mathf.Max(tela[i], cabello[i]);
                if (peso < 0.12f)
                    continue;
                Color pixel = pixeles[i];
                float luma = pixel.r * 0.3f + pixel.g * 0.59f + pixel.b * 0.11f;
                bool esPelo = cabello[i] >= tela[i];
                Color tinte = esPelo ? peloColor : tunicaColor;
                float brillo = Mathf.Clamp(luma / (esPelo ? 0.22f : 0.24f), 0.32f, 1.08f);
                Color destino = Color.Lerp(pixel, tinte * brillo, peso * (esPelo ? 0.82f : 0.92f));
                destino.a = pixel.a;
                pixeles[i] = destino;
            }

            var copia = new Texture2D(ancho, alto, TextureFormat.RGBA32, false);
            copia.SetPixels(pixeles);
            copia.Apply();
            copia.filterMode = FilterMode.Bilinear;
            return copia;
        }

        static Color Hex(string value)
        {
            ColorUtility.TryParseHtmlString(value, out Color color);
            return color;
        }
    }
}
