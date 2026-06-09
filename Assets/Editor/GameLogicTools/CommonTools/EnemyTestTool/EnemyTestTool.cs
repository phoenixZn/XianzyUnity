#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Vector2 = UnityEngine.Vector2;

namespace IGTools
{
    public class EnemyTestTool : EditorRuntimeToolBase
    {
        bool enemyFoldout = true;
        bool enemyCustomFoldout = true;
        string enemylistsearch = "";

        public EnemyTestTool(EditorWindow ownerWindow) : base(ownerWindow)
        {
        }
        
        public override void InitTool()
        {
            ToolName = "怪物测试";
        }
        
        public override void DrawTool()
        {
            DrawCombatTest();
        }

        private void DrawCombatTest()
        {
            GUILayout.Space(10);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUIStyle rich = new GUIStyle(EditorStyles.wordWrappedLabel) { richText = true };
            EditorGUILayout.LabelField(
                "EnemyTestTool.cs",
                rich);
            EditorGUILayout.EndVertical();
            
            DrawFoldout_EnemyCustom();
            
            DrawFoldout_Enemy();

            DrawEnemyList();

            GUILayout.Space(10);
            
        }


        //////////////////////////////////////////////////////////////////////////
        private void LoadCfgs_Enemy()
        {
            var list = DebugNotify("dn_GetEnemyList") as List<KeyValuePair<uint, string>>;
            enemyList = list ?? enemyList;
        }
        private void DrawFoldout_Enemy()
        {
            enemyFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(enemyFoldout, "敌人");
            if (enemyFoldout)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("怪物ID:", GUILayout.Width(60));
                _monsterId = EditorGUILayout.IntField(_monsterId);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("怪物等级:", GUILayout.Width(60));
                _monsterLevel = EditorGUILayout.IntField(_monsterLevel);
                GUILayout.EndHorizontal();
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("生成单个敌人", GUILayout.Height(40)))
                {
                    //ExecCommand($"spawnmonster {monsterId} 1 {defRatio} 0 {hpRatio} {atkRatio}");
                    CreateEnemy(_monsterId, 1);
                }
                GUILayout.Space(50);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("生成10个敌人", GUILayout.Height(40)))
                {
                    //ExecCommand($"spawnmonster {monsterId} 1 {defRatio} 0 {hpRatio} {atkRatio}");
                    CreateEnemy(_monsterId, 10);
                }
                GUILayout.Space(50);
                GUILayout.EndHorizontal();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        
        
        //////////////////////////////////////////////////////////////////////////
        private Vector2 enemyListScrollPos= Vector2.zero;
        private List<KeyValuePair<uint, string>> enemyList = new List<KeyValuePair<uint, string>>();
        int _monsterId = 40;
        int _monsterLevel = 1;
        double _fixHp = 300f;
        float _fixAtk = 100f;
        float _fixDef = 50f;
        
        private bool isUseCustomEnemy = false;
        private void DrawFoldout_EnemyCustom()
        {
            enemyCustomFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(enemyCustomFoldout, "敌人定制");
            if (enemyCustomFoldout)
            {
                GUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                isUseCustomEnemy = EditorGUILayout.Toggle("启用定制属性_负值无效", isUseCustomEnemy);
                // // 检测勾选框变化
                // if (EditorGUI.EndChangeCheck())
                // {
                //     // 执行相应的操作
                // }
                GUILayout.EndHorizontal();

                EditorGUI.BeginDisabledGroup(!isUseCustomEnemy);
                GUILayout.BeginHorizontal();
                GUILayout.Label("指定血量:", GUILayout.Width(60));
                _fixHp = EditorGUILayout.DoubleField(_fixHp);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("指定攻击:", GUILayout.Width(60));
                _fixAtk = EditorGUILayout.FloatField(_fixAtk);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("指定防御:", GUILayout.Width(60));
                _fixDef = EditorGUILayout.FloatField(_fixDef);
                GUILayout.EndHorizontal();
                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void CreateEnemy(int monsterId, int count)
        {
            if (isUseCustomEnemy)
            {
                DebugNotify("dn_spawn_monster", (int) monsterId, count, _monsterLevel, _fixHp, _fixAtk, _fixDef);
            }
            else
            {
                DebugNotify("dn_spawn_monster", (int) monsterId, count, _monsterLevel);
            }
        }
        
        private void DrawEnemyList()
        {
            if (GUILayout.Button("加载敌人配置 (Config)", GUILayout.Height(40)))
            {
                LoadCfgs_Enemy();
            }
            if (enemyList.Count == 0)
            {
                EditorGUILayout.HelpBox("请在游戏运行后，刷新配置", MessageType.Info);
                return;
            }

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("搜索：", GUILayout.Width(30));
            enemylistsearch = GUILayout.TextField(enemylistsearch);
            if (GUILayout.Button("Clear", GUILayout.Width(50)))
            {
                enemylistsearch = "";
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            enemyListScrollPos = EditorGUILayout.BeginScrollView(enemyListScrollPos);
            for (int i = 0; i < enemyList.Count; i++)
            {
                var hide = false;
                if (enemylistsearch != "")
                {
                    hide = true;
                    if (enemyList[i].Value.Contains(enemylistsearch))
                    {
                        hide = false;
                    }
                    if (enemyList[i].Key.ToString().Contains(enemylistsearch))
                    {
                        hide = false;
                    }
                }
                if (hide)
                {
                    continue;
                }
                var enemy = enemyList[i];
                GUILayout.BeginHorizontal();
                if (GUILayout.Button($"选择", GUILayout.Width(50)))
                {
                    _monsterId = (int)enemy.Key;
                }

                if (GUILayout.Button($"直接生成", GUILayout.Width(60)))
                {
                    CreateEnemy((int)enemy.Key, 1);
                }
                

                GUILayout.Label($"{enemy.Key} : {enemy.Value}", GUILayout.Height(20));

                GUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
            // if (GUILayout.Button("Load"))
            // {
            //     LoadTask();
            // }
        }
        
    }
}

#endif