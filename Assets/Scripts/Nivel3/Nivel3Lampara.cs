using UnityEngine;
using UnityEngine.UI;

namespace FluxLab
{
    /// <summary>
    /// Una lámpara colgante del nivel 3: se mece sola, su llama titila, cambia de color según la ronda
    /// (dorada, azul lunar, violeta mágica) y reacciona con un destello cuando entra un ingrediente al caldero.
    /// También sirve como simple luz parpadeante (sin dibujo) para lámparas que ya están pintadas en el fondo.
    /// </summary>
    public class Nivel3Lampara : MonoBehaviour
    {
        // El dibujo azul y el violeta están pintados un poco más pequeños que el dorado: se igualan.
        static readonly float[] AjusteTamano = { 1f, 1.13f, 1.13f };
        static readonly Color[] ColorLuz =
        {
            new Color(1f, 0.72f, 0.28f),    // dorada
            new Color(0.45f, 0.7f, 1f),     // azul lunar
            new Color(0.78f, 0.45f, 1f)     // violeta mágica
        };

        Transform padre;
        RectTransform rect;
        Image cuerpo;
        Image luz;
        Sprite[] dibujos;
        float escala;
        float fase;
        int modo;                // 0 dorada, 1 azul, 2 violeta
        float reaccion;          // 1 al recibir un destello y baja solo
        float temblor;           // sacudida fuerte (cuando explota el caldero)
        Color colorReaccion = Color.white;
        float tamanoLuz;
        bool soloLuz;

        /// <summary>Lámpara con dibujo, colgando de su anillo (el anillo queda en la posición dada).</summary>
        public static Nivel3Lampara Crear(Transform padre, Vector2 anillo, float escala)
        {
            var go = new GameObject("Lampara", typeof(RectTransform));
            go.transform.SetParent(padre, false);
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 1f); // gira alrededor del anillo, como una lámpara colgada de verdad
            r.anchoredPosition = anillo;

            var lampara = go.AddComponent<Nivel3Lampara>();
            lampara.padre = padre;
            lampara.rect = r;
            lampara.escala = escala;
            lampara.fase = Random.Range(0f, 6.28f);

            lampara.dibujos = new[]
            {
                Nivel3UI.SpriteOpcional("FluxLab/Lampara/lampara_dorada_0"),
                Nivel3UI.SpriteOpcional("FluxLab/Lampara/lampara_azul"),
                Nivel3UI.SpriteOpcional("FluxLab/Lampara/lampara_violeta")
            };

            Sprite primero = lampara.dibujos[0];
            Vector2 tamano = primero != null ? primero.rect.size * escala : new Vector2(250f, 270f) * escala;
            r.sizeDelta = tamano;

            lampara.cuerpo = Nivel3UI.Imagen(go.transform, "Cuerpo", primero != null ? primero : Nivel3UI.Solido(), Vector2.zero, tamano, true);
            lampara.cuerpo.enabled = primero != null;

            // El dibujo cuelga del anillo (borde de arriba): así, aunque cada color tenga un tamaño distinto, el anillo no se mueve
            var rc = lampara.cuerpo.rectTransform;
            rc.anchorMin = rc.anchorMax = new Vector2(0.5f, 1f);
            rc.pivot = new Vector2(0.5f, 1f);
            rc.anchoredPosition = Vector2.zero;

            // La luz va delante del dibujo, en el centro de la llama
            lampara.tamanoLuz = 190f * escala;
            lampara.luz = Nivel3UI.Imagen(go.transform, "Luz", Nivel3UI.LuzSuave(), new Vector2(0f, -4f * escala), Vector2.one * lampara.tamanoLuz);
            lampara.luz.color = new Color(1f, 0.72f, 0.28f, 0.2f);
            return lampara;
        }

