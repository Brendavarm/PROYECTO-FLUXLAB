using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FluxLab.Nivel2
{
    /// <summary>
    /// Le da vida al caldero pintado en el fondo. Encima de su boca hay una "superficie"
    /// aplastada en perspectiva con el líquido, un remolino que gira cuando se mezcla y ondas.
    /// Al echar un frasco: destello de luz, chorro de líquido, ondas, remolino del color
    /// del ingrediente, hervor fuerte y una reacción propia de cada ingrediente.
    /// </summary>
    public class CalderoVivo : MonoBehaviour
    {
        /// <summary>Centro de la boca del caldero pintado en el fondo (coordenadas del canvas).</summary>
        public static Vector2 Boca => FondoNivel2.Boca;

        static float Radio => FondoNivel2.RadioBoca;      // medio ancho de la boca
        const float Achatar = FondoNivel2.AchatarBoca;    // la boca vista en perspectiva

        static readonly Color Base = new Color(0.50f, 0.86f, 0.30f, 1f);
        static readonly Color Lodo = new Color(0.29f, 0.23f, 0.16f, 1f);
        static readonly string[] SpritesBurbuja = { "burbuja_a", "burbuja_b", "burbuja_c" };

        EscenaFrascos escena;
        ConfigFrascos config;
        ParticulasUI efectos;
        ParticulasUI ondas;
        RectTransform cuerpo;
        RectTransform superficie;
        CuerpoCaldero caldero;
        Image aura;
        Image liquido;
        Image brillo;
        Image remolino;
        Image reflejo;
        readonly List<Mota> motas = new List<Mota>();
        float relojBloop = 1.5f;

        // Lucecita que sube en espiral alrededor del caldero mientras se mezcla.
        class Mota
        {
            public RectTransform rect;
            public Image imagen;
            public float angulo;
            public float vuelta;
            public float alto;
            public float vida;
            public float total;
            public Color color;
        }
        readonly List<BubbleDrift> burbujas = new List<BubbleDrift>();
        readonly List<Color> mezcla = new List<Color>();

        Color color = Base;
        Color objetivo = Base;
        Color tinteRemolino = Base;
        float mezclando;     // 1 justo al echar un frasco, baja hasta 0
        float hervor;        // 1 = hierve fuerte
        float golpe;
        float temblor;
        float relojVapor;
        float relojHervor;

        float S => config.tamanoReacciones;

        public void Construir(EscenaFrascos escena, ConfigFrascos config, ParticulasUI efectos)
        {
            this.escena = escena;
            this.config = config;
            this.efectos = efectos;

            cuerpo = GetComponent<RectTransform>();
            cuerpo.anchoredPosition = Boca;
            cuerpo.sizeDelta = new Vector2(Radio * 2f, 160f);

            // Detrás de este objeto: primero el aura de luz en la pared y luego el caldero recortado que se mueve.
            Transform canvas = transform.parent;
            int lugar = transform.GetSiblingIndex();
            aura = escena.Imagen(canvas, "AuraCaldero", FormasUI.Humo, new Vector2(0f, 40f), new Vector2(780f, 600f), false);
            aura.transform.SetSiblingIndex(lugar);
            caldero = CuerpoCaldero.Crear(canvas, escena.FondoTextura);
            if (caldero != null)
                caldero.transform.SetSiblingIndex(lugar + 1);

            var plano = new GameObject("Superficie", typeof(RectTransform));
            plano.transform.SetParent(transform, false);
            superficie = plano.GetComponent<RectTransform>();
            superficie.sizeDelta = new Vector2(Radio * 2f, Radio * 2f);
            superficie.localScale = new Vector3(1f, Achatar, 1f);

            liquido = escena.Imagen(superficie, "Liquido", FormasUI.Elipse, Vector2.zero, new Vector2(Radio * 2f, Radio * 2f), false);
            brillo = escena.Imagen(superficie, "Brillo", FormasUI.Humo, Vector2.zero, new Vector2(Radio * 1.9f, Radio * 1.9f), false);
            remolino = escena.Imagen(superficie, "Remolino", FormasUI.Remolino, Vector2.zero, new Vector2(Radio * 1.8f, Radio * 1.8f), false);
            reflejo = escena.Imagen(superficie, "Reflejo", FormasUI.Elipse, new Vector2(-40f, 50f), new Vector2(150f, 90f), false);
            reflejo.color = new Color(1f, 1f, 1f, 0.2f);

            var capaOndas = new GameObject("Ondas", typeof(RectTransform));
            capaOndas.transform.SetParent(superficie, false);
            ondas = capaOndas.AddComponent<ParticulasUI>();

            for (int i = 0; i < config.cantidadBurbujas; i++)
            {
                float lado = Random.Range(18f, 32f);
                var burbuja = escena.Imagen(transform, "Burbuja", escena.Spr(SpritesBurbuja[i % SpritesBurbuja.Length]),
                    new Vector2(Random.Range(-110f, 110f), Random.Range(-6f, 6f)), new Vector2(lado, lado), true);
                var deriva = burbuja.gameObject.AddComponent<BubbleDrift>();
                deriva.amplitude = Random.Range(8f, 12f);
                deriva.limit = 80f;
                burbujas.Add(deriva);
            }

            Reiniciar();
        }

        // ───────────────────────── Estados ─────────────────────────

        public void Reiniciar()
        {
            mezcla.Clear();
            objetivo = Base;
            hervor = 0f;
            temblor = 0f;
            mezclando = 0f;
        }

        public void Vaciar()
        {
            mezcla.Clear();
            objetivo = Base;
            tinteRemolino = Base;
            mezclando = 1f;
            golpe = 0.8f;
            caldero?.Rebotar(0.6f);
            ondas.Una(FormasUI.Anillo, Vector2.zero, Vector2.one * Radio * 2f, Aclarar(color, 0.4f), Vector2.zero, 0.6f, 0f, 0f, -0.85f);
            efectos.Una(FormasUI.Humo, Boca, Vector2.one * 260f * S, Transparente(Base, 0.45f), Vector2.zero, 0.5f, 0f, 0f, -0.5f);
        }

        /// <summary>
        /// Un frasco cae en el caldero. Devuelve el nombre de la combinación si hubo una.
        /// </summary>
        public string Verter(IngredientKind tipo, Color tinte, IngredientKind? anterior)
        {
            mezcla.Add(tinte);
            objetivo = Color.Lerp(Base, Promedio(), 0.9f);
            color = Color.Lerp(color, tinte, 0.8f);   // primero se ve el color nuevo, luego se mezcla
            tinteRemolino = tinte;
            mezclando = 1f;
            hervor = Mathf.Max(hervor, 1f);
            golpe = 1.2f;
            caldero?.Rebotar(1f);

            Espiral(tinte, Chispas(0.7f));
            Destello(tinte, 1f);
            Chorro(tinte, 1f);
            Ondas(tinte, 3);
            Reaccionar(tipo, tinte);

            string combinacion = anterior.HasValue ? Combinar(anterior.Value, tipo, tinte) : null;
            if (combinacion != null)
                StartCoroutine(Cartel(combinacion, tinte));
            return combinacion;
        }

        /// <summary>Receta completa y correcta: la poción explota en colores.</summary>
        public void Exito()
        {
            golpe = 1.6f;
            hervor = 1.4f;
            mezclando = 1f;
            tinteRemolino = Color.white;
            objetivo = Aclarar(objetivo, 0.3f);
            caldero?.Festejar();
            Destello(Color.white, 1.4f);
            Ondas(Color.white, 4);
            for (int i = 0; i < mezcla.Count; i++)
                Espiral(mezcla[i], Chispas(0.4f));

            for (int i = 0; i < mezcla.Count; i++)
            {
                Emitir(new ParticulasUI.Rafaga
                {
                    forma = FormasUI.Humo, cantidad = 7, centro = Boca + new Vector2(0f, 10f), dispersion = new Vector2(50f, 6f),
                    color = Transparente(Aclarar(mezcla[i], 0.15f), 0.7f), angulo = new Vector2(84f, 96f), rapidez = new Vector2(220f, 380f),
                    vida = new Vector2(1.2f, 1.9f), tamano = new Vector2(60f, 110f), crece = 1.2f
                });
            }

            Emitir(new ParticulasUI.Rafaga
            {
                forma = FormasUI.Destello, cantidad = Chispas(2f), centro = Boca + new Vector2(0f, 20f), dispersion = new Vector2(40f, 8f),
                color = new Color(1f, 0.97f, 0.8f, 1f), angulo = new Vector2(5f, 175f), rapidez = new Vector2(180f, 460f),
                vida = new Vector2(0.8f, 1.4f), tamano = new Vector2(22f, 44f), gravedad = 140f, giro = 220f
            });
            Emitir(new ParticulasUI.Rafaga
            {
                forma = escena.Spr("gem_yellow_star"), cantidad = Chispas(0.8f), centro = Boca + new Vector2(0f, 20f), dispersion = new Vector2(40f, 8f),
                color = Color.white, angulo = new Vector2(30f, 150f), rapidez = new Vector2(250f, 480f),
                vida = new Vector2(1f, 1.5f), tamano = new Vector2(20f, 36f), gravedad = 260f, giro = 360f
            });
        }

        /// <summary>Ingrediente equivocado: el caldero se pone negro, erupciona y tiembla.</summary>
        public void Error()
        {
            temblor = 1f;
            golpe = 1.8f;
            hervor = 1.6f;
            mezclando = 1f;
            tinteRemolino = new Color(0.12f, 0.1f, 0.1f, 1f);
            objetivo = Lodo;
            caldero?.Tambalear();
            caldero?.Rebotar(1.3f);
            escena.Sonar("Nivel2/erupcion");
            Destello(new Color(0.55f, 0.1f, 0.05f, 1f), 1.2f);

            Emitir(new ParticulasUI.Rafaga
            {
                forma = FormasUI.Humo, cantidad = Mathf.RoundToInt(26 * config.intensidadHumo), centro = Boca, dispersion = new Vector2(100f, 8f),
                color = new Color(0.1f, 0.08f, 0.08f, 0.85f), angulo = new Vector2(78f, 102f), rapidez = new Vector2(80f, 200f),
                vida = new Vector2(1.6f, 2.6f), tamano = new Vector2(70f, 130f), crece = 1.7f
            });
            Emitir(new ParticulasUI.Rafaga
            {
                forma = FormasUI.Punto, cantidad = Chispas(2f), centro = Boca, dispersion = new Vector2(80f, 4f),
                color = new Color(0.25f, 0.3f, 0.12f, 1f), angulo = new Vector2(55f, 125f), rapidez = new Vector2(320f, 620f),
                vida = new Vector2(0.7f, 1.2f), tamano = new Vector2(12f, 24f), gravedad = 800f
            });
        }

        // ───────────────────────── Reacciones de cada ingrediente ─────────────────────────

        void Reaccionar(IngredientKind tipo, Color tinte)
        {
            switch (tipo)
            {
                case IngredientKind.Fuego: // columna de llamas y chispas
                    escena.Sonar("Nivel2/fuego");
                    Emitir(new ParticulasUI.Rafaga
                    {
                        forma = FormasUI.Humo, cantidad = 16, centro = Boca + new Vector2(0f, 10f), dispersion = new Vector2(80f, 6f),
                        color = new Color(1f, 0.42f, 0.08f, 0.9f), angulo = new Vector2(84f, 96f), rapidez = new Vector2(160f, 300f),
                        vida = new Vector2(0.6f, 1f), tamano = new Vector2(60f, 110f), crece = -0.5f
                    });
                    Emitir(new ParticulasUI.Rafaga
                    {
                        forma = FormasUI.Humo, cantidad = 8, centro = Boca + new Vector2(0f, 10f), dispersion = new Vector2(50f, 6f),
                        color = new Color(1f, 0.85f, 0.3f, 0.9f), angulo = new Vector2(86f, 94f), rapidez = new Vector2(120f, 220f),
                        vida = new Vector2(0.4f, 0.7f), tamano = new Vector2(40f, 70f), crece = -0.6f
                    });
                    Emitir(new ParticulasUI.Rafaga
                    {
                        forma = FormasUI.Punto, cantidad = Chispas(1.5f), centro = Boca, dispersion = new Vector2(80f, 4f),
                        color = new Color(1f, 0.85f, 0.3f, 1f), angulo = new Vector2(65f, 115f), rapidez = new Vector2(280f, 560f),
                        vida = new Vector2(0.7f, 1.2f), tamano = new Vector2(7f, 14f), gravedad = 300f
                    });
                    break;

                case IngredientKind.Gota: // gran salpicadura y ondas
                    escena.Sonar("Nivel2/gota");
                    Chorro(tinte, 1.8f);
                    Ondas(tinte, 3);
                    Emitir(new ParticulasUI.Rafaga
                    {
                        forma = FormasUI.Punto, cantidad = Chispas(1.2f), centro = Boca + new Vector2(0f, 6f), dispersion = new Vector2(40f, 4f),
                        color = Aclarar(tinte, 0.2f), angulo = new Vector2(15f, 165f), rapidez = new Vector2(200f, 380f),
                        vida = new Vector2(0.6f, 0.9f), tamano = new Vector2(10f, 18f), gravedad = 700f
                    });
                    break;

                case IngredientKind.Polvo: // explosión de estrellas doradas
                    escena.Sonar("Nivel2/estrella");
                    Emitir(new ParticulasUI.Rafaga
                    {
                        forma = escena.Spr("gem_yellow_star"), cantidad = Chispas(1.5f), centro = Boca + new Vector2(0f, 10f), dispersion = new Vector2(50f, 6f),
                        color = Color.white, angulo = new Vector2(10f, 170f), rapidez = new Vector2(220f, 460f),
                        vida = new Vector2(0.9f, 1.5f), tamano = new Vector2(20f, 40f), gravedad = 180f, giro = 400f
                    });
                    Emitir(new ParticulasUI.Rafaga
                    {
                        forma = FormasUI.Destello, cantidad = Chispas(0.8f), centro = Boca + new Vector2(0f, 60f), dispersion = new Vector2(140f, 60f),
                        color = new Color(1f, 0.92f, 0.5f, 1f), angulo = new Vector2(80f, 100f), rapidez = new Vector2(10f, 60f),
                        vida = new Vector2(0.5f, 1f), tamano = new Vector2(24f, 46f), giro = 120f, crece = 0.6f
                    });
                    break;

                case IngredientKind.Hierba: // nube verde y lluvia de hojas
                    escena.Sonar("Nivel2/pop");
                    Emitir(new ParticulasUI.Rafaga
                    {
                        forma = FormasUI.Humo, cantidad = Mathf.RoundToInt(14 * config.intensidadHumo), centro = Boca + new Vector2(0f, 10f), dispersion = new Vector2(100f, 8f),
                        color = new Color(0.4f, 0.85f, 0.4f, 0.6f), angulo = new Vector2(78f, 102f), rapidez = new Vector2(50f, 120f),
                        vida = new Vector2(1.6f, 2.4f), tamano = new Vector2(80f, 140f), crece = 1.8f
                    });
                    Emitir(new ParticulasUI.Rafaga
                    {
                        forma = FormasUI.Hoja, cantidad = Chispas(1f), centro = Boca, dispersion = new Vector2(70f, 4f),
                        color = new Color(0.28f, 0.68f, 0.28f, 1f), angulo = new Vector2(40f, 140f), rapidez = new Vector2(180f, 340f),
                        vida = new Vector2(1.2f, 1.8f), tamano = new Vector2(20f, 32f), gravedad = 240f, giro = 360f
                    });
                    break;

                case IngredientKind.Cristal: // destellos morados grandes
                    escena.Sonar("Nivel2/cristal");
                    Emitir(new ParticulasUI.Rafaga
                    {
                        forma = FormasUI.Destello, cantidad = Chispas(1.5f), centro = Boca + new Vector2(0f, 50f), dispersion = new Vector2(150f, 70f),
                        color = new Color(0.85f, 0.62f, 1f, 1f), angulo = new Vector2(70f, 110f), rapidez = new Vector2(20f, 90f),
                        vida = new Vector2(0.6f, 1.1f), tamano = new Vector2(30f, 60f), giro = 140f, crece = 0.7f
                    });
                    Emitir(new ParticulasUI.Rafaga
                    {
                        forma = FormasUI.Punto, cantidad = Chispas(0.8f), centro = Boca + new Vector2(0f, 10f), dispersion = new Vector2(40f, 4f),
                        color = new Color(0.95f, 0.85f, 1f, 1f), angulo = new Vector2(20f, 160f), rapidez = new Vector2(250f, 450f),
                        vida = new Vector2(0.5f, 0.9f), tamano = new Vector2(6f, 12f), gravedad = 400f
                    });
                    break;

                default: // Lágrima: ondas de luz naranja que se abren
                    escena.Sonar("Nivel2/corcho");
                    Destello(tinte, 1.5f);
                    for (int i = 0; i < 4; i++)
                        efectos.Una(FormasUI.Anillo, Boca + new Vector2(0f, i * 18f), new Vector2(120f + i * 40f, 30f + i * 10f) * S,
                            Transparente(Aclarar(tinte, 0.2f), 0.9f - i * 0.15f), new Vector2(0f, 40f), 0.8f + i * 0.2f, 0f, 0f, 2.4f);
                    Emitir(new ParticulasUI.Rafaga
                    {
                        forma = FormasUI.Destello, cantidad = Chispas(0.6f), centro = Boca + new Vector2(0f, 20f), dispersion = new Vector2(60f, 10f),
                        color = new Color(1f, 0.75f, 0.4f, 1f), angulo = new Vector2(20f, 160f), rapidez = new Vector2(120f, 280f),
                        vida = new Vector2(0.7f, 1.1f), tamano = new Vector2(20f, 36f), giro = 120f
                    });
                    break;
            }
        }

        string Combinar(IngredientKind anterior, IngredientKind actual, Color tinte)
        {
            if (anterior == actual)
            {
                hervor = 2f;
                golpe = 2f;
                caldero?.Rebotar(1.5f);
                Emitir(new ParticulasUI.Rafaga
                {
                    forma = escena.Spr("burbuja_a"), cantidad = 20, centro = Boca, dispersion = new Vector2(120f, 8f),
                    color = Color.white, angulo = new Vector2(80f, 100f), rapidez = new Vector2(80f, 200f),
                    vida = new Vector2(1f, 1.6f), tamano = new Vector2(24f, 46f), crece = 0.5f
                });
                return "¡Hervor doble!";
            }

            if (Par(anterior, actual, IngredientKind.Fuego, IngredientKind.Gota))
            {
                escena.Sonar("Nivel2/gota");
                Emitir(new ParticulasUI.Rafaga
                {
                    forma = FormasUI.Humo, cantidad = Mathf.RoundToInt(24 * config.intensidadHumo), centro = Boca, dispersion = new Vector2(100f, 8f),
                    color = new Color(1f, 1f, 1f, 0.65f), angulo = new Vector2(75f, 105f), rapidez = new Vector2(120f, 260f),
                    vida = new Vector2(1.6f, 2.4f), tamano = new Vector2(80f, 140f), crece = 1.6f
                });
                return "¡Vapor!";
            }

            if (Par(anterior, actual, IngredientKind.Polvo, IngredientKind.Cristal))
            {
                Emitir(new ParticulasUI.Rafaga
                {
                    forma = FormasUI.Destello, cantidad = Chispas(2.5f), centro = Boca + new Vector2(0f, 300f), dispersion = new Vector2(260f, 50f),
                    color = new Color(1f, 0.9f, 0.6f, 1f), angulo = new Vector2(260f, 280f), rapidez = new Vector2(100f, 200f),
                    vida = new Vector2(1f, 1.6f), tamano = new Vector2(20f, 40f), giro = 160f
                });
                return "¡Lluvia de destellos!";
            }

            return null;
        }

        // ───────────────────────── Efectos comunes ─────────────────────────

        // Un golpe de luz del color del ingrediente detrás de todo.
        void Destello(Color tinte, float fuerza)
        {
            efectos.Una(FormasUI.Humo, Boca + new Vector2(0f, 30f), Vector2.one * 420f * S * fuerza,
                Transparente(Aclarar(tinte, 0.25f), 0.6f), Vector2.zero, 0.6f, 0f, 0f, 0.6f);
        }

        // El líquido salta hacia arriba cuando cae algo.
        void Chorro(Color tinte, float fuerza)
        {
            Emitir(new ParticulasUI.Rafaga
            {
                forma = FormasUI.Punto, cantidad = Mathf.RoundToInt(Chispas(1f) * fuerza), centro = Boca + new Vector2(0f, 6f), dispersion = new Vector2(24f, 4f),
                color = tinte, angulo = new Vector2(78f, 102f), rapidez = new Vector2(320f * fuerza, 620f * fuerza),
                vida = new Vector2(0.7f, 1.1f), tamano = new Vector2(10f, 22f), gravedad = 1000f
            });
        }

        // Anillos que se abren sobre el líquido (en perspectiva).
        void Ondas(Color tinte, int cantidad)
        {
            for (int i = 0; i < cantidad; i++)
                ondas.Una(FormasUI.Anillo, Vector2.zero, Vector2.one * (50f + i * 25f), Transparente(Aclarar(tinte, 0.3f), 0.95f),
                    Vector2.zero, 0.7f + i * 0.25f, 0f, 0f, 3.6f - i * 0.6f);
        }

        void Emitir(ParticulasUI.Rafaga r)
        {
            r.tamano *= S;
            r.rapidez *= Mathf.Sqrt(S);
            r.dispersion *= Mathf.Sqrt(S);
            efectos.Emitir(r);
        }

        // ───────────────────────── Cada cuadro ─────────────────────────

        void Update()
        {
            if (liquido == null)
                return;

            float dt = Time.deltaTime;
            float t = Time.time;
            mezclando = Mathf.MoveTowards(mezclando, 0f, dt / Mathf.Max(0.1f, config.segundosMezcla));
            hervor = Mathf.MoveTowards(hervor, 0f, dt * 0.55f);
            golpe = Mathf.MoveTowards(golpe, 0f, dt * 1.8f);
            temblor = Mathf.MoveTowards(temblor, 0f, dt * 1.4f);

            // El color se mezcla despacio hacia el de la poción.
            color = Color.Lerp(color, objetivo, dt * 1.4f);
            Color pintura = color;
            pintura.a = 0.95f;
            liquido.color = pintura;

            // Remolino: fuerte y del color del ingrediente al echarlo, suave cuando reposa.
            Color giro = Color.Lerp(Aclarar(color, 0.35f), tinteRemolino, mezclando);
            giro.a = 0.18f + 0.8f * mezclando;
            remolino.color = giro;
            remolino.rectTransform.localRotation = Quaternion.Euler(0f, 0f, remolino.rectTransform.localEulerAngles.z - (35f + 620f * mezclando) * dt);

            // La superficie respira y salta con cada golpe, y sigue al caldero cuando se estira o gira.
            float ola = Mathf.Sin(t * 20f) * golpe;
            float anchoCaldero = caldero != null ? caldero.Escala.x : 1f;
            superficie.localScale = new Vector3(
                anchoCaldero * (1f + Mathf.Sin(t * 2.2f) * 0.012f + ola * 0.04f),
                Achatar * (1f + Mathf.Sin(t * 1.7f) * 0.07f + ola * 0.35f),
                1f);
            if (caldero != null)
            {
                caldero.Hervor = hervor * config.fuerzaHervor;
                cuerpo.anchoredPosition = caldero.Mover(Boca);
                cuerpo.localRotation = Quaternion.Euler(0f, 0f, caldero.Giro);
            }
            else
            {
                cuerpo.anchoredPosition = Boca + (temblor > 0f ? Random.insideUnitCircle * 6f * temblor : Vector2.zero);
            }

            // Luz de la poción sobre la pared y brillo sobre el líquido.
            Color luz = Aclarar(color, 0.2f);
            luz.a = Mathf.Clamp01(0.2f + Mathf.Sin(t * 2f) * 0.05f + golpe * 0.18f + mezclando * 0.15f);
            aura.color = luz;
            aura.rectTransform.localScale = Vector3.one * (1f + golpe * 0.08f + Mathf.Sin(t * 2f) * 0.03f);
            Color reflejoLuz = Aclarar(color, 0.55f);
            reflejoLuz.a = 0.22f + 0.35f * mezclando + Mathf.Sin(t * 3.1f) * 0.06f;
            brillo.color = reflejoLuz;

            Hervir(dt);
            Burbujones(dt);
            MoverMotas(dt);
            Vapor(dt);

            Color tinteBurbuja = Color.Lerp(Color.white, Aclarar(color, 0.35f), 0.6f);
            tinteBurbuja.a = 0.85f;
            for (int i = 0; i < burbujas.Count; i++)
            {
                if (burbujas[i] == null)
                    continue;
                burbujas[i].speed = config.velocidadBurbujas * (0.75f + (i % 3) * 0.25f) * (1f + hervor * 1.5f);
                burbujas[i].GetComponent<Image>().color = tinteBurbuja;
            }
        }

        // Burbujas que nacen sobre el líquido, crecen y revientan. Más hervor = más y más grandes.
        void Hervir(float dt)
        {
            float fuerza = hervor * config.fuerzaHervor;
            float porSegundo = 3f + fuerza * 30f;
            relojHervor -= dt * porSegundo;
            while (relojHervor <= 0f)
            {
                relojHervor += 1f;
                Vector2 punto = PuntoEnLiquido(0.85f);
                float lado = Random.Range(8f, 18f) * (1f + fuerza * 0.8f);
                Color c = Aclarar(color, 0.35f);
                c.a = 0.95f;
                efectos.Una(escena.Spr(SpritesBurbuja[Random.Range(0, SpritesBurbuja.Length)]), punto, Vector2.one * lado, c,
                    new Vector2(0f, Random.Range(10f, 30f)), Random.Range(0.3f, 0.55f), 0f, 0f, 0.9f);

                if (fuerza > 0.4f && Random.value < 0.35f)
                {
                    float gota = Random.Range(6f, 12f);
                    efectos.Una(FormasUI.Punto, punto, Vector2.one * gota, color,
                        new Vector2(Random.Range(-60f, 60f), Random.Range(140f, 280f) * Mathf.Min(1.6f, fuerza)), 0.7f, 650f, 0f, -0.2f);
                }
            }
        }

        // De vez en cuando una burbuja grande se infla en el líquido y revienta. Con hervor, más seguido.
        void Burbujones(float dt)
        {
            relojBloop -= dt * (1f + hervor * 2.5f * config.fuerzaHervor);
            if (relojBloop > 0f)
                return;
            relojBloop = Random.Range(1.2f, 2.6f);
            StartCoroutine(Burbujon(PuntoEnLiquido(0.6f), Random.Range(40f, 70f) * Mathf.Sqrt(S)));
        }

        IEnumerator Burbujon(Vector2 punto, float lado)
        {
            var burbuja = escena.Imagen(efectos.transform, "Burbujon", escena.Spr(SpritesBurbuja[Random.Range(0, SpritesBurbuja.Length)]), punto, Vector2.one * lado, true);
            Color c = Aclarar(color, 0.4f);
            float reloj = 0f;
            const float inflar = 0.55f;
            while (reloj < inflar && burbuja != null)
            {
                reloj += Time.deltaTime;
                float k = reloj / inflar;
                c.a = 0.95f;
                burbuja.color = c;
                burbuja.rectTransform.anchoredPosition = punto + new Vector2(0f, k * lado * 0.3f);
                burbuja.rectTransform.localScale = new Vector3(0.2f + 0.85f * k, (0.2f + 0.85f * k) * (1f + Mathf.Sin(k * 18f) * 0.05f), 1f);
                yield return null;
            }

            if (burbuja == null)
                yield break;
            Vector2 donde = burbuja.rectTransform.anchoredPosition;
            Destroy(burbuja.gameObject);

            // ¡Plop! anillo y gotitas.
            efectos.Una(FormasUI.Anillo, donde, new Vector2(lado, lado * 0.45f), Transparente(Aclarar(color, 0.5f), 0.9f), Vector2.zero, 0.35f, 0f, 0f, 1.4f);
            efectos.Emitir(new ParticulasUI.Rafaga
            {
                forma = FormasUI.Punto, cantidad = 8, centro = donde, dispersion = new Vector2(lado * 0.2f, 2f),
                color = color, angulo = new Vector2(30f, 150f), rapidez = new Vector2(120f, 240f),
                vida = new Vector2(0.4f, 0.7f), tamano = new Vector2(6f, 11f), gravedad = 700f
            });
            escena.Sonar("Nivel2/gota", 0.25f);
        }

        // Lucecitas del color del ingrediente que suben girando alrededor del caldero: se ve que mezcla.
        void Espiral(Color tinte, int cantidad)
        {
            for (int i = 0; i < cantidad; i++)
            {
                float lado = Random.Range(10f, 20f) * Mathf.Sqrt(S);
                var imagen = escena.Imagen(efectos.transform, "Mota", i % 3 == 0 ? FormasUI.Destello : FormasUI.Punto, Boca, Vector2.one * lado, false);
                float total = Random.Range(1.2f, 1.8f);
                motas.Add(new Mota
                {
                    rect = imagen.rectTransform,
                    imagen = imagen,
                    angulo = i * Mathf.PI * 2f / Mathf.Max(1, cantidad),
                    vuelta = Random.Range(4f, 6f),
                    alto = Random.Range(-60f, -20f),
                    vida = total,
                    total = total,
                    color = Aclarar(tinte, 0.25f)
                });
            }
        }

        void MoverMotas(float dt)
        {
            for (int i = motas.Count - 1; i >= 0; i--)
            {
                Mota m = motas[i];
                m.vida -= dt;
                if (m.vida <= 0f || m.rect == null)
                {
                    if (m.rect != null)
                        Destroy(m.rect.gameObject);
                    motas.RemoveAt(i);
                    continue;
                }

                float k = 1f - m.vida / m.total;
                m.angulo += m.vuelta * dt;
                m.alto += (110f + 80f * k) * dt;
                float radio = Mathf.Lerp(Radio * 1.35f, Radio * 0.5f, k);
                m.rect.anchoredPosition = Boca + new Vector2(Mathf.Cos(m.angulo) * radio, Mathf.Sin(m.angulo) * radio * 0.28f + m.alto);
                m.rect.localScale = Vector3.one * (Mathf.Sin(m.angulo) < 0f ? 1.15f : 0.8f);   // más grande por delante
                Color c = m.color;
                c.a = k < 0.1f ? k / 0.1f : 1f - Mathf.Pow((k - 0.1f) / 0.9f, 2f);
                m.imagen.color = c;
            }
        }

        void Vapor(float dt)
        {
            if (config.intensidadHumo <= 0.01f)
                return;
            relojVapor -= dt * (1f + hervor);
            if (relojVapor > 0f)
                return;
            relojVapor = Random.Range(0.3f, 0.5f) / config.intensidadHumo;
            efectos.Una(FormasUI.Humo, Boca + new Vector2(Random.Range(-100f, 100f), 12f), Vector2.one * Random.Range(60f, 100f),
                Transparente(Aclarar(color, 0.5f), 0.2f * Mathf.Min(1.5f, config.intensidadHumo)),
                new Vector2(Random.Range(-8f, 8f), Random.Range(35f, 60f)), 2.2f, 0f, Random.Range(-20f, 20f), 1.3f);
        }

        // ───────────────────────── Ayudas ─────────────────────────

        static Vector2 PuntoEnLiquido(float alcance)
        {
            float angulo = Random.Range(0f, Mathf.PI * 2f);
            float r = Mathf.Sqrt(Random.value) * alcance;
            return Boca + new Vector2(Mathf.Cos(angulo) * Radio * r, Mathf.Sin(angulo) * Radio * Achatar * r);
        }

        IEnumerator Cartel(string texto, Color tinte)
        {
            var cartel = escena.Texto(efectos.transform, texto, 46, Color.white, FontStyle.Bold);
            cartel.rectTransform.sizeDelta = new Vector2(560f, 64f);
            var contorno = cartel.gameObject.AddComponent<Outline>();
            contorno.effectColor = Color.Lerp(tinte, Color.black, 0.55f);
            contorno.effectDistance = new Vector2(3f, -3f);

            float reloj = 0f;
            while (reloj < 1.6f && cartel != null)
            {
                reloj += Time.deltaTime;
                float k = reloj / 1.6f;
                cartel.rectTransform.anchoredPosition = Boca + new Vector2(0f, 80f + k * 70f);
                cartel.rectTransform.localScale = Vector3.one * (k < 0.12f ? Mathf.Lerp(0.4f, 1.2f, k / 0.12f) : Mathf.Lerp(1.2f, 1f, Mathf.Clamp01((k - 0.12f) / 0.1f)));
                cartel.color = new Color(1f, 1f, 1f, k < 0.75f ? 1f : 1f - (k - 0.75f) / 0.25f);
                yield return null;
            }

            if (cartel != null)
                Destroy(cartel.gameObject);
        }

        int Chispas(float factor) => Mathf.Max(0, Mathf.RoundToInt(config.cantidadChispas * factor));

        static bool Par(IngredientKind a, IngredientKind b, IngredientKind x, IngredientKind y) => (a == x && b == y) || (a == y && b == x);

        Color Promedio()
        {
            if (mezcla.Count == 0)
                return Base;
            Color suma = Color.clear;
            foreach (Color c in mezcla)
                suma += c;
            return suma / mezcla.Count;
        }

        static Color Aclarar(Color c, float cuanto) => Color.Lerp(c, Color.white, cuanto);

        static Color Transparente(Color c, float alfa) => new Color(c.r, c.g, c.b, alfa);
    }
}
