using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using UnityEditor;
using UnityEngine;

namespace NodeConfigTool
{
    /// <summary>
    /// XML配置序列化与反序列化工具
    /// </summary>
    public static class XMLConfigSerializer
    {
        // 配置保存的默认路径，自动创建
        public static readonly string DefaultSavePath = "Assets/Res/Assets/Config";

        #region XML 序列化专用数据结构
        [XmlRoot("NodeConfig")]
        public class XMLNodeConfig
        {
            [XmlArray("Nodes"), XmlArrayItem("Node")]
            public List<XMLNode> Nodes { get; set; } = new List<XMLNode>();
        }

        public class XMLNode
        {
            [XmlAttribute("id")]
            public string Id { get; set; }

            [XmlAttribute("name")]
            public string Name { get; set; }

            [XmlAttribute("x")]
            public float X { get; set; }

            [XmlAttribute("y")]
            public float Y { get; set; }

            [XmlArray("Properties"), XmlArrayItem("Property")]
            public List<XMLNodeProperty> Properties { get; set; } = new List<XMLNodeProperty>();

            [XmlArray("NextNodes"), XmlArrayItem("NextId")]
            public List<string> NextNodeIds { get; set; } = new List<string>();
        }

        public class XMLNodeProperty
        {
            [XmlAttribute("type")]
            public string Type { get; set; }

            [XmlAttribute("value")]
            public string Value { get; set; }
        }
        #endregion

        #region 保存配置到XML
        /// <summary>
        /// 保存节点配置到XML文件
        /// </summary>
        public static string SaveConfigToXML(NodeConfigData configData, string fileName)
        {
            // 1. 验证根节点
            var rootNode = configData.GetRootNode();
            if (rootNode == null)
            {
                EditorUtility.DisplayDialog("保存失败", "配置中不存在根节点（root）", "确定");
                return null;
            }

            // 2. 自动创建目录
            if (!Directory.Exists(DefaultSavePath))
            {
                Directory.CreateDirectory(DefaultSavePath);
                AssetDatabase.Refresh();
            }

            // 3. 生成ID映射（处理自定义ID和自动分配ID，检查重复）
            var idMap = GenerateNodeIdMap(configData, out var errorMsg);
            if (!string.IsNullOrEmpty(errorMsg))
            {
                EditorUtility.DisplayDialog("保存失败", errorMsg, "确定");
                return null;
            }

            // 4. 转换为XML结构
            var xmlConfig = ConvertToXMLConfig(configData, idMap);

            // 5. 序列化写入文件
            string fullPath = Path.Combine(DefaultSavePath, $"{fileName}.xml");
            try
            {
                var serializer = new XmlSerializer(typeof(XMLNodeConfig));
                using (var writer = new StreamWriter(fullPath, false, System.Text.Encoding.UTF8))
                {
                    serializer.Serialize(writer, xmlConfig);
                }
                AssetDatabase.Refresh();
                return fullPath;
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("保存失败", $"XML序列化出错：{e.Message}", "确定");
                return null;
            }
        }

        /// <summary>
        /// 生成GUID到最终ID的映射，严格遵循ID分配规则
        /// </summary>
        private static Dictionary<string, string> GenerateNodeIdMap(NodeConfigData configData, out string errorMsg)
        {
            errorMsg = string.Empty;
            var idMap = new Dictionary<string, string>();
            var usedIds = new HashSet<string>();
            var rootNode = configData.GetRootNode();

            // 根节点固定ID为0
            idMap[rootNode.Guid] = "0";
            usedIds.Add("0");

            // 第一步：收集所有自定义ID，检查重复
            foreach (var node in configData.Nodes)
            {
                if (node.IsRoot) continue;

                var idProperty = node.Properties.Find(p => p.PropertyType == NodePropertyType.id);
                if (idProperty != null && !string.IsNullOrEmpty(idProperty.Value))
                {
                    if (usedIds.Contains(idProperty.Value))
                    {
                        errorMsg = $"ID重复：节点【{node.NodeName}】的自定义ID【{idProperty.Value}】已被使用";
                        return null;
                    }
                    usedIds.Add(idProperty.Value);
                    idMap[node.Guid] = idProperty.Value;
                }
            }

            // 第二步：广度优先遍历，同级按X坐标从左到右排序，分配自动ID
            int autoId = 1;
            var queue = new Queue<NodeData>();
            queue.Enqueue(rootNode);

            while (queue.Count > 0)
            {
                var currentNode = queue.Dequeue();
                // 子节点按X坐标升序排序
                var childNodes = currentNode.NextNodeGuids
                    .Select(guid => configData.Nodes.Find(n => n.Guid == guid))
                    .Where(n => n != null)
                    .OrderBy(n => n.Position.x)
                    .ToList();

                foreach (var child in childNodes)
                {
                    // 已有自定义ID的跳过，只加入遍历队列
                    if (idMap.ContainsKey(child.Guid))
                    {
                        queue.Enqueue(child);
                        continue;
                    }

                    // 分配不重复的自动ID
                    while (usedIds.Contains(autoId.ToString()))
                    {
                        autoId++;
                    }
                    string newId = autoId.ToString();
                    idMap[child.Guid] = newId;
                    usedIds.Add(newId);
                    autoId++;

                    queue.Enqueue(child);
                }
            }

            // 第三步：处理孤立节点（不在根节点树里的节点），按X坐标排序分配ID
            var isolatedNodes = configData.Nodes
                .Where(n => !idMap.ContainsKey(n.Guid))
                .OrderBy(n => n.Position.x)
                .ToList();

            foreach (var node in isolatedNodes)
            {
                while (usedIds.Contains(autoId.ToString()))
                {
                    autoId++;
                }
                string newId = autoId.ToString();
                idMap[node.Guid] = newId;
                usedIds.Add(newId);
                autoId++;
            }

            return idMap;
        }

