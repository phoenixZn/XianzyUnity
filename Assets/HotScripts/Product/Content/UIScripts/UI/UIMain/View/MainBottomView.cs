
using System.Collections;
using UnityEngine;


namespace Xease.UI
{
    [UIBaseHandler(UIViewName.MainBottomView)]
    public partial class MainBottomView : UIViewBase
    {
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
        

        
        public void OnClickShopBtn(GameObject obj)
        {
            
        }
        
        
        public void OnClickCardsBtn(GameObject obj)
        {
            
        }
        
        
        public void OnClickBattleBtn(GameObject obj)
        {
            
        }
        
        
        public void OnClickTalentBtn(GameObject obj)
        {
            
        }
        
        
        public void OnClickChallengeBtn(GameObject obj)
        {
            
        }
        
        

    }
}
