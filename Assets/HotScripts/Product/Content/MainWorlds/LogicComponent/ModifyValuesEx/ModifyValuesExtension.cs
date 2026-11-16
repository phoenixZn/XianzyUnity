using Xease.FP;

namespace Xease.CoreGame
{
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
            if (e == null || !e.hasComModifyValues)
            {
                WLogger.LogError("GetAttributeFixPoint !HasAttributes");
                return errorValue;
            }
            var rv = errorValue;
            if (e.comModifyValues.TryGetValue(key, ref rv))
                return rv;
            else
                return errorValue;
        }

        /// <summary>
        /// 获取 float 属性值，读取失败时返回指定错误值。
        /// </summary>
        public static float GetAttributeFloat(this LogicEntity e, int key, float errorValue = 0)
        {
            if (e == null || !e.hasComModifyValues)
            {
                WLogger.LogError("GetAttributeFloat !HasAttributes");
                return errorValue;
            }
            var rv = errorValue;
            if (e.comModifyValues.TryGetValue(key, ref rv))
                return rv;
            else
                return errorValue;
        }

        /// <summary>
        /// 获取 double 属性值，读取失败时返回指定错误值。
        /// </summary>
        public static double GetAttributeDouble(this LogicEntity e, int key, double errorValue = 0)
        {
            if (e == null || !e.hasComModifyValues)
            {
                WLogger.LogError("GetAttributeDouble !HasAttributes");
                return errorValue;
            }
            var rv = errorValue;
            if (e.comModifyValues.TryGetValue(key, ref rv))
                return rv;
            else
                return errorValue;
        }

        /// <summary>
        /// 获取 int 属性值，读取失败时返回指定错误值。
        /// </summary>
        public static int GetAttributeInt(this LogicEntity e, int key, int errorValue = 0)
        {
            if (e == null || !e.hasComModifyValues)
            {
                WLogger.LogError("GetAttributeInt !HasAttributes");
                return errorValue;
            }
            var rv = errorValue;
            if (e.comModifyValues.TryGetValue(key, ref rv))
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
            if (e == null || !e.hasComModifyValues)
            {
                WLogger.LogError("GetAttributeBool !HasAttributes");
                return rv;
            }
            if (!e.hasComModifyValues)
                return rv;
            if (e.comModifyValues.TryGetValue(key, ref rv))
                return rv;
            else
                return rv;
        }

        /// <summary>
        /// 对 float 属性添加指定 flag 的修改值。
        /// </summary>
        public static void ModifyAttribute(this LogicEntity e, int key, float value, int flag)
        {
            if (e == null || !e.hasComModifyValues)
            {
                WLogger.LogError("ModifyAttribute !HasAttributes");
                return;
            }
            e.comModifyValues.Modify(key, value, flag);
        }

        /// <summary>
        /// 对 int 属性添加指定 flag 的修改值。
        /// </summary>
        public static void ModifyAttribute(this LogicEntity e, int key, int value, int flag)
        {
            if (e == null || !e.hasComModifyValues)
            {
                WLogger.LogError("ModifyAttribute !HasAttributes");
                return;
            }
            e.comModifyValues.Modify(key, value, flag);
        }

        /// <summary>
        /// 对 bool 属性添加指定 flag 的修改值。
        /// </summary>
        public static void ModifyAttribute(this LogicEntity e, int key, bool value, int flag)
        {
            if (e == null || !e.hasComModifyValues)
            {
                WLogger.LogError("ModifyAttribute !HasAttributes");
                return;
            }
            e.comModifyValues.Modify(key, value, flag);
        }

        /// <summary>
        /// 移除指定类型属性上对应 flag 的修改值。
        /// </summary>
        public static void RemoveModify<T>(this LogicEntity e, int key, int flag)
        {
            if (e == null || !e.hasComModifyValues)
            {
                WLogger.LogError("RemoveModify !HasAttributes");
                return;
            }
            e.comModifyValues.RemoveModify<T>(key, flag);
        }
        
        
        // public void GetTotalPropertySnapshot(this LogicEntity e, out PropertySnapshot snapshot)
        // {
        //     snapshot = new PropertySnapshot();
        //     snapshot.FillFromEntity(e);
        // }
    }
}
