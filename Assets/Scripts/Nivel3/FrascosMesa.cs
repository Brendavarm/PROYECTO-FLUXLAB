using System.Collections.Generic;
using UnityEngine;

namespace FluxLab
{
    /// <summary>
    /// Los frascos que se pueden tirar de la mesa. Guarda a todos juntos y
    /// avisa a los que están cerca cuando Mishi cae.
    /// </summary>
    public class FrascosMesa : MonoBehaviour
    {
        // Qué tan lejos (en píxeles del lienzo) tiene que caer Mishi para tirar un frasco.
        public float alcance = 170f;

        public AudioClip sonidoGolpe;
        public AudioClip sonidoRotura;

        FrascoQuebradizo[] frascos;
        AudioSource fuente;

        /// <summary>Un frasco de la hoja: cuál es, de qué color es su líquido y dónde se para.</summary>
        public struct Def
        {
            public int dibujo;          // 0 a 7: frasco_N en Resources/FluxLab/Frascos
            public Color color;
            public float x;
            public float y;             // altura de la superficie donde está parado
            public float alto;          // alto del frasco en el lienzo
            public float espacioAbajo;  // fracción vacía bajo el dibujo (medida al cortar la hoja)

            public Def(int dibujo, Color color, float x, float y, float alto, float espacioAbajo)
            {
                this.dibujo = dibujo;
                this.color = color;
                this.x = x;
                this.y = y;
                this.alto = alto;
                this.espacioAbajo = espacioAbajo;
            }
        }

        // Alturas del escenario (las da cada nivel, según su fondo).
        public float alturaMesa = -200f;     // la superficie de la mesa
        public float alturaRotura = -340f;   // dónde se rompe un frasco que cae de la mesa
        public float mesaIzquierda = -650f;
        public float mesaDerecha = 650f;

        /// <param name="alturaMesa">Altura de la mesa: lo que cae de una repisa se rompe al llegar ahí.</param>
        /// <param name="alturaRotura">Dónde se rompe lo que cae de la mesa hacia el frente.</param>
        public static FrascosMesa Crear(Transform lienzo, Def[] frascos, float alturaMesa, float alturaRotura, float mesaIzquierda, float mesaDerecha)
        {
            var go = new GameObject("FrascosMesa", typeof(RectTransform));
            go.transform.SetParent(lienzo, false);
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero; // ocupa todo el lienzo, así las coordenadas son las mismas

            var mesa = go.AddComponent<FrascosMesa>();
            mesa.alturaMesa = alturaMesa;
            mesa.alturaRotura = alturaRotura;
            mesa.mesaIzquierda = mesaIzquierda;
            mesa.mesaDerecha = mesaDerecha;
            mesa.fuente = go.AddComponent<AudioSource>();
            mesa.fuente.playOnAwake = false;
            mesa.sonidoGolpe = Resources.Load<AudioClip>("FluxLab/Audio/frasco_golpe");
            mesa.sonidoRotura = Resources.Load<AudioClip>("FluxLab/Audio/frasco_roto");

            // Si a un frasco le falta el dibujo se salta, y el resto sigue.
            var lista = new List<FrascoQuebradizo>();
            foreach (var def in frascos)
            {
                Sprite dibujo = Fr(def.dibujo);
                if (dibujo == null)
                {
                    Debug.LogWarning("No se encontró el frasco " + def.dibujo + " en Resources/FluxLab/Frascos");
                    continue;
                }

                lista.Add(FrascoQuebradizo.Crear(mesa, dibujo, def.color, def.x, def.y, def.alto, def.espacioAbajo));
            }

            mesa.frascos = lista.ToArray();
            return mesa;
        }

        /// <summary>Uno de los 8 frascos de la hoja (Resources/FluxLab/Frascos/frasco_N).</summary>
        static Sprite Fr(int numero)
        {
            string ruta = "FluxLab/Frascos/frasco_" + numero;
            Sprite sprite = Resources.Load<Sprite>(ruta);
            if (sprite != null)
                return sprite;

            // Si Unity importó el PNG como imagen normal (y no como sprite), se arma el sprite aquí.
            var textura = Resources.Load<Texture2D>(ruta);
            if (textura == null)
                return null;
            return Sprite.Create(textura, new Rect(0f, 0f, textura.width, textura.height), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>Mishi aterrizó en esta posición y altura. Devuelve true si tiró algún frasco.</summary>
        public bool Aterrizo(float x, float y)
        {
            bool tiro = false;
            foreach (var frasco in frascos)
            {
                // Solo los que están en la misma superficie que ella (mesa con mesa, repisa con repisa).
                bool mismaSuperficie = Mathf.Abs(frasco.YBase - y) < 20f;
                if (frasco.Disponible && mismaSuperficie && Mathf.Abs(frasco.X - x) < alcance)
                {
                    frasco.Golpear(x);
                    tiro = true;
                }
            }

            return tiro;
        }

        public void Sonar(AudioClip clip, float volumen)
        {
            if (clip != null && AjustesJuego.EfectosActivos)
                fuente.PlayOneShot(clip, volumen);
        }
    }
}
