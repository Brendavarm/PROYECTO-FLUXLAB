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
            new Vector2(-640f, -200f),
            new Vector2(-500f, 230f),
            new Vector2(-20f, 80f),
            new Vector2(350f, 150f),
            new Vector2(690f, 210f)
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

            var titulo = Imagen(raiz, "Titulo", SpriteRecurso("panel_beige"), new Vector2(0f, 478f), new Vector2(520f, 78f));
            titulo.type = Image.Type.Sliced;
            var tituloTexto = Texto(titulo.transform, "Camino del taller", 32, Hex("#5a3b24"), FontStyle.Bold);
            tituloTexto.rectTransform.sizeDelta = new Vector2(480f, 60f);

            var puntos = Imagen(raiz, "Puntos", SpriteRecurso("panel_beige"), new Vector2(760f, 478f), new Vector2(280f, 86f));
            puntos.type = Image.Type.Sliced;
            var puntosTexto = Texto(puntos.transform, "Puntos: " + AjustesJuego.MejorPuntaje + "\nAbiertos: " + AjustesJuego.NivelAlcanzado + " / 5", 20, Hex("#5a3b24"), FontStyle.Bold);
            puntosTexto.rectTransform.sizeDelta = new Vector2(250f, 70f);

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
                marcas[i].sizeDelta = new Vector2(190f, 150f);

                placas[i] = Imagen(marca.transform, "Placa", SpriteRecurso("panel_beige"), new Vector2(0f, -62f), new Vector2(176f, 54f));
                placas[i].type = Image.Type.Sliced;
                placas[i].raycastTarget = true;
                var boton = placas[i].gameObject.AddComponent<Button>();
                boton.targetGraphic = placas[i];
                boton.navigation = new Navigation { mode = Navigation.Mode.None };
                int capturado = i;
                boton.onClick.AddListener(() => Elegir(capturado));
                placas[i].gameObject.AddComponent<HoverScale>().scale = 1.06f;

                etiquetas[i] = Texto(placas[i].transform, NivelesTaller.Lista[i].corto, 20, Hex("#5a3b24"), FontStyle.Bold);
                etiquetas[i].rectTransform.sizeDelta = new Vector2(160f, 40f);
            }

            var ficha = Imagen(raiz, "Ficha", SpriteRecurso("panel_beige"), new Vector2(40f, -458f), new Vector2(860f, 168f));
            ficha.type = Image.Type.Sliced;
            nombreDetalle = Texto(ficha.transform, "", 30, Hex("#5a3b24"), FontStyle.Bold);
            nombreDetalle.alignment = TextAnchor.MiddleLeft;
            nombreDetalle.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            nombreDetalle.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            nombreDetalle.rectTransform.pivot = new Vector2(0f, 0.5f);
            nombreDetalle.rectTransform.anchoredPosition = new Vector2(28f, 42f);
            nombreDetalle.rectTransform.sizeDelta = new Vector2(560f, 40f);

            cuerpoDetalle = Texto(ficha.transform, "", 20, Hex("#6a4b32"), FontStyle.Normal);
            cuerpoDetalle.alignment = TextAnchor.UpperLeft;
            cuerpoDetalle.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            cuerpoDetalle.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            cuerpoDetalle.rectTransform.pivot = new Vector2(0f, 1f);
            cuerpoDetalle.rectTransform.anchoredPosition = new Vector2(28f, 16f);
            cuerpoDetalle.rectTransform.sizeDelta = new Vector2(560f, 90f);

            botonEntrar = Imagen(ficha.transform, "Entrar", SpriteRecurso("button_brown"), new Vector2(320f, 0f), new Vector2(190f, 72f));
            botonEntrar.type = Image.Type.Sliced;
            botonEntrar.raycastTarget = true;
            var entrar = botonEntrar.gameObject.AddComponent<Button>();
            entrar.targetGraphic = botonEntrar;
            entrar.onClick.AddListener(Entrar);
            textoEntrar = Texto(botonEntrar.transform, "Entrar", 26, Hex("#f6efe4"), FontStyle.Bold);
            Stretch(textoEntrar.rectTransform);

            var volver = Imagen(raiz, "Volver", SpriteRecurso("button_brown"), new Vector2(-800f, -458f), new Vector2(200f, 72f));
            volver.type = Image.Type.Sliced;
            volver.raycastTarget = true;
            var volverBoton = volver.gameObject.AddComponent<Button>();
            volverBoton.targetGraphic = volver;
            volverBoton.onClick.AddListener(Volver);
            var volverTexto = Texto(volver.transform, "Volver", 26, Hex("#f6efe4"), FontStyle.Bold);
            Stretch(volverTexto.rectTransform);

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
                placas[i].color = !libre ? Hex("#d5cfc4") : activo ? Hex("#fff1c2") : Hex("#f4ead7");
                etiquetas[i].text = libre ? NivelesTaller.Lista[i].corto : "Nv. " + (i + 1);
                etiquetas[i].color = libre ? Hex("#5a3b24") : Hex("#7a736a");
            }
        }

        void Entrar()
        {
            if (elegido + 1 > AjustesJuego.NivelAlcanzado)
                return;
            AjustesJuego.ElegirNivel(elegido + 1);
            SceneManager.LoadScene("MesaDeAlquimia");
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
