using System.Collections;
using UnityEngine;

namespace Game
{
    public class ParkSlotView : MonoBehaviour
    {
        [SerializeField] private LineRenderer border;
        [SerializeField] private float slotSize = 0.9f;
        [SerializeField] private Color idleColor = Color.gray;
        [SerializeField] private Color alertColor = Color.red;
        [SerializeField] private float blinkInterval = 0.3f;

        private Coroutine blinkRoutine;

        private void Awake()
        {
            float h = slotSize / 2f;
            border.positionCount = 5;
            border.SetPosition(0, new Vector3(-h, -h, 0));
            border.SetPosition(1, new Vector3(-h, h, 0));
            border.SetPosition(2, new Vector3(h, h, 0));
            border.SetPosition(3, new Vector3(h, -h, 0));
            border.SetPosition(4, new Vector3(-h, -h, 0));
            SetColor(idleColor);
        }

        public void SetAlert(bool active)
        {
            if (active && blinkRoutine == null)
                blinkRoutine = StartCoroutine(BlinkRoutine());
            else if (!active && blinkRoutine != null)
            {
                StopCoroutine(blinkRoutine);
                blinkRoutine = null;
                SetColor(idleColor);
            }
        }

        private IEnumerator BlinkRoutine()
        {
            bool on = false;
            while (true)
            {
                SetColor(on ? alertColor : idleColor);
                on = !on;
                yield return new WaitForSeconds(blinkInterval);
            }
        }

        private void SetColor(Color c)
        {
            border.startColor = c;
            border.endColor = c;
        }
    }
}