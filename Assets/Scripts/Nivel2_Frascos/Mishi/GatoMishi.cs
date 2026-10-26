using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FluxLab.Nivel2
{
    /// <summary>
    /// Mishi, el gato del laboratorio, en la bienvenida del nivel 2: entra saltando,
    /// maúlla con un globito y corazones, y si le haces clic, maúlla.
    /// El gato y sus animaciones son los de Sasha (Gatitos/MishiAnimado): aquí solo se llaman.
    /// Si faltan sus dibujos, se usa la imagen quieta de antes.
    /// </summary>
    public class GatoMishi : MonoBehaviour, IPointerClickHandler
    {
        static readonly string[] Maullidos = { "Nivel2/miau1", "Nivel2/miau2", "Nivel2/miau3" };
        static readonly string[] MaullidosSasha = { "FluxLab/Audio/gato_1", "FluxLab/Audio/gato_2", "FluxLab/Audio/gato_3" };
        static readonly string[] Frases = { "¡Miau!", "¡Prrr~!", "¡Suerte, miau!", "¡Miau miau!" };

        // Cara de frente de Sasha (Resources/FluxLab/Mishi/frente_0 … frente_7) mientras explica.
        const int CaraExplicando = 0;

        EscenaFrascos escena;
        ParticulasUI particulas;
        RectTransform rect;
        RectTransform cuerpo;      // solo con la imagen quieta de antes
        MishiAnimado mishi;        // el gato animado de Sasha
        RectTransform globo;
        CanvasGroup grupoGlobo;    // para que la notita aparezca y se desvanezca suave
        Vector2 lugarGlobo;
        RectTransform puntoGrande; // los puntitos que unen la notita con el gato
        RectTransform puntoChico;
        Text textoGlobo;
        Vector2 sitio;
        float relojGlobo;
        float salto;          // 0..1 mientras salta de alegría (imagen quieta)
        float aplastado;      // al aterrizar (imagen quieta)
        bool caminando;
        Vector2 pies;         // dónde se para el Mishi de Sasha dentro del gato
        float relojTravesura = 2.5f;   // cuánto falta para su próxima travesura

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

            var pies = new Vector2(0f, -alto * 0.45f);
            gato.pies = pies;
            Sprite caraSasha = Resources.Load<Sprite>("FluxLab/Mishi/frente_" + CaraExplicando);
            if (caraSasha != null)
            {
                // El Mishi de Sasha, de frente y quieto explicando; él solo respira, maúlla y salta.
                gato.mishi = MishiAnimado.Crear(raiz.transform);
                gato.mishi.escala = alto / caraSasha.rect.height;
                gato.mishi.Preparar();
                gato.mishi.ColocarExplicando(pies, CaraExplicando);
            }
            else
            {
                Sprite sprite = ImagenesFrascos.Mishi;
                if (sprite == null)
                    sprite = escena.Spr("mishi");
                var imagen = escena.Imagen(raiz.transform, "Cuerpo", sprite, Vector2.zero, new Vector2(alto, alto), true);
                gato.cuerpo = imagen.rectTransform;
                gato.cuerpo.pivot = new Vector2(0.5f, 0.05f);   // se estira desde las patas
                gato.cuerpo.anchoredPosition = pies;
            }

            // Notita de pergamino rústico (el mismo de la receta), con dos puntitos que la unen al gato.
            var fondoGlobo = escena.Imagen(raiz.transform, "Globo", escena.Pergamino, new Vector2(-alto * 0.62f, alto * 0.78f), new Vector2(250f, 84f), false);
            fondoGlobo.pixelsPerUnitMultiplier = 2.2f;
            var sombraGlobo = fondoGlobo.gameObject.AddComponent<Shadow>();
            sombraGlobo.effectColor = new Color(0f, 0f, 0f, 0.35f);
            sombraGlobo.effectDistance = new Vector2(0f, -4f);
            gato.globo = fondoGlobo.rectTransform;
            gato.grupoGlobo = fondoGlobo.gameObject.AddComponent<CanvasGroup>();
            gato.grupoGlobo.alpha = 0f;
            gato.grupoGlobo.blocksRaycasts = false;
            Color papel = new Color(0.86f, 0.73f, 0.53f, 1f);
            gato.puntoGrande = escena.Imagen(gato.globo, "Colita", FormasUI.Punto, Vector2.zero, new Vector2(18f, 18f), false).rectTransform;
            gato.puntoGrande.GetComponent<Image>().color = papel;
            gato.puntoChico = escena.Imagen(gato.globo, "Colita", FormasUI.Punto, Vector2.zero, new Vector2(11f, 11f), false).rectTransform;
            gato.puntoChico.GetComponent<Image>().color = papel;
            gato.textoGlobo = escena.Texto(gato.globo, "", 28, new Color(0.35f, 0.2f, 0.12f, 1f), FontStyle.Bold);
            gato.textoGlobo.rectTransform.sizeDelta = new Vector2(230f, 64f);
            gato.AcomodarGlobo(false);
            return gato;
        }

        /// <summary>Llega saltando desde "desde" hasta "hasta" (coordenadas del padre).</summary>
        public IEnumerator Entrar(Vector2 desde, Vector2 hasta, float duracion = 0.7f)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();   // delante de todo, para que su globito no quede tapado
            caminando = true;
            sitio = hasta;
            if (mishi != null)
            {
                mishi.ColocarExplicando(pies, CaraExplicando);   // vuelve al centro si se había ido paseando
                mishi.Celebrar();   // el salto de Sasha (dibujos de salto) mientras vuela hasta su sitio
                relojTravesura = Random.Range(2.5f, 3.5f);
            }
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
            // Muy arriba la notita se saldría de la pantalla: entonces va a su lado derecho.
            AcomodarGlobo(hasta.y > 250f);
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
            if (mishi != null)
                mishi.Maullar();   // pose y voz del gato de Sasha
            else
                escena.Sonar(Maullidos[Random.Range(0, Maullidos.Length)], 0.9f);
            Globito(frase);
        }

        public void SaltarDeAlegria()
        {
            if (mishi != null)
            {
                // Su salto de alegría; la voz va aparte para no cortar el salto.
                mishi.Celebrar();
                escena.Sonar(MaullidosSasha[Random.Range(0, MaullidosSasha.Length)], 0.7f);
            }
            else
            {
                salto = 1f;
                escena.Sonar(Maullidos[Random.Range(0, Maullidos.Length)], 0.9f);
            }
            Globito("¡Bien, miau!");
        }

        /// <summary>
        /// Mientras espera, cada pocos segundos hace algo con las animaciones de Sasha:
        /// pasea un poquito (y vuelve de frente), lanza su bola mágica o da un saltito.
        /// </summary>
        void Travesura()
        {
            float suerte = Random.value;
            if (suerte < 0.45f)
            {
                // Camina de lado unos pasos (dentro de su sitio) y vuelve a mirar de frente.
                float hacia = mishi.X > pies.x ? -1f : 1f;
                mishi.ExplicarEn(new Vector2(pies.x + hacia * Random.Range(35f, 65f), pies.y), CaraExplicando, null);
                relojTravesura = Random.Range(3f, 4.5f);
            }
            else if (suerte < 0.75f)
            {
                // Bola mágica: se pone un poco a la derecha para lanzarla hacia el centro de la pantalla.
                if (mishi.X > pies.x + 10f)
                    mishi.Lanzar();
                else
                    mishi.ExplicarEn(new Vector2(pies.x + 45f, pies.y), CaraExplicando, mishi.Lanzar);
                relojTravesura = Random.Range(5f, 7f);
            }
            else
            {
                mishi.Celebrar();
                relojTravesura = Random.Range(3f, 4.5f);
            }
        }

        // aLaDerecha = false: arriba a la izquierda del gato; true: a su derecha, a la altura de la cabeza.
        void AcomodarGlobo(bool aLaDerecha)
        {
            float alto = rect.sizeDelta.y;
            lugarGlobo = aLaDerecha ? new Vector2(alto * 1.15f, alto * 0.3f) : new Vector2(-alto * 0.62f, alto * 0.78f);
            globo.anchoredPosition = lugarGlobo;
            float lado = aLaDerecha ? -1f : 1f;
            puntoGrande.anchoredPosition = aLaDerecha ? new Vector2(-132f, -30f) : new Vector2(78f, -52f);
            puntoChico.anchoredPosition = puntoGrande.anchoredPosition + new Vector2(18f * lado, -16f);
        }

        // La notita con la frase y unos destellos dorados que suben, como la magia del juego.
        void Globito(string frase)
        {
            textoGlobo.text = frase ?? Frases[Random.Range(0, Frases.Length)];
            relojGlobo = 1.8f;
            aplastado = 0.7f;
            for (int i = 0; i < 5; i++)
            {
                Vector2 desde = Local(rect.anchoredPosition) + new Vector2(Random.Range(-50f, 30f), rect.sizeDelta.y * 0.25f);
                particulas.Una(FormasUI.Destello, desde, Vector2.one * Random.Range(18f, 30f), new Color(1f, Random.Range(0.82f, 0.92f), 0.5f, 1f),
                    new Vector2(Random.Range(-40f, 40f), Random.Range(70f, 130f)), Random.Range(0.9f, 1.3f), -10f, Random.Range(-90f, 90f), 0.3f);
            }
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
                // El de Sasha ya se balancea solo; la imagen quieta se mece un poquito.
                float meneo = mishi != null ? 0f : Mathf.Sin(t * 1.3f) * 4f;
                rect.localRotation = Quaternion.Euler(0f, 0f, meneo + (salto > 0f ? Mathf.Sin(t * 20f) * 8f * salto : 0f));
            }

            if (mishi != null && !caminando && Time.timeScale > 0f)
            {
                relojTravesura -= dt;
                if (relojTravesura <= 0f)
                    Travesura();
            }

            // Solo la imagen quieta: respira y se aplasta un poquito (el de Sasha ya se anima solo).
            if (cuerpo != null)
            {
                float respirar = Mathf.Sin(t * 2.6f) * 0.03f;
                float aplaste = Mathf.Sin(aplastado * Mathf.PI * 3f) * 0.18f * aplastado;
                cuerpo.localScale = new Vector3(1f + aplaste - respirar * 0.5f, 1f - aplaste + respirar, 1f);
            }

            // La notita aparece subiendo un poquito y se desvanece suave (sin saltos ni rebotes).
            relojGlobo -= dt;
            float objetivo = relojGlobo > 0f ? 1f : 0f;
            grupoGlobo.alpha = Mathf.MoveTowards(grupoGlobo.alpha, objetivo, dt * 5f);
            float suave = Mathf.SmoothStep(0f, 1f, grupoGlobo.alpha);
            globo.anchoredPosition = lugarGlobo + new Vector2(0f, (suave - 1f) * 14f + Mathf.Sin(t * 2f) * 2f);
            globo.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, suave);
        }

        Vector2 Local(Vector2 enPadre)
        {
            Vector3 mundo = rect.parent.TransformPoint(enPadre);
            Vector3 local = particulas.transform.InverseTransformPoint(mundo);
            return new Vector2(local.x, local.y);
        }
    }
}
