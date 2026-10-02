UnityEditor.AssetDatabase.Refresh();
System.IO.Directory.CreateDirectory("Assets/UI/Resources");
var settingsPath = "Assets/UI/Resources/TMP Settings.asset";
var settings = UnityEditor.AssetDatabase.LoadAssetAtPath<TMPro.TMP_Settings>(settingsPath);
if (settings == null)
{
    settings = UnityEngine.ScriptableObject.CreateInstance<TMPro.TMP_Settings>();
    UnityEditor.AssetDatabase.CreateAsset(settings, settingsPath);
}
var settingsObject = new UnityEditor.SerializedObject(settings);
settingsObject.FindProperty("assetVersion").stringValue = "2";
settingsObject.FindProperty("m_defaultFontSize").floatValue = 36f;
settingsObject.FindProperty("m_fallbackFontAssets").arraySize = 0;
settingsObject.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.AssetDatabase.SaveAssets();
TMPro.TMP_Settings.LoadDefaultSettings();
var fontPath = "Assets/UI/Fonts/Roboto SDF.asset";
var font = UnityEditor.AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(fontPath);
if (font == null)
{
    var source = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Font>("Assets/UI/Fonts/Roboto-Regular.ttf");
    if (source == null) throw new System.Exception("The installed font could not be loaded.");
    font = TMPro.TMP_FontAsset.CreateFontAsset(source);
    font.name = "Roboto SDF";
    font.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789áàâãéêíóôõúçÁÀÂÃÉÊÍÓÔÕÚÇ.,:!?—…• /↑↓");
    UnityEditor.AssetDatabase.CreateAsset(font, fontPath);
    font.material.name = "Roboto SDF Material";
    UnityEditor.AssetDatabase.AddObjectToAsset(font.material, font);
    foreach (var texture in font.atlasTextures)
        if (texture != null && !UnityEditor.AssetDatabase.Contains(texture))
            UnityEditor.AssetDatabase.AddObjectToAsset(texture, font);
}
settingsObject.Update();
settingsObject.FindProperty("m_defaultFontAsset").objectReferenceValue = font;
settingsObject.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.EditorUtility.SetDirty(settings);
UnityEditor.EditorUtility.SetDirty(font);
UnityEditor.AssetDatabase.SaveAssets();
TMPro.TMP_Settings.defaultFontAsset = font;
return new { font = font.name, settings = settingsPath, characters = font.characterTable.Count };
