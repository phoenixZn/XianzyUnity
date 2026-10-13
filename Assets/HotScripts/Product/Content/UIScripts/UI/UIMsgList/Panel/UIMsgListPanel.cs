
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;
using System;
using TMPro;
using UnityEngine;
using Xease.UI;


namespace Xease.UI
{
    [UIBaseHandler(UIPanelName.UIMsgList)]
    public partial class UIMsgListPanel : UIPanelBase
    {
        private LoopScroll<MsgData, UIMsgItem> _mailLoop;
        
        public override void InitializeParams(UIParams uiParam)
        {
            base.InitializeParams(uiParam);
            layerId = UILayerId.Pop;
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
            
            _mailLoop = new LoopScroll<MsgData, UIMsgItem>(Scroller, this);
            _mailLoop.SetClickFunc(OnMailClick);
            
            noDataTipsTxt.gameObject.SetActive(false);
            confirmBtnBtn.gameObject.SetActive(false);

            RefreshMailList();
        }

        public override void onHide()
        {
            base.onHide();
            _mailLoop.ClearList();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
        }
        

        
        public void OnClickbreakBtnBtn(GameObject obj)
        {
            Hide();
        }
        
        
        public void OnClickcloseBtnBtn(GameObject obj)
        {
            Hide();
        }
        
        
        public void OnClickconfirmBtnBtn(GameObject obj)
        {
            Hide();
        }
        
        
        private void OnMailClick(object obj)
        {
            var data = obj as MsgData;
            G.Log("OnMailClick");
        }
        
        private void RefreshMailList()
        {
            var list = new List<MsgData>(8);
            list.Add(new MsgData() {DisplayTitle = "AAAA", Timestamp = "2025-11-1", EndTime = "2025-11-10"});
            list.Add(new MsgData() {DisplayTitle = "BBBB", Timestamp = "2026-11-2", EndTime = "2026-12-15"});
            _mailLoop.Refill(list);
            noDataTipsTxt.gameObject.SetActive(list.Count <= 0);
            var hasCanReceiveMail = true;
            confirmBtnBtn.gameObject.SetActive(hasCanReceiveMail);
        }
        
    }
}
