using UnityEngine;
using UnityEngine.UI;

namespace FluxLab.Nivel2
{
    /// <summary>Todas las animaciones que sabe hacer Magnus.</summary>
    public enum AnimacionMagnus
    {
        Quieto,
        Observar,
        Remover,
        Reir,
        Sorprenderse,
        Asustarse,
        Saltar,
        Bailar,
        Cantar,
        Meditar,
        Pensar,
        Enojarse,
        Triste,
        Lanzar,
        Volar,
        FinalAlegre,
        FinalContento,
        FinalAnimo
    }

    /// <summary>
    /// Biblioteca de animaciones de Magnus. Cambia de pose (las imágenes de Imagenes-animacion,
    /// con la personalización aplicada) y mueve el cuerpo por código, más efectos de caricatura
    /// (estrellitas, "!", "?", gotas, lágrimas, notas, corazones).
    ///
    /// Para que se vea fluido y natural:
    ///  - Las poses no cambian de golpe: la anterior se desvanece mientras aparece la nueva.
    ///  - Posición, giro y tamaño siguen a su objetivo con RESORTES: aceleran, se pasan un poquito
    ///    y se acomodan, nunca saltan de un valor a otro.
    ///  - Anticipación y rebote: se agacha antes de saltar o sorprenderse y se aplasta al caer.
    ///
    /// Uso: MagnusAnimado.Crear(...) y luego Reproducir(AnimacionMagnus.X) o los atajos (Reir(), Bailar()...).
    /// Las animaciones cortas vuelven solas a Quieto; las largas (Bailar, Cantar, Meditar, Triste y las
    /// finales) se repiten si se piden con segundos menor que 0.
    /// Para verlas todas sin jugar: escena Nivel2_GaleriaMagnus.
    /// </summary>
    public class MagnusAnimado : MonoBehaviour
    {
        const float DuracionFundido = 0.16f;

        /// <summary>Un valor que sigue a su objetivo como un resorte (con un poquito de rebote).</summary>
        struct Resorte
        {
            public float valor;
            public float velocidad;

            public float Seguir(float objetivo, float frecuencia, float amortiguacion, float dt)
            {
                // Se calcula en pasitos de menos de 1/120 s: con pasos grandes (juego a 30 cuadros por segundo)
                // un resorte rápido se vuelve inestable, crece sin control y termina en NaN (Magnus desaparecía).
                float w = 2f * Mathf.PI * frecuencia;
                int pasos = Mathf.Max(1, Mathf.CeilToInt(dt * 120f));
                float h = dt / pasos;
                for (int i = 0; i < pasos; i++)
                {
                    velocidad += (w * w * (objetivo - valor) - 2f * amortiguacion * w * velocidad) * h;
                    valor += velocidad * h;
                }

                // Seguro: si algo se rompe igual, vuelve al objetivo en vez de desaparecer.
                if (float.IsNaN(valor) || float.IsInfinity(valor) || Mathf.Abs(valor - objetivo) > 5000f)
                {
                    valor = objetivo;
                    velocidad = 0f;
                }

                return valor;
            }
        }

        RectTransform raiz;
        RectTransform cuerpo;
        RawImage imagen;
        RawImage anterior;          // la pose que se está desvaneciendo
        Image sombra;
        Image aura;
        Image accesorio;
        Text marca;
        ParticulasUI efectos;
        Font fuente;

        PersonalizacionMagnus aspecto;
        float alto;
        bool conAccesorio;
        PoseMagnus pose = (PoseMagnus)(-1);
        float fundido = 1f;         // 0 = recién cambió de pose, 1 = ya terminó el fundido

        AnimacionMagnus actual = AnimacionMagnus.Quieto;
        float reloj;
        float duracion;
        bool repetir;
        float relojEfecto;
        bool pico;
        Vector2 volarDesde;
        Vector2 volarHasta;

        Resorte rx, ry, rGiro, rAncho, rAlto, rMirar;

        public AnimacionMagnus Actual => actual;

        // ───────────────────────── Crear ─────────────────────────

        /// <summary>
        /// pies = dónde se para (coordenadas del padre); alto = cuánto mide de pie.
        /// conAccesorio: muestra el frasco del vestidor cuando está en la pose normal.
        /// </summary>
        public static MagnusAnimado Crear(Transform padre, Vector2 pies, float alto, PersonalizacionMagnus aspecto, bool conAccesorio)
        {
            var go = new GameObject("MagnusAnimado", typeof(RectTransform));
            go.transform.SetParent(padre, false);
            var magnus = go.AddComponent<MagnusAnimado>();
            magnus.Armar(pies, alto, aspecto, conAccesorio);
            return magnus;
        }

