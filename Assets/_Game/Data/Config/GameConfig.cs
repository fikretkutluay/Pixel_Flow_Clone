using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Scriptable Objects/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        public int visibleQueueWindow = 3;      
        public float boardPhysicalSize = 8f;    
        public float queuePhysicalWidth = 8f;   
        public float queueSlotSpacing = 1f;
        public Vector3 queueOrigin;  
        public Vector3 parkOrigin;
        public float parkSlotSpacing = 1f;           
    }
}