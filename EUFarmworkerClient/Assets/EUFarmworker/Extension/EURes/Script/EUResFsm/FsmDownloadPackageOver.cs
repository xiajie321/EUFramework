using Cysharp.Threading.Tasks;
using YooAsset;

namespace EUFramework.Extension.EURes
{
    internal class FsmDownloadPackageOver : IStateNode
    {
        private StateMachine _machine;

        public void OnCreate(StateMachine machine)
        {
            _machine = machine;
        }

        public void OnEnter()
        {
            // 下载完成，进入启动游戏状态
              _machine.ChangeState<FsmClearCacheBundle>();
        }

        public void OnUpdate()
        {
            
        }

        public void OnExit()
        {
            
        }
    }
}