        void Armar(Vector2 pies, float altoDePie, PersonalizacionMagnus personalizacion, bool accesorioVisible)
        {
            fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            alto = altoDePie;
            aspecto = personalizacion;
            conAccesorio = accesorioVisible;
            rAncho.valor = rAlto.valor = rMirar.valor = 1f;

            raiz = GetComponent<RectTransform>();
            raiz.anchorMin = raiz.anchorMax = new Vector2(0.5f, 0.5f);
            raiz.sizeDelta = Vector2.zero;
            raiz.anchoredPosition = pies;

            aura = NuevaImagen(raiz, "Aura", FormasUI.Humo, new Vector2(0f, alto * 0.45f), new Vector2(alto * 1.3f, alto * 1.3f));
            aura.color = new Color(1f, 0.9f, 0.5f, 0f);

            sombra = NuevaImagen(raiz, "Sombra", FormasUI.Humo, Vector2.zero, new Vector2(alto * 0.42f, alto * 0.09f));
            sombra.color = new Color(0.08f, 0.04f, 0.03f, 0.45f);

            var go = new GameObject("Cuerpo", typeof(RectTransform));
            go.transform.SetParent(raiz, false);
            cuerpo = go.GetComponent<RectTransform>();
            cuerpo.anchorMin = cuerpo.anchorMax = new Vector2(0.5f, 0.5f);
            cuerpo.pivot = new Vector2(0.5f, 0f);   // se para, gira y se estira desde los pies
            cuerpo.sizeDelta = Vector2.zero;
            anterior = NuevaImagenRaw(cuerpo, "PoseAnterior");
            imagen = NuevaImagenRaw(cuerpo, "Pose");

            Sprite frasco = ImagenesFrascos.Accesorio;
            accesorio = NuevaImagen(imagen.rectTransform, "Accesorio", frasco, Vector2.zero, new Vector2(36f, 58f) * (alto / 380f));
            accesorio.preserveAspect = true;
            accesorio.enabled = false;

            var capa = new GameObject("Efectos", typeof(RectTransform));
            capa.transform.SetParent(raiz, false);
            efectos = capa.AddComponent<ParticulasUI>();

            marca = new GameObject("Marca", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)).GetComponent<Text>();
            marca.transform.SetParent(raiz, false);
            marca.font = fuente;
            marca.fontSize = Mathf.RoundToInt(alto * 0.22f);
            marca.fontStyle = FontStyle.Bold;
            marca.alignment = TextAnchor.MiddleCenter;
            marca.horizontalOverflow = HorizontalWrapMode.Overflow;
            marca.verticalOverflow = VerticalWrapMode.Overflow;
            marca.raycastTarget = false;
            marca.rectTransform.sizeDelta = new Vector2(120f, 120f);
            var contorno = marca.gameObject.AddComponent<Outline>();
            contorno.effectColor = new Color(0.25f, 0.1f, 0.05f, 1f);
            contorno.effectDistance = new Vector2(3f, -3f);
            marca.enabled = false;

            PonerPose(PoseMagnus.Normal, false);
        }

        /// <summary>Cambia la personalización (pelo, túnica, gafas) y redibuja la pose actual.</summary>
        public void CambiarAspecto(PersonalizacionMagnus nuevo)
        {
            aspecto = nuevo;
            PoseMagnus actualPose = pose;
            pose = (PoseMagnus)(-1);
            PonerPose(actualPose, true);
        }

        /// <summary>Mueve a Magnus a otro lugar (por ejemplo después de volar).</summary>
        public void Colocar(Vector2 pies)
        {
            raiz.anchoredPosition = pies;
        }

        // ───────────────────────── Reproducir ─────────────────────────

        /// <summary>
        /// segundos: 0 = lo que dura normalmente; menor que 0 = se repite hasta que se pida otra.
        /// </summary>
        public void Reproducir(AnimacionMagnus animacion, float segundos = 0f)
        {
            actual = animacion;
            reloj = 0f;
            relojEfecto = 0f;
            pico = false;
            repetir = segundos < 0f;
            duracion = segundos > 0f ? segundos : DuracionNormal(animacion);
            marca.enabled = false;
            imagen.color = Color.white;
            PonerPose(PoseDe(animacion, 0f), true);
            AlEmpezar(animacion);
        }

