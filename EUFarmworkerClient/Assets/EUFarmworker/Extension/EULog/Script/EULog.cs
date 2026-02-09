using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace EUFarmworker
{
    /// <summary>
    /// 高性能日志工具类
    /// 使用 [Conditional("ENABLE_LOG")] 属性控制日志编译
    /// 当未定义 ENABLE_LOG 宏时，所有对此类的调用（包括参数计算和字符串拼接）都会在编译阶段被移除
    /// </summary>
    public static class EULog
    {
        [Conditional("UNITY_EDITOR"),Conditional("DEVELOPMENT_BUILD")]
        public static void Log(object message)
        {
            Debug.Log(message);
        }

        [Conditional("UNITY_EDITOR"),Conditional("DEVELOPMENT_BUILD")]
        public static void Log(object message, Object context)
        {
            Debug.Log(message, context);
        }

        [Conditional("UNITY_EDITOR"),Conditional("DEVELOPMENT_BUILD")]
        public static void LogFormat(string format, params object[] args)
        {
            Debug.LogFormat(format, args);
        }

        [Conditional("UNITY_EDITOR"),Conditional("DEVELOPMENT_BUILD")]
        public static void LogFormat(Object context, string format, params object[] args)
        {
            Debug.LogFormat(context, format, args);
        }

        [Conditional("UNITY_EDITOR"),Conditional("DEVELOPMENT_BUILD")]
        public static void LogWarning(object message)
        {
            Debug.LogWarning(message);
        }

        [Conditional("UNITY_EDITOR"),Conditional("DEVELOPMENT_BUILD")]
        public static void LogWarning(object message, Object context)
        {
            Debug.LogWarning(message, context);
        }

        [Conditional("UNITY_EDITOR"),Conditional("DEVELOPMENT_BUILD")]
        public static void LogWarningFormat(string format, params object[] args)
        {
            Debug.LogWarningFormat(format, args);
        }

        [Conditional("UNITY_EDITOR"),Conditional("DEVELOPMENT_BUILD")]
        public static void LogWarningFormat(Object context, string format, params object[] args)
        {
            Debug.LogWarningFormat(context, format, args);
        }

        [Conditional("UNITY_EDITOR"),Conditional("DEVELOPMENT_BUILD")]
        public static void LogError(object message)
        {
            Debug.LogError(message);
        }

        [Conditional("UNITY_EDITOR"),Conditional("DEVELOPMENT_BUILD")]
        public static void LogError(object message, Object context)
        {
            Debug.LogError(message, context);
        }

        [Conditional("UNITY_EDITOR"),Conditional("DEVELOPMENT_BUILD")]
        public static void LogErrorFormat(string format, params object[] args)
        {
            Debug.LogErrorFormat(format, args);
        }

        [Conditional("UNITY_EDITOR"),Conditional("DEVELOPMENT_BUILD")]
        public static void LogErrorFormat(Object context, string format, params object[] args)
        {
            Debug.LogErrorFormat(context, format, args);
        }
        
        [Conditional("UNITY_EDITOR"),Conditional("DEVELOPMENT_BUILD")]
        public static void LogException(System.Exception exception)
        {
            Debug.LogException(exception);
        }
        
        [Conditional("UNITY_EDITOR"),Conditional("DEVELOPMENT_BUILD")]
        public static void LogException(System.Exception exception, Object context)
        {
            Debug.LogException(exception, context);
        }
    }
}
