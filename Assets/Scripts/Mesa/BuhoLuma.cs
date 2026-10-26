using UnityEngine;
using UnityEngine.UI;

namespace FluxLab
{
    /// <summary>
    /// Luma, el búho del taller del aprendiz. Se mece, mira la receta y reacciona al caldero.
    /// </summary>
    public class BuhoLuma : MonoBehaviour
    {
        RectTransform cuerpo;
        RectTransform sombra;
        Text voz;
        Vector2 pies;
        float animo;
        float salto;
        float mira = 1f;
        float vozRestante;
        float esperaSalto = 2.5f;

        public static BuhoLuma Crear(Transform padre, Vector2 pies, float alto)
        {
            Texture2D textura = Nivel3UI.LeerTextura("Companeros/luma.png");
            if (textura == null)
                return null;

            var raiz = new GameObject("Luma", typeof(RectTransform));
            raiz.transform.SetParent(padre, false);
            var buho = raiz.AddComponent<BuhoLuma>();
            buho.pies = pies;
            buho.Armar(textura, alto);
            buho.Decir("¡Hola!");
            return buho;
        }

        void Armar(Texture2D textura, float alto)
        {
            float ancho = alto * textura.width / Mathf.Max(1f, textura.height);
            var sprite = Sprite.Create(textura, new Rect(0f, 0f, textura.width, textura.height), new Vector2(0.5f, 0.04f), 100f);

            var sombraGo = new GameObject("Sombra", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            sombraGo.transform.SetParent(transform, false);
            sombra = sombraGo.GetComponent<RectTransform>();
            sombra.anchorMin = sombra.anchorMax = new Vector2(0.5f, 0.5f);
            sombra.pivot = new Vector2(0.5f, 0.5f);
            sombra.anchoredPosition = pies + new Vector2(8f, 8f);
            sombra.sizeDelta = new Vector2(ancho * 0.62f, 34f);
            var mancha = sombraGo.GetComponent<Image>();
            mancha.sprite = Nivel3UI.LuzSuave();
            mancha.color = new Color(0.08f, 0.04f, 0.03f, 0.38f);
            mancha.raycastTarget = false;

            var cuerpoGo = new GameObject("Cuerpo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            cuerpoGo.transform.SetParent(transform, false);
            cuerpo = cuerpoGo.GetComponent<RectTransform>();
            cuerpo.anchorMin = cuerpo.anchorMax = new Vector2(0.5f, 0.5f);
            cuerpo.pivot = new Vector2(0.5f, 0.04f);
            cuerpo.anchoredPosition = pies;
            cuerpo.sizeDelta = new Vector2(ancho, alto);
            var imagen = cuerpoGo.GetComponent<Image>();
            imagen.sprite = sprite;
            imagen.preserveAspect = true;
            imagen.raycastTarget = false;

            var vozGo = new GameObject("Voz", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Outline));
            vozGo.transform.SetParent(cuerpo, false);
            var marco = vozGo.GetComponent<RectTransform>();
            marco.anchorMin = marco.anchorMax = new Vector2(0.5f, 1f);
            marco.pivot = new Vector2(0.5f, 0f);
            marco.anchoredPosition = new Vector2(0f, 8f);
            marco.sizeDelta = new Vector2(280f, 54f);
            voz = vozGo.GetComponent<Text>();
            voz.font = Nivel3UI.Fuente;
            voz.fontSize = 32;
            voz.alignment = TextAnchor.MiddleCenter;
            voz.color = new Color(1f, 0.96f, 0.9f, 0f);
            voz.horizontalOverflow = HorizontalWrapMode.Overflow;
            voz.raycastTarget = false;
            var borde = vozGo.GetComponent<Outline>();
            borde.effectColor = new Color(0.2f, 0.08f, 0.04f, 0.9f);
            borde.effectDistance = new Vector2(2f, -2f);
        }

        public void MirarReceta()
        {
            mira = -1f;
            Decir("Mira bien...");
        }

        public void MirarCaldero()
        {
            mira = 1f;
            Decir("Tu turno");
        }

        public void Verter()
        {
            salto = Mathf.Max(salto, 1f);
            Decir("¡al caldero!");
        }

        public void Festejar()
        {
            animo = 1.5f;
            salto = 1.25f;
            Decir("¡Eso es!");
        }

        public void Asustarse()
        {
            animo = -1.35f;
            salto = 0.35f;
            Decir("¡uy!");
        }

        void Decir(string frase)
        {
            if (voz == null)
                return;
            voz.text = frase;
            vozRestante = 1.35f;
        }

        void Update()
        {
            if (cuerpo == null)
                return;

            animo = Mathf.MoveTowards(animo, 0f, Time.deltaTime * 0.45f);
            salto = Mathf.Max(0f, salto - Time.deltaTime * 1.7f);
            esperaSalto -= Time.deltaTime;
            if (esperaSalto <= 0f && salto <= 0.05f && animo > -0.2f)
            {
                salto = 0.72f;
                esperaSalto = Random.Range(3.4f, 6.2f);
            }

            float sube = Mathf.Sin(Mathf.Clamp01(salto) * Mathf.PI);
            float aire = sube * (animo > 0.4f ? 46f : 26f);
            float mece = Mathf.Sin(Time.time * 2.1f) * 5f;
            float encoge = animo < 0f ? 1f + animo * 0.1f : 1f + Mathf.Max(0f, animo) * 0.04f;
            float estira = 1f + sube * 0.1f;
            cuerpo.anchoredPosition = pies + new Vector2(Mathf.Sin(Time.time * 1.3f) * 3f, mece + aire);
            cuerpo.localScale = new Vector3(encoge / estira, encoge * estira, 1f);
            float inclinacion = Mathf.Lerp(-10f, 8f, (mira + 1f) * 0.5f);
            cuerpo.localRotation = Quaternion.Euler(0f, 0f, inclinacion + Mathf.Sin(Time.time * 1.6f) * 2.2f);

            if (sombra != null)
            {
                float aplasta = 1f - sube * 0.35f;
                sombra.localScale = new Vector3(aplasta, aplasta, 1f);
                Color mancha = sombra.GetComponent<Image>().color;
                mancha.a = 0.38f * (1f - sube * 0.45f);
                sombra.GetComponent<Image>().color = mancha;
            }

            if (voz != null)
            {
                vozRestante -= Time.deltaTime;
                float alfa = Mathf.Clamp01(vozRestante / 0.35f);
                if (vozRestante > 1f)
                    alfa = 1f;
                Color c = voz.color;
                c.a = alfa;
                voz.color = c;
                var borde = voz.GetComponent<Outline>();
                if (borde != null)
                {
                    Color tinta = borde.effectColor;
                    tinta.a = 0.9f * alfa;
                    borde.effectColor = tinta;
                }
            }
        }
    }
}