        public void Quieto() => Reproducir(AnimacionMagnus.Quieto, -1f);
        public void Observar() => Reproducir(AnimacionMagnus.Observar, -1f);
        public void Remover() => Reproducir(AnimacionMagnus.Remover);
        public void Reir(float segundos = 0f) => Reproducir(AnimacionMagnus.Reir, segundos);
        public void Sorprenderse() => Reproducir(AnimacionMagnus.Sorprenderse);
        public void Asustarse() => Reproducir(AnimacionMagnus.Asustarse);
        public void Saltar() => Reproducir(AnimacionMagnus.Saltar);
        public void Bailar(float segundos = -1f) => Reproducir(AnimacionMagnus.Bailar, segundos);
        public void Cantar(float segundos = -1f) => Reproducir(AnimacionMagnus.Cantar, segundos);
        public void Meditar(float segundos = -1f) => Reproducir(AnimacionMagnus.Meditar, segundos);
        public void Pensar() => Reproducir(AnimacionMagnus.Pensar);
        public void Enojarse() => Reproducir(AnimacionMagnus.Enojarse);
        public void Llorar(float segundos = -1f) => Reproducir(AnimacionMagnus.Triste, segundos);
        public void Lanzar() => Reproducir(AnimacionMagnus.Lanzar);
        public void FinalAlegre() => Reproducir(AnimacionMagnus.FinalAlegre, -1f);
        public void FinalContento() => Reproducir(AnimacionMagnus.FinalContento, -1f);
        public void FinalAnimo() => Reproducir(AnimacionMagnus.FinalAnimo, -1f);

        /// <summary>Vuela desde un punto hasta otro (coordenadas del padre) y se queda allí.</summary>
        public void Volar(Vector2 desde, Vector2 hasta, float segundos = 1.6f)
        {
            volarDesde = desde;
            volarHasta = hasta;
            Colocar(desde);
            Reproducir(AnimacionMagnus.Volar, segundos);
        }

        static float DuracionNormal(AnimacionMagnus animacion)
        {
            switch (animacion)
            {
                case AnimacionMagnus.Remover: return 0.5f;
                case AnimacionMagnus.Reir: return 1.4f;
                case AnimacionMagnus.Sorprenderse: return 1.3f;
                case AnimacionMagnus.Asustarse: return 1.4f;
                case AnimacionMagnus.Saltar: return 1.3f;
                case AnimacionMagnus.Pensar: return 2.6f;
                case AnimacionMagnus.Enojarse: return 1.6f;
                case AnimacionMagnus.Lanzar: return 0.9f;
                case AnimacionMagnus.Volar: return 1.6f;
                default: return 3f;
            }
        }

        static PoseMagnus PoseDe(AnimacionMagnus animacion, float t)
        {
            switch (animacion)
            {
                case AnimacionMagnus.Reir: return PoseMagnus.Riendo;
                case AnimacionMagnus.Sorprenderse: return PoseMagnus.Sorprendido;
                case AnimacionMagnus.Asustarse: return PoseMagnus.Asustado;
                case AnimacionMagnus.Saltar: return PoseMagnus.Saltando;
                case AnimacionMagnus.Bailar: return PoseMagnus.Bailando;
                case AnimacionMagnus.Cantar: return PoseMagnus.Cantando;
                case AnimacionMagnus.Meditar: return PoseMagnus.Meditando;
                case AnimacionMagnus.Pensar: return PoseMagnus.Pensando;
                case AnimacionMagnus.Enojarse: return PoseMagnus.Enojado;
                case AnimacionMagnus.Triste: return PoseMagnus.Triste;
                case AnimacionMagnus.Lanzar: return PoseMagnus.Tirando;
                case AnimacionMagnus.Volar: return PoseMagnus.Volando;
                case AnimacionMagnus.FinalContento: return PoseMagnus.Riendo;
                case AnimacionMagnus.FinalAnimo: return PoseMagnus.Triste;
                // Final alegre: baila 2.4 s y salta 1.3 s, una y otra vez.
                case AnimacionMagnus.FinalAlegre: return Mathf.Repeat(t, CicloFiesta) < 2.4f ? PoseMagnus.Bailando : PoseMagnus.Saltando;
                default: return PoseMagnus.Normal;
            }
        }

        const float CicloFiesta = 3.7f;

