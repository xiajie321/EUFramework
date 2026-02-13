using System;
using UnityEngine;
using UnityEngine.UI;

namespace EUFramework.Extension.EURes
{
    /// <summary>
    /// EUResKit 用户操作弹窗组件
    /// 用户可自定义修改此脚本以实现特定的 UI 交互逻辑
    /// </summary>
    public class EUResKitUserOpePopUp : MonoBehaviour
    {
        [Header("UI 引用")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text contentText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        
        private Action _onConfirm;
        private Action _onCancel;
        
        private void Awake()
        {
            // 自动查找组件（如果没有手动赋值）
            if (titleText == null)
                titleText = transform.Find("Panel/Title")?.GetComponent<Text>();
            
            if (contentText == null)
                contentText = transform.Find("Panel/Content")?.GetComponent<Text>();
            
            if (confirmButton == null)
                confirmButton = transform.Find("Panel/BtnConfirm")?.GetComponent<Button>();
            
            if (cancelButton == null)
                cancelButton = transform.Find("Panel/BtnCancel")?.GetComponent<Button>();
            
            // 绑定按钮事件
            if (confirmButton != null)
                confirmButton.onClick.AddListener(OnConfirmClicked);
            
            if (cancelButton != null)
                cancelButton.onClick.AddListener(OnCancelClicked);
        }
        
        /// <summary>
        /// 显示弹窗
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="content">内容</param>
        /// <param name="onConfirm">确认回调</param>
        /// <param name="onCancel">取消回调</param>
        public void Show(string title, string content, Action onConfirm, Action onCancel)
        {
            if (titleText != null)
                titleText.text = title;
            
            if (contentText != null)
                contentText.text = content;
            
            _onConfirm = onConfirm;
            _onCancel = onCancel;
            
            gameObject.SetActive(true);
        }
        
        /// <summary>
        /// 隐藏弹窗
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }
        
        /// <summary>
        /// 确认按钮点击事件
        /// </summary>
        private void OnConfirmClicked()
        {
            _onConfirm?.Invoke();
            Hide();
        }
        
        /// <summary>
        /// 取消按钮点击事件
        /// </summary>
        private void OnCancelClicked()
        {
            _onCancel?.Invoke();
            Hide();
        }
        
        private void OnDestroy()
        {
            // 清理事件监听
            if (confirmButton != null)
                confirmButton.onClick.RemoveListener(OnConfirmClicked);
            
            if (cancelButton != null)
                cancelButton.onClick.RemoveListener(OnCancelClicked);
        }
    }
}
