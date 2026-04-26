#if UNITY_EDITOR
using UnityEngine.UIElements;

namespace EUFramework.Extension.EUUI.Editor
{
    /// <summary>
    /// EUUI editor window panel contract.
    /// Kept separate from the runtime IEUUIPanel to avoid lifecycle naming ambiguity.
    /// </summary>
    internal interface IEUUIEditorPanel
    {
        void Build(VisualElement contentArea);
    }
}
#endif