        void AlEmpezar(AnimacionMagnus animacion)
        {
            Vector2 cabeza = new Vector2(0f, alto * 0.88f);
            switch (animacion)
            {
                case AnimacionMagnus.Reir:
                case AnimacionMagnus.FinalContento:
                    Chispas(FormasUI.Destello, cabeza, 7, new Color(1f, 0.92f, 0.5f, 1f));
                    break;
                case AnimacionMagnus.Sorprenderse:
                    Marca("!", new Color(1f, 0.85f, 0.3f, 1f));
                    break;
                case AnimacionMagnus.Pensar:
                    Marca("?", new Color(0.7f, 0.85f, 1f, 1f));
                    break;
                case AnimacionMagnus.Asustarse:
                    for (int i = 0; i < 4; i++)
                        efectos.Una(FormasUI.Punto, cabeza + new Vector2(Random.Range(-alto * 0.2f, alto * 0.2f), 0f), Vector2.one * Random.Range(10f, 16f),
                            new Color(0.6f, 0.85f, 1f, 1f), new Vector2(Random.Range(-120f, 120f), Random.Range(80f, 180f)), 0.8f, 600f, 0f, 0f);
                    break;
            }
        }

        // ───────────────────────── Cada cuadro ─────────────────────────

        void Update()
        {
            // Si el juego se pausa (deltaTime = 0) todo se congela; se limita el paso para que los resortes no se disparen.
            float dt = Mathf.Min(Time.deltaTime, 1f / 30f);
            reloj += dt;
            relojEfecto += dt;

            if (!repetir && reloj >= duracion && actual != AnimacionMagnus.Quieto && actual != AnimacionMagnus.Observar)
            {
                if (actual == AnimacionMagnus.Volar)
                {
                    Colocar(volarHasta);
                    rAlto.velocidad -= 2.5f;   // aterriza: se aplasta un poquito
                    rAncho.velocidad += 2f;
                }

                Reproducir(AnimacionMagnus.Quieto, -1f);
            }

            float t = reloj;
            float k = duracion > 0f ? Mathf.Clamp01(reloj / duracion) : 0f;
            PonerPose(PoseDe(actual, t), true);

            // Objetivos de esta animación (los resortes los siguen suavemente).
            Vector2 mover = Vector2.zero;
            float giro = 0f;
            float ancho = 1f;
            float estirar = 1f;
            float mirar = 1f;               // 1 = mira a la derecha, -1 = a la izquierda (se da vuelta suave)
            float tiempo = Time.time;
            float respirar = Mathf.Sin(tiempo * 2.1f);

            switch (actual)
            {
                case AnimacionMagnus.Quieto:
                    // Respira y pasa el peso de un pie al otro, muy suave.
                    mover = new Vector2(Mathf.Sin(tiempo * 0.7f) * 2.5f, respirar * 2.5f);
                    giro = Mathf.Sin(tiempo * 0.7f + 0.6f) * 1.2f;
                    ancho = 1f - respirar * 0.01f;
                    estirar = 1f + respirar * 0.018f;
                    break;

                case AnimacionMagnus.Observar:
                    mover = new Vector2(0f, 4f + respirar * 2f);
                    giro = -5f + Mathf.Sin(tiempo * 1.1f) * 1.5f;
                    break;

                case AnimacionMagnus.Remover:
                {
                    float ola = Mathf.Sin(k * Mathf.PI * 2f);
                    mover = new Vector2(Suave(Mathf.Sin(k * Mathf.PI)) * 26f, Mathf.Abs(ola) * 5f);
                    giro = ola * 8f;
                    break;
                }

                case AnimacionMagnus.Reir:
                {
                    // Rebotes que se van apagando, con la cabeza que se mece.
                    float apagar = Mathf.Exp(-t * 1.6f);
                    float rebote = Mathf.Abs(Mathf.Sin(t * 10f)) * apagar;
                    mover.y = rebote * 14f;
                    estirar = 1f + rebote * 0.05f;
                    ancho = 1f - rebote * 0.03f;
                    giro = Mathf.Sin(t * 5f) * 4f * apagar;
                    break;
                }

                case AnimacionMagnus.Sorprenderse:
                    // Se encoge (anticipación), da un saltito estirado y se queda temblando un poco.
                    if (t < 0.09f)
                    {
                        estirar = 0.9f;
                        ancho = 1.06f;
                    }
                    else if (t < 0.38f)
                    {
                        float u = (t - 0.09f) / 0.29f;
                        mover.y = Mathf.Sin(u * Mathf.PI) * 36f;
                        estirar = 1.08f;
                        ancho = 0.96f;
                    }
                    else
                    {
                        mover.x = Mathf.Sin(t * 38f) * 2.5f * (1f - k);
                    }
                    MoverMarca(k);
                    break;

                case AnimacionMagnus.Asustarse:
                {
                    float atras = Suave(Mathf.Clamp01(t / 0.25f));
                    mover.x = -atras * 26f + Mathf.Sin(t * 40f) * 3.5f * (1f - k);
                    mover.y = Mathf.Sin(Mathf.Clamp01(t / 0.25f) * Mathf.PI) * 18f;
                    giro = atras * 5f;
                    break;
                }

                case AnimacionMagnus.Saltar:
                    Salto(t, ref mover, ref ancho, ref estirar, ref giro);
                    break;

                case AnimacionMagnus.Bailar:
                    Baile(t, ref mover, ref giro, ref mirar, ref estirar);
                    break;

                case AnimacionMagnus.Cantar:
                    giro = Mathf.Sin(t * 1.8f) * 4f;
                    mover = new Vector2(Mathf.Sin(t * 0.9f) * 4f, Mathf.Abs(Mathf.Sin(t * 3.6f)) * 4f);
                    Seguido(0.5f, () => Flotar(FormasUI.Nota, new Vector2(alto * 0.3f, alto * 0.85f), ColorNota()));
                    break;

                case AnimacionMagnus.Meditar:
                    // Sube despacio y flota.
                    mover.y = Suave(Mathf.Clamp01(t / 0.8f)) * 18f + Mathf.Sin(t * 1.4f) * 7f;
                    giro = Mathf.Sin(t * 0.9f) * 1.5f;
                    Seguido(0.25f, () =>
                    {
                        float angulo = Random.Range(0f, Mathf.PI * 2f);
                        Vector2 punto = new Vector2(Mathf.Cos(angulo) * alto * 0.4f, alto * 0.35f + Mathf.Sin(angulo) * alto * 0.15f) + mover;
                        efectos.Una(FormasUI.Destello, punto, Vector2.one * Random.Range(14f, 24f), new Color(1f, 0.92f, 0.6f, 1f), new Vector2(0f, 20f), 1f, 0f, 90f, 0.3f);
                    });
                    break;

                case AnimacionMagnus.Pensar:
                    // Se asoma desde abajo y ladea la cabeza.
                    mover.y = Mathf.Lerp(-alto * 0.25f, 0f, Suave(Mathf.Clamp01(t / 0.45f)));
                    giro = Mathf.Sin(t * 1.3f) * 3f;
                    MoverMarca(k);
                    break;

                case AnimacionMagnus.Enojarse:
                {
                    float pulso = Mathf.Abs(Mathf.Sin(t * 10f));
                    mover = new Vector2(Mathf.Sin(t * 30f) * 2f * (1f - k), -pulso * 4f);
                    estirar = 1f - pulso * 0.025f;
                    imagen.color = Color.Lerp(Color.white, new Color(1f, 0.78f, 0.75f, 1f), pulso * (1f - k));
                    Seguido(0.22f, () => efectos.Una(FormasUI.Humo, new Vector2(Random.Range(-alto * 0.15f, alto * 0.15f), alto * 0.95f), Vector2.one * Random.Range(26f, 40f),
                        new Color(0.85f, 0.85f, 0.85f, 0.7f), new Vector2(Random.Range(-20f, 20f), 90f), 0.7f, 0f, 0f, 1.2f));
                    break;
                }

                case AnimacionMagnus.Triste:
                    giro = Mathf.Sin(t * 1f) * 2f;
                    estirar = 0.98f + Mathf.Sin(t * 1.6f) * 0.008f;
                    mover.y = -3f;
                    Seguido(0.7f, () => efectos.Una(FormasUI.Punto, new Vector2(alto * 0.08f, alto * 0.78f), Vector2.one * 12f,
                        new Color(0.55f, 0.8f, 1f, 1f), new Vector2(0f, -10f), 0.9f, 260f, 0f, -0.2f));
                    break;

                case AnimacionMagnus.Lanzar:
                {
                    // Toma impulso hacia atrás y lanza hacia adelante.
                    float impulso = k < 0.3f ? -Suave(k / 0.3f) : Mathf.Lerp(-1f, 1f, Suave((k - 0.3f) / 0.3f)) * (1f - Suave(Mathf.Clamp01((k - 0.6f) / 0.4f)));
                    giro = -impulso * 8f;
                    mover.x = impulso * 14f;
                    if (!pico && k >= 0.4f)
                    {
                        pico = true;
                        efectos.Una(FormasUI.Anillo, new Vector2(alto * 0.45f, alto * 0.62f), new Vector2(60f, 24f), new Color(1f, 1f, 1f, 0.8f), new Vector2(160f, 0f), 0.4f, 0f, 0f, 1.5f);
                    }
                    break;
                }

                case AnimacionMagnus.Volar:
                {
                    Vector2 medio = (volarDesde + volarHasta) * 0.5f + new Vector2(0f, 160f);
                    float u = Suave(k);
                    Vector2 punto = Curva(volarDesde, medio, volarHasta, u);
                    raiz.anchoredPosition = punto;
                    mover.y = alto * 0.22f * Mathf.Sin(k * Mathf.PI) + Mathf.Sin(t * 5f) * 5f;
                    mirar = volarHasta.x < volarDesde.x ? -1f : 1f;
                    Vector2 adelante = Curva(volarDesde, medio, volarHasta, Mathf.Min(1f, u + 0.02f)) - punto;
                    giro = Mathf.Clamp(Mathf.Atan2(adelante.y, Mathf.Abs(adelante.x) + 0.001f) * Mathf.Rad2Deg * 0.4f, -20f, 20f) * mirar;
                    Seguido(0.06f, () => efectos.Una(FormasUI.Destello, new Vector2(-mirar * alto * 0.4f, alto * 0.3f) + mover,
                        Vector2.one * Random.Range(12f, 20f), new Color(1f, 0.95f, 0.7f, 1f), Vector2.zero, 0.5f, 0f, 120f, -0.5f));
                    break;
                }

                case AnimacionMagnus.FinalAlegre:
                {
                    float ciclo = Mathf.Repeat(t, CicloFiesta);
                    if (ciclo < 2.4f)
                        Baile(ciclo, ref mover, ref giro, ref mirar, ref estirar);
                    else
                        Salto(ciclo - 2.4f, ref mover, ref ancho, ref estirar, ref giro);
                    Seguido(0.3f, () => Flotar(Random.value < 0.5f ? FormasUI.Corazon : Estrella, new Vector2(Random.Range(-alto * 0.4f, alto * 0.4f), alto * 0.9f), ColorFiesta()));
                    break;
                }

                case AnimacionMagnus.FinalContento:
                    mover.y = Mathf.Abs(Mathf.Sin(t * 2.6f)) * 6f;
                    giro = Mathf.Sin(t * 1.3f) * 2.5f;
                    Seguido(1.2f, () => Chispas(FormasUI.Destello, new Vector2(0f, alto * 0.88f), 3, new Color(1f, 0.92f, 0.5f, 1f)));
                    break;

                case AnimacionMagnus.FinalAnimo:
                    giro = Mathf.Sin(t * 1f) * 2f;
                    estirar = 0.98f + Mathf.Sin(t * 1.6f) * 0.01f;
                    Seguido(0.8f, () => Flotar(FormasUI.Corazon, new Vector2(Random.Range(-alto * 0.25f, alto * 0.25f), alto * 0.85f), new Color(1f, 0.55f, 0.7f, 1f)));
                    break;
            }

            // Los resortes siguen a los objetivos: la posición rápido, el tamaño con un poquito más de rebote.
            float px = rx.Seguir(mover.x, 6f, 0.7f, dt);
            float py = ry.Seguir(mover.y, 7f, 0.62f, dt);
            float g = rGiro.Seguir(giro, 5f, 0.6f, dt);
            float sx = rAncho.Seguir(ancho, 6f, 0.4f, dt);
            float sy = rAlto.Seguir(estirar, 6f, 0.4f, dt);
            float m = rMirar.Seguir(mirar, 4f, 0.85f, dt);

            cuerpo.anchoredPosition = new Vector2(px, py);
            cuerpo.localRotation = Quaternion.Euler(0f, 0f, g);
            cuerpo.localScale = new Vector3(sx * m, sy, 1f);

            // Fundido entre la pose anterior y la nueva.
            fundido = Mathf.MoveTowards(fundido, 1f, dt / DuracionFundido);
            float f = Suave(fundido);
            imagen.color = new Color(imagen.color.r, imagen.color.g, imagen.color.b, f);
            anterior.color = new Color(1f, 1f, 1f, 1f - f);
            anterior.enabled = fundido < 1f;

            float elevacion = Mathf.Max(0f, py);
            float tamanoSombra = Mathf.Clamp(1f - elevacion / (alto * 0.5f), 0.5f, 1f);
            sombra.rectTransform.anchoredPosition = new Vector2(px, 0f);
            sombra.rectTransform.localScale = new Vector3(tamanoSombra, tamanoSombra, 1f);
            sombra.enabled = actual != AnimacionMagnus.Volar && pose != PoseMagnus.Pensando;

            bool meditando = actual == AnimacionMagnus.Meditar;
            Color luz = aura.color;
            luz.a = Mathf.MoveTowards(luz.a, meditando ? 0.45f + Mathf.Sin(t * 2f) * 0.1f : 0f, dt * 2f);
            aura.color = luz;
            aura.rectTransform.anchoredPosition = new Vector2(px, alto * 0.45f + py);
        }

