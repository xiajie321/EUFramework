//------------------------------------------------------------------------------
// 业务逻辑部分 - 仅在初始创建时生成，请在此编写你的 UI 逻辑
//------------------------------------------------------------------------------
using UnityEngine;
using Cysharp.Threading.Tasks;
using EUFramework.Extension.EUUI;
using EUFramework.Core.MVC.Interface;

namespace Game.UI
{
    public partial class WndTestPanel : EUUIPanelBase<WndTestPanel>
    {
        public override string PackageName => "Test";
        public override string PanelName => "WndTestPanel";

        public override bool OnCanOpen() => true;
        
        #region 业务
        protected override void OnOpen()
        {
            RegisterUIEvent();
            RegisterEvent();
        }

        protected override void OnShow()
        {
        }

        protected override void OnHide()
        {
        }

        protected override void OnClose()
        {
            UnRegisterUIEvent();
            UnRegisterEvent();
        }
        #endregion

        #region 事件
        private void RegisterUIEvent()
        {
        }

        private void RegisterEvent()
        {
        }

        private void UnRegisterUIEvent()
        {
        }

        private void UnRegisterEvent()
        {
        }
        #endregion
    }
}
