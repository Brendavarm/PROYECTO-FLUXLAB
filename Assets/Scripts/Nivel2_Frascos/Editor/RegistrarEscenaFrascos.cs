using System.Collections.Generic;
using UnityEditor;

namespace FluxLab.Nivel2.EditorTools
{
    /// <summary>
    /// Agrega la escena del nivel 2 a Build Settings al abrir Unity,
    /// para que SceneManager.LoadScene("Nivel2_Frascos") funcione.
    /// </summary>
    [InitializeOnLoad]
    static class RegistrarEscenaFrascos
    {
        const string Ruta = "Assets/Scenes/Nivel2_Frascos.unity";
        const string DespuesDe = "Assets/Scenes/MesaDeAlquimia.unity";

        static RegistrarEscenaFrascos()
        {
            var escenas = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            int indice = escenas.Count;
            for (int i = 0; i < escenas.Count; i++)
            {
                if (escenas[i].path == Ruta)
                    return;
                if (escenas[i].path == DespuesDe)
                    indice = i + 1;
            }

            escenas.Insert(indice, new EditorBuildSettingsScene(Ruta, true));
            EditorBuildSettings.scenes = escenas.ToArray();
        }
    }
}
