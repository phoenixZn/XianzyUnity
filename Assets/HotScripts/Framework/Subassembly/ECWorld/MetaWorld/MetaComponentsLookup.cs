using System.Collections.Generic;

namespace HotUpdate.CoreGame
{
    public static partial class MetaComponentsLookup
    {
        public static List<ComponentTypeIndex> TypeIndexList = new List<ComponentTypeIndex>();
        public static int TotalComponents => TypeIndexList.Count;
    }
}