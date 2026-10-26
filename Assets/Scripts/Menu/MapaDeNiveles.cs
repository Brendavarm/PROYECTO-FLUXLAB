using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FluxLab
{
    /// <summary>
    /// Camino de cinco talleres. Solo se entra al que ya está abierto.
    /// </summary>
    public class MapaDeNiveles : MonoBehaviour
    {
        static readonly Vector2[] Puntos =
        {
            new Vector2(-730f, -238f),
            new Vector2(-576f, 173f),
            new Vector2(-77f, -86f),
            new Vector2(211f, 108f),
            new Vector2(634f, 216f)
        };

        Font font;
        int elegido;
        readonly RectTransform[] marcas = new RectTransform[5];
        readonly Image[] placas = new Image[5];
        readonly Text[] etiquetas = new Text[5];
        Text nombreDetalle;
        Text cuerpoDetalle;
        Image botonEntrar;
        Text textoEntrar;
        RectTransform anillo;

        void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            Construir();
            elegido = Mathf.Clamp(AjustesJuego.NivelAlcanzado, 1, 5) - 1;
            Refrescar();
        }

        void Update()
        {
            if (anillo != null && marcas[elegido] != null)
            {
                anillo.anchoredPosition = marcas[elegido].anchoredPosition;
                float pulso = 1f + Mathf.Sin(Time.time * 2.4f) * 0.08f;
                anillo.localScale = new Vector3(pulso, pulso, 1f);
            }

            for (int i = 0; i < marcas.Length; i++)
            {
                if (marcas[i] == null)
                    continue;
                float bob = i == elegido ? Mathf.Sin(Time.time * 2f + i) * 5f : Mathf.Sin(Time.time * 1.3f + i) * 2f;
                marcas[i].anchoredPosition = Puntos[i] + new Vector2(0f, bob);
            }

            if (Input.GetKeyDown(KeyCode.Escape))
                Volver();
        }

        void Construir()
        {
            if (FindAnyObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            Transform raiz = canvasGo.transform;

            var fondo = ImagenRaw(raiz, "Mapa", Leer("Menu/mapa.jpg"), Vector2.zero, new Vector2(1920f, 1080f));
            Stretch(fondo.rectTransform);

            var titulo = Nivel3UI.Placa(raiz, "placa_titulo", new Vector2(0f, 448f), new Vector2(680f, 175f));
            var tituloTexto = Texto(titulo.transform, "Camino del taller", 42, Hex("#f6efe2"), FontStyle.Normal);
            tituloTexto.font = Nivel3UI.Fuente;
            tituloTexto.rectTransform.sizeDelta = new Vector2(520f, 90f);
            Contorno(tituloTexto, new Color(0.15f, 0.07f, 0.03f, 0.95f), 2.2f);

            var puntos = PlacaMadera(raiz, "Puntos", new Vector2(755f, 458f), new Vector2(310f, 108f));
            var puntosTexto = Texto(puntos.transform, "Puntos: " + AjustesJuego.MejorPuntaje + "\nAbiertos: " + AjustesJuego.NivelAlcanzado + " / 5", 22, Hex("#fff6ea"), FontStyle.Normal);
            puntosTexto.font = Nivel3UI.Fuente;
            puntosTexto.rectTransform.sizeDelta = new Vector2(270f, 78f);
            Contorno(puntosTexto, new Color(0.15f, 0.07f, 0.03f, 0.95f), 1.6f);

            anillo = Imagen(raiz, "Anillo", Disco(96), Puntos[0], new Vector2(120f, 120f)).rectTransform;
            anillo.GetComponent<Image>().color = new Color(1f, 0.86f, 0.45f, 0.55f);

            for (int i = 0; i < NivelesTaller.Lista.Length; i++)
            {
                var marca = new GameObject("Taller" + (i + 1), typeof(RectTransform));
                marca.transform.SetParent(raiz, false);
                marcas[i] = marca.GetComponent<RectTransform>();
                marcas[i].anchorMin = marcas[i].anchorMax = new Vector2(0.5f, 0.5f);
                marcas[i].pivot = new Vector2(0.5f, 0.5f);
                marcas[i].anchoredPosition = Puntos[i];
                marcas[i].sizeDelta = new Vector2(250f, 180f);

                placas[i] = PlacaMadera(marca.transform, "Placa", new Vector2(0f, -64f), new Vector2(236f, 82f));
                placas[i].raycastTarget = true;
                var boton = placas[i].gameObject.AddComponent<Button>();
                boton.targetGraphic = placas[i];
                boton.navigation = new Navigation { mode = Navigation.Mode.None };
                int capturado = i;
                boton.onClick.AddListener(() => Elegir(capturado));
                placas[i].gameObject.AddComponent<HoverScale>().scale = 1.06f;

                etiquetas[i] = Texto(placas[i].transform, NivelesTaller.Lista[i].corto, 28, Hex("#fff6ea"), FontStyle.Normal);
                etiquetas[i].font = Nivel3UI.Fuente;
                etiquetas[i].rectTransform.sizeDelta = new Vector2(200f, 64f);
                Contorno(etiquetas[i], new Color(0.15f, 0.07f, 0.03f, 0.95f), 1.8f);
            }

            var ficha = Nivel3UI.Panel(raiz, "Ficha", new Vector2(30f, -462f), new Vector2(980f, 190f));
            var sombraFicha = ficha.gameObject.AddComponent<Shadow>();
            sombraFicha.effectColor = new Color(0f, 0f, 0f, 0.4f);
            sombraFicha.effectDistance = new Vector2(0f, -10f);

            var placaNombre = PlacaMadera(ficha.transform, "Nombre", new Vector2(-175f, 42f), new Vector2(500f, 88f));
            nombreDetalle = Texto(placaNombre.transform, "", 32, Hex("#fff6ea"), FontStyle.Normal);
            nombreDetalle.font = Nivel3UI.Fuente;
            nombreDetalle.rectTransform.sizeDelta = new Vector2(450f, 70f);
            Contorno(nombreDetalle, new Color(0.15f, 0.07f, 0.03f, 0.95f), 1.8f);

            cuerpoDetalle = Texto(ficha.transform, "", 22, Hex("#5a3b24"), FontStyle.Normal);
            cuerpoDetalle.font = Nivel3UI.Fuente;
            cuerpoDetalle.alignment = TextAnchor.MiddleLeft;
            cuerpoDetalle.rectTransform.anchoredPosition = new Vector2(-175f, -52f);
            cuerpoDetalle.rectTransform.sizeDelta = new Vector2(500f, 72f);

            botonEntrar = PlacaMadera(ficha.transform, "Entrar", new Vector2(350f, 8f), new Vector2(230f, 90f));
            botonEntrar.raycastTarget = true;
            var entrar = botonEntrar.gameObject.AddComponent<Button>();
            entrar.targetGraphic = botonEntrar;
            entrar.navigation = new Navigation { mode = Navigation.Mode.None };
            entrar.onClick.AddListener(Entrar);
            botonEntrar.gameObject.AddComponent<HoverScale>().scale = 1.04f;
            textoEntrar = Texto(botonEntrar.transform, "Entrar", 30, Hex("#fff6ea"), FontStyle.Normal);
            textoEntrar.font = Nivel3UI.Fuente;
            textoEntrar.rectTransform.sizeDelta = new Vector2(190f, 70f);
            Contorno(textoEntrar, new Color(0.15f, 0.07f, 0.03f, 0.95f), 1.8f);

            var volver = PlacaMadera(raiz, "Volver", new Vector2(-800f, -462f), new Vector2(220f, 88f));
            volver.raycastTarget = true;
            var volverBoton = volver.gameObject.AddComponent<Button>();
            volverBoton.targetGraphic = volver;
            volverBoton.navigation = new Navigation { mode = Navigation.Mode.None };
            volverBoton.onClick.AddListener(Volver);
            volver.gameObject.AddComponent<HoverScale>().scale = 1.04f;
            var volverTexto = Texto(volver.transform, "Volver", 28, Hex("#fff6ea"), FontStyle.Normal);
            volverTexto.font = Nivel3UI.Fuente;
            volverTexto.rectTransform.sizeDelta = new Vector2(180f, 68f);
            Contorno(volverTexto, new Color(0.15f, 0.07f, 0.03f, 0.95f), 1.8f);

            PrepararMusica();
        }

        void Elegir(int indice)
        {
            elegido = indice;
            Refrescar();
        }

        void Refrescar()
        {
            Taller taller = NivelesTaller.Lista[elegido];
            bool abierto = elegido + 1 <= AjustesJuego.NivelAlcanzado;
            nombreDetalle.text = taller.nombre;
            cuerpoDetalle.text = taller.detalle + "\n" +
                taller.receta + " ingredientes  ·  " + taller.rondas + " rondas  ·  " +
                taller.segundos.ToString("0.0") + " s" +
                (abierto ? "" : "\nSe abre al terminar el taller anterior.");
            textoEntrar.text = abierto ? "Entrar" : "Cerrado";
            botonEntrar.color = abierto ? Color.white : new Color(0.65f, 0.65f, 0.65f, 1f);
            botonEntrar.GetComponent<Button>().interactable = abierto;

            if (anillo != null)
                anillo.anchoredPosition = Puntos[elegido];

            for (int i = 0; i < placas.Length; i++)
            {
                bool libre = i + 1 <= AjustesJuego.NivelAlcanzado;
                bool activo = i == elegido;
                placas[i].color = !libre
                    ? new Color(0.55f, 0.48f, 0.4f, 1f)
                    : activo ? new Color(1f, 0.84f, 0.46f, 1f) : Color.white;
                etiquetas[i].text = libre ? NivelesTaller.Lista[i].corto : "Nv. " + (i + 1);
                etiquetas[i].color = libre ? Hex("#fff6ea") : new Color(0.82f, 0.76f, 0.68f, 1f);
            }
        }

        void Entrar()
        {
            if (elegido + 1 > AjustesJuego.NivelAlcanzado)
                return;
            AjustesJuego.ElegirNivel(elegido + 1);
            SceneManager.LoadScene(elegido + 1 == 2 ? "Nivel2_Frascos" : "MesaDeAlquimia");
        }

        void Volver()
        {
            SceneManager.LoadScene("MenuPrincipal");
        }

        void PrepararMusica()
        {
            var clip = Resources.Load<AudioClip>("FluxLab/Audio/menu");
            if (clip == null)
                return;
            var fuente = gameObject.AddComponent<AudioSource>();
            fuente.clip = clip;
            fuente.loop = true;
            fuente.playOnAwake = false;
            fuente.volume = AjustesJuego.MusicaActiva ? 0.42f : 0f;
            fuente.Play();
        }

        RawImage ImagenRaw(Transform padre, string nombre, Texture2D textura, Vector2 posicion, Vector2 tamano)
        {
            var go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            go.transform.SetParent(padre, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = posicion;
            rect.sizeDelta = tamano;
            var imagen = go.GetComponent<RawImage>();
            imagen.texture = textura;
            imagen.raycastTarget = false;
            return imagen;
        }

        Image Imagen(Transform padre, string nombre, Sprite sprite, Vector2 posicion, Vector2 tamano)
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
            texto.horizontalOverflow = HorizontalWrapMode.Wrap;
            texto.verticalOverflow = VerticalWrapMode.Overflow;
            texto.raycastTarget = false;
            texto.rectTransform.sizeDelta = new Vector2(300f, 40f);
            return texto;
        }

        Image PlacaMadera(Transform padre, string nombre, Vector2 posicion, Vector2 tamano)
        {
            Sprite madera = Nivel3UI.SpriteUI("boton_ancho");
            var imagen = Imagen(padre, nombre, madera != null ? madera : SpriteRecurso("button_brown"), posicion, tamano);
            imagen.type = Image.Type.Sliced;
            if (madera != null)
                imagen.pixelsPerUnitMultiplier = tamano.y >= 100f ? 1.2f : 1.45f;
            var sombra = imagen.gameObject.AddComponent<Shadow>();
            sombra.effectColor = new Color(0.05f, 0.02f, 0.01f, 0.42f);
            sombra.effectDistance = new Vector2(0f, -5f);
            return imagen;
        }

        static void Contorno(Text texto, Color color, float grosor)
        {
            var contorno = texto.gameObject.AddComponent<Outline>();
            contorno.effectColor = color;
            contorno.effectDistance = new Vector2(grosor, -grosor);
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        Texture2D Leer(string relativo)
        {
            string ruta = Path.Combine(Application.streamingAssetsPath, relativo);
            var textura = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (File.Exists(ruta))
                textura.LoadImage(File.ReadAllBytes(ruta));
            textura.filterMode = FilterMode.Bilinear;
            return textura;
        }

        static Sprite SpriteRecurso(string nombre)
        {
            return Resources.Load<Sprite>("FluxLab/" + nombre);
        }

        static Sprite Disco(int lado)
        {
            var textura = new Texture2D(lado, lado, TextureFormat.RGBA32, false);
            float radio = (lado - 1) * 0.5f;
            var centro = new Vector2(radio, radio);
            for (int y = 0; y < lado; y++)
            {
                for (int x = 0; x < lado; x++)
                {
                    float distancia = Vector2.Distance(new Vector2(x, y), centro) / radio;
                    float alpha = Mathf.Clamp01(1.15f - distancia);
                    alpha *= alpha;
                    textura.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            textura.Apply();
            return Sprite.Create(textura, new Rect(0f, 0f, lado, lado), new Vector2(0.5f, 0.5f), 100f);
        }

        static Color Hex(string valor)
        {
            ColorUtility.TryParseHtmlString(valor, out Color color);
            return color;
        }
    }
}
