using UnityEditor;

namespace MagicBlock.EditorTools
{
    /// <summary>Mọi ảnh trong Assets/MagicBlock/Art tự import thành Sprite (UI).</summary>
    public sealed class MagicBlockSpriteImporter : AssetPostprocessor
    {
        public const string ArtFolder = "Assets/MagicBlock/Art";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtFolder)) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
        }
    }
}
