using UnityEngine;
using UnityEngine.UI;

namespace FluxLab.Nivel2
{
    /// <summary>
    /// Fila de frascos de vida. Cada error rompe uno: tiembla, se cae y queda gris.
    /// </summary>
    public class VidasFrascos : MonoBehaviour
    {
        // Solo si falta FrazcoRosa en NIVEL2-IMAGENES.
        static readonly string[] Respaldo = { "frasco_verde", "frasco_azul", "frasco_rojo", "frasco_morado" };

        const float DuracionRotura = 0.6f;

        Image[] frascos;
        float[] relojRotura;
        Vector2[] posicionBase;
        Text etiqueta;

        public int Total { get; private set; }
        public int Quedan { get; private set; }

        public void Construir(EscenaFrascos escena, int cantidad)
        {
            Total = Mathf.Max(1, cantidad);
            frascos = new Image[Total];
            relojRotura = new float[Total];
            posicionBase = new Vector2[Total];

            etiqueta = escena.TextoMadera(transform, "FRASCOS DE VIDA", 17, new Vector2(0f, 32f), new Vector2(250f, 24f));

            Sprite vida = ImagenesFrascos.Vida;
            float paso = Mathf.Min(54f, 230f / Total);
            float centro = (Total - 1) * 0.5f;
            for (int i = 0; i < Total; i++)
            {
                posicionBase[i] = new Vector2((i - centro) * paso, -8f);
                Sprite sprite = vida != null ? vida : escena.Spr(Respaldo[i % Respaldo.Length]);
                frascos[i] = escena.Imagen(transform, "Vida" + i, sprite, posicionBase[i], new Vector2(38f, 56f), true);
            }

            Restaurar();
        }

        public void Restaurar()
        {
            Quedan = Total;
            for (int i = 0; i < Total; i++)
            {
                relojRotura[i] = -1f;
                frascos[i].color = Color.white;
                frascos[i].rectTransform.anchoredPosition = posicionBase[i];
                frascos[i].rectTransform.localRotation = Quaternion.identity;
                frascos[i].rectTransform.localScale = Vector3.one;
            }
        }

        /// <summary>
        /// Rompe el frasco de más a la derecha que siga entero. Devuelve los que quedan.
        /// </summary>
        public int Romper()
        {
            if (Quedan <= 0)
                return 0;
            Quedan--;
            relojRotura[Quedan] = 0f;
            return Quedan;
        }

        void Update()
        {
            if (frascos == null)
                return;

            for (int i = 0; i < frascos.Length; i++)
            {
                if (relojRotura[i] < 0f || relojRotura[i] > DuracionRotura)
                    continue;

                relojRotura[i] += Time.deltaTime;
                float t = Mathf.Clamp01(relojRotura[i] / DuracionRotura);
                RectTransform rect = frascos[i].rectTransform;

                if (t < 0.4f)
                {
                    // tiembla
                    float temblor = Mathf.Sin(relojRotura[i] * 60f) * 5f;
                    rect.anchoredPosition = posicionBase[i] + new Vector2(temblor, 0f);
                }
                else
                {
                    // se vuelca y queda gris
                    float caida = (t - 0.4f) / 0.6f;
                    rect.anchoredPosition = posicionBase[i] + new Vector2(0f, -10f * caida);
                    rect.localRotation = Quaternion.Euler(0f, 0f, -80f * caida);
                    rect.localScale = Vector3.one * Mathf.Lerp(1f, 0.82f, caida);
                    frascos[i].color = Color.Lerp(Color.white, new Color(0.45f, 0.42f, 0.4f, 0.45f), caida);
                }
            }
        }
    }
}
