using TMPro;
using UnityEngine;

namespace MaquiDemos.Frosted
{
    /// <summary>
    /// Single notification row.
    ///
    /// Row prefab layout:
    ///   NotifRow
    ///   ├── TMP_Text Icon  ← _icon  (emoji glyph)
    ///   ├── Vertical group
    ///   │   ├── TMP_Text Title  ← _title (bold)
    ///   │   └── TMP_Text Body   ← _body  (small)
    ///   └── TMP_Text Time       ← _time  (right-aligned, secondary color)
    /// </summary>
    public sealed class NotifRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text _icon;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _body;
        [SerializeField] private TMP_Text _time;

        public void Bind(NotificationEntry entry)
        {
            if (_icon)  _icon.text  = entry.Icon;
            if (_title) _title.text = entry.Title;
            if (_body)  _body.text  = entry.Body;
            if (_time)  _time.text  = entry.Time;
        }
    }
}
