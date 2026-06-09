#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditor.Overlays;
using System.Linq;
using System;

using Vector2 = UnityEngine.Vector2;

namespace IGTools
{
    public class SupplyTestTool : EditorRuntimeToolBase
    {
        private string supplylistsearch = "";
        private Vector2 supplyListScrollPos = Vector2.zero;
        private List<KeyValuePair<int, string>> _supplyList = new();
        
        public SupplyTestTool(EditorWindow ownerWindow) : base(ownerWindow)
        {
        }
        
        public override void InitTool()
        {
            ToolName = "Supply逻辑测试";
            _supplyList.Clear();
        }

        private bool _waitPlaying = true;
        public override void DrawTool()
        {
            GUILayout.Space(10);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("SupplyTestTool.cs");
            EditorGUILayout.EndVertical();

            if (_waitPlaying && Application.isPlaying)
            {
                _waitPlaying = false;
                LoadSupplyLogicList();
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("加载配置 (Config)", GUILayout.Height(40)))
            {
                LoadSupplyLogicList();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("触发Supply-3", GUILayout.Height(40)))
            {
                DebugNotify("dn_trigger_supply");
            }
            if (GUILayout.Button("触发Supply-5", GUILayout.Height(40)))
            {
                DebugNotify("dn_trigger_supply_5");
            }
            GUILayout.EndHorizontal();
            
            if (_supplyList.Count == 0)
            {
                EditorGUILayout.HelpBox("请在游戏运行后，刷新配置", MessageType.Info);
                return;
            }
            DrawSupplyList();

            GUILayout.Space(10);
        }

        private void DrawSupplyList()
        {
            GUILayout.Space(10);
            // //搜索
            // GUILayout.BeginHorizontal();
            // EditorGUILayout.LabelField("搜索：", GUILayout.Width(30));
            // supplylistsearch = GUILayout.TextField(supplylistsearch);
            // if (GUILayout.Button("Clear", GUILayout.Width(50)))
            // {
            //     supplylistsearch = "";
            // }
            // GUILayout.EndHorizontal();
            // GUILayout.Space(10);
            //
            // //筛选
            // GUILayout.BeginHorizontal();
            // if(GUILayout.Button("全部"))
            // {
            //     supplylistsearch="";
            // }
            // if(GUILayout.Button("测试塔"))
            // {
            //     supplylistsearch="测试塔";
            // }
            // if(GUILayout.Button("守护者"))
            // {
            //     supplylistsearch="守护者";
            // }
            //
            // GUILayout.EndHorizontal();
            //GUILayout.Space(10);

            supplyListScrollPos = EditorGUILayout.BeginScrollView(supplyListScrollPos);
            for (int i = 0; i < _supplyList.Count; i++)
            {
                var hide = false;
                if (supplylistsearch != "")
                {
                    hide = true;
                    if (_supplyList[i].Value.Contains(supplylistsearch))
                    {
                        hide = false;
                    }
                    if (_supplyList[i].Key.ToString().Contains(supplylistsearch))
                    {
                        hide = false;
                    }
                }
                if (hide)
                {
                    continue;
                }
                var item = _supplyList[i];
                GUILayout.BeginHorizontal();
                GUILayout.Space(5);


                if (GUILayout.Button("强制触发", GUILayout.Width(60)))
                {
                    DebugNotify("dn_add_supply", (int)item.Key);
                    GUIUtility.systemCopyBuffer = item.Value ?? string.Empty;
                }

                GUILayout.Label($"{item.Key} : {item.Value}", GUILayout.Height(20));

                GUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
            // if (GUILayout.Button("Load"))
            // {
            //     LoadTask();
            // }
        }

        private void LoadSupplyLogicList()
        {
            var list = DebugNotify("dn_GetSupplyLogicList") as List<KeyValuePair<int, string>>;
            _supplyList = list ?? _supplyList;
        }
    }
}
#endif