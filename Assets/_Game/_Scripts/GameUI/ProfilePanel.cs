using MobileCore;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>
    /// Avatar picker (GDD 5.8). Selection and name persist through PlayerPrefs,
    /// which the GDD lists as optional — it is a few lines here and makes the
    /// panel behave like a real screen rather than a mock-up.
    ///
    /// Unlike the other overlays this one must be left ACTIVE in the scene: Awake
    /// has to run at boot so the menu's profile button shows the saved avatar
    /// before the panel is ever opened. It hides itself instantly in Start.
    /// </summary>
    public class ProfilePanel : BasePanel
    {
        private const string AvatarKey = "avatarIndex";
        private const string NameKey = "playerName";

        [Header("Content")]
        [SerializeField] private Sprite[] avatars;
        [SerializeField] private Button[] slots;

        [Tooltip("Every Image that shows the chosen avatar — panel preview, menu button.")]
        [SerializeField] private Image[] displays;

        [Tooltip("Hollow ring reparented onto the selected slot — use " +
                 "pixelflow_ui_selection, a filled sprite would hide the avatar.")]
        [SerializeField] private RectTransform selectionMarker;

        [Tooltip("How far the ring extends past the avatar, in pixels.")]
        [SerializeField] private float selectionPadding = 12f;

        [SerializeField] private TMP_InputField nameField;

        private int selectedIndex;

        private void Awake()
        {
            // Slot clicks are wired here rather than in the Inspector: the index
            // is simply the slot's position in the array, so there is nothing to
            // type in and nothing to get out of step.
            if (slots != null)
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    if (slots[i] == null) continue;
                    int index = i;
                    slots[i].onClick.AddListener(() => SelectAvatar(index));
                }
            }

            selectedIndex = Mathf.Clamp(PlayerPrefs.GetInt(AvatarKey, 0), 0, MaxIndex);
            if (nameField != null) nameField.text = PlayerPrefs.GetString(NameKey, "Oyuncu");
            ApplySelection();
        }

        private void OnDestroy()
        {
            if (slots == null) return;
            foreach (Button slot in slots)
                if (slot != null) slot.onClick.RemoveAllListeners();
        }

        private void Start()
        {
            // Immediate hide, not the animated one — the panel must not flash on boot.
            CanvasGroup.alpha = 0f;
            CanvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        private int MaxIndex => Mathf.Max((avatars?.Length ?? 0) - 1, 0);

        /// <summary>Wire each slot button's OnClick here, passing its index.</summary>
        public void SelectAvatar(int index)
        {
            if (avatars == null || avatars.Length == 0) return;

            selectedIndex = Mathf.Clamp(index, 0, MaxIndex);
            ApplySelection();
        }

        public void OnSaveButtonClicked()
        {
            PlayerPrefs.SetInt(AvatarKey, selectedIndex);
            if (nameField != null) PlayerPrefs.SetString(NameKey, nameField.text);
            PlayerPrefs.Save();
            Hide();
        }

        /// <summary>Closing without saving puts the stored choice back.</summary>
        public void OnCloseButtonClicked()
        {
            selectedIndex = Mathf.Clamp(PlayerPrefs.GetInt(AvatarKey, 0), 0, MaxIndex);
            if (nameField != null) nameField.text = PlayerPrefs.GetString(NameKey, "Oyuncu");
            ApplySelection();
            Hide();
        }

        private void ApplySelection()
        {
            if (avatars == null || avatars.Length == 0) return;

            Sprite chosen = avatars[selectedIndex];

            if (displays != null)
            {
                foreach (Image display in displays)
                    if (display != null) display.sprite = chosen;
            }

            if (selectionMarker != null && slots != null &&
                selectedIndex < slots.Length && slots[selectedIndex] != null)
            {
                selectionMarker.SetParent(slots[selectedIndex].transform, false);
                selectionMarker.anchorMin = Vector2.zero;
                selectionMarker.anchorMax = Vector2.one;
                // Sits slightly proud of the slot. It draws over the avatar — a
                // child always does — which is why the sprite is a hollow ring.
                selectionMarker.offsetMin = new Vector2(-selectionPadding, -selectionPadding);
                selectionMarker.offsetMax = new Vector2(selectionPadding, selectionPadding);
                selectionMarker.gameObject.SetActive(true);
            }
        }
    }
}
