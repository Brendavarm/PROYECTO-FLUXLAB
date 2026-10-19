using UnityEngine;

namespace FluxLab
{
    /// <summary>
    /// Arma el volumen del gatito: mallas propias, pose y encuadre de la cámara.
    /// </summary>
    [ExecuteAlways]
    [DefaultExecutionOrder(-200)]
    public class FormaMishi : MonoBehaviour
    {
        void Awake()
        {
            Armar();
        }

        void OnEnable()
        {
            Armar();
        }

        Material pelo;
        Material rosa;
        Material blanco;
        Material pupilaMat;
        Material narizMat;

        void Update()
        {
            if (!Application.isPlaying)
                return;

            float giro = Mathf.Sin(Time.time * 0.6f) * 6f;
            transform.localRotation = Quaternion.Euler(0f, giro, 0f);
        }

        void Armar()
        {
            AsegurarMateriales();

            var cuerpo = Unir(
                Unir(
                    Unir(Elipsoide(0.15f, 0.13f, 0.26f), ElipsoideEn(new Vector3(0f, -0.06f, 0.18f), 0.16f, 0.16f, 0.15f)),
                    ElipsoideEn(new Vector3(0f, -0.02f, -0.22f), 0.13f, 0.12f, 0.13f)),
                ElipsoideEn(new Vector3(0f, 0.12f, -0.34f), 0.09f, 0.09f, 0.09f));
            Poner(Buscar("Cuerpo"), cuerpo, new Vector3(0f, 0.30f, 0.02f), Quaternion.identity, Vector3.one, pelo);

            var cabeza = Unir(Elipsoide(0.13f, 0.12f, 0.12f), ElipsoideEn(new Vector3(0.02f, -0.04f, -0.10f), 0.055f, 0.04f, 0.045f));
            Poner(Buscar("Cabeza"), cabeza, new Vector3(0f, 0.54f, -0.40f), Quaternion.identity, Vector3.one, pelo);

            Poner(Buscar("OrejaIzquierda"), OrejaMesh(-1f), new Vector3(-0.05f, 0.10f, 0f), Quaternion.identity, Vector3.one, pelo);
            Poner(Buscar("OrejaDerecha"), OrejaMesh(1f), new Vector3(0.05f, 0.10f, 0f), Quaternion.identity, Vector3.one, pelo);
            Poner(Buscar("InteriorIzquierdo"), InteriorOreja(), Vector3.zero, Quaternion.identity, Vector3.one, rosa);
            Poner(Buscar("InteriorDerecho"), InteriorOreja(), Vector3.zero, Quaternion.identity, Vector3.one, rosa);

            Poner(Buscar("OjoIzquierdo"), Elipsoide(0.028f, 0.034f, 0.016f), new Vector3(-0.04f, 0.02f, -0.08f), Quaternion.identity, Vector3.one, blanco);
            Poner(Buscar("OjoDerecho"), Elipsoide(0.035f, 0.042f, 0.02f), new Vector3(0.07f, 0.02f, -0.09f), Quaternion.identity, Vector3.one, blanco);
            Poner(Buscar("PupilaIzquierda"), Elipsoide(0.014f, 0.018f, 0.008f), new Vector3(-0.008f, 0f, -0.012f), Quaternion.identity, Vector3.one, pupilaMat);
            Poner(Buscar("PupilaDerecha"), Elipsoide(0.018f, 0.024f, 0.01f), new Vector3(0.01f, 0f, -0.016f), Quaternion.identity, Vector3.one, pupilaMat);
            Poner(Buscar("ParpadoIzquierdo"), Elipsoide(0.03f, 0.012f, 0.01f), new Vector3(0f, 0.02f, -0.006f), Quaternion.identity, Vector3.one, pelo);
            Poner(Buscar("ParpadoDerecho"), Elipsoide(0.036f, 0.014f, 0.012f), new Vector3(0f, 0.025f, -0.008f), Quaternion.identity, Vector3.one, pelo);
            Brillo(Buscar("PupilaDerecha"), 1f);

            Poner(Buscar("Nariz"), Elipsoide(0.016f, 0.012f, 0.01f), new Vector3(0.03f, -0.04f, -0.145f), Quaternion.identity, Vector3.one, narizMat);
            Poner(Buscar("BocaFeliz"), Sonrisa(), new Vector3(0.02f, -0.07f, -0.14f), Quaternion.identity, Vector3.one, narizMat);
            Poner(Buscar("BocaHabla"), Elipsoide(0.018f, 0.014f, 0.01f), new Vector3(0.02f, -0.065f, -0.15f), Quaternion.identity, Vector3.one, narizMat);
            Poner(Buscar("BocaSorpresa"), Elipsoide(0.012f, 0.014f, 0.01f), new Vector3(0.02f, -0.06f, -0.15f), Quaternion.identity, Vector3.one, narizMat);

            Poner(Buscar("PataDelanteraIzq"), Unir(Elipsoide(0.04f, 0.10f, 0.04f), ElipsoideEn(new Vector3(0.01f, -0.10f, -0.04f), 0.045f, 0.028f, 0.06f)), new Vector3(-0.06f, 0.15f, -0.18f), Quaternion.identity, Vector3.one, pelo);
            Poner(Buscar("PataDelanteraDer"), Unir(Elipsoide(0.045f, 0.11f, 0.045f), ElipsoideEn(new Vector3(0f, -0.11f, -0.04f), 0.05f, 0.03f, 0.07f)), new Vector3(0.07f, 0.16f, -0.24f), Quaternion.identity, Vector3.one, pelo);
            Poner(Buscar("PataTraseraIzq"), Elipsoide(0.05f, 0.035f, 0.08f), new Vector3(-0.07f, 0.08f, 0.18f), Quaternion.identity, Vector3.one, pelo);
            Poner(Buscar("PataTraseraDer"), Elipsoide(0.06f, 0.04f, 0.09f), new Vector3(0.08f, 0.08f, 0.16f), Quaternion.identity, Vector3.one, pelo);

            Poner(Buscar("Cola"), ColaMesh(), new Vector3(0f, 0.30f, 0.28f), Quaternion.identity, Vector3.one, pelo);
            Ocultar(Buscar("Hocico"));
            Encuadrar();
        }

