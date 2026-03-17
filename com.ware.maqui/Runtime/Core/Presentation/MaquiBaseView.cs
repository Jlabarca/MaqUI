using UnityEngine;
using UnityEngine.UI;

namespace Maqui.Core.Presentation
{
    /// <summary>
    /// Self-contained base view for Maqui — replaces the OneUI BaseView dependency.
    /// Manages CanvasGroup visibility, input blocking, and lifecycle hooks.
    /// Does NOT require a per-view Canvas component (views are parented to layer canvases).
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(RectTransform))]
    public class MaquiBaseView : MonoBehaviour
    {
        public bool InitialVisibility = true;

        private CanvasGroup _canvasGroup;
        private RectTransform _rectTransform;
        private bool _isVisible;

        public bool IsViewVisible => _isVisible;
        public CanvasGroup ViewCanvasGroup => _canvasGroup;
        public RectTransform ViewRectTransform => _rectTransform;

        // ── Unity lifecycle ─────────────────────────────────────────────────

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _rectTransform = GetComponent<RectTransform>();
            OnViewAwake();
        }

        private void Start()
        {
            if (InitialVisibility)
                ShowView();
            else
                HideView();

            OnViewStart();
        }

        private void OnDestroy()
        {
            OnViewDestroy();
        }

        // ── Visibility ──────────────────────────────────────────────────────

        /// <summary>
        /// Makes the view visible and interactive.
        /// </summary>
        public void ShowView()
        {
            _isVisible = true;
            gameObject.SetActive(true);
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;
        }

        /// <summary>
        /// Hides the view and disables input. Does not destroy.
        /// </summary>
        public void HideView()
        {
            _isVisible = false;
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }

        /// <summary>
        /// Toggles between shown and hidden.
        /// </summary>
        public void ToggleView()
        {
            if (_isVisible) HideView();
            else ShowView();
        }

        // ── Virtual hooks ───────────────────────────────────────────────────

        public virtual void OnViewAwake() { }
        public virtual void OnViewStart() { }
        public virtual void OnViewDestroy() { }
    }
}
