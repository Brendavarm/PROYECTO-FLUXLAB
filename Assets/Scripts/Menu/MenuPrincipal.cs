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
        RectTransform letreroTitulo;
        float letreroBaseY;

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

            if (letreroTitulo != null)
            {
                var posicion = letreroTitulo.anchoredPosition;
                posicion.y = letreroBaseY + Mathf.Sin(Time.time * 1.25f) * 5f;
                letreroTitulo.anchoredPosition = posicion;
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
            FondoAnimado.Aplicar(fondo);

            ConstruirTitulo(raiz);

            CrearBoton(raiz, "JUGAR", artes["estrella"], null, new Vector2(0f, 118f), 580f, 104f, true, IrAJugar);
            CrearBoton(raiz, "PERSONALIZAR", artes["icono_magnus"], null, new Vector2(0f, 4f), 540f, 90f, false, IrAPersonalizar);
            CrearBoton(raiz, "OPCIONES", artes["engranaje"], null, new Vector2(0f, -98f), 540f, 90f, false, AbrirOpciones);
            CrearBoton(raiz, "LOGROS", artes["trofeo"], artes["corona"], new Vector2(0f, -200f), 540f, 90f, false, AbrirLogros);
            CrearBoton(raiz, "SALIR", artes["puerta"], null, new Vector2(0f, -300f), 400f, 82f, false, Salir);

            ConstruirMarca(raiz);
            ConstruirSonidos(raiz);
            ConstruirPaneles(raiz);
            ConstruirFicha(raiz);
            PrepararAudio();
        }

        void ConstruirTitulo(Transform raiz)
        {
            var grupo = new GameObject("TituloJuego", typeof(RectTransform));
            grupo.transform.SetParent(raiz, false);
            letreroTitulo = grupo.GetComponent<RectTransform>();
            letreroTitulo.anchorMin = letreroTitulo.anchorMax = new Vector2(0.5f, 0.5f);
            letreroTitulo.pivot = new Vector2(0.5f, 0.5f);
            letreroBaseY = 372f;
            letreroTitulo.anchoredPosition = new Vector2(0f, letreroBaseY);

            var sombra = Imagen(grupo.transform, "Sombra", artes["titulo"], new Vector2(0f, -16f), 900f);
            sombra.color = new Color(0.07f, 0.03f, 0.02f, 0.42f);
            Imagen(grupo.transform, "Letrero", artes["titulo"], Vector2.zero, 900f);
        }

        void ConstruirMarca(Transform raiz)
        {
            var placa = PlacaMadera(raiz, "Marca", new Vector2(770f, 456f), new Vector2(340f, 112f));
            placa.raycastTarget = true;
            var tocarFicha = placa.gameObject.AddComponent<Button>();
            tocarFicha.targetGraphic = placa;
            tocarFicha.navigation = new Navigation { mode = Navigation.Mode.None };
            tocarFicha.onClick.AddListener(AbrirFichaDesdeMarca);
            placa.gameObject.AddComponent<HoverScale>().scale = 1.04f;

            var pocion = Imagen(placa.transform, "Pocion", artes["pocion"], new Vector2(-118f, 2f), 58f);
            pocion.raycastTarget = false;

            nombrePlaca = Texto(placa.transform, "", 22, Hex("#fff6ea"), FontStyle.Normal);
            nombrePlaca.font = Nivel3UI.Fuente;
            nombrePlaca.alignment = TextAnchor.MiddleLeft;
            nombrePlaca.horizontalOverflow = HorizontalWrapMode.Overflow;
            nombrePlaca.rectTransform.anchoredPosition = new Vector2(28f, 18f);
            nombrePlaca.rectTransform.sizeDelta = new Vector2(180f, 34f);
            Contorno(nombrePlaca, new Color(0.16f, 0.07f, 0.03f, 0.95f));
            RefrescarMarca();

            puntajeTexto = Texto(placa.transform, AjustesJuego.MejorPuntaje.ToString(), 26, Hex("#ffe08a"), FontStyle.Normal);
            puntajeTexto.font = Nivel3UI.Fuente;
            puntajeTexto.alignment = TextAnchor.MiddleLeft;
            puntajeTexto.rectTransform.anchoredPosition = new Vector2(28f, -18f);
            puntajeTexto.rectTransform.sizeDelta = new Vector2(160f, 34f);
            Contorno(puntajeTexto, new Color(0.16f, 0.07f, 0.03f, 0.95f));
        }

        void ConstruirSonidos(Transform raiz)
        {
            iconoSonido = BotonIcono(raiz, "Sonido", artes["sonido"], "SONIDO", new Vector2(690f, -440f), AlternarSonido);
            iconoMusica = BotonIcono(raiz, "Musica", artes["musica"], "MÚSICA", new Vector2(840f, -440f), AlternarMusica);
            RefrescarIconos();
        }

        RawImage BotonIcono(Transform padre, string nombre, Texture2D icono, string etiqueta, Vector2 posicion, UnityEngine.Events.UnityAction accion)
        {
            var marco = PlacaRedonda(padre, nombre, posicion, 108f);
            marco.raycastTarget = true;
            var boton = marco.gameObject.AddComponent<Button>();
            boton.targetGraphic = marco;
            boton.navigation = new Navigation { mode = Navigation.Mode.None };
            boton.onClick.AddListener(accion);
            marco.gameObject.AddComponent<HoverScale>().scale = 1.06f;

            var imagen = Imagen(marco.transform, "Icono", icono, Vector2.zero, 58f);
            imagen.raycastTarget = false;

            var texto = Texto(marco.transform, etiqueta, 18, Hex("#fff6ea"), FontStyle.Normal);
            texto.font = Nivel3UI.Fuente;
            texto.rectTransform.anchoredPosition = new Vector2(0f, -74f);
            texto.rectTransform.sizeDelta = new Vector2(150f, 28f);
            Contorno(texto, new Color(0.1f, 0.05f, 0.03f, 0.9f));
            return imagen;
        }

        void ConstruirPaneles(Transform raiz)
        {
            panelOpciones = CrearPanel(raiz, "PanelOpciones", "Opciones", "La música y los efectos se guardan en este equipo.", CerrarPaneles);
            panelLogros = CrearPanel(raiz, "PanelLogros", "Logros", "", CerrarPaneles);
            textoLogros = Texto(panelLogros.transform.GetChild(1), "", 30, Hex("#5a3b24"), FontStyle.Normal);
            textoLogros.rectTransform.anchoredPosition = new Vector2(0f, 16f);
            textoLogros.rectTransform.sizeDelta = new Vector2(640f, 160f);
            panelOpciones.SetActive(false);
            panelLogros.SetActive(false);
        }

        GameObject CrearPanel(Transform padre, string nombre, string titulo, string cuerpo, UnityEngine.Events.UnityAction cerrar)
        {
            var raiz = new GameObject(nombre, typeof(RectTransform));
            raiz.transform.SetParent(padre, false);
            Estirar(raiz.GetComponent<RectTransform>());

            var velo = new GameObject("Velo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            velo.transform.SetParent(raiz.transform, false);
            var veloImagen = velo.GetComponent<Image>();
            veloImagen.color = new Color(0.05f, 0.03f, 0.02f, 0.72f);
            veloImagen.raycastTarget = true;
            Estirar(velo.GetComponent<RectTransform>());

            var tarjeta = Nivel3UI.Panel(raiz.transform, "Tarjeta", new Vector2(0f, -40f), new Vector2(820f, 480f));
            var sombra = tarjeta.gameObject.AddComponent<Shadow>();
            sombra.effectColor = new Color(0f, 0f, 0f, 0.4f);
            sombra.effectDistance = new Vector2(0f, -12f);

            var placa = Nivel3UI.Placa(tarjeta.transform, "placa_titulo", new Vector2(0f, 176f), new Vector2(420f, 120f));
            Nivel3UI.Texto(placa.transform, titulo, 40, Hex("#f6efe2"), Vector2.zero, new Vector2(320f, 70f), TextAnchor.MiddleCenter, true);

            if (!string.IsNullOrEmpty(cuerpo))
            {
                var cuerpoTexto = Texto(tarjeta.transform, cuerpo, 28, Hex("#5a3b24"), FontStyle.Normal);
                cuerpoTexto.rectTransform.anchoredPosition = new Vector2(0f, 30f);
                cuerpoTexto.rectTransform.sizeDelta = new Vector2(640f, 120f);
            }

            BotonMadera(tarjeta.transform, "Cerrar", new Vector2(0f, -160f), new Vector2(300f, 78f), 28, cerrar);
            return raiz;
        }

        Image PlacaMadera(Transform padre, string nombre, Vector2 posicion, Vector2 tamano)
        {
            Sprite madera = Nivel3UI.SpriteUI("boton_ancho");
            var go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(padre, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = posicion;
            rect.sizeDelta = tamano;
            var imagen = go.GetComponent<Image>();
            imagen.sprite = madera != null ? madera : Resources.Load<Sprite>("FluxLab/button_brown");
            imagen.type = Image.Type.Sliced;
            if (madera != null)
                imagen.pixelsPerUnitMultiplier = tamano.y >= 100f ? 1.2f : 1.5f;
            var sombra = go.AddComponent<Shadow>();
            sombra.effectColor = new Color(0.05f, 0.02f, 0.01f, 0.4f);
            sombra.effectDistance = new Vector2(0f, -6f);
            return imagen;
        }

        Image PlacaRedonda(Transform padre, string nombre, Vector2 posicion, float lado)
        {
            Sprite redonda = Nivel3UI.SpriteUI("boton_cuadrado");
            var go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(padre, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = posicion;
            rect.sizeDelta = new Vector2(lado, lado);
            var imagen = go.GetComponent<Image>();
            imagen.sprite = redonda != null ? redonda : Resources.Load<Sprite>("FluxLab/button_brown");
            imagen.preserveAspect = redonda != null;
            var sombra = go.AddComponent<Shadow>();
            sombra.effectColor = new Color(0.05f, 0.02f, 0.01f, 0.4f);
            sombra.effectDistance = new Vector2(0f, -5f);
            return imagen;
        }

        void CrearBoton(Transform padre, string etiqueta, Texture2D iconoIzquierdo, Texture2D iconoDerecho, Vector2 posicion, float ancho, float alto, bool brillar, UnityEngine.Events.UnityAction accion)
        {
            var placa = PlacaMadera(padre, etiqueta, posicion, new Vector2(ancho, alto));
            placa.raycastTarget = true;
            if (brillar)
                placa.color = new Color(1f, 0.84f, 0.46f, 1f);

            var boton = placa.gameObject.AddComponent<Button>();
            boton.targetGraphic = placa;
            boton.navigation = new Navigation { mode = Navigation.Mode.None };
            var colores = boton.colors;
            colores.highlightedColor = new Color(1f, 0.96f, 0.84f, 1f);
            colores.pressedColor = new Color(0.78f, 0.64f, 0.46f, 1f);
            colores.fadeDuration = 0.06f;
            boton.colors = colores;
            boton.onClick.AddListener(() =>
            {
                Sonar("click");
                accion();
            });
            placa.gameObject.AddComponent<HoverScale>().scale = brillar ? 1.045f : 1.035f;

            float margen = 68f;
            if (iconoIzquierdo != null)
            {
                float lado = brillar ? 56f : 46f;
                var icono = Imagen(placa.transform, "Icono", iconoIzquierdo, new Vector2(-ancho * 0.5f + margen, 2f), lado);
                icono.raycastTarget = false;
            }

            if (iconoDerecho != null)
            {
                var icono = Imagen(placa.transform, "IconoDerecho", iconoDerecho, new Vector2(ancho * 0.5f - margen, 2f), 44f);
                icono.raycastTarget = false;
            }

            bool larga = etiqueta.Length > 9;
            int tamano = brillar ? 40 : (larga ? 28 : 34);
            float anchoTexto = ancho - 150f - (iconoDerecho != null ? 40f : 0f);
            var texto = Texto(placa.transform, etiqueta, tamano, Hex("#fff6ea"), FontStyle.Normal);
            texto.font = Nivel3UI.Fuente;
            texto.rectTransform.anchoredPosition = new Vector2(iconoIzquierdo != null && iconoDerecho == null ? 18f : 0f, 2f);
            texto.rectTransform.sizeDelta = new Vector2(anchoTexto, alto - 12f);
            Contorno(texto, new Color(0.15f, 0.07f, 0.03f, 0.95f));
            texto.GetComponent<Outline>().effectDistance = new Vector2(2f, -2f);
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
            Sprite beige = Resources.Load<Sprite>("FluxLab/button_beige");

            panelFicha = new GameObject("Ficha", typeof(RectTransform));
            panelFicha.transform.SetParent(raiz, false);
            Estirar(panelFicha.GetComponent<RectTransform>());

            var velo = new GameObject("Velo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            velo.transform.SetParent(panelFicha.transform, false);
            var veloImagen = velo.GetComponent<Image>();
            veloImagen.color = new Color(0.05f, 0.03f, 0.02f, 0.72f);
            veloImagen.raycastTarget = true;
            Estirar(velo.GetComponent<RectTransform>());

            var tarjeta = Nivel3UI.Panel(panelFicha.transform, "Tarjeta", new Vector2(0f, -165f), new Vector2(940f, 700f));
            var sombra = tarjeta.gameObject.AddComponent<Shadow>();
            sombra.effectColor = new Color(0f, 0f, 0f, 0.45f);
            sombra.effectDistance = new Vector2(0f, -14f);

            var placa = Nivel3UI.Placa(tarjeta.transform, "placa_titulo", new Vector2(0f, 268f), new Vector2(560f, 156f));
            Nivel3UI.Texto(placa.transform, "Tu ficha", 48, Hex("#f6efe2"), Vector2.zero, new Vector2(420f, 90f), TextAnchor.MiddleCenter, true);

            var cuerpo = Texto(tarjeta.transform, "No eres Magnus. Eres quien lo acompaña\npara que llegue a ser el mejor alquimista.", 26, Hex("#5a3b24"), FontStyle.Normal);
            cuerpo.rectTransform.anchoredPosition = new Vector2(0f, 150f);
            cuerpo.rectTransform.sizeDelta = new Vector2(720f, 80f);

            var etiqueta = Texto(tarjeta.transform, "Tu nombre", 24, Hex("#8a5a32"), FontStyle.Bold);
            etiqueta.rectTransform.anchoredPosition = new Vector2(0f, 88f);
            etiqueta.rectTransform.sizeDelta = new Vector2(640f, 34f);

            var bandeja = ImagenSprite(tarjeta.transform, "BandejaNombre", Nivel3UI.SpriteUI("boton_ancho"), new Vector2(0f, 22f), new Vector2(700f, 108f));
            if (bandeja != null)
            {
                bandeja.type = Image.Type.Sliced;
                bandeja.pixelsPerUnitMultiplier = 1.5f;
            }

            campoNombre = CrearCampo(tarjeta.transform, beige);
            campoNombre.text = AjustesJuego.NombreAprendiz;

            fondoAprendiz = BotonMadera(tarjeta.transform, "Aprendiz", new Vector2(-175f, -88f), new Vector2(300f, 88f), 30, () => ElegirGenero(false));
            fondoAprendiza = BotonMadera(tarjeta.transform, "Aprendiza", new Vector2(175f, -88f), new Vector2(300f, 88f), 30, () => ElegirGenero(true));
            eleccionAprendiza = AjustesJuego.EsAprendiza;
            PintarGenero();

            avisoFicha = Texto(tarjeta.transform, "", 24, Hex("#a33b2b"), FontStyle.Bold);
            avisoFicha.rectTransform.anchoredPosition = new Vector2(0f, -148f);
            avisoFicha.rectTransform.sizeDelta = new Vector2(700f, 36f);

            BotonMadera(tarjeta.transform, "Entrar al laboratorio", new Vector2(0f, -222f), new Vector2(520f, 92f), 32, ConfirmarFicha);
            BotonMadera(tarjeta.transform, "Ahora no", new Vector2(0f, -308f), new Vector2(280f, 70f), 26, () => panelFicha.SetActive(false));
            panelFicha.SetActive(false);
        }

        Image ImagenSprite(Transform padre, string nombre, Sprite sprite, Vector2 posicion, Vector2 tamano)
        {
            if (sprite == null)
                return null;
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

        InputField CrearCampo(Transform padre, Sprite fondo)
        {
            var go = new GameObject("Campo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(InputField));
            go.transform.SetParent(padre, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(620f, 68f);
            rect.anchoredPosition = new Vector2(0f, 22f);
            var imagen = go.GetComponent<Image>();
            imagen.sprite = fondo;
            imagen.type = Image.Type.Sliced;
            imagen.color = new Color(1f, 0.97f, 0.9f, 1f);

            var texto = Texto(go.transform, "", 32, Hex("#3b2a22"), FontStyle.Bold);
            texto.alignment = TextAnchor.MiddleCenter;
            texto.rectTransform.anchorMin = Vector2.zero;
            texto.rectTransform.anchorMax = Vector2.one;
            texto.rectTransform.offsetMin = new Vector2(22f, 8f);
            texto.rectTransform.offsetMax = new Vector2(-22f, -8f);
            texto.supportRichText = false;

            var pista = Texto(go.transform, "Escribe tu nombre", 28, Hex("#b08968"), FontStyle.Normal);
            pista.alignment = TextAnchor.MiddleCenter;
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

        Image BotonMadera(Transform padre, string etiqueta, Vector2 posicion, Vector2 tamano, int tamanoLetra, UnityEngine.Events.UnityAction accion)
        {
            Sprite madera = Nivel3UI.SpriteUI("boton_ancho");
            var go = new GameObject(etiqueta, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(padre, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = tamano;
            rect.anchoredPosition = posicion;
            var imagen = go.GetComponent<Image>();
            imagen.sprite = madera != null ? madera : Resources.Load<Sprite>("FluxLab/button_brown");
            imagen.type = Image.Type.Sliced;
            if (madera != null)
                imagen.pixelsPerUnitMultiplier = tamano.y < 82f ? 1.7f : 1.25f;

            var boton = go.GetComponent<Button>();
            boton.targetGraphic = imagen;
            boton.navigation = new Navigation { mode = Navigation.Mode.None };
            var colores = boton.colors;
            colores.highlightedColor = new Color(1f, 0.96f, 0.84f, 1f);
            colores.pressedColor = new Color(0.78f, 0.64f, 0.46f, 1f);
            colores.fadeDuration = 0.06f;
            boton.colors = colores;
            boton.onClick.AddListener(() =>
            {
                Sonar("click");
                accion();
            });
            go.AddComponent<HoverScale>().scale = 1.035f;

            var texto = Texto(go.transform, etiqueta, tamanoLetra, Hex("#fff6ea"), FontStyle.Bold);
            texto.rectTransform.sizeDelta = new Vector2(tamano.x - 36f, tamano.y);
            Contorno(texto, new Color(0.16f, 0.07f, 0.03f, 0.95f));
            texto.GetComponent<Outline>().effectDistance = new Vector2(1.8f, -1.8f);
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
                fondoAprendiz.color = eleccionAprendiza ? new Color(0.62f, 0.54f, 0.44f, 1f) : new Color(1f, 0.84f, 0.48f, 1f);
            if (fondoAprendiza != null)
                fondoAprendiza.color = eleccionAprendiza ? new Color(1f, 0.84f, 0.48f, 1f) : new Color(0.62f, 0.54f, 0.44f, 1f);
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
