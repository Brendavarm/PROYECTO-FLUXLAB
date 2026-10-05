using UnityEditor;

namespace FluxLab.EditorTools
{
    [InitializeOnLoad]
    static class AsegurarEscenas
    {
        static AsegurarEscenas()
        {
            Asegurar("Assets/Scenes/MapaNiveles.unity", "Assets/Scenes/MenuPrincipal.unity");
            Asegurar("Assets/Scenes/PersonalizarMagnus.unity", "Assets/Scenes/MapaNiveles.unity");
        }

        static void Asegurar(string ruta, string despuesDe)
        {
            var escenas = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for (int i = 0; i < escenas.Count; i++)
            {
                if (escenas[i].path == ruta)
                    return;
            }

            int indice = escenas.Count;
            for (int i = 0; i < escenas.Count; i++)
            {
                if (escenas[i].path == despuesDe)
                {
                    indice = i + 1;
                    break;
                }
            }

            escenas.Insert(indice, new EditorBuildSettingsScene(ruta, true));
            EditorBuildSettings.scenes = escenas.ToArray();
        }
    }
}
