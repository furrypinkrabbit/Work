using System;
using System.Collections.Generic;
using UnityEngine;

namespace NodeConfigTool
{
    /// <summary>
    /// 节点属性类型枚举
    /// </summary>
    public enum NodePropertyType
    {
        场景,
        关卡,
        id,
        优先级
    }

    /// <summary>
    /// 节点属性数据
    /// </summary>
    [Serializable]
    public class NodeProperty
    {
        public NodePropertyType PropertyType;
        public string Value;

        public NodeProperty(NodePropertyType type)
        {
            PropertyType = type;
            Value = string.Empty;
        }
    }

    /// <summary>
    /// 节点核心数据
    /// </summary>
    [Serializable]
    public class NodeData
    {
        public string Guid; // 内部唯一标识，不暴露给用户
        public string NodeName;
        public Vector2 Position;
        public List<NodeProperty> Properties = new List<NodeProperty>();
        public List<string> NextNodeGuids = new List<string>(); // 输出的下一个节点GUID列表

        // 是否是根节点
        public bool IsRoot => NodeName == "root";

        public NodeData()
        {
            Guid = System.Guid.NewGuid().ToString();
        }
    }

    /// <summary>
    /// 整个配置的根数据
    /// </summary>
    [Serializable]
    public class NodeConfigData
    {
        public List<NodeData> Nodes = new List<NodeData>();

        // 获取根节点
        public NodeData GetRootNode()
        {
            return Nodes.Find(n => n.IsRoot);
        }
    }
}