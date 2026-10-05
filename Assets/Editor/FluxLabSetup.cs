using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FluxLab.EditorTools
{
    public static class FluxLabSetup
    {
        public static void Build()
        {
            try
            {
                ConfigureImporters();
                CreateScene();
                AssetDatabase.SaveAssets();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError(exception);
                EditorApplication.Exit(1);
            }
        }

        static void ConfigureImporters()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/FluxLab" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                    continue;

                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                bool repeats = name == "pared" || name == "suelo";

                importer.textureType = repeats ? TextureImporterType.Default : TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = repeats ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.textureCompression = repeats
                    ? TextureImporterCompression.Uncompressed
                    : TextureImporterCompression.Compressed;

                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteAlignment = (int)SpriteAlignment.Center;
                importer.SetTextureSettings(settings);

                if (name.StartsWith("panel"))
                    importer.spriteBorder = new Vector4(16f, 16f, 16f, 16f);
                else if (name.StartsWith("button"))
                    importer.spriteBorder = new Vector4(22f, 16f, 22f, 16f);
                else
                    importer.spriteBorder = Vector4.zero;

                importer.SaveAndReimport();
            }
        }

        static void CreateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.29f, 0.24f, 0.22f);
            camera.orthographic = true;
            camera.orthographicSize = 5.4f;
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<AudioListener>();

            var table = new GameObject("MesaDeAlquimia");
            table.AddComponent<MesaDeAlquimia>();

            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/MesaDeAlquimia.unity");
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/MesaDeAlquimia.unity", true)
            };

            PlayerSettings.companyName = "FLUXLAB";
            PlayerSettings.productName = "Laboratorio de Alquimia";
        }
    }
}
