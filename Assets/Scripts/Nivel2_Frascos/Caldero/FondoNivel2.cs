using UnityEngine;

namespace FluxLab.Nivel2
{
    /// <summary>
    /// El fondo del nivel 2 (Resources/FluxLab/NIVEL2-IMAGENES/FondoN2.jpg) y todas sus medidas:
    /// dónde está pintado el caldero, la boca del caldero y la mesa donde se para Magnus.
    /// Si se cambia el fondo, solo hay que volver a medir y cambiar los números de este archivo.
    /// Las medidas están en píxeles del dibujo (1365 x 768), contando desde arriba a la izquierda.
    /// </summary>
    public static class FondoNivel2
    {
        public const string Ruta = "FluxLab/NIVEL2-IMAGENES/FondoN2";
        public const float Ancho = 1365f;
        public const float Alto = 768f;

        // ── Caldero pintado ──
        /// <summary>Rectángulo que contiene el caldero (para recortarlo y animarlo).</summary>
        public static readonly RectInt ZonaCaldero = new RectInt(525, 425, 320, 237);
        public const float CentroCalderoX = 683f;
        public const float PiesCalderoY = 652f;

        /// <summary>Silueta del caldero: elipses (centro x, centro y, radio x, radio y).</summary>
        public static readonly Vector4[] SiluetaCaldero =
        {
            new Vector4(684f, 456f, 121f, 26f), // borde de arriba
            new Vector4(686f, 487f, 118f, 21f), // cuello
            new Vector4(683f, 548f, 135f, 89f), // panza
            new Vector4(556f, 518f, 27f, 45f),  // asa izquierda
            new Vector4(808f, 518f, 27f, 45f),  // asa derecha
            new Vector4(592f, 630f, 20f, 28f),  // pata izquierda
            new Vector4(785f, 630f, 20f, 28f),  // pata derecha
            new Vector4(640f, 637f, 14f, 14f)   // pata del medio
        };

        // ── Boca del caldero (donde está el líquido) ──
        public static Vector2 Boca => ACanvas(683f, 455f);
        /// <summary>Medio ancho del líquido, en unidades del canvas (97 px del dibujo).</summary>
        public static float RadioBoca => 97f * Escala;
        /// <summary>Cuánto se aplasta el círculo del líquido para verse en perspectiva.</summary>
        public const float AchatarBoca = 0.13f;

        // ── Magnus parado sobre la mesa, a la izquierda del caldero ──
        public static Vector2 PiesMagnus => ACanvas(290f, 641f);
        public const float AltoMagnus = 480f;

        // ── Fondo vivo (FondoVivo.cs) ──
        /// <summary>Faroles pintados: (x, y, tamaño del resplandor).</summary>
        public static readonly Vector3[] Faroles =
        {
            new Vector3(168f, 135f, 150f),
            new Vector3(975f, 75f, 150f),
            new Vector3(1222f, 130f, 140f),
            new Vector3(1005f, 745f, 90f)
        };

        public static readonly Vector2 Luna = new Vector2(716f, 72f);
        /// <summary>Ventana de la luna (donde titilan estrellitas).</summary>
        public static readonly Rect Ventana = new Rect(650f, 30f, 140f, 120f);

        /// <summary>Repisas con frascos: allí aparecen destellos de vez en cuando.</summary>
        public static readonly Rect[] Repisas =
        {
            new Rect(270f, 120f, 230f, 110f),
            new Rect(270f, 250f, 230f, 80f),
            new Rect(270f, 360f, 230f, 90f),
            new Rect(500f, 100f, 360f, 180f),
            new Rect(860f, 20f, 240f, 240f),
            new Rect(900f, 310f, 180f, 170f),
            new Rect(1130f, 290f, 235f, 130f),
            new Rect(1120f, 450f, 245f, 250f),
            new Rect(60f, 560f, 190f, 140f),
            new Rect(340f, 520f, 190f, 120f)
        };

        /// <summary>
        /// Frascos pintados que se animan (FrascosDelFondo.cs): elipse (centro x, centro y, radio x, radio y)
        /// que cubre el frasco entero, cuello incluido. Se mecen desde la base.
        /// </summary>
        public static readonly Vector4[] FrascosPintados =
        {
            new Vector4(385f, 588f, 46f, 62f),   // mesa: morado
            new Vector4(444f, 570f, 38f, 68f),   // mesa: amarillo
            new Vector4(495f, 588f, 36f, 55f),   // mesa: verde
            new Vector4(925f, 568f, 32f, 62f),   // mesa: rojo, junto al mortero
            new Vector4(357f, 188f, 23f, 36f),   // repisa izquierda de arriba
            new Vector4(410f, 188f, 22f, 36f),
            new Vector4(460f, 190f, 23f, 34f),
            new Vector4(358f, 286f, 22f, 37f),   // repisa izquierda del medio
            new Vector4(405f, 286f, 23f, 40f),
            new Vector4(455f, 286f, 23f, 40f),
            new Vector4(664f, 236f, 27f, 42f),   // repisa central
            new Vector4(918f, 230f, 21f, 34f),   // repisa derecha, junto al gato
            new Vector4(953f, 232f, 15f, 30f),
            new Vector4(990f, 348f, 21f, 36f),   // repisa derecha del medio
            new Vector4(1040f, 348f, 21f, 36f),
            new Vector4(990f, 447f, 21f, 34f),
            new Vector4(1282f, 530f, 31f, 56f)   // repisa de la derecha, abajo
        };

        /// <summary>Unidades del canvas (1920 x 1080) por cada píxel del dibujo.</summary>
        public static float Escala => 1920f / Ancho;

        /// <summary>Pasa un punto del dibujo a coordenadas del canvas (centro = 0, 0).</summary>
        public static Vector2 ACanvas(float x, float y)
        {
            return new Vector2((x / Ancho - 0.5f) * 1920f, (0.5f - y / Alto) * 1080f);
        }

        public static Texture2D Cargar()
        {
            var textura = Resources.Load<Texture2D>(Ruta);
            if (textura == null)
                Debug.LogWarning("No se encontró el fondo " + Ruta);
            return textura;
        }
    }
}
