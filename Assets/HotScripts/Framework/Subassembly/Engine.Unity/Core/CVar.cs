using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Xease.Engine.Core
{
    public abstract class CVar<T> : IConsoleVariable
    {
        private readonly T _defaultValue;
        private int _priority;
        private string _name;
        private string _description;

        protected CVar(T defaultValue)
        {
            _defaultValue = defaultValue;
            _priority = (int)ConsolePriority.Default;
            _name = string.Empty;
            _description = string.Empty;
            SetRawValue(defaultValue);
        }

        public event Action<T, T> OnValueChanged;

        public string Name => _name;
        public string Description => _description;
        public Type ValueType => typeof(T);
        public bool IsRegistered => !string.IsNullOrEmpty(_name);

        public ConsolePriority Priority
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (ConsolePriority)Volatile.Read(ref _priority);
        }

        public T DefaultValue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _defaultValue;
        }

        public T Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetRawValue();
        }

        object IConsoleVariable.BoxedValue => Value;
        object IConsoleVariable.BoxedDefaultValue => _defaultValue;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetValue(T value, ConsolePriority priority)
        {
            int targetPriority = (int)priority;
            while (true)
            {
                int current = Volatile.Read(ref _priority);
                if (targetPriority < current)
                {
                    return false;
                }

                if (Interlocked.CompareExchange(ref _priority, targetPriority, current) != current)
                {
                    continue;
                }

                T old = GetRawValue();
                if (AreEqual(old, value))
                {
                    return true;
                }

                SetRawValue(value);
                OnValueChanged?.Invoke(old, value);
                return true;
            }
        }

        public void BindMetadata(string name, string description)
        {
            _name = name ?? string.Empty;
            _description = description ?? string.Empty;
        }

        public abstract bool SetFromString(string valueText, ConsolePriority priority);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual bool AreEqual(T a, T b)
        {
            return EqualityComparer<T>.Default.Equals(a, b);
        }

        protected abstract T GetRawValue();
        protected abstract void SetRawValue(T value);

        public virtual string GetValueString()
        {
            T value = GetRawValue();
            return value == null ? string.Empty : value.ToString();
        }
        public override string ToString()
        {
            return $"[{Name}] {_description}: {GetValueString()}";
        }
    }

    public sealed class CVarInt : CVar<int>
    {
        private int _value;

        public CVarInt(int defaultValue) : base(defaultValue)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override int GetRawValue() => Volatile.Read(ref _value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void SetRawValue(int value) => Interlocked.Exchange(ref _value, value);

        public override bool SetFromString(string valueText, ConsolePriority priority)
        {
            if (!int.TryParse(valueText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
            {
                return false;
            }

            return SetValue(parsed, priority);
        }
    }

    public sealed class CVarFloat : CVar<float>
    {
        private int _valueBits;

        public CVarFloat(float defaultValue) : base(defaultValue)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override float GetRawValue() => BitConverter.Int32BitsToSingle(Volatile.Read(ref _valueBits));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void SetRawValue(float value)
        {
            Interlocked.Exchange(ref _valueBits, BitConverter.SingleToInt32Bits(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override bool AreEqual(float a, float b) => Math.Abs(a - b) <= 1e-6f;

        public override bool SetFromString(string valueText, ConsolePriority priority)
        {
            if (!float.TryParse(valueText, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out float parsed))
            {
                return false;
            }

            return SetValue(parsed, priority);
        }
    }

    public sealed class CVarBool : CVar<bool>
    {
        private int _value;

        public CVarBool(bool defaultValue) : base(defaultValue)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override bool GetRawValue() => Volatile.Read(ref _value) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void SetRawValue(bool value) => Interlocked.Exchange(ref _value, value ? 1 : 0);

        public override bool SetFromString(string valueText, ConsolePriority priority)
        {
            if (bool.TryParse(valueText, out bool b))
            {
                return SetValue(b, priority);
            }

            if (int.TryParse(valueText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int i))
            {
                return SetValue(i != 0, priority);
            }

            return false;
        }
    }

    public sealed class CVarString : CVar<string>
    {
        private string _value;

        public CVarString(string defaultValue) : base(defaultValue ?? string.Empty)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override string GetRawValue() => Volatile.Read(ref _value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void SetRawValue(string value) => Volatile.Write(ref _value, value ?? string.Empty);

        public override bool SetFromString(string valueText, ConsolePriority priority)
        {
            return SetValue(valueText ?? string.Empty, priority);
        }
    }
}
