using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FluxLab
{
    /// <summary>
    /// Vestidor de Magnus. Pelo, color de túnica, accesorios y efectos.
    /// Algunas piezas se abren al subir el nivel del taller.
    /// </summary>
    public class PersonalizarMagnus : MonoBehaviour
    {
        struct Pieza
        {
            public string nombre;
            public int nivel;
            public Color color;
        }

        static readonly Pieza[] Pelos =
        {
            new Pieza { nombre = "Clásico", nivel = 1, color = new Color(0f, 0f, 0f, 0f) },
            new Pieza { nombre = "Castaño", nivel = 1, color = Hex("#8a5a32") },
            new Pieza { nombre = "Rubio", nivel = 2, color = Hex("#e2b34a") },
            new Pieza { nombre = "Rojizo", nivel = 3, color = Hex("#c4483a") },
            new Pieza { nombre = "Canoso", nivel = 4, color = Hex("#e4ddd4") }
        };

        static readonly Pieza[] TunicaColores =
        {
            new Pieza { nombre = "Oscuro\nOriginal", nivel = 1, color = Hex("#3c3842") },
            new Pieza { nombre = "Rojo\nRubí", nivel = 1, color = Hex("#d64545") },
            new Pieza { nombre = "Azul\nZafiro", nivel = 1, color = Hex("#3a78d8") },
            new Pieza { nombre = "Amarillo\nEstrella", nivel = 1, color = Hex("#e2b53a") },
            new Pieza { nombre = "Verde\nEsmeralda", nivel = 2, color = Hex("#3ea86a") },
            new Pieza { nombre = "Naranja\nÁmbar", nivel = 3, color = Hex("#e07a2a") }
        };

        static readonly Pieza[] Accesorios =
        {
            new Pieza { nombre = "Ninguno", nivel = 1 },
            new Pieza { nombre = "Frasco", nivel = 1 },
            new Pieza { nombre = "Capa", nivel = 3 },
            new Pieza { nombre = "Medallón", nivel = 4 }
        };

        static readonly Pieza[] Efectos =
        {
            new Pieza { nombre = "Ninguno", nivel = 1 },
            new Pieza { nombre = "Brillo", nivel = 1 },
            new Pieza { nombre = "Estrellas", nivel = 3 },
            new Pieza { nombre = "Aura", nivel = 5 }
        };

        static readonly string[] Categorias = { "Pelo", "Cuerpo", "Accesorio", "Efecto" };
        static readonly string[] Detalles = { "Color", "Color", "Piezas", "Magia" };

        Font font;
        Texture2D baseMagnus;
        Texture2D baseTunica;
        Texture2D capaTextura;
        Texture2D estrella;
        Texture2D medallon;
        Texture2D frasco;
        Texture2D brilloTextura;
        Texture2D[] cabezas;
        readonly Texture2D[,] aspectos = new Texture2D[5, 6];
        RawImage vista;
        RawImage vistaCabeza;
        RawImage capa;
        RawImage broche;
        RawImage aura;
        RectTransform figura;
        Vector2 figuraBase;
        RectTransform efectoRaiz;
        int pelo;
        int color;
        int accesorio;
        int efecto;
        int categoria;
        readonly RawImage[] marcosCategoria = new RawImage[4];
        readonly Image[] marcosOpcion = new Image[6];
        readonly RawImage[] muestrasOpcion = new RawImage[6];
        readonly Text[] textosOpcion = new Text[6];
        readonly Text[] candados = new Text[6];
        readonly Image[] marcosColor = new Image[6];
        Text aviso;
        Text nivelTexto;

        void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            baseMagnus = Recortar(Leer("Personaje/magnus.png"));
            baseTunica = Recortar(Leer("Personaje/tunica_0.png"));
            cabezas = new[]
            {
                baseMagnus,
                RecortarBase(Leer("Personaje/pelo_despeinado.png"), 0.04f),
                RecortarBase(Leer("Personaje/pelo_puntas.png"), 0.08f),
                RecortarBase(Leer("Personaje/pelo_feliz.png"), 0.2f),
                RecortarBase(Leer("Personaje/pelo_sorpresa.png"), 0.16f)
            };
            capaTextura = CrearCapa();
            brilloTextura = CrearBrillo();
            estrella = TexturaDeSprite("FluxLab/gem_yellow_star");
            medallon = TexturaDeSprite("FluxLab/gem_red_circle");
            frasco = TexturaDeSprite("FluxLab/frasco_morado");
            pelo = IndiceValido(Pelos, AjustesJuego.Pelo);
            color = IndiceValido(TunicaColores, AjustesJuego.ColorTunica);
            accesorio = IndiceValido(Accesorios, AjustesJuego.Accesorio);
            efecto = IndiceValido(Efectos, AjustesJuego.Efecto);
            Construir();
            PrepararMusica();
        }

        void Update()
        {
            if (figura != null)
                figura.anchoredPosition = figuraBase + new Vector2(0f, Mathf.Sin(Time.time * 1.6f) * 7f);

            AnimarEfecto();
            if (Input.GetKeyDown(KeyCode.Escape))
                Volver();
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

            var fondo = Imagen(raiz, "Fondo", Leer("Menu/fondo.png"), Vector2.zero, 1920f);
            Estirar(fondo.rectTransform);
            SoltarBurbujas(raiz);

            Texture2D pergamino = Leer("Menu/pergamino.png");
            Texture2D boton = Leer("Menu/boton.png");
            var titulo = Imagen(raiz, "Pergamino", pergamino, new Vector2(0f, 492f), 640f, 78f);
            var sub = Texto(titulo.transform, "Personaliza a Magnus", 30, Hex("#5a3b24"), FontStyle.Bold);
            sub.rectTransform.anchoredPosition = new Vector2(0f, 2f);
            sub.rectTransform.sizeDelta = new Vector2(480f, 44f);

            ConstruirFigura(raiz);
            ConstruirCategorias(raiz, boton);
            ConstruirOpciones(raiz);
            ConstruirColores(raiz);
            ConstruirPie(raiz, boton);
            Refrescar();
        }

        void ConstruirFigura(Transform raiz)
        {
            var grupo = new GameObject("Figura", typeof(RectTransform));
            grupo.transform.SetParent(raiz, false);
            figura = grupo.GetComponent<RectTransform>();
            figura.anchorMin = figura.anchorMax = new Vector2(0.5f, 0.5f);
            figura.pivot = new Vector2(0.5f, 0.5f);
            figura.sizeDelta = new Vector2(280f, 360f);
            figura.anchoredPosition = new Vector2(0f, 318f);
            figuraBase = figura.anchoredPosition;

            aura = Imagen(grupo.transform, "Aura", brilloTextura, new Vector2(0f, 10f), 250f, 280f);
            aura.color = new Color(1f, 1f, 1f, 0.28f);
            aura.gameObject.SetActive(false);

            capa = Imagen(grupo.transform, "Capa", capaTextura, new Vector2(0f, -40f), 170f, 240f);

            vista = Imagen(grupo.transform, "Magnus", baseMagnus, Vector2.zero, 140f);
            EncajarAlto(vista, 330f);
            vistaCabeza = Imagen(grupo.transform, "Cabeza", cabezas[1], new Vector2(0f, 120f), 120f);
            vistaCabeza.gameObject.SetActive(false);

            broche = Imagen(grupo.transform, "Broche", estrella, new Vector2(8f, 18f), 36f);

            var efectos = new GameObject("Efectos", typeof(RectTransform));
            efectos.transform.SetParent(grupo.transform, false);
            efectoRaiz = efectos.GetComponent<RectTransform>();
            efectoRaiz.anchorMin = efectoRaiz.anchorMax = new Vector2(0.5f, 0.5f);
            efectoRaiz.pivot = new Vector2(0.5f, 0.5f);
            efectoRaiz.anchoredPosition = Vector2.zero;
            efectoRaiz.sizeDelta = new Vector2(260f, 340f);
        }

        void ConstruirCategorias(Transform raiz, Texture2D madera)
        {
            for (int i = 0; i < Categorias.Length; i++)
                CrearCategoria(raiz, madera, Categorias[i], Detalles[i], new Vector2(-690f, 248f - i * 92f), i);
        }

        void CrearCategoria(Transform raiz, Texture2D madera, string titulo, string detalle, Vector2 posicion, int indice)
        {
            var placa = Imagen(raiz, titulo, madera, posicion, 300f, 76f);
            placa.raycastTarget = true;
            marcosCategoria[indice] = placa;
            var boton = placa.gameObject.AddComponent<Button>();
            boton.targetGraphic = placa;
            boton.navigation = new Navigation { mode = Navigation.Mode.None };
            int elegido = indice;
            boton.onClick.AddListener(() =>
            {
                categoria = elegido;
                if (aviso != null)
                    aviso.text = "";
                Refrescar();
            });

            var cabeza = Texto(placa.transform, titulo, 26, Hex("#fff6ea"), FontStyle.Bold);
            cabeza.rectTransform.anchoredPosition = new Vector2(0f, 12f);
            cabeza.rectTransform.sizeDelta = new Vector2(240f, 32f);
            Contorno(cabeza, new Color(0.28f, 0.13f, 0.05f, 0.95f));
            var cuerpo = Texto(placa.transform, detalle, 15, Hex("#ffe08a"), FontStyle.Bold);
            cuerpo.rectTransform.anchoredPosition = new Vector2(0f, -16f);
            cuerpo.rectTransform.sizeDelta = new Vector2(240f, 22f);
            Contorno(cuerpo, new Color(0.28f, 0.13f, 0.05f, 0.9f));
        }

        void ConstruirOpciones(Transform raiz)
        {
            Sprite panel = Resources.Load<Sprite>("FluxLab/panel_beige");
            for (int i = 0; i < 6; i++)
            {
                int columna = i % 3;
                int fila = i / 3;
                var marco = new GameObject("Opcion" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                marco.transform.SetParent(raiz, false);
                var rect = marco.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(148f, 132f);
                rect.anchoredPosition = new Vector2(500f + columna * 162f, 188f - fila * 150f);
                var imagen = marco.GetComponent<Image>();
                imagen.sprite = panel;
                imagen.type = Image.Type.Sliced;
                marcosOpcion[i] = imagen;
                int indice = i;
                var boton = marco.GetComponent<Button>();
                boton.targetGraphic = imagen;
                boton.onClick.AddListener(() => ElegirOpcion(indice));

                var muestra = Imagen(marco.transform, "Muestra", Texture2D.whiteTexture, new Vector2(0f, 18f), 46f, 46f);
                muestrasOpcion[i] = muestra;

                textosOpcion[i] = Texto(marco.transform, "", 16, Hex("#3b2a22"), FontStyle.Bold);
                textosOpcion[i].rectTransform.anchoredPosition = new Vector2(0f, -28f);
                textosOpcion[i].rectTransform.sizeDelta = new Vector2(132f, 48f);

                candados[i] = Texto(marco.transform, "", 15, Hex("#6a4030"), FontStyle.Bold);
                candados[i].rectTransform.anchoredPosition = new Vector2(0f, 22f);
                candados[i].rectTransform.sizeDelta = new Vector2(120f, 28f);
            }

            aviso = Texto(raiz, "", 20, Hex("#fff4e0"), FontStyle.Bold);
            aviso.rectTransform.anchoredPosition = new Vector2(620f, -20f);
            aviso.rectTransform.sizeDelta = new Vector2(460f, 56f);
        }

        void ConstruirColores(Transform raiz)
        {
            Sprite panel = Resources.Load<Sprite>("FluxLab/panel_beige");
            for (int i = 0; i < TunicaColores.Length; i++)
            {
                var marco = new GameObject("Color" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                marco.transform.SetParent(raiz, false);
                var rect = marco.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(118f, 86f);
                rect.anchoredPosition = new Vector2(-310f + i * 128f, -392f);
                var imagen = marco.GetComponent<Image>();
                imagen.sprite = panel;
                imagen.type = Image.Type.Sliced;
                marcosColor[i] = imagen;
                int indice = i;
                var boton = marco.GetComponent<Button>();
                boton.targetGraphic = imagen;
                boton.onClick.AddListener(() => ElegirColor(indice));

                var muestra = new GameObject("Muestra", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                muestra.transform.SetParent(marco.transform, false);
                var muestraRect = muestra.GetComponent<RectTransform>();
                muestraRect.anchorMin = muestraRect.anchorMax = new Vector2(0.5f, 0.5f);
                muestraRect.sizeDelta = new Vector2(36f, 36f);
                muestraRect.anchoredPosition = new Vector2(0f, 14f);
                muestra.GetComponent<Image>().color = TunicaColores[i].color;

                var etiqueta = Texto(marco.transform, TunicaColores[i].nombre, 13, Hex("#3b2a22"), FontStyle.Bold);
                etiqueta.rectTransform.anchoredPosition = new Vector2(0f, -24f);
                etiqueta.rectTransform.sizeDelta = new Vector2(110f, 36f);
            }
        }

        void ConstruirPie(Transform raiz, Texture2D boton)
        {
            CrearPlaca(raiz, boton, "GUARDAR Y SALIR", new Vector2(-760f, -470f), 320f, 68f, Guardar);
            CrearPlaca(raiz, boton, "COMO ANTES", new Vector2(0f, -470f), 280f, 68f, Restablecer);
            CrearPlaca(raiz, boton, "VOLVER", new Vector2(760f, -470f), 220f, 68f, Volver);

            var puntos = Imagen(raiz, "Puntos", boton, new Vector2(760f, 455f), 280f, 78f);
            nivelTexto = Texto(puntos.transform, "", 22, Hex("#ffe08a"), FontStyle.Bold);
            Estirar(nivelTexto.rectTransform);
            Contorno(nivelTexto, new Color(0.25f, 0.12f, 0.05f, 0.9f));
        }

        void CrearPlaca(Transform raiz, Texture2D textura, string etiqueta, Vector2 posicion, float ancho, float alto, UnityEngine.Events.UnityAction accion)
        {
            var placa = Imagen(raiz, etiqueta, textura, posicion, ancho, alto);
            placa.raycastTarget = true;
            var ui = placa.gameObject.AddComponent<Button>();
            ui.targetGraphic = placa;
            ui.onClick.AddListener(accion);
            placa.gameObject.AddComponent<HoverScale>().scale = 1.04f;
            var texto = Texto(placa.transform, etiqueta, 22, Hex("#fff6ea"), FontStyle.Bold);
            Estirar(texto.rectTransform);
            Contorno(texto, new Color(0.25f, 0.12f, 0.05f, 0.9f));
        }

        void ElegirOpcion(int indice)
        {
            Pieza[] lista = ListaActual();
            if (indice < 0 || indice >= lista.Length)
                return;
            if (AjustesJuego.NivelAlcanzado < lista[indice].nivel)
            {
                aviso.text = "Se abre en el nivel " + lista[indice].nivel + ".\nCompleta la prueba de la mesa.";
                return;
            }

            aviso.text = "";
            if (categoria == 0)
                pelo = indice;
            else if (categoria == 1)
                color = indice;
            else if (categoria == 2)
                accesorio = indice;
            else
                efecto = indice;
            Refrescar();
        }

        void ElegirColor(int indice)
        {
            if (AjustesJuego.NivelAlcanzado < TunicaColores[indice].nivel)
            {
                aviso.text = "Ese color se abre en el nivel " + TunicaColores[indice].nivel + ".";
                return;
            }

            color = indice;
            aviso.text = "";
            Refrescar();
        }

        void Restablecer()
        {
            pelo = 0;
            color = 0;
            accesorio = 0;
            efecto = 0;
            categoria = 0;
            aviso.text = "Magnus quedo como al principio.\nPulsa Guardar y salir si quieres conservarlo.";
            Refrescar();
        }

        void Refrescar()
        {
            AplicarAspecto();

            capa.gameObject.SetActive(accesorio == 2);
            capa.color = TunicaColores[color].color;
            broche.gameObject.SetActive(accesorio == 1 || accesorio == 3);
            if (accesorio == 1)
            {
                broche.texture = frasco;
                broche.rectTransform.anchoredPosition = new Vector2(-48f, -42f);
                EncajarAlto(broche, 62f);
            }
            else if (accesorio == 3)
            {
                broche.texture = medallon;
                broche.rectTransform.anchoredPosition = new Vector2(0f, 6f);
                EncajarAlto(broche, 34f);
            }

            Color luz = color == 0 ? Hex("#f0d78a") : TunicaColores[color].color;
            aura.gameObject.SetActive(efecto == 1 || efecto == 3);
            aura.color = new Color(luz.r, luz.g, luz.b, efecto == 3 ? 0.7f : 0.42f);
            ArmarEfecto();

            for (int i = 0; i < marcosCategoria.Length; i++)
                marcosCategoria[i].color = i == categoria ? Color.white : new Color(0.62f, 0.54f, 0.44f, 1f);

            Pieza[] lista = ListaActual();
            int seleccionado = SeleccionActual();
            for (int i = 0; i < marcosOpcion.Length; i++)
            {
                bool visible = i < lista.Length;
                marcosOpcion[i].gameObject.SetActive(visible);
                if (!visible)
                    continue;
                bool abierto = AjustesJuego.NivelAlcanzado >= lista[i].nivel;
                bool activo = abierto && i == seleccionado;
                marcosOpcion[i].color = activo ? Hex("#fff1c2") : abierto ? Hex("#f4ead7") : Hex("#d9d3c8");
                PintarMuestra(i, abierto);
                textosOpcion[i].text = lista[i].nombre;
                candados[i].text = abierto ? "" : "Nv. " + lista[i].nivel;
            }

            for (int i = 0; i < marcosColor.Length; i++)
            {
                bool abierto = AjustesJuego.NivelAlcanzado >= TunicaColores[i].nivel;
                marcosColor[i].color = i == color && abierto ? Hex("#fff1c2") : abierto ? Hex("#f4ead7") : Hex("#d9d3c8");
            }

            if (nivelTexto != null)
                nivelTexto.text = "Puntos:  " + AjustesJuego.MejorPuntaje + "\nNivel:  " + AjustesJuego.NivelAlcanzado;
        }

        Pieza[] ListaActual()
        {
            if (categoria == 0)
                return Pelos;
            if (categoria == 1)
                return TunicaColores;
            if (categoria == 2)
                return Accesorios;
            return Efectos;
        }

        int SeleccionActual()
        {
            if (categoria == 0)
                return pelo;
            if (categoria == 1)
                return color;
            if (categoria == 2)
                return accesorio;
            return efecto;
        }

        void ArmarEfecto()
        {
            for (int i = efectoRaiz.childCount - 1; i >= 0; i--)
                Destroy(efectoRaiz.GetChild(i).gameObject);

            if (efecto == 2 && estrella != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    var imagen = Imagen(efectoRaiz, "Estrella" + i, estrella, Vector2.zero, 28f);
                    imagen.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                }
            }
        }

        void AnimarEfecto()
        {
            if (efectoRaiz == null)
                return;
            if (efecto == 1 && aura != null)
            {
                float pulso = 0.96f + Mathf.Sin(Time.time * 1.4f) * 0.04f;
                aura.rectTransform.localScale = new Vector3(pulso, pulso, 1f);
            }
            else if (efecto == 2)
            {
                for (int i = 0; i < efectoRaiz.childCount; i++)
                {
                    var rect = efectoRaiz.GetChild(i) as RectTransform;
                    float angulo = Time.time * 1.3f + i * Mathf.PI * 0.5f;
                    rect.anchoredPosition = new Vector2(Mathf.Cos(angulo) * 78f, 170f + Mathf.Sin(angulo) * 70f);
                }
            }
            else if (efecto == 3 && aura != null)
            {
                float pulso = 1f + Mathf.Sin(Time.time * 2.2f) * 0.08f;
                aura.rectTransform.localScale = new Vector3(pulso, pulso, 1f);
            }
        }

        void AplicarAspecto()
        {
            vistaCabeza.gameObject.SetActive(false);
            vista.texture = Aspecto(pelo, color);
            EncajarAlto(vista, 340f);
            vista.rectTransform.anchoredPosition = Vector2.zero;
            vista.color = Color.white;
        }

        void PintarMuestra(int indice, bool abierto)
        {
            RawImage muestra = muestrasOpcion[indice];
            if (categoria == 0)
            {
                muestra.gameObject.SetActive(true);
                muestra.texture = Aspecto(indice, 0);
                muestra.color = abierto ? Color.white : new Color(1f, 1f, 1f, 0.4f);
                EncajarAlto(muestra, 92f);
                muestra.rectTransform.anchoredPosition = new Vector2(0f, 12f);
                return;
            }

            if (categoria == 1)
            {
                muestra.gameObject.SetActive(abierto);
                muestra.texture = Texture2D.whiteTexture;
                muestra.color = TunicaColores[indice].color;
                muestra.rectTransform.sizeDelta = new Vector2(42f, 42f);
                muestra.rectTransform.anchoredPosition = new Vector2(0f, 18f);
                return;
            }

            muestra.gameObject.SetActive(false);
        }

        Texture2D Aspecto(int indicePelo, int indiceColor)
        {
            if (aspectos[indicePelo, indiceColor] == null)
            {
                bool teñirPelo = Pelos[indicePelo].color.a > 0.2f;
                bool teñirRopa = indiceColor > 0;
                aspectos[indicePelo, indiceColor] = !teñirPelo && !teñirRopa
                    ? baseMagnus
                    : Recolorear(baseMagnus, Pelos[indicePelo].color, teñirPelo, TunicaColores[indiceColor].color, teñirRopa);
            }

            return aspectos[indicePelo, indiceColor];
        }

        Texture2D Recolorear(Texture2D origen, Color peloColor, bool teñirPelo, Color tunicaColor, bool teñirRopa)
        {
            Color[] pixeles = origen.GetPixels();
            int ancho = origen.width;
            int alto = origen.height;
            int total = pixeles.Length;
            var tela = new float[total];
            var cabello = new float[total];
            var libre = new bool[total];

            for (int i = 0; i < total; i++)
            {
                Color pixel = pixeles[i];
                if (pixel.a < 0.12f)
                    continue;
                float luma = pixel.r * 0.3f + pixel.g * 0.59f + pixel.b * 0.11f;
                float max = Mathf.Max(pixel.r, Mathf.Max(pixel.g, pixel.b));
                float min = Mathf.Min(pixel.r, Mathf.Min(pixel.g, pixel.b));
                float sat = max <= 0.001f ? 0f : (max - min) / max;
                float desdeAbajo = ((i / ancho) + 0.5f) / alto;
                bool piel = pixel.r > 0.62f && pixel.g > 0.45f && pixel.r > pixel.b + 0.04f;
                bool gafas = pixel.b > pixel.r + 0.05f && pixel.g > 0.25f;
                bool zapatos = desdeAbajo < 0.11f && pixel.r > pixel.g + 0.03f && pixel.r > pixel.b;
                if (piel || gafas || zapatos || luma < 0.08f || sat > 0.34f)
                    continue;

                libre[i] = true;
                if (teñirPelo && desdeAbajo > 0.68f)
                    cabello[i] = 1f;
                else if (teñirRopa && desdeAbajo > 0.10f && desdeAbajo <= 0.68f)
                    tela[i] = 1f;
            }

            for (int paso = 0; paso < 2; paso++)
            {
                float[] telaSuave = (float[])tela.Clone();
                float[] peloSuave = (float[])cabello.Clone();
                for (int y = 1; y < alto - 1; y++)
                {
                    for (int x = 1; x < ancho - 1; x++)
                    {
                        int i = y * ancho + x;
                        if (!libre[i])
                            continue;
                        float sumaTela = 0f;
                        float sumaPelo = 0f;
                        int cuenta = 0;
                        for (int oy = -1; oy <= 1; oy++)
                        {
                            for (int ox = -1; ox <= 1; ox++)
                            {
                                int j = (y + oy) * ancho + (x + ox);
                                if (!libre[j])
                                    continue;
                                sumaTela += tela[j];
                                sumaPelo += cabello[j];
                                cuenta++;
                            }
                        }

                        if (cuenta == 0)
                            continue;
                        telaSuave[i] = sumaTela / cuenta;
                        peloSuave[i] = sumaPelo / cuenta;
                    }
                }

                tela = telaSuave;
                cabello = peloSuave;
            }

            for (int i = 0; i < total; i++)
            {
                float peso = Mathf.Max(tela[i], cabello[i]);
                if (peso < 0.12f)
                    continue;
                Color pixel = pixeles[i];
                float luma = pixel.r * 0.3f + pixel.g * 0.59f + pixel.b * 0.11f;
                bool esPelo = cabello[i] >= tela[i];
                Color tinte = esPelo ? peloColor : tunicaColor;
                float brillo = Mathf.Clamp(luma / (esPelo ? 0.22f : 0.24f), 0.32f, 1.08f);
                Color destino = Color.Lerp(pixel, tinte * brillo, peso * (esPelo ? 0.82f : 0.92f));
                destino.a = pixel.a;
                pixeles[i] = destino;
            }

            var copia = new Texture2D(ancho, alto, TextureFormat.RGBA32, false);
            copia.SetPixels(pixeles);
            copia.Apply();
            copia.filterMode = FilterMode.Bilinear;
            return copia;
        }

        void Guardar()
        {
            AjustesJuego.Pelo = pelo;
            AjustesJuego.ColorTunica = color;
            AjustesJuego.Accesorio = accesorio;
            AjustesJuego.Efecto = efecto;
            AjustesJuego.GuardarApariencia();
            SceneManager.LoadScene("MenuPrincipal");
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

        void SoltarBurbujas(Transform raiz)
        {
            Texture2D[] burbujas = new Texture2D[3];
            string[] nombres = { "burbuja_a", "burbuja_b", "burbuja_c" };
            for (int i = 0; i < nombres.Length; i++)
                burbujas[i] = TexturaDeSprite("FluxLab/" + nombres[i]);
            for (int i = 0; i < 8; i++)
            {
                if (burbujas[i % 3] == null)
                    continue;
                var imagen = Imagen(raiz, "BurbujaFondo" + i, burbujas[i % 3], new Vector2(-820f + i * 230f, -400f + (i * 140) % 800), 24f + (i % 4) * 12f);
                imagen.color = new Color(1f, 1f, 1f, 0.8f);
                var flotante = imagen.gameObject.AddComponent<BurbujaFlotante>();
                flotante.velocidad = 16f + (i % 3) * 10f;
                flotante.amplitud = 12f;
            }
        }

        static int IndiceValido(Pieza[] lista, int indice)
        {
            indice = Mathf.Clamp(indice, 0, lista.Length - 1);
            if (AjustesJuego.NivelAlcanzado < lista[indice].nivel)
                return 0;
            return indice;
        }

        static Texture2D TexturaDeSprite(string ruta)
        {
            Sprite sprite = Resources.Load<Sprite>(ruta);
            if (sprite != null)
                return sprite.texture;
            return Resources.Load<Texture2D>(ruta);
        }

        static Texture2D CrearCapa()
        {
            const int ancho = 80;
            const int alto = 120;
            var textura = new Texture2D(ancho, alto, TextureFormat.RGBA32, false);
            var pixeles = new Color[ancho * alto];
            for (int y = 0; y < alto; y++)
            {
                for (int x = 0; x < ancho; x++)
                {
                    float nx = (x - ancho * 0.5f) / (ancho * 0.46f);
                    float ny = (y - alto * 0.42f) / (alto * 0.55f);
                    float dentro = nx * nx + ny * ny;
                    pixeles[y * ancho + x] = new Color(1f, 1f, 1f, dentro < 1f ? 0.92f : 0f);
                }
            }

            textura.SetPixels(pixeles);
            textura.Apply();
            textura.filterMode = FilterMode.Bilinear;
            return textura;
        }

        static Texture2D CrearBrillo()
        {
            const int n = 128;
            var textura = new Texture2D(n, n, TextureFormat.RGBA32, false);
            var pixeles = new Color[n * n];
            float centro = (n - 1) * 0.5f;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = (x - centro) / centro;
                    float dy = (y - centro) / (centro * 1.15f);
                    float distancia = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(1f - distancia);
                    alpha *= alpha;
                    pixeles[y * n + x] = new Color(1f, 1f, 1f, alpha * 0.85f);
                }
            }

            textura.SetPixels(pixeles);
            textura.Apply();
            textura.filterMode = FilterMode.Bilinear;
            return textura;
        }

        static Texture2D RecortarBase(Texture2D origen, float quitarAbajo)
        {
            Texture2D recortada = Recortar(origen);
            int quitar = Mathf.RoundToInt(recortada.height * Mathf.Clamp01(quitarAbajo));
            if (quitar < 2 || quitar >= recortada.height - 4)
                return recortada;
            int nuevoAlto = recortada.height - quitar;
            var copia = new Texture2D(recortada.width, nuevoAlto, TextureFormat.RGBA32, false);
            copia.SetPixels(recortada.GetPixels(0, quitar, recortada.width, nuevoAlto));
            copia.Apply();
            copia.filterMode = FilterMode.Bilinear;
            return copia;
        }

        static Texture2D Recortar(Texture2D origen)
        {
            if (origen == null)
                return Texture2D.whiteTexture;
            Color[] pixeles = origen.GetPixels();
            int ancho = origen.width;
            int alto = origen.height;
            int minX = ancho;
            int minY = alto;
            int maxX = 0;
            int maxY = 0;
            bool hay = false;
            for (int y = 0; y < alto; y++)
            {
                for (int x = 0; x < ancho; x++)
                {
                    if (pixeles[y * ancho + x].a <= 0.08f)
                        continue;
                    hay = true;
                    if (x < minX) minX = x;
                    if (y < minY) minY = y;
                    if (x > maxX) maxX = x;
                    if (y > maxY) maxY = y;
                }
            }

            if (!hay)
                return origen;
            int recorteAncho = maxX - minX + 1;
            int recorteAlto = maxY - minY + 1;
            var copia = new Texture2D(recorteAncho, recorteAlto, TextureFormat.RGBA32, false);
            copia.SetPixels(origen.GetPixels(minX, minY, recorteAncho, recorteAlto));
            copia.Apply();
            copia.filterMode = FilterMode.Bilinear;
            return copia;
        }

        static Texture2D Leer(string relativo)
        {
            string ruta = Path.Combine(Application.streamingAssetsPath, relativo);
            if (!File.Exists(ruta))
                return Texture2D.whiteTexture;
            var textura = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            textura.LoadImage(File.ReadAllBytes(ruta));
            textura.filterMode = FilterMode.Bilinear;
            return textura;
        }

        RawImage Imagen(Transform padre, string nombre, Texture2D textura, Vector2 posicion, float ancho, float alto = -1f)
        {
            var go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            go.transform.SetParent(padre, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = posicion;
            float relacion = textura == null || textura.height <= 0 ? 1f : textura.width / (float)Mathf.Max(1, textura.height);
            if (alto < 0f)
                alto = ancho / relacion;
            rect.sizeDelta = new Vector2(ancho, alto);
            var imagen = go.GetComponent<RawImage>();
            imagen.texture = textura;
            imagen.raycastTarget = false;
            return imagen;
        }

        static void EncajarAlto(RawImage imagen, float alto)
        {
            Texture textura = imagen.texture;
            float relacion = textura == null || textura.height <= 0 ? 1f : textura.width / (float)textura.height;
            imagen.rectTransform.sizeDelta = new Vector2(alto * relacion, alto);
        }

        Text Texto(Transform padre, string contenido, int tamano, Color colorTexto, FontStyle estilo)
        {
            var go = new GameObject("Texto", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(padre, false);
            var texto = go.GetComponent<Text>();
            texto.font = font;
            texto.text = contenido;
            texto.fontSize = tamano;
            texto.fontStyle = estilo;
            texto.color = colorTexto;
            texto.alignment = TextAnchor.MiddleCenter;
            texto.horizontalOverflow = HorizontalWrapMode.Wrap;
            texto.verticalOverflow = VerticalWrapMode.Overflow;
            texto.raycastTarget = false;
            texto.rectTransform.sizeDelta = new Vector2(240f, 40f);
            return texto;
        }

        static void Contorno(Text texto, Color color)
        {
            var contorno = texto.gameObject.AddComponent<Outline>();
            contorno.effectColor = color;
            contorno.effectDistance = new Vector2(1.3f, -1.3f);
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
