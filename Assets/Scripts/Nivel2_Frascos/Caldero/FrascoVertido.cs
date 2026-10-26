using UnityEngine;
using UnityEngine.UI;

namespace FluxLab.Nivel2
{
    /// <summary>
    /// Animación de echar un frasco: sale de la carta, vuela en arco hasta el caldero,
    /// se inclina, cae un chorro del color del ingrediente y se desvanece.
    /// "alCaer" se llama cuando el chorro toca el líquido.
    /// </summary>
    public class FrascoVertido : MonoBehaviour
    {
        // Partes de la animación (de 0 a 1 sobre la duración total).
        const float FinVuelo = 0.4f;
        const float FinInclinar = 0.55f;
        const float Impacto = 0.66f;
        const float FinChorro = 0.85f;
        const float Inclinacion = 118f;
        const float CaidaChorro = 0.3f;

        static readonly Vector2 Tamano = new Vector2(64f, 112f);

        RectTransform rect;
        Image imagen;
        ParticulasUI efectos;
        Vector2 desde;
        Vector2 boca;
        Vector2 punto;
        Color color;
        float duracion;
        float t;
        float relojGotas;
        bool cayo;
        System.Action alCaer;

        public static void Lanzar(Transform capa, ParticulasUI efectos, Sprite frasco, Vector2 desde, Vector2 boca, Color color, float duracion, System.Action alCaer)
        {
            var go = new GameObject("FrascoVertido", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(capa, false);
            var vertido = go.AddComponent<FrascoVertido>();
            vertido.rect = go.GetComponent<RectTransform>();
            vertido.rect.anchorMin = vertido.rect.anchorMax = new Vector2(0.5f, 0.5f);
            vertido.rect.sizeDelta = Tamano;
            vertido.rect.anchoredPosition = desde;
            vertido.imagen = go.GetComponent<Image>();
            vertido.imagen.sprite = frasco;
            vertido.imagen.preserveAspect = true;
            vertido.imagen.raycastTarget = false;
            vertido.efectos = efectos;
            vertido.desde = desde;
            vertido.boca = boca;
            vertido.punto = boca + new Vector2(118f, 104f);
            vertido.color = color;
            vertido.duracion = Mathf.Max(0.2f, duracion);
            vertido.alCaer = alCaer;
        }

        void Update()
        {
            t += Time.deltaTime / duracion;

            if (t < FinVuelo)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / FinVuelo);
                Vector2 alto = (desde + punto) * 0.5f + new Vector2(0f, 220f);
                rect.anchoredPosition = Curva(desde, alto, punto, k);
                rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, 20f, k));
                rect.localScale = Vector3.one * Mathf.Lerp(0.8f, 1.1f, k);
            }
            else if (t < FinInclinar)
            {
                float k = Mathf.SmoothStep(0f, 1f, (t - FinVuelo) / (FinInclinar - FinVuelo));
                rect.anchoredPosition = punto;
                rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(20f, Inclinacion, k));
            }
            else if (t < FinChorro)
            {
                rect.localRotation = Quaternion.Euler(0f, 0f, Inclinacion + Mathf.Sin(t * 60f) * 3f);
                Gotear();
            }
            else
            {
                float k = Mathf.Clamp01((t - FinChorro) / (1f - FinChorro));
                rect.anchoredPosition = punto + new Vector2(10f, 30f) * k;
                rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(Inclinacion, 60f, k));
                imagen.color = new Color(1f, 1f, 1f, 1f - k);
            }

            if (!cayo && t >= Impacto)
                Caer();

            if (t >= 1f)
            {
                Caer();
                Destroy(gameObject);
            }
        }

        void Gotear()
        {
            relojGotas -= Time.deltaTime;
            if (relojGotas > 0f)
                return;
            relojGotas = 1f / 70f;

            // La boca del frasco: la punta de arriba, girada con el frasco.
            float angulo = rect.localEulerAngles.z * Mathf.Deg2Rad;
            float largo = Tamano.y * 0.45f * rect.localScale.y;
            Vector2 pico = rect.anchoredPosition + new Vector2(-Mathf.Sin(angulo), Mathf.Cos(angulo)) * largo;

            Vector2 destino = boca + new Vector2(Random.Range(-14f, 14f), 4f);
            float caida = Mathf.Max(1f, pico.y - destino.y);
            float gravedad = 2f * caida / (CaidaChorro * CaidaChorro);
            Vector2 velocidad = new Vector2((destino.x - pico.x) / CaidaChorro, 0f);
            float lado = Random.Range(8f, 14f);
            efectos.Una(FormasUI.Punto, pico, new Vector2(lado, lado), color, velocidad, CaidaChorro, gravedad, 0f, -0.3f);
        }

        void Caer()
        {
            if (cayo)
                return;
            cayo = true;
            alCaer?.Invoke();
        }

        static Vector2 Curva(Vector2 a, Vector2 b, Vector2 c, float k)
        {
            float u = 1f - k;
            return u * u * a + 2f * u * k * b + k * k * c;
        }
    }
}
