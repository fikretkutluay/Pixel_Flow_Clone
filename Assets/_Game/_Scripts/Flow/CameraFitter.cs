using UnityEngine;

namespace Game
{
    /// <summary>
    /// Locks the orthographic camera to a fixed world WIDTH: the board covers the
    /// same horizontal span on every device and aspect-ratio differences are
    /// absorbed vertically. This is a game-specific framing rule, so it lives in
    /// Game rather than Core.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public class CameraFitter : MonoBehaviour
    {
        [SerializeField] private Camera cam;
        [Tooltip("Fixed world width the screen spans horizontally. Should be a little " +
                 "wider than the rail's outer width: the board is fitted inside the " +
                 "rail, so the rail sets the scale.")]
        [SerializeField] private float visibleWorldWidth = 10f;

        private float lastAspect = -1f;

        private void Awake()
        {
            if (cam == null) cam = GetComponent<Camera>();
            cam.orthographic = true;
            Fit();
        }

        // Recomputes live when the Game view aspect is changed in the editor, and on
        // any runtime resolution change. Does work only when the aspect actually
        // changes, not every frame.
        private void Update()
        {
            if (!Mathf.Approximately(cam.aspect, lastAspect))
                Fit();
        }

        private void Fit()
        {
            cam.orthographicSize = visibleWorldWidth / (2f * cam.aspect);
            lastAspect = cam.aspect;
        }
    }
}