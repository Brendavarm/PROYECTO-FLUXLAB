using UnityEngine;
using UnityEngine.UI;

namespace FluxLab
{
    /// <summary>
    /// Mishi, el gato del laboratorio. Cambia de dibujo según lo que esté haciendo
    /// (las hojas están en Resources/FluxLab/Mishi) y además se mueve, se estira y se aplasta por código.
    /// </summary>
    public class MishiAnimado : MonoBehaviour
    {
        public enum Estado
        {
            Quieta,
            Camina,
            Salta,
            Duerme,
            Maulla,
            Magia,
            Viaja
        }

        /// <summary>Un lugar donde Mishi puede estar de pie: de izquierda a derecha, a esa altura.</summary>
        public struct Plataforma
        {
            public float izq;
            public float der;
            public float y;

            public Plataforma(float izq, float der, float y)
            {
                this.izq = izq;
                this.der = der;
                this.y = y;
            }
        }

        // Los lugares por donde se mueve Mishi los define cada nivel (coordenadas del lienzo de 1920x1080,
        // centro = 0,0). Si no se pasa ninguno, usa una sola plataforma en el centro.
        Plataforma[] Plataformas = { new Plataforma(-300f, 300f, -200f) };

        public float velocidad = 70f;
        public float escala = 1.4f;       // tamaño: 1 = el dibujo tal cual
        public float alturaSalto = 70f;
        public float volumenRonroneo = 0.5f; // 0 = mudo, 1 = al máximo
        public float volumenHechizo = 0.7f;

        // Dibujos de cada fila de la hoja
        Sprite[] lado;
        Sprite[] ataque;
        Sprite[] magia;
        Sprite[] dormir;

        // Reposo: este orden de cuadros se repite para que respire y mueva la cola.
        static readonly int[] CicloReposo = { 0, 1, 2, 1, 0, 6, 7, 6 };

        RectTransform rect;
        RectTransform sombra;
        Image imagen;
        AudioSource voz;
        AudioClip[] maullidos;
        AudioClip ronroneo;
        AudioClip hechizo;

        Estado estado = Estado.Quieta;
        float reloj;
        float duracion;
        float x;
        float pisoY;
        int plataforma;
        float destino;
        // datos del viaje entre plataformas
        float viajeX0, viajeY0, viajeX1, viajeY1;
        int viajeA;
        float mira = -1f; // -1 = mira a la izquierda (como el dibujo), 1 = a la derecha
        float proxParpadeo = 2f;
        float tParpadeo = -1f;
        bool bolaLanzada;
        bool distraerAlLlegar;
        bool tranquila;

        /// <summary>
        /// En modo tranquilo Mishi solo descansa (sentada o dormida, en silencio). Quitarlo la despierta.
        /// Las reacciones a lo que hace el jugador (celebrar, maullar del susto) siguen funcionando.
        /// </summary>
        public bool Tranquila
        {
            get { return tranquila; }
            set
            {
                tranquila = value;
                if (!value && estado == Estado.Duerme)
                    Cambiar(Estado.Quieta, 0.5f);
            }
        }
        float impulsoBola = 120f;
        float ritmoMagia = 1f;      // 1 = normal; más alto = carga y lanza más rápido
        int bolasPendientes;        // cuántas bolas más lanzar seguidas

        /// <summary>
        /// Se llama cada vez que Mishi aterriza, con su posición (x) y la altura de la superficie (y).
        /// Debe devolver true si algo reaccionó (por ejemplo, un frasco se cayó): entonces Mishi maúlla del susto.
        /// Es opcional: un nivel que no lo necesite no lo usa.
        /// </summary>
        public System.Func<float, float, bool> AlAterrizar;

        /// <summary>Dónde está parada Mishi ahora.</summary>
        public float X { get { return x; } }
        public float PisoY { get { return pisoY; } }

        /// <summary>Un amigo (el grillo) está cerca: Mishi lo mira y salta tras él.</summary>
        public void ReaccionarA(float xDelOtro)
        {
            if (estado == Estado.Magia || estado == Estado.Viaja)
                return;
            mira = xDelOtro >= x ? 1f : -1f;
            Cambiar(Estado.Salta, 0.7f);
        }

