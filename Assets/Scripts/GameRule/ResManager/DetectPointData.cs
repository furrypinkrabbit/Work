using UnityEngine;
using System.Xml.Serialization;

[System.Serializable]
public class DetectPointData
{
    [XmlAttribute("name")]
    public string name; // 物品名称
    [XmlAttribute("posX")]
    public float posX;
    [XmlAttribute("posY")]
    public float posY;
    [XmlAttribute("posZ")]
    public float posZ;
    [XmlAttribute("rotX")]
    public float rotX;
    [XmlAttribute("rotY")]
    public float rotY;
    [XmlAttribute("rotZ")]
    public float rotZ;
    [XmlAttribute("scaleX")]
    public float scaleX;
    [XmlAttribute("scaleY")]
    public float scaleY;
    [XmlAttribute("scaleZ")]
    public float scaleZ;
    [XmlAttribute("callback")]
    public string callbackFunction; // 回调函数名
    [XmlAttribute("type")]
    public DetectPointType type; // 类型（Cube/Sphere）

    // 无参构造（XML序列化需要）
    public DetectPointData() { }

    // 从Transform构建数据
    public DetectPointData(Transform trans, string callback, DetectPointType type)
    {
        name = trans.name;
        posX = trans.position.x;
        posY = trans.position.y;
        posZ = trans.position.z;
        rotX = trans.eulerAngles.x;
        rotY = trans.eulerAngles.y;
        rotZ = trans.eulerAngles.z;
        scaleX = trans.localScale.x;
        scaleY = trans.localScale.y;
        scaleZ = trans.localScale.z;
        callbackFunction = callback;
        this.type = type;
    }

    // 转换为Transform数据
    public void ApplyToTransform(Transform trans)
    {
        trans.position = new Vector3(posX, posY, posZ);
        trans.rotation = Quaternion.Euler(rotX, rotY, rotZ);
        trans.localScale = new Vector3(scaleX, scaleY, scaleZ);
    }
}

public enum DetectPointType
{
    Cube,
    Sphere
}