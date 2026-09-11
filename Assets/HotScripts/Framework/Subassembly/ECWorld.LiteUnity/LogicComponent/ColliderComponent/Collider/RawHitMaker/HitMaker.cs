using System;

namespace Xease.CoreGame
{
    public abstract class HitMaker : IRawHitMaker
    {
        internal int _hitCount;
        public RawHit[] RawHits { get; protected set; }

        //////////////////////////////////////////////////////////////////////////
        /// IRawHitMaker:
        public virtual void SetCapacity(int capacity)
        {
            // 容量不足才分配，避免池化取出后打掉已有缓冲。
            if (RawHits == null || RawHits.Length < capacity)
                RawHits = new RawHit[capacity];
        }

        public abstract bool MakeRawHits(LogicEntity ownerEntity, float dt);


        public virtual void Cleanup()
        {
            _hitCount = 0;
            Array.Clear(RawHits, 0, RawHits.Length);
        }

        public abstract void OnRecycle();


        //////////////////////////////////////////////////////////////////////////
        public bool IsFull()
        {
            return _hitCount >= RawHits.Length;
        }

        public void Add(RawHit hit)
        {
            RawHits[_hitCount] = hit;
            _hitCount++;
        }

        public bool ContainsHit()
        {
            return _hitCount > 0;
        }
        
        public abstract bool IsHit(LogicEntity ownerEntity, float dt, LogicEntity item, out RawHit hit);
    }
}
