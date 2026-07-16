using System;
using UnityEngine;

namespace MobileCore
{
    [CreateAssetMenu(fileName = "ItemData", menuName = "Scriptable Objects/ItemData")]
    public class ItemData : ScriptableObject
    {
        public string itemID;
        public string itemName;
        public Sprite itemIcon;
        public GameObject itemPrefab;
    }
}