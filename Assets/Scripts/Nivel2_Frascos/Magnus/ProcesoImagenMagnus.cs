using System.Collections.Generic;
using UnityEngine;

namespace FluxLab.Nivel2
{
    /// <summary>
    /// Trabajo de píxeles para las poses de Magnus: quitar el fondo gris, recortar y teñir
    /// pelo y túnica con la personalización. No usa nada de la escena (solo colores),
    /// así se puede probar fuera de Unity.
    /// </summary>
    public static class ProcesoImagenMagnus
    {
        // ───────────────────────── Quitar el fondo gris ─────────────────────────

        /// <summary>
        /// El fondo de las poses es gris, casi igual que el pelo y la túnica: por color no se pueden
        /// separar. Lo que los separa es el borde blanco tipo sticker que rodea a Magnus:
        ///  1. Las líneas o barras de marco en las orillas se pintan del gris del fondo.
        ///  2. El borde blanco se engruesa 1 píxel para tapar huequitos del JPG.
        ///  3. Fondo = zona gris que toca MUCHO la orilla de la imagen (el pelo que apenas la toca, no),
        ///     o un hueco (entre brazo y cuerpo) rodeado casi solo de borde blanco.
        /// </summary>
        public static Color32[] QuitarFondo(Color32[] original, int ancho, int alto)
        {
            int total = original.Length;
            var pixeles = (Color32[])original.Clone();
            // Las líneas de marco se marcan aparte: son fondo, pero hacen de pared (si se pintaran de gris
            // harían de puente entre el pelo que las toca y el resto del fondo).
            bool[] marco = BuscarMarcos(pixeles, ancho, alto);

            // Muro = borde blanco del sticker (engrosado) o línea negra de dibujo. Lo demás gris puede ser fondo.
            // Ojo: la piel también es clara, pero tiene color; el borde del sticker es blanco sin color.
            var claro = new bool[total];
            for (int i = 0; i < total; i++)
                claro[i] = Blanco(pixeles[i]);
            var cercaDeClaro = new bool[total];
            for (int i = 0; i < total; i++)
            {
                int x = i % ancho;
                int y = i / ancho;
                cercaDeClaro[i] = claro[i] ||
                    (x > 0 && claro[i - 1]) || (x < ancho - 1 && claro[i + 1]) ||
                    (y > 0 && claro[i - ancho]) || (y < alto - 1 && claro[i + ancho]);
            }

            // Alrededor de la barra de marco (2 píxeles) también es pared: sus bordes grises del JPG
            // harían de puente entre el pelo que la cruza y el fondo.
            var cercaDeMarco = new bool[total];
            for (int i = 0; i < total; i++)
            {
                if (!marco[i])
                    continue;
                int mx = i % ancho;
                int my = i / ancho;
                for (int oy = -2; oy <= 2; oy++)
                {
                    for (int ox = -2; ox <= 2; ox++)
                    {
                        int nx = mx + ox;
                        int ny = my + oy;
                        if (nx >= 0 && ny >= 0 && nx < ancho && ny < alto)
                            cercaDeMarco[ny * ancho + nx] = true;
                    }
                }
            }

            var libre = new bool[total];
            for (int i = 0; i < total; i++)
                libre[i] = Grisaceo(pixeles[i]) && !cercaDeClaro[i] && !cercaDeMarco[i];

            var zona = new bool[total];
            var esFondo = new bool[total];
            var pendientes = new Queue<int>();
            var zonas = new List<(List<int> miembros, int orilla, bool hueco)>();

            for (int inicio = 0; inicio < total; inicio++)
            {
                if (zona[inicio] || !libre[inicio])
                    continue;

                var miembros = new List<int>();
                int contactoOrilla = 0;
                int vecinosClaros = 0;
                int vecinosOtros = 0;   // líneas negras, piel, cinturón... cualquier cosa que no sea borde blanco
                zona[inicio] = true;
                pendientes.Enqueue(inicio);
                while (pendientes.Count > 0)
                {
                    int i = pendientes.Dequeue();
                    miembros.Add(i);
                    int x = i % ancho;
                    int y = i / ancho;
                    if (x == 0 || y == 0 || x == ancho - 1 || y == alto - 1)
                        contactoOrilla++;

                    for (int v = 0; v < 4; v++)
                    {
                        int nx = x + (v == 0 ? -1 : v == 1 ? 1 : 0);
                        int ny = y + (v == 2 ? -1 : v == 3 ? 1 : 0);
                        if (nx < 0 || ny < 0 || nx >= ancho || ny >= alto)
                            continue;
                        int j = ny * ancho + nx;
                        if (libre[j])
                        {
                            if (!zona[j])
                            {
                                zona[j] = true;
                                pendientes.Enqueue(j);
                            }
                        }
                        else if (cercaDeClaro[j])
                            vecinosClaros++;
                        else
                            vecinosOtros++;
                    }
                }

                // Hueco entre brazo y cuerpo: rodeado casi solo de borde blanco. El pelo toca la cara
                // y la ropa toca líneas, manos y cinturón, así que no cuentan como hueco.
                bool hueco = vecinosClaros > 12 && vecinosOtros <= vecinosClaros * 0.08f;
                zonas.Add((miembros, contactoOrilla, hueco));
            }

            // Fondo = la zona que más toca la orilla, y las que la tocan casi tanto como ella
            // (el fondo puede quedar partido en dos). El pelo o la ropa que apenas tocan la orilla no.
            int mayorOrilla = 0;
            foreach (var z in zonas)
                mayorOrilla = Mathf.Max(mayorOrilla, z.orilla);
            foreach (var z in zonas)
            {
                bool orilla = z.orilla > 0 && z.orilla >= mayorOrilla * 0.35f;
                if (orilla || z.hueco)
                {
                    foreach (int i in z.miembros)
                        esFondo[i] = true;
                }
            }

            for (int i = 0; i < total; i++)
                esFondo[i] |= marco[i];
            BorrarLineasSueltas(pixeles, ancho, alto, esFondo);

            // El píxel gris que quedó pegado al borde blanco (por el engrosado) también es fondo si toca fondo.
            for (int i = 0; i < total; i++)
            {
                if (esFondo[i] || claro[i] || !Grisaceo(pixeles[i]))
                    continue;
                int x = i % ancho;
                int y = i / ancho;
                if ((x > 0 && esFondo[i - 1]) || (x < ancho - 1 && esFondo[i + 1]) ||
                    (y > 0 && esFondo[i - ancho]) || (y < alto - 1 && esFondo[i + ancho]))
                    esFondo[i] = true;
            }

            // El borde blanco del sticker se queda; se suaviza un poco la orilla de afuera.
            var resultado = new Color32[total];
            for (int i = 0; i < total; i++)
            {
                Color32 p = original[i];
                if (esFondo[i])
                {
                    resultado[i] = new Color32(p.r, p.g, p.b, 0);
                    continue;
                }

                int x = i % ancho;
                int y = i / ancho;
                int fondoCerca = 0;
                if (x > 0 && esFondo[i - 1]) fondoCerca++;
                if (x < ancho - 1 && esFondo[i + 1]) fondoCerca++;
                if (y > 0 && esFondo[i - ancho]) fondoCerca++;
                if (y < alto - 1 && esFondo[i + ancho]) fondoCerca++;
                byte alfa = (byte)(fondoCerca >= 3 ? 90 : fondoCerca == 2 ? 170 : 255);
                resultado[i] = new Color32(p.r, p.g, p.b, alfa);
            }

            return resultado;
        }