        // Salto con anticipación: se agacha, sale estirado, cae y se aplasta. Dura 1.3 s.
        void Salto(float t, ref Vector2 mover, ref float ancho, ref float estirar, ref float giro)
        {
            const float agacharse = 0.18f;
            const float aire = 0.85f;
            if (t < agacharse)
            {
                float u = Suave(t / agacharse);
                mover.y = -u * 8f;
                estirar = 1f - u * 0.12f;
                ancho = 1f + u * 0.08f;
            }
            else if (t < agacharse + aire)
            {
                float u = (t - agacharse) / aire;
                mover.y = Mathf.Sin(u * Mathf.PI) * alto * 0.38f;
                estirar = u < 0.5f ? 1.07f : 1.02f;
                ancho = u < 0.5f ? 0.95f : 0.99f;
                giro = Mathf.Sin(u * Mathf.PI * 2f) * 3f;
                if (!pico && u >= 0.5f)
                {
                    pico = true;
                    Chispas(Estrella, new Vector2(0f, alto * 1.1f) + mover, 10, Color.white);
                }
            }
            else
            {
                // Al tocar el suelo: aplastado; el resorte lo devuelve rebotando.
                estirar = 0.88f;
                ancho = 1.08f;
                pico = t > agacharse + aire + 0.12f ? false : pico;
            }
        }

