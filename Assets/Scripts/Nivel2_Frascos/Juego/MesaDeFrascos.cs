using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FluxLab.Nivel2
{
    /// <summary>
    /// Nivel 2: Pasillo de Frascos. Es la mesa del nivel 1 con más dificultad:
    /// la receta se enciende de a un ingrediente, puede repetir ingredientes,
    /// cada error rompe un frasco de vida, la bandeja se baraja y la última ronda va al revés.
    /// Los números se cambian en el Inspector (sección "Config").
    /// </summary>
    public class MesaDeFrascos : MonoBehaviour
    {
        public const int Nivel = 2;
        public const string Escena = "Nivel2_Frascos";
        const string SonidoRomper = "Nivel2/romper";

        [SerializeField] ConfigFrascos config = new ConfigFrascos();

        EscenaFrascos escena;
        IngredientKind[] ordenBandeja = RecetaFrascos.Todos;
        List<IngredientKind> receta = new List<IngredientKind>();
        List<IngredientKind> esperada = new List<IngredientKind>();
        readonly List<IngredientKind> puestos = new List<IngredientKind>();
        readonly List<float> tiemposRespuesta = new List<float>();

        int ronda;
        int puntos;
        int aciertos;
        int racha;
        int rachaMaxima;
        bool turnoAbierto;
        bool vertiendo;
        bool enPausa;
        bool preguntandoSalir;
        bool bienvenidaVista;
        int partida;
        float inicioTurno;

        bool Detenido => enPausa || preguntandoSalir;

        void Awake()
        {
            Time.timeScale = 1f;
            escena = new EscenaFrascos(this);
            escena.Construir(config, new EscenaFrascos.Acciones
            {
                elegir = Elegir,
                vaciar = Vaciar,
                pausar = Pausar,
                continuar = Continuar,
                pedirSalir = PedirSalir,
                confirmarSalir = ConfirmarSalir,
                cancelarSalir = CancelarSalir,
                irAlMapa = IrAlMapa,
                irAlMenu = IrAlMenu,
                reintentar = Empezar,
                siguiente = IrAlSiguiente
            });
        }

        void Start()
        {
            Empezar();
        }

        // Si la escena se cierra en pausa, el resto del juego no debe quedar congelado.
        void OnDestroy()
        {
            Time.timeScale = 1f;
        }

        void Update()
        {
            escena.AnimarBandeja();

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (preguntandoSalir) CancelarSalir();
                else if (enPausa) Continuar();
                else Pausar();
            }
            else if (Input.GetKeyDown(KeyCode.P))
            {
                if (enPausa) Continuar();
                else Pausar();
            }

            if (!turnoAbierto || Detenido)
                return;

            for (int i = 0; i < ordenBandeja.Length; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
                    Elegir(ordenBandeja[i]);
            }

            if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Space))
                Vaciar();
        }

        // ───────────────────────── Flujo del nivel ─────────────────────────

        public void Empezar()
        {
            StopAllCoroutines();
            Time.timeScale = 1f;
            enPausa = false;
            preguntandoSalir = false;
            escena.MostrarPausa(false);
            escena.MostrarConfirmarSalir(false);
            partida++;
            vertiendo = false;
            puntos = 0;
            aciertos = 0;
            racha = 0;
            rachaMaxima = 0;
            ronda = 0;
            tiemposRespuesta.Clear();
            turnoAbierto = false;
            escena.OcultarResultado();
            escena.EnfocarReceta(false);
            escena.LimpiarEfectos();
            escena.Caldero.Reiniciar();
            escena.MostrarEspejo(false);
            escena.Vidas.Restaurar();
            escena.PonerAnimo(AnimoMagnus.Quieto);
            StartCoroutine(Sesion());
        }

        IEnumerator Sesion()
        {
            escena.Marcador(1, config.rondas, puntos);
            escena.Mensaje("", "#5a3b24");
            escena.Temporizador("", 1f);

            // Nuevo: bienvenida (solo la primera vez) y conteo 3, 2, 1.
            if (config.mostrarBienvenida && !bienvenidaVista)
            {
                bienvenidaVista = true;
                yield return escena.Bienvenida.Mostrar();
            }

            if (config.conteoInicial)
                yield return escena.Bienvenida.Conteo();
            escena.Bienvenida.gameObject.SetActive(false);
            escena.SoltarMaderas();

            for (ronda = 1; ronda <= config.rondas; ronda++)
            {
                escena.Marcador(ronda, config.rondas, puntos);
                yield return Ronda();
                if (escena.Vidas.Quedan <= 0)
                    break;
            }

            // Nuevo: mensaje de motivación al terminar (o de ánimo si se rompieron los frascos).
            if (config.mensajesAnimo)
            {
                bool seRompieron = escena.Vidas.Quedan <= 0;
                int estrellas = RecetaFrascos.Estrellas(aciertos, config.rondas, seRompieron);
                yield return escena.Bienvenida.Final(estrellas, config.estrellasParaAvanzar, seRompieron);
            }

            Terminar();
        }

        IEnumerator Ronda()
        {
            bool espejo = config.rondaEspejoFinal && config.rondas > 1 && ronda == config.rondas;
            receta = RecetaFrascos.Generar(Mathf.Clamp(config.largoReceta, 1, EscenaFrascos.MaxReceta), config.permitirRepetidos);
            esperada = RecetaFrascos.Esperada(receta, espejo);
            puestos.Clear();
            turnoAbierto = false;

            escena.Caldero.Reiniciar();

            // Nuevo: aviso gigante antes de la ronda al revés.
            if (espejo && config.anunciarEspejo)
                yield return escena.Bienvenida.AnuncioEspejo();

            escena.MostrarEspejo(espejo);
            if (config.barajarBandeja)
            {
                ordenBandeja = RecetaFrascos.Barajar(RecetaFrascos.Todos);
                escena.AcomodarBandeja(ordenBandeja, true);
            }

            escena.Mensaje(espejo ? "Mira bien: esta receta va al revés." : "Mira cómo se encienden los frascos.", "#5a3b24");
            escena.PonerAnimo(AnimoMagnus.Observa);
            escena.EncenderIngrediente(receta, -1);
            escena.Temporizador("Prepárate...", 1f);
            escena.EnfocarReceta(true);   // nuevo: el pergamino crece mientras se dicta la receta
            yield return new WaitForSeconds(0.6f);

            yield return EncenderReceta();
            escena.EnfocarReceta(false);  // y vuelve a su lugar cuando te toca

            escena.Mensaje(espejo ? "Ahora ponla en el caldero AL REVÉS." : "Ahora repítela en el caldero.", espejo ? "#8d3d36" : "#5a3b24");
            escena.Avance(0, esperada.Count);
            escena.PonerAnimo(AnimoMagnus.Quieto);
            turnoAbierto = true;
            inicioTurno = Time.time;

            while (turnoAbierto)
                yield return null;

            yield return new WaitForSeconds(1.5f);
            escena.MostrarEspejo(false);
        }

        // Nuevo en el nivel 2: la receta aparece ingrediente por ingrediente.
        IEnumerator EncenderReceta()
        {
            float porIngrediente = Mathf.Max(0.1f, config.segundosPorIngrediente);
            float pausa = Mathf.Max(0f, config.pausaEntreIngredientes);
            float total = receta.Count * (porIngrediente + pausa);
            float transcurrido = 0f;

            for (int i = 0; i < receta.Count; i++)
            {
                escena.EncenderIngrediente(receta, i);
                escena.Sonar("FluxLab/Audio/click");
                float reloj = 0f;
                while (reloj < porIngrediente)
                {
                    reloj += Time.deltaTime;
                    transcurrido += Time.deltaTime;
                    escena.Temporizador("Frasco " + (i + 1) + " de " + receta.Count, 1f - transcurrido / total);
                    yield return null;
                }

                escena.EncenderIngrediente(receta, -1);
                reloj = 0f;
                while (reloj < pausa)
                {
                    reloj += Time.deltaTime;
                    transcurrido += Time.deltaTime;
                    escena.Temporizador("Frasco " + (i + 1) + " de " + receta.Count, 1f - transcurrido / total);
                    yield return null;
                }
            }
        }

        // ───────────────────────── Jugadas ─────────────────────────

        // Elegir lanza el frasco; la jugada se revisa cuando el chorro cae (AlCaer).
        public void Elegir(IngredientKind tipo)
        {
            if (!turnoAbierto || vertiendo || Detenido || puestos.Count >= esperada.Count)
                return;

            vertiendo = true;
            int esta = partida;
            IngredientKind? anterior = puestos.Count > 0 ? puestos[puestos.Count - 1] : (IngredientKind?)null;
            escena.Sonar("FluxLab/Audio/click");
            escena.PonerAnimo(AnimoMagnus.Remueve);
            escena.LanzarFrasco(tipo, config.duracionVertido, () =>
            {
                if (esta == partida)
                    AlCaer(tipo, anterior);
            });
        }

        void AlCaer(IngredientKind tipo, IngredientKind? anterior)
        {
            vertiendo = false;
            if (!turnoAbierto)
                return;

            int posicion = puestos.Count;
            puestos.Add(tipo);
            escena.Avance(puestos.Count, esperada.Count);
            escena.Caldero.Verter(tipo, escena.ColorDe(tipo), anterior);

            // Nuevo en el nivel 2: se revisa en el momento, no al final.
            if (!RecetaFrascos.AciertaEn(esperada, posicion, tipo))
            {
                Fallar();
                return;
            }

            if (puestos.Count == esperada.Count)
            {
                Acertar();
                return;
            }

            escena.Magnus.Reir(0.9f);   // ingrediente correcto: Magnus se ríe
            if (config.mensajesAnimo)
                escena.Elogio();
        }

        public void Vaciar()
        {
            if (!turnoAbierto || vertiendo || Detenido || puestos.Count == 0)
                return;
            puestos.Clear();
            escena.Avance(0, esperada.Count);
            escena.Caldero.Vaciar();
            escena.Sonar("FluxLab/Audio/click");
        }

        void Acertar()
        {
            CerrarTurno();
            aciertos++;
            puntos += config.puntosPorAcierto;
            racha++;
            rachaMaxima = Mathf.Max(rachaMaxima, racha);
            escena.Mensaje("Correcto. La mezcla cuajó.", "#2f7a4a");
            escena.Temporizador("Bien hecho", 1f);
            escena.Sonar("FluxLab/Audio/acierto");
            escena.PonerAnimo(AnimoMagnus.Festeja);
            escena.Caldero.Exito();
            escena.FrascosFondo?.Festejar();   // ¡los frascos del fondo festejan!
            if (config.mensajesAnimo)
                escena.Felicitar();
            escena.Marcador(ronda, config.rondas, puntos);
        }

        void Fallar()
        {
            CerrarTurno();
            racha = 0;
            int quedan = escena.Vidas.Romper();
            escena.MostrarRecetaCompleta(receta);
            escena.Mensaje(quedan > 0 ? "¡Crack! Se rompió un frasco. Así era la receta." : "Se rompió el último frasco.", "#8d3d36");
            escena.Temporizador("Quedan " + quedan + " frascos", 0f);
            escena.Sonar(SonidoRomper);
            escena.PonerAnimo(AnimoMagnus.SeEquivoca);
            escena.Caldero.Error();
            escena.FrascosFondo?.RomperVarios();  // se caen y se rompen varios frascos del fondo
            if (config.mensajesAnimo)
                escena.Lamentar(quedan);
        }

        void CerrarTurno()
        {
            turnoAbierto = false;
            tiemposRespuesta.Add(Mathf.Max(0f, Time.time - inicioTurno));
        }

        void Terminar()
        {
            turnoAbierto = false;
            bool seRompieron = escena.Vidas.Quedan <= 0;
            int recordAnterior = AjustesJuego.MejorPuntaje;
            AjustesJuego.RegistrarPuntaje(puntos, aciertos);

            // Nuevo: solo se abre el siguiente nivel con las estrellas mínimas (y sin romper todos los frascos).
            int estrellas = RecetaFrascos.Estrellas(aciertos, config.rondas, seRompieron);
            bool pasa = !seRompieron && estrellas >= config.estrellasParaAvanzar;
            string siguiente = NivelesTaller.De(Nivel + 1).nombre;
            string aviso = seRompieron
                ? "Se rompieron los frascos. ¡Inténtalo otra vez!"
                : pasa ? "" : "Necesitas " + config.estrellasParaAvanzar + " estrellas para abrir " + siguiente + ".";
            if (pasa && AjustesJuego.NivelAlcanzado == Nivel)
            {
                AjustesJuego.AvanzarNivel();
                aviso = "Se abrió " + siguiente + ".";
            }

            float promedio = 0f;
            for (int i = 0; i < tiemposRespuesta.Count; i++)
                promedio += tiemposRespuesta[i];
            if (tiemposRespuesta.Count > 0)
                promedio /= tiemposRespuesta.Count;

            escena.MostrarResultado(new EscenaFrascos.Resultado
            {
                puntos = puntos,
                aciertos = aciertos,
                rondas = config.rondas,
                rachaMaxima = rachaMaxima,
                tiempoMedio = promedio,
                vidasRestantes = escena.Vidas.Quedan,
                vidasTotales = escena.Vidas.Total,
                seRompieron = seRompieron,
                aviso = aviso,
                haySiguiente = AjustesJuego.NivelAlcanzado > Nivel,
                pasa = pasa,
                mejorPuntaje = AjustesJuego.MejorPuntaje,
                nuevoRecord = puntos > 0 && puntos > recordAnterior
            });
        }

        // ───────────────────────── Pausa y salir ─────────────────────────

        // Time.timeScale = 0 congela todo lo que usa Time.deltaTime: receta, frascos, partículas.
        void Pausar()
        {
            if (enPausa || preguntandoSalir || escena.ResultadoVisible)
                return;
            enPausa = true;
            Time.timeScale = 0f;
            escena.MostrarPausa(true);
            escena.Sonar("FluxLab/Audio/click");
        }

        void Continuar()
        {
            enPausa = false;
            preguntandoSalir = false;
            escena.MostrarPausa(false);
            escena.MostrarConfirmarSalir(false);
            Time.timeScale = 1f;
        }

        void PedirSalir()
        {
            if (preguntandoSalir)
                return;
            preguntandoSalir = true;
            Time.timeScale = 0f;
            escena.MostrarPausa(false);
            escena.MostrarConfirmarSalir(true);
            escena.Sonar("FluxLab/Audio/click");
        }

        // "No, seguir": vuelve a la pausa si venía de ahí, si no, sigue jugando.
        void CancelarSalir()
        {
            preguntandoSalir = false;
            escena.MostrarConfirmarSalir(false);
            if (enPausa)
                escena.MostrarPausa(true);
            else
                Time.timeScale = 1f;
        }

        void ConfirmarSalir() => Cargar("MapaNiveles");

        // ───────────────────────── Navegación ─────────────────────────

        void IrAlSiguiente()
        {
            if (AjustesJuego.NivelAlcanzado <= Nivel)
                return;
            AjustesJuego.ElegirNivel(Nivel + 1);
            Cargar("MesaDeAlquimia");
        }

        void IrAlMapa() => Cargar("MapaNiveles");

        void IrAlMenu() => Cargar("MenuPrincipal");

        static void Cargar(string escenaDestino)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(escenaDestino);
        }
    }
}
