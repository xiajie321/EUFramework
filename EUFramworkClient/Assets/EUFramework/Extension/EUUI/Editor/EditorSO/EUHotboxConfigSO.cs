using System;
using System.Collections.Generic;
using UnityEngine;

namespace EUFramework.Extension.EUUI.Editor
{
    /// <summary>
    /// Hotbox 功能编排配置（ScriptableObject）。
    /// 存储所有区域及其条目 ID，由功能编排面板负责管理。
    /// 按 Space 键弹出的快捷层直接读取此配置渲染。
    /// </summary>
    [CreateAssetMenu(
        fileName = "EUHotboxConfig",
        menuName  = "EUFramework/EUUI/Hotbox 功能编排配置",
        order     = 10)]
    public class EUHotboxConfigSO : ScriptableObject
    {
        public List<HotboxZone> zones = new List<HotboxZone>();
    }

    [Serializable]
    public class HotboxZone
    {
        public string zoneName = "新区域";
        public List<HotboxZoneEntry> entries = new List<HotboxZoneEntry>();
    }

    [Serializable]
    public class HotboxZoneEntry
    {
        /// <summary>
        /// 唯一标识：<br/>
        /// • 静态方法   → "TypeFullName::MethodName"<br/>
        /// • IEUHotboxAction → "TypeFullName"
        /// </summary>
        public string entryId;

        /// <summary>
        /// 自定义显示标签，留空则使用 [EUHotboxEntry].Label 或 IEUHotboxAction.Label
        /// </summary>
        public string labelOverride;
    }
}
