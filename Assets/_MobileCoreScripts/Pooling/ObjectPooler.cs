using System.Collections.Generic;
using UnityEngine;
namespace MobileCore
{
    public class ObjectPooler : MonoBehaviour
    {
        public static ObjectPooler Instance;
        [SerializeField] private List<Pool> pools;
        private Dictionary<string, Queue<GameObject>> poolDictionary;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            poolDictionary = new Dictionary<string, Queue<GameObject>>();

            foreach (Pool pool in pools)
            {
                Queue<GameObject> objectPool = new Queue<GameObject>();
                for (int i = 0; i < pool.size; i++)
                {
                    GameObject obj = Instantiate(pool.prefab);
                    obj.SetActive(false);
                    objectPool.Enqueue(obj);
                }
                poolDictionary.Add(pool.tag, objectPool);
            }
        }

        public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
        {
            if (!poolDictionary.ContainsKey(tag)) return null;

            if (poolDictionary[tag].Count == 0)
            {
                Pool pool = pools.Find(p => p.tag == tag);
                GameObject newObj = Instantiate(pool.prefab);
                newObj.SetActive(true);
                newObj.transform.position = position;
                newObj.transform.rotation = rotation;
                return newObj;
            }

            GameObject objectToSpawn = poolDictionary[tag].Dequeue();
            objectToSpawn.SetActive(true);
            objectToSpawn.transform.position = position;
            objectToSpawn.transform.rotation = rotation;
            return objectToSpawn;
        }

        public void ReturnToPool(string tag, GameObject objectToReturn)
        {
            if (!poolDictionary.ContainsKey(tag))
            {
                Debug.LogWarning("Object returned to unknown pool: " + tag);
                return;
            }
            objectToReturn.SetActive(false);
            poolDictionary[tag].Enqueue(objectToReturn);
        }
    }
}