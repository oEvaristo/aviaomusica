if (UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Stop Play mode before saving scenery changes.");
UnityEditor.AssetDatabase.Refresh();
var artPaths = new[] {
    "Assets/Art/Scenery/ceu-entardecer.png",
    "Assets/Art/Scenery/montanhas-costa.png",
    "Assets/Art/Scenery/floresta-proxima.png",
    "Assets/Art/Scenery/nuvem-volumetrica.png"
};
var sprites = new UnityEngine.Sprite[4];
for (int i = 0; i < artPaths.Length; i++)
{
    var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(artPaths[i]);
    if (importer == null) throw new System.Exception("Missing art: " + artPaths[i]);
    importer.textureType = UnityEditor.TextureImporterType.Sprite;
    importer.spriteImportMode = UnityEditor.SpriteImportMode.Single;
    importer.spritePixelsPerUnit = 100f;
    importer.alphaIsTransparency = true;
    importer.mipmapEnabled = false;
    importer.filterMode = UnityEngine.FilterMode.Bilinear;
    importer.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
    importer.maxTextureSize = 2048;
    importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
    var settings = new UnityEditor.TextureImporterSettings();
    importer.ReadTextureSettings(settings);
    settings.spriteMeshType = UnityEngine.SpriteMeshType.FullRect;
    importer.SetTextureSettings(settings);
    importer.SaveAndReimport();
    sprites[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(artPaths[i]);
    if (sprites[i] == null) throw new System.Exception("Sprite import failed: " + artPaths[i]);
}
var scenery = UnityEngine.Object.FindFirstObjectByType<ProximoVoo.ParallaxLandscape>();
if (scenery == null) throw new System.Exception("Open ProximoVoo scene first.");
var camera = UnityEngine.Camera.main;
var material = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/Art/Materials/AircraftUnlit.mat");
UnityEditor.Undo.RecordObject(scenery, "Cenário cinematográfico");
scenery.Configure(camera, sprites[0], sprites[1], sprites[2], sprites[3], material);
scenery.name = "Paisagem cinematográfica em parallax";
UnityEditor.EditorUtility.SetDirty(scenery);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scenery.gameObject.scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scenery.gameObject.scene);
UnityEditor.AssetDatabase.SaveAssets();
return new { scene = scenery.gameObject.scene.path, layers = 4, sky = sprites[0].name, coast = sprites[1].name };
