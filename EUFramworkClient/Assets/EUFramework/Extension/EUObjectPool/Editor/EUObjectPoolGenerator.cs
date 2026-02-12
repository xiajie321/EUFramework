using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace EUFramework.Extension.EUObjectPoolKit.Editor
{
    public class EUObjectPoolGenerator
    {
        [MenuItem("EUFramework/生成/EU对象池注册信息")]
        public static void Generate()
        {
            // Find all classes with EUObjectPoolAttribute
            var types = TypeCache.GetTypesWithAttribute<EUObjectPoolAttribute>();
            
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("using System.Collections;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using UnityEngine;");
            
            // Collect namespaces
            HashSet<string> namespaces = new HashSet<string>();
            foreach (var type in types)
            {
                if (type.IsAbstract || type.IsGenericType) continue;
                if (!string.IsNullOrEmpty(type.Namespace) && type.Namespace != "EUFramework.Extension.EUObjectPool")
                {
                    namespaces.Add(type.Namespace);
                }
            }
            
            foreach (var ns in namespaces)
            {
                sb.AppendLine($"using {ns};");
            }

            sb.AppendLine();
            sb.AppendLine("namespace EUFramework.Extension.EUObjectPool");
            sb.AppendLine("{");
            sb.AppendLine("    public static class EUObjectPoolManager");
            sb.AppendLine("    {");
            
            foreach (var type in types)
            {
                if (type.IsAbstract || type.IsGenericType) continue;

                // Ensure the type has a parameterless constructor
                if (type.GetConstructor(Type.EmptyTypes) == null)
                {
                    Debug.LogError($"Type {type.FullName} does not have a parameterless constructor.");
                    continue;
                }

                // Use the type name for the field name
                sb.AppendLine($"        public static readonly {type.Name} {type.Name} = new {type.Name}();");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            // Write to file
            string[] guids = AssetDatabase.FindAssets("EUObjectPoolManager");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                // Ensure we are writing to the correct file if multiple exist, but usually it's unique enough
                File.WriteAllText(path, sb.ToString());
                AssetDatabase.Refresh();
                Debug.Log($"EUObjectPoolManager generated at {path}");
            }
            else
            {
                Debug.LogError("Could not find EUObjectPoolManager.cs");
            }
        }
    }
}
