using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace EUFramework.Extension.EUUI
{
    /// <summary>
    /// EUUI 弹窗面板基类（参考 Doc/UIPopupPanelBase.cs.txt）
    /// </summary>
    public abstract class EUUIPopupPanelBase<T> : EUUIPanelBase<T>
        where T : EUUIPanelBase<T>, new()
    {
        public override EUUILayerEnum DefaultLayer => EUUILayerEnum.Popup;

        protected virtual bool EnableMask => true;
        protected virtual Color MaskColor => new Color(0, 0, 0, 0.7f);

        private GameObject _maskObject;

        public override void Show()
        {
            CreateMask();
            base.Show();
        }

        private void CreateMask()
        {
            if (_maskObject != null) return;

            _maskObject = new GameObject("PopupMask", typeof(RectTransform), typeof(Image));
            _maskObject.transform.SetParent(transform, false);
            _maskObject.transform.SetAsFirstSibling();

            var maskRect = _maskObject.GetComponent<RectTransform>();
            maskRect.anchorMin = Vector2.zero;
            maskRect.anchorMax = Vector2.one;
            maskRect.offsetMin = Vector2.zero;
            maskRect.offsetMax = Vector2.zero;

            var maskImage = _maskObject.GetComponent<Image>();
            maskImage.color = MaskColor;
            maskImage.raycastTarget = true;

            var maskButton = _maskObject.AddComponent<Button>();
            maskButton.transition = Selectable.Transition.None;
            maskButton.onClick.AddListener(OnMaskClick);
        }

        protected virtual void OnMaskClick()
        {
            if (!EnableMask) return;
            CloseSelf();
        }

        /// <summary>点击遮罩关闭时调用，可重写以接入 UIKit.CloseAsync 等</summary>
        protected virtual void CloseSelf()
        {
            Close();
        }
    }
}