        void AsegurarMateriales()
        {
            if (pelo != null)
                return;

            pelo = Crear(new Color(0.49f, 0.43f, 0.60f), 0.58f);
            rosa = Crear(new Color(0.91f, 0.61f, 0.69f), 0.75f);
            blanco = Crear(new Color(1f, 0.98f, 0.94f), 0.9f);
            pupilaMat = Crear(new Color(0.11f, 0.09f, 0.14f), 0.8f);
            narizMat = Crear(new Color(0.84f, 0.44f, 0.54f), 0.72f);
        }

        Material Crear(Color color, float sombra)
        {
            var sombreador = Shader.Find("FluxLab/Pelaje");
            var material = sombreador != null ? new Material(sombreador) : new Material(Shader.Find("Standard"));
            material.color = color;
            if (material.HasProperty("_Sombra"))
                material.SetFloat("_Sombra", sombra);
            return material;
        }

        void Brillo(Transform pupila, float lado)
        {
            if (pupila == null)
                return;

            Transform brillo = pupila.Find("Brillo");
            if (brillo == null)
            {
                var go = new GameObject("Brillo");
                go.layer = 6;
                go.transform.SetParent(pupila, false);
                go.AddComponent<MeshFilter>();
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                brillo = go.transform;
            }

            Poner(brillo, Elipsoide(0.014f, 0.014f, 0.008f), new Vector3(lado * -0.015f, 0.03f, -0.02f), Quaternion.identity, Vector3.one, blanco);
        }

        static void Ocultar(Transform pieza)
        {
            if (pieza != null)
                pieza.gameObject.SetActive(false);
        }

        void Encuadrar()
        {
            Transform estudio = transform.parent;
            if (estudio == null)
                return;

            Transform camara = estudio.Find("CamaraMishi");
            if (camara != null)
            {
                camara.localPosition = new Vector3(1.15f, 0.62f, -1.45f);
                camara.localRotation = Quaternion.LookRotation(new Vector3(0f, 0.36f, 0f) - camara.localPosition);
                var cam = camara.GetComponent<Camera>();
                if (cam != null)
                {
                    cam.fieldOfView = 30f;
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
                }
            }

            Transform luz = estudio.Find("LuzMishi");
            if (luz != null)
            {
                luz.rotation = Quaternion.LookRotation(new Vector3(0.15f, -0.55f, 0.75f));
                var foco = luz.GetComponent<Light>();
                if (foco != null)
                    foco.intensity = 1.7f;
            }
        }