        // Baile: dos pasos por segundo, se mece y cada dos pasos se da vuelta (suave, como girando).
        void Baile(float t, ref Vector2 mover, ref float giro, ref float mirar, ref float estirar)
        {
            float paso = t * 2f;
            float vaiven = Mathf.Sin(paso * Mathf.PI);
            mover.x = vaiven * 16f;
            mover.y = Mathf.Abs(vaiven) * 12f;
            giro = vaiven * 7f;
            estirar = 1f + Mathf.Abs(vaiven) * 0.03f;
            mirar = Mathf.FloorToInt(paso / 2f) % 2 == 0 ? 1f : -1f;
            Seguido(0.4f, () => Flotar(Random.value < 0.5f ? FormasUI.Nota : FormasUI.Corazon, new Vector2(Random.Range(-alto * 0.35f, alto * 0.35f), alto * 0.8f), ColorFiesta()));
        }

        // ───────────────────────── Ayudas ─────────────────────────

        void PonerPose(PoseMagnus nueva, bool conFundido)
        {
            if (nueva == pose)
                return;
            Texture2D textura = AspectoPoses.Textura(nueva, aspecto);
            if (textura == null && nueva != PoseMagnus.Normal)
            {
                nueva = PoseMagnus.Normal;
                textura = AspectoPoses.Textura(nueva, aspecto);
            }

            // La pose que se va queda encima un momento y se desvanece.
            if (conFundido && imagen.texture != null)
            {
                anterior.texture = imagen.texture;
                anterior.rectTransform.sizeDelta = imagen.rectTransform.sizeDelta;
                fundido = 0f;
                rAlto.velocidad -= 0.6f;   // pequeño "pop" al cambiar de pose
                rAncho.velocidad += 0.5f;
            }

            pose = nueva;
            imagen.texture = textura;
            if (textura != null)
            {
                // La pose Normal es una imagen grande (se escala por su propio alto); las demás se dibujaron a ~305 px de pie.
                float pixelesDePie = nueva == PoseMagnus.Normal ? textura.height : PosesMagnus.PixelesDePiePoses;
                imagen.rectTransform.sizeDelta = new Vector2(textura.width, textura.height) * (alto / pixelesDePie);
            }

            // El frasco del vestidor solo calza con la pose normal.
            accesorio.enabled = conAccesorio && accesorio.sprite != null && nueva == PoseMagnus.Normal;
            if (accesorio.enabled)
            {
                // En la mano derecha de Magnus (a la izquierda de la imagen), contado desde el centro de la imagen.
                Vector2 tamano = imagen.rectTransform.sizeDelta;
                accesorio.rectTransform.anchoredPosition = new Vector2(-tamano.x * 0.4f, -tamano.y * 0.13f);
            }
        }

