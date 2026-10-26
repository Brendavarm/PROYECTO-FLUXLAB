using UnityEngine;

namespace FluxLab
{
    /// <summary>
    /// Gatito negro del tutorial. Cambia ojos, boca y orejas según la expresión.
    /// </summary>
    public class ExpresionesMishi : MonoBehaviour
    {
        public enum Expresion
        {
            Quieto,
            Curioso,
            Habla,
            Feliz,
            Sorpresa,
            Guino
        }

        public Expresion expresion = Expresion.Quieto;

        Transform cabeza;
        Transform ojoIzquierdo;
        Transform ojoDerecho;
        Transform parpadoIzquierdo;
        Transform parpadoDerecho;
        Transform orejaIzquierda;
        Transform orejaDerecha;
        Transform bocaFeliz;
        Transform bocaHabla;
        Transform bocaSorpresa;
        Transform cola;
        Transform cuerpo;

        Vector3 ojoBase;
        Vector3 parpadoBase;
        Vector3 bocaHablaBase;
        Quaternion cabezaBase;
        Quaternion orejaIzquierdaBase;
        Quaternion orejaDerechaBase;
        Quaternion colaBase;
        Vector3 cuerpoBase;
        float parpadeo = 2.2f;

        void Start()
        {
            cabeza = Buscar("Cabeza");
            ojoIzquierdo = Buscar("OjoIzquierdo");
            ojoDerecho = Buscar("OjoDerecho");
            parpadoIzquierdo = Buscar("ParpadoIzquierdo");
            parpadoDerecho = Buscar("ParpadoDerecho");
            orejaIzquierda = Buscar("OrejaIzquierda");
            orejaDerecha = Buscar("OrejaDerecha");
            bocaFeliz = Buscar("BocaFeliz");
            bocaHabla = Buscar("BocaHabla");
            bocaSorpresa = Buscar("BocaSorpresa");
            cola = Buscar("Cola");
            cuerpo = Buscar("Cuerpo");

            if (ojoIzquierdo != null)
                ojoBase = ojoIzquierdo.localScale;
            if (parpadoIzquierdo != null)
                parpadoBase = parpadoIzquierdo.localScale;
            if (bocaHabla != null)
                bocaHablaBase = bocaHabla.localScale;
            if (cabeza != null)
                cabezaBase = cabeza.localRotation;
            if (orejaIzquierda != null)
                orejaIzquierdaBase = orejaIzquierda.localRotation;
            if (orejaDerecha != null)
                orejaDerechaBase = orejaDerecha.localRotation;
            if (cola != null)
                colaBase = cola.localRotation;
            if (cuerpo != null)
                cuerpoBase = cuerpo.localScale;
        }

        public void Poner(Expresion siguiente)
        {
            expresion = siguiente;
        }

        void Update()
        {
            Respirar();
            MoverCola();
            AplicarCara();
        }

        void Respirar()
        {
            if (cuerpo == null)
                return;
            float pulso = 1f + Mathf.Sin(Time.time * 1.6f) * 0.03f;
            cuerpo.localScale = new Vector3(cuerpoBase.x * pulso, cuerpoBase.y, cuerpoBase.z);
        }

        void MoverCola()
        {
            if (cola == null)
                return;
            float giro = Mathf.Sin(Time.time * 2.2f) * 16f;
            cola.localRotation = colaBase * Quaternion.Euler(0f, giro, 0f);
        }

        void AplicarCara()
        {
            parpadeo -= Time.deltaTime;
            bool pestaneo = parpadeo < 0.14f && parpadeo > 0f;
            if (parpadeo <= 0f)
                parpadeo = Random.Range(2.2f, 4.4f);

            float altoOjo = 1f;
            float altoParpado = 0.22f;
            float inclinacion = 0f;
            bool cerrarIzquierdo = false;

            switch (expresion)
            {
                case Expresion.Curioso:
                    altoOjo = 1.12f;
                    inclinacion = 14f;
                    break;
                case Expresion.Habla:
                    altoOjo = 1.05f;
                    inclinacion = Mathf.Sin(Time.time * 6f) * 4f;
                    break;
                case Expresion.Feliz:
                    altoOjo = 0.45f;
                    altoParpado = 0.7f;
                    break;
                case Expresion.Sorpresa:
                    altoOjo = 1.35f;
                    altoParpado = 0.08f;
                    break;
                case Expresion.Guino:
                    cerrarIzquierdo = true;
                    break;
            }

            if (pestaneo && expresion != Expresion.Feliz && expresion != Expresion.Guino)
            {
                altoOjo = 0.12f;
                altoParpado = 1f;
            }

            PonerOjo(ojoIzquierdo, cerrarIzquierdo ? 0.12f : altoOjo);
            PonerOjo(ojoDerecho, altoOjo);
            PonerParpado(parpadoIzquierdo, cerrarIzquierdo ? 1f : altoParpado);
            PonerParpado(parpadoDerecho, altoParpado);

            if (cabeza != null)
                cabeza.localRotation = cabezaBase * Quaternion.Euler(0f, 0f, inclinacion);
            if (orejaIzquierda != null)
                orejaIzquierda.localRotation = orejaIzquierdaBase * Quaternion.Euler(expresion == Expresion.Sorpresa ? 18f : 0f, 0f, 0f);
            if (orejaDerecha != null)
                orejaDerecha.localRotation = orejaDerechaBase * Quaternion.Euler(expresion == Expresion.Sorpresa ? 18f : 0f, 0f, 0f);

            Activar(bocaFeliz, expresion != Expresion.Habla && expresion != Expresion.Sorpresa);
            Activar(bocaHabla, expresion == Expresion.Habla);
            Activar(bocaSorpresa, expresion == Expresion.Sorpresa);

            if (expresion == Expresion.Habla && bocaHabla != null)
            {
                float abre = 0.65f + Mathf.Abs(Mathf.Sin(Time.time * 9f)) * 0.55f;
                bocaHabla.localScale = new Vector3(bocaHablaBase.x, bocaHablaBase.y * abre, bocaHablaBase.z);
            }
        }

        void PonerOjo(Transform ojo, float alto)
        {
            if (ojo == null)
                return;
            ojo.localScale = new Vector3(ojoBase.x, ojoBase.y * alto, ojoBase.z);
        }

        void PonerParpado(Transform parpado, float alto)
        {
            if (parpado == null)
                return;
            parpado.localScale = new Vector3(parpadoBase.x, parpadoBase.y * alto, parpadoBase.z);
        }

        static void Activar(Transform pieza, bool activo)
        {
            if (pieza != null && pieza.gameObject.activeSelf != activo)
                pieza.gameObject.SetActive(activo);
        }

        Transform Buscar(string nombre)
        {
            Transform[] piezas = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < piezas.Length; i++)
            {
                if (piezas[i].name == nombre)
                    return piezas[i];
            }

            return null;
        }
    }
}
