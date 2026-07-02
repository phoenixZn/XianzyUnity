using System;
using UnityEngine;

namespace Xease.CoreGame
{
    public readonly struct AnyValve
    {
        readonly long _long0;
        readonly float _float0;
        readonly object _object0;
        readonly int _index;

        AnyValve(int index, long i0 = 0, float f0 = 0, object obj0 = null)
        {
            _index = index;
            _long0 = i0;
            _float0 = f0;
            _object0 = obj0;
        }
        
        public int Index => _index;

        public const int IndexInt = 0;
        public const int IndexLong = 1;
        public const int IndexBool = 2;
        public const int IndexFloat = 3;

        public const int IndexString = 7;
        public const int IndexObject = 8;

        public bool IsInt => _index == IndexInt;
        public bool IsLong => _index == IndexLong;
        public bool IsBool => _index == IndexBool;
        public bool IsFloat => _index == IndexFloat;
        public bool IsString => _index == IndexString;
        public bool IsObject => _index == IndexObject;

        public int AsInt => IsInt ? (int)_long0 : throw new InvalidOperationException($"Cannot return as int _index={_index}");

        public long AsLong => IsLong ? _long0 : throw new InvalidOperationException($"Cannot return as long _index={_index}");

        public bool AsBool => IsBool ? _long0 != 0 : throw new InvalidOperationException($"Cannot return as bool _index={_index}");

        public float AsFloat => IsFloat ? _float0 : throw new InvalidOperationException($"Cannot return as float _index={_index}");

        public string AsString => IsString ? _object0 as string : throw new InvalidOperationException($"Cannot return as String _index={_index}");

        public object AsObject => IsObject ? _object0 : throw new InvalidOperationException($"Cannot return as Object _index={_index}");

        public static implicit operator AnyValve(int i) => new AnyValve(IndexInt, i0: i);
        public static implicit operator AnyValve(long l) => new AnyValve(IndexLong, i0: l);
        public static implicit operator AnyValve(bool b) => new AnyValve(IndexBool, i0: b ? 1 : 0);
        public static implicit operator AnyValve(float f) => new AnyValve(IndexFloat, f0: f);
        public static implicit operator AnyValve(string s) => new AnyValve(IndexString, obj0: s);

        public static AnyValve FromObject(object o)
        {
            return new AnyValve(IndexObject, obj0: o);
        }

        private const double Tolerance = 0.000001;

        bool Equals(AnyValve other) =>
            _index == other._index &&
            _index switch
            {
                IndexInt => _long0 == other._long0,
                IndexLong => _long0 == other._long0,
                IndexBool => _long0 == other._long0,
                IndexFloat => Math.Abs((double)_float0 - (double)other._float0) < Tolerance,
                IndexString => Equals(_object0, other._object0),
                IndexObject => Equals(_object0, other._object0),
                _ => false
            };


        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            return obj is AnyValve o && Equals(o);
        }
    }
}