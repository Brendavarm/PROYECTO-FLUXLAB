using System.Collections.Generic;
using UnityEngine;

namespace FluxLab.Nivel2
{
    /// <summary>
    /// Imágenes propias del nivel 2 (Resources/FluxLab/NIVEL2-IMAGENES), preparadas al cargarlas:
    ///  - FRAZCOS: JPG con fondo rosado; se borra el fondo y el borde claro tipo sticker.
    ///  - figuras: las figuras de los ingredientes y las burbujas, JPG con fondo blanco.
    ///    Reemplazan SOLO en este nivel a gem_*.png y burbuja_*.png (las de Brenda no se tocan).
    /// Los archivos originales no se modifican.
    /// </summary>
    public static class ImagenesFrascos
    {
        const string Carpeta = "FluxLab/NIVEL2-IMAGENES/FRAZCOS/";
        const string CarpetaFiguras = "FluxLab/NIVEL2-IMAGENES/figuras/";

        /// <summary>Cómo se quita el fondo de cada imagen.</summary>
        enum Fondo
        {
            Recortar,      // fondo de un color (rosado o blanco): se borra desde las orillas
            Transparentar  // burbujas: lo que se parece al fondo se vuelve transparente en proporción
        }

        static readonly Dictionary<string, Sprite> guardados = new Dictionary<string, Sprite>();

        public static Sprite DeIngrediente(IngredientKind tipo) => Cargar(Archivo(tipo));

        /// <summary>Frasco de vida (los que se rompen).</summary>
        public static Sprite Vida => Cargar("FrazcoRosa");

        /// <summary>Poción que se gana en la pantalla de resultados.</summary>
        public static Sprite Premio => Cargar("FrazcoUnicornio");

        /// <summary>Frasco que lleva Magnus si eligió ese accesorio.</summary>
        public static Sprite Accesorio => Cargar("FrazcoLila");

        /// <summary>
        /// Gato Mishi en alta calidad, si alguien pone una imagen "Mishi" en NIVEL2-IMAGENES/MISHI.
        /// Si no existe devuelve null y se usa el sprite FluxLab/mishi de siempre.
        /// </summary>
        public static Sprite Mishi => CargarDe("FluxLab/NIVEL2-IMAGENES/MISHI/", "Mishi", false);

        /// <summary>
        /// Figura o burbuja nueva que reemplaza, solo en este nivel, a un sprite de FluxLab
        /// (gem_red_circle, burbuja_a...). Devuelve null si no hay reemplazo: entonces se usa el original.
        /// </summary>
        public static Sprite Figura(string nombreOriginal)
        {
            string archivo = ArchivoFigura(nombreOriginal);
            if (archivo == null)
                return null;
            bool burbuja = nombreOriginal.StartsWith("burbuja");
            return CargarDe(CarpetaFiguras, archivo, false, burbuja ? Fondo.Transparentar : Fondo.Recortar);
        }

        /// <summary>Racimo de burbujas (sube de vez en cuando por el fondo).</summary>
        public static Sprite BurbujasGrupo => CargarDe(CarpetaFiguras, "BurbujasGrupo", false, Fondo.Transparentar);

        static string ArchivoFigura(string nombreOriginal)
        {
            switch (nombreOriginal)
            {
                case "gem_red_circle": return "CirculoRojo";
                case "gem_blue_triangle": return "TrianguloAzul";
                case "gem_yellow_star": return "EstrellaAmarillo";
                case "gem_green_square": return "CuadradoVerde";
                case "gem_purple_pentagon": return "PentagonoLila";
                case "gem_orange_crescent": return "LunaCobre";
                case "burbuja_a":
                case "burbuja_b":
                case "burbuja_c": return "Burbuja";
                default: return null;
            }
        }