        /// <summary>Crea a Mishi dentro del lienzo y devuelve su script.</summary>
        /// <param name="plataformas">Dónde puede estar de pie. Empieza en la primera.</param>
        public static MishiAnimado Crear(Transform lienzo, Plataforma[] plataformas = null)
        {
            var go = new GameObject("Mishi", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(lienzo, false);

            var imagen = go.GetComponent<Image>();
            imagen.preserveAspect = true;
            imagen.raycastTarget = false; // para que nunca bloquee un clic

            var r = go.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0f); // el pivote está en los pies

            var mishi = go.AddComponent<MishiAnimado>();
            if (plataformas != null && plataformas.Length > 0)
                mishi.Plataformas = plataformas;
            return mishi;
        }

        void Start()
        {
            rect = GetComponent<RectTransform>();
            imagen = GetComponent<Image>();

            lado = Cargar("lado", 8);
            ataque = Cargar("ataque", 8);
            magia = Cargar("magia", 7);
            dormir = Cargar("dormir", 6);

            CrearSombra();
            CrearAudio();

            plataforma = 0; // empieza en la primera plataforma
            x = (Plataformas[0].izq + Plataformas[0].der) * 0.5f;
            pisoY = Plataformas[0].y;
            Cambiar(Estado.Quieta, Random.Range(1.5f, 3f));
        }

        static Sprite[] Cargar(string fila, int cantidad)
        {
            var lista = new Sprite[cantidad];
            for (int i = 0; i < cantidad; i++)
                lista[i] = Resources.Load<Sprite>("FluxLab/Mishi/" + fila + "_" + i);
            return lista;
        }

        void CrearSombra()
        {
            var go = new GameObject("SombraMishi", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform.parent, false);
            go.transform.SetSiblingIndex(transform.GetSiblingIndex()); // justo detrás de Mishi

            var img = go.GetComponent<Image>();
            img.sprite = DiscoSuave(64);
            img.color = new Color(0.08f, 0.04f, 0.03f, 0.35f);
            img.raycastTarget = false;

            sombra = go.GetComponent<RectTransform>();
            sombra.anchorMin = sombra.anchorMax = new Vector2(0.5f, 0.5f);
            sombra.sizeDelta = new Vector2(190f, 28f);
        }

        void CrearAudio()
        {
            voz = gameObject.AddComponent<AudioSource>();
            voz.playOnAwake = false;

            maullidos = new[]
            {
                Resources.Load<AudioClip>("FluxLab/Audio/gato_1"),
                Resources.Load<AudioClip>("FluxLab/Audio/gato_2"),
                Resources.Load<AudioClip>("FluxLab/Audio/gato_3")
            };

            hechizo = Resources.Load<AudioClip>("FluxLab/Audio/hechizo");

            // Si existe Resources/FluxLab/Audio/ronroneo.(mp3/wav/ogg) se usa ese; si no, el provisional.
            ronroneo = Resources.Load<AudioClip>("FluxLab/Audio/ronroneo");
            if (ronroneo == null)
                ronroneo = CrearRonroneo();
        }

