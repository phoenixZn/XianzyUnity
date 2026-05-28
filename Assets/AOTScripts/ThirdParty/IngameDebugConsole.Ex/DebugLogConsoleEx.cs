#if UNITY_EDITOR || UNITY_STANDALONE
// 在移动端，Unity 的 Text 组件不正确渲染 <b> 标签
#define USE_BOLD_COMMAND_SIGNATURES
#endif

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Emit;
using Object = UnityEngine.Object;
#if UNITY_EDITOR && UNITY_2021_1_OR_NEWER
using SystemInfo = UnityEngine.Device.SystemInfo; // 用于支持 Unity 2021.1+ 的 Device Simulator
#endif

namespace IngameDebugConsole
{

    public delegate bool ExecuteConsoleCommand(string text); 
    
    //DebugLogConsole的修改，集中整理
    public static partial class DebugLogConsole
    {
        public static void TempCmdMethodPlaceholder()
        {
            // 统一占位函数：只提供 MethodInfo 供补全/候选 UI 使用
        }
        
        //////////////////////////////////////////////////////////////////////////
        /// IngameDebugConsole 注入部分:
        //////////////////////////////////////////////////////////////////////////
        public static Dictionary<string, string> CmdsGroupDic = new();   //<cmdKey, group>
        public static Dictionary<string, ExecuteConsoleCommand> GroupActionExDic = new(); //<group, Action>
        
        public static void RegisterCmdGroup(string cmdKey, string group)
        {
            if (string.IsNullOrEmpty(cmdKey))
                return;
            if (CmdsGroupDic.ContainsKey(cmdKey))
            {
                return;
            }
            CmdsGroupDic.Add(cmdKey, group);
        }
        
        public static void SetSpecCommandHandler(string group, ExecuteConsoleCommand action)
        {
            GroupActionExDic[group] = action;
        }
        
        public static bool ExecuteCommandSpec(string group, string command)
        {
            if (GroupActionExDic.TryGetValue(group, out var action))
            {
                return action(command);
            }
            return false;
        }
        
        public static bool SubmitCmdSpec(string text)
        {
            string cmdKey = ExtractCmdKey(text);
            if (string.IsNullOrEmpty(cmdKey))
                return false;
            if (CmdsGroupDic.TryGetValue(cmdKey, out var group))
            {
                return ExecuteCommandSpec(group, text);
            }
            return false;
        }
        
        public static void AddCommand_Spec(string command, string group, string description)
        {
            if (string.IsNullOrEmpty(command))
                return;
            if (CmdsGroupDic.TryGetValue(command, out var existingGroup))
            {
                if (existingGroup != group)
                    Debug.Log($"AddCommand_Spec 已存在的 cmd:{command}, group:{group}");
                return;
            }
            CmdsGroupDic.Add(command, group);
            
            // 将命令注册到路由表中，并向 DebugLogConsole 注入一个“占位签名”，
            // 让插件自带的自动补全/候选预览 UI 能显示该命令。
            AddCommand(command, description, TempCmdMethodPlaceholder);
        }
        

        //////////////////////////////////////////////////////////////////////////
        //This：
        public static bool IsCommandInGroup(string command, string checkGroup)
        {
            string cmdKey = ExtractCmdKey(command);
            if (string.IsNullOrEmpty(cmdKey))
                return false;
            if (CmdsGroupDic.TryGetValue(cmdKey, out var group))
            {
                return group == checkGroup;
            }
            return false;
        }

        
        /// <summary>
        /// 从传入的完整文本中提取第一个命令键（cmdkey）。
        /// 例如：`"kickOutSelf 123"` 或 `"kickOutSelf,123"` -> `"kickOutSelf"`
        /// </summary>
        public static string ExtractCmdKey(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            text = text.Trim();

            int whiteSpaceIndex = -1;
            for (int i = 0; i < text.Length; i++)
            {
                if (char.IsWhiteSpace(text[i]))
                {
                    whiteSpaceIndex = i;
                    break;
                }
            }

            int commaIndex = text.IndexOf(',');
            if (commaIndex < 0)
                commaIndex = int.MaxValue;

            int endIndex;
            if (whiteSpaceIndex < 0)
                endIndex = commaIndex;
            else
                endIndex = Math.Min(whiteSpaceIndex, commaIndex);

            if (endIndex <= 0 || endIndex == int.MaxValue)
                return text;

            return text.Substring(0, endIndex);
        }
    }
}