using System;
using UnityEngine;

namespace EUFramework.Extension.EURes
{
    /// <summary>
    /// 服务器协议类型
    /// </summary>
    public enum ServerProtocol
    {
        HTTP,
        HTTPS,
        Custom  // 自定义完整URL
    }

    /// <summary>
    /// 资源服务器配置
    /// </summary>
    [CreateAssetMenu(fileName = "EUResServerConfig", menuName = "YooAsset/Create EUResServer Config", order = 0)]
    public class EUResServerConfig : ScriptableObject
    {
        [Header("服务器配置")]
        [Tooltip("协议类型")]
        public ServerProtocol protocol = ServerProtocol.HTTP;

        [Tooltip("主机服务器地址（IP或域名）")]
        public string hostServer = "127.0.0.1";

        [Tooltip("服务器端口号")]
        [Range(1, 65535)]
        public int port = 80;

        [Tooltip("自定义完整URL（仅在协议类型为Custom时使用）")]
        public string customUrl = "";

        [Header("版本信息")]
        [Tooltip("应用版本号")]
        public string appVersion = "1.0.0";

        /// <summary>
        /// 获取完整的服务器地址
        /// </summary>
        public string GetServerUrl()
        {
            // 如果使用自定义URL，直接返回
            if (protocol == ServerProtocol.Custom)
            {
                return customUrl;
            }

            // 根据协议类型构建URL
            string protocolPrefix = protocol == ServerProtocol.HTTPS ? "https" : "http";
            return $"{protocolPrefix}://{hostServer}:{port}";
        }

        /// <summary>
        /// 获取完整的服务器地址（带自定义端口）
        /// </summary>
        public string GetServerUrl(int customPort)
        {
            // 如果使用自定义URL，直接返回
            if (protocol == ServerProtocol.Custom)
            {
                return customUrl;
            }

            // 根据协议类型构建URL
            string protocolPrefix = protocol == ServerProtocol.HTTPS ? "https" : "http";
            return $"{protocolPrefix}://{hostServer}:{customPort}";
        }

        /// <summary>
        /// 验证配置是否有效
        /// </summary>
        public bool IsValid()
        {
            // 如果使用自定义URL，检查自定义URL
            if (protocol == ServerProtocol.Custom)
            {
                if (string.IsNullOrEmpty(customUrl))
                    return false;
            }
            else
            {
                // 否则检查主机地址
                if (string.IsNullOrEmpty(hostServer))
                    return false;
            }
            
            if (string.IsNullOrEmpty(appVersion))
                return false;

            return true;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 如果使用自定义URL模式
            if (protocol == ServerProtocol.Custom)
            {
                if (!string.IsNullOrEmpty(customUrl))
                {
                    // 验证URL是否包含协议
                    if (!customUrl.StartsWith("http://") && !customUrl.StartsWith("https://"))
                    {
                        Debug.LogWarning($"[EUResServerConfig] 自定义URL建议包含协议前缀 (http:// 或 https://): {customUrl}");
                    }
                }
            }
            else
            {
                // 验证主机地址格式
                if (!string.IsNullOrEmpty(hostServer))
                {
                    // 检查是否为IP地址
                    var parts = hostServer.Split('.');
                    if (parts.Length == 4)
                    {
                        // 可能是IP地址，验证每部分是否为数字
                        bool isValidIP = true;
                        foreach (var part in parts)
                        {
                            if (!int.TryParse(part, out int num) || num < 0 || num > 255)
                            {
                                isValidIP = false;
                                break;
                            }
                        }
                        if (!isValidIP)
                        {
                            Debug.LogWarning($"[EUResServerConfig] IP地址格式可能不正确: {hostServer}");
                        }
                    }
                    // 否则假设是域名，域名格式较为灵活，这里不做严格验证
                }

                // 验证端口号
                if (port < 1 || port > 65535)
                {
                    Debug.LogWarning($"[EUResServerConfig] 端口号必须在 1-65535 之间: {port}");
                    port = Mathf.Clamp(port, 1, 65535);
                }
            }

            // 验证版本号格式
            if (!string.IsNullOrEmpty(appVersion))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(appVersion, @"^\d+\.\d+\.\d+"))
                {
                    Debug.LogWarning($"[EUResServerConfig] 版本号格式建议使用 x.x.x 格式: {appVersion}");
                }
            }
        }
#endif
    }
}
