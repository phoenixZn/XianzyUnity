
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;
using System;
using TMPro;
using UnityEngine;
using Xease.UI;


namespace Xease.UI
{
    public partial class UIMsgListPanel
    {
        #region UIElement声明
        
        [NonSerialized]
        public Image breakBtnImg;
        [NonSerialized]
        public Button breakBtnBtn;
        [NonSerialized]
        public Image closeBtnImg;
        [NonSerialized]
        public Button closeBtnBtn;
        [NonSerialized]
        public TextMeshProUGUI noDataTipsTxt;
        [NonSerialized]
        public Image ScrollerImg;
        [NonSerialized]
        public ScrollerPro Scroller;
        [NonSerialized]
        public Image confirmBtnImg;
        [NonSerialized]
        public Button confirmBtnBtn;
        #endregion

        public override void BindUI()
        {
            base.BindUI();

            #region 初始化UIElement
            
            breakBtnImg = this.transform.Find("t_breakBtn").GetComponent<Image>();
            breakBtnBtn = this.transform.Find("t_breakBtn").GetComponent<Button>();
            closeBtnImg = this.transform.Find("Scale/t_closeBtn").GetComponent<Image>();
            closeBtnBtn = this.transform.Find("Scale/t_closeBtn").GetComponent<Button>();
            noDataTipsTxt = this.transform.Find("Scale/bg2/t_noDataTips").GetComponent<TextMeshProUGUI>();
            ScrollerImg = this.transform.Find("Scale/bg2/t_Scroller").GetComponent<Image>();
            Scroller = this.transform.Find("Scale/bg2/t_Scroller").GetComponent<ScrollerPro>();
            confirmBtnImg = this.transform.Find("Scale/t_confirmBtn").GetComponent<Image>();
            confirmBtnBtn = this.transform.Find("Scale/t_confirmBtn").GetComponent<Button>();
            #endregion
        }

        public override void RegisterEvents()
        {
            base.RegisterEvents();
            #region 注册事件
            
            UIEventListener.OnClick(breakBtnBtn.gameObject).AddListener(OnClickbreakBtnBtn);
            UIEventListener.OnClick(closeBtnBtn.gameObject).AddListener(OnClickcloseBtnBtn);
            UIEventListener.OnClick(confirmBtnBtn.gameObject).AddListener(OnClickconfirmBtnBtn);
            #endregion
        }
        
        public override void UnRegisterEvents()
        {
            base.UnRegisterEvents();
            #region 反注册事件
            
            UIEventListener.OnClick(breakBtnBtn.gameObject).RemoveListener(OnClickbreakBtnBtn);
            UIEventListener.OnClick(closeBtnBtn.gameObject).RemoveListener(OnClickcloseBtnBtn);
            UIEventListener.OnClick(confirmBtnBtn.gameObject).RemoveListener(OnClickconfirmBtnBtn);
            #endregion
        }
    }
}
