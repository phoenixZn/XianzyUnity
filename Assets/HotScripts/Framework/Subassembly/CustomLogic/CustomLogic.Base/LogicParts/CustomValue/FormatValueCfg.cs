namespace Xease.CoreGame
{
    //////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// 兼容常量、黑板变量 等多种格式化配置方式
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class FormatValueCfg<T> : IValueConfig<T>
    {
        public string VarID { get; protected set; } = null;
        protected T _defaultValue;
        public T DefaultValue => _defaultValue;

        public FormatValueCfg(T defaultValue)
        {
            _defaultValue = defaultValue;
        }

        public FormatValueCfg(string varID, T defaultValue = default)
        {
            SetVarID(varID);
            _defaultValue = defaultValue;
        }

        //as: IValueConfig<T>
        public T GetDefaultValue()
        {
            return _defaultValue;
        }

        public T GetValue(CustomNode node)
        {
            VarEnv varLib = node.VarEnvRef;
            if (varLib == null || string.IsNullOrEmpty(VarID))
                return _defaultValue;
            return varLib.ReadVar<T>(VarID, out var ret) ? ret : _defaultValue;
        }

        //常量解析
        public virtual bool ParseByString(string str)
        {
            return false;
        }

        //格式化解析
        public bool ParseByFormatString(string str)
        {
            //判断是否变量
            if (str.StartsWith("BB#"))
            {
                VarID = str.Substring(3);
                return true;
            }

            // 常量解析成功则与变量绑定互斥，避免复用实例仍读黑板
            if (!ParseByString(str))
                return false;
            VarID = null;
            return true;
        }

        public void SetVarID(string varID)
        {
            VarID = varID;
        }

        public void ResetDefaultValue(T v)
        {
            _defaultValue = v;
        }

        public string LogStr => $"[DefaultV={DefaultValue}, VarID={VarID}]";

    }

    //////////////////////////////////////////////////////////////////////////
    public class IntCfg : FormatValueCfg<int>
    {
        public IntCfg(int defaultValue) : base(defaultValue)
        {
        }

        public IntCfg(string varID, int defaultValue = default) : base(varID, defaultValue)
        {
        }

        public override bool ParseByString(string str)
        {
            // 失败时 TryParse 仍会把 out 写成 0，不能直接覆写已有默认值
            if (int.TryParse(str, out var v))
            {
                _defaultValue = v;
                return true;
            }

            return false;
        }
    }

    //////////////////////////////////////////////////////////////////////////
    public class LongCfg : FormatValueCfg<long>
    {
        public LongCfg(long defaultValue) : base(defaultValue)
        {
        }

        public LongCfg(string varID, long defaultValue = default) : base(varID, defaultValue)
        {
        }

        public override bool ParseByString(string str)
        {
            // 失败时 TryParse 仍会把 out 写成 0，不能直接覆写已有默认值
            if (long.TryParse(str, out var v))
            {
                _defaultValue = v;
                return true;
            }

            return false;
        }
    }

    //////////////////////////////////////////////////////////////////////////
    public class FloatCfg : FormatValueCfg<float>
    {
        public FloatCfg(float defaultValue) : base(defaultValue)
        {
        }

        public FloatCfg(string varID, float defaultValue = default) : base(varID, defaultValue)
        {
        }

        public override bool ParseByString(string str)
        {
            // 失败时 TryParse 仍会把 out 写成 0，不能直接覆写已有默认值
            if (float.TryParse(str, out var v))
            {
                _defaultValue = v;
                return true;
            }

            return false;
        }
    }

    //////////////////////////////////////////////////////////////////////////
    public class StringCfg : FormatValueCfg<string>
    {
        public StringCfg(string defaultValue) : base(defaultValue)
        {
        }

        public override bool ParseByString(string str)
        {
            _defaultValue = str;
            return true;
        }
    }
}