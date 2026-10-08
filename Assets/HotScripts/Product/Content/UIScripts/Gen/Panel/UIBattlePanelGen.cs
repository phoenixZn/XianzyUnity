

using UnityEngine.UI;
using System;


namespace Xease.UI
{
    public partial class UIBattlePanel
    {
        #region UIElement声明
        
        [NonSerialized]
        public Image ExitImg;
        [NonSerialized]
        public Button ExitBtn;
        #endregion

        public override void BindUI()
        {
            base.BindUI();

            #region 初始化UIElement
            
            ExitImg = this.transform.Find("Root/t_Exit").GetComponent<Image>();
            ExitBtn = this.transform.Find("Root/t_Exit").GetComponent<Button>();
            #endregion
        }

        public override void RegisterEvents()
        {
            base.RegisterEvents();
            #region 注册事件
            
            UIEventListener.OnClick(ExitBtn.gameObject).AddListener(OnClickExitBtn);
            #endregion
        }
        
        public override void UnRegisterEvents()
        {
            base.UnRegisterEvents();
            #region 反注册事件
            
            UIEventListener.OnClick(ExitBtn.gameObject).RemoveListener(OnClickExitBtn);
            #endregion
        }
    }
}