        /// <summary>
        /// Algunas imágenes traen líneas o barras negras de marco en las orillas.
        /// En la franja cercana a cada orilla, las filas/columnas mayormente oscuras son marco.
        /// </summary>
        static bool[] BuscarMarcos(Color32[] pixeles, int ancho, int alto)
        {
            var marco = new bool[pixeles.Length];
            int maxFilas = Mathf.Max(3, (int)(alto * 0.1f));
            int maxColumnas = Mathf.Max(3, (int)(ancho * 0.16f));
            for (int lado = 0; lado < 2; lado++)
            {
                for (int d = 0; d < maxFilas; d++)
                {
                    int y = lado == 0 ? d : alto - 1 - d;
                    if (!LineaDeMarco(pixeles, ancho, y * ancho, 1, ancho))
                        continue;
                    for (int x = 0; x < ancho; x++)
                        marco[y * ancho + x] = Luz(pixeles[y * ancho + x]) < 0.2f;   // solo la barra, no el pelo que la cruza
                }

                for (int d = 0; d < maxColumnas; d++)
                {
                    int x = lado == 0 ? d : ancho - 1 - d;
                    if (!LineaDeMarco(pixeles, ancho, x, ancho, alto))
                        continue;
                    for (int y = 0; y < alto; y++)
                        marco[y * ancho + x] = Luz(pixeles[y * ancho + x]) < 0.2f;
                }
            }

            return marco;
        }

