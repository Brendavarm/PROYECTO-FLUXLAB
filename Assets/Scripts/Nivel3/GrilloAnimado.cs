using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace FluxLab
{
    /// <summary>
    /// El grillo del laboratorio: se posa en distintos lugares, camina, da saltitos, canta "cri-cri",
    /// juega con Mishi y, cuando el nivel lo pide, cruza de un salto enorme por encima del pergamino para distraer.
    /// Usa los 13 cuadros de Resources/FluxLab/Grillo (grillo_0 a grillo_12).
    /// </summary>
    public class GrilloAnimado : MonoBehaviour
    {
        enum Estado
        {
            Reposo,
            Camina,
            Salto,
            Saltito,
            Canta
        }

        public float escala = 0.55f;            // tamaño: 1 = el dibujo tal cual
        public float velocidad = 95f;
        public float volumenChirrido = 0.3f;

        // Cuadros: 0 preparado, 1 impulso, 2 y 3 en el aire, 4 aterrizaje, 5 pose final,
        // 6 reposo, 7 a 10 pasos, 11 salto corto, 12 mirando
        static readonly int[] Pasos = { 7, 8, 9, 10 };

        MishiAnimado.Plataforma[] plataformas;
        MishiAnimado mishi;
        Sprite[] cuadros;
        RectTransform rect;
        RectTransform sombra;
        Image imagen;
        AudioSource voz;
        AudioClip chirrido;
        Transform capa;

        Estado estado = Estado.Reposo;
        float reloj;
        float duracion;
        float x;
        float pisoY;
        int plataforma;
        float mira = 1f;            // 1 = mira a la derecha (como el dibujo), -1 = a la izquierda
        float destino;              // al caminar
        float saltoX0, saltoY0, saltoX1, saltoY1, saltoAlto;
        int saltoA;
        bool distraerPendiente;
        int saltosDistraccion = 3;

        /// <summary>En modo tranquilo el grillo se queda quieto y callado (salvo que lo mande a distraer el juego).</summary>
        public bool tranquilo;

        public static GrilloAnimado Crear(Transform lienzo, MishiAnimado.Plataforma[] plataformas, MishiAnimado mishi)
        {
            var go = new GameObject("Grillo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(lienzo, false);

            var imagen = go.GetComponent<Image>();
            imagen.preserveAspect = true;
            imagen.raycastTarget = false;

            var r = go.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0f); // el pivote está en las patas

            var grillo = go.AddComponent<GrilloAnimado>();
            grillo.plataformas = plataformas;
            grillo.mishi = mishi;
            grillo.capa = lienzo;
            return grillo;
        }

        void Start()
        {
            rect = GetComponent<RectTransform>();
            imagen = GetComponent<Image>();

            cuadros = new Sprite[13];
            for (int i = 0; i < cuadros.Length; i++)
                cuadros[i] = Nivel3UI.SpriteOpcional("FluxLab/Grillo/grillo_" + i);

            var sombraImg = Nivel3UI.Imagen(capa, "SombraGrillo", FrascoQuebradizo.Disco(), Vector2.zero, new Vector2(90f, 14f));
            sombraImg.color = new Color(0.08f, 0.04f, 0.03f, 0.3f);
            sombraImg.transform.SetSiblingIndex(transform.GetSiblingIndex());
            sombra = sombraImg.rectTransform;

            voz = gameObject.AddComponent<AudioSource>();
            voz.playOnAwake = false;
            chirrido = Resources.Load<AudioClip>("FluxLab/Audio/grillo");
            if (chirrido == null)
                chirrido = CrearChirrido();

            plataforma = 0;
            x = (plataformas[0].izq + plataformas[0].der) * 0.5f;
            pisoY = plataformas[0].y;

            StartCoroutine(Cerebro());
        }

        // =====================================================================================
        // Animación
        // =====================================================================================

        void Update()
        {
            if (rect == null)
                return;

            reloj += Time.deltaTime;
            float avance = duracion <= 0.01f ? 1f : Mathf.Clamp01(reloj / duracion);
            float baseY = pisoY;
            float y = baseY;
            float giro = 0f;
            float ancho = 1f;
            float alto = 1f + Mathf.Sin(Time.time * 3f) * 0.012f;
            Sprite cuadro = cuadros[6];

            switch (estado)
            {
                case Estado.Reposo:
                    cuadro = ((int)(Time.time / 2.4f) % 3 == 2) ? cuadros[12] : cuadros[6];
                    break;

                case Estado.Camina:
                    x = Mathf.MoveTowards(x, destino, velocidad * Time.deltaTime);
                    cuadro = cuadros[Pasos[(int)(Time.time * 9f) % Pasos.Length]];
                    y += Mathf.Abs(Mathf.Sin(Time.time * 9f * Mathf.PI)) * 4f;
                    if (Mathf.Approximately(x, destino))
                        Cambiar(Estado.Reposo, 0f);
                    break;

                case Estado.Salto:
                    x = Mathf.Lerp(saltoX0, saltoX1, avance);
                    baseY = Mathf.Lerp(saltoY0, saltoY1, avance);
                    float vuelo = Mathf.Sin(avance * Mathf.PI);
                    y = baseY + vuelo * saltoAlto;
                    cuadro = avance < 0.12f ? cuadros[0] : avance < 0.25f ? cuadros[1] : avance < 0.55f ? cuadros[2]
                        : avance < 0.8f ? cuadros[3] : avance < 0.93f ? cuadros[4] : cuadros[5];
                    if (avance < 0.12f || avance > 0.93f)
                    {
                        alto *= 0.94f;
                        ancho *= 1.05f;
                    }

                    if (avance >= 1f)
                    {
                        x = saltoX1;
                        pisoY = saltoY1;
                        plataforma = saltoA;
                        Cambiar(Estado.Reposo, 0f);
                    }
                    break;

                case Estado.Saltito:
                    y += Mathf.Sin(avance * Mathf.PI) * 60f;
                    cuadro = (avance > 0.12f && avance < 0.88f) ? cuadros[11] : cuadros[6];
                    if (avance >= 1f)
                        Cambiar(Estado.Reposo, 0f);
                    break;

                case Estado.Canta:
                    cuadro = cuadros[6];
                    giro = Mathf.Sin(Time.time * 60f) * 2.5f;       // las alitas vibran
                    ancho = 1f + Mathf.Sin(Time.time * 45f) * 0.03f;
                    if (avance >= 1f)
                        Cambiar(Estado.Reposo, 0f);
                    break;
            }

            imagen.enabled = cuadro != null;   // si falta algún dibujo, no se ve un cuadro blanco
            if (cuadro != null)
            {
                imagen.sprite = cuadro;
                rect.sizeDelta = cuadro.rect.size * escala;
            }

            rect.anchoredPosition = new Vector2(x, y);
            rect.localRotation = Quaternion.Euler(0f, 0f, giro);
            rect.localScale = new Vector3(mira * ancho, alto, 1f);

            float elevacion = y - baseY;
            float s = Mathf.Clamp(1f - elevacion / 200f, 0.4f, 1f);
            sombra.anchoredPosition = new Vector2(x, baseY - 3f);
            sombra.localScale = new Vector3(s, s, 1f);
        }

        void Cambiar(Estado nuevo, float segundos)
        {
            estado = nuevo;
            reloj = 0f;
            duracion = segundos;
        }

        // =====================================================================================
        // Qué hace el grillo y cuándo
        // =====================================================================================

        /// <summary>Para distraer al jugador: da un salto enorme que cruza por encima del pergamino.</summary>
        public void Distraer(int saltos = 3)
        {
            saltosDistraccion = Mathf.Max(1, saltos);
            distraerPendiente = true;
        }

        IEnumerator Cerebro()
        {
            yield return new WaitForSeconds(1.5f);
            float proximoJuego = Time.time + Random.Range(10f, 18f);

            while (true)
            {
                if (distraerPendiente)
                {
                    distraerPendiente = false;
                    yield return DistraerCo(saltosDistraccion);
                    continue;
                }

                if (tranquilo)
                {
                    yield return Reposar(1f);
                    continue;
                }

                if (mishi != null && Time.time >= proximoJuego)
                {
                    proximoJuego = Time.time + Random.Range(20f, 32f);
                    yield return JugarCon();
                    continue;
                }

                float suerte = Random.value;
                if (suerte < 0.28f)
                    yield return Caminar();
                else if (suerte < 0.48f)
                    yield return IrAOtraPlataforma();
                else if (suerte < 0.66f)
                    yield return Saltito();
                else if (suerte < 0.88f)
                    yield return Cantar();
                else
                    yield return Reposar(Random.Range(1.2f, 2.2f));

                yield return Reposar(Random.Range(0.4f, 1.1f));
            }
        }

        IEnumerator Reposar(float segundos)
        {
            Cambiar(Estado.Reposo, 0f);
            float t = 0f;
            while (t < segundos && !distraerPendiente)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }

        IEnumerator Caminar()
        {
            var sitio = plataformas[plataforma];
            if (sitio.der - sitio.izq < 60f)
                yield break;

            destino = Random.Range(sitio.izq, sitio.der);
            mira = destino >= x ? 1f : -1f;
            Cambiar(Estado.Camina, 0f);
            while (estado == Estado.Camina && !distraerPendiente)
                yield return null;
            if (estado == Estado.Camina)
                Cambiar(Estado.Reposo, 0f);
        }

        IEnumerator Saltito()
        {
            Cambiar(Estado.Saltito, 0.5f);
            while (estado == Estado.Saltito)
                yield return null;
        }

        IEnumerator Cantar()
        {
            Cambiar(Estado.Canta, 1.1f);
            if (chirrido != null && AjustesJuego.EfectosActivos)
                voz.PlayOneShot(chirrido, volumenChirrido);
            StartCoroutine(TextoFlotante());
            while (estado == Estado.Canta)
                yield return null;
        }

        IEnumerator IrAOtraPlataforma()
        {
            if (plataformas.Length < 2)
                yield break;

            int otra = plataforma;
            while (otra == plataforma)
                otra = Random.Range(0, plataformas.Length);

            var meta = plataformas[otra];
            yield return SaltarA(otra, Random.Range(meta.izq, meta.der), 120f, 1f);
        }

        IEnumerator SaltarA(int destinoPlataforma, float xDestino, float alturaArco, float segundos)
        {
            saltoX0 = x;
            saltoY0 = pisoY;
            saltoX1 = xDestino;
            saltoY1 = plataformas[destinoPlataforma].y;
            saltoAlto = alturaArco;
            saltoA = destinoPlataforma;
            mira = xDestino >= x ? 1f : -1f;
            Cambiar(Estado.Salto, segundos);
            while (estado == Estado.Salto)
                yield return null;
        }

        /// <summary>Se acerca a Mishi y le salta por encima de un lado al otro; ella salta tras él.</summary>
        IEnumerator JugarCon()
        {
            int indice = PlataformaDeMishi();
            if (indice < 0)
                yield break;

            var sitio = plataformas[indice];
            float lado = mishi.X >= (sitio.izq + sitio.der) * 0.5f ? -1f : 1f; // llega por donde hay más sitio
            float llegada = Mathf.Clamp(mishi.X + lado * 200f, sitio.izq, sitio.der);

            yield return SaltarA(indice, llegada, 110f, 0.9f);
            mishi.ReaccionarA(x);
            yield return Reposar(0.5f);

            for (int i = 0; i < 2; i++)
            {
                float otroLado = Mathf.Clamp(mishi.X - lado * 200f, sitio.izq, sitio.der);
                mishi.ReaccionarA(x);
                yield return SaltarA(indice, otroLado, 190f, 0.9f);
                lado = -lado;
                yield return Reposar(0.4f);
            }

            yield return Cantar();
        }

        int PlataformaDeMishi()
        {
            int mejor = -1;
            float distancia = float.MaxValue;
            for (int i = 0; i < plataformas.Length; i++)
            {
                if (Mathf.Abs(plataformas[i].y - mishi.PisoY) > 8f)
                    continue;
                float centro = (plataformas[i].izq + plataformas[i].der) * 0.5f;
                float d = Mathf.Abs(centro - mishi.X);
                if (d < distancia)
                {
                    distancia = d;
                    mejor = i;
                }
            }

            return mejor;
        }

        IEnumerator DistraerCo(int saltos)
        {
            if (chirrido != null && AjustesJuego.EfectosActivos)
                voz.PlayOneShot(chirrido, volumenChirrido);

            // Salta una y otra vez de una repisa a la más lejana: cada arco pasa por encima del pergamino
            for (int n = 0; n < saltos; n++)
            {
                int mejor = -1;
                float distancia = -1f;
                for (int i = 0; i < plataformas.Length; i++)
                {
                    if (i == plataforma || plataformas[i].y <= 0f)
                        continue;
                    float d = Mathf.Abs((plataformas[i].izq + plataformas[i].der) * 0.5f - x);
                    if (d > distancia)
                    {
                        distancia = d;
                        mejor = i;
                    }
                }

                if (mejor < 0)
                {
                    yield return Saltito();
                    continue;
                }

                var meta = plataformas[mejor];
                yield return SaltarA(mejor, Random.Range(meta.izq, meta.der), 340f, 1.1f);
                if (n < saltos - 1)
                    yield return Reposar(0.12f);
            }

            yield return Cantar();
        }

        // =====================================================================================
        // Sonido y detalles
        // =====================================================================================

        /// <summary>Un "¡cri-cri!" que sube y se desvanece sobre su cabeza.</summary>
        IEnumerator TextoFlotante()
        {
            var texto = Nivel3UI.Texto(capa, "¡cri-cri!", 34, new Color(0.85f, 1f, 0.55f), new Vector2(x, pisoY + 150f), new Vector2(240f, 50f),
                TextAnchor.MiddleCenter, true);
            float t = 0f;
            while (t < 1f && texto != null)
            {
                t += Time.deltaTime / 1.1f;
                var r = texto.rectTransform;
                r.anchoredPosition = new Vector2(x, pisoY + 150f + t * 60f);
                var color = texto.color;
                color.a = 1f - t * t;
                texto.color = color;
                yield return null;
            }

            if (texto != null)
                Destroy(texto.gameObject);
        }

        /// <summary>
        /// Un "cri-cri" provisional hecho por el código: dos grupos de pulsitos agudos.
        /// Si existe Resources/FluxLab/Audio/grillo (mp3/wav/ogg), se usa ese.
        /// </summary>
        static AudioClip CrearChirrido()
        {
            const int frecuencia = 44100;
            var datos = new float[(int)(frecuencia * 0.6f)];
            float[] inicioGrupo = { 0f, 0.27f };

            foreach (float grupo in inicioGrupo)
            {
                for (int pulso = 0; pulso < 4; pulso++)
                {
                    float inicio = grupo + pulso * 0.038f;
                    int desde = (int)(inicio * frecuencia);
                    int largoPulso = (int)(0.024f * frecuencia);
                    for (int i = 0; i < largoPulso && desde + i < datos.Length; i++)
                    {
                        float t = (desde + i) / (float)frecuencia;
                        float envolvente = Mathf.Pow(Mathf.Sin(Mathf.PI * i / largoPulso), 1.5f);
                        float tono = Mathf.Sin(2f * Mathf.PI * 4300f * t) + 0.3f * Mathf.Sin(2f * Mathf.PI * 8600f * t);
                        datos[desde + i] += tono * envolvente * 0.35f;
                    }
                }
            }

            var clip = AudioClip.Create("grillo", datos.Length, 1, frecuencia, false);
            clip.SetData(datos, 0);
            return clip;
        }
    }
}
