using UnityEditor;

/// <summary>
/// Resources/Sprites 아래의 모든 텍스처를 Sprite 로 자동 임포트한다.
/// (코드에서 Resources.Load&lt;Sprite&gt; 로 읽기 위함)
/// PixelsPerUnit = 1 : 1px == 1 world unit — 레거시 좌표계(가로 2800px)를 그대로 사용한다.
/// </summary>
public class SpriteImportSettings : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.Replace('\\', '/').Contains("/Resources/Sprites/")) return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 1f;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.filterMode = UnityEngine.FilterMode.Point; // 도트(픽셀아트) 또렷하게
        importer.maxTextureSize = 2048;
        importer.isReadable = true; // 배경 하단 색 샘플링용

        // 타일드 드로우모드(지면) 지원을 위해 FullRect 메시 사용
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = UnityEngine.SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
    }
}
