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
        protected List<InGamePlayerInfo> _playerInfoList = new();
        
        public List<InGamePlayerInfo> PlayerInfoList => _playerInfoList;
        public int PlayerCount => _playerInfoList.Count;
        public InGamePlayerInfo LocalPlayerRef { get; protected set; }
        
        
        //////////////////////////////////////////////////////////////////////////
        /// 
        public override void DisposeOnRemove()
        {
            _playerInfoList?.Clear();
            LocalPlayerRef = null;
        }
        
        public void Init(List<InGamePlayerInfo> playerInfoList)
        {
            if (playerInfoList == null)
            {
                return;
            }

            _playerInfoList = playerInfoList;
            LocalPlayerRef = null;

            for (int i = 0; i < _playerInfoList.Count; i++)
            {
                var info = _playerInfoList[i];
                if (info.IsLocalPlayer)
                {
                    if (LocalPlayerRef != null)
                    {
                        WLogger.LogError($"Init exist_uid={LocalPlayerRef.PlayerID}, new_uid={info.PlayerID}");
                    }
                    LocalPlayerRef = info;
                }
            }
        }
        
        //////////////////////////////////////////////////////////////////////////
        public InGamePlayerInfo GetPlayerInfo(long playerUid)
        {
            if (_playerInfoList == null)
            {
                WLogger.LogError($"GetPlayerInfo _playerInfoList == null, playerUid={playerUid}");
                return null;
            }
            foreach (var info in _playerInfoList)
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
            if (_playerInfoList == null)
            {
                WLogger.LogError($"GetPlayerInfo _playerInfoList == null, index={index}");
                return null;
            }
            if (index >= 0 && index < PlayerCount)
            {
                return _playerInfoList[index];
            }
            if (WLogger.IsDev)
                WLogger.LogError($"GetPlayerInfo return null; index={index}, PlayerCount={PlayerCount}");
            return null;
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
            component.Init(playerInfoList);
            SetUniqueComponent(index, component);
        }
    }

    public static partial class MetaComponentsLookup
    {
        private static ComponentTypeIndex _ComUniPlayersIndex = new(typeof(UniPlayersComponent));
        public static int ComUniPlayers => _ComUniPlayersIndex.Index;
    }
}