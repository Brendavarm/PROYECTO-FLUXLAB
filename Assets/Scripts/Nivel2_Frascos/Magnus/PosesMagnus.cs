using UnityEngine;

namespace FluxLab.Nivel2
{
    /// <summary>Las poses de Magnus. Normal es Magnus de pie (Magnuss.jpg / MagnussG.jpg).</summary>
    public enum PoseMagnus
    {
        Normal,
        Asustado,
        Bailando,
        Cantando,
        Enojado,
        Meditando,
        Pensando,
        Riendo,
        Saltando,
        Sorprendido,
        Tirando,
        Triste,
        Volando
    }

    /// <summary>
    /// Dónde está cada imagen, de qué tamaño se dibujó y dónde queda la cabeza (para teñir el pelo).
    /// Imágenes: StreamingAssets/Personaje/Imagenes-animacion/Magnus{Pose}.jpg (sin gafas)
    ///           y Magnus{Pose}G.jpg (con gafas). La pose Normal es Magnuss.jpg / MagnussG.jpg
    ///           (ya no se usa magnus.png: era poco nítida).
    /// </summary>
    public static class PosesMagnus
    {
        public const string Carpeta = "Personaje/Imagenes-animacion/";

        /// <summary>
        /// Las poses se dibujaron con Magnus de pie midiendo unos 305 píxeles; la Normal es más grande
        /// (768). Con esto todas se ven del mismo tamaño en pantalla.
        /// </summary>
        public const float PixelesDePiePoses = 305f;

        public static readonly PoseMagnus[] Todas = (PoseMagnus[])System.Enum.GetValues(typeof(PoseMagnus));

        /// <summary>Ruta dentro de StreamingAssets.</summary>
        public static string Archivo(PoseMagnus pose, bool gafas)
        {
            string nombre = pose == PoseMagnus.Normal ? "Magnuss" : "Magnus" + pose;
            return Carpeta + nombre + (gafas ? "G" : "") + ".jpg";
        }

        /// <summary>La Normal tiene fondo blanco; las demás poses, fondo gris.</summary>
        public static bool FondoBlanco(PoseMagnus pose) => pose == PoseMagnus.Normal;

        /// <summary>
        /// Elipse de la cabeza en la imagen ya recortada: (centro x, centro y, radio x, radio y),
        /// en proporción 0..1 contando desde arriba a la izquierda. Medida a ojo sobre cada pose.
        /// </summary>
        public static Vector4 Cabeza(PoseMagnus pose)
        {
            switch (pose)
            {
                case PoseMagnus.Asustado: return new Vector4(0.33f, 0.12f, 0.37f, 0.14f);
                case PoseMagnus.Bailando: return new Vector4(0.41f, 0.11f, 0.27f, 0.13f);
                case PoseMagnus.Cantando: return new Vector4(0.33f, 0.12f, 0.37f, 0.14f);
                case PoseMagnus.Enojado: return new Vector4(0.45f, 0.12f, 0.43f, 0.14f);
                case PoseMagnus.Meditando: return new Vector4(0.48f, 0.18f, 0.31f, 0.20f);
                case PoseMagnus.Pensando: return new Vector4(0.50f, 0.30f, 0.39f, 0.33f);
                case PoseMagnus.Riendo: return new Vector4(0.50f, 0.12f, 0.40f, 0.14f);
                case PoseMagnus.Saltando: return new Vector4(0.52f, 0.11f, 0.26f, 0.13f);
                case PoseMagnus.Sorprendido: return new Vector4(0.48f, 0.12f, 0.31f, 0.13f);
                case PoseMagnus.Tirando: return new Vector4(0.36f, 0.12f, 0.24f, 0.14f);
                case PoseMagnus.Triste: return new Vector4(0.45f, 0.13f, 0.41f, 0.16f);
                case PoseMagnus.Volando: return new Vector4(0.75f, 0.25f, 0.21f, 0.28f);
                default: return new Vector4(0.50f, 0.15f, 0.50f, 0.18f);   // Normal (Magnuss)
            }
        }
    }
}