        // Ejecuta "accion" cada "cada" segundos mientras dure la animación.
        void Seguido(float cada, System.Action accion)
        {
            if (relojEfecto < cada)
                return;
            relojEfecto = 0f;
            accion();
        }

        void Chispas(Sprite forma, Vector2 centro, int cantidad, Color color)
        {
            efectos.Emitir(new ParticulasUI.Rafaga
            {
                forma = forma, cantidad = cantidad, centro = centro, dispersion = new Vector2(alto * 0.15f, alto * 0.05f),
                color = color, angulo = new Vector2(20f, 160f), rapidez = new Vector2(80f, 200f),
                vida = new Vector2(0.6f, 1f), tamano = new Vector2(16f, 28f), gravedad = 120f, giro = 200f
            });
        }

        void Flotar(Sprite forma, Vector2 desde, Color color)
        {
            efectos.Una(forma, desde, Vector2.one * Random.Range(20f, 32f), color,
                new Vector2(Random.Range(-30f, 30f), Random.Range(70f, 120f)), Random.Range(1.2f, 1.8f), -10f, Random.Range(-60f, 60f), 0.2f);
        }

        void Marca(string texto, Color color)
        {
            marca.text = texto;
            marca.color = color;
            marca.enabled = true;
        }

        void MoverMarca(float k)
        {
            if (!marca.enabled)
                return;
            float entrada = Suave(Mathf.Clamp01(k / 0.15f));
            float salida = Mathf.Clamp01((k - 0.8f) / 0.2f);
            float altoPose = imagen.rectTransform.sizeDelta.y;
            marca.rectTransform.anchoredPosition = new Vector2(alto * 0.22f, altoPose + 30f + Mathf.Sin(reloj * 5f) * 6f) + cuerpo.anchoredPosition;
            marca.rectTransform.localScale = Vector3.one * Mathf.LerpUnclamped(0.3f, 1f, Rebote(entrada)) * (1f + Mathf.Sin(reloj * 8f) * 0.04f);
            Color c = marca.color;
            c.a = 1f - salida;
            marca.color = c;
        }

