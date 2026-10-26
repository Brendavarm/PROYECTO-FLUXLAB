using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace FluxLab
{
    /// <summary>
    /// Historia del capítulo 1. Cada hito es un punto del lienzo.
    /// De un punto al siguiente, el desplazamiento es el vector destino − origen.
    /// </summary>
    public class TutorialCapitulo1 : MonoBehaviour
    {
        struct Hito
        {
            public string titulo;
            public string relato;
            public Vector2 punto;
        }

        static readonly Hito[] Historia =
        {
            new Hito
            {
                titulo = "El compañero",
                relato = "«Soy Mishi. Magnus quiere ser el mejor alquimista del rito, y no puede solo. Tú, {nombre}, eres su {companero}.»",
                punto = new Vector2(-520f, -40f)
            },
            new Hito
            {
                titulo = "La promesa",
                relato = "«Esta es la Mesa del Aprendiz: tres ingredientes y tres rondas. Cada acierto deja a Magnus un paso más cerca del Caldero Mayor.»",
                punto = new Vector2(-690f, 398f)
            },
            new Hito
            {
                titulo = "El pergamino",
                relato = "«El pergamino muestra la receta de izquierda a derecha. La barra verde dura cinco segundos. Léela con él, antes de que se apague.»",
                punto = new Vector2(20f, 348f)
            },
            new Hito
            {
                titulo = "El caldero",
                relato = "«Cuando el pergamino calle, la receta queda en ustedes dos. Repítela sobre el caldero, en el mismo orden.»",
                punto = new Vector2(0f, -20f)
            },
            new Hito
            {
                titulo = "Los frascos",
                relato = "«Abajo están los frascos. Cada uno es una forma y un color. Tócalos, o pulsa del 1 al 6. Tu mano entrena la de Magnus.»",
                punto = new Vector2(-90f, -440f)
            },
            new Hito
            {
                titulo = "Vaciar",
                relato = "«Si la mezcla se adelanta, Vaciar devuelve todo al estante. No es un fallo: es cómo {el} {rol} aprende a no precipitarse.»",
                punto = new Vector2(790f, -440f)
            },
            new Hito
            {
                titulo = "El rito",
                relato = "«Si aciertan, se abre el siguiente taller. Ayuda a Magnus hasta que el laboratorio lo reconozca. Cuando quieras, {nombre}, empezamos.»",
                punto = new Vector2(180f, 40f)
            }
        };

        Font font;
        Sprite solido;
        Sprite disco;
        Action alTerminar;
        RectTransform raiz;
        RectTransform anillo;
        MishiAnimado mishi;
        RectTransform caja;
        RectTransform[] marcas;
        Text titulo;
        Text relato;
        Text paso;
        Text seguirTexto;
        Button seguir;
        int indice;
        bool enCamino;
        bool cerrado;
        PuntoDeCamino[] escena;

        // Caras de Resources/FluxLab/Mishi/frente_0 … frente_7, una por hito.
        static readonly int[] Caras = { 1, 6, 7, 0, 5, 3, 4 };

        public void Abrir(Transform lienzo, Font fuente, Action terminar)
        {
            font = fuente;
            alTerminar = terminar;
            solido = Cuadrado();
            disco = Disco(96);
            TomarPuntosDeLaEscena();
            Construir(lienzo);
            indice = 0;
            anillo.anchoredPosition = marcas[0].anchoredPosition;
            if (mishi != null)
            {
                mishi.ColocarExplicando(PiesDe(marcas[0].anchoredPosition), CaraDe(0));
                TraerMishiAlFrente();
            }
            MostrarTexto(0);
        }

        void Update()
        {
            if (cerrado || anillo == null)
                return;

            float pulso = 1f + Mathf.Sin(Time.time * 2.4f) * 0.07f;
            anillo.localScale = new Vector3(pulso, pulso, 1f);

            if (enCamino)
                return;

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
                Seguir();
            else if (Input.GetKeyDown(KeyCode.Escape))
                Saltar();
        }

        void Construir(Transform lienzo)
        {
            var root = new GameObject("TutorialCapitulo1", typeof(RectTransform));
            root.transform.SetParent(lienzo, false);
            raiz = root.GetComponent<RectTransform>();
            raiz.anchorMin = Vector2.zero;
            raiz.anchorMax = Vector2.one;
            raiz.offsetMin = Vector2.zero;
            raiz.offsetMax = Vector2.zero;

            var velo = Imagen(raiz, "Velo", solido, Vector2.zero, new Vector2(1920f, 1080f));
            velo.color = new Color(0.1f, 0.06f, 0.04f, 0.58f);
            velo.raycastTarget = true;
            Estirar(velo.rectTransform);

            var camino = new GameObject("Camino", typeof(RectTransform));
            camino.transform.SetParent(raiz, false);
            var caminoRect = camino.GetComponent<RectTransform>();
            caminoRect.anchorMin = caminoRect.anchorMax = new Vector2(0.5f, 0.5f);
            caminoRect.sizeDelta = Vector2.zero;

            marcas = new RectTransform[Cantidad];
            for (int i = 0; i < Cantidad; i++)
            {
                var marca = new GameObject("Punto " + (i + 1), typeof(RectTransform));
                marca.transform.SetParent(camino.transform, false);
                var rect = marca.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Posicion(i);
                rect.sizeDelta = new Vector2(24f, 24f);
                marcas[i] = rect;
            }

            TrazarCamino(camino.transform);

            anillo = Imagen(raiz, "Anillo", disco, marcas[0].anchoredPosition, new Vector2(168f, 168f)).rectTransform;
            anillo.GetComponent<Image>().color = new Color(1f, 0.86f, 0.45f, 0.48f);

            mishi = MishiAnimado.Crear(raiz);
            mishi.escala = 1.55f;
            mishi.velocidad = 360f;
            mishi.Tranquila = true;

            caja = Imagen(raiz, "Relato", SpriteDe("panel_beige"), new Vector2(120f, -340f), new Vector2(1080f, 360f)).rectTransform;
            var placa = caja.GetComponent<Image>();
            placa.type = Image.Type.Sliced;
            placa.color = new Color(1f, 0.97f, 0.92f, 0.98f);

            titulo = Texto(caja, "", 42, Hex("#5a3b24"), FontStyle.Normal, TextAnchor.MiddleLeft);
            titulo.rectTransform.anchorMin = new Vector2(0f, 1f);
            titulo.rectTransform.anchorMax = new Vector2(0f, 1f);
            titulo.rectTransform.pivot = new Vector2(0f, 1f);
            titulo.rectTransform.anchoredPosition = new Vector2(32f, -18f);
            titulo.rectTransform.sizeDelta = new Vector2(680f, 56f);

            paso = Texto(caja, "", 26, Hex("#8a6848"), FontStyle.Normal, TextAnchor.MiddleRight);
            paso.rectTransform.anchorMin = new Vector2(1f, 1f);
            paso.rectTransform.anchorMax = new Vector2(1f, 1f);
            paso.rectTransform.pivot = new Vector2(1f, 1f);
            paso.rectTransform.anchoredPosition = new Vector2(-28f, -22f);
            paso.rectTransform.sizeDelta = new Vector2(280f, 52f);

            relato = Texto(caja, "", 34, Hex("#5a3b24"), FontStyle.Normal, TextAnchor.UpperLeft);
            relato.lineSpacing = 1.05f;
            relato.rectTransform.anchorMin = new Vector2(0f, 0f);
            relato.rectTransform.anchorMax = new Vector2(1f, 1f);
            relato.rectTransform.offsetMin = new Vector2(32f, 108f);
            relato.rectTransform.offsetMax = new Vector2(-32f, -80f);

            var saltarImagen = Imagen(caja, "Saltar", SpriteDe("button_beige"), new Vector2(-300f, -96f), new Vector2(320f, 70f));
            saltarImagen.type = Image.Type.Sliced;
            saltarImagen.raycastTarget = true;
            var saltar = saltarImagen.gameObject.AddComponent<Button>();
            saltar.targetGraphic = saltarImagen;
            saltar.onClick.AddListener(Saltar);
            var saltarTexto = Texto(saltarImagen.transform, "Saltar historia", 30, Hex("#5a3b24"), FontStyle.Normal, TextAnchor.MiddleCenter);
            Estirar(saltarTexto.rectTransform);

            var seguirImagen = Imagen(caja, "Seguir", SpriteDe("button_brown"), new Vector2(310f, -96f), new Vector2(260f, 70f));
            seguirImagen.type = Image.Type.Sliced;
            seguirImagen.raycastTarget = true;
            seguir = seguirImagen.gameObject.AddComponent<Button>();
            seguir.targetGraphic = seguirImagen;
            seguir.onClick.AddListener(Seguir);
            seguirTexto = Texto(seguirImagen.transform, "Seguir", 32, Hex("#f6efe4"), FontStyle.Normal, TextAnchor.MiddleCenter);
            Estirar(seguirTexto.rectTransform);
        }

        void TrazarCamino(Transform camino)
        {
            for (int i = 0; i < marcas.Length - 1; i++)
            {
                Vector2 origen = marcas[i].anchoredPosition;
                Vector2 destino = marcas[i + 1].anchoredPosition;
                Vector2 vector = destino - origen;
                int cantidad = Mathf.Max(1, Mathf.RoundToInt(vector.magnitude / 46f));
                for (int m = 1; m < cantidad; m++)
                {
                    float t = m / (float)cantidad;
                    Vector2 punto = origen + vector * t;
                    var gota = Imagen(camino, "Trazo", disco, punto, new Vector2(14f, 14f));
                    gota.color = new Color(1f, 0.9f, 0.62f, 0.8f);
                }
            }
        }

        void Seguir()
        {
            if (cerrado || enCamino)
                return;

            if (indice >= Cantidad - 1)
            {
                Cerrar();
                return;
            }

            StartCoroutine(Recorrer(indice + 1));
        }

        void Saltar()
        {
            if (cerrado)
                return;
            StopAllCoroutines();
            Cerrar();
        }

        IEnumerator Recorrer(int siguiente)
        {
            enCamino = true;
            seguir.interactable = false;

            Vector2 origenAnillo = anillo.anchoredPosition;
            Vector2 destinoAnillo = marcas[siguiente].anchoredPosition;
            bool llego = mishi == null;
            if (mishi != null)
            {
                mishi.ExplicarEn(PiesDe(destinoAnillo), CaraDe(siguiente), () => llego = true);
                TraerMishiAlFrente();
            }

            float t = 0f;
            while (t < 1f || !llego)
            {
                t = Mathf.Min(1f, t + Time.deltaTime / 0.75f);
                anillo.anchoredPosition = Vector2.Lerp(origenAnillo, destinoAnillo, t);
                yield return null;
                if (t >= 1f && llego)
                    break;
            }

            indice = siguiente;
            anillo.anchoredPosition = destinoAnillo;
            MostrarTexto(indice);
            enCamino = false;
            seguir.interactable = true;
        }

        void MostrarTexto(int cual)
        {
            titulo.text = Titulo(cual);
            relato.text = Relato(cual);
            paso.text = "Capítulo 1   ·   " + (cual + 1) + " / " + Cantidad;
            seguirTexto.text = cual >= Cantidad - 1 ? "Empezar" : "Seguir";
            AcomodarCaja(Posicion(cual));
        }

        void TraerMishiAlFrente()
        {
            if (mishi == null)
                return;
            Transform sombra = mishi.transform.parent.Find("SombraMishi");
            if (sombra != null)
                sombra.SetAsLastSibling();
            mishi.transform.SetAsLastSibling();
        }

        int CaraDe(int cual)
        {
            return Caras[Mathf.Clamp(cual, 0, Caras.Length - 1)];
        }

        Vector2 PiesDe(Vector2 punto)
        {
            Vector2 lado = LadoDe(punto);
            lado.y -= 80f;
            lado.x = Mathf.Clamp(lado.x, -820f, 820f);
            lado.y = Mathf.Clamp(lado.y, -460f, 400f);
            return lado;
        }

        void TomarPuntosDeLaEscena()
        {
            escena = FindObjectsByType<PuntoDeCamino>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (escena == null || escena.Length == 0)
            {
                escena = null;
                return;
            }

            System.Array.Sort(escena, (a, b) => a.orden.CompareTo(b.orden));
        }

        int Cantidad => escena != null ? escena.Length : Historia.Length;

        Vector2 Posicion(int i)
        {
            if (escena == null)
                return Historia[i].punto;
            Vector3 puesto = escena[i].transform.position;
            return new Vector2(puesto.x * 100f, puesto.y * 100f);
        }

        string Titulo(int i)
        {
            return escena != null ? escena[i].titulo : Historia[i].titulo;
        }

        string Relato(int i)
        {
            string texto = escena != null ? escena[i].relato : Historia[i].relato;
            return texto
                .Replace("{nombre}", AjustesJuego.NombreParaHablar)
                .Replace("{companero}", AjustesJuego.Companero)
                .Replace("{rol}", AjustesJuego.Rol)
                .Replace("{el}", AjustesJuego.Articulo);
        }

        void AcomodarCaja(Vector2 punto)
        {
            if (punto.y >= -80f)
                caja.anchoredPosition = new Vector2(120f, -320f);
            else if (punto.x > 400f)
                caja.anchoredPosition = new Vector2(-220f, 30f);
            else
                caja.anchoredPosition = new Vector2(260f, 30f);
        }

        static Vector2 LadoDe(Vector2 punto)
        {
            float x = punto.x > 80f ? punto.x - 150f : punto.x + 150f;
            float y = punto.y > 220f ? punto.y - 130f : punto.y + 120f;
            return new Vector2(x, y);
        }

        void Cerrar()
        {
            if (cerrado)
                return;
            cerrado = true;
            if (raiz != null)
                Destroy(raiz.gameObject);
            Action terminar = alTerminar;
            alTerminar = null;
            if (terminar != null)
                terminar();
            Destroy(this);
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

        Text Texto(Transform padre, string contenido, int tamano, Color color, FontStyle estilo, TextAnchor ancla)
        {
            var go = new GameObject("Texto", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(padre, false);
            var texto = go.GetComponent<Text>();
            texto.font = font;
            texto.text = contenido;
            texto.fontSize = tamano;
            texto.fontStyle = estilo;
            texto.color = color;
            texto.alignment = ancla;
            texto.horizontalOverflow = HorizontalWrapMode.Wrap;
            texto.verticalOverflow = VerticalWrapMode.Overflow;
            texto.raycastTarget = false;
            return texto;
        }

        static void Estirar(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        Sprite SpriteDe(string nombre)
        {
            var sprite = Resources.Load<Sprite>("FluxLab/" + nombre);
            return sprite != null ? sprite : solido;
        }

        static Color Hex(string valor)
        {
            ColorUtility.TryParseHtmlString(valor, out Color color);
            return color;
        }

        static Sprite Cuadrado()
        {
            var textura = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var pixeles = new Color[16];
            for (int i = 0; i < pixeles.Length; i++)
                pixeles[i] = Color.white;
            textura.SetPixels(pixeles);
            textura.Apply();
            return Sprite.Create(textura, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
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
    }
}
