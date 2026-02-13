using UnityEngine;
using System.Collections.Generic;

namespace Framework
{
    public enum UINodeBindType
    {
        RectTransform,
        Image,
        Text,
        Button,
    }

    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public  class UINodeBind : MonoBehaviour
    {
        [Tooltip("生成的组件类型。若为空，则默认生成 RectTransform")]
        public UINodeBindType ComponentType;

        [Tooltip("生成的变量名。若为空，则默认使用 GameObject 的名称")]
        public string MemberName;

        public string GetFinalMemberName()
        {
            return string.IsNullOrEmpty(MemberName) ? gameObject.name : MemberName;
        }

        public UINodeBindType GetFinalComponentType()
        {
            return ComponentType;
        }
    }
}