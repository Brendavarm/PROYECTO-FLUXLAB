using UnityEngine;
using UnityEngine.UI;

namespace FluxLab
{
    /// <summary>
    /// La mariposa mágica del nivel 3: vuela despacio por la escena dejando un rastro de brillos,
    /// de vez en cuando se posa a descansar y, si hay magia en el caldero, acude a revolotear a su alrededor.
    /// Usa los 11 cuadros de Resources/FluxLab/Mariposa (mariposa_0 a mariposa_10).
    /// </summary>
    public class MariposaAnimada : MonoBehaviour
    {
        enum Estado
        {
            Vuela,
            Aterriza,
            Posada,
            Despega,
            Atraida
        }

        public float escala = 0.5f;          // tamaño: 1 = el dibujo tal cual
        public float velocidad = 150f;

        // Cuadros: 0 a 4 aleteo, 5 reposo, 6 preparación, 7 impulso, 8 vuelo, 9 aterrizaje, 10 posa
        static readonly int[] CicloAleteo = { 0, 1, 2, 3, 4, 3, 2, 1 };

        // Zona por la que vuela (coordenadas del lienzo)
        static readonly Rect Zona = new Rect(-780f, -60f, 1560f, 480f);

        Transform capa;
        Vector2[] posaderos;
        Sprite[] cuadros;
        RectTransform rect;
        Image imagen;
        Image sombraLuz;

        Estado estado = Estado.Vuela;
        float reloj;
        float duracion;
        Vector2 pos;
        Vector2 velocidadActual;
        Vector2 destino;
        float mira = 1f;                 // 1 = mira a la derecha (como el dibujo)
        float fase;
        float proximaChispa;
        float proximoPosarse;
        int posaderoActual;
        Vector2 inicioAterrizaje;
        Vector2 centroAtraido;
        bool atraerAlDespegar;
        bool tranquila;

        // Visita al pergamino: vuela hasta él, se posa encima un rato (tapando un poco la receta) y luego se va a una repisa
        bool visitaPendiente;
        Vector2 puntoVisita;
        float segundosVisita;
        bool usaPosaderoPropio;     // está posada (o yendo a posarse) en el punto de la visita
        Vector2 posaderoPropio;
        bool salirYa;
        bool posarEnRepisaAlSalir;

        /// <summary>En modo tranquilo la mariposa se posa y descansa; quitarlo la hace volar de nuevo.</summary>
        public bool Tranquila
        {
            get { return tranquila; }
            set
            {
                tranquila = value;
                if (!value && estado == Estado.Posada)
                    duracion = reloj + 0.3f;    // despega enseguida
            }
        }

        /// <param name="posaderos">Sitios donde puede posarse: el punto donde apoyan sus patitas.</param>
        public static MariposaAnimada Crear(Transform lienzo, Vector2[] posaderos)
        {
            var go = new GameObject("Mariposa", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(lienzo, false);
            var imagen = go.GetComponent<Image>();
            imagen.preserveAspect = true;
            imagen.raycastTarget = false;

            var r = go.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);

            var mariposa = go.AddComponent<MariposaAnimada>();
            mariposa.capa = lienzo;
            mariposa.posaderos = posaderos;
            return mariposa;
        }

        void Start()
        {
            rect = GetComponent<RectTransform>();
            imagen = GetComponent<Image>();
            fase = Random.Range(0f, 6.28f);

            cuadros = new Sprite[11];
            for (int i = 0; i < cuadros.Length; i++)
                cuadros[i] = Nivel3UI.SpriteOpcional("FluxLab/Mariposa/mariposa_" + i);

            // Un halo suave que la acompaña
            sombraLuz = Nivel3UI.Imagen(transform, "Halo", Nivel3UI.LuzSuave(), Vector2.zero, Vector2.one * 150f);
            sombraLuz.color = new Color(0.65f, 0.7f, 1f, 0.22f);
            sombraLuz.transform.SetAsFirstSibling();

            pos = new Vector2(-300f, 250f);
            ElegirDestino();
            proximoPosarse = Time.time + Random.Range(6f, 12f);
        }

        // =====================================================================================

        /// <summary>Hay magia en el caldero: la mariposa acude y revolotea a su alrededor un rato.</summary>
        /// <param name="aunqueDescanse">true para que acuda aunque esté tranquila (por ejemplo, de premio al terminar una receta)</param>
        public void Atraer(Vector2 punto, bool aunqueDescanse = false)
        {
            if ((tranquila && !aunqueDescanse) || usaPosaderoPropio || visitaPendiente)
                return;

            centroAtraido = punto;
            if (estado == Estado.Atraida)
            {
                reloj = 0f; // sigue ahí mientras haya magia
                return;
            }

            if (estado == Estado.Posada || estado == Estado.Aterriza)
            {
                // primero despega, y luego acude
                atraerAlDespegar = true;
                estado = Estado.Despega;
                reloj = 0f;
                duracion = 0.45f;
                return;
            }

            estado = Estado.Atraida;
            reloj = 0f;
            duracion = 3f;
        }

