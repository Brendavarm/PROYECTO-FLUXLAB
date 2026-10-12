using UnityEditor;
using UnityEngine;

namespace FluxLab.EditorTools
{
    /// <summary>
    /// Unity ejecuta esto solo cada vez que importa una imagen.
    /// Todo lo que esté en Resources/FluxLab/Mishi, Frascos, Grillo, Lampara, Mariposa o Nivel3 se configura como sprite nítido.
    /// </summary>
    public class MishiImportador : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Resources/FluxLab/Mishi/") && !assetPath.StartsWith("Assets/Resources/FluxLab/Frascos/")
                && !assetPath.StartsWith("Assets/Resources/FluxLab/Grillo/")
                && !assetPath.StartsWith("Assets/Resources/FluxLab/Nivel3/")
                && !assetPath.StartsWith("Assets/Resources/FluxLab/Lampara/")
                && !assetPath.StartsWith("Assets/Resources/FluxLab/Mariposa/"))
                return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}
