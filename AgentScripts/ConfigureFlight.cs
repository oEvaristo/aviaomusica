// Run as a statement block using the connected Unity Editor's eval_file command.
if (UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Stop Play mode before configuring the scene.");
UnityEditor.AssetDatabase.Refresh();
var bodyPath = "Assets/Art/Aircraft/aviao-sem-helice.png";
var sheetPath = "Assets/Art/Aircraft/helice-spritesheet.png";
var bodyImporter = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(bodyPath);
bodyImporter.textureType = UnityEditor.TextureImporterType.Sprite;
bodyImporter.spriteImportMode = UnityEditor.SpriteImportMode.Single;
bodyImporter.spritePixelsPerUnit = 350f;
bodyImporter.alphaIsTransparency = true;
bodyImporter.mipmapEnabled = false;
bodyImporter.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
bodyImporter.maxTextureSize = 2048;
bodyImporter.SaveAndReimport();
var sheetImporter = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(sheetPath);
sheetImporter.textureType = UnityEditor.TextureImporterType.Sprite;
sheetImporter.spriteImportMode = UnityEditor.SpriteImportMode.Multiple;
sheetImporter.spritePixelsPerUnit = 390f;
sheetImporter.alphaIsTransparency = true;
sheetImporter.mipmapEnabled = false;
sheetImporter.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
sheetImporter.maxTextureSize = 2048;
sheetImporter.SaveAndReimport();

var factory = new UnityEditor.U2D.Sprites.SpriteDataProviderFactories();
factory.Init();
var provider = factory.GetSpriteEditorDataProviderFromObject(sheetImporter);
if (provider == null) throw new System.Exception("Sprite data provider unavailable.");
provider.InitSpriteEditorDataProvider();
var edit = provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteFrameEditCapability>();
if (edit == null) throw new System.Exception("Sprite editing capabilities unavailable; aborting.");
var capabilities = edit.GetEditCapability();
foreach (var capability in new[] { UnityEditor.U2D.Sprites.EEditCapability.CreateAndDeleteSprite,
    UnityEditor.U2D.Sprites.EEditCapability.EditSpriteRect, UnityEditor.U2D.Sprites.EEditCapability.EditPivot,
    UnityEditor.U2D.Sprites.EEditCapability.EditSpriteName })
    if (!capabilities.HasCapability(capability)) throw new System.Exception("Sprite slicing unsupported: " + capability);
var sheetTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(sheetPath);
float cell = sheetTexture.width / 2f;
var rects = new UnityEditor.SpriteRect[4];
var names = new System.Collections.Generic.List<UnityEditor.SpriteNameFileIdPair>();
for (int i = 0; i < 4; i++)
{
    var id = UnityEngine.GUID.Generate();
    var name = "helice_" + i;
    rects[i] = new UnityEditor.SpriteRect { name = name, spriteID = id,
        rect = new UnityEngine.Rect(i % 2 * cell, (1 - i / 2) * cell, cell, cell),
        alignment = UnityEngine.SpriteAlignment.Center, pivot = new UnityEngine.Vector2(.5f, .5f) };
    names.Add(new UnityEditor.SpriteNameFileIdPair(name, id));
}
provider.SetSpriteRects(rects);
var nameProvider = provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteNameFileIdDataProvider>();
if (nameProvider == null) throw new System.Exception("Sprite ID mapping unavailable.");
nameProvider.SetNameFileIdPairs(names);
provider.Apply();
sheetImporter.SaveAndReimport();
var frames = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.OrderBy(
    System.Linq.Enumerable.OfType<UnityEngine.Sprite>(UnityEditor.AssetDatabase.LoadAllAssetsAtPath(sheetPath)), s => s.name));
if (frames.Length != 4) throw new System.Exception("Expected four propeller frames, found " + frames.Length);
var bodySprite = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(bodyPath);
if (bodySprite == null) throw new System.Exception("Aircraft sprite failed to import.");

var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (currentScene.isDirty && !string.IsNullOrEmpty(currentScene.path))
    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(currentScene);
var flightScene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
    UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single);
var cameraObject = new UnityEngine.GameObject("Main Camera", typeof(UnityEngine.Camera), typeof(UnityEngine.AudioListener));
cameraObject.tag = "MainCamera";
var camera = cameraObject.GetComponent<UnityEngine.Camera>();
camera.orthographic = true;
camera.orthographicSize = 6f;
camera.clearFlags = UnityEngine.CameraClearFlags.SolidColor;
camera.backgroundColor = new UnityEngine.Color(.35f,.4f,.6f);
camera.transform.position = new UnityEngine.Vector3(3.5f,5f,-10f);
var plane = new UnityEngine.GameObject("Avião — Aviões e Musica", typeof(ProximoVoo.FlightController));
plane.transform.position = new UnityEngine.Vector3(0f,4f,0f);
var visual = new UnityEngine.GameObject("Arte do avião").transform;
visual.SetParent(plane.transform, false);
var body = new UnityEngine.GameObject("Fuselagem", typeof(UnityEngine.SpriteRenderer));
body.transform.SetParent(visual, false);
var bodyRenderer = body.GetComponent<UnityEngine.SpriteRenderer>();
bodyRenderer.sprite = bodySprite;
bodyRenderer.sortingOrder = 10;
var blades = new UnityEngine.GameObject("Hélice animada", typeof(UnityEngine.SpriteRenderer));
blades.transform.SetParent(visual, false);
blades.transform.localPosition = new UnityEngine.Vector3(2.45f,.028f,0f);
var propeller = blades.GetComponent<UnityEngine.SpriteRenderer>();
propeller.sprite = frames[0];
propeller.sortingOrder = 9;
var spriteShader = UnityEngine.Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
if (spriteShader == null) spriteShader = UnityEngine.Shader.Find("Sprites/Default");
var spriteMaterial = new UnityEngine.Material(spriteShader);
System.IO.Directory.CreateDirectory("Assets/Art/Materials");
var materialPath = "Assets/Art/Materials/AircraftUnlit.mat";
if (UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(materialPath) == null)
    UnityEditor.AssetDatabase.CreateAsset(spriteMaterial, materialPath);
else
{
    UnityEngine.Object.DestroyImmediate(spriteMaterial);
    spriteMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(materialPath);
}
bodyRenderer.sharedMaterial = spriteMaterial;
propeller.sharedMaterial = spriteMaterial;
plane.GetComponent<ProximoVoo.FlightController>().Configure(visual, camera, propeller, frames);
var scenery = new UnityEngine.GameObject("Paisagem em parallax", typeof(ProximoVoo.ParallaxLandscape));
scenery.GetComponent<ProximoVoo.ParallaxLandscape>().Configure(camera,
    UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>("Assets/Art/Scenery/ceu-entardecer.png"),
    UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>("Assets/Art/Scenery/montanhas-costa.png"),
    UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>("Assets/Art/Scenery/floresta-proxima.png"),
    UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>("Assets/Art/Scenery/nuvem-volumetrica.png"), spriteMaterial);
var scenePath = "Assets/Scenes/ProximoVoo.unity";
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(flightScene, scenePath);
UnityEditor.EditorBuildSettings.scenes = new[] { new UnityEditor.EditorBuildSettingsScene(scenePath, true) };
UnityEditor.AssetDatabase.SaveAssets();
UnityEditor.Selection.activeGameObject = plane;
UnityEditor.SceneView.lastActiveSceneView?.FrameSelected();
return new { scene = scenePath, sprites = frames.Length, camera = camera.name, shader = spriteShader.name };
