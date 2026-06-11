using System.Collections.Generic;

namespace Xease.CoreGame
{
    public partial class InGamePlayerInfo
    {
        public long PlayerID { get; private set; } = 0;
        public int PlayerIndex { get; private set; } = 0;
        public bool IsLocalPlayer { get; private set; } = false;
    }

    //////////////////////////////////////////////////////////////////////////
    public class UniPlayersComponent : MetaComponent
    {
        private List<InGamePlayerInfo> mPlayerInfoList = new();
        public List<InGamePlayerInfo> PlayerInfoList => mPlayerInfoList;


        private InGamePlayerInfo mLocalPlayerRef = null;
        public InGamePlayerInfo LocalPlayerRef => mLocalPlayerRef;

        public int PlayerCount => mPlayerInfoList.Count;

        public override void DisposeOnRemove()
        {
            if (mPlayerInfoList == null)
                mPlayerInfoList.Clear();
            mLocalPlayerRef = null;
        }

        public InGamePlayerInfo GetPlayerInfo(long playerUid)
        {
            if (mPlayerInfoList == null)
            {
                if (WLogger.IsDev)
                    WLogger.LogError($"GetPlayerInfo mPlayerInfoList == null, playerUid={playerUid}");
                return null;
            }
            foreach (var info in mPlayerInfoList)
            {
                if (info.PlayerID == playerUid)
                {
                    return info;
                }
            }
            if (WLogger.IsDev)
                WLogger.LogError($"GetPlayerInfo return null; playerUid={playerUid}, PlayerCount={PlayerCount}");
            return null;
        }

        public InGamePlayerInfo GetPlayerInfoByIndex(int index)
        {
            if (mPlayerInfoList == null)
            {
                if (WLogger.IsDev)
                    WLogger.LogError($"GetPlayerInfo mPlayerInfoList == null, index={index}");
                return null;
            }
            if (index >= 0 && index < PlayerCount)
            {
                return mPlayerInfoList[index];
            }
            if (WLogger.IsDev)
                WLogger.LogError($"GetPlayerInfo return null; index={index}, PlayerCount={PlayerCount}");
            return null;
        }
        
        public void InitPlayerInfoList(List<InGamePlayerInfo> playerInfoList)
        {
            if (playerInfoList == null)
            {
                return;
            }

            mPlayerInfoList = playerInfoList;
            mLocalPlayerRef = null;

            for (int i = 0; i < mPlayerInfoList.Count; i++)
            {
                var info = mPlayerInfoList[i];
                if (info.IsLocalPlayer)
                {
                    if (mLocalPlayerRef != null)
                    {
                        WLogger.LogError($"InitPlayerInfoList exist_uid={mLocalPlayerRef.PlayerID}, new_uid={info.PlayerID}");
                    }

                    mLocalPlayerRef = info;
                }
            }
        }
    }

    public partial class MetaWorld
    {
        public UniPlayersComponent comUniPlayers
        {
            get { return GetUniqueComponent<UniPlayersComponent>(MetaComponentsLookup.ComUniPlayers); }
        }

        public bool hasComUniPlayers
        {
            get { return HasUniqueComponent(MetaComponentsLookup.ComUniPlayers); }
        }

        public void SetComUniPlayers(List<InGamePlayerInfo> playerInfoList)
        {
            var index = MetaComponentsLookup.ComUniPlayers;
            var component = (UniPlayersComponent)UniqueEntity.CreateComponent(index, typeof(UniPlayersComponent));
            component.InitPlayerInfoList(playerInfoList);
            SetUniqueComponent(index, component);
        }
    }

    public static partial class MetaComponentsLookup
    {
        private static ComponentTypeIndex _ComUniPlayersIndex = new(typeof(UniPlayersComponent));
        public static int ComUniPlayers => _ComUniPlayersIndex.Index;
    }
}