        void Update()
        {
            if (rect == null)
                return;

            PruebasDeTeclado();

            // Si el jugador apaga el sonido mientras ronronea, se calla al instante.
            if (voz != null && voz.loop)
                voz.volume = AjustesJuego.EfectosActivos ? volumenRonroneo : 0f;

            reloj += Time.deltaTime;
            float avance = duracion <= 0.01f ? 1f : Mathf.Clamp01(reloj / duracion);

            // Valores base: respirar suave todo el tiempo.
            float respirar = Mathf.Sin(Time.time * 2.2f);
            float baseY = pisoY;
            float y = baseY;
            float giro = 0f;
            float ancho = 1f - respirar * 0.01f;
            float alto = 1f + respirar * 0.02f;
            Sprite cuadro = null;

            switch (estado)
            {
                case Estado.Quieta:
                    cuadro = CuadroLado(5f);
                    if (reloj >= duracion)
                        ElegirSiguiente();
                    break;

                case Estado.Camina:
                    x = Mathf.MoveTowards(x, destino, velocidad * Time.deltaTime);
                    float paso = Time.time * 11f;
                    y += Mathf.Abs(Mathf.Sin(paso)) * 8f; // cada paso rebota un poco
                    giro = Mathf.Sin(paso) * 3f;          // y se balancea
                    cuadro = CuadroLado(9f);
                    if (Mathf.Approximately(x, destino))
                        Cambiar(Estado.Quieta, Random.Range(1.5f, 3.5f));
                    break;

                case Estado.Viaja:
                    // x avanza en línea recta; y sigue la recta más un arco, como un salto de verdad.
                    x = Mathf.Lerp(viajeX0, viajeX1, avance);
                    baseY = Mathf.Lerp(viajeY0, viajeY1, avance);
                    float vuelo = Mathf.Sin(avance * Mathf.PI);
                    y = baseY + vuelo * 130f;
                    alto *= 1f + vuelo * 0.08f;
                    ancho *= 1f - vuelo * 0.05f;
                    cuadro = avance < 0.12f ? ataque[0] : avance < 0.26f ? ataque[1] : avance < 0.85f ? ataque[6] : ataque[7];
                    if (avance >= 1f)
                    {
                        plataforma = viajeA;
                        pisoY = viajeY1;
                        x = viajeX1;
                        Cambiar(Estado.Quieta, Random.Range(1f, 2.5f));
                        Aterrizar();
                        if (distraerAlLlegar)
                        {
                            distraerAlLlegar = false;
                            Distraer();
                        }
                    }
                    break;

                case Estado.Salta:
                    float arco = Mathf.Sin(avance * Mathf.PI);   // 0 -> 1 -> 0
                    y += arco * alturaSalto;
                    alto *= 1f + arco * 0.08f;                    // se estira en el aire
                    ancho *= 1f - arco * 0.05f;
                    cuadro = avance < 0.12f ? ataque[0] : avance < 0.26f ? ataque[1] : avance < 0.85f ? ataque[6] : ataque[7];
                    if (avance >= 1f)
                    {
                        Cambiar(Estado.Quieta, Random.Range(1f, 2.5f));
                        Aterrizar();
                    }
                    break;

                case Estado.Duerme:
                    cuadro = CuadroDormir();
                    if (reloj >= duracion)
                        Cambiar(Estado.Quieta, Random.Range(1f, 2f));
                    break;

                case Estado.Maulla:
                    cuadro = lado[0];
                    float grito = Mathf.Sin(avance * Mathf.PI);
                    giro = grito * 5f * (-mira);              // levanta la cabeza
                    alto *= 1f + grito * 0.06f;
                    if (avance >= 1f)
                        Cambiar(Estado.Quieta, Random.Range(1f, 2.5f));
                    break;

                case Estado.Magia:
                    float tiempoMagia = reloj * ritmoMagia;
                    cuadro = CuadroMagia(tiempoMagia);
                    if (!bolaLanzada && tiempoMagia >= 1.2f)
                        LanzarBola();
                    if (tiempoMagia >= 2.1f)
                    {
                        if (bolasPendientes > 0)
                        {
                            // Otra bola enseguida, para seguir distrayendo
                            bolasPendientes--;
                            impulsoBola = 420f;
                            Lanzar();
                        }
                        else
                        {
                            ritmoMagia = 1f;
                            Cambiar(Estado.Quieta, Random.Range(1.5f, 3f));
                        }
                    }
                    break;
            }

            // Aplicar: el dibujo mira a la izquierda, así que se voltea con escala X negativa.
            if (cuadro != null)
            {
                imagen.sprite = cuadro;
                rect.sizeDelta = cuadro.rect.size * escala;
            }

            rect.anchoredPosition = new Vector2(x, y);
            rect.localRotation = Quaternion.Euler(0f, 0f, giro);
            rect.localScale = new Vector3(-mira * ancho, alto, 1f);

            if (sombra != null)
            {
                float elevacion = y - baseY;
                float s = Mathf.Clamp(1f - elevacion / 120f, 0.55f, 1f);
                sombra.anchoredPosition = new Vector2(x, baseY - 4f);
                sombra.localScale = new Vector3(s, s, 1f);
            }
        }

        /// <summary>Si cae cerca de un frasco de su misma superficie, lo tira; y maúlla del susto.</summary>
        void Aterrizar()
        {
            if (AlAterrizar != null && AlAterrizar(x, pisoY))
                Maullar();
        }

