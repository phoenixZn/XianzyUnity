using UniFramework.Event;

namespace Launcher
{
    public class SceneEventDefine
    {
        public class StartGame : IEventMessage
        {
            public static void SendEventMessage()
            {
                var msg = new StartGame();
                UniEvent.SendMessage(msg);
            }
        }

    }
}
