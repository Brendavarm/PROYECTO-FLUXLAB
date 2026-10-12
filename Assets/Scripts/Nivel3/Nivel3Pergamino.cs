using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace FluxLab
{
    /// <summary>
    /// El pergamino de la receta: se abre, enseña los ingredientes (de golpe, uno por uno o en sombras)
    /// y se enrolla hacia arriba. Su contenido lo manda el juego.
    /// Usa Resources/FluxLab/Nivel3/UI/pergamino.png si existe; si no, el pergamino de siempre del menú.
    /// </summary>
    public class Nivel3Pergamino : MonoBehaviour
    {
        const float SegundosAnimacion = 0.35f;
        const int MaxHuecos = 5;
        static readonly Color ColorSombra = new Color(0.1f, 0.06f, 0.18f, 1f);

        // Medidas de cada versión del pergamino. "Centro" es dónde queda el centro del PAPEL respecto al centro del dibujo.
        struct Medidas
        {
            public Vector2 tamano;
            public Vector2 posicion;
            public float desplazamiento;   // el papel está un poco más abajo que el centro del dibujo
            public int letraTitulo, letraIndicacion, letraNombre, letraTemporizador;
            public float yTitulo, yIndicacion, yGemas, yNombre, yTemporizador, yBarra;
            public float lado, gema, anchoBarra, altoBarra, separacion4;
        }

        // Pergamino con arte (papel rústico, grande y liso): todo el dibujo es papel, con los bordes rotos
        static readonly Medidas ConArte = new Medidas
        {
            tamano = new Vector2(700f, 375f), posicion = new Vector2(20f, 348f), desplazamiento = 0f,
            letraTitulo = 40, letraIndicacion = 24, letraNombre = 22, letraTemporizador = 26,
            yTitulo = 118f, yIndicacion = 74f, yGemas = 10f, yNombre = -50f, yTemporizador = -102f, yBarra = -130f,
            lado = 98f, gema = 76f, anchoBarra = 440f, altoBarra = 12f, separacion4 = 134f
        };

        // Pergamino de siempre
        static readonly Medidas Clasico = new Medidas
        {
            tamano = new Vector2(700f, 286f), posicion = new Vector2(30f, 368f), desplazamiento = 0f,
            letraTitulo = 42, letraIndicacion = 25, letraNombre = 24, letraTemporizador = 28,
            yTitulo = 90f, yIndicacion = 48f, yGemas = -6f, yNombre = -54f, yTemporizador = -92f, yBarra = -122f,
            lado = 98f, gema = 76f, anchoBarra = 460f, altoBarra = 14f, separacion4 = 134f
        };

        Medidas m;
        RectTransform pergamino;
        CanvasGroup grupo;
        Text titulo;
        Text indicacion;
        Text temporizador;
        Image barraRelleno;
        GameObject barra;
        Color tinta;

        readonly RectTransform[] huecos = new RectTransform[MaxHuecos];
        readonly Image[] gemas = new Image[MaxHuecos];
        readonly Text[] nombres = new Text[MaxHuecos];

        public static Nivel3Pergamino Crear(Transform padre)
        {
            Sprite arte = Nivel3UI.SpriteUI("pergamino");
            Medidas medidas = arte != null ? ConArte : Clasico;

            var go = new GameObject("Pergamino", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(padre, false);
            var imagen = go.GetComponent<Image>();
            imagen.sprite = arte != null ? arte : Nivel3UI.SpriteDeArchivo("Menu/pergamino.png");
            imagen.preserveAspect = true;
            imagen.raycastTarget = false;

            var r = go.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.sizeDelta = medidas.tamano;
            // Cuelga de su borde de arriba: al achicarse se enrolla hacia arriba, sin moverse de sitio.
            r.pivot = new Vector2(0.5f, 1f);
            r.anchoredPosition = new Vector2(medidas.posicion.x, medidas.posicion.y + medidas.tamano.y * 0.5f);

            var p = go.AddComponent<Nivel3Pergamino>();
            p.m = medidas;
            p.pergamino = r;
            p.grupo = go.AddComponent<CanvasGroup>();
            p.Armar();
            p.Aplicar(0f);
            return p;
        }

        // Los hijos se colocan respecto al centro del dibujo; "y" lo corrige para que quede centrado en el papel.
        float Y(float valor)
        {
            return valor + m.desplazamiento;
        }

        void Armar()
        {
            tinta = Nivel3UI.Hex("#5a3b24");

            titulo = Nivel3UI.Texto(transform, "La receta", m.letraTitulo, tinta, new Vector2(0f, Y(m.yTitulo)), new Vector2(560f, 50f));
            indicacion = Nivel3UI.Texto(transform, "", m.letraIndicacion, tinta, new Vector2(0f, Y(m.yIndicacion)), new Vector2(560f, 36f));

            for (int i = 0; i < MaxHuecos; i++)
            {
                var hueco = Nivel3UI.Vacio(transform, "Hueco" + i, Vector2.zero, new Vector2(120f, 120f));
                huecos[i] = hueco;

                var fondo = Nivel3UI.Imagen(hueco, "Fondo", FrascoQuebradizo.Disco(), Vector2.zero, Vector2.one * m.lado);
                fondo.color = new Color(0.55f, 0.4f, 0.28f, 0.28f);

                gemas[i] = Nivel3UI.Imagen(hueco, "Gema", Nivel3UI.Solido(), Vector2.zero, Vector2.one * m.gema, true);
                gemas[i].enabled = false;

                nombres[i] = Nivel3UI.Texto(hueco, "?", m.letraNombre, tinta, new Vector2(0f, m.yNombre - m.yGemas), new Vector2(150f, 30f));
            }

            temporizador = Nivel3UI.Texto(transform, "", m.letraTemporizador, Nivel3UI.Hex("#6a4b32"), new Vector2(0f, Y(m.yTemporizador)), new Vector2(460f, 32f));

            var pista = Nivel3UI.Imagen(transform, "Tiempo", Nivel3UI.Solido(), new Vector2(0f, Y(m.yBarra)), new Vector2(m.anchoBarra, m.altoBarra));
            pista.color = Nivel3UI.Hex("#d7c3a2");
            barra = pista.gameObject;
            barraRelleno = Nivel3UI.Imagen(pista.transform, "Relleno", Nivel3UI.Solido(), Vector2.zero, new Vector2(m.anchoBarra, m.altoBarra));
            barraRelleno.type = Image.Type.Filled;
            barraRelleno.fillMethod = Image.FillMethod.Horizontal;
            barraRelleno.fillOrigin = (int)Image.OriginHorizontal.Left;
            barraRelleno.color = Nivel3UI.Hex("#7dce63");
            Nivel3UI.Estirar(barraRelleno.rectTransform);
        }

        // ---------------- Contenido ----------------

        /// <summary>Coloca tantos huecos como ingredientes tenga la receta.</summary>
        public void Preparar(int largo)
        {
            float paso = largo >= 5 ? m.separacion4 * 0.84f : largo == 4 ? m.separacion4 : m.separacion4 * 1.2f;
            float centro = (largo - 1) * 0.5f;
            for (int i = 0; i < MaxHuecos; i++)
            {
                bool activo = i < largo;
                huecos[i].gameObject.SetActive(activo);
                if (activo)
                    huecos[i].anchoredPosition = new Vector2((i - centro) * paso, Y(m.yGemas));
                gemas[i].enabled = false;
                nombres[i].text = "?";
            }

            PonerTemporizador("", -1f);
        }

        public void PonerTitulo(string texto)
        {
            titulo.text = texto;
        }

        public void PonerIndicacion(string texto)
        {
            indicacion.text = texto;
        }

        /// <param name="fraccion">De 0 a 1 para la barra; negativo la esconde.</param>
        public void PonerTemporizador(string texto, float fraccion)
        {
            temporizador.text = texto;
            bool hayBarra = fraccion >= 0f;
            barra.SetActive(hayBarra);
            if (hayBarra)
            {
                barraRelleno.fillAmount = fraccion;
                barraRelleno.color = Color.Lerp(Nivel3UI.Hex("#d4654e"), Nivel3UI.Hex("#7dce63"), fraccion);
            }
        }

        /// <summary>Muestra un ingrediente en su hueco; como sombra, solo se ve la forma.</summary>
        public void MostrarGema(int i, Sprite dibujo, string nombre, bool comoSombra)
        {
            gemas[i].sprite = dibujo;
            gemas[i].color = comoSombra ? ColorSombra : Color.white;
            gemas[i].enabled = true;
            nombres[i].text = comoSombra ? "?" : nombre;
        }

        public void OcultarGema(int i)
        {
            gemas[i].enabled = false;
            nombres[i].text = "?";
        }

        public void OcultarTodas()
        {
            for (int i = 0; i < MaxHuecos; i++)
                OcultarGema(i);
        }

        // ---------------- Abrir y enrollar ----------------

        public IEnumerator Abrir()
        {
            yield return Mover(true);
        }

        public IEnumerator Cerrar()
        {
            yield return Mover(false);
        }

        public void Enrollar()
        {
            Aplicar(0f);
        }

        // k = 0 enrollado, k = 1 abierto del todo
        void Aplicar(float k)
        {
            pergamino.localScale = new Vector3(1f, Mathf.Max(0.02f, k), 1f);
            grupo.alpha = Mathf.Clamp01(k * 1.6f);
        }

        IEnumerator Mover(bool abrir)
        {
            float desde = abrir ? 0f : 1f;
            float hasta = abrir ? 1f : 0f;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / SegundosAnimacion;
                Aplicar(Mathf.SmoothStep(desde, hasta, Mathf.Clamp01(t)));
                yield return null;
            }

            Aplicar(hasta);
        }
    }
}
