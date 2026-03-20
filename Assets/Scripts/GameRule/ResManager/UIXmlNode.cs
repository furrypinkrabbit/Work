using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

[Serializable]
public class UIXmlNode
{
    public UIXmlNode() { }

    [XmlAttribute("X")]
    public float X { get; set; }
    [XmlAttribute("Y")]
    public float Y { get; set; }
    [XmlAttribute("Width")]
    public float Width;
    [XmlAttribute("Height")]
    public float Height;
    [XmlAttribute("name")]
    public string NodeName { get; set; }
    [XmlArray("Nodes"), XmlArrayItem("UINode")]
    public List<UIXmlNode> nodes { get; set; } = new List<UIXmlNode>();

    // 简化根节点判断
    public bool IsShow => string.Equals(NodeName, "show", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 递归查找当前节点及子节点中的show根节点
    /// </summary>
    public UIXmlNode GetShow()
    {
        if (IsShow) return this;

        foreach (var child in nodes)
        {
            var showNode = child.GetShow();
            if (showNode != null) return showNode;
        }

        return null;
    }

    /// <summary>
    /// XML配置根节点（修正序列化特性，避免结构冲突）
    /// </summary>
    [XmlRoot("UIConfig")] // 更语义化的根节点名称
    public class UIXmlConfig
    {
        [XmlArray("Nodes"), XmlArrayItem("Node")] // 统一节点数组命名
        public List<UIXmlNode> Nodes = new List<UIXmlNode>();
    }
}