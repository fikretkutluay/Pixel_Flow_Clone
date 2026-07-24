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
        private Camera mainCam;   // Init'te cache — her SlotPosition'da Camera.main aramamak için

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

        public Shooter PeekTopShooter(int column)
        {
            if (visible == null || column < 0 || column >= visible.Length) return null;
            return visible[column].Count > 0 ? visible[column][0] : null;
        }

        private void HandleTap(Vector2 screenPos)
        {
            Ray ray = mainCam.ScreenPointToRay(screenPos);
            if (!Physics.Raycast(ray, out RaycastHit hit)) return;

            Shooter s = hit.collider.GetComponent<Shooter>();
            if (s == null) return;

            OnShooterTapped(s);
        }

        public void Init(ShooterDef[] queueDefs, int columnCount)
        {
            this.columnCount = columnCount;
            mainCam = Camera.main;
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

        public void Clear()
        {
            if (visible == null) return;
            for (int col = 0; col < columnCount; col++)
            {
                foreach (Shooter s in visible[col])
                {
                    ObjectPooler.Instance.ReturnToPool("Shooter", s.gameObject);
                }
                visible[col].Clear();
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

        // Sütunlar yatay yayılır (ekran genişliğine), derinlik (index) dikey aşağı yığılır.
        // Konum sabit origin'den değil, queueBand + görünür genişlikten türetilir.
        private Vector3 SlotPosition(int column, int index)
        {
            float usableWidth = GameLayout.VisibleWidth(mainCam) * config.contentWidthFactor;
            float columnWidth = usableWidth / columnCount;
            float x = -usableWidth * 0.5f + columnWidth * (column + 0.5f);

            float visH = GameLayout.VisibleHeight(mainCam);
            float bandTopY = GameLayout.QueueBandCenterY(mainCam, config) + config.queueBand * visH * 0.5f;
            float y = bandTopY - config.queueSlotSpacing * (index + 0.5f);

            return new Vector3(x, y, 0f);
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