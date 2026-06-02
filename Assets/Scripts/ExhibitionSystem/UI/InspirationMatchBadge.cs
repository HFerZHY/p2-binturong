using ExhibitionSystem.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ExhibitionSystem.UI
{
    /// <summary>
    /// Shows the exhibit icon associated with a known or currently assigned inspiration.
    /// </summary>
    public class InspirationMatchBadge : MonoBehaviour
    {
        [SerializeField] private Image _badgeIcon;

        public void SetData(ExhibitItemData matchedItem)
        {
            bool hasMatch = matchedItem != null && matchedItem.icon != null;
            gameObject.SetActive(hasMatch);

            if (_badgeIcon == null)
                return;

            _badgeIcon.sprite = hasMatch ? matchedItem.icon : null;
            _badgeIcon.enabled = hasMatch;
            _badgeIcon.preserveAspect = true;
            _badgeIcon.raycastTarget = false;
        }
    }
}
