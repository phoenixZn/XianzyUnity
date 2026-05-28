using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif
#if UNITY_EDITOR && UNITY_2021_1_OR_NEWER
using Screen = UnityEngine.Device.Screen; // To support Device Simulator on Unity 2021.1+
#endif

namespace IngameDebugConsole
{
    public delegate bool SubmitConsoleCommand(string text); 
    
    public partial class DebugLogManager
    {
        [SerializeField]
        private Button commandTriggerButton;
        
        //////////////////////////////////////////////////////////////////////////
        /// IngameDebugConsole 注入部分:
        //////////////////////////////////////////////////////////////////////////
        private void AwakeEx()
        {
            commandTriggerButton.onClick.AddListener( TriggerCmd );
        }

        //DebugLogManager 的外接处理（可空）
        public SubmitConsoleCommand ExSubmitCmdAction { get; set; }

        private bool SubmitCommandEx(string text)
        {
            if (ExSubmitCmdAction != null)
            {
                return ExSubmitCmdAction(text);
            }
            
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }
            if( text.Length > 0 )
            {
                //尝试触发项目自定义命令
                if (DebugLogConsole.SubmitCmdSpec(text))
                    return true;

                //尝试触发 IngameDebugConsole 框架标准原生命令
                if(SubmitCmd_Base(text))
                    return true;
                
                Debug.Log($"SubmitCommandEx Fail : {text}");
            } 
            return false;
        }
        
        
        
        //////////////////////////////////////////////////////////////////////////
        /// IngameDebugConsole 注入部分:
        //////////////////////////////////////////////////////////////////////////
        //命令触发检测流程
        public void TriggerCmd()
        {
            string input = commandInputField.text;
            if (string.IsNullOrEmpty(input))
                return;

            // 如果第一个 cmdKey 不在注册的命令字典中，则默认先做一次自动补全（等价于按一次 Tab）
            string cmdKey = DebugLogConsole.ExtractCmdKey(input);
            bool hasRegisteredCmd = !string.IsNullOrEmpty(cmdKey) && DebugLogConsole.CmdsGroupDic.ContainsKey(cmdKey);
            if (!hasRegisteredCmd)
            {
                // 清掉上一次 Tab 的“基准文本”，确保只执行一次当前输入的自动补全
                commandInputFieldAutoCompleteBase = null;
                commandInputFieldAutoCompletedNow = false;

                // 复用 IngameDebugConsole 原生 Tab 补全逻辑
                OnValidateCommand(input, 0, '\t');
                return;
            }
            OnValidateCommand(input, 0, '\n');
        }


        public void AddDebugButton(string name, string cmdText)
        {
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogError("AddDebugButton fail: name is null or empty.");
                return;
            }

            if (string.IsNullOrEmpty(cmdText))
            {
                Debug.LogError($"AddDebugButton fail: cmdText is null or empty, name:{name}");
                return;
            }

            CustomDebugCommands customCommands = GetComponentInChildren<CustomDebugCommands>(true);
            if (customCommands == null)
            {
                Debug.LogError($"AddDebugButton fail: CustomDebugCommands not found, name:{name}");
                return;
            }

            GameObject template = customCommands.GetTemplate();
            if (template == null)
            {
                Debug.LogError($"AddDebugButton fail: template(CustomBtn) is null, name:{name}");
                return;
            }

            customCommands.RemoveInjectedButtonByName(name);

            GameObject buttonGo = customCommands.CreateInjectedButton();
            if (buttonGo == null)
            {
                Debug.LogError($"AddDebugButton fail: create button failed, name:{name}");
                return;
            }

            buttonGo.name = name;

            Text buttonText = null;
            Transform textNode = buttonGo.transform.Find("CustomBtnTxt");
            if (textNode != null)
            {
                buttonText = textNode.GetComponent<Text>();
            }

            if (buttonText == null)
            {
                Debug.LogError($"AddDebugButton fail: CustomBtnTxt/Text missing, name:{name}");
                Destroy(buttonGo);
                return;
            }

            Button button = buttonGo.GetComponent<Button>();
            if (button == null)
            {
                Debug.LogError($"AddDebugButton fail: Button component missing, name:{name}");
                Destroy(buttonGo);
                return;
            }

            buttonText.text = name;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                commandInputField.text = cmdText;
                TriggerCmd();
            });
        }

        //////////////////////////////////////////////////////////////////////////
        /// IngameDebugConsole 框架标准原生命令
        public bool SubmitCmd_Base(string text)
        {
            if (commandHistory.Count == 0 || commandHistory[commandHistory.Count - 1] != text)
                commandHistory.Add(text);

            commandHistoryIndex = -1;
            unfinishedCommand = null;

            // Execute the command
            DebugLogConsole.ExecuteCommand(text);

            // Snap to bottom and select the latest entry
            SetSnapToBottom(true);
            return true;
        }

    }
    
}
