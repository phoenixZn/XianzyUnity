
using System.Collections;
using UnityEngine;


namespace Xease.UI
{
    [UIBaseHandler(UIViewName.MainTopView)]
    public partial class MainTopView : UIViewBase
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
        

        
        public void OnClickCoinAddBtn(GameObject obj)
        {
            
        }
        
        
        public void OnClickDiamondAddBtn(GameObject obj)
        {
            
        }
        
        
        public void OnClickEnergyAddBtn(GameObject obj)
        {
            
        }
        
        

    }
}
