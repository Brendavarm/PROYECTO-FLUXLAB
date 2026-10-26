using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FluxLab.Nivel2
{
    public enum AnimoMagnus
    {
        Quieto,
        Observa,
        Remueve,
        Festeja,
        SeEquivoca
    }

    /// <summary>
    /// Todo lo que se ve en el Pasillo de Frascos. Usa el mismo laboratorio de la
    /// mesa del nivel 1 y le agrega los frascos de vida y el aviso de ronda espejo.
    /// MesaDeFrascos decide, esta clase solo dibuja y anima.
    /// </summary>
    public class EscenaFrascos
    {
        public const int MaxReceta = 5;

        struct Ing
        {
            public string Sprite;
            public string Nombre;
            public Color Glow;
        }

        public struct Resultado
        {
            public int puntos;
            public int aciertos;
            public int rondas;
            public int rachaMaxima;
            public float tiempoMedio;
            public int vidasRestantes;
            public int vidasTotales;
            public bool seRompieron;
            public string aviso;
            public bool haySiguiente;
            public bool pasa;   // ganó las estrellas necesarias
            public int mejorPuntaje;
            public bool nuevoRecord;
        }

        readonly MonoBehaviour host;
        readonly Dictionary<IngredientKind, Ing> catalogo = new Dictionary<IngredientKind, Ing>();
        readonly Dictionary<IngredientKind, RectTransform> cartas = new Dictionary<IngredientKind, RectTransform>();

        Font font;
        bool letraTallada;   // la letra ya es gruesa: no se le agrega negrita
        Sprite solido;
        Sprite disco;
        AudioSource sfx;

        int largo;
        Text prompt;
        Text timerTexto;
        Image timerFill;
        Text rondaLabel;
        Text puntosLabel;
        GameObject avisoEspejo;
        RectTransform capaEfectos;
        ParticulasUI efectos;
        Image[] gemasReceta;
        Image[] brillosReceta;
        Text[] nombresReceta;
        RectTransform[] huecosReceta;
        GameObject panelPausa;
        GameObject panelSalir;


        ResultadoFrascos resultados;

        public VidasFrascos Vidas { get; private set; }
        public CalderoVivo Caldero { get; private set; }
        public MagnusAnimado Magnus { get; private set; }
        public FrascosDelFondo FrascosFondo { get; private set; }
        public BienvenidaFrascos Bienvenida { get; private set; }
        public Sprite Disco => disco;
        public Font Fuente => font;

        readonly Dictionary<string, Sprite> artesMenu = new Dictionary<string, Sprite>();
        readonly List<MaderaColgante> maderas = new List<MaderaColgante>();
        readonly List<RectTransform> gemasBandeja = new List<RectTransform>();
        readonly List<Image> halosBandeja = new List<Image>();

        /// <summary>Hace caer los letreros y botones de madera del HUD.</summary>
        public void SoltarMaderas()
        {
            foreach (MaderaColgante madera in maderas)
                madera.Soltar();
        }

        /// <summary>
        /// Arte de madera o pergamino del menú (StreamingAssets/Menu) preparado para estirarse
        /// sin deformar las esquinas (9-slice). borde = izquierda, abajo, derecha, arriba en píxeles.
        /// </summary>
        public Sprite ArteMenu(string archivo, Vector4 borde)
        {
            if (artesMenu.TryGetValue(archivo, out Sprite listo))
                return listo;
            Texture2D textura = Archivo("Menu/" + archivo);
            Sprite sprite = textura == null ? Spr("panel_beige")
                : Sprite.Create(textura, new Rect(0f, 0f, textura.width, textura.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, borde);
            artesMenu[archivo] = sprite;
            return sprite;
        }

        /// <summary>
        /// Pieza tallada del Jardín de Luna (Resources/FluxLab/Nivel3/UI/&lt;nombre&gt;.png), la misma de la
        /// Mesa del Aprendiz. Solo se usa la imagen, no se cambia nada del nivel 3. Null si falta.
        /// </summary>
        Sprite Tallado(string nombre)
        {
            string clave = "Nivel3/" + nombre;
            if (!artesMenu.TryGetValue(clave, out Sprite listo))
            {
                listo = Nivel3UI.SpriteUI(nombre);
                artesMenu[clave] = listo;
            }
            return listo;
        }

        // Maderas y pergamino tallados; si faltan las imágenes, las del menú de antes.
        public Sprite Madera => Tallado("boton_ancho") ?? ArteMenu("boton.png", new Vector4(100f, 80f, 100f, 80f));
        public Sprite Pergamino => Tallado("pergamino") ?? ArteMenu("pergamino.png", new Vector4(115f, 70f, 115f, 70f));
        /// <summary>Tabla grande con postes a los lados (título del nivel, letreros). Se usa sin estirar.</summary>
        public Sprite Placa => Tallado("placa_titulo") ?? Madera;
        /// <summary>Tabla mediana (Ronda y Puntos). Se usa sin estirar.</summary>
        public Sprite PlacaRonda => Tallado("placa_ronda") ?? Madera;
        /// <summary>Botón redondo de madera (música y sonido).</summary>
        public Sprite Redondo => Tallado("boton_cuadrado") ?? Madera;
        /// <summary>Las 6 tablas de colores de la bandeja, en una sola imagen.</summary>
        Sprite BandejaTallada => Tallado("bandeja");

        /// <summary>El laboratorio pintado; CalderoVivo recorta de aquí el caldero para animarlo.</summary>
        public Texture2D FondoTextura { get; private set; }

        public EscenaFrascos(MonoBehaviour host)
        {
            this.host = host;
        }

        // ───────────────────────── Construcción ─────────────────────────

        /// <summary>Qué hace cada botón de la pantalla. Lo llena MesaDeFrascos.</summary>
        public struct Acciones
        {
            public System.Action<IngredientKind> elegir;
            public System.Action vaciar;
            public System.Action pausar;
            public System.Action continuar;
            public System.Action pedirSalir;
            public System.Action confirmarSalir;
            public System.Action cancelarSalir;
            public System.Action irAlMapa;
            public System.Action irAlMenu;
            public System.Action reintentar;
            public System.Action siguiente;
        }

        public void Construir(ConfigFrascos config, Acciones acciones)
        {
            LlenarCatalogo();
            // La misma letra redondita de la Mesa del Aprendiz y el Jardín de Luna (si falta, la de Unity).
            font = Nivel3UI.Fuente;
            letraTallada = font != null && font.name.Contains("Lilita");
            solido = CrearSolido();
            disco = CrearDisco(96);
            largo = Mathf.Clamp(config.largoReceta, 1, MaxReceta);

            AsegurarEventSystem();
            Transform raiz = CrearCanvas().transform;
            ConstruirCuarto(raiz, config);
            ConstruirMagnus(raiz);
            ConstruirCaldero(raiz);
            ConstruirReceta(raiz);
            ConstruirBandeja(raiz, acciones.elegir, acciones.vaciar);
            ConstruirHud(raiz, acciones);
            ConstruirVidas(raiz, config.vidas);
            ConstruirAvisoEspejo(raiz);
            ConstruirCapaEfectos(raiz);
            Caldero.Construir(this, config, efectos);
            ConstruirResultado(raiz, acciones.irAlMenu, acciones.reintentar, acciones.siguiente, acciones.irAlMapa);
            ConstruirBienvenida(raiz, config);
            ConstruirPausa(raiz, acciones);
            ConstruirConfirmarSalir(raiz, acciones);
            ConstruirAudio();
        }

        void ConstruirCuarto(Transform padre, ConfigFrascos config)
        {
            // Fondo propio del nivel 2 (FondoN2). Todas sus medidas están en FondoNivel2.cs.
            FondoTextura = FondoNivel2.Cargar();
            if (FondoTextura == null)
                FondoTextura = Archivo("Menu/fondo.png");
            var fondo = Raw(padre, "Laboratorio", FondoTextura, Color.white);
            Estirar(fondo.rectTransform);

            // Frascos pintados que se mecen y brillan (solo con el fondo del nivel 2, que es donde están medidos).
            // Van antes de la viñeta para tener la misma luz que el resto del dibujo.
            if (FondoTextura != null && FondoTextura.name.Contains("FondoN2"))
            {
                var frascos = new GameObject("FrascosDelFondo", typeof(RectTransform));
                frascos.transform.SetParent(padre, false);
                var frascosVivos = frascos.AddComponent<FrascosDelFondo>();
                frascosVivos.Construir(this, config, FondoTextura);
                FrascosFondo = frascosVivos;
                // Si un frasco se cae y se rompe, Magnus se sorprende (solo si está tranquilo, para no
                // interrumpirlo mientras mira la receta).
                frascosVivos.AlRomperse += () =>
                {
                    if (Magnus != null && Magnus.Actual == AnimacionMagnus.Quieto)
                        Magnus.Sorprenderse();
                };
            }

            // Viñeta: oscurece un poco las orillas para que la atención vaya al centro (el fondo tiene muchos detalles).
            var vineta = Imagen(padre, "Vineta", FormasUI.Vineta, Vector2.zero, Vector2.zero, false);
            Estirar(vineta.rectTransform);
            vineta.color = new Color(0.05f, 0.02f, 0.08f, 0.75f);

            // Fondo vivo: burbujas, faroles que titilan, luna, polvo mágico y destellos en los frascos.
            var vivo = new GameObject("FondoVivo", typeof(RectTransform));
            vivo.transform.SetParent(padre, false);
            vivo.AddComponent<FondoVivo>().Construir(this, config);
        }

        void ConstruirMagnus(Transform padre)
        {
            // Magnus con su personalización (pelo, túnica, gafas), parado sobre la mesa del fondo.
            // En el juego solo usa Riendo y Sorprendido; el resto de la biblioteca está en Magnus/.
            var aspecto = PersonalizacionMagnus.Guardada;
            AspectoPoses.Precargar(aspecto, PoseMagnus.Normal, PoseMagnus.Riendo, PoseMagnus.Sorprendido,
                PoseMagnus.Bailando, PoseMagnus.Saltando, PoseMagnus.Triste);
            Magnus = MagnusAnimado.Crear(padre, FondoNivel2.PiesMagnus, FondoNivel2.AltoMagnus, aspecto, AjustesJuego.Accesorio == 1);
            Magnus.Quieto();
        }

        // El líquido vivo va encima del caldero pintado; sus efectos van en la capa de arriba.
        // A diferencia del nivel 1, aquí no se muestran las figuras sobre el caldero.
        void ConstruirCaldero(Transform padre)
        {
            var vivo = new GameObject("CalderoVivo", typeof(RectTransform));
            vivo.transform.SetParent(padre, false);
            var rect = vivo.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            Caldero = vivo.AddComponent<CalderoVivo>();
        }

        void ConstruirReceta(Transform padre)
        {
            // Resplandor detrás del pergamino: se enciende cuando la receta se agranda.
            resplandorReceta = Imagen(padre, "ResplandorReceta", FormasUI.Humo, RecetaNormal, new Vector2(1000f, 520f), false);
            resplandorReceta.color = new Color(1f, 0.85f, 0.45f, 0f);

            var tablero = Imagen(padre, "Receta", Pergamino, RecetaNormal, new Vector2(700f, 290f), false);
            receta = tablero.rectTransform;

            var titulo = Texto(tablero.transform, "La receta", 32, Hex("#5a3b24"), FontStyle.Bold);
            titulo.rectTransform.anchoredPosition = new Vector2(0f, 92f);
            titulo.rectTransform.sizeDelta = new Vector2(520f, 42f);

            prompt = Texto(tablero.transform, "", 22, Hex("#5a3b24"), FontStyle.Normal);
            prompt.rectTransform.anchoredPosition = new Vector2(0f, 54f);
            prompt.rectTransform.sizeDelta = new Vector2(600f, 34f);

            gemasReceta = new Image[MaxReceta];
            brillosReceta = new Image[MaxReceta];
            nombresReceta = new Text[MaxReceta];
            huecosReceta = new RectTransform[MaxReceta];
            for (int i = 0; i < MaxReceta; i++)
            {
                huecosReceta[i] = Contenedor(tablero.transform, "HuecoReceta" + i);
                gemasReceta[i] = Hueco(huecosReceta[i], 76f, out brillosReceta[i]);
                var numero = Texto(huecosReceta[i], (i + 1).ToString(), 18, Hex("#8a6848"), FontStyle.Bold);
                numero.rectTransform.anchoredPosition = new Vector2(-34f, 28f);
                numero.rectTransform.sizeDelta = new Vector2(28f, 24f);
                nombresReceta[i] = Texto(huecosReceta[i], "?", 18, Hex("#5a3b24"), FontStyle.Bold);
                nombresReceta[i].rectTransform.anchoredPosition = new Vector2(0f, -52f);
                nombresReceta[i].rectTransform.sizeDelta = new Vector2(120f, 26f);
            }

            AcomodarHuecos();

            timerTexto = Texto(tablero.transform, "", 22, Hex("#4a2c17"), FontStyle.Bold);
            timerTexto.rectTransform.anchoredPosition = new Vector2(0f, -84f);
            timerTexto.rectTransform.sizeDelta = new Vector2(460f, 30f);

            // Barra más notoria: marco de madera, fondo oscuro, relleno de color con una punta que brilla
            // y divisiones (una por ingrediente) cuando es tu turno.
            var marcoBarra = Imagen(tablero.transform, "MarcoBarra", Madera, new Vector2(0f, -116f), new Vector2(520f, 42f), false);
            marcoBarra.type = Image.Type.Sliced;
            marcoBarra.pixelsPerUnitMultiplier = 4f;
            barra = marcoBarra.rectTransform;
            var pista = Imagen(marcoBarra.transform, "Tiempo", solido, Vector2.zero, new Vector2(AnchoBarra, 20f), false);
            pista.color = Hex("#3c2a22");
            timerFill = Imagen(pista.transform, "Relleno", solido, Vector2.zero, new Vector2(AnchoBarra, 20f), false);
            timerFill.color = Hex("#7dce63");
            timerFill.type = Image.Type.Filled;
            timerFill.fillMethod = Image.FillMethod.Horizontal;
            timerFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            Estirar(timerFill.rectTransform);
            var reflejoBarra = Imagen(pista.transform, "Reflejo", solido, new Vector2(0f, 5f), new Vector2(AnchoBarra, 4f), false);
            reflejoBarra.color = new Color(1f, 1f, 1f, 0.25f);

            separadores = new Image[MaxReceta - 1];
            for (int i = 0; i < separadores.Length; i++)
            {
                separadores[i] = Imagen(pista.transform, "Division", solido, Vector2.zero, new Vector2(4f, 20f), false);
                separadores[i].color = Hex("#3c2a22");
                separadores[i].enabled = false;
            }

            puntaBarra = Imagen(pista.transform, "Punta", disco, Vector2.zero, new Vector2(46f, 46f), false);
            puntaBarra.color = new Color(1f, 1f, 0.8f, 0.9f);
        }

        // ───────────────────────── Pergamino grande / normal ─────────────────────────

        static readonly Vector2 RecetaNormal = new Vector2(30f, 368f);
        static readonly Vector2 RecetaGrande = new Vector2(30f, 268f);
        const float EscalaGrande = 1.5f;
        const float AnchoBarra = 476f;

        RectTransform receta;
        Image resplandorReceta;
        RectTransform barra;
        Image puntaBarra;
        Image[] separadores;
        Coroutine moverReceta;

        /// <summary>
        /// grande = true: el pergamino crece y brilla mientras se dicta la receta.
        /// grande = false: vuelve a su lugar para que se vea el caldero.
        /// </summary>
        public void EnfocarReceta(bool grande)
        {
            if (moverReceta != null)
                host.StopCoroutine(moverReceta);
            moverReceta = host.StartCoroutine(MoverReceta(grande));
        }

        IEnumerator MoverReceta(bool grande)
        {
            if (grande)
                SubirReceta(true);   // al frente de las partículas del caldero mientras está grande

            Vector2 desde = receta.anchoredPosition;
            Vector2 hasta = grande ? RecetaGrande : RecetaNormal;
            float escalaDesde = receta.localScale.x;
            float escalaHasta = grande ? EscalaGrande : 1f;
            float brilloDesde = resplandorReceta.color.a;
            float brilloHasta = grande ? 0.55f : 0f;
            const float duracion = 0.45f;
            float reloj = 0f;
            while (reloj < duracion)
            {
                reloj += Time.deltaTime;
                float k = Mathf.Clamp01(reloj / duracion);
                const float c1 = 1.4f;
                float u = k - 1f;
                float rebote = 1f + (c1 + 1f) * u * u * u + c1 * u * u;
                receta.anchoredPosition = Vector2.LerpUnclamped(desde, hasta, rebote);
                receta.localScale = Vector3.one * Mathf.LerpUnclamped(escalaDesde, escalaHasta, rebote);
                resplandorReceta.rectTransform.anchoredPosition = receta.anchoredPosition;
                resplandorReceta.rectTransform.localScale = receta.localScale;
                Color c = resplandorReceta.color;
                c.a = Mathf.Lerp(brilloDesde, brilloHasta, k);
                resplandorReceta.color = c;
                yield return null;
            }

            if (!grande)
                SubirReceta(false);  // ya en su tamaño: vuelve detrás, para que los frascos que vuelan se vean encima
            moverReceta = null;
        }

        int lugarResplandor = -1;

        // Cambia el orden de dibujo: arriba = el pergamino (y su resplandor) se dibujan encima de los efectos.
        void SubirReceta(bool arriba)
        {
            if (arriba && lugarResplandor < 0)
            {
                // Al subir a un lugar más alto, los de en medio bajan uno:
                // queda "efectos, resplandor, pergamino", y todo lo demás (pausa, bienvenida) sigue encima.
                lugarResplandor = resplandorReceta.transform.GetSiblingIndex();
                resplandorReceta.transform.SetSiblingIndex(capaEfectos.GetSiblingIndex());
                receta.SetSiblingIndex(resplandorReceta.transform.GetSiblingIndex());
            }
            else if (!arriba && lugarResplandor >= 0)
            {
                resplandorReceta.transform.SetSiblingIndex(lugarResplandor);
                receta.SetSiblingIndex(lugarResplandor + 1);
                lugarResplandor = -1;
            }
        }

        void PonerBarra(float relleno, Color color, int divisiones)
        {
            timerFill.fillAmount = Mathf.Clamp01(relleno);
            timerFill.color = color;
            puntaBarra.enabled = timerFill.fillAmount > 0.01f;
            puntaBarra.rectTransform.anchoredPosition = new Vector2(-AnchoBarra * 0.5f + AnchoBarra * timerFill.fillAmount, 0f);
            Color brillo = Color.Lerp(color, Color.white, 0.6f);
            brillo.a = 0.9f;
            puntaBarra.color = brillo;
            for (int i = 0; i < separadores.Length; i++)
            {
                bool visible = i < divisiones - 1;
                separadores[i].enabled = visible;
                if (visible)
                    separadores[i].rectTransform.anchoredPosition = new Vector2(-AnchoBarra * 0.5f + AnchoBarra * (i + 1f) / divisiones, 0f);
            }
        }

        // Pequeño latido de la barra cuando avanza.
        IEnumerator LatidoBarra()
        {
            float reloj = 0f;
            while (reloj < 0.3f)
            {
                reloj += Time.deltaTime;
                barra.localScale = Vector3.one * (1f + Mathf.Sin(reloj / 0.3f * Mathf.PI) * 0.12f);
                yield return null;
            }

            barra.localScale = Vector3.one;
        }

        // ───────────────────────── Bandeja tallada ─────────────────────────
        // La imagen "bandeja" (Mesa del Aprendiz) trae 6 tablas de colores pegadas. Como en este nivel
        // las cartas cambian de lugar, cada tabla se recorta aparte y viaja con su figura.

        const float BandejaAnchoArte = 1509f;   // tamaño de la imagen original, en píxeles
        const float BandejaAltoArte = 174f;
        const float TablaAnchoArte = 206f;      // cada tabla de color dentro de la imagen
        const float TablaAltoArte = 149f;
        const float TablaCentroYArte = 80.5f;   // desde arriba
        static readonly float[] TablaCentroXArte = { 137f, 382f, 628f, 876f, 1123f, 1373f };

        static readonly Vector2 CentroBandeja = new Vector2(-90f, -440f);
        const float AnchoBandeja = 1352f;
        static float EscalaBandeja => AnchoBandeja / BandejaAnchoArte;
        const float AlturaGema = 14f;           // la figura va un poco arriba del centro de su tabla

        // Qué tabla de color le toca a cada ingrediente (rojo, azul, amarillo, verde, morado, naranja).
        static int ColorDeTabla(IngredientKind tipo)
        {
            switch (tipo)
            {
                case IngredientKind.Fuego: return 0;
                case IngredientKind.Gota: return 1;
                case IngredientKind.Polvo: return 2;
                case IngredientKind.Hierba: return 3;
                case IngredientKind.Cristal: return 4;
                default: return 5;
            }
        }

        // Centro del lugar número "puesto" de la bandeja (0 = el de la izquierda), en la pantalla.
        static Vector2 LugarEnBandeja(int puesto)
        {
            float k = EscalaBandeja;
            float x = (TablaCentroXArte[Mathf.Clamp(puesto, 0, TablaCentroXArte.Length - 1)] - BandejaAnchoArte * 0.5f) * k;
            float y = (BandejaAltoArte * 0.5f - TablaCentroYArte) * k;
            return CentroBandeja + new Vector2(x, y);
        }

        // Recorta una tabla de color de la imagen de la bandeja.
        Sprite TablaDeColor(int cual)
        {
            Sprite bandeja = BandejaTallada;
            if (bandeja == null)
                return null;
            Rect zona = bandeja.rect;
            float x = zona.x + TablaCentroXArte[cual] - TablaAnchoArte * 0.5f;
            float y = zona.y + BandejaAltoArte - TablaCentroYArte - TablaAltoArte * 0.5f;
            return Sprite.Create(bandeja.texture, new Rect(x, y, TablaAnchoArte, TablaAltoArte), new Vector2(0.5f, 0.5f), 100f);
        }

        void ConstruirBandeja(Transform padre, System.Action<IngredientKind> alElegir, System.Action vaciar)
        {
            // El marco de madera con sus 6 huecos; encima, la tabla de color de cada ingrediente.
            float k = EscalaBandeja;
            Sprite arteBandeja = BandejaTallada;
            var repisa = Imagen(padre, "Bandeja", arteBandeja != null ? arteBandeja : Madera, CentroBandeja, new Vector2(AnchoBandeja, BandejaAltoArte * k), arteBandeja != null);
            var sombraRepisa = repisa.gameObject.AddComponent<Shadow>();
            sombraRepisa.effectColor = new Color(0f, 0f, 0f, 0.4f);
            sombraRepisa.effectDistance = new Vector2(0f, -6f);

            float ancho = TablaAnchoArte * k;
            foreach (IngredientKind tipo in RecetaFrascos.Todos)
            {
                Ing info = catalogo[tipo];
                Sprite tabla = TablaDeColor(ColorDeTabla(tipo));
                var carta = Imagen(padre, "Carta_" + tipo, tabla != null ? tabla : Pergamino, Vector2.zero, new Vector2(ancho, TablaAltoArte * k), tabla != null);
                carta.raycastTarget = true;

                var boton = carta.gameObject.AddComponent<Button>();
                boton.targetGraphic = carta;
                boton.navigation = new Navigation { mode = Navigation.Mode.None };
                var colores = boton.colors;
                colores.highlightedColor = new Color(1f, 0.97f, 0.9f, 1f);
                colores.pressedColor = new Color(0.85f, 0.75f, 0.6f, 1f);
                colores.fadeDuration = 0.08f;
                boton.colors = colores;
                IngredientKind capturado = tipo;
                boton.onClick.AddListener(() => alElegir(capturado));
                carta.gameObject.AddComponent<HoverScale>().scale = 1.08f;

                // Halo claro que late detrás de la figura (sobre la tabla de color se ve como un brillo).
                var halo = Imagen(carta.transform, "Halo", disco, new Vector2(0f, AlturaGema), new Vector2(104f, 104f), false);
                Color brillo = Color.Lerp(info.Glow, Color.white, 0.55f);
                brillo.a = 0.45f;
                halo.color = brillo;
                halosBandeja.Add(halo);

                // Solo la figura, igual que en la receta, para no confundir. El frasco aparece al echarlo.
                var gema = Imagen(carta.transform, "Gema", Spr(info.Sprite), new Vector2(0f, AlturaGema), new Vector2(64f, 64f), true);
                gemasBandeja.Add(gema.rectTransform);

                // El nombre en letra crema, como en la Mesa del Aprendiz.
                var nombre = Texto(carta.transform, info.Nombre, 25, Hex("#fff3da"), FontStyle.Bold);
                nombre.rectTransform.anchoredPosition = new Vector2(0f, -36f);
                nombre.rectTransform.sizeDelta = new Vector2(ancho - 10f, 34f);
                var contornoNombre = nombre.gameObject.AddComponent<Outline>();
                contornoNombre.effectColor = new Color(0.1f, 0.05f, 0.03f, 0.75f);
                contornoNombre.effectDistance = new Vector2(1.6f, -1.6f);

                // Sin número en la carta (confundía con los números de la receta).
                // Las teclas 1-6 siguen funcionando según la posición en pantalla.
                cartas[tipo] = carta.rectTransform;
            }

            AcomodarBandeja(RecetaFrascos.Todos, false);

            var botonVaciar = BotonMadera(padre, "VACIAR", new Vector2(790f, -440f), new Vector2(252f, 95f), 30, vaciar, 0.35f, true, true);
            Anclar(botonVaciar.rectTransform, new Vector2(1f, 0f));
            var icono = Imagen(botonVaciar.transform, "Icono", Spr("caldero"), new Vector2(-72f, 4f), new Vector2(54f, 46f), true);
            icono.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -25f);
        }

        // Las figuras de la bandeja flotan y sus halos laten (lo llama MesaDeFrascos cada cuadro).
        public void AnimarBandeja()
        {
            float t = Time.time;
            if (puntaBarra != null)
                puntaBarra.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(t * 8f) * 0.18f);
            for (int i = 0; i < gemasBandeja.Count; i++)
            {
                gemasBandeja[i].anchoredPosition = new Vector2(0f, AlturaGema + Mathf.Sin(t * 2.2f + i * 0.9f) * 3.5f);
                gemasBandeja[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.6f + i) * 4f);
                Color c = halosBandeja[i].color;
                c.a = 0.35f + Mathf.Sin(t * 2.6f + i * 0.9f) * 0.12f;
                halosBandeja[i].color = c;
                halosBandeja[i].rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(t * 2.6f + i * 0.9f) * 0.08f);
            }
        }

        void ConstruirHud(Transform padre, Acciones acciones)
        {
            // Placa tallada del título (la de la Mesa del Aprendiz), colgando de dos cuerdas.
            var letrero = Imagen(padre, "LetreroNivel", Placa, Vector2.zero, new Vector2(430f, 128f), true);
            letrero.rectTransform.pivot = new Vector2(0.5f, 1f);
            letrero.rectTransform.anchoredPosition = new Vector2(-733f, 528f);
            Anclar(letrero.rectTransform, new Vector2(0f, 1f));
            for (int lado = -1; lado <= 1; lado += 2)
            {
                var cuerda = Imagen(letrero.transform, "Cuerda", null, new Vector2(lado * 172f, 94f), new Vector2(7f, 70f), false);
                cuerda.color = Hex("#6b4a2b");
            }
            var pocion = Imagen(letrero.transform, "Icono", SpriteArchivo("Menu/pocion.png"), new Vector2(-138f, 0f), new Vector2(60f, 60f), true);
            pocion.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 10f);
            var titulo = TextoMadera(letrero.transform, NivelesTaller.De(MesaDeFrascos.Nivel).nombre, 32, new Vector2(26f, 0f), new Vector2(300f, 70f));
            titulo.horizontalOverflow = HorizontalWrapMode.Overflow;
            var colgante = letrero.gameObject.AddComponent<MaderaColgante>();
            maderas.Add(colgante);
            colgante.balanceo = 2.5f;
            colgante.velocidad = 1.1f;
            colgante.reaccionaAlMouse = false;

            // Nuevo: Pausa (también con Esc o P) y Salir, que pregunta antes de irse.
            var pausa = BotonMadera(padre, "PAUSA", new Vector2(-835f, 384f), new Vector2(200f, 80f), 26, acciones.pausar, 0.15f, true, true);
            for (int barra = -1; barra <= 1; barra += 2)
            {
                var palo = Imagen(pausa.transform, "Barra", null, new Vector2(-58f + barra * 8f, 2f), new Vector2(10f, 30f), false);
                palo.color = Hex("#fff8ee");
                var sombraPalo = palo.gameObject.AddComponent<Shadow>();
                sombraPalo.effectColor = new Color(0.28f, 0.13f, 0.05f, 0.9f);
                sombraPalo.effectDistance = new Vector2(2f, -2f);
            }

            Anclar(pausa.rectTransform, new Vector2(0f, 1f));

            var salir = BotonMadera(padre, "SALIR", new Vector2(-628f, 384f), new Vector2(200f, 80f), 26, acciones.pedirSalir, 0.25f, true, true);
            Anclar(salir.rectTransform, new Vector2(0f, 1f));

            // Nuevo: silenciar música y sonidos (se guarda para todo el juego).
            Anclar(BotonAudio(padre, true, new Vector2(-878f, 290f), 0.3f, true).rectTransform, new Vector2(0f, 1f));
            Anclar(BotonAudio(padre, false, new Vector2(-772f, 290f), 0.35f, true).rectTransform, new Vector2(0f, 1f));
            Imagen(salir.transform, "Icono", SpriteArchivo("Menu/puerta.png"), new Vector2(-58f, 2f), new Vector2(44f, 44f), true);

            // Marcador en la placa tallada de Ronda y Puntos, con la estrella del menú.
            var marcador = Imagen(padre, "PlacaMarcador", PlacaRonda, new Vector2(765f, 460f), new Vector2(340f, 128f), true);
            Anclar(marcador.rectTransform, new Vector2(1f, 1f));
            rondaLabel = TextoMadera(marcador.transform, "", 32, new Vector2(0f, 18f), new Vector2(280f, 44f));
            Imagen(marcador.transform, "Estrella", SpriteArchivo("Menu/estrella.png"), new Vector2(-76f, -22f), new Vector2(32f, 32f), true);
            puntosLabel = TextoMadera(marcador.transform, "", 26, new Vector2(14f, -22f), new Vector2(200f, 36f));
            puntosLabel.color = Hex("#e6c98b");
            var colganteMarcador = marcador.gameObject.AddComponent<MaderaColgante>();
            colganteMarcador.retraso = 0.1f;
            colganteMarcador.balanceo = 1f;
            colganteMarcador.reaccionaAlMouse = false;
            maderas.Add(colganteMarcador);
        }

        // Nuevo en el nivel 2: placa con los frascos de vida bajo el marcador.
        void ConstruirVidas(Transform padre, int cantidad)
        {
            var placa = Imagen(padre, "PlacaVidas", Madera, new Vector2(765f, 340f), new Vector2(300f, 104f), false);
            Anclar(placa.rectTransform, new Vector2(1f, 1f));
            Vidas = placa.gameObject.AddComponent<VidasFrascos>();
            Vidas.Construir(this, cantidad);
            var colgante = placa.gameObject.AddComponent<MaderaColgante>();
            colgante.retraso = 0.2f;
            colgante.balanceo = 1f;
            colgante.reaccionaAlMouse = false;
            maderas.Add(colgante);
        }

        // Nuevo en el nivel 2: cartel que avisa la ronda espejo.
        void ConstruirAvisoEspejo(Transform padre)
        {
            var cartel = Imagen(padre, "AvisoEspejo", Madera, new Vector2(610f, 120f), new Vector2(450f, 100f), false);
            var texto = TextoMadera(cartel.transform, "¡Ronda espejo: al revés!", 28, Vector2.zero, new Vector2(400f, 70f));
            texto.color = Hex("#ffe08a");
            avisoEspejo = cartel.gameObject;
            avisoEspejo.SetActive(false);
        }

        // Nuevo: capa transparente encima de todo (menos el resultado) para frascos y partículas.
        void ConstruirCapaEfectos(Transform padre)
        {
            var capa = new GameObject("CapaEfectos", typeof(RectTransform));
            capa.transform.SetParent(padre, false);
            capaEfectos = capa.GetComponent<RectTransform>();
            Estirar(capaEfectos);
            efectos = capa.AddComponent<ParticulasUI>();
        }

        // Nuevo: bienvenida y conteo 3, 2, 1. Va debajo de la pausa para poder pausar durante ella.
        void ConstruirBienvenida(Transform padre, ConfigFrascos config)
        {
            var go = new GameObject("Bienvenida", typeof(RectTransform));
            go.transform.SetParent(padre, false);
            Bienvenida = go.AddComponent<BienvenidaFrascos>();
            Bienvenida.Construir(this, config);
        }

        // Nuevo: ventana de pausa. El juego se congela mientras está abierta.
        void ConstruirPausa(Transform padre, Acciones acciones)
        {
            Transform panel = Ventana(padre, "Pausa", new Vector2(600f, 540f), out panelPausa);
            var titulo = Texto(panel, "Pausa", 52, Hex("#4a2c17"), FontStyle.Bold);
            titulo.rectTransform.anchoredPosition = new Vector2(0f, 168f);
            titulo.rectTransform.sizeDelta = new Vector2(400f, 64f);
            var detalle = Texto(panel, "El caldero espera.\nTómate tu tiempo.", 22, Hex("#6a4b32"), FontStyle.Normal);
            detalle.rectTransform.anchoredPosition = new Vector2(0f, 110f);
            detalle.rectTransform.sizeDelta = new Vector2(380f, 60f);

            BotonAudio(panel, true, new Vector2(-60f, 30f), 0f, false);
            BotonAudio(panel, false, new Vector2(60f, 30f), 0.05f, false);

            BotonMadera(panel, "CONTINUAR", new Vector2(0f, -78f), new Vector2(300f, 96f), 28, acciones.continuar, 0.1f, false);
            BotonMadera(panel, "SALIR", new Vector2(0f, -180f), new Vector2(240f, 82f), 24, acciones.pedirSalir, 0.15f, false);
        }

        // Nuevo: pregunta antes de salir.
        void ConstruirConfirmarSalir(Transform padre, Acciones acciones)
        {
            Transform panel = Ventana(padre, "ConfirmarSalir", new Vector2(760f, 400f), out panelSalir);
            var pregunta = Texto(panel, "¿Seguro que quieres salir?", 38, Hex("#4a2c17"), FontStyle.Bold);
            pregunta.rectTransform.anchoredPosition = new Vector2(0f, 92f);
            pregunta.rectTransform.sizeDelta = new Vector2(560f, 50f);
            var detalle = Texto(panel, "Se perderá el avance de este nivel.", 24, Hex("#8d3d36"), FontStyle.Normal);
            detalle.rectTransform.anchoredPosition = new Vector2(0f, 38f);
            detalle.rectTransform.sizeDelta = new Vector2(560f, 34f);

            BotonMadera(panel, "SÍ, SALIR", new Vector2(-132f, -72f), new Vector2(240f, 92f), 24, acciones.confirmarSalir, 0.05f, false);
            BotonMadera(panel, "NO, SEGUIR", new Vector2(132f, -72f), new Vector2(240f, 92f), 24, acciones.cancelarSalir, 0.15f, false);
        }

        // Fondo oscuro que tapa todo (y bloquea los clics) con un marco de madera y pergamino en el centro.
        Transform Ventana(Transform padre, string nombre, Vector2 tamano, out GameObject raiz)
        {
            var velo = Imagen(padre, nombre, solido, Vector2.zero, Vector2.zero, false);
            Estirar(velo.rectTransform);
            velo.color = new Color(0.06f, 0.03f, 0.05f, 0.62f);
            velo.raycastTarget = true;

            // El pergamino rústico de la Mesa del Aprendiz; los botones de madera van encima.
            var marco = Imagen(velo.transform, "Panel", Pergamino, Vector2.zero, tamano, false);
            var sombraMarco = marco.gameObject.AddComponent<Shadow>();
            sombraMarco.effectColor = new Color(0f, 0f, 0f, 0.5f);
            sombraMarco.effectDistance = new Vector2(0f, -10f);
            var hoja = Imagen(marco.transform, "Contenido", null, Vector2.zero, tamano - new Vector2(60f, 60f), false);
            hoja.enabled = false;   // solo agrupa lo de adentro
            raiz = velo.gameObject;
            raiz.SetActive(false);
            return hoja.transform;
        }

        public void MostrarPausa(bool visible)
        {
            panelPausa.SetActive(visible);
            if (visible)
                host.StartCoroutine(PopSinTiempo(panelPausa.transform.GetChild(0) as RectTransform));
        }

        public void MostrarConfirmarSalir(bool visible)
        {
            panelSalir.SetActive(visible);
            if (visible)
                host.StartCoroutine(PopSinTiempo(panelSalir.transform.GetChild(0) as RectTransform));
        }

        public bool ResultadoVisible => resultados != null && resultados.gameObject.activeSelf;

        // Pantalla de resultados (madera, pergamino y todo animado): ver ResultadoFrascos.cs.
        void ConstruirResultado(Transform padre, System.Action irAlMenu, System.Action reintentar, System.Action siguiente, System.Action irAlMapa)
        {
            var go = new GameObject("Resultado", typeof(RectTransform));
            go.transform.SetParent(padre, false);
            resultados = go.AddComponent<ResultadoFrascos>();
            resultados.Construir(this, irAlMenu, reintentar, siguiente, irAlMapa);
        }

        void ConstruirAudio()
        {
            sfx = host.gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            musica = Bucle("FluxLab/Audio/ambiente", AjustesJuego.MusicaActiva ? VolumenMusica : 0f);
            burbujeo = Bucle("FluxLab/Audio/burbujas", AjustesJuego.EfectosActivos ? VolumenBurbujeo : 0f);
        }

        // ───────────────────────── Receta y caldero ─────────────────────────

        public void Mensaje(string texto, string color)
        {
            prompt.text = texto;
            prompt.color = Hex(color);
        }

        public void Temporizador(string texto, float relleno)
        {
            timerTexto.text = texto;
            PonerBarra(relleno, Color.Lerp(Hex("#ff6a4d"), Hex("#7de85a"), Mathf.Clamp01(relleno)), 0);
        }

        public void Marcador(int ronda, int rondas, int puntos)
        {
            rondaLabel.text = "Ronda " + ronda + " / " + rondas;
            puntosLabel.text = "Puntos: " + puntos;
        }

        public void MostrarEspejo(bool visible)
        {
            avisoEspejo.SetActive(visible);
            if (visible)
                host.StartCoroutine(Pop(avisoEspejo.GetComponent<RectTransform>()));
        }

        /// <summary>
        /// Enciende un solo hueco de la receta (los demás quedan con "?").
        /// indice = -1 apaga todos.
        /// </summary>
        public void EncenderIngrediente(List<IngredientKind> receta, int indice)
        {
            for (int i = 0; i < largo; i++)
            {
                bool encendido = i == indice;
                gemasReceta[i].enabled = encendido;
                nombresReceta[i].text = encendido ? catalogo[receta[i]].Nombre : "?";
                Color brillo = encendido ? catalogo[receta[i]].Glow : new Color(0.55f, 0.4f, 0.28f, 1f);
                brillo.a = encendido ? 0.75f : 0.28f;
                brillosReceta[i].color = brillo;
                if (encendido)
                {
                    gemasReceta[i].sprite = Spr(catalogo[receta[i]].Sprite);
                    host.StartCoroutine(Pop(huecosReceta[i]));
                }
            }
        }

        /// <summary>
        /// Muestra la receta entera, como al final de una ronda en el nivel 1.
        /// </summary>
        public void MostrarRecetaCompleta(List<IngredientKind> receta)
        {
            for (int i = 0; i < largo; i++)
            {
                gemasReceta[i].enabled = true;
                gemasReceta[i].sprite = Spr(catalogo[receta[i]].Sprite);
                nombresReceta[i].text = catalogo[receta[i]].Nombre;
                brillosReceta[i].color = new Color(0.55f, 0.4f, 0.28f, 0.28f);
            }
        }

        /// <summary>
        /// Cuántos frascos van en el caldero. Reemplaza a las figuras que el nivel 1 muestra encima.
        /// </summary>
        public void Avance(int puestos, int total)
        {
            timerTexto.text = "Tu turno   ·   " + puestos + " / " + total;
            PonerBarra(total <= 0 ? 0f : (float)puestos / total, Hex("#5ad1ff"), total);
            if (puestos > 0)
                host.StartCoroutine(LatidoBarra());
        }

        public Color ColorDe(IngredientKind tipo) => catalogo[tipo].Glow;

        /// <summary>
        /// El frasco del ingrediente sale de su carta y se vacía en el caldero.
        /// </summary>
        public void LanzarFrasco(IngredientKind tipo, float duracion, System.Action alCaer)
        {
            Sprite frasco = ImagenesFrascos.DeIngrediente(tipo) ?? Spr(catalogo[tipo].Sprite);
            Vector2 desde = cartas[tipo].anchoredPosition + new Vector2(0f, 20f);
            FrascoVertido.Lanzar(capaEfectos, efectos, frasco, desde, CalderoVivo.Boca, catalogo[tipo].Glow, duracion, alCaer);
        }

        public void LimpiarEfectos()
        {
            efectos.Limpiar();
            foreach (Transform hijo in capaEfectos)
            {
                if (hijo.name == "Mensaje")
                    Object.Destroy(hijo.gameObject);
            }
        }

        // ───────────────────────── Mensajes de ánimo ─────────────────────────

        static readonly string[] Elogios = { "¡Bien!", "¡Eso es!", "¡Muy bien!", "¡Sigue así!" };
        static readonly string[] Felicitaciones = { "¡Perfecto!", "¡Excelente!", "¡Genial!", "¡Lo lograste!" };
        static readonly string[] Lamentos = { "¡Oh nooo!", "¡Ay, nooo!", "¡Oh nooo, casi!" };

        /// <summary>Mensajito verde al lado del caldero cuando un ingrediente es correcto.</summary>
        public void Elogio()
        {
            host.StartCoroutine(Flotante(Elogios[Random.Range(0, Elogios.Length)], Hex("#c9f27a"), new Color(0.1f, 0.28f, 0.06f, 1f),
                CalderoVivo.Boca + new Vector2(300f, 20f), 48, 0.95f, false, null));
        }

        /// <summary>Mensaje grande y dorado cuando la receta sale completa.</summary>
        public void Felicitar()
        {
            host.StartCoroutine(Flotante(Felicitaciones[Random.Range(0, Felicitaciones.Length)], Hex("#ffe08a"), new Color(0.42f, 0.2f, 0.04f, 1f),
                new Vector2(0f, -40f), 88, 1.5f, false, "¡Receta correcta!"));
        }

        /// <summary>"¡Oh nooo!" grande y temblando cuando se rompe un frasco.</summary>
        public void Lamentar(int quedan)
        {
            host.StartCoroutine(Flotante(Lamentos[Random.Range(0, Lamentos.Length)], Hex("#ff8f7d"), new Color(0.35f, 0.04f, 0.1f, 1f),
                new Vector2(0f, -40f), 88, 1.6f, true, quedan > 0 ? "¡Tú puedes, sigue intentando!" : "Se acabaron los frascos..."));
        }

        IEnumerator Flotante(string texto, Color color, Color borde, Vector2 donde, int letra, float duracion, bool temblar, string debajo)
        {
            var raiz = new GameObject("Mensaje", typeof(RectTransform)).GetComponent<RectTransform>();
            raiz.SetParent(capaEfectos, false);
            raiz.anchoredPosition = donde;

            var principal = Texto(raiz, texto, letra, color, FontStyle.Bold);
            principal.rectTransform.sizeDelta = new Vector2(1200f, letra * 1.5f);
            principal.horizontalOverflow = HorizontalWrapMode.Overflow;
            var contorno = principal.gameObject.AddComponent<Outline>();
            contorno.effectColor = borde;
            contorno.effectDistance = new Vector2(letra / 16f, -letra / 16f);
            var sombraTexto = principal.gameObject.AddComponent<Shadow>();
            sombraTexto.effectColor = new Color(0f, 0f, 0f, 0.45f);
            sombraTexto.effectDistance = new Vector2(letra / 24f, -letra / 10f);

            Text secundario = null;
            if (debajo != null)
            {
                secundario = Texto(raiz, debajo, Mathf.RoundToInt(letra * 0.38f), Color.white, FontStyle.Bold);
                secundario.rectTransform.anchoredPosition = new Vector2(0f, -letra * 0.85f);
                secundario.rectTransform.sizeDelta = new Vector2(1000f, letra * 0.6f);
                var contornoDebajo = secundario.gameObject.AddComponent<Outline>();
                contornoDebajo.effectColor = borde;
                contornoDebajo.effectDistance = new Vector2(2f, -2f);
            }

            float reloj = 0f;
            while (reloj < duracion && raiz != null)
            {
                reloj += Time.deltaTime;
                float k = reloj / duracion;
                float entrada = Mathf.Clamp01(k / 0.18f);
                float salida = Mathf.Clamp01((k - 0.75f) / 0.25f);
                const float c1 = 1.70158f;
                float u = entrada - 1f;
                float rebote = 1f + (c1 + 1f) * u * u * u + c1 * u * u;
                float temblor = temblar && k > 0.15f && k < 0.6f ? Mathf.Sin(reloj * 55f) * 9f * (0.6f - k) / 0.45f : 0f;

                raiz.anchoredPosition = donde + new Vector2(temblor, k * 50f);
                raiz.localScale = Vector3.one * Mathf.LerpUnclamped(0.3f, 1f, rebote);
                raiz.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(reloj * 7f) * 3f * (1f - salida));
                float alfa = 1f - salida;
                principal.color = new Color(color.r, color.g, color.b, alfa);
                if (secundario != null)
                    secundario.color = new Color(1f, 1f, 1f, alfa);
                yield return null;
            }

            if (raiz != null)
                Object.Destroy(raiz.gameObject);
        }

        /// <summary>
        /// Coloca las cartas en el orden dado. Las teclas 1-6 siguen la posición en pantalla.
        /// </summary>
        public void AcomodarBandeja(IngredientKind[] orden, bool animar)
        {
            for (int i = 0; i < orden.Length; i++)
            {
                RectTransform carta = cartas[orden[i]];
                carta.anchoredPosition = LugarEnBandeja(i);
                if (animar)
                    host.StartCoroutine(Pop(carta));
            }
        }

        void AcomodarHuecos()
        {
            if (huecosReceta == null)
                return;

            float paso = largo >= 5 ? 104f : largo == 4 ? 122f : 148f;
            float centro = (largo - 1) * 0.5f;
            for (int i = 0; i < MaxReceta; i++)
            {
                bool activo = i < largo;
                huecosReceta[i].gameObject.SetActive(activo);
                if (activo)
                    huecosReceta[i].anchoredPosition = new Vector2((i - centro) * paso, -6f);
            }
        }

        // ───────────────────────── Resultado ─────────────────────────

        public void OcultarResultado() => resultados.Ocultar();

        public void MostrarResultado(Resultado r) => resultados.Mostrar(r);

        // ───────────────────────── Magnus ─────────────────────────

        /// <summary>
        /// Lo que hace Magnus en el juego. Solo usa poses de Riendo y Sorprendido;
        /// Observa y Remueve son movimientos con la imagen normal.
        /// </summary>
        public void PonerAnimo(AnimoMagnus siguiente)
        {
            switch (siguiente)
            {
                case AnimoMagnus.Observa: Magnus.Observar(); break;
                case AnimoMagnus.Remueve: Magnus.Remover(); break;
                case AnimoMagnus.Festeja: Magnus.Reir(); break;
                case AnimoMagnus.SeEquivoca: Magnus.Sorprenderse(); break;
                default: Magnus.Quieto(); break;
            }
        }

        // ───────────────────────── Sonido ─────────────────────────

        /// <summary>
        /// ruta dentro de una carpeta Resources, por ejemplo "FluxLab/Audio/click" o "Nivel2/romper".
        /// </summary>
        public void Sonar(string ruta, float volumen = 0.85f)
        {
            if (!AjustesJuego.EfectosActivos || sfx == null)
                return;
            var clip = Resources.Load<AudioClip>(ruta);
            if (clip != null)
                sfx.PlayOneShot(clip, volumen);
        }

        AudioSource Bucle(string ruta, float volumen)
        {
            var clip = Resources.Load<AudioClip>(ruta);
            if (clip == null)
                return null;
            var fuente = host.gameObject.AddComponent<AudioSource>();
            fuente.clip = clip;
            fuente.loop = true;
            fuente.volume = volumen;
            fuente.playOnAwake = false;
            fuente.Play();
            return fuente;
        }

        // ───────────────────────── Música y sonidos ─────────────────────────

        const float VolumenMusica = 0.28f;
        const float VolumenBurbujeo = 0.16f;
        AudioSource musica;
        AudioSource burbujeo;
        readonly List<Image> iconosMusica = new List<Image>();
        readonly List<Image> iconosSonido = new List<Image>();

        /// <summary>Prende o apaga la música. Se guarda igual que en el menú (AjustesJuego).</summary>
        public void AlternarMusica()
        {
            AjustesJuego.MusicaActiva = !AjustesJuego.MusicaActiva;
            RefrescarAudio();
        }

        /// <summary>Prende o apaga los efectos de sonido (también el burbujeo del caldero).</summary>
        public void AlternarSonido()
        {
            AjustesJuego.EfectosActivos = !AjustesJuego.EfectosActivos;
            RefrescarAudio();
            Sonar("FluxLab/Audio/click");
        }

        void RefrescarAudio()
        {
            if (musica != null)
                musica.volume = AjustesJuego.MusicaActiva ? VolumenMusica : 0f;
            if (burbujeo != null)
                burbujeo.volume = AjustesJuego.EfectosActivos ? VolumenBurbujeo : 0f;
            foreach (Image icono in iconosMusica)
                PintarIconoAudio(icono, AjustesJuego.MusicaActiva);
            foreach (Image icono in iconosSonido)
                PintarIconoAudio(icono, AjustesJuego.EfectosActivos);
        }

        // Prendido: icono normal. Apagado: icono apagado con una raya roja encima.
        static void PintarIconoAudio(Image icono, bool prendido)
        {
            icono.color = prendido ? Color.white : new Color(1f, 1f, 1f, 0.4f);
            icono.transform.GetChild(0).gameObject.SetActive(!prendido);
        }

        /// <summary>Botón cuadrado de madera con el icono de música o de sonido del menú.</summary>
        Image BotonAudio(Transform padre, bool esMusica, Vector2 posicion, float retraso, bool esperarSenal)
        {
            var placa = BotonMadera(padre, "", posicion, new Vector2(92f, 92f), 20, esMusica ? (System.Action)AlternarMusica : AlternarSonido, retraso, esperarSenal);
            // Botón redondo tallado, como los de pausa y sonido del Jardín de Luna.
            placa.sprite = Redondo;
            placa.type = Image.Type.Simple;
            placa.preserveAspect = true;
            var icono = Imagen(placa.transform, "Icono", SpriteArchivo(esMusica ? "Menu/musica.png" : "Menu/sonido.png"), new Vector2(0f, 2f), new Vector2(52f, 52f), true);
            var raya = Imagen(icono.transform, "Apagado", null, Vector2.zero, new Vector2(64f, 8f), false);
            raya.color = Hex("#d64545");
            raya.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -45f);
            (esMusica ? iconosMusica : iconosSonido).Add(icono);
            PintarIconoAudio(icono, esMusica ? AjustesJuego.MusicaActiva : AjustesJuego.EfectosActivos);
            return placa;
        }

        // ───────────────────────── Animaciones cortas ─────────────────────────

        // Como Pop, pero usa el reloj real: se anima aunque el juego esté en pausa.
        IEnumerator PopSinTiempo(RectTransform objetivo)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime * 7f;
                if (objetivo != null)
                    objetivo.localScale = Vector3.one * Mathf.Lerp(0.7f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)));
                yield return null;
            }
        }

        public IEnumerator Pop(RectTransform objetivo)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 6f;
                if (objetivo != null)
                    objetivo.localScale = Vector3.one * Mathf.Lerp(0.45f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)));
                yield return null;
            }
        }

        // ───────────────────────── Ayudas de UI (las usa también VidasFrascos) ─────────────────────────

        public Image Imagen(Transform padre, string nombre, Sprite sprite, Vector2 posicion, Vector2 tamano, bool aspecto)
        {
            var go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(padre, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = posicion;
            rect.sizeDelta = tamano;
            var imagen = go.GetComponent<Image>();
            imagen.sprite = sprite;
            imagen.preserveAspect = aspecto;
            imagen.raycastTarget = false;
            // Maderas y pergaminos traen sus bordes marcados: se estiran por el centro sin deformar las esquinas.
            if (!aspecto && sprite != null && sprite.border.sqrMagnitude > 0f)
                imagen.type = Image.Type.Sliced;
            return imagen;
        }

        public Text Texto(Transform padre, string contenido, int tamano, Color color, FontStyle estilo)
        {
            var go = new GameObject("Texto", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(padre, false);
            var texto = go.GetComponent<Text>();
            texto.font = font;
            texto.text = contenido;
            texto.fontSize = tamano;
            texto.fontStyle = letraTallada && estilo == FontStyle.Bold ? FontStyle.Normal : estilo;
            texto.color = color;
            texto.alignment = TextAnchor.MiddleCenter;
            texto.horizontalOverflow = HorizontalWrapMode.Wrap;
            texto.verticalOverflow = VerticalWrapMode.Overflow;
            texto.raycastTarget = false;
            texto.rectTransform.sizeDelta = new Vector2(300f, 40f);
            return texto;
        }

        public Sprite Spr(string nombre)
        {
            // Figuras y burbujas nuevas del nivel 2 (NIVEL2-IMAGENES/figuras), si hay una para este nombre.
            Sprite propia = ImagenesFrascos.Figura(nombre);
            if (propia != null)
                return propia;

            var sprite = Resources.Load<Sprite>("FluxLab/" + nombre);
            if (sprite != null)
                return sprite;
            Debug.LogWarning("No se encontró el sprite FluxLab/" + nombre);
            return solido;
        }

        public static Color Hex(string valor)
        {
            ColorUtility.TryParseHtmlString(valor, out Color color);
            return color;
        }

        RawImage Raw(Transform padre, string nombre, Texture2D textura, Color color)
        {
            var go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            go.transform.SetParent(padre, false);
            var imagen = go.GetComponent<RawImage>();
            imagen.texture = textura;
            imagen.color = color;
            imagen.raycastTarget = false;
            return imagen;
        }

        RectTransform Contenedor(Transform padre, string nombre)
        {
            var go = new GameObject(nombre, typeof(RectTransform));
            go.transform.SetParent(padre, false);
            return go.GetComponent<RectTransform>();
        }

        Image Hueco(Transform padre, float lado, out Image fondo)
        {
            fondo = Imagen(padre, "Hueco", disco, Vector2.zero, new Vector2(lado, lado), true);
            fondo.color = new Color(0.55f, 0.4f, 0.28f, 0.28f);
            var gema = Imagen(fondo.transform, "Gema", solido, Vector2.zero, new Vector2(lado * 0.78f, lado * 0.78f), true);
            gema.enabled = false;
            return gema;
        }

        /// <summary>
        /// Botón de tabla de madera como los del menú principal: texto crema con contorno,
        /// cae rebotando al empezar, se balancea, crece con el mouse y se aplasta al hacer clic.
        /// </summary>
        public Image BotonMadera(Transform padre, string etiqueta, Vector2 posicion, Vector2 tamano, int letra, System.Action accion, float retraso, bool esperarSenal = true, bool conIcono = false)
        {
            var placa = Imagen(padre, etiqueta, Madera, posicion, tamano, false);
            placa.raycastTarget = true;
            var boton = placa.gameObject.AddComponent<Button>();
            boton.targetGraphic = placa;
            boton.navigation = new Navigation { mode = Navigation.Mode.None };
            var colores = boton.colors;
            colores.highlightedColor = new Color(1f, 0.96f, 0.88f, 1f);
            colores.pressedColor = new Color(0.82f, 0.7f, 0.52f, 1f);
            colores.fadeDuration = 0.06f;
            boton.colors = colores;
            boton.onClick.AddListener(() =>
            {
                Sonar("FluxLab/Audio/click");
                accion?.Invoke();
            });

            var movimiento = placa.gameObject.AddComponent<MaderaColgante>();
            movimiento.retraso = retraso;
            movimiento.balanceo = 1.2f;
            movimiento.esperarSenal = esperarSenal;
            if (esperarSenal)
                maderas.Add(movimiento);

            // Con icono, el texto se corre a la derecha para dejarle lugar; sin icono, va centrado.
            if (conIcono)
                TextoMadera(placa.transform, etiqueta, letra, new Vector2(tamano.x * 0.1f, 0f), new Vector2(tamano.x * 0.62f, tamano.y * 0.8f));
            else
                TextoMadera(placa.transform, etiqueta, letra, Vector2.zero, new Vector2(tamano.x * 0.82f, tamano.y * 0.8f));
            return placa;
        }

        /// <summary>
        /// Pega un elemento a una esquina de la pantalla (por ejemplo (0,1) = arriba a la izquierda)
        /// sin moverlo en 1920x1080. Así no se sale de la pantalla si la ventana es más angosta.
        /// </summary>
        static void Anclar(RectTransform rect, Vector2 esquina)
        {
            Vector2 desdeCentro = rect.anchoredPosition;
            rect.anchorMin = esquina;
            rect.anchorMax = esquina;
            rect.anchoredPosition = desdeCentro - new Vector2((esquina.x - 0.5f) * 1920f, (esquina.y - 0.5f) * 1080f);
        }

        // Texto crema con contorno café oscuro, el mismo estilo de los botones de la Mesa del Aprendiz.
        public Text TextoMadera(Transform padre, string contenido, int letra, Vector2 posicion, Vector2 tamano)
        {
            var texto = Texto(padre, contenido, letra, Hex("#f3e4c4"), FontStyle.Bold);
            texto.rectTransform.anchoredPosition = posicion;
            texto.rectTransform.sizeDelta = tamano;
            var contorno = texto.gameObject.AddComponent<Outline>();
            contorno.effectColor = new Color(0.28f, 0.13f, 0.05f, 0.95f);
            contorno.effectDistance = new Vector2(1.8f, -1.8f);
            return texto;
        }

        public Image Boton(Transform padre, string etiqueta, Sprite sprite, Vector2 posicion, Vector2 tamano, int letra, Color colorTexto, System.Action accion)
        {
            var imagen = Imagen(padre, etiqueta, sprite, posicion, tamano, false);
            imagen.type = Image.Type.Sliced;
            imagen.raycastTarget = true;
            var boton = imagen.gameObject.AddComponent<Button>();
            boton.targetGraphic = imagen;
            boton.navigation = new Navigation { mode = Navigation.Mode.None };
            boton.onClick.AddListener(() => accion?.Invoke());
            imagen.gameObject.AddComponent<HoverScale>().scale = 1.05f;
            var texto = Texto(imagen.transform, etiqueta, letra, colorTexto, FontStyle.Bold);
            Estirar(texto.rectTransform);
            return imagen;
        }

        Canvas CrearCanvas()
        {
            var go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var escalador = go.GetComponent<CanvasScaler>();
            escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escalador.referenceResolution = new Vector2(1920f, 1080f);
            escalador.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        static void AsegurarEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        static void Contorno(Text texto, Color color)
        {
            var contorno = texto.gameObject.AddComponent<Outline>();
            contorno.effectColor = color;
            contorno.effectDistance = new Vector2(1.2f, -1.2f);
        }

        static void Estirar(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        Texture2D Archivo(string relativo)
        {
            string ruta = Path.Combine(Application.streamingAssetsPath, relativo);
            if (!File.Exists(ruta))
                return Tex("pared");
            var textura = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            textura.LoadImage(File.ReadAllBytes(ruta));
            textura.filterMode = FilterMode.Bilinear;
            return textura;
        }

        Sprite SpriteArchivo(string relativo)
        {
            Texture2D textura = Archivo(relativo);
            if (textura == null)
                return solido;
            return Sprite.Create(textura, new Rect(0f, 0f, textura.width, textura.height), new Vector2(0.5f, 0.5f), 100f);
        }

        static Texture2D Tex(string nombre)
        {
            return Resources.Load<Texture2D>("FluxLab/" + nombre);
        }

        void LlenarCatalogo()
        {
            catalogo[IngredientKind.Fuego] = new Ing { Sprite = "gem_red_circle", Nombre = "Esencia", Glow = Hex("#e23b3b") };
            catalogo[IngredientKind.Gota] = new Ing { Sprite = "gem_blue_triangle", Nombre = "Gota", Glow = Hex("#3a78d8") };
            catalogo[IngredientKind.Polvo] = new Ing { Sprite = "gem_yellow_star", Nombre = "Polvo", Glow = Hex("#f0c43a") };
            catalogo[IngredientKind.Hierba] = new Ing { Sprite = "gem_green_square", Nombre = "Hierba", Glow = Hex("#3ea86a") };
            catalogo[IngredientKind.Cristal] = new Ing { Sprite = "gem_purple_pentagon", Nombre = "Cristal", Glow = Hex("#8a4ec4") };
            catalogo[IngredientKind.Lagrima] = new Ing { Sprite = "gem_orange_crescent", Nombre = "Lágrima", Glow = Hex("#f08a2a") };
        }

        static Sprite CrearSolido()
        {
            var textura = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var pixeles = new Color[16];
            for (int i = 0; i < pixeles.Length; i++)
                pixeles[i] = Color.white;
            textura.SetPixels(pixeles);
            textura.Apply();
            return Sprite.Create(textura, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
        }

        static Sprite CrearDisco(int lado)
        {
            var textura = new Texture2D(lado, lado, TextureFormat.RGBA32, false);
            float radio = (lado - 1) * 0.5f;
            var centro = new Vector2(radio, radio);
            for (int y = 0; y < lado; y++)
            {
                for (int x = 0; x < lado; x++)
                {
                    float alfa = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(x, y), centro) / radio);
                    textura.SetPixel(x, y, new Color(1f, 1f, 1f, alfa * alfa));
                }
            }

            textura.Apply();
            return Sprite.Create(textura, new Rect(0f, 0f, lado, lado), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
