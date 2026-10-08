
using UnityEngine.UI;
using System;
using TMPro;


namespace Xease.UI
{
    public partial class MainTopView
    {
        #region UIElement声明
        
        [NonSerialized]
        public TextMeshProUGUI NameTxt;
        [NonSerialized]
        public Image CurExpImg;
        [NonSerialized]
        public TextMeshProUGUI LvlTxt;
        [NonSerialized]
        public TextMeshProUGUI Lvl1Txt;
        [NonSerialized]
        public Image LvlBgImg;
        [NonSerialized]
        public Image CoinIconImg;
        [NonSerialized]
        public TextMeshProUGUI CoinCountTxt;
        [NonSerialized]
        public Image CoinAddImg;
        [NonSerialized]
        public Button CoinAddBtn;
        [NonSerialized]
        public Image DiamondIconImg;
        [NonSerialized]
        public TextMeshProUGUI DiamondCountTxt;
        [NonSerialized]
        public Image DiamondAddImg;
        [NonSerialized]
        public Button DiamondAddBtn;
        [NonSerialized]
        public Image EnergyBarImg;
        [NonSerialized]
        public Image EnergyIconImg;
        [NonSerialized]
        public TextMeshProUGUI EnergyCountTxt;
        [NonSerialized]
        public Image EnergyAddImg;
        [NonSerialized]
        public Button EnergyAddBtn;
        #endregion

        public override void BindUI()
        {
            base.BindUI();

            #region 初始化UIElement
            
            NameTxt = this.transform.Find("Head/PlayerName/t_Name").GetComponent<TextMeshProUGUI>();
            CurExpImg = this.transform.Find("Head/Exp/t_CurExp").GetComponent<Image>();
            LvlTxt = this.transform.Find("Head/t_LvlBg/t_Lvl").GetComponent<TextMeshProUGUI>();
            Lvl1Txt = this.transform.Find("Head/t_LvlBg/t_Lvl1").GetComponent<TextMeshProUGUI>();
            LvlBgImg = this.transform.Find("Head/t_LvlBg").GetComponent<Image>();
            CoinIconImg = this.transform.Find("Coin/t_CoinIcon").GetComponent<Image>();
            CoinCountTxt = this.transform.Find("Coin/t_CoinCount").GetComponent<TextMeshProUGUI>();
            CoinAddImg = this.transform.Find("Coin/t_CoinAdd").GetComponent<Image>();
            CoinAddBtn = this.transform.Find("Coin/t_CoinAdd").GetComponent<Button>();
            DiamondIconImg = this.transform.Find("Diamond/t_DiamondIcon").GetComponent<Image>();
            DiamondCountTxt = this.transform.Find("Diamond/t_DiamondCount").GetComponent<TextMeshProUGUI>();
            DiamondAddImg = this.transform.Find("Diamond/t_DiamondAdd").GetComponent<Image>();
            DiamondAddBtn = this.transform.Find("Diamond/t_DiamondAdd").GetComponent<Button>();
            EnergyBarImg = this.transform.Find("Power/t_EnergyBar").GetComponent<Image>();
            EnergyIconImg = this.transform.Find("Power/t_EnergyIcon").GetComponent<Image>();
            EnergyCountTxt = this.transform.Find("Power/t_EnergyCount").GetComponent<TextMeshProUGUI>();
            EnergyAddImg = this.transform.Find("Power/t_EnergyAdd").GetComponent<Image>();
            EnergyAddBtn = this.transform.Find("Power/t_EnergyAdd").GetComponent<Button>();
            #endregion
        }

        public override void RegisterEvents()
        {
            base.RegisterEvents();
            #region 注册事件
            
            UIEventListener.OnClick(CoinAddBtn.gameObject).AddListener(OnClickCoinAddBtn);
            UIEventListener.OnClick(DiamondAddBtn.gameObject).AddListener(OnClickDiamondAddBtn);
            UIEventListener.OnClick(EnergyAddBtn.gameObject).AddListener(OnClickEnergyAddBtn);
            #endregion
        }
        
        public override void UnRegisterEvents()
        {
            base.UnRegisterEvents();
            #region 反注册事件
            
            UIEventListener.OnClick(CoinAddBtn.gameObject).RemoveListener(OnClickCoinAddBtn);
            UIEventListener.OnClick(DiamondAddBtn.gameObject).RemoveListener(OnClickDiamondAddBtn);
            UIEventListener.OnClick(EnergyAddBtn.gameObject).RemoveListener(OnClickEnergyAddBtn);
            #endregion
        }
    }
}
