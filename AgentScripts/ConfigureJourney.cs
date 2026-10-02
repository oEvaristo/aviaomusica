if (UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Stop Play mode before configuring the journey.");
UnityEditor.AssetDatabase.Refresh();
var flight = UnityEngine.Object.FindFirstObjectByType<ProximoVoo.FlightController>();
var camera = UnityEngine.Camera.main;
var canvas = UnityEngine.GameObject.Find("AberturaCanvas");
if (flight == null || camera == null || canvas == null) throw new System.Exception("Existing flight scene is missing.");
if (canvas.transform.Find("ContadorNotas") != null) throw new System.Exception("Journey UI already exists; edit it in place.");
var path = "Assets/Art/Collectibles/nota-musical-dourada.png";
var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
importer.textureType = UnityEditor.TextureImporterType.Sprite;
importer.spriteImportMode = UnityEditor.SpriteImportMode.Single;
importer.spritePixelsPerUnit = 512f;
importer.alphaIsTransparency = true;
importer.mipmapEnabled = false;
importer.maxTextureSize = 2048;
importer.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
importer.SaveAndReimport();
var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(path);
var material = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/Art/Materials/AircraftUnlit.mat");
var font = UnityEditor.AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/UI/Fonts/Roboto SDF.asset");
font.TryAddCharacters("Notas: 0123456789Nos hangaresCompartilhando conhecimentoUma nova conquistaUm legado que inspiraLito Sousa dedicou mais de 35 anos à aviação.Como mecânico, passou por Varig, Transbrasil e United.Em 2004, criou o blog Aviões e Músicas.Em 2010, levou ao YouTube sua paixão por ensinar.Em 2021, Lito também se tornou piloto.No mesmo ano, recebeu o Destaque SIPAER do Cenipa.Com explicações simples, ajudou pessoas a perder o medo de voar.Seu legado de conhecimento continua inspirando novos sonhos.");
UnityEditor.EditorUtility.SetDirty(font);
foreach (var atlas in font.atlasTextures) if (atlas != null && !UnityEditor.AssetDatabase.Contains(atlas)) UnityEditor.AssetDatabase.AddObjectToAsset(atlas, font);

System.Func<string,UnityEngine.Transform,UnityEngine.Vector2,UnityEngine.Vector2,UnityEngine.Vector2,UnityEngine.Vector2,UnityEngine.GameObject> makeRect =
    (name,parent,anchor,pivot,position,size) =>
{
    var item = new UnityEngine.GameObject(name, typeof(UnityEngine.RectTransform));
    var rect = item.GetComponent<UnityEngine.RectTransform>();
    rect.SetParent(parent, false);
    rect.anchorMin = rect.anchorMax = anchor;
    rect.pivot = pivot;
    rect.anchoredPosition = position;
    rect.sizeDelta = size;
    return item;
};
System.Func<string,string,UnityEngine.Transform,UnityEngine.Vector2,UnityEngine.Vector2,float,TMPro.TextMeshProUGUI> makeText =
    (name,content,parent,position,size,fontSize) =>
{
    var item = makeRect(name,parent,new UnityEngine.Vector2(.5f,.5f),new UnityEngine.Vector2(.5f,.5f),position,size);
    var text = item.AddComponent<TMPro.TextMeshProUGUI>();
    text.font = font;
    text.text = content;
    text.fontSize = fontSize;
    text.color = new UnityEngine.Color(1f,.98f,.91f);
    text.alignment = TMPro.TextAlignmentOptions.Center;
    text.raycastTarget = false;
    return text;
};
var score = makeRect("ContadorNotas",canvas.transform,new UnityEngine.Vector2(0f,1f),new UnityEngine.Vector2(0f,1f),new UnityEngine.Vector2(24f,-24f),new UnityEngine.Vector2(300f,80f));
var scoreBackground = score.AddComponent<UnityEngine.UI.Image>();
scoreBackground.color = new UnityEngine.Color(.025f,.09f,.16f,.78f);
scoreBackground.raycastTarget = false;
var icon = makeRect("NotaDourada",score.transform,new UnityEngine.Vector2(0f,.5f),new UnityEngine.Vector2(.5f,.5f),new UnityEngine.Vector2(40f,0f),new UnityEngine.Vector2(46f,60f));
var iconImage = icon.AddComponent<UnityEngine.UI.Image>();
iconImage.sprite = sprite;
iconImage.preserveAspect = true;
iconImage.raycastTarget = false;
var scoreText = makeText("TotalNotas","Notas: 0",score.transform,new UnityEngine.Vector2(26f,0f),new UnityEngine.Vector2(214f,60f),32f);
scoreText.alignment = TMPro.TextAlignmentOptions.Left;
scoreText.color = new UnityEngine.Color(1f,.85f,.39f);
score.SetActive(false);

var message = makeRect("MensagemHomenagem",canvas.transform,new UnityEngine.Vector2(.5f,1f),new UnityEngine.Vector2(.5f,1f),new UnityEngine.Vector2(0f,-124f),new UnityEngine.Vector2(1450f,184f));
var background = message.AddComponent<UnityEngine.UI.Image>();
background.color = new UnityEngine.Color(.025f,.09f,.16f,.8f);
background.raycastTarget = false;
var group = message.AddComponent<UnityEngine.CanvasGroup>();
group.interactable = false;
group.blocksRaycasts = false;
group.alpha = 0f;
var title = makeText("TituloBloco","Nos hangares",message.transform,new UnityEngine.Vector2(0f,52f),new UnityEngine.Vector2(1390f,46f),30f);
title.fontStyle = TMPro.FontStyles.Bold;
title.color = new UnityEngine.Color(1f,.85f,.39f);
var body = makeText("TextoBloco","",message.transform,new UnityEngine.Vector2(0f,-24f),new UnityEngine.Vector2(1390f,100f),30f);
message.SetActive(false);
var journey = new UnityEngine.GameObject("Notas e homenagem",typeof(ProximoVoo.FlightJourney)).GetComponent<ProximoVoo.FlightJourney>();
journey.Configure(flight,camera,sprite,material,score,scoreText,group,title,body);
UnityEditor.EditorUtility.SetDirty(journey);
var serializedFlight = new UnityEditor.SerializedObject(flight);
serializedFlight.FindProperty("flightDuration").floatValue = 80f;
serializedFlight.ApplyModifiedProperties();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(flight.gameObject.scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
UnityEditor.AssetDatabase.SaveAssets();
return new {scene=flight.gameObject.scene.path,duration=flight.FlightDuration,sprite=sprite.name,ui=message.name};
