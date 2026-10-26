using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace FluxLab
{
    /// <summary>
    /// Fondo animado con cuadros sueltos (StreamingAssets/Menu/FondoAnimado/fondo_00.jpg, fondo_01.jpg...).
    /// Mientras cargan se ve la imagen estática del fondo; al terminar empieza a reproducirse en bucle.
    /// </summary>
    public class FondoAnimado : MonoBehaviour
    {
        public float segundosPorCuadro = 0.09f;

        RawImage imagen;
        string[] archivos;
        Texture2D[] cuadros;
        bool listo;
        float reloj;

        /// <summary>Convierte un fondo en animado. Devuelve false si no hay cuadros.</summary>
        public static bool Aplicar(RawImage imagen)
        {
            string carpeta = Path.Combine(Application.streamingAssetsPath, "Menu", "FondoAnimado");
            if (!Directory.Exists(carpeta))
                return false;

            string[] archivos = Directory.GetFiles(carpeta, "*.jpg");
            if (archivos.Length < 2)
                return false;

            Array.Sort(archivos);

            var animado = imagen.gameObject.AddComponent<FondoAnimado>();
            animado.imagen = imagen;
            animado.archivos = archivos;
            return true;
        }

        IEnumerator Start()
        {
            cuadros = new Texture2D[archivos.Length];
            for (int i = 0; i < archivos.Length; i++)
            {
                var textura = new Texture2D(2, 2, TextureFormat.RGB24, false);
                textura.LoadImage(File.ReadAllBytes(archivos[i]));
                textura.filterMode = FilterMode.Bilinear;
                cuadros[i] = textura;
                yield return null; // un cuadro por frame para no congelar el menú al abrir
            }

            listo = true;
        }

        void Update()
        {
            if (!listo)
                return;

            reloj += Time.deltaTime;
            int cuadro = (int)(reloj / segundosPorCuadro) % cuadros.Length;
            if (imagen.texture != cuadros[cuadro])
                imagen.texture = cuadros[cuadro];
        }

        void OnDestroy()
        {
            if (cuadros == null)
                return;
            foreach (var textura in cuadros)
                if (textura != null)
                    Destroy(textura);
        }
    }
}
