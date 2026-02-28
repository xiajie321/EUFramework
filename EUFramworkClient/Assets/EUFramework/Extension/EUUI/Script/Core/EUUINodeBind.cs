#if UNITY_EDITOR
using UnityEngine;

namespace EUFramework.Extension.EUUI
{
    /// <summary>
    /// 节点绑定组件类型（仅编辑期使用）
    /// </summary>
    public enum EUUINodeBindType
    {
        RectTransform,
        Image,
        Text,
        Button,
        TextMeshProUGUI,
    }

    /// <summary>
    /// EUUI 节点绑定（仅编辑期使用，导出 Prefab 时会移除）
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class EUUINodeBind : MonoBehaviour
    {
        [Tooltip("生成的组件类型。若为空，则默认生成 RectTransform")]
        public EUUINodeBindType ComponentType;

        [Tooltip("生成的变量名。若为空，则默认使用 GameObject 的名称")]
        public string MemberName;

        public string GetFinalMemberName()
        {
            return string.IsNullOrEmpty(MemberName) ? gameObject.name : MemberName;
        }

        public EUUINodeBindType GetFinalComponentType()
        {
            return ComponentType;
        }
    }
}
#endif
