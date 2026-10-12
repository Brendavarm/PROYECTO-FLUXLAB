using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FluxLab.Nivel2
{
    /// <summary>
    /// Escena de prueba (Assets/Scenes/Nivel2_GaleriaMagnus.unity): un botón por cada animación de Magnus
    /// y controles para probar pelo, túnica y gafas. Sirve para ver y aprobar las animaciones
    /// sin jugar el nivel. No guarda nada: la personalización de aquí es solo para mirar.
    /// </summary>
    public class GaleriaMagnus : MonoBehaviour
    {
        static readonly string[] NombresPelo = { "Clásico", "Castaño", "Rubio", "Rojizo", "Canoso" };
        static readonly string[] NombresTunica = { "Oscuro original", "Rojo rubí", "Azul zafiro", "Amarillo estrella", "Verde esmeralda", "Naranja ámbar" };

        Font fuente;
        MagnusAnimado magnus;
        PersonalizacionMagnus aspecto;
        Text textoAnimacion;
        Text textoPelo;
        Text textoTunica;
        Text textoGafas;

        void Awake()
        {
            fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (FindAnyObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var lienzo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            lienzo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var escalador = lienzo.GetComponent<CanvasScaler>();
            escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escalador.referenceResolution = new Vector2(1920f, 1080f);
            escalador.matchWidthOrHeight = 0.5f;
            Transform raiz = lienzo.transform;

            var fondo = new GameObject("Fondo", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RawImage>();
            fondo.transform.SetParent(raiz, false);
            fondo.texture = FondoNivel2.Cargar();
            Estirar(fondo.rectTransform);
            var velo = Panel(raiz, Vector2.zero, Vector2.zero, new Color(0.05f, 0.03f, 0.08f, 0.45f));
            Estirar(velo.rectTransform);

            var titulo = Texto(raiz, "Galería de Magnus", 54, new Vector2(0f, 480f), new Vector2(900f, 70f));
            titulo.color = new Color(1f, 0.88f, 0.5f, 1f);
            Texto(raiz, "Solo para probar: no se guarda nada", 22, new Vector2(0f, 430f), new Vector2(900f, 30f));

            aspecto = PersonalizacionMagnus.Guardada;
            var tarima = Panel(raiz, new Vector2(0f, -330f), new Vector2(360f, 40f), new Color(0f, 0f, 0f, 0.25f));
            tarima.sprite = FormasUI.Humo;
            magnus = MagnusAnimado.Crear(raiz, new Vector2(0f, -330f), 460f, aspecto, AjustesJuego.Accesorio == 1);
            textoAnimacion = Texto(raiz, "Quieto", 34, new Vector2(0f, -400f), new Vector2(700f, 50f));

            ConstruirBotonesAnimacion(raiz);
            ConstruirPersonalizacion(raiz);
        }

        void ConstruirBotonesAnimacion(Transform raiz)
        {
            var animaciones = (AnimacionMagnus[])System.Enum.GetValues(typeof(AnimacionMagnus));
            for (int i = 0; i < animaciones.Length; i++)
            {
                AnimacionMagnus animacion = animaciones[i];
                int columna = i % 2;
                int fila = i / 2;
                Vector2 posicion = new Vector2(-820f + columna * 250f, 360f - fila * 82f);
                Boton(raiz, animacion.ToString(), posicion, new Vector2(234f, 70f), () => Probar(animacion));
            }
        }

        void Probar(AnimacionMagnus animacion)
        {
            textoAnimacion.text = animacion.ToString();
            if (animacion == AnimacionMagnus.Volar)
            {
                magnus.Volar(new Vector2(-420f, -120f), new Vector2(0f, -330f));
                return;
            }

            bool larga = animacion == AnimacionMagnus.Bailar || animacion == AnimacionMagnus.Cantar ||
                         animacion == AnimacionMagnus.Meditar || animacion == AnimacionMagnus.Triste ||
                         animacion == AnimacionMagnus.Observar || animacion == AnimacionMagnus.Quieto ||
                         animacion.ToString().StartsWith("Final");
            magnus.Colocar(new Vector2(0f, -330f));
            magnus.Reproducir(animacion, larga ? -1f : 0f);
        }

        void ConstruirPersonalizacion(Transform raiz)
        {
            Texto(raiz, "Personalización", 34, new Vector2(700f, 360f), new Vector2(460f, 50f)).color = new Color(1f, 0.88f, 0.5f, 1f);

            textoPelo = Texto(raiz, "", 26, new Vector2(700f, 280f), new Vector2(300f, 40f));
            Boton(raiz, "<", new Vector2(520f, 280f), new Vector2(70f, 60f), () => CambiarPelo(-1));
            Boton(raiz, ">", new Vector2(880f, 280f), new Vector2(70f, 60f), () => CambiarPelo(1));

            textoTunica = Texto(raiz, "", 26, new Vector2(700f, 190f), new Vector2(300f, 40f));
            Boton(raiz, "<", new Vector2(520f, 190f), new Vector2(70f, 60f), () => CambiarTunica(-1));
            Boton(raiz, ">", new Vector2(880f, 190f), new Vector2(70f, 60f), () => CambiarTunica(1));

            textoGafas = Texto(raiz, "", 26, new Vector2(700f, 100f), new Vector2(300f, 40f));
            Boton(raiz, "Cambiar", new Vector2(700f, 40f), new Vector2(200f, 60f), () =>
            {
                aspecto.gafas = !aspecto.gafas;
                Aplicar();
            });

            Boton(raiz, "Usar lo guardado", new Vector2(700f, -60f), new Vector2(300f, 64f), () =>
            {
                aspecto = PersonalizacionMagnus.Guardada;
                Aplicar();
            });

            Refrescar();
        }

        void CambiarPelo(int paso)
        {
            aspecto.pelo = (aspecto.pelo + paso + AspectoPoses.CantidadPelos) % AspectoPoses.CantidadPelos;
            Aplicar();
        }

        void CambiarTunica(int paso)
        {
            aspecto.colorTunica = (aspecto.colorTunica + paso + AspectoPoses.CantidadTunicas) % AspectoPoses.CantidadTunicas;
            Aplicar();
        }

        void Aplicar()
        {
            magnus.CambiarAspecto(aspecto);
            Refrescar();
        }

        void Refrescar()
        {
            textoPelo.text = "Pelo: " + NombresPelo[Mathf.Clamp(aspecto.pelo, 0, NombresPelo.Length - 1)];
            textoTunica.text = "Túnica: " + NombresTunica[Mathf.Clamp(aspecto.colorTunica, 0, NombresTunica.Length - 1)];
            textoGafas.text = "Gafas: " + (aspecto.gafas ? "sí" : "no");
        }

        // ───────────────────────── Ayudas de UI ─────────────────────────

        void Boton(Transform padre, string etiqueta, Vector2 posicion, Vector2 tamano, UnityEngine.Events.UnityAction accion)
        {
            var imagen = Panel(padre, posicion, tamano, Color.white);
            imagen.sprite = Resources.Load<Sprite>("FluxLab/button_brown");
            imagen.type = Image.Type.Sliced;
            imagen.raycastTarget = true;
            var boton = imagen.gameObject.AddComponent<Button>();
            boton.targetGraphic = imagen;
            boton.onClick.AddListener(accion);
            imagen.gameObject.AddComponent<HoverScale>().scale = 1.05f;
            var texto = Texto(imagen.transform, etiqueta, 24, Vector2.zero, tamano);
            texto.color = new Color(1f, 0.97f, 0.93f, 1f);
        }

        Image Panel(Transform padre, Vector2 posicion, Vector2 tamano, Color color)
        {
            var go = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(padre, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = posicion;
            rect.sizeDelta = tamano;
            var imagen = go.GetComponent<Image>();
            imagen.color = color;
            imagen.raycastTarget = false;
            return imagen;
        }

        Text Texto(Transform padre, string contenido, int tamano, Vector2 posicion, Vector2 caja)
        {
            var go = new GameObject("Texto", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(padre, false);
            var texto = go.GetComponent<Text>();
            texto.font = fuente;
            texto.text = contenido;
            texto.fontSize = tamano;
            texto.fontStyle = FontStyle.Bold;
            texto.alignment = TextAnchor.MiddleCenter;
            texto.color = Color.white;
            texto.raycastTarget = false;
            texto.rectTransform.anchoredPosition = posicion;
            texto.rectTransform.sizeDelta = caja;
            var contorno = go.AddComponent<Outline>();
            contorno.effectColor = new Color(0.2f, 0.1f, 0.05f, 0.9f);
            contorno.effectDistance = new Vector2(2f, -2f);
            return texto;
        }

        static void Estirar(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