        /// <summary>
        /// Vuela hasta el pergamino, se posa donde apoyan sus patitas (pies) y se queda ahí unos segundos, tapando parte de la receta.
        /// Al terminar (o cuando se llame a TerminarVisita) se va volando a una repisa.
        /// </summary>
        public void VisitarPergamino(Vector2 pies, float segundos)
        {
            puntoVisita = pies;
            segundosVisita = segundos;
            visitaPendiente = true;
            salirYa = false;

            // Si está descansando, primero se levanta
            if (estado == Estado.Posada || estado == Estado.Aterriza)
            {
                estado = Estado.Despega;
                reloj = 0f;
                duracion = 0.45f;
            }
        }

        /// <summary>Se acabó el tiempo de la receta: si estaba posada sobre el pergamino, se va ya.</summary>
        public void TerminarVisita()
        {
            visitaPendiente = false;
            salirYa = true;
        }

        void ElegirDestino()
        {
            destino = new Vector2(Random.Range(Zona.xMin, Zona.xMax), Random.Range(Zona.yMin, Zona.yMax));
        }

        void Update()
        {
            if (rect == null || cuadros == null)
                return;

            float dt = Time.deltaTime;
            reloj += dt;
            float t = Time.time;
            Sprite cuadro = cuadros[0];
            float giro = 0f;
            float ancho = 1f;

            switch (estado)
            {
                case Estado.Vuela:
                {
                    if (visitaPendiente)
                    {
                        // Se acerca por encima del pergamino y baja a posarse
                        Vector2 sobre = puntoVisita + new Vector2(0f, 160f);
                        if ((sobre - pos).magnitude < 90f)
                        {
                            EmpezarAterrizajeEn(puntoVisita);
                        }
                        else
                        {
                            Navegar(sobre, dt, 1.5f);
                            cuadro = Aleteo(14f);
                            giro = Mathf.Clamp(velocidadActual.y * 0.05f, -14f, 14f);
                        }
                        break;
                    }

                    if (posarEnRepisaAlSalir && posaderos != null && posaderos.Length > 0)
                    {
                        posarEnRepisaAlSalir = false;
                        EmpezarAterrizaje(true);
                        break;
                    }

                    if (tranquila && posaderos != null && posaderos.Length > 0)
                    {
                        EmpezarAterrizaje();
                        break;
                    }

                    Navegar(destino, dt, 1f);
                    cuadro = Aleteo(13f);
                    giro = Mathf.Clamp(velocidadActual.y * 0.05f, -14f, 14f);
                    if ((destino - pos).magnitude < 60f)
                    {
                        if (posaderos != null && posaderos.Length > 0 && Time.time >= proximoPosarse)
                            EmpezarAterrizaje();
                        else
                            ElegirDestino();
                    }
                    break;
                }

                case Estado.Aterriza:
                {
                    float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(reloj / duracion));
                    Vector2 meta = CentroPosada();
                    pos = Vector2.Lerp(inicioAterrizaje, meta, k);
                    cuadro = k < 0.7f ? cuadros[9] : cuadros[10];
                    if (reloj >= duracion)
                    {
                        estado = Estado.Posada;
                        reloj = 0f;
                        duracion = usaPosaderoPropio ? segundosVisita : Random.Range(3.5f, 6.5f);
                    }
                    break;
                }

                case Estado.Posada:
                {
                    // Descansa: abre y cierra las alas despacio
                    cuadro = (tranquila || (int)(t / 1.1f) % 2 == 0) ? cuadros[10] : cuadros[5];
                    pos = CentroPosada(cuadro);
                    ancho = 1f + Mathf.Sin(t * 5f) * 0.015f;
                    if (usaPosaderoPropio)
                    {
                        // Sobre el pergamino: se va cuando pasa el rato o cuando se acaba el tiempo de la receta
                        if (salirYa || reloj >= duracion)
                        {
                            estado = Estado.Despega;
                            reloj = 0f;
                            duracion = 0.5f;
                        }
                    }
                    else if (tranquila && reloj >= duracion)
                    {
                        reloj = 0f;     // sigue descansando mientras esté tranquila
                    }
                    else if (reloj >= duracion)
                    {
                        estado = Estado.Despega;
                        reloj = 0f;
                        duracion = 0.5f;
                    }
                    break;
                }

                case Estado.Despega:
                {
                    float k = Mathf.Clamp01(reloj / duracion);
                    cuadro = k < 0.5f ? cuadros[6] : cuadros[7];
                    pos += new Vector2(mira * 40f, 120f) * dt;
                    if (reloj >= duracion)
                    {
                        proximoPosarse = Time.time + Random.Range(10f, 18f);
                        velocidadActual = new Vector2(mira * 80f, 120f);
                        ElegirDestino();
                        estado = atraerAlDespegar ? Estado.Atraida : Estado.Vuela;
                        atraerAlDespegar = false;
                        if (usaPosaderoPropio)
                        {
                            usaPosaderoPropio = false;
                            salirYa = false;
                            posarEnRepisaAlSalir = true;    // y se va a posar en alguna repisa
                            estado = Estado.Vuela;
                        }
                        reloj = 0f;
                        duracion = 3f;
                    }
                    break;
                }

                case Estado.Atraida:
                {
                    // Da vueltas alrededor del punto con magia
                    float angulo = t * 2.2f + fase;
                    Vector2 objetivo = centroAtraido + new Vector2(Mathf.Cos(angulo) * 140f, Mathf.Sin(angulo * 1.3f) * 70f + 20f);
                    Navegar(objetivo, dt, 2.2f);
                    cuadro = Aleteo(16f);
                    giro = Mathf.Clamp(velocidadActual.y * 0.05f, -14f, 14f);
                    if (reloj >= duracion)
                    {
                        ElegirDestino();
                        estado = Estado.Vuela;
                    }
                    break;
                }
            }

