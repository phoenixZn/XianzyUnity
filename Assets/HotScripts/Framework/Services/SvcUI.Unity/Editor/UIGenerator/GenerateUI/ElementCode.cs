namespace Xease.UI
{
    /// <summary>
    /// 各控件字段声明的代码模板。占位符由生成器替换。
    /// </summary>
    public class ElementCodeDefine
    {
        public static string imgCompFormat =
            @"
        [NonSerialized]
        public Image #命名#Img;";
        // public static string imgCompFormat =
        //     @"
        // [NonSerialized]
        // private Image m#命名#Img;
        // public Image #命名#Img
        // {
        //     get { 
        //         if (m#命名#Img == null)
        //             m#命名#Img = this.transform.Find(#路径#).GetComponent<Image>(); 
        //         return m#命名#Img; 
        //     }
        // }";

        public static string imageCtrlCompFormat =
            @"
        [NonSerialized]
        public UIImageCtrl #命名#ImageCtrl;";
    
        public static string textureCtrlCompFormat =
            @"
        [NonSerialized]
        public UITextureCtrl #命名#TextureCtrl;";

        public static string btnCompFormat =
            @"
        [NonSerialized]
        public Button #命名#Btn;";

        public static string rawImgCompFormat =
            @"
        [NonSerialized]
        public RawImage #命名#RawImg;";

        public static string txtCompFormat =
            @"
        [NonSerialized]
        public TextMeshProUGUI #命名#Txt;";

        public static string scrollRectCompFormat =
            @"
        [NonSerialized]
        public LoopScrollRect #命名#ScrollRect;";
    
        // public static string scrollCompFormat =
        //     @"
        // [NonSerialized]
        // public LoopScroll<#命名#ScrollData, #命名#ScrollItem> #命名#Scroll;";
    
        // public static string scrollCellCompFormat =
        //     @"
        // public EnhancedScrollerCellView[] #命名#Cell;";

        public static string togCompFormat =
            @"
        [NonSerialized]
        public Toggle #命名#Tog;";

        public static string inputFieldCompFormat =
            @"
        [NonSerialized]
        public TMP_InputField #命名#;";
    
        // public static string scrollDelegateCompFormat =
        //     @"
        // [NonSerialized]
        // public BaseScrollDelegate #命名#;";
    
        public static string sliderCompFormat =
            @"
        [NonSerialized]
        public Slider #命名#;";
    
        public static string viewBaseCompFormat =
            @"
        [NonSerialized]
        public UIViewBase #命名#;";
        public static string dropDownCompFormat =
            @"
        [NonSerialized]
        public TMP_Dropdown #命名#;";
    
        public static string objCompFormat =
            @"
        [NonSerialized]
        public GameObject #命名#;";
    
        public static string containerCompFormat =
            @"
        [NonSerialized]
        public UIContainer #命名#;";

        public static string scrollerProCompFormat =
            @"
        [NonSerialized]
        public ScrollerPro #命名#;";
    
        public static string ui3DDisplayCompFormat =
            @"
        [NonSerialized]
        public UI3DDisplay #命名#Display;";
    }

    /// <summary>
    /// 各控件 BindUI 赋值语句的代码模板。
    /// </summary>
    public class ElementCodeInitDefine
    {
        public static string imgCompInitFormat =
            @"
            #名字#Img = this.transform.Find(#路径#).GetComponent<Image>();";

        public static string imageCtrlCompInitFormat =
            @"
            #名字#ImageCtrl = this.transform.Find(#路径#).GetComponent<UIImageCtrl>();";

        public static string textureCtrlCompInitFormat =
            @"
            #名字#TextureCtrl = this.transform.Find(#路径#).GetComponent<UITextureCtrl>();";

        public static string btnCompInitFormat =
            @"
            #名字#Btn = this.transform.Find(#路径#).GetComponent<Button>();";

        public static string rawImgCompInitFormat =
            @"
            #名字#RawImg = this.transform.Find(#路径#).GetComponent<RawImage>();";

        public static string txtCompInitFormat =
            @"
            #名字#Txt = this.transform.Find(#路径#).GetComponent<TextMeshProUGUI>();";

        public static string scrollRectCompInitFormat =
            @"
            #名字#ScrollRect = this.transform.Find(#路径#).GetComponent<LoopScrollRect>();";
        // #名字#Scroll = new LoopScroll<#名字#ScrollData, #名字#ScrollItem>(#名字#ScrollRect);

        public static string togCompInitFormat =
            @"
            #名字#Tog = this.transform.Find(#路径#).GetComponent<Toggle>();";

        public static string inputFieldInitFormat =
            @"
            #名字# = this.transform.Find(#路径#).GetComponent<TMP_InputField>();";

        // public static string scrollDelegateInitFormat =
        //     @"
        //     #名字# = this.transform.Find(#路径#).GetComponent<BaseScrollDelegate>();";

        public static string scrollerProInitFormat =
            @"
            #名字# = this.transform.Find(#路径#).GetComponent<ScrollerPro>();";

        public static string sliderInitFormat =
            @"
            #名字# = this.transform.Find(#路径#).GetComponent<Slider>();";
    
        public static string viewBaseInitFormat =
            @"
            #名字# = this.transform.Find(#路径#).GetComponent<UIViewBase>();";
        public static string dropDownInitFormat =
            @"
            #名字# = this.transform.Find(#路径#).GetComponent<TMP_Dropdown>();";
        public static string objInitFormat =
            @"
            #名字# = this.transform.Find(#路径#).gameObject;";
        public static string containerInitFormat =
            @"
            #名字# = new UIContainer(this.transform.Find(#路径#).gameObject);";
        public static string ui3DDisplayInitFormat =
            @"
            #名字#Display = this.transform.Find(#路径#).GetComponent<UI3DDisplay>();";
    }
}