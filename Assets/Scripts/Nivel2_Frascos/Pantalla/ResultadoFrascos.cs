using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace FluxLab.Nivel2
{
    /// <summary>
    /// Pantalla de resultados del nivel 2, de madera y pergamino y con todo animado en orden:
    /// título que se desenrolla → paneles que entran → estrellas que caen una por una → puntaje que cuenta
    /// (y sello de "¡Nuevo récord!") → desempeño fila por fila → poción ganada y Magnus → botones.
    /// Un clic en cualquier parte acelera la secuencia.
    /// </summary>
    public class ResultadoFrascos : MonoBehaviour
    {
        static readonly Color Tinta = new Color(0.29f, 0.17f, 0.09f, 1f);
        static readonly Color Dorado = new Color(1f, 0.85f, 0.4f, 1f);
        static readonly Color Crema = new Color(0.95f, 0.89f, 0.77f, 1f);   // letra sobre la madera tallada
        static readonly Color Ambar = new Color(0.72f, 0.4f, 0.1f, 1f);

        struct Fila
        {
            public RectTransform rect;
            public Text valor;
        }

        EscenaFrascos escena;
        ParticulasUI chispas;
        Image velo;
        RectTransform rollo;
        Text titulo;
        Text subtitulo;
        RectTransform tabla;
        Image[] estrellas = new Image[3];
        RectTransform cinta;
        Text rango;
        RectTransform panelPuntos;
        Text puntosTexto;
        Text mejorTexto;
        RectTransform sello;
        RectTransform panelDesempeno;
        Fila[] filas = new Fila[4];
        RectTransform pocion;
        Image brilloPocion;
        RectTransform placaPocion;
        GameObject peanaPocion;
        Text nombrePocion;
        Text avisoTexto;
        RectTransform botones;
        Image siguienteImagen;
        Text siguienteTexto;
        MagnusAnimado magnus;

        bool rapido;
        bool gano;
        bool haySiguiente;
        float relojConfeti;

        float Paso => Time.deltaTime * (rapido ? 6f : 1f);

        // ───────────────────────── Construir ─────────────────────────

        public void Construir(EscenaFrascos escena, System.Action irAlMenu, System.Action reintentar, System.Action siguiente, System.Action irAlMapa)
        {
            this.escena = escena;
            var raiz = GetComponent<RectTransform>();
            raiz.anchorMin = Vector2.zero;
            raiz.anchorMax = Vector2.one;
            raiz.offsetMin = Vector2.zero;
            raiz.offsetMax = Vector2.zero;

            // Velo oscuro: el laboratorio queda detrás. Un clic acelera la animación.
            velo = escena.Imagen(transform, "Velo", null, Vector2.zero, Vector2.zero, false);
            velo.rectTransform.anchorMin = Vector2.zero;
            velo.rectTransform.anchorMax = Vector2.one;
            velo.rectTransform.offsetMin = new Vector2(-400f, -400f);
            velo.rectTransform.offsetMax = new Vector2(400f, 400f);
            velo.raycastTarget = true;
            var clic = velo.gameObject.AddComponent<Button>();
            clic.transition = Selectable.Transition.None;
            clic.onClick.AddListener(() => rapido = true);

            ConstruirTitulo();
            ConstruirEstrellas();
            ConstruirPuntos();
            ConstruirDesempeno();
            ConstruirPocion();

            magnus = MagnusAnimado.Crear(transform, new Vector2(-140f, -330f), 400f, PersonalizacionMagnus.Guardada, AjustesJuego.Accesorio == 1);

            ConstruirBotones(irAlMenu, reintentar, siguiente, irAlMapa);

            var capa = new GameObject("Chispas", typeof(RectTransform));
            capa.transform.SetParent(transform, false);
            chispas = capa.AddComponent<ParticulasUI>();

            gameObject.SetActive(false);
        }

        void ConstruirTitulo()
        {
            // Placa tallada del título (la de la Mesa del Aprendiz), con letra crema.
            var imagen = escena.Imagen(transform, "Titulo", escena.Placa, new Vector2(0f, 448f), new Vector2(820f, 190f), true);
            rollo = imagen.rectTransform;
            titulo = escena.TextoMadera(rollo, "", 46, new Vector2(0f, 18f), new Vector2(520f, 60f));
            titulo.horizontalOverflow = HorizontalWrapMode.Overflow;
            subtitulo = escena.TextoMadera(rollo, NivelesTaller.De(MesaDeFrascos.Nivel).nombre + "  ·  Nivel " + MesaDeFrascos.Nivel, 22, new Vector2(0f, -30f), new Vector2(520f, 32f));
            subtitulo.color = new Color(0.9f, 0.79f, 0.55f, 1f);
        }

        void ConstruirEstrellas()
        {
            var madera = escena.Imagen(transform, "TablaEstrellas", escena.Placa, new Vector2(0f, 262f), new Vector2(600f, 170f), true);
            tabla = madera.rectTransform;
            Sprite estrella = escena.Spr("gem_yellow_star");
            for (int i = 0; i < 3; i++)
            {
                float lado = i == 1 ? 140f : 118f;
                Vector2 lugar = new Vector2((i - 1) * 175f, i == 1 ? 10f : -4f);
                var hueco = escena.Imagen(tabla, "Hueco" + i, estrella, lugar, Vector2.one * lado, true);
                hueco.color = new Color(0.15f, 0.08f, 0.05f, 0.55f);
                estrellas[i] = escena.Imagen(tabla, "Estrella" + i, estrella, lugar, Vector2.one * lado, true);
            }

            var cintaImg = escena.Imagen(transform, "Rango", escena.Madera, new Vector2(0f, 150f), new Vector2(520f, 76f), false);
            cinta = cintaImg.rectTransform;
            rango = escena.TextoMadera(cinta, "", 30, Vector2.zero, new Vector2(480f, 60f));
            rango.color = Dorado;
        }

        void ConstruirPuntos()
        {
            var hoja = escena.Imagen(transform, "PanelPuntos", escena.Pergamino, new Vector2(-610f, 20f), new Vector2(480f, 400f), false);
            hoja.type = Image.Type.Sliced;
            hoja.pixelsPerUnitMultiplier = 1.3f;
            panelPuntos = hoja.rectTransform;

            var encabezado = escena.Texto(panelPuntos, "PUNTOS", 34, Tinta, FontStyle.Bold);
            encabezado.rectTransform.anchoredPosition = new Vector2(0f, 130f);
            encabezado.rectTransform.sizeDelta = new Vector2(360f, 46f);
            escena.Imagen(panelPuntos, "Estrella", escena.ArteMenu("estrella.png", Vector4.zero), new Vector2(-150f, 130f), new Vector2(44f, 44f), true);

            puntosTexto = escena.Texto(panelPuntos, "0", 104, Ambar, FontStyle.Bold);
            puntosTexto.rectTransform.anchoredPosition = new Vector2(0f, 25f);
            puntosTexto.rectTransform.sizeDelta = new Vector2(380f, 130f);
            var contorno = puntosTexto.gameObject.AddComponent<Outline>();
            contorno.effectColor = new Color(0.35f, 0.18f, 0.05f, 1f);
            contorno.effectDistance = new Vector2(3f, -3f);

            mejorTexto = escena.Texto(panelPuntos, "", 24, new Color(0.45f, 0.3f, 0.2f, 1f), FontStyle.Bold);
            mejorTexto.rectTransform.anchoredPosition = new Vector2(0f, -70f);
            mejorTexto.rectTransform.sizeDelta = new Vector2(380f, 34f);

            // Sello rojo de "¡Nuevo récord!".
            var selloImg = escena.Imagen(panelPuntos, "Sello", FormasUI.Punto, new Vector2(110f, -130f), new Vector2(150f, 150f), false);
            selloImg.color = new Color(0.78f, 0.2f, 0.18f, 0.95f);
            var aro = escena.Imagen(selloImg.rectTransform, "Aro", FormasUI.Anillo, Vector2.zero, new Vector2(136f, 136f), false);
            aro.color = new Color(1f, 0.85f, 0.75f, 0.9f);
            var textoSello = escena.Texto(selloImg.rectTransform, "¡NUEVO\nRÉCORD!", 24, Color.white, FontStyle.Bold);
            textoSello.rectTransform.sizeDelta = new Vector2(130f, 80f);
            sello = selloImg.rectTransform;
            sello.localRotation = Quaternion.Euler(0f, 0f, -14f);
        }

        void ConstruirDesempeno()
        {
            var hoja = escena.Imagen(transform, "PanelDesempeno", escena.Pergamino, new Vector2(610f, 20f), new Vector2(480f, 400f), false);
            hoja.type = Image.Type.Sliced;
            hoja.pixelsPerUnitMultiplier = 1.3f;
            panelDesempeno = hoja.rectTransform;

            var encabezado = escena.Texto(panelDesempeno, "DESEMPEÑO", 34, Tinta, FontStyle.Bold);
            encabezado.rectTransform.anchoredPosition = new Vector2(0f, 130f);
            encabezado.rectTransform.sizeDelta = new Vector2(360f, 46f);

            Sprite vida = ImagenesFrascos.Vida;
            filas[0] = NuevaFila(escena.ArteMenu("trofeo.png", Vector4.zero), "Precisión", 62f);
            filas[1] = NuevaFila(FormasUI.Reloj, "Tiempo medio", -2f);
            filas[2] = NuevaFila(escena.ArteMenu("corona.png", Vector4.zero), "Racha máxima", -66f);
            filas[3] = NuevaFila(vida != null ? vida : escena.Spr("frasco_rojo"), "Frascos intactos", -130f);
        }

        Fila NuevaFila(Sprite icono, string etiqueta, float y)
        {
            var go = new GameObject("Fila", typeof(RectTransform));
            go.transform.SetParent(panelDesempeno, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(400f, 56f);

            var fondo = escena.Imagen(rect, "Fondo", null, Vector2.zero, new Vector2(380f, 52f), false);
            fondo.color = new Color(0.55f, 0.35f, 0.2f, 0.12f);
            var imagen = escena.Imagen(rect, "Icono", icono, new Vector2(-160f, 0f), new Vector2(42f, 42f), true);
            if (icono == FormasUI.Reloj)
                imagen.color = Tinta;
            var nombre = escena.Texto(rect, etiqueta, 24, Tinta, FontStyle.Normal);
            nombre.alignment = TextAnchor.MiddleLeft;
            nombre.rectTransform.anchoredPosition = new Vector2(-20f, 0f);
            nombre.rectTransform.sizeDelta = new Vector2(220f, 40f);
            var valor = escena.Texto(rect, "", 28, Ambar, FontStyle.Bold);
            valor.alignment = TextAnchor.MiddleRight;
            valor.rectTransform.anchoredPosition = new Vector2(120f, 0f);
            valor.rectTransform.sizeDelta = new Vector2(130f, 40f);
            return new Fila { rect = rect, valor = valor };
        }

        void ConstruirPocion()
        {
            brilloPocion = escena.Imagen(transform, "BrilloPocion", FormasUI.Humo, new Vector2(160f, -170f), new Vector2(300f, 300f), false);
            brilloPocion.color = new Color(0.82f, 0.68f, 1f, 0.6f);
            var peana = escena.Imagen(transform, "Peana", escena.Madera, new Vector2(160f, -282f), new Vector2(230f, 52f), false);
            peana.type = Image.Type.Sliced;
            peana.pixelsPerUnitMultiplier = 4f;
            peanaPocion = peana.gameObject;
            // La poción dibujada como los íconos de los botones (menú), para que vaya con la madera tallada.
            Sprite premio = escena.ArteMenu("pocion.png", Vector4.zero);
            if (premio == null)
                premio = ImagenesFrascos.Premio;
            pocion = escena.Imagen(transform, "Pocion", premio != null ? premio : escena.Spr("frasco_rojo"), new Vector2(160f, -180f), new Vector2(130f, 170f), true).rectTransform;

            var placa = escena.Imagen(transform, "PlacaPocion", escena.Madera, new Vector2(160f, -352f), new Vector2(460f, 88f), false);
            placaPocion = placa.rectTransform;
            nombrePocion = escena.TextoMadera(placaPocion, "", 22, new Vector2(0f, 12f), new Vector2(420f, 30f));
            nombrePocion.horizontalOverflow = HorizontalWrapMode.Overflow;
            avisoTexto = escena.TextoMadera(placaPocion, "", 18, new Vector2(0f, -16f), new Vector2(420f, 26f));
            avisoTexto.color = Dorado;
            avisoTexto.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        void ConstruirBotones(System.Action irAlMenu, System.Action reintentar, System.Action siguiente, System.Action irAlMapa)
        {
            var go = new GameObject("Botones", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            botones = go.GetComponent<RectTransform>();

            escena.BotonMadera(botones, "MENÚ", new Vector2(-650f, -462f), new Vector2(220f, 86f), 26, irAlMenu, 0f, false);
            escena.BotonMadera(botones, "REINTENTAR", new Vector2(-330f, -462f), new Vector2(290f, 92f), 28, reintentar, 0f, false);
            siguienteImagen = escena.BotonMadera(botones, "SIGUIENTE NIVEL", new Vector2(110f, -458f), new Vector2(400f, 104f), 30, () =>
            {
                if (haySiguiente)
                    siguiente?.Invoke();
            }, 0f, false);
            siguienteTexto = siguienteImagen.GetComponentInChildren<Text>();
            escena.BotonMadera(botones, "MAPA", new Vector2(600f, -462f), new Vector2(220f, 86f), 26, irAlMapa, 0f, false);
        }

        // ───────────────────────── Mostrar ─────────────────────────

        public void Ocultar()
        {
            StopAllCoroutines();
            gameObject.SetActive(false);
        }

        public void Mostrar(EscenaFrascos.Resultado r)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            StopAllCoroutines();
            chispas.Limpiar();
            rapido = false;
            StartCoroutine(Secuencia(r));
        }

        IEnumerator Secuencia(EscenaFrascos.Resultado r)
        {
            int estrellasGanadas = RecetaFrascos.Estrellas(r.aciertos, r.rondas, r.seRompieron);
            gano = r.pasa;
            haySiguiente = r.haySiguiente;
            int precision = r.rondas <= 0 ? 0 : Mathf.RoundToInt(r.aciertos * 100f / r.rondas);
            bool perfecto = r.pasa && estrellasGanadas >= 3;

            titulo.text = perfecto ? "¡RECETA PERFECTA!" : r.pasa ? "¡NIVEL COMPLETADO!" : "¡SIGUE PRACTICANDO!";
            rango.text = r.seRompieron ? "Se rompieron los frascos"
                : estrellasGanadas >= 3 ? "Estrella de alquimista"
                : estrellasGanadas == 2 ? "Mano firme"
                : estrellasGanadas == 1 ? "Chispa del taller"
                : "El caldero espera";
            mejorTexto.text = "Mejor puntaje:  " + r.mejorPuntaje;
            // La poción solo se gana si pasó el nivel; si no, la placa solo da el aviso.
            nombrePocion.text = r.pasa ? "Poción ganada:  Tintura de frascos" : "Esta vez no hay poción";
            avisoTexto.text = r.aviso;
            pocion.gameObject.SetActive(r.pasa);
            peanaPocion.SetActive(r.pasa);
            brilloPocion.gameObject.SetActive(r.pasa);
            placaPocion.anchoredPosition = new Vector2(160f, r.pasa ? -352f : -250f);

            siguienteTexto.text = r.haySiguiente ? "SIGUIENTE NIVEL" : "AÚN CERRADO";
            siguienteImagen.color = r.haySiguiente ? Color.white : new Color(0.55f, 0.52f, 0.5f, 1f);
            siguienteImagen.GetComponent<Button>().interactable = r.haySiguiente;

            // Todo empieza escondido.
            velo.color = new Color(0.05f, 0.03f, 0.07f, 0f);
            rollo.localScale = new Vector3(0.05f, 1f, 1f);
            titulo.color = new Color(Crema.r, Crema.g, Crema.b, 0f);
            subtitulo.color = new Color(0.9f, 0.79f, 0.55f, 0f);
            panelPuntos.anchoredPosition = new Vector2(-1400f, 20f);
            panelDesempeno.anchoredPosition = new Vector2(1400f, 20f);
            tabla.anchoredPosition = new Vector2(0f, 900f);
            foreach (Image estrella in estrellas)
                estrella.rectTransform.localScale = Vector3.zero;
            cinta.localScale = Vector3.zero;
            puntosTexto.text = "0";
            mejorTexto.color = new Color(0.45f, 0.3f, 0.2f, 0f);
            sello.localScale = Vector3.zero;
            foreach (Fila fila in filas)
            {
                fila.rect.anchoredPosition = new Vector2(700f, fila.rect.anchoredPosition.y);
                fila.valor.text = "";
            }
            pocion.anchoredPosition = new Vector2(160f, -600f);
            brilloPocion.color = new Color(0.82f, 0.68f, 1f, 0f);
            placaPocion.localScale = Vector3.zero;
            botones.anchoredPosition = new Vector2(0f, -300f);
            magnus.gameObject.SetActive(false);

            // 1. Se oscurece y el título se desenrolla.
            yield return Animar(0.3f, k => velo.color = new Color(0.05f, 0.03f, 0.07f, 0.62f * k));
            escena.Sonar("FluxLab/Audio/click", 0.8f);
            yield return Animar(0.45f, k =>
            {
                rollo.localScale = new Vector3(Mathf.LerpUnclamped(0.05f, 1f, Rebote(k)), 1f, 1f);
                titulo.color = new Color(Crema.r, Crema.g, Crema.b, Mathf.Clamp01(k * 2f - 0.6f));
                subtitulo.color = new Color(0.9f, 0.79f, 0.55f, Mathf.Clamp01(k * 2f - 0.8f));
            });

            // 2. Los paneles entran desde los lados y la tabla de estrellas cae.
            escena.Sonar("Nivel2/pop", 0.7f);
            yield return Animar(0.6f, k =>
            {
                float e = Rebote(k);
                panelPuntos.anchoredPosition = new Vector2(Mathf.LerpUnclamped(-1400f, -610f, e), 20f);
                panelDesempeno.anchoredPosition = new Vector2(Mathf.LerpUnclamped(1400f, 610f, e), 20f);
                tabla.anchoredPosition = new Vector2(0f, Mathf.LerpUnclamped(900f, 262f, Elastico(k)));
            });

            // 3. Las estrellas caen una por una.
            for (int i = 0; i < 3; i++)
            {
                bool ganada = i < estrellasGanadas;
                Image estrella = estrellas[i];
                estrella.color = ganada ? Color.white : new Color(1f, 1f, 1f, 0f);
                if (!ganada)
                    continue;

                escena.Sonar("Nivel2/estrella", 0.9f);
                yield return Animar(0.32f, k =>
                {
                    estrella.rectTransform.localScale = Vector3.one * Mathf.LerpUnclamped(3f, 1f, Rebote(k));
                    estrella.rectTransform.localRotation = Quaternion.Euler(0f, 0f, (1f - k) * 200f);
                });
                Explosion(estrella.rectTransform.position, Dorado, 16);
                yield return Animar(0.18f, k => tabla.anchoredPosition = new Vector2(Mathf.Sin(k * 40f) * 6f * (1f - k), 262f));
            }

            // 4. El título del rango.
            escena.Sonar("FluxLab/Audio/hechizo", 0.7f);
            yield return Animar(0.35f, k => cinta.localScale = Vector3.one * Mathf.LerpUnclamped(0f, 1f, Rebote(k)));

            // 5. El puntaje cuenta hacia arriba.
            float sonido = 0f;
            yield return Animar(1f, k =>
            {
                puntosTexto.text = Mathf.RoundToInt(r.puntos * Suave(k)).ToString();
                puntosTexto.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(k * Mathf.PI) * 0.12f);
                sonido -= Paso;
                if (sonido <= 0f && k < 1f)
                {
                    sonido = 0.08f;
                    escena.Sonar("FluxLab/Audio/click", 0.25f);
                }
            });
            yield return Animar(0.3f, k => mejorTexto.color = new Color(0.45f, 0.3f, 0.2f, k));
            if (r.nuevoRecord)
            {
                escena.Sonar("Nivel2/estrella", 1f);
                yield return Animar(0.3f, k =>
                {
                    sello.localScale = Vector3.one * Mathf.Lerp(2.6f, 1f, Suave(k));
                    sello.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-40f, -14f, k));
                });
                Explosion(sello.position, new Color(1f, 0.55f, 0.45f, 1f), 18);
            }

            // 6. Desempeño, fila por fila.
            string[] valores =
            {
                precision + "%",
                r.tiempoMedio.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " s",
                r.rachaMaxima + " / " + r.rondas,
                r.vidasRestantes + " / " + r.vidasTotales
            };
            for (int i = 0; i < filas.Length; i++)
            {
                Fila fila = filas[i];
                string valor = valores[i];
                escena.Sonar("FluxLab/Audio/click", 0.5f);
                yield return Animar(0.3f, k =>
                {
                    fila.rect.anchoredPosition = new Vector2(Mathf.LerpUnclamped(700f, 0f, Rebote(k)), fila.rect.anchoredPosition.y);
                    fila.valor.text = k < 0.6f ? "" : valor;
                });
                yield return Animar(0.15f, k => fila.valor.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(k * Mathf.PI) * 0.3f));
            }

            // 7. La poción sube brillando y aparece Magnus.
            escena.Sonar("Nivel2/corcho", 0.8f);
            magnus.gameObject.SetActive(true);
            if (perfecto)
                magnus.FinalAlegre();
            else if (r.pasa)
                magnus.FinalContento();
            else
                magnus.FinalAnimo();
            yield return Animar(0.55f, k =>
            {
                pocion.anchoredPosition = new Vector2(160f, Mathf.LerpUnclamped(-600f, -180f, Rebote(k)));
                brilloPocion.color = new Color(0.82f, 0.68f, 1f, 0.6f * k);
                placaPocion.localScale = Vector3.one * Mathf.LerpUnclamped(0f, 1f, Rebote(Mathf.Clamp01(k * 1.4f - 0.4f)));
            });
            if (r.pasa)
                Explosion(pocion.position, new Color(0.85f, 0.95f, 1f, 1f), 20);

            // 8. Los botones suben rebotando.
            escena.Sonar("Nivel2/pop", 0.7f);
            yield return Animar(0.5f, k => botones.anchoredPosition = new Vector2(0f, Mathf.LerpUnclamped(-300f, 0f, Rebote(k))));
            rapido = false;
        }

        // ───────────────────────── Cada cuadro ─────────────────────────

        void Update()
        {
            float t = Time.time;

            // La poción flota y brilla.
            if (gano && brilloPocion.color.a > 0.05f)
            {
                pocion.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.5f) * 4f);
                brilloPocion.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(t * 2.2f) * 0.08f);
                if (Random.value < Time.deltaTime * 3f)
                    chispas.Una(FormasUI.Destello, ALocal(pocion.position) + new Vector2(Random.Range(-60f, 60f), Random.Range(-80f, 80f)),
                        Vector2.one * Random.Range(14f, 26f), new Color(0.9f, 0.97f, 1f, 1f), new Vector2(0f, 30f), 0.8f, 0f, 90f, 0.3f);
            }

            // Las estrellas ganadas destellan de vez en cuando.
            for (int i = 0; i < estrellas.Length; i++)
            {
                if (estrellas[i].color.a < 0.5f || estrellas[i].rectTransform.localScale.x < 0.9f)
                    continue;
                estrellas[i].rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(t * 3f + i) * 0.04f);
                if (Random.value < Time.deltaTime * 0.8f)
                    chispas.Una(FormasUI.Destello, ALocal(estrellas[i].rectTransform.position) + new Vector2(Random.Range(-40f, 40f), Random.Range(-40f, 40f)),
                        Vector2.one * Random.Range(18f, 30f), Color.white, Vector2.zero, 0.6f, 0f, 120f, 0.4f);
            }

            // "Siguiente nivel" late para invitar a seguir.
            if (haySiguiente && botones.anchoredPosition.y > -10f)
                siguienteImagen.color = Color.Lerp(Color.white, new Color(1f, 0.92f, 0.65f, 1f), (Mathf.Sin(t * 4f) + 1f) * 0.5f);

            // Si pasó el nivel: magia suave en vez de confeti (destellos que titilan y luciérnagas doradas que suben).
            if (gano && velo.color.a > 0.3f)
            {
                relojConfeti -= Time.deltaTime;
                if (relojConfeti <= 0f)
                {
                    relojConfeti = 0.08f;
                    if (Random.value < 0.6f)
                        chispas.Una(FormasUI.Destello, new Vector2(Random.Range(-940f, 940f), Random.Range(-520f, 520f)),
                            Vector2.one * Random.Range(24f, 44f), Random.value < 0.6f ? Color.white : Dorado,
                            new Vector2(0f, Random.Range(4f, 12f)), Random.Range(1.4f, 2.4f), 0f, 40f, -0.25f);
                    else
                        chispas.Una(FormasUI.Punto, new Vector2(Random.Range(-940f, 940f), -560f),
                            Vector2.one * Random.Range(8f, 14f), new Color(1f, 0.88f, 0.5f, 0.9f),
                            new Vector2(Random.Range(-15f, 15f), Random.Range(60f, 120f)), Random.Range(5f, 7f), 0f, 0f, 0f);
                }
            }
        }

        // ───────────────────────── Ayudas ─────────────────────────

        IEnumerator Animar(float duracion, System.Action<float> paso)
        {
            float reloj = 0f;
            while (reloj < duracion)
            {
                reloj += Paso;
                paso(Mathf.Clamp01(reloj / duracion));
                yield return null;
            }

            paso(1f);
        }

        void Explosion(Vector3 posicionMundo, Color color, int cantidad)
        {
            chispas.Emitir(new ParticulasUI.Rafaga
            {
                forma = FormasUI.Destello, cantidad = cantidad, centro = ALocal(posicionMundo), dispersion = new Vector2(20f, 10f),
                color = color, angulo = new Vector2(0f, 360f), rapidez = new Vector2(200f, 480f),
                vida = new Vector2(0.5f, 0.9f), tamano = new Vector2(18f, 36f), gravedad = 250f, giro = 240f
            });
        }

        Vector2 ALocal(Vector3 posicionMundo)
        {
            Vector3 local = chispas.transform.InverseTransformPoint(posicionMundo);
            return new Vector2(local.x, local.y);
        }

        static Color[] Arcoiris() => new[]
        {
            new Color(1f, 0.48f, 0.35f, 1f), new Color(1f, 0.82f, 0.35f, 1f),
            new Color(0.5f, 0.88f, 0.42f, 1f), new Color(0.55f, 0.75f, 1f, 1f), new Color(0.85f, 0.62f, 1f, 1f)
        };

        static float Suave(float t) => t * t * (3f - 2f * t);

        static float Rebote(float t)
        {
            const float c1 = 1.70158f;
            float u = t - 1f;
            return 1f + (c1 + 1f) * u * u * u + c1 * u * u;
        }

        static float Elastico(float t)
        {
            if (t <= 0f || t >= 1f)
                return t;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * (2f * Mathf.PI / 3f)) + 1f;
        }
    }
}