            // Rastro de brillos mientras vuela
            if (estado == Estado.Vuela || estado == Estado.Atraida)
            {
                proximaChispa -= dt;
                if (proximaChispa <= 0f)
                {
                    proximaChispa = Random.Range(0.08f, 0.16f);
                    Color color = Color.Lerp(new Color(0.7f, 0.8f, 1f), new Color(1f, 0.9f, 0.6f), Random.value);
                    Particula.Crear(capa, FrascoBrillante.Estrella(), pos + Random.insideUnitCircle * 25f * escala * 2f,
                        Vector2.one * Random.Range(10f, 22f), color, new Vector2(Random.Range(-15f, 15f), Random.Range(-30f, -5f)), -5f,
                        Random.Range(0.6f, 1.1f), Random.Range(-90f, 90f), 0.1f);
                }
            }

            // Colocar
            if (cuadro != null)
            {
                imagen.sprite = cuadro;
                rect.sizeDelta = cuadro.rect.size * escala;
            }

            imagen.enabled = cuadro != null;
            rect.anchoredPosition = pos;
            rect.localRotation = Quaternion.Euler(0f, 0f, giro);
            rect.localScale = new Vector3(mira * ancho, 1f, 1f);
        }

        Sprite Aleteo(float cuadrosPorSegundo)
        {
            return cuadros[CicloAleteo[(int)(Time.time * cuadrosPorSegundo + fase) % CicloAleteo.Length]];
        }

        /// <summary>Se dirige hacia un punto con movimiento suave (acelera y gira poco a poco), con un vaivén al volar.</summary>
        void Navegar(Vector2 objetivo, float dt, float rapidez)
        {
            Vector2 deseada = (objetivo - pos);
            float distancia = deseada.magnitude;
            deseada = distancia > 1f ? deseada / distancia : Vector2.zero;
            deseada *= velocidad * rapidez * Mathf.Clamp01(distancia / 120f + 0.35f);

            velocidadActual = Vector2.Lerp(velocidadActual, deseada, 1f - Mathf.Exp(-2.2f * dt));

            // Vaivén de mariposa: sube y baja al aletear
            float vaiven = Mathf.Sin(Time.time * 5.5f + fase) * 55f;
            pos += (velocidadActual + new Vector2(0f, vaiven)) * dt;

            if (Mathf.Abs(velocidadActual.x) > 25f)
                mira = velocidadActual.x >= 0f ? 1f : -1f;
        }

        /// <param name="soloRepisas">true para elegir solo entre los sitios altos (las repisas)</param>
        void EmpezarAterrizaje(bool soloRepisas = false)
        {
            usaPosaderoPropio = false;
            posaderoActual = Random.Range(0, posaderos.Length);
            if (soloRepisas)
            {
                var altos = new System.Collections.Generic.List<int>();
                for (int i = 0; i < posaderos.Length; i++)
                    if (posaderos[i].y > 100f)
                        altos.Add(i);
                if (altos.Count > 0)
                    posaderoActual = altos[Random.Range(0, altos.Count)];
            }

            inicioAterrizaje = pos;
            estado = Estado.Aterriza;
            reloj = 0f;
            duracion = 0.9f;
            mira = posaderos[posaderoActual].x >= pos.x ? 1f : -1f;
        }

        void EmpezarAterrizajeEn(Vector2 pies)
        {
            usaPosaderoPropio = true;
            posaderoPropio = pies;
            visitaPendiente = false;
            inicioAterrizaje = pos;
            estado = Estado.Aterriza;
            reloj = 0f;
            duracion = 0.7f;
            mira = pies.x >= pos.x ? 1f : -1f;
        }

        Vector2 CentroPosada(Sprite cuadro = null)
        {
            Sprite s = cuadro != null ? cuadro : cuadros[10];
            float alto = s != null ? s.rect.height * escala : 100f;
            // Las poses apoyadas tienen las patas al fondo del dibujo: el centro queda medio dibujo por encima del suelo
            Vector2 pies = usaPosaderoPropio ? posaderoPropio : posaderos[posaderoActual];
            return pies + new Vector2(0f, alto * 0.5f);
        }
    }
}