        /// <summary>Solo en el editor: F1 a F5 fuerzan cada acción para probarla sin esperar.</summary>
        void PruebasDeTeclado()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Input.GetKeyDown(KeyCode.F1)) Lanzar();
            if (Input.GetKeyDown(KeyCode.F2)) Maullar();
            if (Input.GetKeyDown(KeyCode.F3)) Cambiar(Estado.Duerme, 6f);
            if (Input.GetKeyDown(KeyCode.F4)) Viajar();
            if (Input.GetKeyDown(KeyCode.F5)) Cambiar(Estado.Salta, 0.7f);
#endif
        }

        // ---- Qué dibujo toca en cada momento ----

        Sprite CuadroLado(float cuadrosPorSegundo)
        {
            // De vez en cuando parpadea: cuadros 3, 4 y 5 de la fila.
            if (tParpadeo >= 0f)
            {
                tParpadeo += Time.deltaTime;
                int k = (int)(tParpadeo / 0.1f);
                if (k < 3)
                    return lado[3 + k];
                tParpadeo = -1f;
                proxParpadeo = Random.Range(2f, 5f);
            }
            else
            {
                proxParpadeo -= Time.deltaTime;
                if (proxParpadeo <= 0f)
                    tParpadeo = 0f;
            }

            return lado[CicloReposo[(int)(Time.time * cuadrosPorSegundo) % CicloReposo.Length]];
        }

        Sprite CuadroDormir()
        {
            const float acostarse = 0.4f;   // segundos por cuadro al acostarse
            float t = reloj;
            float tiempoDespertar = 0.9f;

            if (t < acostarse * 3f)
                return dormir[(int)(t / acostarse)];                  // cuadros 0, 1, 2
            if (t > duracion - tiempoDespertar)
            {
                int k = (int)((duracion - t) / (tiempoDespertar / 3f)); // vuelve 2, 1, 0
                return dormir[Mathf.Clamp(k, 0, 2)];
            }

            int[] dormido = { 3, 4, 5, 4 };                           // con zzz
            return dormir[dormido[(int)(t / 0.9f) % dormido.Length]];
        }

        Sprite CuadroMagia(float t)
        {
            if (t < 0.25f) return magia[0];
            if (t < 0.5f) return magia[1];
            if (t < 0.85f) return magia[2];   // empieza a cargar la luz
            if (t < 1.2f) return magia[3];
            if (t < 1.5f) return magia[4];    // la suelta
            return magia[6];                  // vuelve a sentarse
        }

        // ---- Qué hace Mishi y cuándo ----

        void ElegirSiguiente()
        {
            if (tranquila)
            {
                // En calma sigue recorriendo el escenario, pero sin ruido ni travesuras:
                // pasea, salta a otra plataforma, se sienta o duerme un rato (sin maullar, ronronear ni lanzar bolas).
                float calma = Random.value;
                Plataforma aqui = Plataformas[plataforma];
                bool anchaParaPasear = aqui.der - aqui.izq >= 100f;

                if (calma < 0.32f && Plataformas.Length > 1)
                {
                    Viajar();
                }
                else if (calma < 0.58f && anchaParaPasear)
                {
                    destino = Random.Range(aqui.izq, aqui.der);
                    mira = destino >= x ? 1f : -1f;
                    Cambiar(Estado.Camina, 0f);
                }
                else if (calma < 0.78f)
                {
                    Cambiar(Estado.Duerme, Random.Range(6f, 10f));
                }
                else
                {
                    Cambiar(Estado.Quieta, Random.Range(2f, 4f));
                }

                return;
            }

            float suerte = Random.value;
            Plataforma sitio = Plataformas[plataforma];
            if (suerte < 0.28f)
            {
                // En una repisa angosta no hay dónde pasear: mejor viaja a otro lado.
                if (sitio.der - sitio.izq < 100f)
                    Viajar();
                else
                {
                    destino = Random.Range(sitio.izq, sitio.der);
                    mira = destino >= x ? 1f : -1f;
                    Cambiar(Estado.Camina, 0f);
                }
            }
            else if (suerte < 0.50f)
            {
                Viajar();
            }
            else if (suerte < 0.62f)
            {
                Cambiar(Estado.Salta, 0.7f);
            }
            else if (suerte < 0.75f)
            {
                Maullar();
            }
            else if (suerte < 0.88f)
            {
                Lanzar();
            }
            else
            {
                Cambiar(Estado.Duerme, Random.Range(5f, 8f));
            }
        }

        /// <summary>Salta a otra plataforma (de la repisa a la mesa o al revés).</summary>
        void Viajar()
        {
            int otra = plataforma;
            while (otra == plataforma && Plataformas.Length > 1)
                otra = Random.Range(0, Plataformas.Length);
            ViajarA(otra);
        }

        void ViajarA(int otra)
        {
            Plataforma meta = Plataformas[otra];
            viajeX0 = x;
            viajeY0 = pisoY;
            viajeX1 = Random.Range(meta.izq, meta.der);
            viajeY1 = meta.y;
            viajeA = otra;
            mira = viajeX1 >= x ? 1f : -1f;
            Cambiar(Estado.Viaja, 1.05f);
        }

        void Cambiar(Estado nuevo, float segundos)
        {
            estado = nuevo;
            reloj = 0f;
            duracion = segundos;
            bolaLanzada = false;

            if (voz != null)
            {
                bool ronronea = nuevo == Estado.Duerme && AjustesJuego.EfectosActivos && !tranquila;   // dormida en silencio si está tranquila
                if (ronronea)
                {
                    voz.clip = ronroneo;
                    voz.loop = true;
                    voz.volume = volumenRonroneo;
                    voz.Play();
                }
                else if (voz.loop)
                {
                    voz.loop = false;
                    voz.Stop();
                }
            }
        }

        /// <summary>Maúlla con una de las voces y sigue con la cabeza arriba mientras suena.</summary>
        public void Maullar()
        {
            AudioClip clip = maullidos[Random.Range(0, maullidos.Length)];
            float segundos = clip != null ? Mathf.Clamp(clip.length, 0.8f, 2f) : 1f;
            Cambiar(Estado.Maulla, segundos);
            if (clip != null && AjustesJuego.EfectosActivos)
                voz.PlayOneShot(clip, 0.7f);
        }

        /// <summary>Para que el juego pueda hacerla saltar (por ejemplo al acertar).</summary>
        public void Celebrar()
        {
            Cambiar(Estado.Salta, 0.7f);
        }

        /// <summary>Lanza ya una bola de luz, sin esperar al azar.</summary>
        public void Lanzar()
        {
            // La bola sale hacia el lado donde hay más escenario (el centro).
            mira = x > 0f ? 1f : -1f;
            Cambiar(Estado.Magia, 2.1f);
        }

        /// <summary>
        /// Para distraer al jugador: sube a una repisa (si no está en una) y lanza la bola en alto,
        /// de modo que cruza por donde está la receta.
        /// </summary>
        /// <summary>Lanza varias bolas seguidas, cada una más rápido de lo normal (para distraer en las rondas difíciles).</summary>
        public void DistraerVarias(int veces)
        {
            ritmoMagia = 1.8f;
            bolasPendientes = Mathf.Max(0, veces - 1);
            Distraer();
        }

        public void Distraer()
        {
            if (Plataformas[plataforma].y > 0f)
            {
                impulsoBola = 420f;
                Lanzar();
                return;
            }

            for (int i = 0; i < Plataformas.Length; i++)
            {
                if (Plataformas[i].y > 0f)
                {
                    distraerAlLlegar = true;
                    ViajarA(i);
                    return;
                }
            }

            impulsoBola = 420f; // no hay repisas: lanza desde donde esté
            Lanzar();
        }

        void LanzarBola()
        {
            bolaLanzada = true;
            float direccion = -mira; // el dibujo original lanza hacia la derecha
            Vector2 mano = new Vector2(x + direccion * 70f * escala, pisoY + 65f * escala);
            BolaMagica.Lanzar(transform.parent, mano, direccion, impulsoBola);
            impulsoBola = 120f;

            if (hechizo != null && AjustesJuego.EfectosActivos)
                voz.PlayOneShot(hechizo, volumenHechizo);
        }

        // ---- Utilidades ----

        static Sprite DiscoSuave(int lado)
        {
            var textura = new Texture2D(lado, lado, TextureFormat.RGBA32, false);
            float radio = (lado - 1) * 0.5f;
            var centro = new Vector2(radio, radio);
            for (int py = 0; py < lado; py++)
            {
                for (int px = 0; px < lado; px++)
                {
                    float d = Vector2.Distance(new Vector2(px, py), centro) / radio;
                    float alpha = Mathf.Clamp01(1.15f - d);
                    textura.SetPixel(px, py, new Color(1f, 1f, 1f, alpha * alpha));
                }
            }

            textura.Apply();
            return Sprite.Create(textura, new Rect(0f, 0f, lado, lado), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>
        /// Ronroneo provisional: un zumbido grave que "late" 24 veces por segundo.
        /// Dura exactamente 1 s para que el bucle no se note. Se puede cambiar por un audio real.
        /// </summary>
        static AudioClip CrearRonroneo()
        {
            const int frecuencia = 44100;
            var datos = new float[frecuencia];
            var azar = new System.Random(7);
            for (int i = 0; i < datos.Length; i++)
            {
                float t = i / (float)frecuencia;
                float latido = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 24f * t);
                float tono = Mathf.Sin(2f * Mathf.PI * 55f * t) + 0.6f * Mathf.Sin(2f * Mathf.PI * 110f * t);
                float ruido = (float)(azar.NextDouble() * 2.0 - 1.0) * 0.25f;
                datos[i] = (tono + ruido) * latido * 0.3f;
            }

            var clip = AudioClip.Create("ronroneo", datos.Length, 1, frecuencia, false);
            clip.SetData(datos, 0);
            return clip;
        }
    }
}