        /// <summary>Solo una luz que titila (para una lámpara que ya está en el dibujo del fondo).</summary>
        public static Nivel3Lampara CrearSoloLuz(Transform padre, Vector2 centro, float tamano)
        {
            var go = new GameObject("LuzParpadeante", typeof(RectTransform));
            go.transform.SetParent(padre, false);
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = centro;
            r.sizeDelta = Vector2.zero;

            var lampara = go.AddComponent<Nivel3Lampara>();
            lampara.padre = padre;
            lampara.rect = r;
            lampara.soloLuz = true;
            lampara.escala = 1f;
            lampara.fase = Random.Range(0f, 6.28f);
            lampara.tamanoLuz = tamano;
            lampara.luz = Nivel3UI.Imagen(go.transform, "Luz", Nivel3UI.LuzSuave(), Vector2.zero, Vector2.one * tamano);
            lampara.luz.color = new Color(1f, 0.72f, 0.28f, 0.2f);
            return lampara;
        }

        /// <summary>Cambia el color de la lámpara: 0 dorada, 1 azul lunar, 2 violeta mágica.</summary>
        public void CambiarColor(int nuevoModo)
        {
            modo = Mathf.Clamp(nuevoModo, 0, 2);
            if (cuerpo != null && !soloLuz && dibujos[modo] != null)
            {
                cuerpo.sprite = dibujos[modo];
                float ajuste = AjusteTamano[modo];
                cuerpo.rectTransform.sizeDelta = dibujos[modo].rect.size * escala * ajuste;
            }
        }

        /// <summary>Un destello: la luz se enciende con el color dado, salen chispas y la lámpara se balancea un poco.</summary>
        public void Reaccionar(Color color, float fuerza)
        {
            reaccion = Mathf.Clamp01(fuerza);
            colorReaccion = Color.Lerp(color, ColorLuz[modo], 0.35f);

            int cantidad = Mathf.RoundToInt(5 + 8 * fuerza);
            Sprite estrella = FrascoBrillante.Estrella();
            Vector2 centro = rect.anchoredPosition + new Vector2(0f, -rect.sizeDelta.y * 0.5f);
            for (int i = 0; i < cantidad; i++)
            {
                float angulo = Random.Range(0f, Mathf.PI * 2f);
                float rapidez = Random.Range(60f, 190f) * (0.6f + fuerza);
                Particula.Crear(padre, estrella, centro + new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo)) * 40f * escala,
                    Vector2.one * Random.Range(16f, 34f), Color.Lerp(colorReaccion, Color.white, 0.5f),
                    new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo)) * rapidez, 40f, Random.Range(0.7f, 1.2f), Random.Range(-200f, 200f), 0.1f);
            }
        }

        /// <summary>La lámpara se sacude fuerte (cuando explota el caldero).</summary>
        public void Sacudir()
        {
            temblor = 1f;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            float t = Time.time;
            reaccion = Mathf.MoveTowards(reaccion, 0f, dt * 1.1f);
            temblor = Mathf.MoveTowards(temblor, 0f, dt * 1.6f);

            // La llama titila con un ruido suave (nunca igual)
            float parpadeo = Mathf.PerlinNoise(t * 2.1f + fase, 3.7f);

            Color baseLuz = ColorLuz[modo];
            Color color = Color.Lerp(baseLuz, colorReaccion, reaccion);
            luz.color = new Color(color.r, color.g, color.b, 0.12f + 0.16f * parpadeo + 0.5f * reaccion);
            luz.rectTransform.sizeDelta = Vector2.one * tamanoLuz * (0.95f + 0.1f * parpadeo + 0.5f * reaccion);

            if (soloLuz)
                return;

            // Balanceo: suave siempre, más con un destello y muy fuerte con la explosión
            float meciendo = Mathf.Sin(t * 1.25f + fase) * 3f;
            float destello = Mathf.Sin(t * 13f) * 3.5f * reaccion;
            float sacudida = Mathf.Sin(t * 30f) * 9f * temblor;
            rect.localRotation = Quaternion.Euler(0f, 0f, meciendo + destello + sacudida);
        }
    }
}
