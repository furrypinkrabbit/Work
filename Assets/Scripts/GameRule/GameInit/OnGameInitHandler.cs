using System;

namespace GameRule.GameInit
{
    /// <summary>
    /// 标记需要在游戏初始化时执行的处理器类
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class OnGameInitHandler : Attribute
    {
        // 特性类无需额外逻辑，仅作为标记使用
    }
}