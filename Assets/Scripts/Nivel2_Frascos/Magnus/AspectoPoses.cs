using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace FluxLab.Nivel2
{
    /// <summary>Cómo personalizó el jugador a Magnus.</summary>
    public struct PersonalizacionMagnus
    {
        public int pelo;          // índice de AjustesJuego.Pelo (0 = color original)
        public int colorTunica;   // índice de AjustesJuego.ColorTunica (0 = color original)
        public bool gafas;        // true = poses con gafas (G)

        /// <summary>Lo que el jugador eligió en "Personalizar" (y la opción de gafas, si existe).</summary>
        public static PersonalizacionMagnus Guardada => new PersonalizacionMagnus
        {
            pelo = AjustesJuego.Pelo,
            colorTunica = AjustesJuego.ColorTunica,
            gafas = AspectoPoses.LlevaGafas
        };

        public string Clave => pelo + "_" + colorTunica + "_" + (gafas ? "G" : "");
    }

    /// <summary>
    /// Prepara la imagen de cada pose con la personalización de Magnus:
    /// la carga, le quita el fondo gris, la recorta y tiñe pelo y túnica.
    /// Las imágenes ya preparadas se guardan para no repetir el trabajo.
    /// </summary>
    public static class AspectoPoses
    {
        /// <summary>
        /// Opción de gafas. Hoy no existe en "Personalizar"; si Brenda la agrega, basta con que la guarde
        /// en PlayerPrefs con esta clave (1 = con gafas, 0 = sin gafas). Si no existe, Magnus lleva gafas.
        /// </summary>
        public const string ClaveGafas = "flux.gafas";

        public static bool LlevaGafas => PlayerPrefs.GetInt(ClaveGafas, 1) == 1;

        // Los mismos colores que usan AspectoMagnus y PersonalizarMagnus (archivos de Brenda).
        static readonly Color32[] Pelos =
        {
            new Color32(0, 0, 0, 0),           // Clásico: sin teñir
            new Color32(138, 90, 50, 255),     // Castaño  #8a5a32
            new Color32(226, 179, 74, 255),    // Rubio    #e2b34a
            new Color32(196, 72, 58, 255),     // Rojizo   #c4483a
            new Color32(228, 221, 212, 255)    // Canoso   #e4ddd4
        };

        static readonly Color32[] Tunicas =
        {
            new Color32(60, 56, 66, 255),      // Oscuro original: sin teñir
            new Color32(214, 69, 69, 255),     // Rojo rubí        #d64545
            new Color32(58, 120, 216, 255),    // Azul zafiro      #3a78d8
            new Color32(226, 181, 58, 255),    // Amarillo estrella #e2b53a
            new Color32(62, 168, 106, 255),    // Verde esmeralda  #3ea86a
            new Color32(224, 122, 42, 255)     // Naranja ámbar    #e07a2a
        };

        static readonly Dictionary<string, Texture2D> preparadas = new Dictionary<string, Texture2D>();

        public static int CantidadPelos => Pelos.Length;
        public static int CantidadTunicas => Tunicas.Length;

        /// <summary>La imagen de una pose con la personalización dada (null si no se encontró).</summary>
        public static Texture2D Textura(PoseMagnus pose, PersonalizacionMagnus personalizacion)
        {
            string clave = pose + "|" + personalizacion.Clave;
            if (preparadas.TryGetValue(clave, out Texture2D lista))
                return lista;

            Texture2D original = Leer(PosesMagnus.Archivo(pose, personalizacion.gafas));
            if (original == null)
            {
                preparadas[clave] = null;
                return null;
            }

            int ancho = original.width;
            int alto = original.height;
            Color32[] pixeles = original.GetPixels32();
            Object.Destroy(original);
            // La Normal tiene fondo blanco (se borra igual que el rosado de los frascos);
            // las demás poses tienen fondo gris (ver ProcesoImagenMagnus).
            pixeles = PosesMagnus.FondoBlanco(pose)
                ? ImagenesFrascos.QuitarFondo(pixeles, ancho, alto)
                : ProcesoImagenMagnus.QuitarFondo(pixeles, ancho, alto);

            RectInt caja = ProcesoImagenMagnus.Caja(pixeles, ancho, alto);
            pixeles = ProcesoImagenMagnus.Recortar(pixeles, ancho, caja);

            Vector4 cabeza = PosesMagnus.Cabeza(pose);
            int pelo = Mathf.Clamp(personalizacion.pelo, 0, Pelos.Length - 1);
            int tunica = Mathf.Clamp(personalizacion.colorTunica, 0, Tunicas.Length - 1);
            ProcesoImagenMagnus.Tenir(pixeles, caja.width, caja.height, cabeza.x, cabeza.y, cabeza.z, cabeza.w,
                pelo > 0 ? Pelos[pelo] : (Color32?)null,
                tunica > 0 ? Tunicas[tunica] : (Color32?)null);

            var textura = new Texture2D(caja.width, caja.height, TextureFormat.RGBA32, false);
            textura.SetPixels32(pixeles);
            textura.Apply();
            textura.filterMode = FilterMode.Bilinear;
            textura.wrapMode = TextureWrapMode.Clamp;
            textura.name = "Magnus" + pose;
            preparadas[clave] = textura;
            return textura;
        }

        /// <summary>Prepara de antemano varias poses (para que no se trabe la primera vez que aparecen).</summary>
        public static void Precargar(PersonalizacionMagnus personalizacion, params PoseMagnus[] poses)
        {
            foreach (PoseMagnus pose in poses)
                Textura(pose, personalizacion);
        }

        static Texture2D Leer(string relativo)
        {
            string ruta = Path.Combine(Application.streamingAssetsPath, relativo);
            if (!File.Exists(ruta))
            {
                Debug.LogWarning("No se encontró la imagen de Magnus " + ruta);
                return null;
            }

            var textura = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            textura.LoadImage(File.ReadAllBytes(ruta));
            return textura;
        }
    }
}
