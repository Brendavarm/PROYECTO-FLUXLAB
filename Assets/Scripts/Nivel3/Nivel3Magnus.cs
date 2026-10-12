using UnityEngine;
using UnityEngine.UI;

namespace FluxLab
{
    /// <summary>
    /// Magnus, el hechicero, igual que en la mesa de Brenda: mismo dibujo (con su pelo y su túnica)
    /// y los mismos movimientos. Aquí solo se coloca sobre la mesa del nivel 3.
    /// </summary>
    public class Nivel3Magnus : MonoBehaviour
    {
        public enum Animo
        {
            Quieto,
            Observa,
            Remueve,
            Festeja,
            SeEquivoca
        }

        RectTransform rect;
        RectTransform sombra;
        Vector2 baseP;
        Animo animo = Animo.Quieto;
        float reloj;
        float duracion;
        Animo despues = Animo.Quieto;

        public static Nivel3Magnus Crear(Transform padre, float x, float ySuelo)
        {
            // Sombra bajo los pies
            var sombraImg = Nivel3UI.Imagen(padre, "SombraMagnus", FrascoQuebradizo.Disco(), new Vector2(x, ySuelo - 4f), new Vector2(130f, 26f));
            sombraImg.color = new Color(0.08f, 0.04f, 0.03f, 0.35f);

            var go = new GameObject("Magnus", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            go.transform.SetParent(padre, false);
            var cuerpo = go.GetComponent<RawImage>();
            cuerpo.texture = AspectoMagnus.Crear();
            if (cuerpo.texture == null)
                cuerpo.texture = Resources.Load<Texture2D>("FluxLab/magnus");
            cuerpo.raycastTarget = false;

            var r = go.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(140f, 380f);

            var magnus = go.AddComponent<Nivel3Magnus>();
            magnus.rect = r;
            magnus.sombra = sombraImg.rectTransform;
            magnus.baseP = new Vector2(x, ySuelo + 190f);   // el centro queda a media altura sobre los pies
            r.anchoredPosition = magnus.baseP;

            if (AjustesJuego.Accesorio == 1)
            {
                var frasco = Nivel3UI.Imagen(go.transform, "Frasco", Nivel3UI.Spr("frasco_morado"), new Vector2(-36f, -28f), new Vector2(36f, 48f), true);
                frasco.raycastTarget = false;
            }

            return magnus;
        }

        public void Poner(Animo nuevo, float segundos, Animo luego = Animo.Quieto)
        {
            animo = nuevo;
            reloj = 0f;
            duracion = segundos;
            despues = luego;
        }

        void Update()
        {
            reloj += Time.deltaTime;
            float avance = duracion <= 0.01f ? 1f : Mathf.Clamp01(reloj / duracion);
            float respirar = Mathf.Sin(Time.time * 2.3f);
            Vector2 posicion = baseP + new Vector2(0f, respirar * 4f);
            float giro = 0f;
            float ancho = 1f - respirar * 0.012f;
            float alto = 1f + respirar * 0.02f;

            if (animo == Animo.Observa)
            {
                posicion.y += 8f;
                giro = -8f + Mathf.Sin(Time.time * 1.4f) * 3f;
            }
            else if (animo == Animo.Remueve)
            {
                float ola = Mathf.Sin(avance * Mathf.PI * 2f);
                posicion.x += Mathf.Sin(avance * Mathf.PI) * 34f;
                posicion.y += Mathf.Abs(ola) * 6f;
                giro = ola * 11f;
                if (avance >= 1f)
                    Poner(despues, 0f);
            }
            else if (animo == Animo.Festeja)
            {
                float salto = Mathf.Sin(avance * Mathf.PI);
                posicion.y += salto * 42f;
                ancho *= 1f - salto * 0.05f;
                alto *= 1f + salto * 0.08f;
                giro = Mathf.Sin(avance * Mathf.PI * 2f) * 7f;
                if (avance >= 1f)
                    Poner(Animo.Quieto, 0f);
            }
            else if (animo == Animo.SeEquivoca)
            {
                float calma = 1f - avance;
                giro = Mathf.Sin(avance * Mathf.PI * 6f) * 12f * calma;
                posicion.y -= avance * 6f;
                if (avance >= 1f)
                    Poner(Animo.Quieto, 0f);
            }

            rect.anchoredPosition = posicion;
            rect.localRotation = Quaternion.Euler(0f, 0f, giro);
            rect.localScale = new Vector3(ancho, alto, 1f);

            float elevacion = Mathf.Max(0f, posicion.y - baseP.y);
            float escala = Mathf.Clamp(1f - elevacion / 70f, 0.62f, 1f);
            sombra.anchoredPosition = new Vector2(posicion.x, baseP.y - 190f - 4f);
            sombra.localScale = new Vector3(escala, escala, 1f);
        }
    }
}
