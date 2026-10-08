using UnityEditor;
using UnityEngine;

namespace PrincessCollection.EditorTools
{
    /// <summary>
    /// Assets/PrincessCollection/Sprites 以下の画像を自動でスプライト設定にする。
    /// PPU=50 にしているので、50px の石がちょうど 1 ワールド単位（1マス）になる。
    /// </summary>
    public class PrincessSpriteImporter : AssetPostprocessor
    {
        public const string SpriteRoot = "Assets/PrincessCollection/Sprites/";
        public const float PixelsPerUnit = 50f;

        void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').StartsWith(SpriteRoot)) return;
            Apply((TextureImporter)assetImporter);
        }

        public static bool Apply(TextureImporter ti)
        {
            bool changed = ti.textureType != TextureImporterType.Sprite
                           || ti.spriteImportMode != SpriteImportMode.Single
                           || !Mathf.Approximately(ti.spritePixelsPerUnit, PixelsPerUnit)
                           || ti.mipmapEnabled
                           || ti.textureCompression != TextureImporterCompression.Uncompressed;

            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = PixelsPerUnit;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.filterMode = FilterMode.Bilinear;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.wrapMode = TextureWrapMode.Clamp;
            return changed;
        }
    }
}
