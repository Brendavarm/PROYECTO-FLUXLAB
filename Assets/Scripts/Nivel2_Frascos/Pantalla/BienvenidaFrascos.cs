using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FluxLab.Nivel2
{
    /// <summary>
    /// Pantallas grandes del nivel 2, con el mismo estilo de madera y pergamino del menú:
    ///  - Mostrar(): bienvenida con lo que cambia respecto al nivel 1 (espera "¡Estoy listo!").
    ///  - Conteo(): 3, 2, 1, "¡Vamos, atentooo!".
    ///  - AnuncioEspejo(): aviso gigante antes de la ronda al revés.
    /// Mishi, el gato, acompaña con maullidos. MesaDeFrascos las llama con "yield return".
    /// </summary>
    public class BienvenidaFrascos : MonoBehaviour
    {
        static readonly Color Tinta = new Color(0.29f, 0.17f, 0.09f, 1f);
        static readonly Color Crema = new Color(1f, 0.92f, 0.72f, 1f);
        static readonly Color Dorado = new Color(1f, 0.85f, 0.4f, 1f);

        EscenaFrascos escena;
        ConfigFrascos config;
        Image velo;
        ParticulasUI chispas;
        GatoMishi gato;

        RectTransform tablero;
        RectTransform rollo;
        RectTransform tituloTexto;
        RectTransform datos;
        RectTransform boton;
        RectTransform frascoIzquierdo;
        RectTransform frascoDerecho;
        readonly List<RectTransform> filas = new List<RectTransform>();
        bool listo;
        bool esperandoBoton;
        bool saltar;
        float relojAmbiente;

        public void Construir(EscenaFrascos escena, ConfigFrascos config)
        {
            this.escena = escena;
            this.config = config;
            var raiz = GetComponent<RectTransform>();
            raiz.anchorMin = Vector2.zero;
            raiz.anchorMax = Vector2.one;
            raiz.offsetMin = Vector2.zero;
            raiz.offsetMax = Vector2.zero;

            velo = escena.Imagen(transform, "Velo", null, Vector2.zero, Vector2.zero, false);
            velo.rectTransform.anchorMin = Vector2.zero;
            velo.rectTransform.anchorMax = Vector2.one;
            velo.rectTransform.offsetMin = new Vector2(-400f, -400f);
            velo.rectTransform.offsetMax = new Vector2(400f, 400f);
            velo.raycastTarget = true;
            var clicVelo = velo.gameObject.AddComponent<Button>();
            clicVelo.transition = Selectable.Transition.None;
            clicVelo.onClick.AddListener(() => saltar = true);

            ConstruirTablero();

            var capa = new GameObject("Chispas", typeof(RectTransform));
            capa.transform.SetParent(transform, false);
            chispas = capa.AddComponent<ParticulasUI>();

            gato = GatoMishi.Crear(transform, escena, chispas, 190f);
            gato.gameObject.SetActive(false);

            gameObject.SetActive(false);
        }

        // ───────────────────────── Tablero de madera con pergamino ─────────────────────────

        void ConstruirTablero()
        {
            frascoIzquierdo = Frasco(ImagenesFrascos.DeIngrediente(IngredientKind.Gota), new Vector2(-790f, 40f));
            frascoDerecho = Frasco(ImagenesFrascos.DeIngrediente(IngredientKind.Hierba), new Vector2(790f, 40f));

            // La madera se repite en mosaico (si se estirara, las vetas quedarían como bandas gruesas).
            var madera = escena.Imagen(transform, "Tablero", escena.Madera, new Vector2(0f, -70f), new Vector2(1260f, 720f), false);
            madera.type = Image.Type.Tiled;
            tablero = madera.rectTransform;

            var hoja = escena.Imagen(tablero, "Pergamino", escena.Pergamino, new Vector2(0f, -20f), new Vector2(1160f, 560f), false);
            hoja.type = Image.Type.Sliced;

            // Rollo del título, que se desenrolla al entrar.
            var rolloImg = escena.Imagen(tablero, "RolloTitulo", escena.Pergamino, new Vector2(0f, 360f), new Vector2(1020f, 210f), false);
            rolloImg.type = Image.Type.Sliced;
            rollo = rolloImg.rectTransform;
            var titulo = escena.Texto(rollo, "¡Bienvenido al Pasillo de Frascos!", 52, Tinta, FontStyle.Bold);
            titulo.rectTransform.anchoredPosition = new Vector2(0f, 22f);
            titulo.rectTransform.sizeDelta = new Vector2(900f, 70f);
            titulo.horizontalOverflow = HorizontalWrapMode.Overflow;
            Contorno(titulo, new Color(1f, 0.95f, 0.8f, 0.9f), 2f, false);
            tituloTexto = titulo.rectTransform;
            var sub = escena.Texto(rollo, "Nivel 2  ·  ¡más difícil que la Mesa del Aprendiz!", 26, new Color(0.55f, 0.28f, 0.15f, 1f), FontStyle.Bold);
            sub.rectTransform.anchoredPosition = new Vector2(0f, -34f);
            sub.rectTransform.sizeDelta = new Vector2(860f, 40f);

            float y = 200f;
            Fila(hoja.transform, ref y, Icono.Uno, "Los ingredientes se encienden <b>uno por uno</b>. ¡Atento!");
            if (config.permitirRepetidos)
                Fila(hoja.transform, ref y, Icono.Repetido, "¡Ojo! Un ingrediente <b>puede repetirse</b> en la receta.");
            Fila(hoja.transform, ref y, Icono.Vida, "Tienes <b>" + config.vidas + " frascos de vida</b>: cada error rompe uno.");
            if (config.barajarBandeja)
                Fila(hoja.transform, ref y, Icono.Barajar, "La bandeja <b>cambia de lugar</b> en cada ronda.");
            if (config.rondaEspejoFinal && config.rondas > 1)
                Fila(hoja.transform, ref y, Icono.Espejo, "La última ronda es <b>espejo</b>: ¡se pone al revés!");

            var placa = escena.Imagen(hoja.transform, "Datos", escena.Madera, new Vector2(0f, -232f), new Vector2(880f, 74f), false);
            placa.type = Image.Type.Sliced;
            placa.pixelsPerUnitMultiplier = 2.4f;
            datos = placa.rectTransform;
            var textoDatos = escena.Texto(datos, config.rondas + " rondas  ·  recetas de " + config.largoReceta + " ingredientes  ·  necesitas " + config.estrellasParaAvanzar + " estrellas para pasar", 24, Crema, FontStyle.Bold);
            textoDatos.rectTransform.sizeDelta = new Vector2(860f, 60f);
            Contorno(textoDatos, new Color(0.2f, 0.1f, 0.05f, 0.9f), 2f, false);

            var listoImg = escena.Boton(transform, "¡Estoy listo!", escena.Madera, new Vector2(0f, -455f), new Vector2(460f, 128f), 42, Crema, () => listo = true);
            listoImg.pixelsPerUnitMultiplier = 1.4f;
            Contorno(listoImg.GetComponentInChildren<Text>(), new Color(0.2f, 0.1f, 0.05f, 1f), 3f, true);
            boton = listoImg.rectTransform;
        }

        enum Icono { Uno, Repetido, Vida, Barajar, Espejo }

        void Fila(Transform padre, ref float y, Icono icono, string texto)
        {
            var fila = new GameObject("Fila", typeof(RectTransform)).GetComponent<RectTransform>();
            fila.SetParent(padre, false);
            fila.anchoredPosition = new Vector2(0f, y);
            fila.sizeDelta = new Vector2(980f, 80f);

            // El pergamino deja libre de -465 a 465 (los rollos ocupan los lados):
            // icono en -360, texto de -290 a 430. Todo el bloque queda centrado.
            var circulo = escena.Imagen(fila, "Circulo", escena.Disco, new Vector2(-360f, 0f), new Vector2(108f, 108f), false);
            circulo.color = new Color(1f, 0.72f, 0.3f, 0.7f);
            DibujarIcono(fila, icono, new Vector2(-360f, 0f));

            var linea = escena.Texto(fila, texto, 28, Tinta, FontStyle.Normal);
            linea.alignment = TextAnchor.MiddleLeft;
            linea.rectTransform.anchoredPosition = new Vector2(70f, 0f);
            linea.rectTransform.sizeDelta = new Vector2(720f, 72f);

            var raya = escena.Imagen(fila, "Raya", null, new Vector2(10f, -42f), new Vector2(820f, 2f), false);
            raya.color = new Color(0.55f, 0.35f, 0.2f, 0.25f);

            filas.Add(fila);
            y -= 84f;
        }

        void DibujarIcono(Transform padre, Icono icono, Vector2 centro)
        {
            switch (icono)
            {
                case Icono.Uno:
                    escena.Imagen(padre, "Gema", escena.Spr("gem_yellow_star"), centro, new Vector2(58f, 58f), true);
                    escena.Imagen(padre, "Destello", FormasUI.Destello, centro + new Vector2(24f, 24f), new Vector2(42f, 42f), false);
                    break;
                case Icono.Repetido:
                    escena.Imagen(padre, "Gema", escena.Spr("gem_red_circle"), centro + new Vector2(-15f, 7f), new Vector2(48f, 48f), true);
                    escena.Imagen(padre, "Gema", escena.Spr("gem_red_circle"), centro + new Vector2(15f, -7f), new Vector2(48f, 48f), true);
                    break;
                case Icono.Vida:
                    Sprite vida = ImagenesFrascos.Vida;
                    escena.Imagen(padre, "Vida", vida != null ? vida : escena.Spr("frasco_rojo"), centro, new Vector2(58f, 80f), true);
                    break;
                case Icono.Barajar:
                    escena.Imagen(padre, "Gema", escena.Spr("gem_blue_triangle"), centro + new Vector2(-17f, 11f), new Vector2(44f, 44f), true);
                    escena.Imagen(padre, "Gema", escena.Spr("gem_green_square"), centro + new Vector2(17f, -11f), new Vector2(44f, 44f), true);
                    break;
                default:
                    escena.Imagen(padre, "Gema", escena.Spr("gem_purple_pentagon"), centro + new Vector2(-19f, 0f), new Vector2(40f, 40f), true);
                    var reflejo = escena.Imagen(padre, "Reflejo", escena.Spr("gem_purple_pentagon"), centro + new Vector2(19f, 0f), new Vector2(40f, 40f), true);
                    reflejo.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
                    reflejo.color = new Color(1f, 1f, 1f, 0.55f);
                    escena.Imagen(padre, "Linea", null, centro, new Vector2(4f, 58f), false).color = new Color(1f, 1f, 1f, 0.9f);
                    break;
            }
        }

        RectTransform Frasco(Sprite sprite, Vector2 posicion)
        {
            return escena.Imagen(transform, "FrascoDecorado", sprite != null ? sprite : escena.Spr("frasco_azul"), posicion, new Vector2(150f, 250f), true).rectTransform;
        }

        // ───────────────────────── Bienvenida ─────────────────────────

        public IEnumerator Mostrar()
        {
            Abrir();
            listo = false;
            esperandoBoton = false;
            MostrarTablero(true);
            PrepararEntrada();

            yield return Animar(0.35f, k => velo.color = new Color(0.06f, 0.03f, 0.08f, 0.8f * k));

            // El tablero de madera cae y rebota.
            escena.Sonar("Nivel2/pop");
            yield return Animar(0.8f, k =>
            {
                tablero.anchoredPosition = new Vector2(0f, Mathf.LerpUnclamped(1100f, -70f, Elastico(k)));
                tablero.localRotation = Quaternion.Euler(0f, 0f, (1f - k) * -6f);
            });
            Polvo(tablero.anchoredPosition + new Vector2(0f, -360f), 900f);

            // El rollo del título se desenrolla...
            escena.Sonar("FluxLab/Audio/click", 0.8f);
            yield return Animar(0.45f, k => rollo.localScale = new Vector3(Mathf.LerpUnclamped(0.05f, 1f, Rebote(k)), 1f, 1f));

            // ...y ¡tan, tan, taaan!
            yield return Golpe(0.85f, 1.12f, "Nivel2/estrella", 14);
            yield return Golpe(0.85f, 1.16f, "Nivel2/estrella", 18);
            yield return Golpe(0.8f, 1.3f, "FluxLab/Audio/hechizo", 34);

            // Las instrucciones entran deslizándose una por una.
            foreach (RectTransform fila in filas)
            {
                escena.Sonar("FluxLab/Audio/click", 0.55f);
                RectTransform esta = fila;
                yield return Animar(0.24f, k =>
                {
                    esta.anchoredPosition = new Vector2(Mathf.LerpUnclamped(-1300f, 0f, Rebote(k)), esta.anchoredPosition.y);
                    esta.localRotation = Quaternion.Euler(0f, 0f, (1f - k) * 6f);
                });
                yield return Esperar(0.07f);
            }

            yield return Animar(0.25f, k => datos.localScale = Vector3.one * Mathf.LerpUnclamped(0f, 1f, Rebote(k)));

            // Llega Mishi saltando al borde del tablero.
            yield return gato.Entrar(new Vector2(1150f, 420f), new Vector2(520f, 368f));
            gato.Maullar("¡Hola! ¡Miau!");
            yield return Esperar(0.3f);

            escena.Sonar("Nivel2/pop", 0.7f);
            yield return Animar(0.4f, k => boton.localScale = Vector3.one * Mathf.LerpUnclamped(0f, 1f, Rebote(k)));

            esperandoBoton = true;
            while (!listo)
                yield return null;
            esperandoBoton = false;

            // Al aceptar: Mishi salta, chispas, y el tablero sale volando hacia arriba.
            gato.SaltarDeAlegria();
            escena.Sonar("FluxLab/Audio/acierto", 0.7f);
            Explosion(boton.position, Dorado, 34);
            yield return Animar(0.2f, k => boton.localScale = Vector3.one * (1f - 0.15f * Mathf.Sin(k * Mathf.PI)));
            yield return Esperar(0.35f);
            Vector2 inicioTablero = tablero.anchoredPosition;
            yield return Animar(0.45f, k =>
            {
                float e = k * k;
                tablero.anchoredPosition = inicioTablero + new Vector2(0f, e * 1300f);
                tablero.localRotation = Quaternion.Euler(0f, 0f, e * 8f);
                boton.anchoredPosition = new Vector2(0f, -455f - e * 400f);
                frascoIzquierdo.anchoredPosition += new Vector2(-1500f, 0f) * Time.deltaTime;
                frascoDerecho.anchoredPosition += new Vector2(1500f, 0f) * Time.deltaTime;
            });
            yield return gato.Entrar(gato.GetComponent<RectTransform>().anchoredPosition, new Vector2(1200f, -200f), 0.5f);
            gato.gameObject.SetActive(false);
            MostrarTablero(false);
        }

        void PrepararEntrada()
        {
            velo.color = new Color(0.06f, 0.03f, 0.08f, 0f);
            tablero.anchoredPosition = new Vector2(0f, 1100f);
            tablero.localRotation = Quaternion.identity;
            rollo.localScale = new Vector3(0.05f, 1f, 1f);
            foreach (RectTransform fila in filas)
                fila.anchoredPosition = new Vector2(-1300f, fila.anchoredPosition.y);
            datos.localScale = Vector3.zero;
            boton.localScale = Vector3.zero;
            boton.anchoredPosition = new Vector2(0f, -455f);
            frascoIzquierdo.anchoredPosition = new Vector2(-790f, 40f);
            frascoDerecho.anchoredPosition = new Vector2(790f, 40f);
            gato.gameObject.SetActive(false);
        }

        void MostrarTablero(bool visible)
        {
            tablero.gameObject.SetActive(visible);
            boton.gameObject.SetActive(visible);
            frascoIzquierdo.gameObject.SetActive(visible);
            frascoDerecho.gameObject.SetActive(visible);
        }

        // Un "¡tan!": el título late, suena y suelta destellos.
        IEnumerator Golpe(float duracion, float fuerza, string sonido, int destellos)
        {
            escena.Sonar(sonido);
            Explosion(tituloTexto.position, Dorado, destellos);
            yield return Animar(0.22f * duracion, k => tituloTexto.localScale = Vector3.one * Mathf.Lerp(fuerza, 1f, k));
            yield return Esperar(0.12f * duracion);
        }

        // ───────────────────────── Conteo 3, 2, 1 ─────────────────────────

        public IEnumerator Conteo()
        {
            Abrir();
            MostrarTablero(false);
            if (velo.color.a < 0.5f)
                yield return Animar(0.25f, k => velo.color = new Color(0.06f, 0.03f, 0.08f, 0.6f * k));

            yield return Numero("3", new Color(1f, 0.48f, 0.35f, 1f));
            yield return Numero("2", new Color(1f, 0.82f, 0.35f, 1f));
            yield return Numero("1", new Color(0.5f, 0.88f, 0.42f, 1f));
            yield return Grito("¡Vamos, atentooo!", new Color(0.45f, 0.15f, 0.6f, 1f), "FluxLab/Audio/hechizo");

            yield return Cerrar();
        }

        IEnumerator Numero(string texto, Color color)
        {
            escena.Sonar("FluxLab/Audio/click", 1f);
            Vector2 centro = new Vector2(0f, 60f);
            var halo = escena.Imagen(transform, "Halo", FormasUI.Humo, centro, new Vector2(520f, 520f), false);
            var numero = escena.Texto(transform, texto, 260, color, FontStyle.Bold);
            numero.rectTransform.anchoredPosition = centro;
            numero.rectTransform.sizeDelta = new Vector2(500f, 320f);
            Contorno(numero, new Color(0.15f, 0.05f, 0.1f, 1f), 6f, true);

            chispas.Una(FormasUI.Anillo, centro, new Vector2(200f, 200f), color, Vector2.zero, 0.6f, 0f, 0f, 3.2f);
            chispas.Una(FormasUI.Anillo, centro, new Vector2(140f, 140f), Color.white, Vector2.zero, 0.45f, 0f, 0f, 3.8f);
            Explosion(numero.rectTransform.position, color, 22);

            yield return Animar(0.8f, k =>
            {
                float entrada = Mathf.Clamp01(k / 0.4f);
                float salida = Mathf.Clamp01((k - 0.78f) / 0.22f);
                float escala = Mathf.LerpUnclamped(2.8f, 1f, Rebote(entrada)) * (1f + Mathf.Sin(k * 20f) * 0.03f * (1f - salida)) * Mathf.Lerp(1f, 0.5f, salida);
                numero.rectTransform.localScale = Vector3.one * escala;
                numero.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-30f, 0f, Rebote(entrada)));
                numero.color = new Color(color.r, color.g, color.b, Mathf.Clamp01(entrada * 3f) * (1f - salida));
                halo.color = new Color(color.r, color.g, color.b, 0.55f * Mathf.Clamp01(entrada * 2f) * (1f - salida));
                halo.rectTransform.localScale = Vector3.one * (0.6f + entrada * 0.5f + Mathf.Sin(k * 14f) * 0.04f);
            });

            Destroy(numero.gameObject);
            Destroy(halo.gameObject);
        }

        // Texto grande que entra temblando con anillos de colores y sale hacia arriba.
        IEnumerator Grito(string frase, Color borde, string sonido)
        {
            escena.Sonar(sonido, 1f);
            Vector2 centro = new Vector2(0f, 60f);
            var halo = escena.Imagen(transform, "Halo", FormasUI.Humo, centro, new Vector2(900f, 600f), false);
            var texto = escena.Texto(transform, frase, 120, Color.white, FontStyle.Bold);
            texto.rectTransform.anchoredPosition = centro;
            texto.rectTransform.sizeDelta = new Vector2(1600f, 200f);
            texto.horizontalOverflow = HorizontalWrapMode.Overflow;
            Contorno(texto, borde, 6f, true);

            Color[] colores = Arcoiris();
            for (int i = 0; i < colores.Length; i++)
            {
                chispas.Una(FormasUI.Anillo, centro, new Vector2(160f, 160f) * (1f + i * 0.3f), colores[i], Vector2.zero, 0.7f + i * 0.08f, 0f, 0f, 4f);
                Explosion(texto.rectTransform.position, colores[i], 12);
            }

            yield return Animar(1.2f, k =>
            {
                float entrada = Mathf.Clamp01(k / 0.3f);
                float salida = Mathf.Clamp01((k - 0.75f) / 0.25f);
                float temblor = k > 0.3f && k < 0.75f ? Mathf.Sin(k * 90f) * 6f : 0f;
                texto.rectTransform.localScale = Vector3.one * Mathf.LerpUnclamped(0.2f, 1f, Rebote(entrada)) * (1f + salida * 0.4f);
                texto.rectTransform.anchoredPosition = centro + new Vector2(temblor, salida * 140f);
                texto.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(k * 30f) * 3f * (1f - salida));
                texto.color = new Color(1f, 1f, 1f, 1f - salida);
                Color c = colores[Mathf.FloorToInt(k * 12f) % colores.Length];
                halo.color = new Color(c.r, c.g, c.b, 0.5f * entrada * (1f - salida));
            });

            Destroy(texto.gameObject);
            Destroy(halo.gameObject);
        }

        // ───────────────────────── Aviso de ronda espejo ─────────────────────────

        public IEnumerator AnuncioEspejo()
        {
            Abrir();
            MostrarTablero(false);
            saltar = false;
            yield return Animar(0.3f, k => velo.color = new Color(0.05f, 0.04f, 0.12f, 0.8f * k));

            Vector2 centro = new Vector2(0f, 90f);
            var marco = new GameObject("Espejo", typeof(RectTransform)).GetComponent<RectTransform>();
            marco.SetParent(transform, false);
            marco.anchoredPosition = centro;

            // El espejo: vidrio celeste, marco plateado y un brillo que lo cruza.
            var vidrio = escena.Imagen(marco, "Vidrio", FormasUI.Elipse, Vector2.zero, new Vector2(1180f, 560f), false);
            vidrio.color = new Color(0.6f, 0.85f, 1f, 0.28f);
            var aro = escena.Imagen(marco, "Marco", FormasUI.Anillo, Vector2.zero, new Vector2(1240f, 620f), false);
            aro.color = new Color(0.86f, 0.9f, 1f, 0.95f);
            var aroDorado = escena.Imagen(marco, "MarcoDorado", FormasUI.Anillo, Vector2.zero, new Vector2(1290f, 660f), false);
            aroDorado.color = new Color(1f, 0.8f, 0.4f, 0.8f);
            var brillo = escena.Imagen(marco, "Brillo", null, new Vector2(-700f, 0f), new Vector2(70f, 620f), false);
            brillo.color = new Color(1f, 1f, 1f, 0.35f);
            brillo.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -18f);

            var titulo = escena.Texto(marco, "¡RONDA ESPEJO!", 124, Color.white, FontStyle.Bold);
            titulo.rectTransform.anchoredPosition = new Vector2(0f, 130f);
            titulo.rectTransform.sizeDelta = new Vector2(1400f, 170f);
            titulo.horizontalOverflow = HorizontalWrapMode.Overflow;
            Contorno(titulo, new Color(0.35f, 0.2f, 0.7f, 1f), 6f, true);

            var linea1 = escena.Texto(marco, "Esta receta va  AL REVÉS", 52, Dorado, FontStyle.Bold);
            linea1.rectTransform.anchoredPosition = new Vector2(0f, 20f);
            linea1.rectTransform.sizeDelta = new Vector2(1100f, 70f);
            Contorno(linea1, new Color(0.2f, 0.1f, 0.25f, 1f), 3f, true);
            var linea2 = escena.Texto(marco, "Pon primero el ÚLTIMO ingrediente que veas", 34, Color.white, FontStyle.Bold);
            linea2.rectTransform.anchoredPosition = new Vector2(0f, -44f);
            linea2.rectTransform.sizeDelta = new Vector2(1100f, 50f);
            Contorno(linea2, new Color(0.2f, 0.1f, 0.25f, 1f), 3f, false);

            // Demostración: tres figuras que cambian de lugar.
            string[] gemas = { "gem_red_circle", "gem_blue_triangle", "gem_yellow_star" };
            var demo = new RectTransform[3];
            var numeros = new Text[3];
            for (int i = 0; i < 3; i++)
            {
                demo[i] = escena.Imagen(marco, "Demo" + i, escena.Spr(gemas[i]), new Vector2((i - 1) * 150f, -160f), new Vector2(92f, 92f), true).rectTransform;
                numeros[i] = escena.Texto(demo[i], (i + 1).ToString(), 30, Color.white, FontStyle.Bold);
                numeros[i].rectTransform.anchoredPosition = new Vector2(0f, -64f);
                Contorno(numeros[i], new Color(0.2f, 0.1f, 0.25f, 1f), 2f, false);
            }

            CanvasGroup grupo = marco.gameObject.AddComponent<CanvasGroup>();
            escena.Sonar("FluxLab/Audio/hechizo", 1f);
            Color[] colores = Arcoiris();
            for (int i = 0; i < colores.Length; i++)
                chispas.Una(FormasUI.Anillo, centro, new Vector2(200f, 100f) * (1f + i * 0.35f), colores[i], Vector2.zero, 0.8f + i * 0.08f, 0f, 0f, 4f);
            Explosion(marco.position, new Color(0.75f, 0.85f, 1f, 1f), 40);

            // El espejo aparece y el título "da la vuelta" como reflejo.
            yield return Animar(0.7f, k =>
            {
                marco.localScale = Vector3.one * Mathf.LerpUnclamped(0.3f, 1f, Rebote(Mathf.Clamp01(k / 0.6f)));
                grupo.alpha = Mathf.Clamp01(k * 3f);
                titulo.rectTransform.localScale = new Vector3(Mathf.Cos((1f - k) * Mathf.PI), 1f, 1f);
            });

            if (gato != null)
                StartCoroutine(GatoEspejo());

            // Brillo que cruza el espejo y las figuras se dan vuelta: 1 2 3 → 3 2 1.
            // La demostración se repite en ciclos mientras dura el aviso, para que dé tiempo de leer.
            const float Ciclo = 3.2f;
            float reloj = 0f;
            float duracion = Mathf.Max(2f, config.segundosMensajesGrandes);
            int ciclosCambiados = 0;
            while (reloj < duracion && !saltar)
            {
                reloj += Time.deltaTime;
                brillo.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(-700f, 700f, Mathf.Repeat(reloj / 1.8f, 1f)), 0f);
                titulo.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(reloj * 4f) * 0.03f);

                // Cada ciclo: quietas 0.7 s, se cruzan en 0.9 s, quedan al revés y al final se desvanecen para empezar de nuevo.
                float enCiclo = Mathf.Repeat(reloj, Ciclo);
                float cambio = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((enCiclo - 0.7f) / 0.9f));
                float alfa = enCiclo > Ciclo - 0.25f ? (Ciclo - enCiclo) / 0.25f : enCiclo < 0.2f ? enCiclo / 0.2f : 1f;
                for (int i = 0; i < 3; i++)
                {
                    float desde = (i - 1) * 150f;
                    float hasta = (1 - i) * 150f;
                    float arco = Mathf.Sin(cambio * Mathf.PI) * (i == 0 ? 90f : i == 2 ? -90f : 0f);
                    demo[i].anchoredPosition = new Vector2(Mathf.Lerp(desde, hasta, cambio), -160f + arco);
                    demo[i].localRotation = Quaternion.Euler(0f, 0f, cambio * 360f * (i == 1 ? 0f : 1f));
                    demo[i].GetComponent<Image>().color = new Color(1f, 1f, 1f, alfa);
                    numeros[i].color = new Color(1f, 1f, 1f, alfa);
                }

                int ciclo = Mathf.FloorToInt(reloj / Ciclo);
                if (cambio >= 1f && ciclosCambiados <= ciclo)
                {
                    ciclosCambiados = ciclo + 1;
                    escena.Sonar("Nivel2/estrella", 0.6f);
                    foreach (RectTransform d in demo)
                        Explosion(d.position, Dorado, 8);
                }

                yield return null;
            }

            yield return Animar(0.35f, k =>
            {
                marco.localScale = Vector3.one * (1f + k * 0.3f);
                grupo.alpha = 1f - k;
            });
            Destroy(marco.gameObject);
            if (gato != null)
                gato.gameObject.SetActive(false);
            yield return Cerrar();
        }

        IEnumerator GatoEspejo()
        {
            yield return gato.Entrar(new Vector2(1200f, -560f), new Vector2(700f, -330f), 0.6f);
            gato.Maullar("¡Al revés, miau!");
        }

        // ───────────────────────── Mensaje final ─────────────────────────

        /// <summary>
        /// Al terminar el nivel: letrero de madera con un mensaje de motivación.
        /// Si ganó hay confeti y Mishi festeja; si perdió, un mensaje de ánimo.
        /// </summary>
        public IEnumerator Final(int estrellas, int necesarias, bool seRompieron)
        {
            Abrir();
            MostrarTablero(false);
            yield return Animar(0.3f, k => velo.color = new Color(0.06f, 0.03f, 0.08f, 0.72f * k));

            bool gano = !seRompieron && estrellas >= necesarias;
            bool perfecto = gano && estrellas >= 3;
            string titulo = seRompieron ? "¡NO TE RINDAS!" : !gano ? "¡CASI LO LOGRAS!" : perfecto ? "¡INCREÍBLE!" : "¡LO LOGRASTE!";
            string mensaje = seRompieron
                ? "Cada intento entrena tu memoria.\n¡Tú puedes, inténtalo otra vez!"
                : !gano
                    ? "Necesitas " + necesarias + " estrellas para pasar de nivel.\n¡Inténtalo otra vez, ya casi!"
                : perfecto
                    ? "¡Todas las recetas perfectas!\nEres un verdadero alquimista."
                    : "¡Superaste el Pasillo de Frascos!\nCada vez recuerdas mejor las recetas. ¡Sigue así!";

            var grupo = new GameObject("Final", typeof(RectTransform), typeof(CanvasGroup));
            grupo.transform.SetParent(transform, false);
            var raiz = grupo.GetComponent<RectTransform>();
            var transparencia = grupo.GetComponent<CanvasGroup>();

            var letrero = escena.Imagen(raiz, "Letrero", escena.Madera, new Vector2(0f, 120f), new Vector2(980f, 230f), false);
            var textoTitulo = escena.TextoMadera(letrero.transform, titulo, 96, Vector2.zero, new Vector2(900f, 160f));
            textoTitulo.horizontalOverflow = HorizontalWrapMode.Overflow;
            textoTitulo.color = gano ? Dorado : Crema;

            // Las estrellas ganadas, encima del letrero.
            var estrellasImg = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                estrellasImg[i] = escena.Imagen(raiz, "Estrella" + i, escena.Spr("gem_yellow_star"), new Vector2((i - 1) * 120f, 300f + (i == 1 ? 20f : 0f)), new Vector2(100f, 100f), true);
                estrellasImg[i].color = new Color(0.35f, 0.32f, 0.3f, 0.8f);
                estrellasImg[i].rectTransform.localScale = Vector3.zero;
            }

            var hoja = escena.Imagen(raiz, "Pergamino", escena.Pergamino, new Vector2(0f, -110f), new Vector2(900f, 230f), false);
            hoja.type = Image.Type.Sliced;
            var textoMensaje = escena.Texto(hoja.transform, mensaje, 34, Tinta, FontStyle.Bold);
            textoMensaje.rectTransform.sizeDelta = new Vector2(660f, 170f);

            if (gano)
            {
                escena.Sonar("FluxLab/Audio/acierto", 1f);
                escena.Sonar("FluxLab/Audio/hechizo", 0.8f);
                Color[] colores = Arcoiris();
                for (int i = 0; i < colores.Length; i++)
                    chispas.Una(FormasUI.Anillo, new Vector2(0f, 120f), new Vector2(200f, 120f) * (1f + i * 0.35f), colores[i], Vector2.zero, 0.8f + i * 0.08f, 0f, 0f, 4f);
                Explosion(letrero.transform.position, Dorado, 40);
            }
            else
            {
                escena.Sonar("Nivel2/pop", 0.8f);
            }

            // El letrero cae rebotando y el pergamino se desenrolla.
            yield return Animar(0.7f, k =>
            {
                letrero.rectTransform.anchoredPosition = new Vector2(0f, Mathf.LerpUnclamped(900f, 120f, Elastico(k)));
                hoja.rectTransform.localScale = new Vector3(Mathf.LerpUnclamped(0.05f, 1f, Rebote(Mathf.Clamp01((k - 0.3f) / 0.7f))), 1f, 1f);
            });

            StartCoroutine(GatoFinal(gano));

            // Magnus a la izquierda: alegre si fue perfecto, contentito si pasó, triste dando ánimo si no.
            var magnus = MagnusAnimado.Crear(raiz, new Vector2(-720f, -400f), 400f, PersonalizacionMagnus.Guardada, AjustesJuego.Accesorio == 1);
            if (perfecto)
                magnus.FinalAlegre();
            else if (gano)
                magnus.FinalContento();
            else
                magnus.FinalAnimo();

            // Las estrellas aparecen una por una; las ganadas brillan y suenan.
            for (int i = 0; i < 3; i++)
            {
                bool ganada = i < estrellas;
                Image estrella = estrellasImg[i];
                if (ganada)
                {
                    estrella.color = Color.white;
                    escena.Sonar("Nivel2/estrella", 0.7f);
                    Explosion(estrella.transform.position, Dorado, 10);
                }

                yield return Animar(0.22f, k => estrella.rectTransform.localScale = Vector3.one * Mathf.LerpUnclamped(0f, 1f, Rebote(k)));
            }

            // Se queda unos segundos (o hasta que hagan clic) con confeti o corazones.
            saltar = false;
            float reloj = 0f;
            while (reloj < config.segundosMensajesGrandes && !saltar)
            {
                reloj += Time.deltaTime;
                textoTitulo.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(reloj * 5f) * 0.04f);
                letrero.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(reloj * 1.6f) * 1.5f);
                if (Random.value < Time.deltaTime * (gano ? 40f : 8f))
                {
                    Color c = Arcoiris()[Random.Range(0, 5)];
                    bool estrella = gano && Random.value < 0.4f;
                    chispas.Una(gano ? (estrella ? escena.Spr("gem_yellow_star") : FormasUI.Punto) : FormasUI.Corazon,
                        new Vector2(Random.Range(-900f, 900f), gano ? 600f : -560f), Vector2.one * Random.Range(14f, 30f),
                        gano ? c : new Color(1f, 0.55f, 0.7f, 0.9f),
                        new Vector2(Random.Range(-40f, 40f), gano ? Random.Range(-260f, -160f) : Random.Range(120f, 200f)),
                        Random.Range(3f, 5f), 0f, Random.Range(-200f, 200f), 0f);
                }

                yield return null;
            }

            yield return Animar(0.35f, k =>
            {
                raiz.localScale = Vector3.one * (1f + k * 0.2f);
                transparencia.alpha = 1f - k;
            });
            Destroy(grupo);
            gato.gameObject.SetActive(false);
            yield return Cerrar();
        }

        IEnumerator GatoFinal(bool gano)
        {
            // A la derecha del pergamino, para que el globito no tape el mensaje.
            yield return gato.Entrar(new Vector2(1200f, -560f), new Vector2(740f, -330f), 0.6f);
            if (gano)
                gato.SaltarDeAlegria();
            else
                gato.Maullar("¡Ánimo, miau!");
        }

        // ───────────────────────── Cada cuadro ─────────────────────────

        void Update()
        {
            float t = Time.time;
            bool pausado = Time.timeScale <= 0f;

            if (frascoIzquierdo != null && frascoIzquierdo.gameObject.activeSelf)
            {
                frascoIzquierdo.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.8f) * 10f);
                frascoDerecho.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.8f + 1.5f) * -10f);
                frascoIzquierdo.localScale = Vector3.one * (1f + Mathf.Sin(t * 3.6f) * 0.04f);
                frascoDerecho.localScale = Vector3.one * (1f + Mathf.Sin(t * 3.6f + 1.5f) * 0.04f);
            }

            // Ambiente mágico: burbujas y destellos que flotan detrás de todo.
            relojAmbiente -= Time.deltaTime;
            if (relojAmbiente <= 0f && !pausado)
            {
                relojAmbiente = 0.12f;
                bool burbuja = Random.value < 0.5f;
                chispas.Una(burbuja ? escena.Spr("burbuja_a") : FormasUI.Destello,
                    new Vector2(Random.Range(-950f, 950f), -560f), Vector2.one * Random.Range(14f, 34f),
                    burbuja ? new Color(1f, 1f, 1f, 0.6f) : new Color(1f, 0.9f, 0.6f, 0.9f),
                    new Vector2(Random.Range(-20f, 20f), Random.Range(90f, 170f)), Random.Range(4f, 7f), 0f, burbuja ? 0f : 60f, 0.2f);
            }

            if (esperandoBoton)
            {
                boton.localScale = Vector3.one * (1f + Mathf.Sin(t * 5f) * 0.06f);
                boton.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 2.5f) * 2f);
                if (Random.value < Time.deltaTime * 5f)
                    chispas.Una(FormasUI.Destello, ALocal(boton.position) + new Vector2(Random.Range(-220f, 220f), Random.Range(-56f, 56f)),
                        Vector2.one * Random.Range(18f, 32f), Dorado, new Vector2(0f, 30f), 0.6f, 0f, 90f, 0.4f);
                if (!pausado && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space)))
                    listo = true;
            }
            else if (!pausado && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)))
            {
                saltar = true;
            }
        }

        // ───────────────────────── Ayudas ─────────────────────────

        void Abrir()
        {
            gameObject.SetActive(true);
            saltar = false;
        }

        IEnumerator Cerrar()
        {
            Color inicial = velo.color;
            yield return Animar(0.3f, k => velo.color = new Color(inicial.r, inicial.g, inicial.b, inicial.a * (1f - k)));
            gameObject.SetActive(false);
        }

        void Explosion(Vector3 posicionMundo, Color color, int cantidad)
        {
            chispas.Emitir(new ParticulasUI.Rafaga
            {
                forma = FormasUI.Destello, cantidad = cantidad, centro = ALocal(posicionMundo), dispersion = new Vector2(30f, 10f),
                color = color, angulo = new Vector2(0f, 360f), rapidez = new Vector2(250f, 620f),
                vida = new Vector2(0.6f, 1.1f), tamano = new Vector2(22f, 44f), gravedad = 300f, giro = 240f
            });
            chispas.Emitir(new ParticulasUI.Rafaga
            {
                forma = escena.Spr("gem_yellow_star"), cantidad = cantidad / 2, centro = ALocal(posicionMundo), dispersion = new Vector2(20f, 10f),
                color = Color.white, angulo = new Vector2(20f, 160f), rapidez = new Vector2(300f, 600f),
                vida = new Vector2(0.8f, 1.3f), tamano = new Vector2(20f, 34f), gravedad = 600f, giro = 360f
            });
        }

        // Nubecitas de polvo cuando el tablero aterriza.
        void Polvo(Vector2 donde, float ancho)
        {
            chispas.Emitir(new ParticulasUI.Rafaga
            {
                forma = FormasUI.Humo, cantidad = 16, centro = donde, dispersion = new Vector2(ancho * 0.5f, 6f),
                color = new Color(1f, 0.93f, 0.8f, 0.6f), angulo = new Vector2(60f, 120f), rapidez = new Vector2(40f, 110f),
                vida = new Vector2(0.5f, 0.9f), tamano = new Vector2(50f, 90f), crece = 1f
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

        static void Contorno(Text texto, Color color, float grosor, bool sombra)
        {
            if (texto == null)
                return;
            var contorno = texto.gameObject.AddComponent<Outline>();
            contorno.effectColor = color;
            contorno.effectDistance = new Vector2(grosor, -grosor);
            if (!sombra)
                return;
            var debajo = texto.gameObject.AddComponent<Shadow>();
            debajo.effectColor = new Color(0f, 0f, 0f, 0.45f);
            debajo.effectDistance = new Vector2(grosor * 0.6f, -grosor * 1.4f);
        }

        static IEnumerator Animar(float duracion, System.Action<float> paso)
        {
            float reloj = 0f;
            while (reloj < duracion)
            {
                reloj += Time.deltaTime;
                paso(Mathf.Clamp01(reloj / duracion));
                yield return null;
            }

            paso(1f);
        }

        static IEnumerator Esperar(float segundos)
        {
            float reloj = 0f;
            while (reloj < segundos)
            {
                reloj += Time.deltaTime;
                yield return null;
            }
        }

        // Pasa un poco del final y vuelve (efecto "resorte").
        static float Rebote(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }

        // Rebota varias veces antes de quedarse quieto.
        static float Elastico(float t)
        {
            if (t <= 0f || t >= 1f)
                return t;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * (2f * Mathf.PI / 3f)) + 1f;
        }
    }
}
