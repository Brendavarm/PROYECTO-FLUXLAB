using UnityEngine;

namespace FluxLab.Nivel2
{
    /// <summary>
    /// Números del Pasillo de Frascos. Aparecen en el Inspector dentro del
    /// componente MesaDeFrascos, así se ajusta la dificultad sin tocar código.
    /// </summary>
    [System.Serializable]
    public class ConfigFrascos
    {
        [Header("Entrada al nivel")]
        [Tooltip("Muestra la bienvenida con las instrucciones al entrar (no al reintentar).")]
        public bool mostrarBienvenida = true;

        [Tooltip("Cuenta 3, 2, 1 antes de empezar (también al reintentar).")]
        public bool conteoInicial = true;

        [Header("Receta")]
        [Tooltip("Cuántas recetas hay que preparar en el nivel.")]
        [Range(1, 8)]
        public int rondas = 4;

        [Tooltip("Cuántos ingredientes tiene cada receta (el nivel 1 usa 3).")]
        [Range(2, 5)]
        public int largoReceta = 4;

        [Tooltip("La receta puede pedir el mismo ingrediente más de una vez.")]
        public bool permitirRepetidos = true;

        [Header("Velocidad de la receta")]
        [Tooltip("Segundos que se ve encendido cada ingrediente. Menos = más difícil.")]
        [Range(0.3f, 3f)]
        public float segundosPorIngrediente = 0.9f;

        [Tooltip("Pausa entre un ingrediente y el siguiente.")]
        [Range(0f, 1f)]
        public float pausaEntreIngredientes = 0.25f;

        [Header("Dificultad extra")]
        [Tooltip("Frascos de vida. Cada ingrediente equivocado rompe uno.")]
        [Range(1, 5)]
        public int vidas = 3;

        [Tooltip("La bandeja cambia de orden en cada ronda.")]
        public bool barajarBandeja = true;

        [Tooltip("La última ronda se repite al revés.")]
        public bool rondaEspejoFinal = true;

        [Tooltip("Mensajes de ánimo: ¡Bien!, ¡Perfecto!, ¡Oh nooo! y el mensaje final de motivación.")]
        public bool mensajesAnimo = true;

        [Tooltip("Antes de la ronda espejo sale un aviso gigante con Mishi.")]
        public bool anunciarEspejo = true;

        [Tooltip("Segundos que se quedan el aviso de ronda espejo y el mensaje final (se pueden saltar con clic).")]
        [Range(2f, 15f)]
        public float segundosMensajesGrandes = 3.4f;

        [Header("Pasar de nivel")]
        [Tooltip("Estrellas mínimas para abrir el siguiente nivel. Si se rompen todos los frascos, nunca pasa.")]
        [Range(1, 3)]
        public int estrellasParaAvanzar = 2;

        [Header("Puntos")]
        [Range(10, 500)]
        public int puntosPorAcierto = 150;

        [Header("Burbujas del caldero")]
        [Tooltip("Cuántas burbujas salen del caldero.")]
        [Range(0, 12)]
        public int cantidadBurbujas = 4;

        [Tooltip("Velocidad con la que suben las burbujas. Se puede cambiar en Play.")]
        [Range(5f, 150f)]
        public float velocidadBurbujas = 34f;

        [Header("Fondo vivo")]
        [Tooltip("Burbujas, faroles que titilan, luna, polvo mágico y destellos en los frascos del fondo.")]
        public bool fondoVivo = true;

        [Tooltip("Los frascos del fondo reaccionan: al fallar uno se cae y se rompe; al acertar todos festejan.")]
        public bool frascosReaccionan = true;

        [Tooltip("Cuántas burbujas suben por el fondo (1 = normal). Se puede cambiar en Play.")]
        [Range(0f, 3f)]
        public float burbujasFondo = 1f;

        [Header("Animación del frasco")]
        [Tooltip("Segundos que tarda en volar, inclinarse y vaciarse. Menos = más rápido.")]
        [Range(0.3f, 2.5f)]
        public float duracionVertido = 0.9f;

        [Header("Reacciones del caldero")]
        [Tooltip("Cuántas chispas, gotas y destellos salen en cada reacción. Se puede cambiar en Play.")]
        [Range(0, 60)]
        public int cantidadChispas = 26;

        [Tooltip("Qué tan grandes son las reacciones (1 = normal). Se puede cambiar en Play.")]
        [Range(0.5f, 3f)]
        public float tamanoReacciones = 1.6f;

        [Tooltip("Cuánto vapor y humo sale del caldero (0 = nada). Se puede cambiar en Play.")]
        [Range(0f, 2f)]
        public float intensidadHumo = 1.2f;

        [Tooltip("Qué tan fuerte hierve el caldero después de echar algo. Se puede cambiar en Play.")]
        [Range(0f, 2f)]
        public float fuerzaHervor = 1.2f;

        [Tooltip("Segundos que el líquido se ve girando y mezclándose después de echar un frasco.")]
        [Range(0.5f, 4f)]
        public float segundosMezcla = 2f;
    }
}
