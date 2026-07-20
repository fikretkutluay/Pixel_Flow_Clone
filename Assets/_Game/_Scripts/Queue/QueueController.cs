using System.Collections.Generic;
using UnityEngine;
using MobileCore;

namespace Game
{
    public class QueueController : MonoBehaviour
    {
        [SerializeField] private TrackController trackController;
        [SerializeField] private GameConfig config;

        private Queue<ShooterDef>[] pending;
        private List<Shooter>[] visible;
        private int columnCount;

        [SerializeField] private PointerRouter inputRouter;

        private void OnEnable()
        {
            if (inputRouter != null)
                inputRouter.OnTap += HandleTap;
        }

        private void OnDisable()
        {
            if (inputRouter != null)
                inputRouter.OnTap -= HandleTap;
        }

        private void HandleTap(Vector2 screenPos)
        {
            Ray ray = Camera.main.ScreenPointToRay(screenPos);
            if (!Physics.Raycast(ray, out RaycastHit hit)) return;

            Shooter s = hit.collider.GetComponent<Shooter>();
            if (s == null) return;

            OnShooterTapped(s);
        }
        public void Init(ShooterDef[] queueDefs, int columnCount)
        {
            this.columnCount = columnCount;
            pending = new Queue<ShooterDef>[columnCount];
            visible = new List<Shooter>[columnCount];

            for (int i = 0; i < columnCount; i++)
            {
                pending[i] = new Queue<ShooterDef>();
                visible[i] = new List<Shooter>();
            }

            foreach (var def in queueDefs)
            {
                pending[def.column].Enqueue(def);
            }

            for (int col = 0; col < columnCount; col++)
            {
                FillWindow(col);
            }
        }

        private void FillWindow(int column)
        {
            while ((visible[column].Count < config.visibleQueueWindow) && (pending[column].Count > 0))
            {
                ShooterDef def = pending[column].Dequeue();
                Shooter s = SpawnShooter(def);
                visible[column].Add(s);
            }

            RefreshColumn(column);
        }

        private Shooter SpawnShooter(ShooterDef def)
        {
            GameObject obj = ObjectPooler.Instance.SpawnFromPool("Shooter", Vector3.zero, Quaternion.identity);
            if (obj == null) return null;

            Shooter s = obj.GetComponent<Shooter>();
            s.Init(def.color, def.ammo, def.isHidden);

            return s;
        }

        private void RefreshColumn(int column)
        {
            for (int i = 0; i < visible[column].Count; i++)
            {
                Shooter s = visible[column][i];
                s.transform.position = SlotPosition(column, i);
                if (i == 0 && s.IsHidden) s.Reveal();
            }
        }

        private Vector3 SlotPosition(int column, int index)
        {
            float columnWidth = config.queuePhysicalWidth / columnCount;
            float x = config.queueOrigin.x + columnWidth * (column + 0.5f);
            float y = config.queueOrigin.y - index * config.queueSlotSpacing;
            return new Vector3(x, y, 0);
        }

        public void OnShooterTapped(Shooter tapped)
        {
            int column = FindColumn(tapped);
            if (column < 0) return;

            if (visible[column][0] != tapped)
            {
                RejectTap(tapped);
                return;
            }

            if (!trackController.TryAddShooter(tapped))
            {
                RejectTap(tapped);
                return;
            }

            visible[column].RemoveAt(0);
            FillWindow(column);
        }

        private void RejectTap(Shooter s)
        {
            Debug.Log("rejected");
        }

        private int FindColumn(Shooter s)
        {
            for (int col = 0; col < columnCount; col++)
            {
                if (visible[col].Contains(s)) return col;
            }
            return -1;
        }

    }


}