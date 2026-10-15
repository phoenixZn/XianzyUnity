using System.Collections.Generic;

namespace Xease.CoreGame
{
    /// <summary>
    /// 免疫组件（通用机制层，不含具体业务免疫语义，业务具名免疫见 Product 侧 ImmuneEx 扩展）
    /// 其实是特化的属性组件，但是其中的免疫属性功能较为统一、数量可能会膨胀很多，而且具有较强的语义性，故单独拆出来
    /// </summary>
    public class ImmuneComponent : LogicComponent
    {
        protected readonly Dictionary<int, MultChangeBool_OR> _boolImmunes = new(); // 布尔类型免疫效果，使用 OR 关系叠加
        protected readonly Dictionary<int, MultChangeInt_ADD> _permyriadImmunes = new(); // 万分比类型免疫效果，使用加法关系叠加
        protected readonly Dictionary<int, MultChangeInt_ADD> _buffImmunes = new(); // Buff免疫效果，使用加法关系叠加
        protected readonly Dictionary<int, MultChangeInt_ADD> _buffTagImmunes = new(); // Buff标签免疫效果，使用加法关系叠加

        //////////////////////////////////////////////////////////////////////////
        /// LogicComponent：override

        public override void DisposeOnRemove()
        {
            base.DisposeOnRemove();
            _boolImmunes.Clear();
            _permyriadImmunes.Clear();
            _buffImmunes.Clear();
            _buffTagImmunes.Clear();
        }

        //////////////////////////////////////////////////////////////////////////
        /// This: 布尔免疫

        /// <summary>
        /// 查询布尔免疫，无任何来源时视为不免疫
        /// </summary>
        public bool GetBoolImmune(int boolImmuneType)
        {
            if (_boolImmunes.TryGetValue(boolImmuneType, out var multChange))
            {
                return multChange.Value;
            }
            return false;
        }

        /// <summary>
        /// 添加一路布尔免疫来源，同 flag 重复添加会覆盖旧值；空值（false）不会建立条目
        /// </summary>
        public void AddBoolImmune(int boolImmuneType, bool value, int flag = 0)
        {
            if (!_boolImmunes.TryGetValue(boolImmuneType, out var multChange))
            {
                if (value == false)
                {
                    return;
                }
                multChange = new MultChangeBool_OR(false);
                _boolImmunes.Add(boolImmuneType, multChange);
            }
            multChange.AddChange(value, flag);
        }

        /// <summary>
        /// 按 flag 移除一路布尔免疫来源
        /// </summary>
        public void RemoveBoolImmune(int boolImmuneType, int flag = 0)
        {
            if (_boolImmunes.TryGetValue(boolImmuneType, out var multChange))
            {
                multChange.RemoveChange(flag);
            }
        }

        //////////////////////////////////////////////////////////////////////////
        /// This: 万分比免疫

        /// <summary>
        /// 查询万分比免疫（万分比，10000 为完全免疫），无来源时为 0
        /// </summary>
        public int GetPermyriadImmune(int permyriadImmueType)
        {
            if (_permyriadImmunes.TryGetValue(permyriadImmueType, out var multChange))
            {
                return multChange.Value;
            }
            return 0;
        }

        /// <summary>
        /// 添加一路万分比免疫来源，同 flag 重复添加会覆盖旧值；空值（0）不会建立条目
        /// </summary>
        public void AddPermyriadImmune(int permyriadImmueType, int value, int flag = 0)
        {
            if (!_permyriadImmunes.TryGetValue(permyriadImmueType, out var multChange))
            {
                if (value == 0)
                {
                    return;
                }
                multChange = new MultChangeInt_ADD(0);
                _permyriadImmunes.Add(permyriadImmueType, multChange);
            }
            multChange.AddChange(value, flag);
        }

        /// <summary>
        /// 按 flag 移除一路万分比免疫来源
        /// </summary>
        public void RemovePermyriadImmune(int permyriadImmueType, int flag = 0)
        {
            if (_permyriadImmunes.TryGetValue(permyriadImmueType, out var multChange))
            {
                multChange.RemoveChange(flag);
            }
        }

        //////////////////////////////////////////////////////////////////////////
        /// This: 指定 Buff 免疫