        /// <summary>
        /// Líneas rectas finas que sobran (restos de marco dentro de la imagen): manchas oscuras
        /// de 1 a 4 píxeles de grosor y largas. Las líneas del dibujo están todas unidas al personaje, así que no son finas.
        /// </summary>
        static void BorrarLineasSueltas(Color32[] pixeles, int ancho, int alto, bool[] esFondo)
        {
            var visto = new bool[pixeles.Length];
            var pendientes = new Queue<int>();
            var miembros = new List<int>();
            for (int inicio = 0; inicio < pixeles.Length; inicio++)
            {
                if (visto[inicio] || esFondo[inicio] || Luz(pixeles[inicio]) >= 0.19f)
                    continue;

                miembros.Clear();
                int minX = ancho, minY = alto, maxX = 0, maxY = 0;
                visto[inicio] = true;
                pendientes.Enqueue(inicio);
                while (pendientes.Count > 0)
                {
                    int i = pendientes.Dequeue();
                    miembros.Add(i);
                    int x = i % ancho;
                    int y = i / ancho;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                    for (int v = 0; v < 4; v++)
                    {
                        int nx = x + (v == 0 ? -1 : v == 1 ? 1 : 0);
                        int ny = y + (v == 2 ? -1 : v == 3 ? 1 : 0);
                        if (nx < 0 || ny < 0 || nx >= ancho || ny >= alto)
                            continue;
                        int j = ny * ancho + nx;
                        if (visto[j] || esFondo[j] || Luz(pixeles[j]) >= 0.19f)
                            continue;
                        visto[j] = true;
                        pendientes.Enqueue(j);
                    }
                }

                int w = maxX - minX + 1;
                int h = maxY - minY + 1;
                bool vertical = w <= 4 && h >= alto * 0.2f;
                bool horizontal = h <= 4 && w >= ancho * 0.2f;
                if (vertical || horizontal)
                {
                    foreach (int i in miembros)
                        esFondo[i] = true;
                }
            }
        }

        static bool LineaDeMarco(Color32[] pixeles, int ancho, int inicio, int paso, int largo)
        {
            int oscuros = 0;
            for (int k = 0; k < largo; k++)
            {
                if (Luz(pixeles[inicio + k * paso]) < 0.2f)
                    oscuros++;
            }
            return oscuros >= largo * 0.6f;
        }

        // Blanco o casi blanco y sin color: el borde del sticker.
        static bool Blanco(Color32 p) => Luz(p) > 0.55f && Saturacion(p) < 0.18f;

