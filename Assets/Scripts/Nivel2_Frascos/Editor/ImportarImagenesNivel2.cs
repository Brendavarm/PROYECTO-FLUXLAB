using UnityEditor;
using UnityEngine;

namespace FluxLab.Nivel2.EditorTools
{
    /// <summary>
    /// Importa las imágenes de Resources/FluxLab/NIVEL2-IMAGENES sin deformarlas
    /// (el proyecto está en modo 3D y Unity las estiraría a 256x512) y sin compresión,
    /// para que los frascos se vean nítidos.
    /// </summary>
    class ImportarImagenesNivel2 : AssetPostprocessor
    {
        const string Carpeta = "/NIVEL2-IMAGENES/";

        public override uint GetVersion() => 1;

        void OnPreprocessTexture()
        {
            if (!assetPath.Contains(Carpeta))
                return;

            var importador = (TextureImporter)assetImporter;
            importador.textureType = TextureImporterType.Default;
            importador.npotScale = TextureImporterNPOTScale.None;
            importador.mipmapEnabled = false;
            importador.isReadable = true;
            importador.textureCompression = TextureImporterCompression.Uncompressed;
            importador.wrapMode = TextureWrapMode.Clamp;
            importador.filterMode = FilterMode.Bilinear;
        }
    }
}