        public static string Archivo(IngredientKind tipo)
        {
            switch (tipo)
            {
                case IngredientKind.Fuego: return "FrazcoRojo";
                case IngredientKind.Gota: return "FrazcoAzul";
                case IngredientKind.Polvo: return "FrazcoAmarillo";
                case IngredientKind.Hierba: return "FrazcoVerde";
                case IngredientKind.Cristal: return "FrazcoLila";
                default: return "FrazcoNaranja";
            }
        }

        static Sprite Cargar(string nombre) => CargarDe(Carpeta, nombre, true);

        static Sprite CargarDe(string carpeta, string nombre, bool avisar, Fondo fondo = Fondo.Recortar)
        {
            if (guardados.TryGetValue(carpeta + nombre, out Sprite listo))
                return listo;

            var original = Resources.Load<Texture2D>(carpeta + nombre);
            if (original == null)
            {
                if (avisar)
                    Debug.LogWarning("No se encontró la imagen " + carpeta + nombre);
                guardados[carpeta + nombre] = null;
                return null;
            }

            int ancho = original.width;
            int alto = original.height;
            Color32[] pixeles = LeerPixeles(original);
            if (fondo == Fondo.Transparentar)
            {
                pixeles = Transparentar(pixeles, ancho, alto);
            }
            else
            {
                pixeles = QuitarFondo(pixeles, ancho, alto);
                QuedarseConLoMasGrande(pixeles, ancho, alto);   // borra rayitas o manchas sueltas
            }
            RectInt caja = Recorte(pixeles, ancho, alto);

            var textura = new Texture2D(caja.width, caja.height, TextureFormat.RGBA32, false);
            var recortados = new Color32[caja.width * caja.height];
            for (int y = 0; y < caja.height; y++)
                System.Array.Copy(pixeles, (caja.y + y) * ancho + caja.x, recortados, y * caja.width, caja.width);
            textura.SetPixels32(recortados);
            textura.Apply();
            textura.filterMode = FilterMode.Bilinear;
            textura.wrapMode = TextureWrapMode.Clamp;

            var sprite = Sprite.Create(textura, new Rect(0f, 0f, caja.width, caja.height), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = nombre;
            guardados[carpeta + nombre] = sprite;
            return sprite;
        }

        /// <summary>
        /// Para las burbujas: lo que se parece al color del fondo (promedio de las orillas) se vuelve transparente;
        /// lo distinto (borde tornasol, brillos) se queda con su color, un poco más claro. Las orillas de la imagen
        /// se apagan para que no quede marcado el rectángulo.
        /// </summary>
        static Color32[] Transparentar(Color32[] pixeles, int ancho, int alto)
        {
            float fr = 0f, fg = 0f, fb = 0f;
            int cuenta = 0;
            for (int x = 0; x < ancho; x++)
            {
                foreach (int i in new[] { x, (alto - 1) * ancho + x })
                {
                    fr += pixeles[i].r; fg += pixeles[i].g; fb += pixeles[i].b; cuenta++;
                }
            }

            for (int y = 0; y < alto; y++)
            {
                foreach (int i in new[] { y * ancho, y * ancho + ancho - 1 })
                {
                    fr += pixeles[i].r; fg += pixeles[i].g; fb += pixeles[i].b; cuenta++;
                }
            }

            fr /= cuenta; fg /= cuenta; fb /= cuenta;
            var salida = new Color32[pixeles.Length];
            float margen = Mathf.Min(ancho, alto) * 0.08f;
            for (int i = 0; i < pixeles.Length; i++)
            {
                Color32 p = pixeles[i];
                float diferencia = (Mathf.Abs(p.r - fr) + Mathf.Abs(p.g - fg) + Mathf.Abs(p.b - fb)) / (3f * 255f);
                float alfa = Mathf.Clamp01((diferencia - 0.03f) * 7f);
                int x = i % ancho;
                int y = i / ancho;
                float orilla = Mathf.Min(Mathf.Min(x, ancho - 1 - x), Mathf.Min(y, alto - 1 - y));
                alfa *= Mathf.Clamp01(orilla / margen);
                salida[i] = new Color32(
                    (byte)Mathf.Min(255f, p.r * 1.08f + 12f),
                    (byte)Mathf.Min(255f, p.g * 1.08f + 12f),
                    (byte)Mathf.Min(255f, p.b * 1.08f + 12f),
                    (byte)(alfa * 255f));
            }

            return salida;
        }

        /// <summary>Deja solo la mancha visible más grande (la figura); borra rayitas o puntos sueltos.</summary>
        static void QuedarseConLoMasGrande(Color32[] pixeles, int ancho, int alto)
        {
            var zona = new int[pixeles.Length];
            var pendientes = new Queue<int>();
            var tamanos = new List<int> { 0 };
            for (int inicio = 0; inicio < pixeles.Length; inicio++)
            {
                if (zona[inicio] != 0 || pixeles[inicio].a < 40)
                    continue;
                int numero = tamanos.Count;
                int tamano = 0;
                zona[inicio] = numero;
                pendientes.Enqueue(inicio);
                while (pendientes.Count > 0)
                {
                    int i = pendientes.Dequeue();
                    tamano++;
                    int x = i % ancho;
                    int y = i / ancho;
                    if (x > 0) Unir(i - 1);
                    if (x < ancho - 1) Unir(i + 1);
                    if (y > 0) Unir(i - ancho);
                    if (y < alto - 1) Unir(i + ancho);
                }
                tamanos.Add(tamano);

                void Unir(int j)
                {
                    if (zona[j] != 0 || pixeles[j].a < 40)
                        return;
                    zona[j] = numero;
                    pendientes.Enqueue(j);
                }
            }

            int mayor = 0;
            for (int n = 1; n < tamanos.Count; n++)
            {
                if (tamanos[n] > tamanos[mayor])
                    mayor = n;
            }

            for (int i = 0; i < pixeles.Length; i++)
            {
                if (zona[i] != mayor || mayor == 0)
                    pixeles[i] = new Color32(pixeles[i].r, pixeles[i].g, pixeles[i].b, zona[i] == 0 ? pixeles[i].a : (byte)0);
            }
        }

        // Si la textura no es legible se copia por la GPU, así funciona aunque cambie el importador.
        public static Color32[] LeerPixeles(Texture2D textura)
        {
            if (textura.isReadable)
                return textura.GetPixels32();

            var temporal = RenderTexture.GetTemporary(textura.width, textura.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(textura, temporal);
            RenderTexture anterior = RenderTexture.active;
            RenderTexture.active = temporal;
            var copia = new Texture2D(textura.width, textura.height, TextureFormat.RGBA32, false);
            copia.ReadPixels(new Rect(0, 0, textura.width, textura.height), 0, 0);
            copia.Apply();
            RenderTexture.active = anterior;
            RenderTexture.ReleaseTemporary(temporal);
            Color32[] pixeles = copia.GetPixels32();
            Object.Destroy(copia);
            return pixeles;
        }

        /// <summary>
        /// Relleno desde los bordes: borra el rosado del fondo y el borde claro del sticker.
        /// Se detiene en el contorno oscuro del frasco, así el vidrio de adentro no se toca.
        /// </summary>
        public static Color32[] QuitarFondo(Color32[] pixeles, int ancho, int alto)
        {
            Color32 fondo = Promedio(pixeles, ancho, alto);
            var esFondo = new bool[pixeles.Length];
            var pendientes = new Queue<int>();

            for (int x = 0; x < ancho; x++)
            {
                Sembrar(x, pixeles, fondo, esFondo, pendientes);
                Sembrar((alto - 1) * ancho + x, pixeles, fondo, esFondo, pendientes);
            }

            for (int y = 0; y < alto; y++)
            {
                Sembrar(y * ancho, pixeles, fondo, esFondo, pendientes);
                Sembrar(y * ancho + ancho - 1, pixeles, fondo, esFondo, pendientes);
            }

            while (pendientes.Count > 0)
            {
                int i = pendientes.Dequeue();
                int x = i % ancho;
                int y = i / ancho;
                if (x > 0) Sembrar(i - 1, pixeles, fondo, esFondo, pendientes);
                if (x < ancho - 1) Sembrar(i + 1, pixeles, fondo, esFondo, pendientes);
                if (y > 0) Sembrar(i - ancho, pixeles, fondo, esFondo, pendientes);
                if (y < alto - 1) Sembrar(i + ancho, pixeles, fondo, esFondo, pendientes);
            }

            var resultado = new Color32[pixeles.Length];
            for (int i = 0; i < pixeles.Length; i++)
            {
                Color32 p = pixeles[i];
                if (esFondo[i])
                {
                    resultado[i] = new Color32(p.r, p.g, p.b, 0);
                    continue;
                }

                // Borde suave: los píxeles pegados al fondo quedan medio transparentes
                // según cuánto se parezcan todavía al fondo.
                int x = i % ancho;
                int y = i / ancho;
                bool orilla = (x > 0 && esFondo[i - 1]) || (x < ancho - 1 && esFondo[i + 1]) ||
                              (y > 0 && esFondo[i - ancho]) || (y < alto - 1 && esFondo[i + ancho]);
                byte alfa = 255;
                if (orilla)
                    alfa = (byte)(255f * Mathf.Clamp01((1f - Luz(p)) / 0.45f));
                resultado[i] = new Color32(p.r, p.g, p.b, alfa);
            }

            return resultado;
        }

        static void Sembrar(int i, Color32[] pixeles, Color32 fondo, bool[] esFondo, Queue<int> pendientes)
        {
            if (esFondo[i] || !PareceFondo(pixeles[i], fondo))
                return;
            esFondo[i] = true;
            pendientes.Enqueue(i);
        }

        static bool PareceFondo(Color32 p, Color32 fondo)
        {
            float dr = (p.r - fondo.r) / 255f;
            float dg = (p.g - fondo.g) / 255f;
            float db = (p.b - fondo.b) / 255f;
            if (dr * dr + dg * dg + db * db < 0.03f)
                return true;
            // borde claro del sticker (menta / blanco)
            return Luz(p) > 0.78f && Saturacion(p) < 0.3f;
        }

        static float Luz(Color32 p) => (p.r * 0.3f + p.g * 0.59f + p.b * 0.11f) / 255f;

        static float Saturacion(Color32 p)
        {
            float max = Mathf.Max(p.r, Mathf.Max(p.g, p.b));
            float min = Mathf.Min(p.r, Mathf.Min(p.g, p.b));
            return max <= 0f ? 0f : (max - min) / max;
        }

        static Color32 Promedio(Color32[] pixeles, int ancho, int alto)
        {
            int[] esquinas = { 0, ancho - 1, (alto - 1) * ancho, alto * ancho - 1, ancho / 2, (alto - 1) * ancho + ancho / 2 };
            int r = 0, g = 0, b = 0;
            foreach (int i in esquinas)
            {
                r += pixeles[i].r;
                g += pixeles[i].g;
                b += pixeles[i].b;
            }

            return new Color32((byte)(r / esquinas.Length), (byte)(g / esquinas.Length), (byte)(b / esquinas.Length), 255);
        }

        static RectInt Recorte(Color32[] pixeles, int ancho, int alto)
        {
            int minX = ancho, minY = alto, maxX = -1, maxY = -1;
            for (int y = 0; y < alto; y++)
            {
                for (int x = 0; x < ancho; x++)
                {
                    if (pixeles[y * ancho + x].a < 8)
                        continue;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }

            if (maxX < 0)
                return new RectInt(0, 0, ancho, alto);
            return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }
    }
}
