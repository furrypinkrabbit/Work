using UnityEngine;
using System.IO;
using System.Xml.Serialization;
using System.Collections.Generic;

public static class XmlSerializerTool
{
    // 保存碰撞点数据到XML
    public static void SaveDetectPointsToXml(List<DetectPointData> dataList, string path)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(List<DetectPointData>));
        using (FileStream fs = new FileStream(path, FileMode.Create))
        {
            serializer.Serialize(fs, dataList);
        }
    }

    // 从XML读取碰撞点数据
    public static List<DetectPointData> LoadDetectPointsFromXml(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError($"XML文件不存在: {path}");
            return new List<DetectPointData>();
        }

        XmlSerializer serializer = new XmlSerializer(typeof(List<DetectPointData>));
        using (FileStream fs = new FileStream(path, FileMode.Open))
        {
            return serializer.Deserialize(fs) as List<DetectPointData>;
        }
    }
}