        static void Poner(Transform pieza, Mesh malla, Vector3 posicion, Quaternion rotacion, Vector3 escala, Material material)
        {
            if (pieza == null)
                return;

            pieza.localPosition = posicion;
            pieza.localRotation = rotacion;
            pieza.localScale = escala;
            pieza.gameObject.layer = 6;

            var filtro = pieza.GetComponent<MeshFilter>();
            if (filtro != null && malla != null)
                filtro.sharedMesh = malla;

            var renderer = pieza.GetComponent<MeshRenderer>();
            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
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

        static Mesh Elipsoide(float rx, float ry, float rz)
        {
            const int lat = 12;
            const int lon = 16;
            var vertices = new Vector3[(lat + 1) * (lon + 1)];
            var indices = new int[lat * lon * 6];
            int v = 0;
            for (int y = 0; y <= lat; y++)
            {
                float anguloY = Mathf.PI * y / lat;
                float sy = Mathf.Cos(anguloY);
                float radio = Mathf.Sin(anguloY);
                for (int x = 0; x <= lon; x++)
                {
                    float anguloX = 2f * Mathf.PI * x / lon;
                    vertices[v++] = new Vector3(rx * radio * Mathf.Cos(anguloX), ry * sy, rz * radio * Mathf.Sin(anguloX));
                }
            }

            int t = 0;
            for (int y = 0; y < lat; y++)
            {
                for (int x = 0; x < lon; x++)
                {
                    int a = y * (lon + 1) + x;
                    int b = a + lon + 1;
                    indices[t++] = a;
                    indices[t++] = a + 1;
                    indices[t++] = b;
                    indices[t++] = a + 1;
                    indices[t++] = b + 1;
                    indices[t++] = b;
                }
            }

            return Cerrar(vertices, indices);
        }

        static Mesh ElipsoideEn(Vector3 centro, float rx, float ry, float rz)
        {
            var malla = Elipsoide(rx, ry, rz);
            var vertices = malla.vertices;
            for (int i = 0; i < vertices.Length; i++)
                vertices[i] += centro;
            malla.vertices = vertices;
            malla.RecalculateBounds();
            return malla;
        }

        static Mesh Unir(Mesh a, Mesh b)
        {
            var malla = new Mesh();
            var va = a.vertices;
            var vb = b.vertices;
            var vertices = new Vector3[va.Length + vb.Length];
            va.CopyTo(vertices, 0);
            vb.CopyTo(vertices, va.Length);
            var ta = a.triangles;
            var tb = b.triangles;
            var indices = new int[ta.Length + tb.Length];
            ta.CopyTo(indices, 0);
            for (int i = 0; i < tb.Length; i++)
                indices[ta.Length + i] = tb[i] + va.Length;
            malla.vertices = vertices;
            malla.triangles = indices;
            malla.RecalculateNormals();
            malla.RecalculateBounds();
            return malla;
        }

        static Mesh OrejaMesh(float signo)
        {
            return Piramide(
                new Vector3(-signo * 0.035f, 0f, 0.02f),
                new Vector3(signo * 0.03f, 0f, 0f),
                new Vector3(0f, 0f, -0.04f),
                new Vector3(signo * 0.01f, 0.16f, -0.01f));
        }

        static Mesh InteriorOreja()
        {
            Vector3 a = new Vector3(-0.015f, 0.03f, -0.03f);
            Vector3 b = new Vector3(0.012f, 0.03f, -0.025f);
            Vector3 c = new Vector3(0f, 0.13f, -0.02f);
            return Cerrar(new[] { a, b, c, a, c, b }, new[] { 0, 1, 2, 3, 4, 5 });
        }

        static Mesh Piramide(Vector3 a, Vector3 b, Vector3 c, Vector3 punta)
        {
            return Cerrar(
                new[] { a, b, c, punta },
                new[] { 0, 1, 3, 1, 2, 3, 2, 0, 3, 0, 2, 1 });
        }

        static Mesh ColaMesh()
        {
            return Tubo(new[]
            {
                Vector3.zero,
                new Vector3(0.06f, 0.12f, 0.12f),
                new Vector3(0.14f, 0.25f, 0.04f),
                new Vector3(0.16f, 0.36f, -0.10f),
                new Vector3(0.08f, 0.42f, -0.20f)
            }, 0.028f);
        }

        static Mesh Cono(float radio, float alto)
        {
            const int lados = 10;
            var vertices = new Vector3[lados + 2];
            vertices[0] = new Vector3(0f, alto, 0f);
            for (int i = 0; i < lados; i++)
            {
                float angulo = 2f * Mathf.PI * i / lados;
                vertices[i + 1] = new Vector3(Mathf.Cos(angulo) * radio, 0f, Mathf.Sin(angulo) * radio);
            }
            vertices[lados + 1] = Vector3.zero;

            var indices = new int[lados * 6];
            int t = 0;
            for (int i = 0; i < lados; i++)
            {
                int siguiente = i == lados - 1 ? 1 : i + 2;
                indices[t++] = 0;
                indices[t++] = siguiente;
                indices[t++] = i + 1;
                indices[t++] = lados + 1;
                indices[t++] = i + 1;
                indices[t++] = siguiente;
            }

            return Cerrar(vertices, indices);
        }

        static Mesh Tubo(Vector3[] curva, float radio)
        {
            const int lados = 8;
            var vertices = new Vector3[curva.Length * lados];
            var indices = new int[(curva.Length - 1) * lados * 6];

            for (int i = 0; i < curva.Length; i++)
            {
                Vector3 tangente = i == curva.Length - 1
                    ? (curva[i] - curva[i - 1]).normalized
                    : (curva[i + 1] - curva[i]).normalized;
                Vector3 lado = Vector3.Cross(tangente, Vector3.up);
                if (lado.sqrMagnitude < 0.0001f)
                    lado = Vector3.Cross(tangente, Vector3.right);
                lado.Normalize();
                Vector3 arriba = Vector3.Cross(lado, tangente).normalized;

                for (int a = 0; a < lados; a++)
                {
                    float angulo = 2f * Mathf.PI * a / lados;
                    vertices[i * lados + a] = curva[i] + (lado * Mathf.Cos(angulo) + arriba * Mathf.Sin(angulo)) * radio;
                }
            }

            int t = 0;
            for (int i = 0; i < curva.Length - 1; i++)
            {
                for (int a = 0; a < lados; a++)
                {
                    int siguiente = (a + 1) % lados;
                    int b0 = i * lados + a;
                    int b1 = i * lados + siguiente;
                    int c0 = (i + 1) * lados + a;
                    int c1 = (i + 1) * lados + siguiente;
                    indices[t++] = b0;
                    indices[t++] = c0;
                    indices[t++] = b1;
                    indices[t++] = b1;
                    indices[t++] = c0;
                    indices[t++] = c1;
                }
            }

            return Cerrar(vertices, indices);
        }

        static Mesh Sonrisa()
        {
            return Tubo(new[]
            {
                new Vector3(-0.045f, 0.012f, 0f),
                new Vector3(-0.02f, 0f, -0.01f),
                new Vector3(0.02f, 0f, -0.01f),
                new Vector3(0.045f, 0.012f, 0f)
            }, 0.008f);
        }

        static Mesh Cerrar(Vector3[] vertices, int[] indices)
        {
            var malla = new Mesh();
            malla.vertices = vertices;
            malla.triangles = indices;
            malla.RecalculateNormals();
            malla.RecalculateBounds();
            if (vertices.Length > 1 && Vector3.Dot(malla.normals[1], vertices[1].normalized) < 0f)
            {
                for (int i = 0; i < indices.Length; i += 3)
                {
                    int intercambio = indices[i + 1];
                    indices[i + 1] = indices[i + 2];
                    indices[i + 2] = intercambio;
                }
                malla.triangles = indices;
                malla.RecalculateNormals();
            }
            return malla;
        }
    }
}
