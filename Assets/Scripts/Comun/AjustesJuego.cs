using UnityEngine;

namespace FluxLab
{
    public static class AjustesJuego
    {
        const string MusicaKey = "flux.musica";
        const string EfectosKey = "flux.efectos";
        const string PuntosKey = "flux.puntos";
        const string PocionesKey = "flux.pociones";
        const string PeloKey = "flux.pelo";
        const string TunicaKey = "flux.tunica";
        const string ColorKey = "flux.color";
        const string AccesorioKey = "flux.accesorio";
        const string EfectoKey = "flux.efecto";
        const string NivelKey = "flux.nivel";
        const string NombreKey = "flux.nombre";
        const string GeneroKey = "flux.genero";

        public static bool MusicaActiva
        {
            get => PlayerPrefs.GetInt(MusicaKey, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(MusicaKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool EfectosActivos
        {
            get => PlayerPrefs.GetInt(EfectosKey, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(EfectosKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static int MejorPuntaje => PlayerPrefs.GetInt(PuntosKey, 0);

        public static int PocionesHechas => PlayerPrefs.GetInt(PocionesKey, 0);

        public static int Pelo
        {
            get => PlayerPrefs.GetInt(PeloKey, 0);
            set => PlayerPrefs.SetInt(PeloKey, value);
        }

        public static int Tunica
        {
            get => PlayerPrefs.GetInt(TunicaKey, 0);
            set => PlayerPrefs.SetInt(TunicaKey, value);
        }

        public static int ColorTunica
        {
            get => PlayerPrefs.GetInt(ColorKey, 0);
            set => PlayerPrefs.SetInt(ColorKey, value);
        }

        public static int Accesorio
        {
            get => PlayerPrefs.GetInt(AccesorioKey, 0);
            set => PlayerPrefs.SetInt(AccesorioKey, value);
        }

        public static int Efecto
        {
            get => PlayerPrefs.GetInt(EfectoKey, 0);
            set => PlayerPrefs.SetInt(EfectoKey, value);
        }

        public static int NivelAlcanzado => Mathf.Clamp(PlayerPrefs.GetInt(NivelKey, 1), 1, 5);

        public static int NivelSeleccionado = 1;

        public static string NombreAprendiz
        {
            get => PlayerPrefs.GetString(NombreKey, "");
            set
            {
                PlayerPrefs.SetString(NombreKey, value == null ? "" : value.Trim());
                PlayerPrefs.Save();
            }
        }

        public static bool EsAprendiza
        {
            get => PlayerPrefs.GetInt(GeneroKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(GeneroKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool TieneFicha => !string.IsNullOrWhiteSpace(NombreAprendiz);

        public static string NombreParaHablar => TieneFicha ? NombreAprendiz.Trim() : "aprendiz";

        public static string Rol => EsAprendiza ? "aprendiza" : "aprendiz";

        public static string Companero => EsAprendiza ? "compañera" : "compañero";

        public static string Articulo => EsAprendiza ? "la" : "el";

        public static void ElegirNivel(int nivel)
        {
            NivelSeleccionado = Mathf.Clamp(nivel, 1, NivelAlcanzado);
        }

        public static void AvanzarNivel()
        {
            if (NivelAlcanzado >= 5)
                return;
            PlayerPrefs.SetInt(NivelKey, NivelAlcanzado + 1);
            PlayerPrefs.Save();
        }

        public static void GuardarApariencia()
        {
            PlayerPrefs.Save();
        }

        public static void RegistrarPuntaje(int puntos, int aciertos)
        {
            if (puntos > MejorPuntaje)
                PlayerPrefs.SetInt(PuntosKey, puntos);
            PlayerPrefs.SetInt(PocionesKey, PocionesHechas + Mathf.Max(0, aciertos));
            PlayerPrefs.Save();
        }
    }
}
