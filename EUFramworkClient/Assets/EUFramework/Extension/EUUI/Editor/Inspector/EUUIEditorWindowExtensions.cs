#if UNITY_EDITOR
using UnityEditor;

namespace EUFramework.Extension.EUUI.Editor
{
    internal static class EUUIEditorWindowExtensions
    {
        public static void CenterOnMainWin(this EditorWindow window)
        {
            var main = EditorGUIUtility.GetMainWindowPosition();
            var pos = window.position;
            pos.x = main.x + (main.width - pos.width) * 0.5f;
            pos.y = main.y + (main.height - pos.height) * 0.5f;
            window.position = pos;
        }
    }
}
#endif
