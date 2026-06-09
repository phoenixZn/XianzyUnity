#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace IGTools
{
    public class EditorRuntimeToolBase
    {
        public EditorWindow OwnerWindow { get; protected set; }
        public string ToolName { get; protected set; }

        public EditorRuntimeToolBase(EditorWindow ownerWindow)
        {
            OwnerWindow = ownerWindow;
        }
        
        public virtual void InitTool()
        {
            ToolName = "???";
        }
        public virtual void DrawTool()
        {
            
        }
        
        public virtual void ExecCommand(string gmCommand)
        {
            Debug.Log($"ExecCommand : {gmCommand}");
            var isRun = UnityEngine.Application.isPlaying;
            if (!isRun) 
                return;
            //HotfixFunc.ExecuteCommand_GM(gmCommand);
        }

        public virtual object DebugNotify(string cmdType, params object[] data)
        {
            //object res = HotfixFunc.CallPublicStaticMethod("HotUpdate.CoreGame", "SysDebugCoreGame", "DebugNotify", cmdType, data);
            object res = null;
            return res;
        }
        
        public virtual object SendDebugToolCmd(string cmdType, params object[] datas)
        {
            //object res = HotfixFunc.CallPublicStaticMethod("HotUpdate.CoreGame", "GameUtility", "SendDebugToolCmd", cmdType, datas);
            object res = null;
            return res;
        }
        
        public virtual object SendADRecoderToolCmd(string cmdType, params object[] datas)
        {
            //object res = HotfixFunc.CallPublicStaticMethod("HotUpdate.CoreGame", "SysDebugADRecoder", "ADDebugNotify", cmdType, datas);
            object res = null;
            return res;
        }
        
    }
}

#endif