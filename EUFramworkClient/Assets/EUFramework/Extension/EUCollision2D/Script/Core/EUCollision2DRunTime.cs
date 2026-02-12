using EUFarmworker.Extension.EUCollision2DKit.Core;
using UnityEngine;

namespace EUFramwork.Extension.EUCollision2DKit.Core
{
    /// <summary>
    /// 碰撞系统的运行时驱动器。
    /// 该类继承自 MonoBehaviour，由 EUCollision2DCore 自动创建并常驻场景中。
    /// 它的唯一职责是监听 Unity 的生命周期事件，并将其转发给核心管理类。
    /// </summary>
    public sealed class EUCollision2DRunTime : MonoBehaviour
    {
        private void Update()
        {
            EUCollision2DCore.Update();
        }

        private void FixedUpdate()
        {
            EUCollision2DCore.FixedUpdate();
        }

        private void LateUpdate()
        {
            EUCollision2DCore.LateUpdate();
        }

        private void OnDestroy()
        {
            EUCollision2DCore.OnDestroy();
        }
    }
}
