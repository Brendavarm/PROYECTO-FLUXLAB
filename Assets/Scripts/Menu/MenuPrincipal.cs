using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FluxLab
{
    /// <summary>
    /// Pantalla principal. Se arma al pulsar Play.
    /// </summary>
    public class MenuPrincipal : MonoBehaviour
    {
        Font font;
        AudioSource musica;
        AudioSource sfx;
        Text puntajeTexto;
        RawImage iconoSonido;
        RawImage iconoMusica;
        GameObject panelOpciones;
        GameObject panelLogros;
        Text textoLogros;

        readonly System.Collections.Generic.Dictionary<string, Texture2D> artes = new System.Collections.Generic.Dictionary<string, Texture2D>();

        void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            CargarArte();
            Construir();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (panelOpciones.activeSelf || panelLogros.activeSelf)
                    CerrarPaneles();
            }
        }

        void CargarArte()
        {
            string[] nombres =
            {
                "fondo", "pergamino", "titulo", "boton",
                "estrella", "engranaje", "trofeo", "corona", "puerta", "sonido", "musica", "pocion", "icono_magnus"
            };
            foreach (string nombre in nombres)
                artes[nombre] = LeerTextura(nombre + ".png");
        }

        Texture2D LeerTextura(string archivo)
        {
            string ruta = Path.Combine(Application.streamingAssetsPath, "Menu", archivo);
            if (!File.Exists(ruta))
            {
                Debug.LogWarning("No está el arte de menú: " + ruta);
                return Texture2D.whiteTexture;
            }

            var textura = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            textura.LoadImage(File.ReadAllBytes(ruta));
            textura.filterMode = FilterMode.Bilinear;
            textura.wrapMode = TextureWrapMode.Clamp;
            return textura;
        }

        void Construir()
        {
            if (FindAnyObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var lienzo = new GameObject("Lienzo", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = lienzo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = lienzo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Transform raiz = lienzo.transform;
            var fondo = Imagen(raiz, "Fondo", artes["fondo"], Vector2.zero, 1920f);
            Estirar(fondo.rectTransform);
            SoltarBurbujas(raiz);

            var pergamino = Imagen(raiz, "Pergamino", artes["pergamino"], new Vector2(0f, 400f), 860f);
            Imagen(pergamino.transform, "Titulo", artes["titulo"], new Vector2(0f, 4f), 640f);

            CrearBoton(raiz, "JUGAR", artes["estrella"], null, new Vector2(0f, 118f), 600f, 100f, true, IrAJugar);
            CrearBoton(raiz, "PERSONALIZAR", artes["icono_magnus"], null, new Vector2(0f, 8f), 520f, 86f, false, IrAPersonalizar);
            CrearBoton(raiz, "OPCIONES", artes["engranaje"], null, new Vector2(0f, -92f), 500f, 86f, false, AbrirOpciones);
            CrearBoton(raiz, "LOGROS", artes["trofeo"], artes["corona"], new Vector2(0f, -190f), 500f, 86f, false, AbrirLogros);
            CrearBoton(raiz, "SALIR", artes["puerta"], null, new Vector2(0f, -286f), 380f, 76f, false, Salir);

            ConstruirMarca(raiz);
            ConstruirSonidos(raiz);
            ConstruirPaneles(raiz);
            PrepararAudio();
        }

        void ConstruirMarca(Transform raiz)
        {
            var placa = Imagen(raiz, "Marca", artes["boton"], new Vector2(735f, 452f), 340f);
            var pocion = Imagen(placa.transform, "Pocion", artes["pocion"], new Vector2(-108f, 2f), 62f);
            pocion.rectTransform.anchorMin = pocion.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);

            var nombre = Texto(placa.transform, "Magnus", 22, Hex("#fff4e4"), FontStyle.Bold);
            nombre.alignment = TextAnchor.MiddleLeft;
            nombre.rectTransform.anchoredPosition = new Vector2(28f, 16f);
            nombre.rectTransform.sizeDelta = new Vector2(180f, 30f);
            Contorno(nombre, new Color(0.25f, 0.12f, 0.05f, 0.9f));

            puntajeTexto = Texto(placa.transform, AjustesJuego.MejorPuntaje.ToString(), 28, Hex("#ffe08a"), FontStyle.Bold);
            puntajeTexto.alignment = TextAnchor.MiddleLeft;
            puntajeTexto.rectTransform.anchoredPosition = new Vector2(28f, -16f);
            puntajeTexto.rectTransform.sizeDelta = new Vector2(180f, 34f);
            Contorno(puntajeTexto, new Color(0.25f, 0.12f, 0.05f, 0.9f));
        }

        void ConstruirSonidos(Transform raiz)
        {
            iconoSonido = BotonIcono(raiz, "Sonido", artes["sonido"], "SONIDO", new Vector2(690f, -470f), AlternarSonido);
            iconoMusica = BotonIcono(raiz, "Musica", artes["musica"], "MÚSICA", new Vector2(840f, -470f), AlternarMusica);
            RefrescarIconos();
        }

        RawImage BotonIcono(Transform padre, string nombre, Texture2D icono, string etiqueta, Vector2 posicion, UnityEngine.Events.UnityAction accion)
        {
            var imagen = Imagen(padre, nombre, icono, posicion, 78f);
            imagen.raycastTarget = true;
            var boton = imagen.gameObject.AddComponent<Button>();
            boton.targetGraphic = imagen;
            boton.navigation = new Navigation { mode = Navigation.Mode.None };
            boton.onClick.AddListener(accion);
            imagen.gameObject.AddComponent<HoverScale>().scale = 1.08f;

            var texto = Texto(imagen.transform, etiqueta, 18, Hex("#fff6ea"), FontStyle.Bold);
            texto.rectTransform.anchoredPosition = new Vector2(0f, -58f);
            texto.rectTransform.sizeDelta = new Vector2(140f, 28f);
            Contorno(texto, new Color(0.1f, 0.05f, 0.03f, 0.85f));
            return imagen;
        }

        void ConstruirPaneles(Transform raiz)
        {
            Sprite panel = Resources.Load<Sprite>("FluxLab/panel_beige");
            panelOpciones = CrearPanel(raiz, "PanelOpciones", panel, "Opciones", "La música y los efectos se guardan en este equipo.", CerrarPaneles);
            panelLogros = CrearPanel(raiz, "PanelLogros", panel, "Logros", "", CerrarPaneles);
            textoLogros = Texto(panelLogros.transform.GetChild(1), "", 30, Hex("#3b2a22"), FontStyle.Normal);
            textoLogros.rectTransform.anchoredPosition = new Vector2(0f, 10f);
            textoLogros.rectTransform.sizeDelta = new Vector2(620f, 180f);
            panelOpciones.SetActive(false);
            panelLogros.SetActive(false);
        }

        GameObject CrearPanel(Transform padre, string nombre, Sprite sprite, string titulo, string cuerpo, UnityEngine.Events.UnityAction cerrar)
        {
            var raiz = new GameObject(nombre, typeof(RectTransform));
            raiz.transform.SetParent(padre, false);
            Estirar(raiz.GetComponent<RectTransform>());

            var velo = new GameObject("Velo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            velo.transform.SetParent(raiz.transform, false);
            var veloImagen = velo.GetComponent<Image>();
            veloImagen.color = new Color(0.08f, 0.05f, 0.04f, 0.62f);
            veloImagen.raycastTarget = true;
            Estirar(velo.GetComponent<RectTransform>());

            var tarjetaGo = new GameObject("Tarjeta", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            tarjetaGo.transform.SetParent(raiz.transform, false);
            var tarjeta = tarjetaGo.GetComponent<RectTransform>();
            tarjeta.anchorMin = tarjeta.anchorMax = new Vector2(0.5f, 0.5f);
            tarjeta.sizeDelta = new Vector2(760f, 440f);
            var imagen = tarjetaGo.GetComponent<Image>();
            imagen.sprite = sprite;
            imagen.type = Image.Type.Sliced;
            imagen.color = Hex("#f4ead7");

            var tituloTexto = Texto(tarjetaGo.transform, titulo, 42, Hex("#3b2a22"), FontStyle.Bold);
            tituloTexto.rectTransform.anchoredPosition = new Vector2(0f, 150f);
            tituloTexto.rectTransform.sizeDelta = new Vector2(680f, 60f);

            if (!string.IsNullOrEmpty(cuerpo))
            {
                var cuerpoTexto = Texto(tarjetaGo.transform, cuerpo, 26, Hex("#3b2a22"), FontStyle.Normal);
                cuerpoTexto.rectTransform.anchoredPosition = new Vector2(0f, 40f);
                cuerpoTexto.rectTransform.sizeDelta = new Vector2(640f, 120f);
            }

            var cerrarImagen = Imagen(tarjetaGo.transform, "Cerrar", artes["boton"], new Vector2(0f, -150f), 280f);
            cerrarImagen.raycastTarget = true;
            var boton = cerrarImagen.gameObject.AddComponent<Button>();
            boton.targetGraphic = cerrarImagen;
            boton.onClick.AddListener(cerrar);
            var etiqueta = Texto(cerrarImagen.transform, "Cerrar", 28, Hex("#fff6ea"), FontStyle.Bold);
            Estirar(etiqueta.rectTransform);
            Contorno(etiqueta, new Color(0.25f, 0.12f, 0.05f, 0.8f));
            return raiz;
        }

        void CrearBoton(Transform padre, string etiqueta, Texture2D iconoIzquierdo, Texture2D iconoDerecho, Vector2 posicion, float ancho, float alto, bool brillar, UnityEngine.Events.UnityAction accion)
        {
            if (brillar)
            {
                var brillo = Imagen(padre, etiqueta + "Brillo", artes["boton"], posicion, ancho + 28f, alto + 18f);
                brillo.color = new Color(0.55f, 1f, 0.95f, 0.5f);
            }

            var placa = Imagen(padre, etiqueta, artes["boton"], posicion, ancho, alto);
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
                Sonar("click");
                accion();
            });
            placa.gameObject.AddComponent<HoverScale>().scale = brillar ? 1.04f : 1.035f;

            bool etiquetaLarga = etiqueta.Length > 9;
            if (iconoIzquierdo != null)
            {
                float lado = brillar ? 72f : (etiquetaLarga ? 52f : 60f);
                var icono = Imagen(placa.transform, "Icono", iconoIzquierdo, new Vector2(-ancho * (etiquetaLarga ? 0.40f : 0.34f), 2f), lado);
                icono.raycastTarget = false;
            }

            if (iconoDerecho != null)
            {
                var icono = Imagen(placa.transform, "IconoDerecho", iconoDerecho, new Vector2(ancho * 0.34f, 4f), 58f);
                icono.raycastTarget = false;
            }

            int tamano = brillar ? 40 : (etiquetaLarga ? 26 : 32);
            var texto = Texto(placa.transform, etiqueta, tamano, Hex("#fff8ee"), FontStyle.Bold);
            texto.rectTransform.sizeDelta = new Vector2(ancho * (etiquetaLarga ? 0.58f : 0.62f), 70f);
            Contorno(texto, new Color(0.28f, 0.13f, 0.05f, 0.95f));
        }

        void PrepararAudio()
        {
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            var clip = Resources.Load<AudioClip>("FluxLab/Audio/menu");
            if (clip == null)
                return;
            musica = gameObject.AddComponent<AudioSource>();
            musica.clip = clip;
            musica.loop = true;
            musica.playOnAwake = false;
            musica.volume = AjustesJuego.MusicaActiva ? 0.42f : 0f;
            musica.Play();
        }

        void IrAJugar()
        {
            SceneManager.LoadScene("MapaNiveles");
        }

        void IrAPersonalizar()
        {
            SceneManager.LoadScene("PersonalizarMagnus");
        }

        void SoltarBurbujas(Transform raiz)
        {
            Texture2D[] burbujas = new Texture2D[3];
            string[] nombres = { "burbuja_a", "burbuja_b", "burbuja_c" };
            for (int i = 0; i < nombres.Length; i++)
            {
                Sprite sprite = Resources.Load<Sprite>("FluxLab/" + nombres[i]);
                burbujas[i] = sprite != null ? sprite.texture : Resources.Load<Texture2D>("FluxLab/" + nombres[i]);
            }

            for (int i = 0; i < 12; i++)
            {
                Texture2D textura = burbujas[i % 3];
                if (textura == null)
                    continue;

                float x = -860f + (i * 157f) % 1720f;
                float y = -560f + (i * 113f) % 1040f;
                var imagen = Imagen(raiz, "Burbuja" + i, textura, new Vector2(x, y), 28f + (i % 5) * 12f);
                imagen.color = i % 4 == 0
                    ? new Color(0.75f, 1f, 0.9f, 0.9f)
                    : new Color(1f, 1f, 1f, 0.82f);
                var flotante = imagen.gameObject.AddComponent<BurbujaFlotante>();
                flotante.velocidad = 18f + (i % 4) * 11f;
                flotante.amplitud = 10f + (i % 3) * 7f;
            }
        }

        void AbrirOpciones()
        {
            CerrarPaneles();
            panelOpciones.SetActive(true);
        }

        void AbrirLogros()
        {
            CerrarPaneles();
            int puntos = AjustesJuego.MejorPuntaje;
            int pociones = AjustesJuego.PocionesHechas;
            textoLogros.text = puntos == 0 && pociones == 0
                ? "Todavía no hay marcas.\nCompleta la primera prueba en la mesa."
                : "Mejor puntaje:  " + puntos + "\nPociones acertadas:  " + pociones;
            panelLogros.SetActive(true);
        }

        void CerrarPaneles()
        {
            panelOpciones.SetActive(false);
            panelLogros.SetActive(false);
            if (puntajeTexto != null)
                puntajeTexto.text = AjustesJuego.MejorPuntaje.ToString();
        }

        void AlternarSonido()
        {
            AjustesJuego.EfectosActivos = !AjustesJuego.EfectosActivos;
            RefrescarIconos();
            Sonar("click");
        }

        void AlternarMusica()
        {
            AjustesJuego.MusicaActiva = !AjustesJuego.MusicaActiva;
            if (musica != null)
                musica.volume = AjustesJuego.MusicaActiva ? 0.42f : 0f;
            RefrescarIconos();
            Sonar("click");
        }

        void RefrescarIconos()
        {
            if (iconoSonido != null)
                iconoSonido.color = AjustesJuego.EfectosActivos ? Color.white : new Color(1f, 1f, 1f, 0.35f);
            if (iconoMusica != null)
                iconoMusica.color = AjustesJuego.MusicaActiva ? Color.white : new Color(1f, 1f, 1f, 0.35f);
        }

        void Salir()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void Sonar(string clip)
        {
            if (!AjustesJuego.EfectosActivos || sfx == null)
                return;
            var audio = Resources.Load<AudioClip>("FluxLab/Audio/" + clip);
            if (audio != null)
                sfx.PlayOneShot(audio, 0.8f);
        }

        RawImage Imagen(Transform padre, string nombre, Texture2D textura, Vector2 posicion, float ancho, float alto = -1f)
        {
            var go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            go.transform.SetParent(padre, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = posicion;
            float relacion = textura.height <= 0 ? 1f : textura.width / (float)textura.height;
            if (alto < 0f)
                alto = ancho / relacion;
            rect.sizeDelta = new Vector2(ancho, alto);
            var imagen = go.GetComponent<RawImage>();
            imagen.texture = textura;
            imagen.raycastTarget = false;
            return imagen;
        }

        Text Texto(Transform padre, string contenido, int tamano, Color color, FontStyle estilo)
        {
            var go = new GameObject("Texto", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(padre, false);
            var texto = go.GetComponent<Text>();
            texto.font = font;
            texto.text = contenido;
            texto.fontSize = tamano;
            texto.fontStyle = estilo;
            texto.color = color;
            texto.alignment = TextAnchor.MiddleCenter;
            texto.horizontalOverflow = HorizontalWrapMode.Overflow;
            texto.verticalOverflow = VerticalWrapMode.Overflow;
            texto.raycastTarget = false;
            texto.rectTransform.sizeDelta = new Vector2(320f, 48f);
            return texto;
        }

        static void Contorno(Text texto, Color color)
        {
            var contorno = texto.gameObject.AddComponent<Outline>();
            contorno.effectColor = color;
            contorno.effectDistance = new Vector2(1.4f, -1.4f);
        }

        static void Estirar(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static Color Hex(string valor)
        {
            ColorUtility.TryParseHtmlString(valor, out Color color);
            return color;
        }
    }
}