        /// <summary>
        /// 查询对指定 Buff 的免疫值（万分比），无来源时为 0
        /// </summary>
        public int GetBuffImmune(int buffId)
        {
            if (_buffImmunes.TryGetValue(buffId, out var multChange))
            {
                return multChange.Value;
            }
            return 0;
        }

        /// <summary>
        /// 添加一路指定 Buff 免疫来源，同 flag 重复添加会覆盖旧值；空值（0）不会建立条目
        /// </summary>
        public void AddBuffImmune(int buffId, int value, int flag = 0)
        {
            if (!_buffImmunes.TryGetValue(buffId, out var multChange))
            {
                if (value == 0)
                {
                    return;
                }
                multChange = new MultChangeInt_ADD(0);
                _buffImmunes.Add(buffId, multChange);
            }
            multChange.AddChange(value, flag);
        }

        /// <summary>
        /// 按 flag 移除一路指定 Buff 免疫来源
        /// </summary>
        public void RemoveBuffImmune(int buffId, int flag = 0)
        {
            if (_buffImmunes.TryGetValue(buffId, out var multChange))
            {
                multChange.RemoveChange(flag);
            }
        }

        //////////////////////////////////////////////////////////////////////////
        /// This: Buff 标签免疫

        /// <summary>
        /// 查询对指定 BuffTag 的免疫值（万分比）：所有被 buffTag 完全包含的来源标签累加
        /// </summary>
        public int GetBuffTagImmune(int buffTag)
        {
            var value = 0;
            foreach (var kv in _buffTagImmunes)
            {
                if ((kv.Key & buffTag) == kv.Key)
                {
                    value += kv.Value.Value;
                }
            }
            return value;
        }

        /// <summary>
        /// 添加一路 BuffTag 免疫来源，同 flag 重复添加会覆盖旧值；空值（0）不会建立条目
        /// </summary>
        public void AddBuffTagImmune(int buffTag, int value, int flag = 0)
        {
            if (!_buffTagImmunes.TryGetValue(buffTag, out var multChange))
            {
                if (value == 0)
                {
                    return;
                }
                multChange = new MultChangeInt_ADD(0);
                _buffTagImmunes.Add(buffTag, multChange);
            }
            multChange.AddChange(value, flag);
        }

        /// <summary>
        /// 按 flag 移除一路 BuffTag 免疫来源
        /// </summary>
        public void RemoveBuffTagImmune(int buffTag, int flag = 0)
        {
            if (_buffTagImmunes.TryGetValue(buffTag, out var multChange))
            {
                multChange.RemoveChange(flag);
            }
        }
    }

    public partial class LogicEntity
    {
        public ImmuneComponent comImmune
        {
            get { return (ImmuneComponent)GetComponent(LogicComponentsLookup.ComImmune); }
        }

        public bool hasComImmune
        {
            get { return HasComponent(LogicComponentsLookup.ComImmune); }
        }

        public void AddComImmune()
        {
            var index = LogicComponentsLookup.ComImmune;
            if (!hasComImmune)
            {
                var component = (ImmuneComponent)CreateComponent(index, typeof(ImmuneComponent));
                AddComponent(index, component);
            }
        }

        public void RemoveComImmune()
        {
            if (hasComImmune)
            {
                RemoveComponent(LogicComponentsLookup.ComImmune);
            }
        }

        /// <summary>
        /// 查询对指定 Buff 的免疫值（万分比），无免疫组件时为 0
        /// </summary>
        public int GetBuffImmune(int buffId)
        {
            if (hasComImmune)
            {
                return comImmune.GetBuffImmune(buffId);
            }
            return 0;
        }

        /// <summary>
        /// 查询对指定 BuffTag 的免疫值（万分比），无免疫组件时为 0
        /// </summary>
        public int GetBuffTagImmune(int buffTag)
        {
            if (hasComImmune)
            {
                return comImmune.GetBuffTagImmune(buffTag);
            }
            return 0;
        }
    }


    public static partial class LogicComponentsLookup
    {
        private static ComponentTypeIndex _ComImmuneIndex = new(typeof(ImmuneComponent));
        public static int ComImmune => _ComImmuneIndex.Index;
    }
}
