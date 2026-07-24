using System;
using System.Collections.Generic;
using Entitas;

namespace Xease.CoreGame
{
    public class TagComponent : LogicComponent
    {
        public uint Tags { get; protected set; }

        public void SetTags(uint tags)
        {
            Tags = tags;
        }

        public void AddTags(uint tags)
        {
            Tags |= tags;
        }

        public void RemoveTags(uint tags)
        {
            uint mask = ~tags;
            Tags &= mask;
        }

        public bool HasTags(uint tags)
        {
            return (Tags & tags) != 0;
        }

        /// <summary>
        /// 将 Tags 中每个置位掩码拆分为独立的 int key（如 0b1010 → [2, 8]）。
        /// </summary>
        public int[] GetTagKeys()
        {
            return GetTagKeys(Tags);
        }

        /// <summary>
        /// 将 Tags 中每个置位拆分为位索引数组（如 0b1010 → [1, 3]）。
        /// </summary>
        public int[] GetTagIndexArray()
        {
            return GetTagIndexArray(Tags);
        }

        /// <summary>
        /// 将位掩码中每个置位掩码拆分为独立的 int key。
        /// </summary>
        public static int[] GetTagKeys(uint tags)
        {
            if (tags == 0)
                return Array.Empty<int>();

            var keys = new int[PopCount(tags)];
            var idx = 0;
            while (tags != 0)
            {
                var flag = tags & (uint)-(int)tags;
                keys[idx++] = (int)flag;
                tags &= ~flag;
            }
            return keys;
        }

        /// <summary>
        /// 将位掩码中每个置位拆分为位索引。
        /// </summary>
        public static int[] GetTagIndexArray(uint tags)
        {
            if (tags == 0)
                return Array.Empty<int>();

            var keys = new int[PopCount(tags)];
            var idx = 0;
            for (int i = 0; i < 32; i++)
            {
                if ((tags & (1u << i)) != 0)
                    keys[idx++] = i;
            }
            return keys;
        }

        /// <summary>
        /// 统计 uint 中置位个数（population count）。
        /// </summary>
        public static int PopCount(uint value)
        {
            var count = 0;
            while (value != 0)
            {
                count++;
                value &= value - 1;
            }
            return count;
        }
    }

    //////////////////////////////////////////////////////////////////////////
    public partial class LogicEntity
    {
        public TagComponent comTag
        {
            get { return (TagComponent)GetComponent(LogicComponentsLookup.ComTag); }
        }

        public bool hasComTag
        {
            get { return HasComponent(LogicComponentsLookup.ComTag); }
        }

        public void AddComTags(uint newTags)
        {
            var index = LogicComponentsLookup.ComTag;
            if (!hasComTag)
            {
                var component = (TagComponent)CreateComponent(index, typeof(TagComponent));
                component.SetTags(newTags);
                AddComponent(index, component);
                return;
            }

            // 新实例再 Replace，保证 previous 仍持旧 Tags，避免 EntityIndex 旧 key 残留
            var previous = (TagComponent)GetComponent(index);
            var nextTags = previous.Tags | newTags;
            if (nextTags == previous.Tags)
                return;

            var next = (TagComponent)CreateComponent(index, typeof(TagComponent));
            next.SetTags(nextTags);
            ReplaceComponent(index, next);
        }

        public void RemoveComTag()
        {
            RemoveComponent(LogicComponentsLookup.ComTag);
        }

        public void RemoveTags(uint tags)
        {
            if (!hasComTag)
                return;

            // 新实例再 Replace，保证 previous 仍持旧 Tags，避免 EntityIndex 旧 key 残留
            var index = LogicComponentsLookup.ComTag;
            var previous = (TagComponent)GetComponent(index);
            var nextTags = previous.Tags & ~tags;
            if (nextTags == previous.Tags)
                return;

            var next = (TagComponent)CreateComponent(index, typeof(TagComponent));
            next.SetTags(nextTags);
            ReplaceComponent(index, next);
        }

        public bool HasTags(uint tags)
        {
            if (!hasComTag)
                return false;
            var index = LogicComponentsLookup.ComTag;
            var component = (TagComponent)GetComponent(index);
            return component.HasTags(tags);
        }
    }
    

    //////////////////////////////////////////////////////////////////////////
    public static partial class LogicComponentsLookup
    {
        private static ComponentTypeIndex _ComTagIndex = new(typeof(TagComponent));
        public static int ComTag => _ComTagIndex.Index;
    }
}