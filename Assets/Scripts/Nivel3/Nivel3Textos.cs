using UnityEngine;

namespace FluxLab
{
    /// <summary>Todos los mensajes del nivel 3, juntos para cambiarlos fácil.</summary>
    public static class Nivel3Textos
    {
        // ---- Bienvenida ----
        public const string BienvenidaTitulo = "¡Bienvenido al Nivel 3!";
        public const string BienvenidaNivel = "Jardín de Luna";
        public const string BienvenidaFrase = "Bajo la luz de la luna, las plantas guardan secretos... ¡y tu caldero también!";

        public static readonly string[] Instrucciones =
        {
            "1.  Mira bien la receta: cada ronda te la enseña de una forma distinta.",
            "2.  Cuando el pergamino se cierre, echa los ingredientes al caldero en el mismo orden.",
            "3.  ¡Cuidado! Si te equivocas, el caldero explota y la ronda empieza de nuevo.",
            "4.  ¿Te confundiste a mitad? El botón Vaciar limpia el caldero."
        };

        public const string BotonComenzar = "¡Comenzar la magia!";

        // ---- Rondas ----
        public static readonly string[] TituloRonda =
        {
            "Ronda 1: ¡La receta completa!",
            "Ronda 2: ¡Uno por uno!",
            "Ronda 3: ¡Solo sombras!"
        };

        public static readonly string[] SubtituloRonda =
        {
            "Memoriza todos los ingredientes",
            "Los ingredientes aparecerán de a uno",
            "Reconoce los ingredientes por su forma"
        };

        public static readonly string[] IndicacionPergamino =
        {
            "¡Memoriza el orden, de izquierda a derecha!",
            "¡Uno por uno! Fíjate bien en el orden.",
            "¡Solo verás sus sombras! Recuerda las formas."
        };

        public const string TuTurno = "¡Tu turno! Echa los ingredientes al caldero";

        // ---- Mientras mezclas ----
        public static readonly string[] AciertoIngrediente =
        {
            "¡Muy bien!",
            "¡Vas increíble!",
            "¡Excelente, sigue así!",
            "¡Eso es, mago!",
            "¡La poción te adora!",
            "¡Qué memoria tan mágica!"
        };

        public static readonly string[] RondaCompleta =
        {
            "¡Poción perfecta!",
            "¡Magnífico, aprendiz!",
            "¡Lo lograste, qué talento!",
            "¡El caldero está feliz!"
        };

        public static readonly string[] Error =
        {
            "¡Ese no era! ¡BOOM!",
            "¡Uy! Ese ingrediente no iba ahí.",
            "¡Pum! Casi... ¡Vuelve a intentarlo!",
            "¡El caldero no lo aceptó! Mira la receta otra vez."
        };

        public const string CalderoLimpio = "¡Caldero limpio! Empieza la mezcla de nuevo.";

        // ---- Pausa ----
        public const string PausaTitulo = "Pausa";

        // ---- Confirmar la salida al mapa ----
        public const string ConfirmarTitulo = "¿Volver al mapa?";
        public const string ConfirmarTexto = "Si sales ahora perderás lo que llevas en este nivel y tendrás que empezar de nuevo.";
        public const string ConfirmarSeguir = "Seguir jugando";
        public const string ConfirmarSalir = "Sí, salir";

        // ---- Resultados ----
        public const string ResultadoTitulo = "¡Nivel completado!";
        public const string ResultadoDesbloqueo = "¡Felicidades! Pasaste al siguiente nivel:";

        public static string Elegir(string[] opciones)
        {
            return opciones[Random.Range(0, opciones.Length)];
        }
    }
}
