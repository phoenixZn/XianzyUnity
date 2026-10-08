
using System.Collections.Generic;
using System.Collections;
using UnityEngine;



namespace Xease.UI
{
    [UIBaseHandler(UIPanelName.UIMain)]
    public partial class UIMainPanel : UIPanelBase
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
        

        
        public void OnClickTestBtn(GameObject obj)
        {
            
        }
        
        
        public void OnClickChallengeBtn(GameObject obj)
        {
            
        }
        
        
        public void OnClickDailyactivitiesBtn(GameObject obj)
        {
            
        }
        
        
        public void OnClickSettingBtn(GameObject obj)
        {
            
        }
        
        
        public void OnClickStartBtnBtn(GameObject obj)
        {
            Hide();
            GEnv.Inst.EnvStateMng.ChangeEnvState(EnvStateID.ES_Battle);
        }
        
        
    }
}
