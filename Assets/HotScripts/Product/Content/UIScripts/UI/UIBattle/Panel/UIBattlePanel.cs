
using System.Collections.Generic;
using System.Collections;
using UnityEngine;



namespace Xease.UI
{
    [UIBaseHandler(UIPanelName.UIBattle)]
    public partial class UIBattlePanel : UIPanelBase
    {
        public override void InitializeParams(UIParams uiParam)
        {
            base.InitializeParams(uiParam);
            layerId = UILayerId.Panel;
            // TODO ： 如果没有view，下面这行删除
            ViewNameList ??= new List<string>();
        }
        
        public override IEnumerator Prepare()
        {
            yield break;
        }
        
        public override void onCreate()
        {
            base.onCreate();
        }

        public override void onShow(object[] objs)
        {
            base.onShow(objs);
        }

        public override void onHide()
        {
            base.onHide();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
        }
        

        
        public void OnClickExitBtn(GameObject obj)
        {
            Hide();
            GEnv.Inst.EnvStateMng.ChangeEnvState(EnvStateID.ES_Main);
        }
        
        
    }
}
