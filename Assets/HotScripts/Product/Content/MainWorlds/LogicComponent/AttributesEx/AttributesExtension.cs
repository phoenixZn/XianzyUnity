using Xease.FP;

namespace Xease.CoreGame
{
    public static partial class AttributeBool
    {
        public const int Moveable = 1001; // 是否可以移动
        public const int CanCollision = 1002; // 是否可以发生碰撞
        public const int Visible = 1003;
        public const int Stunned = 1004; // 是否处于不能行动状态（眩晕等）
        public const int CanBeTargeted = 1005; // 是否可以被索敌
        public const int CanBeHurted = 1006; // 是否可以被伤害
        
        public static AttributesComponent AddBaseAttributeBool(this AttributesComponent component)
        {
            // 默认的布尔状态需要在这里定义
            component.SetAttribute<bool>(AttributeBool.Moveable, new MultChangeBool_AND(true));
            component.SetAttribute<bool>(AttributeBool.CanCollision, new MultChangeBool_AND(true));
            component.SetAttribute<bool>(AttributeBool.Visible, new MultChangeBool_AND(true));
            component.SetAttribute<bool>(AttributeBool.Stunned, new MultChangeBool_OR(false));
            component.SetAttribute<bool>(AttributeBool.CanBeHurted, new MultChangeBool_AND(true));
            component.SetAttribute<bool>(AttributeBool.CanBeTargeted, new MultChangeBool_AND(true));
            return component;
        }
        
    }

    //////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// LogicEntity 属性读取和修改的快捷扩展。语法糖
    /// </summary>
    public static partial class AttributesExtensionFoundation
    {
        /// <summary>
        /// 获取定点数属性值，读取失败时返回指定错误值。
        /// </summary>
        public static FixPoint GetAttributeFixPoint(this LogicEntity e, int key, FixPoint errorValue = default(FixPoint))
        {
            var rv = errorValue;
            if (e.comAttributes.TryGetValue(key, ref rv))
                return rv;
            else
                return errorValue;
        }

        /// <summary>
        /// 获取 float 属性值，读取失败时返回指定错误值。
        /// </summary>
        public static float GetAttributeFloat(this LogicEntity e, int key, float errorValue = 0)
        {
            if (e == null || !e.hasComAttributes)
            {
                WLogger.LogError("e.GetAttribute  !HasAttributes");
            }

            var rv = errorValue;
            if (e.comAttributes.TryGetValue(key, ref rv))
                return rv;
            else
                return errorValue;
        }

        /// <summary>
        /// 获取 double 属性值，读取失败时返回指定错误值。
        /// </summary>
        public static double GetAttributeDouble(this LogicEntity e, int key, double errorValue = 0)
        {
            if (e == null || !e.hasComAttributes)
            {
                WLogger.LogError("e.GetAttribute  !HasAttributes");
            }

            var rv = errorValue;
            if (e.comAttributes.TryGetValue(key, ref rv))
                return rv;
            else
                return errorValue;
        }

        /// <summary>
        /// 获取 int 属性值，读取失败时返回指定错误值。
        /// </summary>
        public static int GetAttributeInt(this LogicEntity e, int key, int errorValue = 0)
        {
            if (e == null || !e.hasComAttributes)
            {
                WLogger.LogError("e.GetAttribute  !HasAttributes");
            }

            var rv = errorValue;
            if (e.comAttributes.TryGetValue(key, ref rv))
                return rv;
            else
                return errorValue;
        }

        /// <summary>
        /// 获取 bool 属性值，读取失败时返回指定错误值。
        /// </summary>
        public static bool GetAttributeBool(this LogicEntity e, int key, bool errorValue = true)
        {
            var rv = errorValue;
            if (e == null || !e.hasComAttributes)
            {
                WLogger.LogError("e.GetAttribute  !HasAttributes");
                return rv;
            }

            if (!e.hasComAttributes)
                return rv;
            if (e.comAttributes.TryGetValue(key, ref rv))
                return rv;
            else
                return rv;
        }

        /// <summary>
        /// 对 float 属性添加指定 flag 的修改值。
        /// </summary>
        public static void ModifyAttribute(this LogicEntity e, int key, float value, int flag)
        {
            if (e == null || !e.hasComAttributes)
            {
                WLogger.LogError("Actor.SetAttribute  !HasAttributes");
            }

            e.comAttributes.Modify(key, value, flag);
        }

        /// <summary>
        /// 对 int 属性添加指定 flag 的修改值。
        /// </summary>
        public static void ModifyAttribute(this LogicEntity e, int key, int value, int flag)
        {
            if (e == null || !e.hasComAttributes)
            {
                WLogger.LogError("Actor.SetAttribute  !HasAttributes");
            }

            e.comAttributes.Modify(key, value, flag);
        }

        /// <summary>
        /// 对 bool 属性添加指定 flag 的修改值。
        /// </summary>
        public static void ModifyAttribute(this LogicEntity e, int key, bool value, int flag)
        {
            if (e == null || !e.hasComAttributes)
            {
                WLogger.LogError("Actor.SetAttribute  !HasAttributes");
            }

            e.comAttributes.Modify(key, value, flag);
        }

        /// <summary>
        /// 移除指定类型属性上对应 flag 的修改值。
        /// </summary>
        public static void RemoveModify<T>(this LogicEntity e, int key, int flag)
        {
            if (e == null || !e.hasComAttributes)
            {
                WLogger.LogError("Actor.RemoveAttribute  !HasAttributes");
            }
            e.comAttributes.RemoveModify<T>(key, flag);
        }
        
        
        // public void GetTotalPropertySnapshot(this LogicEntity e, out PropertySnapshot snapshot)
        // {
        //     snapshot = new PropertySnapshot();
        //     snapshot.FillFromEntity(e);
        // }
    }
}
