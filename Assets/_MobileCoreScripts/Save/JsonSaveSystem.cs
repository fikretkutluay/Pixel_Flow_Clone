using System.IO;
using UnityEngine;
namespace MobileCore
{
public class JsonSaveSystem : ISerializer
{
    public void Save<T>(T data, string fileName)
    {
        string json = JsonUtility.ToJson(data, true);
        string path = Path.Combine(Application.persistentDataPath, fileName + ".json");
        System.IO.File.WriteAllText(path, json);
        Debug.Log("Data saved to: " + path);
    }
    public void Load<T>(string fileName, out T data)
    {
        string path = Path.Combine(Application.persistentDataPath, fileName + ".json");
        if (System.IO.File.Exists(path))
        {
            string json = System.IO.File.ReadAllText(path);
            data = JsonUtility.FromJson<T>(json);
            Debug.Log("Data loaded from: " + path);
        }
        else
        {
            Debug.LogWarning("Save file not found at: " + path);
            data = default(T);
        }
    }   
}
}