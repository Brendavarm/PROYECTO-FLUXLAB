using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FluxLab
{
    /// <summary>
    /// Herramientas para armar la interfaz del nivel 3 (imágenes, textos y botones).
    /// Todo en coordenadas del lienzo de 1920x1080, con el centro en (0, 0).
    /// </summary>
    public static class Nivel3UI
    {
        static Font fuente;
        static Sprite solido;
        static Sprite luz;

        public static Font Fuente
        {
            get
            {
                if (fuente != null)
                    return fuente;

                fuente = Resources.Load<Font>("FluxLab/Nivel3/Fuentes/LilitaOne-Regular");
                if (fuente == null)
                {
                    Debug.LogWarning("No se encontró la fuente del nivel 3; se usa la de Unity.");
                    fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (fuente == null)
                        fuente = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }

                return fuente;
            }
        }

        public static Color Hex(string valor)
        {
            ColorUtility.TryParseHtmlString(valor, out Color color);
            return color;
        }

        /// <summary>Un dibujo de Resources/FluxLab. Si falta, devuelve un cuadro liso para no romper nada.</summary>
        public static Sprite Spr(string nombre)
        {
            var sprite = Resources.Load<Sprite>("FluxLab/" + nombre);
            if (sprite != null)
                return sprite;
            Debug.LogWarning("No se encontró el sprite FluxLab/" + nombre);
            return Solido();
        }

        /// <summary>
        /// Un dibujo de Resources por su ruta completa (por ejemplo "FluxLab/Nivel3/caldero"). Si Unity lo importó
        /// como imagen normal y no como sprite, arma el sprite aquí. Devuelve null si no existe.
        /// </summary>
        public static Sprite SpriteOpcional(string ruta)
        {
            var sprite = Resources.Load<Sprite>(ruta);
            if (sprite != null)
                return sprite;

            var textura = Resources.Load<Texture2D>(ruta);
            if (textura == null)
                return null;
            return Sprite.Create(textura, new Rect(0f, 0f, textura.width, textura.height), new Vector2(0.5f, 0.5f), 100f);
        }

        public static Sprite Solido()
        {
            if (solido != null)
                return solido;

            var textura = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                    textura.SetPixel(x, y, Color.white);
            textura.Apply();
            solido = Sprite.Create(textura, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 100f);
            return solido;
        }

        /// <summary>Un círculo de luz que se apaga hacia los bordes.</summary>
        public static Sprite LuzSuave()
        {
            if (luz != null)
                return luz;

            const int lado = 64;
            var textura = new Texture2D(lado, lado, TextureFormat.RGBA32, false);
            float radio = (lado - 1) * 0.5f;
            var c = new Vector2(radio, radio);
            for (int y = 0; y < lado; y++)
            {
                for (int x = 0; x < lado; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), c) / radio;
                    float a = Mathf.Clamp01(1f - d);
                    textura.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            }

            textura.Apply();
            luz = Sprite.Create(textura, new Rect(0f, 0f, lado, lado), new Vector2(0.5f, 0.5f), 100f);
            return luz;
        }

        /// <summary>Lee una imagen de StreamingAssets (por ejemplo "Menu/pergamino.png").</summary>
        public static Texture2D LeerTextura(string relativo)
        {
            string ruta = Path.Combine(Application.streamingAssetsPath, relativo);
            if (!File.Exists(ruta))
            {
                Debug.LogWarning("No está la imagen: " + ruta);
                return null;
            }

            var textura = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            textura.LoadImage(File.ReadAllBytes(ruta));
            textura.filterMode = FilterMode.Bilinear;
            return textura;
        }

        public static Sprite SpriteDeArchivo(string relativo)
        {
            var textura = LeerTextura(relativo);
            if (textura == null)
                return Solido();
            return Sprite.Create(textura, new Rect(0f, 0f, textura.width, textura.height), new Vector2(0.5f, 0.5f), 100f);
        }

        public static RectTransform Vacio(Transform padre, string nombre, Vector2 posicion, Vector2 tamano)
        {
            var go = new GameObject(nombre, typeof(RectTransform));
            go.transform.SetParent(padre, false);
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = posicion;
            r.sizeDelta = tamano;
            return r;
        }

        public static Image Imagen(Transform padre, string nombre, Sprite sprite, Vector2 posicion, Vector2 tamano, bool conservarProporcion = false)
        {
            var go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(padre, false);
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = posicion;
            r.sizeDelta = tamano;
            var imagen = go.GetComponent<Image>();
            imagen.sprite = sprite;
            imagen.preserveAspect = conservarProporcion;
            imagen.raycastTarget = false;
            return imagen;
        }

        public static Text Texto(Transform padre, string contenido, int tamano, Color color, Vector2 posicion, Vector2 caja,
            TextAnchor alineacion = TextAnchor.MiddleCenter, bool contorno = false)
        {
            var go = new GameObject("Texto", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(padre, false);
            var texto = go.GetComponent<Text>();
            texto.font = Fuente;
            texto.text = contenido;
            texto.fontSize = tamano;
            texto.fontStyle = FontStyle.Normal;
            texto.color = color;
            texto.alignment = alineacion;
            texto.horizontalOverflow = HorizontalWrapMode.Wrap;
            texto.verticalOverflow = VerticalWrapMode.Overflow;
            texto.raycastTarget = false;
            var r = texto.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = posicion;
            r.sizeDelta = caja;
            if (contorno)
                Contorno(texto, new Color(0.15f, 0.07f, 0.04f, 0.95f), 2.5f);
            return texto;
        }

        public static void Contorno(Text texto, Color color, float grosor = 2f)
        {
            var contorno = texto.gameObject.AddComponent<Outline>();
            contorno.effectColor = color;
            contorno.effectDistance = new Vector2(grosor, -grosor);
        }

        /// <summary>Un botón de madera con texto.</summary>
        public static Image Boton(Transform padre, string etiqueta, Vector2 posicion, Vector2 tamano, UnityAction alPulsar, int tamanoLetra = 34)
        {
            // Los botones de los cuadros de mensaje usan las tablillas de madera (o el botón liso de siempre si faltan)
            return BotonConArte(padre, "boton_ancho", etiqueta, posicion, tamano, alPulsar, tamanoLetra);
        }

        /// <summary>Un cuadro de mensaje: el pergamino rústico, que se estira por el centro; o el panel beige de siempre.</summary>
        public static Image Panel(Transform padre, string nombre, Vector2 posicion, Vector2 tamano)
        {
            Sprite dibujo = SpriteUI("pergamino");
            var panel = Imagen(padre, nombre, dibujo != null ? dibujo : Spr("panel_beige"), posicion, tamano);
            panel.type = Image.Type.Sliced;
            return panel;
        }

        // ---------------------------------------------------------------------------------
        // Piezas de interfaz con arte opcional
        // Si existe Resources/FluxLab/Nivel3/UI/<nombre>.png se usa ese dibujo; si no, el de siempre.
        // ---------------------------------------------------------------------------------

        public static Sprite SpriteUI(string nombre)
        {
            return SpriteOpcional("FluxLab/Nivel3/UI/" + nombre);
        }

        /// <summary>Una placa (marco con fondo) para textos: con arte propio, o el panel beige de siempre.</summary>
        public static Image Placa(Transform padre, string arte, Vector2 posicion, Vector2 tamano)
        {
            Sprite dibujo = SpriteUI(arte);
            if (dibujo != null)
                return Imagen(padre, arte, dibujo, posicion, tamano, true);

            var imagen = Imagen(padre, arte, Spr("panel_beige"), posicion, tamano);
            imagen.type = Image.Type.Sliced;
            return imagen;
        }

        /// <summary>Un botón con arte propio (o de madera lisa), con texto opcional y un icono opcional.</summary>
        public static Image BotonConArte(Transform padre, string arte, string etiqueta, Vector2 posicion, Vector2 tamano,
            UnityAction alPulsar, int tamanoLetra = 34)
        {
            Sprite dibujo = SpriteUI(arte);
            var placa = Imagen(padre, "Boton_" + (string.IsNullOrEmpty(etiqueta) ? arte : etiqueta), dibujo != null ? dibujo : Spr("button_brown"), posicion, tamano, dibujo != null);
            if (dibujo == null)
                placa.type = Image.Type.Sliced;
            placa.raycastTarget = true;

            // Si el botón no tiene las proporciones del dibujo, se estira por el centro (el arte trae sus bordes marcados)
            if (dibujo != null)
            {
                float proporcionDibujo = dibujo.rect.width / dibujo.rect.height;
                float proporcionBoton = tamano.x / tamano.y;
                if (dibujo.border.sqrMagnitude > 0f && Mathf.Abs(proporcionBoton - proporcionDibujo) / proporcionDibujo > 0.15f)
                {
                    placa.type = Image.Type.Sliced;
                    placa.preserveAspect = false;
                }
            }

            var boton = placa.gameObject.AddComponent<Button>();
            boton.targetGraphic = placa;
            boton.navigation = new Navigation { mode = Navigation.Mode.None };
            var colores = boton.colors;
            colores.highlightedColor = new Color(1f, 0.95f, 0.85f, 1f);
            colores.pressedColor = new Color(0.8f, 0.7f, 0.55f, 1f);
            colores.fadeDuration = 0.06f;
            boton.colors = colores;
            boton.onClick.AddListener(alPulsar);
            placa.gameObject.AddComponent<HoverScale>().scale = 1.05f;

            if (!string.IsNullOrEmpty(etiqueta))
            {
                // Sobre madera: letra crema con una sombra suave; sobre el botón liso de siempre, el contorno de antes
                var texto = Texto(placa.transform, etiqueta, tamanoLetra, dibujo != null ? Hex("#f3e4c4") : Hex("#fff8ee"), Vector2.zero, tamano, TextAnchor.MiddleCenter, dibujo == null);
                if (dibujo != null)
                    Contorno(texto, new Color(0.1f, 0.05f, 0.03f, 0.7f), 1.6f);
                Estirar(texto.rectTransform);
            }

            return placa;
        }

        /// <summary>Un icono pequeño dentro de un botón (lee la imagen de StreamingAssets/Menu).</summary>
        public static Image Icono(Transform padre, string archivoMenu, Vector2 posicion, float lado)
        {
            var icono = Imagen(padre, "Icono", SpriteDeArchivo("Menu/" + archivoMenu), posicion, new Vector2(lado, lado), true);
            icono.raycastTarget = false;
            return icono;
        }

        public static void Estirar(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
        }
    }
}
