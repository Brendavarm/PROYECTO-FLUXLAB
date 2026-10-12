using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FluxLab.Nivel2
{
    /// <summary>
    /// Mishi, el gato del laboratorio, en versión cute: entra saltando, respira,
    /// ladea la cabeza, maúlla con un globito y corazones, y si le haces clic, maúlla.
    /// </summary>
    public class GatoMishi : MonoBehaviour, IPointerClickHandler
    {
        static readonly string[] Maullidos = { "Nivel2/miau1", "Nivel2/miau2", "Nivel2/miau3" };
        static readonly string[] Frases = { "¡Miau!", "¡Prrr~!", "¡Suerte, miau!", "¡Miau miau!" };

        EscenaFrascos escena;
        ParticulasUI particulas;
        RectTransform rect;
        RectTransform cuerpo;
        RectTransform globo;
        Text textoGlobo;
        Vector2 sitio;
        float relojGlobo;
        float salto;          // 0..1 mientras salta de alegría
        float aplastado;      // al aterrizar
        bool caminando;

        public static GatoMishi Crear(Transform padre, EscenaFrascos escena, ParticulasUI particulas, float alto)
        {
            var raiz = new GameObject("Mishi", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            raiz.transform.SetParent(padre, false);
            var gato = raiz.AddComponent<GatoMishi>();
            gato.escena = escena;
            gato.particulas = particulas;
            gato.rect = raiz.GetComponent<RectTransform>();
            gato.rect.sizeDelta = new Vector2(alto, alto);

            // Zona de clic invisible del tamaño del gato.
            var zona = raiz.GetComponent<Image>();
            zona.color = new Color(1f, 1f, 1f, 0f);
            zona.raycastTarget = true;

            Sprite sprite = ImagenesFrascos.Mishi;
            if (sprite == null)
                sprite = escena.Spr("mishi");
            var imagen = escena.Imagen(raiz.transform, "Cuerpo", sprite, Vector2.zero, new Vector2(alto, alto), true);
            gato.cuerpo = imagen.rectTransform;
            gato.cuerpo.pivot = new Vector2(0.5f, 0.05f);   // se estira desde las patas
            gato.cuerpo.anchoredPosition = new Vector2(0f, -alto * 0.45f);

            var fondoGlobo = escena.Imagen(raiz.transform, "Globo", escena.Spr("panel_beige"), new Vector2(-alto * 0.55f, alto * 0.75f), new Vector2(230f, 74f), false);
            fondoGlobo.type = Image.Type.Sliced;
            gato.globo = fondoGlobo.rectTransform;
            var colita = escena.Imagen(gato.globo, "Colita", FormasUI.Punto, new Vector2(70f, -38f), new Vector2(22f, 22f), false);
            colita.color = new Color(0.98f, 0.93f, 0.84f, 1f);
            gato.textoGlobo = escena.Texto(gato.globo, "", 28, new Color(0.35f, 0.2f, 0.12f, 1f), FontStyle.Bold);
            gato.textoGlobo.rectTransform.sizeDelta = new Vector2(220f, 64f);
            gato.globo.localScale = Vector3.zero;
            return gato;
        }

        /// <summary>Llega saltando desde "desde" hasta "hasta" (coordenadas del padre).</summary>
        public IEnumerator Entrar(Vector2 desde, Vector2 hasta, float duracion = 0.7f)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();   // delante de todo, para que su globito no quede tapado
            caminando = true;
            sitio = hasta;
            float reloj = 0f;
            while (reloj < duracion)
            {
                reloj += Time.deltaTime;
                float k = Mathf.Clamp01(reloj / duracion);
                float arco = Mathf.Sin(k * Mathf.PI) * 160f;
                rect.anchoredPosition = Vector2.Lerp(desde, hasta, k) + new Vector2(0f, arco);
                rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(k * Mathf.PI) * (desde.x > hasta.x ? 12f : -12f));
                yield return null;
            }

            rect.anchoredPosition = hasta;
            rect.localRotation = Quaternion.identity;
            caminando = false;
            aplastado = 1f;
            particulas.Emitir(new ParticulasUI.Rafaga
            {
                forma = FormasUI.Humo, cantidad = 6, centro = Local(hasta + new Vector2(0f, -rect.sizeDelta.y * 0.45f)), dispersion = new Vector2(40f, 4f),
                color = new Color(1f, 0.95f, 0.85f, 0.7f), angulo = new Vector2(0f, 180f), rapidez = new Vector2(40f, 90f),
                vida = new Vector2(0.4f, 0.6f), tamano = new Vector2(30f, 50f), crece = 0.8f
            });
        }

        public void Maullar(string frase = null)
        {
            escena.Sonar(Maullidos[Random.Range(0, Maullidos.Length)], 0.9f);
            textoGlobo.text = frase ?? Frases[Random.Range(0, Frases.Length)];
            relojGlobo = 1.8f;
            aplastado = 0.7f;
            for (int i = 0; i < 5; i++)
            {
                Vector2 desde = Local(rect.anchoredPosition) + new Vector2(Random.Range(-40f, 20f), rect.sizeDelta.y * 0.2f);
                particulas.Una(FormasUI.Corazon, desde, Vector2.one * Random.Range(22f, 36f), new Color(1f, Random.Range(0.45f, 0.6f), 0.7f, 1f),
                    new Vector2(Random.Range(-50f, 50f), Random.Range(90f, 160f)), Random.Range(1f, 1.5f), -20f, Random.Range(-60f, 60f), 0.3f);
            }
        }

        public void SaltarDeAlegria()
        {
            salto = 1f;
            Maullar("¡Bien, miau!");
        }

        public void OnPointerClick(PointerEventData evento)
        {
            if (Time.timeScale > 0f)
                Maullar();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            float t = Time.time;
            aplastado = Mathf.MoveTowards(aplastado, 0f, dt * 2.5f);
            salto = Mathf.MoveTowards(salto, 0f, dt * 1.6f);

            if (!caminando)
            {
                float brinco = Mathf.Sin((1f - salto) * Mathf.PI) * 70f * (salto > 0f ? 1f : 0f);
                rect.anchoredPosition = sitio + new Vector2(0f, brinco);
                rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.3f) * 4f + (salto > 0f ? Mathf.Sin(t * 20f) * 8f * salto : 0f));
            }

            // Respira, y al aterrizar o maullar se aplasta un poquito (squash & stretch).
            float respirar = Mathf.Sin(t * 2.6f) * 0.03f;
            float aplaste = Mathf.Sin(aplastado * Mathf.PI * 3f) * 0.18f * aplastado;
            cuerpo.localScale = new Vector3(1f + aplaste - respirar * 0.5f, 1f - aplaste + respirar, 1f);

            relojGlobo -= dt;
            float objetivo = relojGlobo > 0f ? 1f : 0f;
            float actual = globo.localScale.x;
            float nuevo = Mathf.MoveTowards(actual, objetivo, dt * 7f);
            globo.localScale = Vector3.one * (objetivo > 0f ? Mathf.Min(1.1f, nuevo + Mathf.Sin(nuevo * Mathf.PI) * 0.15f) : nuevo);
        }

        Vector2 Local(Vector2 enPadre)
        {
            Vector3 mundo = rect.parent.TransformPoint(enPadre);
            Vector3 local = particulas.transform.InverseTransformPoint(mundo);
            return new Vector2(local.x, local.y);
        }
    }
}
