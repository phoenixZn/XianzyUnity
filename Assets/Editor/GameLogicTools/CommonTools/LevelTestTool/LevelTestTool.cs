#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using GameLogicToolData;


namespace IGTools
{
    public class LevelTestTool : EditorRuntimeToolBase
    {
        bool levelFoldout = true;
        bool gameFoldout = true;
        private Vector2 _scrollPos;
        public LevelTestTool(EditorWindow ownerWindow) : base(ownerWindow)
        {

        }
        
        public override void InitTool()
        {
            ToolName = "关卡测试";
            
            // 初始化测试数据默认值
            _testInitData.LevelCfgTid = 2;
            _testInitData.PlayerLevel = 10;
            _testInitData.Units = new List<GameLogicToolData.TestUnitData>();
            _testInitData.Units.Add(new GameLogicToolData.TestUnitData()
            {
                FighterTid = 1701,
                UintTid = 109,
            });
        }
         
        public override void DrawTool()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandHeight(true));
            DrawCombatTest();
            EditorGUILayout.EndScrollView();
        }

        private void DrawCombatTest()
        {
            GUILayout.Space(10);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("LevelTestTool.cs");
            EditorGUILayout.EndVertical();

            DrawFoldout_LevelChoose();

            DrawFoldout_TimeScale();
            
            DrawFoldout_LogDev();

            DrawFoldout_Result();

            DrawFoldout_Custom();

            GUILayout.Space(10);
        }

        //////////////////////////////////////////////////////////////////////////
        #region 关卡
        //////////////////////////////////////////////////////////////////////////
        int _levelIndex = 0;
        string[] _levelNames={
            "手动输入",
            "调试专用关卡",
        };
        int[] _levelTidArray={
            0,
            -1001,
        };
        private void LoadCfgs_Level()
        {
            var list = DebugNotify("dn_GetLevelCfgList") as List<KeyValuePair<int,string>>;
            _levelNames = new string[list.Count];
            _levelTidArray = new int[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                var kv = list[i];
                _levelNames[i] = kv.Value;
                _levelTidArray[i] = kv.Key;
            }
            if (list.Count > 0)
            {
                _levelIndex = 0;
                _testInitData.LevelCfgTid = _levelTidArray[_levelIndex];
            }
        }

        private bool isUseCustomLv = true;
        private TestGameInitData _testInitData = new TestGameInitData();
        
        /// <summary>
        /// 克隆测试数据
        /// </summary>
        private TestGameInitData CloneTestData()
        {
            return new TestGameInitData
            {
                LevelCfgTid = _testInitData.LevelCfgTid,
                PlayerLevel = _testInitData.PlayerLevel,
                OverrideLevelLogicID = _testInitData.OverrideLevelLogicID,
                OverrideModeLogicID = _testInitData.OverrideModeLogicID,
                Units = _testInitData.Units != null ? new List<GameLogicToolData.TestUnitData>(_testInitData.Units) : null,
            };
        }

        private void DrawFoldout_LevelChoose()
        {
            levelFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(levelFoldout, "地图");
            if (levelFoldout)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUIStyle rich = new GUIStyle(EditorStyles.wordWrappedLabel) { richText = true };
                EditorGUILayout.LabelField(
                    "- 请在游戏运行后，刷新配置\n- <b>指定英雄等级</b>设为 -1 时不生成英雄",
                    rich);
                EditorGUILayout.EndVertical(); 
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("加载关卡配置 (Config)", GUILayout.Height(40)))
                {
                    LoadCfgs_Level();
                    Debug.Log("刷新关卡配置");
                }
                GUILayout.EndHorizontal();
                
                GUILayout.Space(20);
                GUILayout.BeginHorizontal();
                GUILayout.Label("关卡:", GUILayout.Width(60));
                bool locklevelIndex = _levelIndex != 0;
                if (locklevelIndex)
                {
                    GUILayout.Label(_testInitData.LevelCfgTid.ToString());
                }
                else
                {
                    _testInitData.LevelCfgTid = EditorGUILayout.IntField(_testInitData.LevelCfgTid);
                }

                _levelIndex = EditorGUILayout.Popup(_levelIndex, _levelNames);
                if (EditorGUI.EndChangeCheck() && _levelIndex != 0)
                {
                    _testInitData.LevelCfgTid = _levelTidArray[_levelIndex];
                }
                GUILayout.EndHorizontal();
                
                
                GUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                isUseCustomLv = EditorGUILayout.Toggle("启用自定义玩家属性", isUseCustomLv);
                GUILayout.EndHorizontal();

                EditorGUI.BeginDisabledGroup(!isUseCustomLv);
                
                GUILayout.BeginHorizontal();
                GUILayout.Label("指定PlayerLevel:", GUILayout.Width(100));
                _testInitData.PlayerLevel = EditorGUILayout.IntField(_testInitData.PlayerLevel);
                GUILayout.EndHorizontal();

                
                
                EditorGUI.EndDisabledGroup();
                
                GUILayout.Space(20);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("进入关卡", GUILayout.Height(40)))
                {
                    //ExecCommand("entertest " + mapid.ToString());
                    var d1 = CloneTestData();
                    d1.OverrideLevelLogicID = -1;
                    SendDebugToolCmd($"cmdForceEnterInGameTest", d1);
                }

                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("进入调试关卡A", GUILayout.Height(40)))
                {
                    var d1 = CloneTestData();
                    d1.OverrideLevelLogicID = 2000101;
                    SendDebugToolCmd("cmdForceEnterInGameTest", d1);
                }
                
                if (GUILayout.Button("进入调试关卡B", GUILayout.Height(40)))
                {
                    var d1 = CloneTestData();
                    d1.OverrideLevelLogicID = 2000101;
                    d1.OverrideModeLogicID = 1000103;
                    SendDebugToolCmd("cmdForceEnterInGameTest", d1);
                }
                GUILayout.EndHorizontal();


                GUILayout.Space(20);
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        #endregion

        

        //////////////////////////////////////////////////////////////////////////
        float gameSpeed = 1f;
        bool timeScaleFoldout = true;

        private void DrawFoldout_TimeScale()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.EndHorizontal();
            timeScaleFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(timeScaleFoldout, "时间缩放");
            if (timeScaleFoldout)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("x0.1", GUILayout.Height(30), GUILayout.Width(60)))
                {
                    gameSpeed = 0.1f;
                    ChangeGameSpeed(gameSpeed);
                }
                if (GUILayout.Button("x0.5", GUILayout.Height(30), GUILayout.Width(60)))
                {
                    gameSpeed = 0.5f;
                    ChangeGameSpeed(gameSpeed);
                }
                if (GUILayout.Button("x1", GUILayout.Height(30), GUILayout.Width(60)))
                {
                    gameSpeed = 1;
                    ChangeGameSpeed(gameSpeed);
                }
                if (GUILayout.Button("x1.5", GUILayout.Height(30), GUILayout.Width(60)))
                {
                    gameSpeed = 1.5f;
                    ChangeGameSpeed(gameSpeed);
                }
                if (GUILayout.Button("x2", GUILayout.Height(30), GUILayout.Width(60)))
                {
                    gameSpeed = 2;
                    ChangeGameSpeed(gameSpeed);
                }
                if (GUILayout.Button("x3", GUILayout.Height(30), GUILayout.Width(60)))
                {
                    gameSpeed = 3;
                    ChangeGameSpeed(gameSpeed);
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                GUILayout.Label("游戏速度:", GUILayout.Width(60));
                gameSpeed = EditorGUILayout.FloatField(gameSpeed);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("更改", GUILayout.Height(40), GUILayout.Width(200)))
                {
                    ChangeGameSpeed(gameSpeed);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.Space(20);
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void ChangeGameSpeed(float speed)
        {
            DebugNotify("dn_change_game_speed", speed);
        }


        //////////////////////////////////////////////////////////////////////////
        /// 自定义
        private void DrawFoldout_Custom()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            for (int i = 0; i < 6; i++)
            {
                if (GUILayout.Button($"{i}", GUILayout.Height(30), GUILayout.Width(60)))
                {
                    SendDebugToolCmd("cmdCustomTest", i);
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
            
            GUILayout.Space(20);
            EditorGUILayout.EndFoldoutHeaderGroup();
        }


        
        //////////////////////////////////////////////////////////////////////////
        /// 日志
        private bool openLogDev = false;
        bool logDevFoldout = true;
        private void DrawFoldout_LogDev()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.EndHorizontal();
            logDevFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(logDevFoldout, "开启DevLog");
            if (logDevFoldout)
            {
                GUILayout.BeginHorizontal();
                openLogDev = EditorGUILayout.Toggle("开启", openLogDev);
                GUILayout.EndHorizontal();
                GUILayout.Space(10);
            }

            GUILayout.Space(20);
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        
        //////////////////////////////////////////////////////////////////////////
        /// 结算
        bool resultFoldout = true;
        
        private void DrawFoldout_Result()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.EndHorizontal();
            resultFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(resultFoldout, "结算");

            if (resultFoldout)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("胜利", GUILayout.Height(30), GUILayout.Width(60)))
                {
                    DebugNotify("dn_win");
                }
                if (GUILayout.Button("失败", GUILayout.Height(30), GUILayout.Width(60)))
                {
                    DebugNotify("dn_lose");
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(10);
            }
            GUILayout.Space(20);
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }
}

#endif
