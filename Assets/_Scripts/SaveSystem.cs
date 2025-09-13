using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private static readonly string saveFolder = Application.persistentDataPath + "/saves/";

    public static void Save<T>(T data, string fileName)
    {
        if (!Directory.Exists(saveFolder))
            Directory.CreateDirectory(saveFolder);

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(saveFolder + fileName + ".json", json);
        Debug.Log($"[SaveSystem] Saved to {saveFolder}{fileName}.json");
    }

    public static T Load<T>(string fileName)
    {
        string path = saveFolder + fileName + ".json";

        if (!File.Exists(path))
        {
            Debug.LogWarning($"[SaveSystem] Save file not found: {path}");
            return default;
        }

        string json = File.ReadAllText(path);
        return JsonUtility.FromJson<T>(json);
    }

    public static bool SaveExists(string fileName)
    {
        return File.Exists(saveFolder + fileName + ".json");
    }

    public static void DeleteSave(string fileName)
    {
        string path = saveFolder + fileName + ".json";
        if (File.Exists(path))
            File.Delete(path);
    }
}
