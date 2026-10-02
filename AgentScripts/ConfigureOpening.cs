if (UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Stop Play mode before configuring the opening.");
UnityEditor.AssetDatabase.Refresh();
var plane = UnityEngine.Object.FindFirstObjectByType<ProximoVoo.FlightController>();
if (plane == null) throw new System.Exception("FlightController is missing.");
var camera = UnityEngine.Camera.main;
var airportPath = "Assets/Art/Scenery/aeroporto-entardecer.png";
var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(airportPath);
importer.textureType = UnityEditor.TextureImporterType.Sprite;
importer.spriteImportMode = UnityEditor.SpriteImportMode.Single;
importer.spritePixelsPerUnit = 100f;
importer.alphaIsTransparency = true;
importer.mipmapEnabled = false;
importer.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
importer.maxTextureSize = 2048;
var importSettings = new UnityEditor.TextureImporterSettings();
importer.ReadTextureSettings(importSettings);
importSettings.spriteMeshType = UnityEngine.SpriteMeshType.FullRect;
importer.SetTextureSettings(importSettings);
importer.SaveAndReimport();
var airportSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(airportPath);
var material = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/Art/Materials/AircraftUnlit.mat");
var airport = UnityEngine.Object.FindFirstObjectByType<ProximoVoo.AirportOpening>();
if (airport == null) airport = new UnityEngine.GameObject("Aeroporto e pista", typeof(ProximoVoo.AirportOpening)).GetComponent<ProximoVoo.AirportOpening>();
airport.Configure(plane, camera, airportSprite, material);
UnityEditor.EditorUtility.SetDirty(airport);
plane.transform.position = new UnityEngine.Vector3(0f,1.65f,0f);
plane.transform.Find("Arte do avião").localRotation = UnityEngine.Quaternion.Euler(0f,0f,9f);
camera.transform.position = new UnityEngine.Vector3(3.5f,3.9425f,-10f);

var fonts = UnityEditor.AssetDatabase.FindAssets("t:TMP_FontAsset");
if (fonts.Length == 0) throw new System.Exception("TMP fonts are not yet imported.");
var font = TMPro.TMP_Settings.defaultFontAsset;
if (font == null) font = UnityEditor.AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(UnityEditor.AssetDatabase.GUIDToAssetPath(fonts[0]));
var existingCanvas = UnityEngine.GameObject.Find("AberturaCanvas");
if (existingCanvas != null) throw new System.Exception("Opening canvas already exists; edit it rather than rebuilding.");
var canvasObject = new UnityEngine.GameObject("AberturaCanvas", typeof(UnityEngine.RectTransform), typeof(UnityEngine.Canvas),
    typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster), typeof(ProximoVoo.FlightOpeningUI));
var canvas = canvasObject.GetComponent<UnityEngine.Canvas>();
canvas.renderMode = UnityEngine.RenderMode.ScreenSpaceOverlay;
canvas.sortingOrder = 100;
var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
scaler.referenceResolution = new UnityEngine.Vector2(1920f,1080f);
scaler.matchWidthOrHeight = .5f;
var menu = new UnityEngine.GameObject("MenuInicial", typeof(UnityEngine.RectTransform));
var menuRect = menu.GetComponent<UnityEngine.RectTransform>();
menuRect.SetParent(canvasObject.transform, false);
menuRect.anchorMin = UnityEngine.Vector2.zero;
menuRect.anchorMax = UnityEngine.Vector2.one;
menuRect.offsetMin = UnityEngine.Vector2.zero;
menuRect.offsetMax = UnityEngine.Vector2.zero;

