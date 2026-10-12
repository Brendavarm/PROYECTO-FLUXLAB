using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FluxLab.Nivel2
{
    /// <summary>
    /// Le da vida al fondo pintado: va encima del dibujo y detrás del juego (Magnus, caldero, HUD).
    ///  - Burbujas mágicas que suben meciéndose; algunas revientan.
    ///  - Faroles con un resplandor cálido que titila como una llama.
    ///  - La luna brilla despacio y titilan estrellitas en la ventana.
    ///  - Polvo mágico dorado flotando.
    ///  - Destellos de vez en cuando en los frascos de las repisas.
    /// Las posiciones (faroles, luna, repisas) están en FondoNivel2.cs.
    /// </summary>
    public class FondoVivo : MonoBehaviour
    {
        class Burbuja
        {
            public RectTransform rect;
            public Image imagen;
            public float x;
            public float y;
            public float subida;
            public float fase;
            public float vaiven;
            public float revientaEn;
            public float alfa;
        }

        static readonly string[] SpritesBurbuja = { "burbuja_a", "burbuja_b", "burbuja_c" };

        EscenaFrascos escena;
        ConfigFrascos config;
        ParticulasUI chispas;
        readonly List<Image> faroles = new List<Image>();
        readonly List<float> tamanoFaroles = new List<float>();
        readonly List<Burbuja> burbujas = new List<Burbuja>();
        Image luna;
        float relojBurbuja;
        float relojPolvo;
        float relojDestello;
        float relojEstrella;

        public void Construir(EscenaFrascos escena, ConfigFrascos config)
        {
            this.escena = escena;
            this.config = config;
            var raiz = GetComponent<RectTransform>();
            raiz.anchorMin = Vector2.zero;
            raiz.anchorMax = Vector2.one;
            raiz.offsetMin = Vector2.zero;
            raiz.offsetMax = Vector2.zero;

            // Resplandor de la luna y de cada farol.
            luna = escena.Imagen(transform, "BrilloLuna", FormasUI.Humo, FondoNivel2.ACanvas(FondoNivel2.Luna.x, FondoNivel2.Luna.y), new Vector2(260f, 260f), false);
            luna.color = new Color(0.6f, 0.75f, 1f, 0.3f);
            foreach (Vector3 farol in FondoNivel2.Faroles)
            {
                float lado = farol.z * FondoNivel2.Escala * 2f;
                var brillo = escena.Imagen(transform, "BrilloFarol", FormasUI.Humo, FondoNivel2.ACanvas(farol.x, farol.y), new Vector2(lado, lado), false);
                brillo.color = new Color(1f, 0.7f, 0.3f, 0.35f);
                faroles.Add(brillo);
                tamanoFaroles.Add(lado);
            }

            var capa = new GameObject("Chispas", typeof(RectTransform));
            capa.transform.SetParent(transform, false);
            chispas = capa.AddComponent<ParticulasUI>();

            // Empieza con algunas burbujas ya flotando, para que no aparezcan todas desde abajo.
            for (int i = 0; i < 3; i++)
                NuevaBurbuja(Random.Range(-500f, 400f));
        }

        void Update()
        {
            if (!config.fondoVivo)
                return;
            float dt = Time.deltaTime;
            float t = Time.time;

            // Faroles: la llama titila (ruido suave, no parpadeo brusco).
            for (int i = 0; i < faroles.Count; i++)
            {
                float llama = Mathf.PerlinNoise(t * 2.2f, i * 7.3f);
                Color c = faroles[i].color;
                c.a = 0.22f + llama * 0.28f;
                faroles[i].color = c;
                faroles[i].rectTransform.sizeDelta = Vector2.one * tamanoFaroles[i] * (0.9f + llama * 0.2f);
            }

            // Luna: brillo lento.
            Color luz = luna.color;
            luz.a = 0.22f + (Mathf.Sin(t * 0.8f) + 1f) * 0.08f;
            luna.color = luz;

            MoverBurbujas(dt, t);

            relojBurbuja -= dt * config.burbujasFondo;
            if (relojBurbuja <= 0f)
            {
                relojBurbuja = Random.Range(1.3f, 2.6f);   // pocas burbujas: que acompañen, no distraigan
                NuevaBurbuja(-600f);
            }

            // Polvo mágico dorado que aparece, flota y se apaga.
            relojPolvo -= dt;
            if (relojPolvo <= 0f)
            {
                relojPolvo = 0.12f;
                chispas.Una(FormasUI.Punto, new Vector2(Random.Range(-940f, 940f), Random.Range(-420f, 520f)), Vector2.one * Random.Range(4f, 9f),
                    new Color(1f, 0.9f, 0.55f, Random.Range(0.4f, 0.8f)), new Vector2(Random.Range(-12f, 12f), Random.Range(6f, 22f)),
                    Random.Range(2.5f, 4.5f), 0f, 0f, 0f);
            }

            // Un frasco de alguna repisa brilla un instante.
            relojDestello -= dt;
            if (relojDestello <= 0f)
            {
                relojDestello = Random.Range(0.25f, 0.6f);
                Rect repisa = FondoNivel2.Repisas[Random.Range(0, FondoNivel2.Repisas.Length)];
                Vector2 punto = FondoNivel2.ACanvas(Random.Range(repisa.xMin, repisa.xMax), Random.Range(repisa.yMin, repisa.yMax));
                chispas.Una(FormasUI.Destello, punto, Vector2.one * Random.Range(18f, 34f), new Color(1f, 1f, 0.9f, 0.9f), Vector2.zero, 0.7f, 0f, 90f, 0.6f);
            }

            // Estrellitas en la ventana de la luna.
            relojEstrella -= dt;
            if (relojEstrella <= 0f)
            {
                relojEstrella = Random.Range(0.4f, 0.9f);
                Rect ventana = FondoNivel2.Ventana;
                Vector2 punto = FondoNivel2.ACanvas(Random.Range(ventana.xMin, ventana.xMax), Random.Range(ventana.yMin, ventana.yMax));
                chispas.Una(FormasUI.Destello, punto, Vector2.one * Random.Range(10f, 18f), new Color(0.85f, 0.9f, 1f, 1f), Vector2.zero, 1.2f, 0f, 40f, 0.3f);
            }
        }

        // ───────────────────────── Burbujas ─────────────────────────

        void NuevaBurbuja(float desdeY)
        {
            // De vez en cuando sube un racimo de burbujas en vez de una sola.
            Sprite racimo = Random.value < 0.15f ? ImagenesFrascos.BurbujasGrupo : null;
            float lado = racimo != null ? Random.Range(70f, 110f) : Random.Range(18f, 58f);
            Sprite forma = racimo != null ? racimo : escena.Spr(SpritesBurbuja[Random.Range(0, SpritesBurbuja.Length)]);
            var imagen = escena.Imagen(transform, "Burbuja", forma, Vector2.zero, Vector2.one * lado, true);
            var burbuja = new Burbuja
            {
                rect = imagen.rectTransform,
                imagen = imagen,
                x = Random.Range(-920f, 920f),
                y = desdeY,
                subida = Random.Range(28f, 60f) * (1.2f - lado / 100f),
                fase = Random.Range(0f, 6.3f),
                vaiven = Random.Range(10f, 26f),
                // Algunas revientan a media altura; las demás salen por arriba.
                revientaEn = Random.value < 0.45f ? Random.Range(-150f, 450f) : 9999f,
                alfa = Random.Range(0.55f, 0.9f)
            };
            imagen.color = new Color(1f, 1f, 1f, 0f);
            burbujas.Add(burbuja);
        }

        void MoverBurbujas(float dt, float t)
        {
            for (int i = burbujas.Count - 1; i >= 0; i--)
            {
                Burbuja b = burbujas[i];
                b.y += b.subida * dt;
                float x = b.x + Mathf.Sin(t * 0.9f + b.fase) * b.vaiven;
                b.rect.anchoredPosition = new Vector2(x, b.y);
                b.rect.localScale = new Vector3(1f + Mathf.Sin(t * 2.3f + b.fase) * 0.04f, 1f - Mathf.Sin(t * 2.3f + b.fase) * 0.04f, 1f);

                // Aparece suave al nacer.
                Color c = b.imagen.color;
                c.a = Mathf.MoveTowards(c.a, b.alfa, dt * 0.6f);
                b.imagen.color = c;

                bool revienta = b.y >= b.revientaEn;
                bool salio = b.y > 600f;
                if (!revienta && !salio)
                    continue;

                if (revienta)
                {
                    float lado = b.rect.sizeDelta.x;
                    chispas.Una(FormasUI.Anillo, b.rect.anchoredPosition, Vector2.one * lado, new Color(1f, 1f, 1f, 0.6f), Vector2.zero, 0.3f, 0f, 0f, 0.9f);
                    chispas.Emitir(new ParticulasUI.Rafaga
                    {
                        forma = FormasUI.Punto, cantidad = 5, centro = b.rect.anchoredPosition, dispersion = new Vector2(lado * 0.2f, lado * 0.2f),
                        color = new Color(0.85f, 0.95f, 1f, 0.8f), angulo = new Vector2(0f, 360f), rapidez = new Vector2(40f, 90f),
                        vida = new Vector2(0.3f, 0.5f), tamano = new Vector2(4f, 7f), gravedad = 80f
                    });
                }

                Destroy(b.rect.gameObject);
                burbujas.RemoveAt(i);
            }
        }
    }
}