        // Gris sin color (fondo, pelo, túnica). Las líneas negras del dibujo, más oscuras, no cuentan.
        static bool Grisaceo(Color32 p)
        {
            float luz = Luz(p);
            return luz >= 0.19f && luz < 0.36f && Saturacion(p) < 0.16f;
        }

        // ───────────────────────── Recortar ─────────────────────────

        public static RectInt Caja(Color32[] pixeles, int ancho, int alto)
        {
            int minX = ancho, minY = alto, maxX = -1, maxY = -1;
            for (int y = 0; y < alto; y++)
            {
                for (int x = 0; x < ancho; x++)
                {
                    if (pixeles[y * ancho + x].a < 40)
                        continue;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }

            return maxX < 0 ? new RectInt(0, 0, ancho, alto) : new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        public static Color32[] Recortar(Color32[] pixeles, int ancho, RectInt caja)
        {
            var salida = new Color32[caja.width * caja.height];
            for (int y = 0; y < caja.height; y++)
                System.Array.Copy(pixeles, (caja.y + y) * ancho + caja.x, salida, y * caja.width, caja.width);
            return salida;
        }

        // ───────────────────────── Teñir pelo y túnica ─────────────────────────

        /// <summary>
        /// Pinta el pelo y la túnica con los colores de la personalización.
        /// La cabeza es una elipse (centro x, centro y, radio x, radio y) en proporción 0..1 de la imagen
        /// recortada, medida desde arriba a la izquierda (la tiene PosesMagnus para cada pose).
        /// Lo negro dentro de la cabeza es pelo; lo negro fuera es túnica. Las líneas del dibujo,
        /// la piel, las gafas y los zapatos no se tocan. Con null no se tiñe esa parte.
        /// </summary>
        public static void Tenir(Color32[] pixeles, int ancho, int alto, float cx, float cy, float rx, float ry, Color32? pelo, Color32? tunica)
        {
            if (pelo == null && tunica == null)
                return;

            for (int i = 0; i < pixeles.Length; i++)
            {
                Color32 p = pixeles[i];
                if (p.a < 40)
                    continue;
                float luz = Luz(p);
                if (luz < 0.07f || luz > 0.5f || Saturacion(p) > 0.3f)
                    continue;
                // Lo café (zapatos, marco de las gafas, sombras de la piel) no se tiñe.
                if (p.r > p.g + 4 && p.r > p.b)
                    continue;

                float x = (i % ancho + 0.5f) / ancho;
                float desdeArriba = 1f - (i / ancho + 0.5f) / alto;   // en Unity la fila 0 es la de abajo
                float dx = (x - cx) / rx;
                float dy = (desdeArriba - cy) / ry;
                bool enCabeza = dx * dx + dy * dy <= 1f;
                Color32? tinte = enCabeza ? pelo : tunica;
                if (tinte == null)
                    continue;

                Color32 t = tinte.Value;
                float brillo = Mathf.Clamp(luz / (enCabeza ? 0.22f : 0.24f), 0.32f, 1.08f);
                float peso = enCabeza ? 0.82f : 0.9f;
                pixeles[i] = new Color32(
                    Mezclar(p.r, t.r * brillo, peso),
                    Mezclar(p.g, t.g * brillo, peso),
                    Mezclar(p.b, t.b * brillo, peso),
                    p.a);
            }
        }

        static byte Mezclar(byte original, float destino, float peso)
        {
            float valor = original + (Mathf.Clamp(destino, 0f, 255f) - original) * peso;
            return (byte)Mathf.Clamp(valor, 0f, 255f);
        }

        public static float Luz(Color32 p) => (p.r * 0.3f + p.g * 0.59f + p.b * 0.11f) / 255f;

        public static float Saturacion(Color32 p)
        {
            float max = Mathf.Max(p.r, Mathf.Max(p.g, p.b));
            float min = Mathf.Min(p.r, Mathf.Min(p.g, p.b));
            return max <= 0f ? 0f : (max - min) / max;
        }
    }
}
