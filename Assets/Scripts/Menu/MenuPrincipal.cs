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
        Text nombrePlaca;
        GameObject panelFicha;
        InputField campoNombre;
        Text avisoFicha;
        Image fondoAprendiz;
        Image fondoAprendiza;
        bool eleccionAprendiza;
        bool entrarAlGuardar;
        RawImage iconoSonido;
        RawImage iconoMusica;
        GameObject panelOpciones;
        GameObject panelLogros;
        Text textoLogros;

        readonly System.Collections.Generic.Dictionary<string, Texture2D> artes = new System.Collections.Generic.Dictionary<string, Texture2D>();

        void Awake()
        {
            font = Resources.Load<Font>("FluxLab/Nunito-Bold");
            if (font == null)
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
                if (panelFicha != null && panelFicha.activeSelf)
                {
                    panelFicha.SetActive(false);
                    return;
                }
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
            ConstruirFicha(raiz);
            PrepararAudio();
        }

        void ConstruirMarca(Transform raiz)
        {
            var placa = Imagen(raiz, "Marca", artes["boton"], new Vector2(735f, 452f), 340f);
            var pocion = Imagen(placa.transform, "Pocion", artes["pocion"], new Vector2(-108f, 2f), 62f);
            pocion.rectTransform.anchorMin = pocion.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);

            placa.raycastTarget = true;
            var tocarFicha = placa.gameObject.AddComponent<Button>();
            tocarFicha.targetGraphic = placa;
            tocarFicha.onClick.AddListener(AbrirFichaDesdeMarca);

            nombrePlaca = Texto(placa.transform, "", 22, Hex("#fff4e4"), FontStyle.Bold);
            nombrePlaca.alignment = TextAnchor.MiddleLeft;
            nombrePlaca.horizontalOverflow = HorizontalWrapMode.Wrap;
            nombrePlaca.rectTransform.anchoredPosition = new Vector2(28f, 16f);
            nombrePlaca.rectTransform.sizeDelta = new Vector2(200f, 34f);
            Contorno(nombrePlaca, new Color(0.25f, 0.12f, 0.05f, 0.9f));
            RefrescarMarca();

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
            if (!AjustesJuego.TieneFicha)
            {
                entrarAlGuardar = true;
                AbrirFicha();
                return;
            }

            SceneManager.LoadScene("MapaNiveles");
        }

        void AbrirFichaDesdeMarca()
        {
            entrarAlGuardar = false;
            AbrirFicha();
        }

        void ConstruirFicha(Transform raiz)
        {
            Sprite panel = Resources.Load<Sprite>("FluxLab/panel_beige");
            Sprite beige = Resources.Load<Sprite>("FluxLab/button_beige");
            Sprite marron = Resources.Load<Sprite>("FluxLab/button_brown");

            panelFicha = new GameObject("Ficha", typeof(RectTransform));
            panelFicha.transform.SetParent(raiz, false);
            Estirar(panelFicha.GetComponent<RectTransform>());

            var velo = new GameObject("Velo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            velo.transform.SetParent(panelFicha.transform, false);
            var veloImagen = velo.GetComponent<Image>();
            veloImagen.color = new Color(0.08f, 0.05f, 0.04f, 0.62f);
            veloImagen.raycastTarget = true;
            Estirar(velo.GetComponent<RectTransform>());

            var tarjetaGo = new GameObject("Tarjeta", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            tarjetaGo.transform.SetParent(panelFicha.transform, false);
            var tarjeta = tarjetaGo.GetComponent<RectTransform>();
            tarjeta.anchorMin = tarjeta.anchorMax = new Vector2(0.5f, 0.5f);
            tarjeta.sizeDelta = new Vector2(860f, 620f);
            var imagen = tarjetaGo.GetComponent<Image>();
            imagen.sprite = panel;
            imagen.type = Image.Type.Sliced;
            imagen.color = new Color(1f, 0.97f, 0.92f, 0.98f);

            var titulo = Texto(tarjeta, "Tu ficha", 46, Hex("#5a3b24"), FontStyle.Normal);
            titulo.rectTransform.anchoredPosition = new Vector2(0f, 230f);
            titulo.rectTransform.sizeDelta = new Vector2(760f, 64f);

            var cuerpo = Texto(tarjeta, "No eres Magnus. Eres quien lo acompaña\npara que llegue a ser el mejor alquimista.", 28, Hex("#5a3b24"), FontStyle.Normal);
            cuerpo.rectTransform.anchoredPosition = new Vector2(0f, 145f);
            cuerpo.rectTransform.sizeDelta = new Vector2(740f, 90f);

            var etiqueta = Texto(tarjeta, "Nombre", 26, Hex("#8a6848"), FontStyle.Normal);
            etiqueta.alignment = TextAnchor.MiddleLeft;
            etiqueta.rectTransform.anchoredPosition = new Vector2(-250f, 70f);
            etiqueta.rectTransform.sizeDelta = new Vector2(240f, 36f);

            campoNombre = CrearCampo(tarjeta, beige);
            campoNombre.text = AjustesJuego.NombreAprendiz;

            fondoAprendiz = BotonGenero(tarjeta, beige, "Aprendiz", new Vector2(-160f, -30f), false);
            fondoAprendiza = BotonGenero(tarjeta, beige, "Aprendiza", new Vector2(160f, -30f), true);
            eleccionAprendiza = AjustesJuego.EsAprendiza;
            PintarGenero();

            avisoFicha = Texto(tarjeta, "", 24, Hex("#a33b2b"), FontStyle.Normal);
            avisoFicha.rectTransform.anchoredPosition = new Vector2(0f, -110f);
            avisoFicha.rectTransform.sizeDelta = new Vector2(700f, 40f);

            BotonFicha(tarjeta, marron, "Entrar al laboratorio", new Vector2(0f, -190f), new Vector2(460f, 78f), Hex("#f6efe4"), ConfirmarFicha);
            BotonFicha(tarjeta, beige, "Ahora no", new Vector2(0f, -270f), new Vector2(240f, 58f), Hex("#5a3b24"), () => panelFicha.SetActive(false));
            panelFicha.SetActive(false);
        }

        InputField CrearCampo(Transform padre, Sprite fondo)
        {
            var go = new GameObject("Campo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(InputField));
            go.transform.SetParent(padre, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(620f, 72f);
            rect.anchoredPosition = new Vector2(0f, 10f);
            var imagen = go.GetComponent<Image>();
            imagen.sprite = fondo;
            imagen.type = Image.Type.Sliced;
            imagen.color = new Color(1f, 0.98f, 0.94f, 1f);

            var texto = Texto(go.transform, "", 32, Hex("#3b2a22"), FontStyle.Normal);
            texto.alignment = TextAnchor.MiddleLeft;
            texto.rectTransform.anchorMin = Vector2.zero;
            texto.rectTransform.anchorMax = Vector2.one;
            texto.rectTransform.offsetMin = new Vector2(22f, 8f);
            texto.rectTransform.offsetMax = new Vector2(-22f, -8f);
            texto.supportRichText = false;

            var pista = Texto(go.transform, "Escribe tu nombre", 32, Hex("#b08968"), FontStyle.Normal);
            pista.alignment = TextAnchor.MiddleLeft;
            pista.fontStyle = FontStyle.Italic;
            pista.rectTransform.anchorMin = Vector2.zero;
            pista.rectTransform.anchorMax = Vector2.one;
            pista.rectTransform.offsetMin = new Vector2(22f, 8f);
            pista.rectTransform.offsetMax = new Vector2(-22f, -8f);

            var campo = go.GetComponent<InputField>();
            campo.textComponent = texto;
            campo.placeholder = pista;
            campo.characterLimit = 18;
            campo.lineType = InputField.LineType.SingleLine;
            return campo;
        }

        Image BotonGenero(Transform padre, Sprite sprite, string etiqueta, Vector2 posicion, bool aprendiza)
        {
            var imagen = BotonFicha(padre, sprite, etiqueta, posicion, new Vector2(260f, 70f), Hex("#5a3b24"), () => ElegirGenero(aprendiza));
            return imagen;
        }

        void ElegirGenero(bool aprendiza)
        {
            eleccionAprendiza = aprendiza;
            PintarGenero();
        }

        void PintarGenero()
        {
            if (fondoAprendiz != null)
                fondoAprendiz.color = eleccionAprendiza ? new Color(1f, 0.96f, 0.9f, 1f) : Hex("#e7b15a");
            if (fondoAprendiza != null)
                fondoAprendiza.color = eleccionAprendiza ? Hex("#e7b15a") : new Color(1f, 0.96f, 0.9f, 1f);
        }

        Image BotonFicha(Transform padre, Sprite sprite, string etiqueta, Vector2 posicion, Vector2 tamano, Color colorTexto, UnityEngine.Events.UnityAction accion)
        {
            var go = new GameObject(etiqueta, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(padre, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = tamano;
            rect.anchoredPosition = posicion;
            var imagen = go.GetComponent<Image>();
            imagen.sprite = sprite;
            imagen.type = Image.Type.Sliced;
            var boton = go.GetComponent<Button>();
            boton.targetGraphic = imagen;
            boton.onClick.AddListener(accion);
            var texto = Texto(go.transform, etiqueta, 30, colorTexto, FontStyle.Normal);
            texto.rectTransform.sizeDelta = tamano;
            return imagen;
        }

        void AbrirFicha()
        {
            CerrarPaneles();
            eleccionAprendiza = AjustesJuego.EsAprendiza;
            if (campoNombre != null)
                campoNombre.text = AjustesJuego.NombreAprendiz;
            if (avisoFicha != null)
                avisoFicha.text = "";
            PintarGenero();
            panelFicha.SetActive(true);
        }

        void ConfirmarFicha()
        {
            string nombre = campoNombre != null ? campoNombre.text.Trim() : "";
            if (nombre.Length < 2)
            {
                if (avisoFicha != null)
                    avisoFicha.text = "Escribe un nombre de al menos dos letras.";
                return;
            }

            AjustesJuego.NombreAprendiz = nombre;
            AjustesJuego.EsAprendiza = eleccionAprendiza;
            RefrescarMarca();
            if (entrarAlGuardar)
            {
                SceneManager.LoadScene("MapaNiveles");
                return;
            }

            panelFicha.SetActive(false);
        }

        void RefrescarMarca()
        {
            if (nombrePlaca == null)
                return;
            nombrePlaca.text = AjustesJuego.TieneFicha ? AjustesJuego.NombreAprendiz : "Tu ficha";
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
