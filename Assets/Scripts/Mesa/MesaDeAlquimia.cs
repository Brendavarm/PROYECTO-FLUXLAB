using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FluxLab
{
    /// <summary>
    /// Mesa de alquimia. La receta se memoriza y luego se repite en el caldero.
    /// La escena se arma al pulsar Play, sobre el laboratorio pintado.
    /// </summary>
    public class MesaDeAlquimia : MonoBehaviour
    {
        enum Animo
        {
            Quieto,
            Observa,
            Remueve,
            Festeja,
            SeEquivoca
        }

        const int MaxReceta = 5;
        int rondas = 3;
        int largoReceta = 3;
        float segundosMemoria = 5f;

        static readonly IngredientKind[] TrayOrder =
        {
            IngredientKind.Fuego,
            IngredientKind.Gota,
            IngredientKind.Polvo,
            IngredientKind.Hierba,
            IngredientKind.Cristal,
            IngredientKind.Lagrima
        };

        struct Ing
        {
            public string Sprite;
            public string Nombre;
            public Color Glow;
        }

        readonly Dictionary<IngredientKind, Ing> catalog = new Dictionary<IngredientKind, Ing>();

        Font font;
        Sprite solid;
        Sprite disc;
        Texture2D magnusTex;

        Text roundLabel;
        Text scoreLabel;
        Text promptLabel;
        Text timerTexto;
        Image timerFill;
        Image cauldronGlow;
        RectTransform cauldronRect;
        Vector2 cauldronBase;
        RectTransform magnusRect;
        RectTransform sombraRect;
        Vector2 magnusBase;
        Animo animo;
        float animoReloj;
        float animoDuracion;

        Image[] recipeGems;
        Text[] recipeNames;
        Image[] slotGems;
        RectTransform[] huecosReceta;
        RectTransform[] huecosCaldero;
        Text tituloTaller;
        readonly List<IngredientKind> recipe = new List<IngredientKind>();
        readonly List<IngredientKind> placed = new List<IngredientKind>();
        readonly List<float> recallTimes = new List<float>();

        static readonly string[] Pociones =
        {
            "Elixir de memoria",
            "Tintura de frascos",
            "Rocío de luna",
            "Esencia de cristal",
            "Elixir del maestro"
        };

        GameObject resultRoot;
        Text resultTitulo;
        Text resultNivel;
        Text resultPuntos;
        Text resultRango;
        Text resultPrecision;
        Text resultTiempo;
        Text resultRacha;
        Text resultPocion;
        Text resultAviso;
        Image[] resultEstrellas;
        Image resultFrasco;
        Image siguienteImagen;
        Text siguienteTexto;
        RectTransform magnusResultado;
        RectTransform[] confeti;
        AudioSource sfx;

        int score;
        int roundIndex;
        int correct;
        int racha;
        int rachaMaxima;
        bool inputOpen;
        float recallStart;
        bool built;
        Transform lienzo;
        bool vistaJardin;
        float yHuecoReceta = -6f;
        float yHuecoCaldero = 176f;
        float sombraY = -168f;
        RectTransform mundoJardin;
        Nivel3Lampara lamparaJardin;
        Image cuerpoCaldero;
        Image destelloCaldero;
        Image fogonazo;
        Text textoBoom;
        Vector2 bocaCaldero;

        void Awake()
        {
            // El nivel 3 tiene su propio código (carpeta Nivel3); aquí solo se le cede el control.
            if (AjustesJuego.NivelSeleccionado == Nivel3Juego.Nivel)
            {
                gameObject.AddComponent<Nivel3Juego>();
                enabled = false;
                return;
            }

            FillCatalog();
            font = Resources.Load<Font>("FluxLab/Nunito-Bold");
            if (AjustesJuego.NivelSeleccionado == 1)
            {
                var lilita = Resources.Load<Font>("FluxLab/Nivel3/Fuentes/LilitaOne-Regular");
                if (lilita != null)
                    font = lilita;
            }
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            solid = MakeSolid();
            disc = MakeDisc(96);
            magnusTex = AspectoMagnus.Crear();
            AplicarTaller();
            BuildScene();
            built = true;
        }

        void Start()
        {
            if (AjustesJuego.NivelSeleccionado <= 1)
            {
                var tutorial = gameObject.AddComponent<TutorialCapitulo1>();
                tutorial.Abrir(lienzo, font, BeginSession);
                return;
            }

            BeginSession();
        }

        void Update()
        {
            AnimarMagnus();
            AnimarFestejo();

            if (!inputOpen)
                return;

            for (int i = 0; i < TrayOrder.Length; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
                    TryAdd(TrayOrder[i]);
            }

            if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Space))
                Vaciar();
        }

        public void VolverAlMenu()
        {
            SceneManager.LoadScene("MenuPrincipal");
        }

        public void VolverAlMapa()
        {
            SceneManager.LoadScene("MapaNiveles");
        }

        public void BeginSession()
        {
            if (!built)
                return;

            StopAllCoroutines();
            score = 0;
            correct = 0;
            racha = 0;
            rachaMaxima = 0;
            recallTimes.Clear();
            inputOpen = false;
            if (resultRoot != null)
                resultRoot.SetActive(false);
            if (cauldronRect != null)
            {
                cauldronRect.anchoredPosition = cauldronBase;
                cauldronRect.localScale = Vector3.one;
            }

            if (cuerpoCaldero != null)
                cuerpoCaldero.color = Color.white;
            if (destelloCaldero != null)
                destelloCaldero.color = new Color(1f, 1f, 1f, 0f);
            if (fogonazo != null)
                fogonazo.color = new Color(1f, 0.55f, 0.2f, 0f);
            if (textoBoom != null)
                textoBoom.color = new Color(1f, 0.54f, 0.4f, 0f);
            if (mundoJardin != null)
                mundoJardin.anchoredPosition = Vector2.zero;

            PonerAnimo(Animo.Quieto, 0f);
            StartCoroutine(Session());
        }

        public void TryAdd(IngredientKind kind)
        {
            if (!inputOpen || placed.Count >= largoReceta)
                return;

            placed.Add(kind);
            PlayOne("click");
            PonerAnimo(Animo.Remueve, 0.46f);
            RefreshSlots(popLast: true);
            if (cauldronGlow != null)
            {
                Color glow = catalog[kind].Glow;
                glow.a = 0.55f;
                cauldronGlow.color = glow;
            }

            if (placed.Count < largoReceta)
                return;

            inputOpen = false;
            bool ok = Matches();
            recallTimes.Add(Mathf.Max(0f, Time.time - recallStart));
            ShowRecipe(true);

            if (ok)
            {
                correct++;
                score += 100;
                racha++;
                if (racha > rachaMaxima)
                    rachaMaxima = racha;
                promptLabel.text = "Correcto. La mezcla cuajó.";
                promptLabel.color = Hex("#2f7a4a");
                if (timerTexto != null)
                    timerTexto.text = "Bien hecho";
                PlayOne("acierto");
                PonerAnimo(Animo.Festeja, 0.72f);
                if (vistaJardin)
                    StartCoroutine(TriunfoJardin());
                else
                    StartCoroutine(Pulse(cauldronRect));
            }
            else
            {
                racha = 0;
                promptLabel.text = "Ese no era el orden. Así estaba el pedido.";
                promptLabel.color = Hex("#8d3d36");
                if (timerTexto != null)
                    timerTexto.text = "Casi";
                PlayOne("error");
                PonerAnimo(Animo.SeEquivoca, 0.55f);
                if (vistaJardin)
                    StartCoroutine(ExplotarJardin());
                else
                    StartCoroutine(Wobble(cauldronRect));
            }

            UpdateHud();
        }

        public void Vaciar()
        {
            if (!inputOpen || placed.Count == 0)
                return;

            placed.Clear();
            RefreshSlots(popLast: false);
            PlayOne("click");
        }

        IEnumerator Session()
        {
            for (roundIndex = 1; roundIndex <= rondas; roundIndex++)
            {
                UpdateHud();
                yield return Round();
            }

            ShowResult();
        }

        IEnumerator Round()
        {
            recipe.Clear();
            recipe.AddRange(DrawRecipe());
            placed.Clear();
            inputOpen = false;
            ShowRecipe(true);
            RefreshSlots(popLast: false);
            promptLabel.text = "Memoriza el orden, de izquierda a derecha.";
            promptLabel.color = Hex("#5a3b24");
            PonerAnimo(Animo.Observa, 0f);
            if (cauldronGlow != null)
                cauldronGlow.color = new Color(0.45f, 0.9f, 0.5f, 0.35f);

            float remaining = segundosMemoria;
            while (remaining > 0f)
            {
                remaining -= Time.deltaTime;
                float normalized = Mathf.Clamp01(remaining / segundosMemoria);
                timerFill.fillAmount = normalized;
                timerFill.color = Color.Lerp(Hex("#d4654e"), Hex("#7dce63"), normalized);
                if (timerTexto != null)
                    timerTexto.text = "Recuerda: " + Mathf.Max(0f, remaining).ToString("0.0", CultureInfo.InvariantCulture) + "s";
                yield return null;
            }

            ShowRecipe(false);
            promptLabel.text = "Ahora repítelo en el caldero.";
            promptLabel.color = Hex("#5a3b24");
            if (timerTexto != null)
                timerTexto.text = "Tu turno";
            timerFill.fillAmount = 0f;
            PonerAnimo(Animo.Quieto, 0f);
            inputOpen = true;
            recallStart = Time.time;

            while (inputOpen)
                yield return null;

            yield return new WaitForSeconds(vistaJardin ? 2.4f : 1.35f);
        }

        void ShowResult()
        {
            inputOpen = false;
            float promedio = 0f;
            for (int i = 0; i < recallTimes.Count; i++)
                promedio += recallTimes[i];
            if (recallTimes.Count > 0)
                promedio /= recallTimes.Count;

            AjustesJuego.RegistrarPuntaje(score, correct);
            string aviso = "";
            if (correct > 0 && AjustesJuego.NivelSeleccionado >= AjustesJuego.NivelAlcanzado && AjustesJuego.NivelAlcanzado < 5)
            {
                AjustesJuego.AvanzarNivel();
                aviso = "Se abrió " + NivelesTaller.De(AjustesJuego.NivelAlcanzado).nombre + ".";
            }

            int nivel = Mathf.Clamp(AjustesJuego.NivelSeleccionado, 1, 5);
            Taller taller = NivelesTaller.De(nivel);
            int estrellas = correct <= 0 ? 0 : correct >= rondas ? 3 : correct * 3 >= rondas * 2 ? 2 : 1;
            string rango = estrellas >= 3 ? "Estrella de alquimista" : estrellas == 2 ? "Mano firme" : estrellas == 1 ? "Chispa del taller" : "El caldero espera";
            int precision = rondas <= 0 ? 0 : Mathf.RoundToInt(correct * 100f / rondas);

            resultTitulo.text = "Laboratorio de alquimia";
            resultNivel.text = "Resultados de nivel  ·  Nivel " + nivel;
            resultPuntos.text = "Puntos del nivel:  " + score;
            resultRango.text = rango;
            resultPrecision.text = "Precisión     " + precision + "%";
            resultTiempo.text = "Tiempo medio     " + promedio.ToString("0.0", CultureInfo.InvariantCulture) + " s";
            resultRacha.text = "Racha máxima     " + rachaMaxima + " / " + rondas;
            resultPocion.text = "Poción del nivel " + nivel + ":  " + Pociones[nivel - 1];
            if (resultFrasco != null)
                resultFrasco.sprite = Spr(nivel == 2 ? "frasco_rojo" : nivel == 3 ? "frasco_verde" : nivel == 4 ? "frasco_morado" : "frasco_azul");
            resultAviso.text = aviso;

            for (int i = 0; i < resultEstrellas.Length; i++)
                resultEstrellas[i].color = i < estrellas && !vistaJardin ? Color.white : new Color(0.4f, 0.35f, 0.32f, 0.55f);

            if (vistaJardin)
            {
                resultTitulo.text = estrellas > 0 ? "¡Nivel completado!" : "El caldero espera";
                resultNivel.text = taller.nombre;
                resultPuntos.text = "Puntos: " + score;
                resultRango.text = rango;
                resultPrecision.text = "Precisión: " + precision + "%     Rondas: " + correct + " / " + rondas;
                resultTiempo.text = "Tiempo medio: " + promedio.ToString("0.0", CultureInfo.InvariantCulture) + " s";
                resultRacha.text = "Racha máxima: " + rachaMaxima + " / " + rondas;
                if (!string.IsNullOrEmpty(aviso))
                {
                    resultPocion.text = "¡Felicidades! Pasaste al siguiente nivel:";
                    resultAviso.text = NivelesTaller.De(AjustesJuego.NivelAlcanzado).nombre;
                    resultAviso.color = Nivel3UI.Hex("#ffd86b");
                }
                else
                {
                    resultPocion.text = "Poción del nivel: " + Pociones[nivel - 1];
                    resultAviso.text = "";
                }
            }

            bool haySiguiente = nivel < 5 && nivel + 1 <= AjustesJuego.NivelAlcanzado;
            siguienteTexto.text = haySiguiente ? "Siguiente nivel" : nivel >= 5 ? "Fin del camino" : "Aún cerrado";
            siguienteImagen.color = haySiguiente ? (vistaJardin ? Color.white : Hex("#ffe08a")) : new Color(0.72f, 0.72f, 0.72f, 1f);
            siguienteImagen.GetComponent<Button>().interactable = haySiguiente;
            resultRoot.SetActive(true);
            if (vistaJardin)
                StartCoroutine(EncenderEstrellas(estrellas));
        }

        public void IrAlSiguiente()
        {
            int nivel = AjustesJuego.NivelSeleccionado;
            if (nivel >= 5 || nivel + 1 > AjustesJuego.NivelAlcanzado)
                return;
            AjustesJuego.ElegirNivel(nivel + 1);
            SceneManager.LoadScene(nivel + 1 == 2 ? "Nivel2_Frascos" : "MesaDeAlquimia");
        }

        void UpdateHud()
        {
            roundLabel.text = "Ronda " + roundIndex + " / " + rondas;
            scoreLabel.text = "Puntos: " + score;
        }

        bool Matches()
        {
            if (placed.Count != recipe.Count)
                return false;
            for (int i = 0; i < recipe.Count; i++)
            {
                if (placed[i] != recipe[i])
                    return false;
            }
            return true;
        }

        List<IngredientKind> DrawRecipe()
        {
            var pool = new List<IngredientKind>(TrayOrder);
            for (int i = 0; i < pool.Count; i++)
            {
                int swap = Random.Range(i, pool.Count);
                IngredientKind tmp = pool[i];
                pool[i] = pool[swap];
                pool[swap] = tmp;
            }

            return pool.GetRange(0, largoReceta);
        }

        void ShowRecipe(bool visible)
        {
            for (int i = 0; i < largoReceta; i++)
            {
                recipeGems[i].sprite = Spr(catalog[recipe[i]].Sprite);
                recipeGems[i].enabled = visible;
                if (recipeNames != null)
                    recipeNames[i].text = visible ? catalog[recipe[i]].Nombre : "?";
            }
        }

        void RefreshSlots(bool popLast)
        {
            for (int i = 0; i < largoReceta; i++)
            {
                bool filled = i < placed.Count;
                slotGems[i].enabled = filled;
                slotGems[i].rectTransform.localScale = Vector3.one;
                if (filled)
                    slotGems[i].sprite = Spr(catalog[placed[i]].Sprite);
            }

            if (popLast && placed.Count > 0)
                StartCoroutine(Pop(slotGems[placed.Count - 1].rectTransform));
        }

        void BuildScene()
        {
            EnsureEventSystem();
            var canvas = CreateCanvas();
            lienzo = canvas.transform;
            Transform escena = lienzo;
            if (vistaJardin)
            {
                var mundo = new GameObject("Mundo", typeof(RectTransform));
                mundo.transform.SetParent(lienzo, false);
                mundoJardin = mundo.GetComponent<RectTransform>();
                Stretch(mundoJardin, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                escena = mundo.transform;
            }

            BuildRoom(escena);
            if (vistaJardin)
                AdornarCielo(escena);
            BuildCast(escena);
            BuildCauldron(escena);
            if (vistaJardin)
                AdornarFrascos(escena);
            BuildRecipe(escena);
            BuildTray(escena);
            BuildHud(escena);
            if (vistaJardin)
                CrearFogonazo();
            BuildResult(lienzo);
            BuildAudio();
        }

        void AplicarTaller()
        {
            Taller taller = NivelesTaller.De(AjustesJuego.NivelSeleccionado);
            rondas = taller.rondas;
            largoReceta = Mathf.Clamp(taller.receta, 1, MaxReceta);
            segundosMemoria = taller.segundos;
            vistaJardin = AjustesJuego.NivelSeleccionado == 1;
        }

        void AcomodarHuecos()
        {
            if (huecosReceta == null || huecosCaldero == null)
                return;

            float paso = largoReceta >= 5 ? 104f : largoReceta == 4 ? 122f : 148f;
            float pasoCaldero = largoReceta >= 5 ? 84f : largoReceta == 4 ? 96f : 104f;
            for (int i = 0; i < MaxReceta; i++)
            {
                bool activo = i < largoReceta;
                huecosReceta[i].gameObject.SetActive(activo);
                huecosCaldero[i].gameObject.SetActive(activo);
                if (!activo)
                    continue;
                float centro = (largoReceta - 1) * 0.5f;
                huecosReceta[i].anchoredPosition = new Vector2((i - centro) * paso, yHuecoReceta);
                huecosCaldero[i].anchoredPosition = new Vector2((i - centro) * pasoCaldero, yHuecoCaldero);
            }
        }

        void BuildRoom(Transform parent)
        {
            Texture2D textura = null;
            if (vistaJardin)
                textura = Nivel3UI.LeerTextura("Menu/fondo_nivel3.png");
            if (textura == null && vistaJardin)
                textura = Nivel3UI.LeerTextura("Menu/fondo_sin_gato.png");
            if (textura == null)
                textura = Archivo("Menu/fondo.png");

            var fondo = CreateRaw(parent, "Laboratorio", textura, Color.white);
            Stretch(fondo.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        void AdornarCielo(Transform parent)
        {
            Nivel3Luna.Crear(parent, new Vector2(200f, 440f), 150f);
            lamparaJardin = Nivel3Lampara.CrearSoloLuz(parent, new Vector2(-630f, -139f), 190f);
        }

        void AdornarFrascos(Transform parent)
        {
            var rojo = new Color(0.95f, 0.3f, 0.15f, 0.85f);
            var verde = new Color(0.4f, 0.9f, 0.25f, 0.85f);
            var azul = new Color(0.2f, 0.45f, 1f, 0.85f);
            var amarillo = new Color(1f, 0.8f, 0.2f, 0.85f);
            var morado = new Color(0.65f, 0.3f, 1f, 0.85f);
            var oscuro = new Color(0.8f, 0.25f, 0.9f, 0.85f);
            FrascosMesa.Crear(parent, new[]
            {
                new FrascosMesa.Def(2, azul, -330f, -255f, 140f, 0.037f),
                new FrascosMesa.Def(0, rojo, 300f, -255f, 120f, 0.015f),
                new FrascosMesa.Def(1, verde, 420f, -255f, 140f, 0.022f),
                new FrascosMesa.Def(3, amarillo, 545f, -255f, 120f, 0.033f),
                new FrascosMesa.Def(4, morado, 410f, 116f, 110f, 0.057f),
                new FrascosMesa.Def(7, oscuro, 905f, -55f, 105f, 0.010f)
            }, -255f, -330f, -690f, 730f);
        }

        void BuildCast(Transform parent)
        {
            sombraY = vistaJardin ? -259f : -168f;
            Vector2 sombraPos = vistaJardin ? new Vector2(-520f, sombraY) : new Vector2(-430f, sombraY);
            var sombra = CreateImage(parent, "SombraMagnus", disc, sombraPos, new Vector2(130f, 26f), true);
            sombra.color = new Color(0.08f, 0.04f, 0.03f, 0.35f);
            sombraRect = sombra.rectTransform;

            var cuerpo = CreateRaw(parent, "Magnus", magnusTex != null ? magnusTex : Tex("magnus"), Color.white);
            magnusRect = cuerpo.rectTransform;
            magnusRect.anchorMin = magnusRect.anchorMax = new Vector2(0.5f, 0.5f);
            magnusRect.pivot = new Vector2(0.5f, 0.5f);
            magnusRect.sizeDelta = new Vector2(140f, 380f);
            magnusBase = vistaJardin ? new Vector2(-520f, -65f) : new Vector2(-430f, 22f);
            magnusRect.anchoredPosition = magnusBase;

            if (AjustesJuego.Accesorio == 1)
            {
                var frasco = CreateImage(cuerpo.transform, "Frasco", Spr("frasco_morado"), new Vector2(-36f, -28f), new Vector2(36f, 48f), true);
                frasco.rectTransform.localScale = Vector3.one;
            }
        }

        void BuildCauldron(Transform parent)
        {
            var vivo = new GameObject("CalderoVivo", typeof(RectTransform));
            vivo.transform.SetParent(parent, false);
            cauldronRect = vivo.GetComponent<RectTransform>();
            cauldronRect.anchorMin = cauldronRect.anchorMax = new Vector2(0.5f, 0.5f);
            cauldronRect.pivot = new Vector2(0.5f, 0.5f);
            Sprite dibujoCaldero = vistaJardin ? Nivel3UI.SpriteOpcional("FluxLab/Nivel3/caldero") : null;
            float altoCaldero = dibujoCaldero != null ? 330f : 160f;
            float anchoCaldero = dibujoCaldero != null ? altoCaldero * dibujoCaldero.rect.width / dibujoCaldero.rect.height : 280f;
            float centroY = dibujoCaldero != null ? -255f - altoCaldero * 0.057f + altoCaldero * 0.5f : 128f;
            cauldronRect.sizeDelta = new Vector2(anchoCaldero, altoCaldero);
            cauldronBase = new Vector2(dibujoCaldero != null ? 0f : 8f, centroY);
            cauldronRect.anchoredPosition = cauldronBase;
            float yBrillo = dibujoCaldero != null ? altoCaldero * 0.252f : 8f;
            bocaCaldero = cauldronBase + new Vector2(0f, yBrillo);
            if (dibujoCaldero != null)
            {
                cuerpoCaldero = CreateImage(vivo.transform, "Cuerpo", dibujoCaldero, Vector2.zero, new Vector2(anchoCaldero, altoCaldero), true);
                cuerpoCaldero.transform.SetAsFirstSibling();
                yHuecoCaldero = centroY + altoCaldero * 0.252f + 150f;
                destelloCaldero = CreateImage(vivo.transform, "Destello", disc, new Vector2(0f, yBrillo), new Vector2(anchoCaldero * 1.7f, altoCaldero * 1.3f), true);
                destelloCaldero.color = new Color(1f, 1f, 1f, 0f);
            }

            cauldronGlow = CreateImage(vivo.transform, "Brillo", disc, new Vector2(0f, yBrillo), dibujoCaldero != null ? new Vector2(anchoCaldero * 0.9f, 80f) : new Vector2(210f, 72f), true);
            cauldronGlow.color = new Color(0.45f, 0.9f, 0.5f, 0.28f);

            SpawnBubble(vivo.transform, "burbuja_a", new Vector2(-28f, 4f), new Vector2(34f, 34f), 28f, 10f);
            SpawnBubble(vivo.transform, "burbuja_b", new Vector2(22f, -6f), new Vector2(22f, 22f), 36f, 8f);
            SpawnBubble(vivo.transform, "burbuja_c", new Vector2(2f, 10f), new Vector2(28f, 28f), 22f, 12f);

            slotGems = new Image[MaxReceta];
            huecosCaldero = new RectTransform[MaxReceta];
            for (int i = 0; i < MaxReceta; i++)
            {
                var estacion = new GameObject("HuecoCaldero" + i, typeof(RectTransform));
                estacion.transform.SetParent(parent, false);
                huecosCaldero[i] = estacion.GetComponent<RectTransform>();
                huecosCaldero[i].anchorMin = huecosCaldero[i].anchorMax = new Vector2(0.5f, 0.5f);
                huecosCaldero[i].pivot = new Vector2(0.5f, 0.5f);
                slotGems[i] = CreateSocket(estacion.transform, Vector2.zero, 70f);
            }

            AcomodarHuecos();
        }

        void BuildRecipe(Transform parent)
        {
            Sprite pergamino = vistaJardin ? Nivel3UI.SpriteUI("pergamino") : null;
            bool arte = pergamino != null;
            yHuecoReceta = arte ? 10f : -6f;
            var board = CreateImage(parent, "Receta", arte ? pergamino : SpriteArchivo("Menu/pergamino.png"),
                arte ? new Vector2(20f, 348f) : new Vector2(30f, 368f),
                arte ? new Vector2(700f, 375f) : new Vector2(700f, 286f), true);

            var title = MakeText(board.transform, "La receta", arte ? 40 : 32, Hex("#5a3b24"), FontStyle.Bold);
            title.rectTransform.anchoredPosition = new Vector2(0f, arte ? 118f : 92f);
            title.rectTransform.sizeDelta = new Vector2(520f, 42f);

            promptLabel = MakeText(board.transform, "Memoriza el orden, de izquierda a derecha.", arte ? 24 : 22, Hex("#5a3b24"), FontStyle.Normal);
            promptLabel.rectTransform.anchoredPosition = new Vector2(0f, arte ? 74f : 54f);
            promptLabel.rectTransform.sizeDelta = new Vector2(560f, 34f);

            recipeGems = new Image[MaxReceta];
            recipeNames = new Text[MaxReceta];
            huecosReceta = new RectTransform[MaxReceta];
            for (int i = 0; i < MaxReceta; i++)
            {
                var estacion = new GameObject("HuecoReceta" + i, typeof(RectTransform));
                estacion.transform.SetParent(board.transform, false);
                huecosReceta[i] = estacion.GetComponent<RectTransform>();
                huecosReceta[i].anchorMin = huecosReceta[i].anchorMax = new Vector2(0.5f, 0.5f);
                huecosReceta[i].pivot = new Vector2(0.5f, 0.5f);
                recipeGems[i] = CreateSocket(estacion.transform, Vector2.zero, 76f);
                var number = MakeText(estacion.transform, (i + 1).ToString(), 18, Hex("#8a6848"), FontStyle.Bold);
                number.rectTransform.anchoredPosition = new Vector2(-34f, 28f);
                number.rectTransform.sizeDelta = new Vector2(28f, 24f);
                recipeNames[i] = MakeText(estacion.transform, "?", arte ? 22 : 18, Hex("#5a3b24"), FontStyle.Bold);
                recipeNames[i].rectTransform.anchoredPosition = new Vector2(0f, arte ? -60f : -52f);
                recipeNames[i].rectTransform.sizeDelta = new Vector2(120f, 26f);
            }

            AcomodarHuecos();

            timerTexto = MakeText(board.transform, "Recuerda: 5.0s", arte ? 26 : 20, Hex("#6a4b32"), FontStyle.Bold);
            timerTexto.rectTransform.anchoredPosition = new Vector2(0f, arte ? -102f : -88f);
            timerTexto.rectTransform.sizeDelta = new Vector2(420f, 28f);

            var track = CreateImage(board.transform, "Tiempo", solid, new Vector2(0f, arte ? -130f : -114f), new Vector2(arte ? 440f : 460f, arte ? 12f : 14f), false);
            track.color = Hex("#d7c3a2");
            timerFill = CreateImage(track.transform, "Relleno", solid, Vector2.zero, track.rectTransform.sizeDelta, false);
            timerFill.color = Hex("#7dce63");
            timerFill.type = Image.Type.Filled;
            timerFill.fillMethod = Image.FillMethod.Horizontal;
            timerFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            timerFill.fillAmount = 1f;
            Stretch(timerFill.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        void BuildTray(Transform parent)
        {
            if (vistaJardin && Nivel3UI.SpriteUI("bandeja") != null)
            {
                BuildTrayJardin(parent);
                return;
            }

            var tray = CreateImage(parent, "Bandeja", Spr("panel_beige"), new Vector2(-40f, -470f), new Vector2(1280f, 168f), false);
            tray.type = Image.Type.Sliced;
            tray.color = new Color(0.91f, 0.78f, 0.58f, 0.94f);

            const float cardWidth = 176f;
            const float gap = 14f;
            float total = TrayOrder.Length * cardWidth + (TrayOrder.Length - 1) * gap;
            float x = -total * 0.5f + cardWidth * 0.5f - 70f;

            for (int i = 0; i < TrayOrder.Length; i++)
            {
                IngredientKind kind = TrayOrder[i];
                Ing info = catalog[kind];
                var card = CreateImage(parent, "Carta_" + kind, Spr("panel_beige"), new Vector2(x, -468f), new Vector2(cardWidth, 142f), false);
                card.type = Image.Type.Sliced;
                card.color = Hex("#fff8ee");
                card.raycastTarget = true;
                var shadow = card.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0.28f, 0.16f, 0.08f, 0.28f);
                shadow.effectDistance = new Vector2(0f, -4f);

                var button = card.gameObject.AddComponent<Button>();
                button.targetGraphic = card;
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                var colors = button.colors;
                colors.highlightedColor = Hex("#fff3dd");
                colors.pressedColor = Hex("#ead7b6");
                colors.fadeDuration = 0.08f;
                button.colors = colors;
                IngredientKind captured = kind;
                button.onClick.AddListener(() => TryAdd(captured));
                card.gameObject.AddComponent<HoverScale>().scale = 1.05f;

                CreateImage(card.transform, "Gema", Spr(info.Sprite), new Vector2(0f, 18f), new Vector2(64f, 64f), true);
                var banda = CreateImage(card.transform, "Banda", solid, new Vector2(0f, -48f), new Vector2(cardWidth - 18f, 32f), false);
                banda.color = info.Glow;
                var name = MakeText(card.transform, info.Nombre, 18, Color.white, FontStyle.Bold);
                name.rectTransform.anchoredPosition = new Vector2(0f, -48f);
                name.rectTransform.sizeDelta = new Vector2(cardWidth - 16f, 28f);
                AddOutline(name, new Color(0.15f, 0.08f, 0.05f, 0.55f));
                var key = MakeText(card.transform, (i + 1).ToString(), 18, Hex("#8a6848"), FontStyle.Bold);
                key.alignment = TextAnchor.UpperLeft;
                key.rectTransform.anchorMin = new Vector2(0f, 1f);
                key.rectTransform.anchorMax = new Vector2(0f, 1f);
                key.rectTransform.pivot = new Vector2(0f, 1f);
                key.rectTransform.anchoredPosition = new Vector2(12f, -8f);
                key.rectTransform.sizeDelta = new Vector2(28f, 24f);

                x += cardWidth + gap;
            }

            var clear = CreateImage(parent, "Vaciar", Spr("button_brown"), new Vector2(760f, -468f), new Vector2(168f, 86f), false);
            clear.type = Image.Type.Sliced;
            clear.raycastTarget = true;
            var clearButton = clear.gameObject.AddComponent<Button>();
            clearButton.targetGraphic = clear;
            clearButton.navigation = new Navigation { mode = Navigation.Mode.None };
            clearButton.onClick.AddListener(Vaciar);
            var clearLabel = MakeText(clear.transform, "Vaciar", 26, Hex("#f6efe4"), FontStyle.Bold);
            Stretch(clearLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        void BuildTrayJardin(Transform parent)
        {
            const float bandejaAncho = 1509f;
            const float bandejaAlto = 174f;
            const float cartaAncho = 206f;
            const float cartaAlto = 149f;
            const float cartaCentroY = 80.5f;
            float[] cartaCentroX = { 137f, 382f, 628f, 876f, 1123f, 1373f };
            const float ancho = 1352f;
            float k = ancho / bandejaAncho;
            var centro = new Vector2(-90f, -440f);
            var arte = Nivel3UI.SpriteUI("bandeja");
            CreateImage(parent, "Bandeja", arte, centro, new Vector2(ancho, bandejaAlto * k), true);

            for (int i = 0; i < TrayOrder.Length && i < cartaCentroX.Length; i++)
            {
                IngredientKind kind = TrayOrder[i];
                Ing info = catalog[kind];
                float cx = (cartaCentroX[i] - bandejaAncho * 0.5f) * k;
                float cy = (bandejaAlto * 0.5f - cartaCentroY) * k;
                var carta = CreateImage(parent, "Carta_" + kind, solid, centro + new Vector2(cx, cy), new Vector2(cartaAncho * k, cartaAlto * k), false);
                carta.color = new Color(1f, 1f, 1f, 0.01f);
                carta.raycastTarget = true;
                var boton = carta.gameObject.AddComponent<Button>();
                boton.targetGraphic = carta;
                boton.transition = Selectable.Transition.None;
                boton.navigation = new Navigation { mode = Navigation.Mode.None };
                IngredientKind captured = kind;
                boton.onClick.AddListener(() => TryAdd(captured));
                carta.gameObject.AddComponent<HoverScale>().scale = 1.06f;

                CreateImage(carta.transform, "Gema", Spr(info.Sprite), new Vector2(0f, 16f), new Vector2(64f, 64f), true);
                var name = MakeText(carta.transform, info.Nombre, 25, Hex("#fff3da"), FontStyle.Bold);
                name.rectTransform.anchoredPosition = new Vector2(0f, -36f);
                name.rectTransform.sizeDelta = new Vector2(170f, 34f);
                AddOutline(name, new Color(0.1f, 0.05f, 0.03f, 0.75f));
                var numero = MakeText(carta.transform, (i + 1).ToString(), 22, Hex("#fff3da"), FontStyle.Bold);
                numero.rectTransform.anchoredPosition = new Vector2(-70f, 44f);
                numero.rectTransform.sizeDelta = new Vector2(34f, 28f);
                AddOutline(numero, new Color(0.1f, 0.05f, 0.03f, 0.75f));
            }

            var vaciar = Nivel3UI.BotonConArte(parent, "boton_ancho", "Vaciar", new Vector2(790f, -440f), new Vector2(252f, 95f), Vaciar, 34);
            if (vaciar.transform.childCount > 0)
                vaciar.transform.GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector2(26f, 0f);
            Nivel3UI.Icono(vaciar.transform, "pocion.png", new Vector2(-66f, 0f), 48f);
        }

        void BuildHud(Transform parent)
        {
            if (vistaJardin && Nivel3UI.SpriteUI("placa_titulo") != null)
            {
                BuildHudJardin(parent);
                return;
            }

            var izquierda = CreateImage(parent, "PlacaTitulo", Spr("panel_beige"), new Vector2(-740f, 470f), new Vector2(340f, 86f), false);
            izquierda.type = Image.Type.Sliced;
            tituloTaller = MakeText(izquierda.transform, NivelesTaller.De(AjustesJuego.NivelSeleccionado).nombre, 20, Hex("#5a3b24"), FontStyle.Bold);
            tituloTaller.rectTransform.sizeDelta = new Vector2(310f, 70f);

            var mapa = CreateImage(parent, "IrAlMapa", Spr("button_brown"), new Vector2(-760f, 390f), new Vector2(180f, 52f), false);
            mapa.type = Image.Type.Sliced;
            mapa.raycastTarget = true;
            var mapaBoton = mapa.gameObject.AddComponent<Button>();
            mapaBoton.targetGraphic = mapa;
            mapaBoton.onClick.AddListener(VolverAlMapa);
            var mapaTexto = MakeText(mapa.transform, "Mapa", 22, Hex("#f6efe4"), FontStyle.Bold);
            Stretch(mapaTexto.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var derecha = CreateImage(parent, "PlacaMarcador", Spr("panel_beige"), new Vector2(760f, 470f), new Vector2(280f, 92f), false);
            derecha.type = Image.Type.Sliced;
            roundLabel = MakeText(derecha.transform, "Ronda 1 / 3", 24, Hex("#5a3b24"), FontStyle.Bold);
            roundLabel.rectTransform.anchoredPosition = new Vector2(0f, 16f);
            roundLabel.rectTransform.sizeDelta = new Vector2(250f, 32f);
            scoreLabel = MakeText(derecha.transform, "Puntos: 0", 22, Hex("#8a5a20"), FontStyle.Bold);
            scoreLabel.rectTransform.anchoredPosition = new Vector2(0f, -16f);
            scoreLabel.rectTransform.sizeDelta = new Vector2(250f, 30f);
        }

        void BuildHudJardin(Transform parent)
        {
            Color texto = Hex("#f3e4c4");
            Color puntos = Hex("#e6c98b");
            Color sombra = new Color(0.1f, 0.05f, 0.03f, 0.7f);

            var placaNivel = Nivel3UI.Placa(parent, "placa_titulo", new Vector2(-690f, 398f), new Vector2(460f, 137f));
            tituloTaller = MakeText(placaNivel.transform, NivelesTaller.De(AjustesJuego.NivelSeleccionado).nombre, 36, texto, FontStyle.Normal);
            tituloTaller.rectTransform.sizeDelta = new Vector2(400f, 90f);
            AddOutline(tituloTaller, sombra);

            var mapa = Nivel3UI.BotonConArte(parent, "boton_ancho", "Mapa", new Vector2(-790f, 282f), new Vector2(210f, 79f), VolverAlMapa, 29);
            if (mapa.transform.childCount > 0)
                mapa.transform.GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector2(22f, 0f);
            Nivel3UI.Icono(mapa.transform, "puerta.png", new Vector2(-52f, 0f), 36f);

            var placaRonda = Nivel3UI.Placa(parent, "placa_ronda", new Vector2(640f, 398f), new Vector2(330f, 125f));
            roundLabel = MakeText(placaRonda.transform, "Ronda 1 / 3", 32, texto, FontStyle.Normal);
            roundLabel.rectTransform.anchoredPosition = new Vector2(0f, 17f);
            roundLabel.rectTransform.sizeDelta = new Vector2(280f, 44f);
            AddOutline(roundLabel, sombra);
            scoreLabel = MakeText(placaRonda.transform, "Puntos: 0", 26, puntos, FontStyle.Normal);
            scoreLabel.rectTransform.anchoredPosition = new Vector2(0f, -22f);
            scoreLabel.rectTransform.sizeDelta = new Vector2(280f, 36f);
            AddOutline(scoreLabel, sombra);
        }

        void BuildResult(Transform parent)
        {
            if (vistaJardin)
            {
                BuildResultJardin(parent);
                return;
            }

            resultRoot = new GameObject("Resultado", typeof(RectTransform));
            resultRoot.transform.SetParent(parent, false);
            Stretch(resultRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Transform raiz = resultRoot.transform;

            Texture2D fondoResultado = vistaJardin ? Nivel3UI.LeerTextura("Menu/fondo_nivel3.png") : null;
            if (fondoResultado == null)
                fondoResultado = Archivo("Menu/fondo.png");
            var fondo = CreateRaw(raiz, "FondoResultado", fondoResultado, Color.white);
            Stretch(fondo.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            fondo.raycastTarget = true;

            Sprite cintaSprite = vistaJardin ? Nivel3UI.SpriteUI("pergamino") : null;
            var cinta = CreateImage(raiz, "Cinta", cintaSprite != null ? cintaSprite : SpriteArchivo("Menu/pergamino.png"), new Vector2(0f, 455f), new Vector2(700f, 200f), true);
            resultTitulo = MakeText(cinta.transform, "Laboratorio de alquimia", 30, Hex("#5a3b24"), FontStyle.Bold);
            resultTitulo.rectTransform.anchoredPosition = new Vector2(0f, 18f);
            resultTitulo.rectTransform.sizeDelta = new Vector2(420f, 40f);
            resultNivel = MakeText(cinta.transform, "", 18, Hex("#6a4b32"), FontStyle.Bold);
            resultNivel.rectTransform.anchoredPosition = new Vector2(0f, -22f);
            resultNivel.rectTransform.sizeDelta = new Vector2(420f, 28f);

            var izquierda = CreateImage(raiz, "Puntos", Spr("panel_beige"), new Vector2(-610f, 30f), new Vector2(470f, 500f), false);
            izquierda.type = Image.Type.Sliced;
            resultPuntos = MakeText(izquierda.transform, "Puntos del nivel", 26, Hex("#5a3b24"), FontStyle.Bold);
            resultPuntos.rectTransform.anchoredPosition = new Vector2(0f, 180f);
            resultPuntos.rectTransform.sizeDelta = new Vector2(400f, 40f);

            var marco = CreateImage(izquierda.transform, "MarcoEstrellas", solid, new Vector2(0f, 40f), new Vector2(390f, 168f), false);
            marco.color = Hex("#6a4630");
            var interior = CreateImage(marco.transform, "Interior", solid, Vector2.zero, new Vector2(366f, 144f), false);
            interior.color = Hex("#3c2a22");
            resultEstrellas = new Image[3];
            for (int i = 0; i < 3; i++)
                resultEstrellas[i] = CreateImage(interior.transform, "Estrella" + i, Spr("gem_yellow_star"), new Vector2((i - 1) * 112f, 0f), new Vector2(96f, 96f), true);

            resultRango = MakeText(izquierda.transform, "", 28, Hex("#5a3b24"), FontStyle.Bold);
            resultRango.rectTransform.anchoredPosition = new Vector2(0f, -150f);
            resultRango.rectTransform.sizeDelta = new Vector2(400f, 80f);

            var derecha = CreateImage(raiz, "Desempeno", Spr("panel_beige"), new Vector2(610f, 30f), new Vector2(470f, 500f), false);
            derecha.type = Image.Type.Sliced;
            var desemTitulo = MakeText(derecha.transform, "Desempeño", 30, Hex("#5a3b24"), FontStyle.Bold);
            desemTitulo.rectTransform.anchoredPosition = new Vector2(0f, 180f);
            desemTitulo.rectTransform.sizeDelta = new Vector2(400f, 42f);
            resultPrecision = LineaResultado(derecha.transform, 70f);
            resultTiempo = LineaResultado(derecha.transform, -10f);
            resultRacha = LineaResultado(derecha.transform, -90f);

            var brillo = CreateImage(raiz, "BrilloPocion", disc, new Vector2(-150f, 90f), new Vector2(180f, 180f), true);
            brillo.color = new Color(0.7f, 0.95f, 1f, 0.45f);
            var peana = CreateImage(raiz, "Peana", Spr("mesa"), new Vector2(-150f, -20f), new Vector2(210f, 130f), true);
            resultFrasco = CreateImage(raiz, "Pocion", Spr("frasco_azul"), new Vector2(-150f, 100f), new Vector2(110f, 160f), true);

            var magnus = CreateRaw(raiz, "MagnusResultado", magnusTex != null ? magnusTex : Tex("magnus"), Color.white);
            magnusResultado = magnus.rectTransform;
            magnusResultado.anchorMin = magnusResultado.anchorMax = new Vector2(0.5f, 0.5f);
            magnusResultado.pivot = new Vector2(0.5f, 0.5f);
            magnusResultado.sizeDelta = new Vector2(190f, 500f);
            magnusResultado.anchoredPosition = new Vector2(90f, 55f);

            confeti = new RectTransform[16];
            Color[] colores =
            {
                Hex("#f2d15a"), Hex("#7dce63"), Hex("#f08a9a"), Hex("#8fd4f2"),
                Hex("#e7b15a"), Hex("#c9f27a"), Hex("#f6efe4"), Hex("#d98cff")
            };
            for (int i = 0; i < confeti.Length; i++)
            {
                bool estrella = i % 3 != 0;
                float x = -220f + (i % 8) * 58f;
                float y = -60f + (i / 8) * 90f;
                var pieza = CreateImage(raiz, "Confeti" + i, estrella ? Spr("gem_yellow_star") : disc, new Vector2(x, y), estrella ? new Vector2(26f, 26f) : new Vector2(16f, 16f), true);
                pieza.color = colores[i % colores.Length];
                confeti[i] = pieza.rectTransform;
            }

            var pocion = CreateImage(raiz, "NombrePocion", Spr("panel_beige"), new Vector2(0f, -300f), new Vector2(700f, 92f), false);
            pocion.type = Image.Type.Sliced;
            resultPocion = MakeText(pocion.transform, "", 24, Hex("#5a3b24"), FontStyle.Bold);
            resultPocion.rectTransform.anchoredPosition = new Vector2(0f, 14f);
            resultPocion.rectTransform.sizeDelta = new Vector2(640f, 36f);
            resultAviso = MakeText(pocion.transform, "", 18, Hex("#6a4b32"), FontStyle.Bold);
            resultAviso.rectTransform.anchoredPosition = new Vector2(0f, -22f);
            resultAviso.rectTransform.sizeDelta = new Vector2(640f, 28f);

            CrearBotonResultado(raiz, "Menú", Spr("button_beige"), new Vector2(-560f, -455f), new Vector2(240f, 78f), Hex("#3b2a22"), VolverAlMenu);
            CrearBotonResultado(raiz, "Reintentar", Spr("button_brown"), new Vector2(-250f, -455f), new Vector2(280f, 78f), Hex("#f6efe4"), BeginSession);
            siguienteImagen = CrearBotonResultado(raiz, "Siguiente nivel", Spr("button_brown"), new Vector2(140f, -452f), new Vector2(360f, 90f), Hex("#3b2a22"), IrAlSiguiente);
            siguienteTexto = siguienteImagen.GetComponentInChildren<Text>();
            CrearBotonResultado(raiz, "Volver", Spr("button_beige"), new Vector2(620f, -455f), new Vector2(220f, 78f), Hex("#3b2a22"), VolverAlMapa);

            resultRoot.SetActive(false);
        }

        void BuildResultJardin(Transform parent)
        {
            resultRoot = new GameObject("Resultado", typeof(RectTransform));
            resultRoot.transform.SetParent(parent, false);
            Stretch(resultRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Transform raiz = resultRoot.transform;

            var oscuro = Nivel3UI.Imagen(raiz, "Oscuro", Nivel3UI.Solido(), Vector2.zero, Vector2.zero);
            Nivel3UI.Estirar(oscuro.rectTransform);
            oscuro.color = new Color(0.05f, 0.03f, 0.16f, 0.82f);
            oscuro.raycastTarget = true;

            var panel = Nivel3UI.Panel(raiz, "Tarjeta", Vector2.zero, new Vector2(1180f, 900f));
            Color tinta = Nivel3UI.Hex("#5a3b24");
            Color oro = Nivel3UI.Hex("#ffd86b");

            resultTitulo = Nivel3UI.Texto(panel.transform, "¡Nivel completado!", 76, oro, new Vector2(0f, 340f), new Vector2(1000f, 100f), TextAnchor.MiddleCenter, true);
            resultNivel = Nivel3UI.Texto(panel.transform, "", 36, tinta, new Vector2(0f, 262f), new Vector2(900f, 50f));

            resultEstrellas = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                resultEstrellas[i] = Nivel3UI.Imagen(panel.transform, "Estrella" + i, Nivel3UI.Spr("gem_yellow_star"), new Vector2((i - 1) * 170f, 155f), new Vector2(140f, 140f), true);
                resultEstrellas[i].color = new Color(0.4f, 0.35f, 0.32f, 0.55f);
            }

            resultRango = Nivel3UI.Texto(panel.transform, "", 40, oro, new Vector2(0f, 50f), new Vector2(900f, 56f), TextAnchor.MiddleCenter, true);
            resultPuntos = Nivel3UI.Texto(panel.transform, "Puntos: 0", 48, tinta, new Vector2(0f, -10f), new Vector2(900f, 64f));
            resultPrecision = Nivel3UI.Texto(panel.transform, "", 32, Nivel3UI.Hex("#6a4b32"), new Vector2(0f, -70f), new Vector2(980f, 44f));
            resultTiempo = Nivel3UI.Texto(panel.transform, "", 32, Nivel3UI.Hex("#6a4b32"), new Vector2(0f, -118f), new Vector2(980f, 44f));
            resultRacha = Nivel3UI.Texto(panel.transform, "", 32, Nivel3UI.Hex("#6a4b32"), new Vector2(0f, -166f), new Vector2(980f, 44f));
            resultPocion = Nivel3UI.Texto(panel.transform, "", 36, Nivel3UI.Hex("#2f7a4a"), new Vector2(0f, -230f), new Vector2(1000f, 50f));
            resultAviso = Nivel3UI.Texto(panel.transform, "", 52, oro, new Vector2(0f, -290f), new Vector2(1000f, 70f), TextAnchor.MiddleCenter, true);

            Nivel3UI.Boton(panel.transform, "Menú", new Vector2(-360f, -390f), new Vector2(210f, 88f), VolverAlMenu, 30);
            Nivel3UI.Boton(panel.transform, "Reintentar", new Vector2(-110f, -390f), new Vector2(250f, 96f), BeginSession, 32);
            siguienteImagen = Nivel3UI.Boton(panel.transform, "Siguiente nivel", new Vector2(180f, -390f), new Vector2(320f, 104f), IrAlSiguiente, 30);
            siguienteTexto = siguienteImagen.GetComponentInChildren<Text>();
            Nivel3UI.Boton(panel.transform, "Mapa", new Vector2(450f, -390f), new Vector2(190f, 88f), VolverAlMapa, 30);

            resultRoot.SetActive(false);
        }

        void CrearFogonazo()
        {
            fogonazo = Nivel3UI.Imagen(lienzo, "Fogonazo", Nivel3UI.Solido(), Vector2.zero, Vector2.zero);
            Nivel3UI.Estirar(fogonazo.rectTransform);
            fogonazo.color = new Color(1f, 0.55f, 0.2f, 0f);
            fogonazo.raycastTarget = false;

            textoBoom = Nivel3UI.Texto(lienzo, "", 64, Nivel3UI.Hex("#ff8a66"), new Vector2(0f, 80f), new Vector2(1200f, 90f), TextAnchor.MiddleCenter, true);
            textoBoom.color = new Color(1f, 0.54f, 0.4f, 0f);
        }

        IEnumerator ExplotarJardin()
        {
            if (lamparaJardin != null)
                lamparaJardin.Sacudir();
            StartCoroutine(FogonazoJardin());
            StartCoroutine(SacudirMundo(0.55f, 14f));
            StartCoroutine(AvisoBoom());

            Transform padre = mundoJardin != null ? mundoJardin : lienzo;
            Sprite disco = FrascoQuebradizo.Disco();
            Vector2 boca = bocaCaldero;

            for (int i = 0; i < 22; i++)
            {
                Color color = i % 2 == 0 ? new Color(0.45f, 0.95f, 0.35f, 0.95f) : new Color(1f, 0.6f, 0.15f, 0.95f);
                Particula.Crear(padre, disco, boca + new Vector2(Random.Range(-60f, 60f), 0f), Vector2.one * Random.Range(18f, 42f), color,
                    new Vector2(Random.Range(-380f, 380f), Random.Range(380f, 900f)), 1500f, Random.Range(0.9f, 1.3f), 0f, 0.6f);
            }

            Chispas(padre, boca, 16, new Color(1f, 0.82f, 0.3f), 320f, 720f);
            for (int i = 0; i < 8; i++)
            {
                Particula.Crear(padre, disco, boca + new Vector2(Random.Range(-70f, 70f), Random.Range(0f, 30f)), Vector2.one * Random.Range(60f, 110f),
                    new Color(0.4f, 0.37f, 0.45f, 0.7f), new Vector2(Random.Range(-60f, 60f), Random.Range(90f, 170f)), -30f, Random.Range(1.3f, 1.8f), 0f, 2.3f);
            }

            Anillo(padre, boca, new Color(1f, 0.6f, 0.2f), 0.5f, 5f);
            StartCoroutine(PulsoCaldero(1.25f, 0.3f));

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.55f;
                float resto = 1f - Mathf.Clamp01(t);
                if (destelloCaldero != null)
                    destelloCaldero.color = new Color(1f, 0.95f, 0.8f, 0.95f * resto * resto);
                if (cauldronRect != null)
                    cauldronRect.anchoredPosition = cauldronBase + Random.insideUnitCircle * (18f * resto);
                if (cuerpoCaldero != null)
                {
                    float hollin = Mathf.Lerp(0.5f, 1f, Mathf.Clamp01(t));
                    cuerpoCaldero.color = new Color(hollin, hollin * 0.95f, hollin * 0.95f, 1f);
                }

                yield return null;
            }

            if (cauldronRect != null)
                cauldronRect.anchoredPosition = cauldronBase;
            if (destelloCaldero != null)
                destelloCaldero.color = new Color(1f, 1f, 1f, 0f);
            if (cuerpoCaldero != null)
                cuerpoCaldero.color = Color.white;
        }

        IEnumerator TriunfoJardin()
        {
            Transform padre = mundoJardin != null ? mundoJardin : lienzo;
            Vector2 boca = bocaCaldero;
            if (cauldronGlow != null)
                cauldronGlow.color = new Color(1f, 0.85f, 0.3f, 0.7f);
            Chispas(padre, boca, 30, new Color(1f, 0.92f, 0.5f), 200f, 520f);
            Anillo(padre, boca, new Color(1f, 0.9f, 0.4f), 0.9f, 4.5f);
            Anillo(padre, boca, new Color(1f, 1f, 0.8f), 1.3f, 6.5f);
            StartCoroutine(HumoJardin(padre, boca, new Color(1f, 0.85f, 0.3f), 16, 1.1f));
            PlayOne("hechizo");
            yield return PulsoCaldero(1.14f, 0.35f);
        }

        IEnumerator FogonazoJardin()
        {
            if (fogonazo == null)
                yield break;
            fogonazo.transform.SetAsLastSibling();
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.45f;
                float resto = 1f - Mathf.Clamp01(t);
                fogonazo.color = new Color(1f, 0.55f, 0.2f, 0.5f * resto * resto);
                yield return null;
            }

            fogonazo.color = new Color(1f, 0.55f, 0.2f, 0f);
        }

        IEnumerator SacudirMundo(float duracion, float fuerza)
        {
            if (mundoJardin == null)
                yield break;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / duracion;
                float resto = 1f - Mathf.Clamp01(t);
                mundoJardin.anchoredPosition = Random.insideUnitCircle * (fuerza * resto);
                yield return null;
            }

            mundoJardin.anchoredPosition = Vector2.zero;
        }

        IEnumerator AvisoBoom()
        {
            if (textoBoom == null)
                yield break;
            string[] frases =
            {
                "¡Ese no era! ¡BOOM!",
                "¡Uy! Ese ingrediente no iba ahí.",
                "¡El caldero no lo aceptó!"
            };
            textoBoom.text = frases[Random.Range(0, frases.Length)];
            textoBoom.transform.SetAsLastSibling();
            Color color = Nivel3UI.Hex("#ff8a66");
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.18f;
                float k = Mathf.Clamp01(t);
                textoBoom.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, k);
                textoBoom.color = new Color(color.r, color.g, color.b, k);
                yield return null;
            }

            yield return new WaitForSeconds(1.5f);
            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.35f;
                textoBoom.color = new Color(color.r, color.g, color.b, 1f - Mathf.Clamp01(t));
                yield return null;
            }

            textoBoom.color = new Color(color.r, color.g, color.b, 0f);
        }

        void Chispas(Transform padre, Vector2 boca, int cantidad, Color color, float velocidadMin, float velocidadMax)
        {
            Sprite estrella = FrascoBrillante.Estrella();
            for (int i = 0; i < cantidad; i++)
            {
                float angulo = Random.Range(0.15f, Mathf.PI - 0.15f);
                float rapidez = Random.Range(velocidadMin, velocidadMax);
                Color c = Color.Lerp(color, Color.white, Random.Range(0f, 0.4f));
                Particula.Crear(padre, estrella, boca + new Vector2(Random.Range(-50f, 50f), 0f), Vector2.one * Random.Range(22f, 44f), c,
                    new Vector2(Mathf.Cos(angulo) * rapidez, Mathf.Sin(angulo) * rapidez), 380f, Random.Range(0.7f, 1.1f), Random.Range(-300f, 300f), 0.1f);
            }
        }

        void Anillo(Transform padre, Vector2 boca, Color color, float duracion, float crecimiento)
        {
            color.a = 0.55f;
            Particula.Crear(padre, Nivel3UI.LuzSuave(), boca, Vector2.one * 90f, color, Vector2.zero, 0f, duracion, 0f, crecimiento);
        }

        IEnumerator HumoJardin(Transform padre, Vector2 boca, Color color, int bocanadas, float segundos)
        {
            Color humo = Color.Lerp(color, Color.white, 0.15f);
            humo.a = 0.6f;
            float espera = segundos / Mathf.Max(1, bocanadas);
            for (int i = 0; i < bocanadas; i++)
            {
                Particula.Crear(padre, Nivel3UI.LuzSuave(), boca + new Vector2(Random.Range(-40f, 40f), Random.Range(-6f, 10f)),
                    Vector2.one * Random.Range(90f, 150f), humo, new Vector2(Random.Range(-28f, 28f), Random.Range(90f, 170f)),
                    -18f, Random.Range(1.5f, 2.2f), Random.Range(-20f, 20f), 2.4f);
                yield return new WaitForSeconds(espera);
            }
        }

        IEnumerator PulsoCaldero(float escala, float segundos)
        {
            if (cauldronRect == null)
                yield break;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / segundos;
                float s = 1f + (escala - 1f) * Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
                cauldronRect.localScale = new Vector3(s, s, 1f);
                yield return null;
            }

            cauldronRect.localScale = Vector3.one;
        }

        IEnumerator EncenderEstrellas(int estrellas)
        {
            PlayOne("hechizo");
            for (int i = 0; i < estrellas; i++)
            {
                yield return new WaitForSeconds(0.5f);
                if (resultRoot == null || !resultRoot.activeSelf || resultEstrellas == null)
                    yield break;
                resultEstrellas[i].color = Color.white;
                StartCoroutine(Aparecer(resultEstrellas[i].rectTransform));
                PlayOne("acierto");
                for (int c = 0; c < 10; c++)
                {
                    Particula.Crear(resultEstrellas[i].transform.parent, FrascoBrillante.Estrella(), resultEstrellas[i].rectTransform.anchoredPosition,
                        Vector2.one * Random.Range(24f, 46f), new Color(1f, 0.92f, 0.5f), new Vector2(Random.Range(-260f, 260f), Random.Range(80f, 360f)),
                        420f, Random.Range(0.6f, 1f), Random.Range(-300f, 300f), 0.1f);
                }
            }

            while (resultRoot != null && resultRoot.activeSelf && estrellas > 0)
            {
                Particula.Crear(resultRoot.transform, FrascoBrillante.Estrella(), new Vector2(Random.Range(-950f, 950f), 600f),
                    Vector2.one * Random.Range(22f, 44f), Color.Lerp(new Color(1f, 0.9f, 0.5f), new Color(0.7f, 0.9f, 1f), Random.value),
                    new Vector2(Random.Range(-30f, 30f), Random.Range(-160f, -300f)), 0f, Random.Range(3f, 4.5f), Random.Range(-90f, 90f), 0.5f)
                    .transform.SetSiblingIndex(1);
                yield return new WaitForSeconds(0.14f);
            }
        }

        IEnumerator Aparecer(RectTransform rect)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.22f;
                if (rect == null)
                    yield break;
                rect.localScale = Vector3.one * Mathf.Lerp(0.4f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)));
                yield return null;
            }

            if (rect != null)
                rect.localScale = Vector3.one;
        }

        Text LineaResultado(Transform padre, float y)
        {
            var punto = CreateImage(padre, "Marca", disc, new Vector2(-150f, y), new Vector2(18f, 18f), true);
            punto.color = Hex("#3ea86a");
            var linea = MakeText(padre, "", 26, Hex("#5a3b24"), FontStyle.Bold);
            linea.alignment = TextAnchor.MiddleLeft;
            linea.rectTransform.anchoredPosition = new Vector2(40f, y);
            linea.rectTransform.sizeDelta = new Vector2(340f, 42f);
            return linea;
        }

        Image CrearBotonResultado(Transform padre, string etiqueta, Sprite sprite, Vector2 posicion, Vector2 tamano, Color colorTexto, UnityEngine.Events.UnityAction accion)
        {
            var imagen = CreateImage(padre, etiqueta, sprite, posicion, tamano, false);
            imagen.type = Image.Type.Sliced;
            imagen.raycastTarget = true;
            var boton = imagen.gameObject.AddComponent<Button>();
            boton.targetGraphic = imagen;
            boton.onClick.AddListener(accion);
            var texto = MakeText(imagen.transform, etiqueta, 24, colorTexto, FontStyle.Bold);
            Stretch(texto.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return imagen;
        }

        void AnimarFestejo()
        {
            if (vistaJardin || resultRoot == null || !resultRoot.activeSelf)
                return;

            if (magnusResultado != null)
            {
                float salto = Mathf.Abs(Mathf.Sin(Time.time * 2.6f));
                magnusResultado.anchoredPosition = new Vector2(90f, 55f + salto * 22f);
                float alto = 1f + salto * 0.05f;
                magnusResultado.localScale = new Vector3(1.04f - salto * 0.04f, alto, 1f);
                magnusResultado.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 2.6f) * 4f);
            }

            if (confeti == null)
                return;
            for (int i = 0; i < confeti.Length; i++)
            {
                if (confeti[i] == null)
                    continue;
                Vector2 posicion = confeti[i].anchoredPosition;
                posicion.y += (28f + (i % 3) * 10f) * Time.deltaTime;
                posicion.x += Mathf.Sin(Time.time * 1.6f + i) * 12f * Time.deltaTime;
                if (posicion.y > 240f)
                    posicion.y = -80f;
                confeti[i].anchoredPosition = posicion;
                confeti[i].localRotation = Quaternion.Euler(0f, 0f, Time.time * (40f + i * 12f));
            }
        }

        void BuildAudio()
        {
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            PlayLoop("ambiente", AjustesJuego.MusicaActiva ? 0.28f : 0f);
            PlayLoop("burbujas", AjustesJuego.EfectosActivos ? 0.16f : 0f);
        }

        Image CreateSocket(Transform parent, Vector2 position, float size)
        {
            var back = CreateImage(parent, "Hueco", disc, position, new Vector2(size, size), true);
            back.color = new Color(0.55f, 0.4f, 0.28f, 0.28f);
            var gem = CreateImage(back.transform, "Gema", solid, Vector2.zero, new Vector2(size * 0.78f, size * 0.78f), true);
            gem.enabled = false;
            return gem;
        }

        void SpawnBubble(Transform parent, string sprite, Vector2 position, Vector2 size, float speed, float amplitude)
        {
            var bubble = CreateImage(parent, "Burbuja", Spr(sprite), position, size, true);
            bubble.color = new Color(1f, 1f, 1f, 0.85f);
            var drift = bubble.gameObject.AddComponent<BubbleDrift>();
            drift.speed = speed;
            drift.amplitude = amplitude;
            drift.limit = 72f;
        }

        void PonerAnimo(Animo siguiente, float duracion)
        {
            animo = siguiente;
            animoReloj = 0f;
            animoDuracion = duracion;
        }

        void AnimarMagnus()
        {
            if (magnusRect == null)
                return;

            animoReloj += Time.deltaTime;
            float avance = animoDuracion <= 0.01f ? 1f : Mathf.Clamp01(animoReloj / animoDuracion);
            float respirar = Mathf.Sin(Time.time * 2.3f);
            Vector2 posicion = magnusBase + new Vector2(0f, respirar * 4f);
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
                    PonerAnimo(inputOpen ? Animo.Quieto : Animo.Observa, 0f);
            }
            else if (animo == Animo.Festeja)
            {
                float salto = Mathf.Sin(avance * Mathf.PI);
                posicion.y += salto * 42f;
                ancho *= 1f - salto * 0.05f;
                alto *= 1f + salto * 0.08f;
                giro = Mathf.Sin(avance * Mathf.PI * 2f) * 7f;
                if (avance >= 1f)
                    PonerAnimo(Animo.Quieto, 0f);
            }
            else if (animo == Animo.SeEquivoca)
            {
                float calma = 1f - avance;
                giro = Mathf.Sin(avance * Mathf.PI * 6f) * 12f * calma;
                posicion.y -= avance * 6f;
                if (avance >= 1f)
                    PonerAnimo(Animo.Quieto, 0f);
            }

            magnusRect.anchoredPosition = posicion;
            magnusRect.localRotation = Quaternion.Euler(0f, 0f, giro);
            magnusRect.localScale = new Vector3(ancho, alto, 1f);

            if (sombraRect != null)
            {
                float elevacion = Mathf.Max(0f, posicion.y - magnusBase.y);
                float escala = Mathf.Clamp(1f - elevacion / 70f, 0.62f, 1f);
                sombraRect.anchoredPosition = new Vector2(posicion.x, sombraY);
                sombraRect.localScale = new Vector3(escala, escala, 1f);
            }
        }

        Texture2D Archivo(string relativo)
        {
            string ruta = Path.Combine(Application.streamingAssetsPath, relativo);
            if (!File.Exists(ruta))
                return Tex("pared");

            var textura = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            textura.LoadImage(File.ReadAllBytes(ruta));
            textura.filterMode = FilterMode.Bilinear;
            return textura;
        }

        Sprite SpriteArchivo(string relativo)
        {
            Texture2D textura = Archivo(relativo);
            if (textura == null)
                return solid;
            return Sprite.Create(textura, new Rect(0f, 0f, textura.width, textura.height), new Vector2(0.5f, 0.5f), 100f);
        }

        IEnumerator Pop(RectTransform target)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 6f;
                float s = Mathf.Lerp(0.45f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)));
                if (target != null)
                    target.localScale = Vector3.one * s;
                yield return null;
            }
        }

        IEnumerator Pulse(RectTransform target)
        {
            Vector2 origin = target.anchoredPosition;
            float t = 0f;
            while (t < 0.24f)
            {
                t += Time.deltaTime;
                float s = 1f + Mathf.Sin(t / 0.24f * Mathf.PI) * 0.07f;
                target.localScale = Vector3.one * s;
                yield return null;
            }

            target.localScale = Vector3.one;
            target.anchoredPosition = origin;
        }

        IEnumerator Wobble(RectTransform target)
        {
            Vector2 origin = target.anchoredPosition;
            float t = 0f;
            while (t < 0.3f)
            {
                t += Time.deltaTime;
                float damp = 1f - t / 0.3f;
                float x = Mathf.Sin(t * 46f) * 8f * damp;
                target.anchoredPosition = origin + new Vector2(x, 0f);
                yield return null;
            }

            target.anchoredPosition = origin;
        }

        void FillCatalog()
        {
            catalog[IngredientKind.Fuego] = new Ing { Sprite = "gem_red_circle", Nombre = "Esencia", Glow = Hex("#e23b3b") };
            catalog[IngredientKind.Gota] = new Ing { Sprite = "gem_blue_triangle", Nombre = "Gota", Glow = Hex("#3a78d8") };
            catalog[IngredientKind.Polvo] = new Ing { Sprite = "gem_yellow_star", Nombre = "Polvo", Glow = Hex("#f0c43a") };
            catalog[IngredientKind.Hierba] = new Ing { Sprite = "gem_green_square", Nombre = "Hierba", Glow = Hex("#3ea86a") };
            catalog[IngredientKind.Cristal] = new Ing { Sprite = "gem_purple_pentagon", Nombre = "Cristal", Glow = Hex("#8a4ec4") };
            catalog[IngredientKind.Lagrima] = new Ing { Sprite = "gem_orange_crescent", Nombre = "Lágrima", Glow = Hex("#f08a2a") };
        }

        Canvas CreateCanvas()
        {
            var go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
                return;

            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            go.transform.SetParent(null);
        }

        Image CreateImage(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 size, bool aspect)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = aspect;
            image.raycastTarget = false;
            return image;
        }

        RawImage CreateRaw(Transform parent, string name, Texture2D texture, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<RawImage>();
            image.texture = texture;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        Text MakeText(Transform parent, string content, int size, Color color, FontStyle style)
        {
            var go = new GameObject("Texto", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = font;
            text.text = content;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.rectTransform.sizeDelta = new Vector2(300f, 40f);
            return text;
        }

        static void AddOutline(Text text, Color color)
        {
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(1.2f, -1.2f);
        }

        static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        void PlayLoop(string clipName, float volume)
        {
            var clip = Resources.Load<AudioClip>("FluxLab/Audio/" + clipName);
            if (clip == null)
                return;

            var source = gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.volume = volume;
            source.playOnAwake = false;
            source.Play();
        }

        void PlayOne(string clipName)
        {
            if (!AjustesJuego.EfectosActivos)
                return;
            var clip = Resources.Load<AudioClip>("FluxLab/Audio/" + clipName);
            if (clip == null || sfx == null)
                return;
            sfx.PlayOneShot(clip, 0.85f);
        }

        Sprite Spr(string name)
        {
            var sprite = Resources.Load<Sprite>("FluxLab/" + name);
            if (sprite != null)
                return sprite;
            Debug.LogWarning("No se encontró el sprite FluxLab/" + name);
            return solid;
        }

        static Texture2D Tex(string name)
        {
            return Resources.Load<Texture2D>("FluxLab/" + name);
        }

        static Color Hex(string value)
        {
            ColorUtility.TryParseHtmlString(value, out Color color);
            return color;
        }

        static Sprite MakeSolid()
        {
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;
            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
        }

        static Sprite MakeDisc(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float radius = (size - 1) * 0.5f;
            var center = new Vector2(radius, radius);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
                    float alpha = Mathf.Clamp01(1f - distance);
                    alpha *= alpha;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

    }
}
