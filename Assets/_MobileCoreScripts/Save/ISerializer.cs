using UnityEngine;
namespace MobileCore
{
public interface ISerializer
{
    public void Save<T>(T data, string fileName);
    public void Load<T>(string fileName, out T data);
}
}
