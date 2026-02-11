using System;
using YooAsset;

namespace EUFarmworker.Extension.EURes
{
    internal class ResKitPatchOperation : GameAsyncOperation
    {
        private enum ESteps
        {
            None,//尚未启动
            Update,//补丁流程运行态。驱动内部 StateMachine
            Done,//补丁流程完成态。停止状态机更新
        }
        private readonly string _packageName;
        private ESteps _steps = ESteps.None;
        private readonly StateMachine _machine;

        #region 资源状态中Actions调用
        public Action OnInitializePackageFailed;
        #endregion

        public ResKitPatchOperation(string packageName, EPlayMode playMode)
        {
            _packageName = packageName;

            // 创建状态机
            _machine = new StateMachine(this);

            _machine.SetBlackboardValue("PackageName", packageName);
            _machine.SetBlackboardValue("PlayMode", playMode);
        }
        public void SetFinish()
        {

        }
        protected override void OnAbort()
        {

        }

        protected override void OnStart()
        {
            _steps = ESteps.Update;
            _machine.Run<FsmInitializePackage>();
        }

        protected override void OnUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.Update)
            {
                _machine.Update();
            }
        }
    }
}
