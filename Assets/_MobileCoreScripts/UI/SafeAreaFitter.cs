using UnityEngine;

namespace MobileCore
{
    /// <summary>
    /// Shrinks a RectTransform to the device's safe area, keeping content clear of
    /// the notch / Dynamic Island and the home gesture bar. Put it on a full-screen
    /// child of the Canvas and parent the edge-hugging UI to that.
    ///
    /// Re-applies when the resolution or orientation changes, which also covers the
    /// Simulator switching device profiles in the Editor.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class SafeAreaFitter : MonoBehaviour
    {
        [Tooltip("Leave off for elements that should still reach the screen edge.")]
        [SerializeField] private bool applyTop = true;
        [SerializeField] private bool applyBottom = true;
        [SerializeField] private bool applyLeft = true;
        [SerializeField] private bool applyRight = true;

        private RectTransform rect;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;
        private int lastEdgeMask = -1;

        private int EdgeMask => (applyTop ? 1 : 0) | (applyBottom ? 2 : 0)
                              | (applyLeft ? 4 : 0) | (applyRight ? 8 : 0);

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
        }

        // Deliberately not in Awake: writing anchors raises
        // OnRectTransformDimensionsChange, and Unity forbids that SendMessage
        // during Awake / OnValidate. Start still lands before the first frame.
        private void Start()
        {
            Apply();
        }

        private void Update()
        {
            // Also catches the edge toggles being flipped in the Inspector while
            // playing, which is why OnValidate isn't used (it cannot write anchors).
            if (Screen.safeArea == lastSafeArea && EdgeMask == lastEdgeMask &&
                Screen.width == lastScreenSize.x && Screen.height == lastScreenSize.y)
                return;

            Apply();
        }

        private void Apply()
        {
            if (rect == null) rect = GetComponent<RectTransform>();
            if (Screen.width <= 0 || Screen.height <= 0) return;

            lastSafeArea = Screen.safeArea;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            lastEdgeMask = EdgeMask;

            Vector2 min = lastSafeArea.position;
            Vector2 max = lastSafeArea.position + lastSafeArea.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;

            // A disabled edge falls back to the full screen on that side.
            rect.anchorMin = new Vector2(applyLeft ? min.x : 0f, applyBottom ? min.y : 0f);
            rect.anchorMax = new Vector2(applyRight ? max.x : 1f, applyTop ? max.y : 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
