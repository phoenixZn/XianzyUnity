using UnityEngine.UI;
using System;
using TMPro;


namespace Xease.UI
{
    public partial class MainBottomView
    {
        #region UIElement声明
        
        [NonSerialized]
        public Image ShopNoSelectImg;
        [NonSerialized]
        public Image ShopIconImg;
        [NonSerialized]
        public TextMeshProUGUI ShopNameTxt;
        [NonSerialized]
        public Image ShopSelectImg;
        [NonSerialized]
        public Button ShopBtn;
        [NonSerialized]
        public Image CardsNoSelectImg;
        [NonSerialized]
        public Image CardsIconImg;
        [NonSerialized]
        public TextMeshProUGUI CardsNameTxt;
        [NonSerialized]
        public Image CardsSelectImg;
        [NonSerialized]
        public Button CardsBtn;
        [NonSerialized]
        public Image BattleNoSelectImg;
        [NonSerialized]
        public Image BattleIconImg;
        [NonSerialized]
        public TextMeshProUGUI BattleNameTxt;
        [NonSerialized]
        public Image BattleSelectImg;
        [NonSerialized]
        public Button BattleBtn;
        [NonSerialized]
        public Image TalentNoSelectImg;
        [NonSerialized]
        public Image TalentIconImg;
        [NonSerialized]
        public TextMeshProUGUI TalentNameTxt;
        [NonSerialized]
        public Image TalentSelectImg;
        [NonSerialized]
        public Button TalentBtn;
        [NonSerialized]
        public Image ChallengeNoSelectImg;
        [NonSerialized]
        public Image ChallengeIconImg;
        [NonSerialized]
        public TextMeshProUGUI ChallengeNameTxt;
        [NonSerialized]
        public Image ChallengeSelectImg;
        [NonSerialized]
        public Button ChallengeBtn;
        #endregion

        public override void BindUI()
        {
            base.BindUI();

            #region 初始化UIElement
            
            ShopNoSelectImg = this.transform.Find("Btns/t_Shop/t_ShopNoSelect").GetComponent<Image>();
            ShopIconImg = this.transform.Find("Btns/t_Shop/t_ShopSelect/t_ShopIcon").GetComponent<Image>();
            ShopNameTxt = this.transform.Find("Btns/t_Shop/t_ShopSelect/t_ShopName").GetComponent<TextMeshProUGUI>();
            ShopSelectImg = this.transform.Find("Btns/t_Shop/t_ShopSelect").GetComponent<Image>();
            ShopBtn = this.transform.Find("Btns/t_Shop").GetComponent<Button>();
            CardsNoSelectImg = this.transform.Find("Btns/t_Cards/t_CardsNoSelect").GetComponent<Image>();
            CardsIconImg = this.transform.Find("Btns/t_Cards/t_CardsSelect/t_CardsIcon").GetComponent<Image>();
            CardsNameTxt = this.transform.Find("Btns/t_Cards/t_CardsSelect/t_CardsName").GetComponent<TextMeshProUGUI>();
            CardsSelectImg = this.transform.Find("Btns/t_Cards/t_CardsSelect").GetComponent<Image>();
            CardsBtn = this.transform.Find("Btns/t_Cards").GetComponent<Button>();
            BattleNoSelectImg = this.transform.Find("Btns/t_Battle/t_BattleNoSelect").GetComponent<Image>();
            BattleIconImg = this.transform.Find("Btns/t_Battle/t_BattleSelect/t_BattleIcon").GetComponent<Image>();
            BattleNameTxt = this.transform.Find("Btns/t_Battle/t_BattleSelect/t_BattleName").GetComponent<TextMeshProUGUI>();
            BattleSelectImg = this.transform.Find("Btns/t_Battle/t_BattleSelect").GetComponent<Image>();
            BattleBtn = this.transform.Find("Btns/t_Battle").GetComponent<Button>();
            TalentNoSelectImg = this.transform.Find("Btns/t_Talent/t_TalentNoSelect").GetComponent<Image>();
            TalentIconImg = this.transform.Find("Btns/t_Talent/t_TalentSelect/t_TalentIcon").GetComponent<Image>();
            TalentNameTxt = this.transform.Find("Btns/t_Talent/t_TalentSelect/t_TalentName").GetComponent<TextMeshProUGUI>();
            TalentSelectImg = this.transform.Find("Btns/t_Talent/t_TalentSelect").GetComponent<Image>();
            TalentBtn = this.transform.Find("Btns/t_Talent").GetComponent<Button>();
            ChallengeNoSelectImg = this.transform.Find("Btns/t_Challenge/t_ChallengeNoSelect").GetComponent<Image>();
            ChallengeIconImg = this.transform.Find("Btns/t_Challenge/t_ChallengeSelect/t_ChallengeIcon").GetComponent<Image>();
            ChallengeNameTxt = this.transform.Find("Btns/t_Challenge/t_ChallengeSelect/t_ChallengeName").GetComponent<TextMeshProUGUI>();
            ChallengeSelectImg = this.transform.Find("Btns/t_Challenge/t_ChallengeSelect").GetComponent<Image>();
            ChallengeBtn = this.transform.Find("Btns/t_Challenge").GetComponent<Button>();
            #endregion
        }

        public override void RegisterEvents()
        {
            base.RegisterEvents();
            #region 注册事件
            
            UIEventListener.OnClick(ShopBtn.gameObject).AddListener(OnClickShopBtn);
            UIEventListener.OnClick(CardsBtn.gameObject).AddListener(OnClickCardsBtn);
            UIEventListener.OnClick(BattleBtn.gameObject).AddListener(OnClickBattleBtn);
            UIEventListener.OnClick(TalentBtn.gameObject).AddListener(OnClickTalentBtn);
            UIEventListener.OnClick(ChallengeBtn.gameObject).AddListener(OnClickChallengeBtn);
            #endregion
        }
        
        public override void UnRegisterEvents()
        {
            base.UnRegisterEvents();
            #region 反注册事件
            
            UIEventListener.OnClick(ShopBtn.gameObject).RemoveListener(OnClickShopBtn);
            UIEventListener.OnClick(CardsBtn.gameObject).RemoveListener(OnClickCardsBtn);
            UIEventListener.OnClick(BattleBtn.gameObject).RemoveListener(OnClickBattleBtn);
            UIEventListener.OnClick(TalentBtn.gameObject).RemoveListener(OnClickTalentBtn);
            UIEventListener.OnClick(ChallengeBtn.gameObject).RemoveListener(OnClickChallengeBtn);
            #endregion
        }
    }
}