System.Func<string,string,UnityEngine.Transform,UnityEngine.Vector2,UnityEngine.Vector2,float,TMPro.TextMeshProUGUI> makeText =
    (name, content, parent, anchor, size, pointSize) =>
{
    var item = new UnityEngine.GameObject(name, typeof(UnityEngine.RectTransform), typeof(TMPro.TextMeshProUGUI));
    var rect = item.GetComponent<UnityEngine.RectTransform>();
    rect.SetParent(parent, false);
    rect.anchorMin = rect.anchorMax = anchor;
    rect.pivot = new UnityEngine.Vector2(.5f,.5f);
    rect.sizeDelta = size;
    var text = item.GetComponent<TMPro.TextMeshProUGUI>();
    text.font = font;
    text.text = content;
    text.fontSize = pointSize;
    text.alignment = TMPro.TextAlignmentOptions.Center;
    text.color = new UnityEngine.Color(1f,.97f,.88f);
    text.raycastTarget = false;
    var shadow = item.AddComponent<UnityEngine.UI.Shadow>();
    shadow.effectColor = new UnityEngine.Color(.015f,.05f,.12f,.7f);
    shadow.effectDistance = new UnityEngine.Vector2(1f,-3f);
    return text;
};
var title = makeText("Titulo", "O Próximo Voo", menu.transform, new UnityEngine.Vector2(.5f,.83f), new UnityEngine.Vector2(1000f,100f), 76f);
title.fontStyle = TMPro.FontStyles.Bold;
makeText("Subtitulo", "Tudo pronto para partir.", menu.transform, new UnityEngine.Vector2(.5f,.75f), new UnityEngine.Vector2(800f,60f), 30f);
var buttonObject = new UnityEngine.GameObject("IniciarVoo", typeof(UnityEngine.RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
var buttonRect = buttonObject.GetComponent<UnityEngine.RectTransform>();
buttonRect.SetParent(menu.transform, false);
buttonRect.anchorMin = buttonRect.anchorMax = new UnityEngine.Vector2(.5f,.64f);
buttonRect.pivot = new UnityEngine.Vector2(.5f,.5f);
buttonRect.sizeDelta = new UnityEngine.Vector2(330f,80f);
var image = buttonObject.GetComponent<UnityEngine.UI.Image>();
image.color = new UnityEngine.Color(.035f,.20f,.34f,.97f);
image.raycastTarget = true;
var button = buttonObject.GetComponent<UnityEngine.UI.Button>();
button.targetGraphic = image;
var colors = button.colors;
colors.normalColor = UnityEngine.Color.white;
colors.highlightedColor = new UnityEngine.Color(.75f,.91f,1f);
colors.pressedColor = new UnityEngine.Color(.65f,.78f,.88f);
button.colors = colors;
var buttonShadow = buttonObject.AddComponent<UnityEngine.UI.Shadow>();
buttonShadow.effectColor = new UnityEngine.Color(0f,0f,0f,.35f);
buttonShadow.effectDistance = new UnityEngine.Vector2(0f,-5f);
var buttonLabel = makeText("Texto", "Iniciar voo", buttonObject.transform, new UnityEngine.Vector2(.5f,.5f), new UnityEngine.Vector2(310f,70f), 32f);
buttonLabel.fontStyle = TMPro.FontStyles.Bold;
UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, plane.StartFlight);
makeText("Instrucao", "Decolagem assistida. Depois, W para subir e S para descer.", menu.transform,
    new UnityEngine.Vector2(.5f,.55f), new UnityEngine.Vector2(1100f,55f), 25f);
var status = makeText("EstadoDoVoo", "", canvasObject.transform, new UnityEngine.Vector2(.5f,.88f), new UnityEngine.Vector2(1300f,65f), 30f);
status.gameObject.SetActive(false);
canvasObject.GetComponent<ProximoVoo.FlightOpeningUI>().Configure(plane, menu, status);
var eventSystems = UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(UnityEngine.FindObjectsSortMode.None);
if (eventSystems.Length > 1) throw new System.Exception("Multiple EventSystems need resolving.");
var eventSystem = eventSystems.Length == 1 ? eventSystems[0] :
    new UnityEngine.GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem),
        typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule)).GetComponent<UnityEngine.EventSystems.EventSystem>();
if (eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
    eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
eventSystem.firstSelectedGameObject = buttonObject;
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(plane.gameObject.scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(plane.gameObject.scene);
UnityEditor.AssetDatabase.SaveAssets();
return new { scene = plane.gameObject.scene.path, button = button.name, clickTarget = button.onClick.GetPersistentTarget(0).name,
    eventSystems = UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(UnityEngine.FindObjectsSortMode.None).Length };