        Sprite Estrella
        {
            get
            {
                Sprite estrella = ImagenesFrascos.Figura("gem_yellow_star");
                if (estrella == null)
                    estrella = Resources.Load<Sprite>("FluxLab/gem_yellow_star");
                return estrella != null ? estrella : FormasUI.Destello;
            }
        }

        // Curva suave (empieza y termina despacio).
        static float Suave(float t) => t * t * (3f - 2f * t);

        // Pasa un poco del final y vuelve.
        static float Rebote(float t)
        {
            const float c1 = 1.70158f;
            float u = t - 1f;
            return 1f + (c1 + 1f) * u * u * u + c1 * u * u;
        }

        static Color ColorNota()
        {
            Color[] colores = { new Color(1f, 0.85f, 0.4f, 1f), new Color(0.6f, 0.85f, 1f, 1f), new Color(0.95f, 0.6f, 0.9f, 1f) };
            return colores[Random.Range(0, colores.Length)];
        }

        static Color ColorFiesta()
        {
            Color[] colores =
            {
                new Color(1f, 0.48f, 0.35f, 1f), new Color(1f, 0.82f, 0.35f, 1f), new Color(0.5f, 0.88f, 0.42f, 1f),
                new Color(0.55f, 0.75f, 1f, 1f), new Color(1f, 0.55f, 0.75f, 1f)
            };
            return colores[Random.Range(0, colores.Length)];
        }

        static Vector2 Curva(Vector2 a, Vector2 b, Vector2 c, float k)
        {
            float u = 1f - k;
            return u * u * a + 2f * u * k * b + k * k * c;
        }

        static Image NuevaImagen(Transform padre, string nombre, Sprite sprite, Vector2 posicion, Vector2 tamano)
        {
            var go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(padre, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = posicion;
            rect.sizeDelta = tamano;
            var imagen = go.GetComponent<Image>();
            imagen.sprite = sprite;
            imagen.raycastTarget = false;
            return imagen;
        }

        // Imagen de pose: se apoya en los pies (abajo al centro) del cuerpo.
        static RawImage NuevaImagenRaw(Transform padre, string nombre)
        {
            var go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            go.transform.SetParent(padre, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = Vector2.zero;
            var imagen = go.GetComponent<RawImage>();
            imagen.raycastTarget = false;
            return imagen;
        }
    }
}