        /// <summary>
        /// 内部数据转换为XML序列化结构
        /// </summary>
        private static XMLNodeConfig ConvertToXMLConfig(NodeConfigData configData, Dictionary<string, string> idMap)
        {
            var xmlConfig = new XMLNodeConfig();

            foreach (var node in configData.Nodes)
            {
                var xmlNode = new XMLNode
                {
                    Id = idMap[node.Guid],
                    Name = node.NodeName,
                    X = node.Position.x,
                    Y = node.Position.y
                };

                // 转换所有属性（包括id属性，方便回读还原）
                foreach (var prop in node.Properties)
                {
                    xmlNode.Properties.Add(new XMLNodeProperty
                    {
                        Type = prop.PropertyType.ToString(),
                        Value = prop.Value
                    });
                }

                // 转换连线关系
                foreach (var nextGuid in node.NextNodeGuids)
                {
                    if (idMap.TryGetValue(nextGuid, out var nextId))
                    {
                        xmlNode.NextNodeIds.Add(nextId);
                    }
                }

                xmlConfig.Nodes.Add(xmlNode);
            }

            return xmlConfig;
        }
        #endregion

        #region 从XML加载配置
        /// <summary>
        /// 从XML文件加载配置，还原为节点数据
        /// </summary>
        public static NodeConfigData LoadConfigFromXML(string filePath)
        {
            if (!File.Exists(filePath))
            {
                EditorUtility.DisplayDialog("加载失败", "文件不存在", "确定");
                return null;
            }

            try
            {
                var serializer = new XmlSerializer(typeof(XMLNodeConfig));
                using (var reader = new StreamReader(filePath, System.Text.Encoding.UTF8))
                {
                    var xmlConfig = (XMLNodeConfig)serializer.Deserialize(reader);
                    return ConvertFromXMLConfig(xmlConfig);
                }
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("加载失败", $"XML解析出错：{e.Message}", "确定");
                return null;
            }
        }

        /// <summary>
        /// XML结构转换为内部节点数据
        /// </summary>
        private static NodeConfigData ConvertFromXMLConfig(XMLNodeConfig xmlConfig)
        {
            var configData = new NodeConfigData();
            var idToGuidMap = new Dictionary<string, string>(); // XML的ID对应内部GUID

            // 第一步：创建所有节点，生成GUID，建立ID映射
            foreach (var xmlNode in xmlConfig.Nodes)
            {
                var node = new NodeData
                {
                    NodeName = xmlNode.Name,
                    Position = new Vector2(xmlNode.X, xmlNode.Y)
                };

                // 还原属性
                foreach (var xmlProp in xmlNode.Properties)
                {
                    if (Enum.TryParse<NodePropertyType>(xmlProp.Type, out var propType))
                    {
                        node.Properties.Add(new NodeProperty(propType)
                        {
                            Value = xmlProp.Value
                        });
                    }
                }

                configData.Nodes.Add(node);
                idToGuidMap[xmlNode.Id] = node.Guid;
            }

            // 第二步：还原节点之间的连线
            for (int i = 0; i < xmlConfig.Nodes.Count; i++)
            {
                var xmlNode = xmlConfig.Nodes[i];
                var node = configData.Nodes[i];

                foreach (var nextId in xmlNode.NextNodeIds)
                {
                    if (idToGuidMap.TryGetValue(nextId, out var nextGuid))
                    {
                        node.NextNodeGuids.Add(nextGuid);
                    }
                }
            }

            return configData;
        }
        #endregion
    }
}