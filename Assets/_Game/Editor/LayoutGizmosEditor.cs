using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// LayoutGizmos için Scene view'da sürüklenebilir bant sınırları.
    /// GameConfig tek doğruluk kaynağı kalır — tutamaçlar onu düzenlemenin
    /// görsel bir yolu. Bir sınırı sürüklemek komşu iki bandın oranını
    /// (toplamları sabit kalacak şekilde) karşılıklı değiştirir.
    /// </summary>
    [CustomEditor(typeof(LayoutGizmos))]
    public class LayoutGizmosEditor : Editor
    {
        private const float MinBandFraction = 0.02f;

        private void OnSceneGUI()
        {
            var gizmos = (LayoutGizmos)target;
            Camera cam = gizmos.GizmoCamera;
            GameConfig config = gizmos.GizmoConfig;
            if (cam == null || config == null || !cam.orthographic) return;

            float[] bands = { config.topBarBand, config.boardBand, config.parkBand, config.queueBand, config.bottomBand };
            string[] labels = { "Top/Board", "Board/Park", "Park/Queue", "Queue/Bottom" };

            float[] cum = new float[6];
            cum[0] = 0f;
            for (int i = 0; i < 5; i++) cum[i + 1] = cum[i] + bands[i];

            float visW = GameLayout.VisibleWidth(cam);
            float cx = cam.transform.position.x;

            EditorGUI.BeginChangeCheck();

            for (int d = 1; d <= 4; d++)
            {
                float worldY = GameLayout.WorldYAtViewportFraction(cam, cum[d]);
                Vector3 handlePos = new Vector3(cx, worldY, 0f);

                Handles.color = Color.yellow;
                Handles.DrawLine(new Vector3(cx - visW * 0.5f, worldY, 0f), new Vector3(cx + visW * 0.5f, worldY, 0f));

                float handleSize = HandleUtility.GetHandleSize(handlePos) * 0.15f;
                Vector3 newPos = Handles.FreeMoveHandle(handlePos, handleSize, Vector3.zero, Handles.SphereHandleCap);

                Handles.Label(handlePos + new Vector3(visW * 0.5f + 0.2f, 0f, 0f),
                    $"{labels[d - 1]}  ({bands[d - 1] * 100f:0}% / {bands[d] * 100f:0}%)");

                if (newPos != handlePos)
                {
                    // Sürüklenen noktanın SADECE dikey (viewport-Y) bileşeni işimize yarıyor;
                    // yan/derinlik sapması olsa da fraksiyon hesabını etkilemez.
                    Vector3 vp = cam.WorldToViewportPoint(newPos);
                    float newCum = Mathf.Clamp(1f - vp.y, cum[d - 1] + MinBandFraction, cum[d + 1] - MinBandFraction);

                    float delta = newCum - cum[d];
                    bands[d - 1] += delta;
                    bands[d] -= delta;
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(config, "Adjust Layout Band");
                config.topBarBand = bands[0];
                config.boardBand = bands[1];
                config.parkBand = bands[2];
                config.queueBand = bands[3];
                config.bottomBand = bands[4];
                EditorUtility.SetDirty(config);
            }
        }
    }
}