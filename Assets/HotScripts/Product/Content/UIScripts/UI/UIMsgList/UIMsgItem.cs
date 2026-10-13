using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;
using System;
using TMPro;
using UnityEngine;

namespace Xease.UI
{
    public class MsgData
    {
        public string DisplayTitle { get; set; }
        public string Timestamp { get; set; } 
        public string EndTime { get; set; } // 过期时间戳
    }
    
    public class UIMsgItem : LoopScrollItem
    {
        private MsgData _data;

        public TextMeshProUGUI titleText;
        public TextMeshProUGUI expiredText;
        public TextMeshProUGUI timeText;

        public override void UpdateData(object obj)
        {
            base.UpdateData(obj);

            _data = obj as MsgData;

            titleText.text = _data.DisplayTitle;
            timeText.text = _data.Timestamp;
            expiredText.text = _data.EndTime;
        }
    }
    
}