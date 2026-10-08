using UnityEngine.UI;
using System;
using TMPro;
using UnityEngine;


namespace Xease.UI
{
    public partial class UIMainPanel
    {
        #region UIElement声明
        
        [NonSerialized]
        public Image SelectionBtnImg;
        [NonSerialized]
        public TextMeshProUGUI LevelIndexTxt;
        [NonSerialized]
        public TextMeshProUGUI LevelNameTxt;
        [NonSerialized]
        public TextMeshProUGUI LevelDesTxt;
        [NonSerialized]
        public Image TestImg;
        [NonSerialized]
        public Button TestBtn;
        [NonSerialized]
        public Image ChallengeImageImg;
        [NonSerialized]
        public TextMeshProUGUI ChallengeTextTxt;
        [NonSerialized]
        public Image ChallengeImg;
        [NonSerialized]
        public Button ChallengeBtn;
        [NonSerialized]
        public Image DailyactivitiesImageImg;
        [NonSerialized]
        public TextMeshProUGUI DailyactivitiesTextTxt;
        [NonSerialized]
        public Image DailyactivitiesImg;
        [NonSerialized]
        public Button DailyactivitiesBtn;
        [NonSerialized]
        public Image SettingImg;
        [NonSerialized]
        public Button SettingBtn;
        [NonSerialized]
        public TextMeshProUGUI CostTxt;
        [NonSerialized]
        public TextMeshProUGUI StartTitleTxt;
        [NonSerialized]
        public Image CostIconImg;
        [NonSerialized]
        public Image StartBtnImg;
        [NonSerialized]
        public Button StartBtnBtn;
        [NonSerialized]
        public GameObject Battle;
        #endregion

        public override void BindUI()
        {
            base.BindUI();

            #region 初始化UIElement
            
            SelectionBtnImg = this.transform.Find("Root/t_Battle/t_SelectionBtn").GetComponent<Image>();
            LevelIndexTxt = this.transform.Find("Root/t_Battle/t_LevelIndex").GetComponent<TextMeshProUGUI>();
            LevelNameTxt = this.transform.Find("Root/t_Battle/t_LevelName").GetComponent<TextMeshProUGUI>();
            LevelDesTxt = this.transform.Find("Root/t_Battle/t_LevelDes").GetComponent<TextMeshProUGUI>();
            TestImg = this.transform.Find("Root/t_Battle/t_Test").GetComponent<Image>();
            TestBtn = this.transform.Find("Root/t_Battle/t_Test").GetComponent<Button>();
            ChallengeImageImg = this.transform.Find("Root/t_Battle/t_Challenge/t_ChallengeImage").GetComponent<Image>();
            ChallengeTextTxt = this.transform.Find("Root/t_Battle/t_Challenge/t_ChallengeText").GetComponent<TextMeshProUGUI>();
            ChallengeImg = this.transform.Find("Root/t_Battle/t_Challenge").GetComponent<Image>();
            ChallengeBtn = this.transform.Find("Root/t_Battle/t_Challenge").GetComponent<Button>();
            DailyactivitiesImageImg = this.transform.Find("Root/t_Battle/t_Dailyactivities/t_DailyactivitiesImage").GetComponent<Image>();
            DailyactivitiesTextTxt = this.transform.Find("Root/t_Battle/t_Dailyactivities/t_DailyactivitiesText").GetComponent<TextMeshProUGUI>();
            DailyactivitiesImg = this.transform.Find("Root/t_Battle/t_Dailyactivities").GetComponent<Image>();
            DailyactivitiesBtn = this.transform.Find("Root/t_Battle/t_Dailyactivities").GetComponent<Button>();
            SettingImg = this.transform.Find("Root/t_Battle/t_Setting").GetComponent<Image>();
            SettingBtn = this.transform.Find("Root/t_Battle/t_Setting").GetComponent<Button>();
            CostTxt = this.transform.Find("Root/t_Battle/t_StartBtn/t_Cost").GetComponent<TextMeshProUGUI>();
            StartTitleTxt = this.transform.Find("Root/t_Battle/t_StartBtn/t_StartTitle").GetComponent<TextMeshProUGUI>();
            CostIconImg = this.transform.Find("Root/t_Battle/t_StartBtn/t_CostIcon").GetComponent<Image>();
            StartBtnImg = this.transform.Find("Root/t_Battle/t_StartBtn").GetComponent<Image>();
            StartBtnBtn = this.transform.Find("Root/t_Battle/t_StartBtn").GetComponent<Button>();
            Battle = this.transform.Find("Root/t_Battle").gameObject;
            #endregion
        }

        public override void RegisterEvents()
        {
            base.RegisterEvents();
            #region 注册事件
            var tr = UIEventListener.OnClick(TestBtn.gameObject);
            if (tr != null)
            {
                tr.AddListener(OnClickTestBtn);    
            }
            else
            {
                Debug.Log("UIEventListener.OnClick(TestBtn.gameObject) == null");
            }
            
            UIEventListener.OnClick(ChallengeBtn.gameObject).AddListener(OnClickChallengeBtn);
            UIEventListener.OnClick(DailyactivitiesBtn.gameObject).AddListener(OnClickDailyactivitiesBtn);
            UIEventListener.OnClick(SettingBtn.gameObject).AddListener(OnClickSettingBtn);
            UIEventListener.OnClick(StartBtnBtn.gameObject).AddListener(OnClickStartBtnBtn);
            #endregion
        }
        
        public override void UnRegisterEvents()
        {
            base.UnRegisterEvents();
            #region 反注册事件
            
            // UIEventListener.OnClick(TestBtn.gameObject).RemoveListener(OnClickTestBtn);
            // UIEventListener.OnClick(ChallengeBtn.gameObject).RemoveListener(OnClickChallengeBtn);
            // UIEventListener.OnClick(DailyactivitiesBtn.gameObject).RemoveListener(OnClickDailyactivitiesBtn);
            // UIEventListener.OnClick(SettingBtn.gameObject).RemoveListener(OnClickSettingBtn);
            // UIEventListener.OnClick(StartBtnBtn.gameObject).RemoveListener(OnClickStartBtnBtn);
            #endregion
        }
    }
}
