namespace Xease.UI
{
    /// <summary>
    /// 生成器识别的控件种类。采集器目前只会写入其中一部分。
    /// </summary>
    public enum EElementType
    {
        ESTART = -1,
        EIMAGE = 0,
        ERAWIMAGE = 1,
        ETEXT = 2,
        EBUTTON = 3,
        EGAMEOBJECT = 4,
        ETRANSFORM = 5,
        ESCROLLVIEW = 6,
        ETOGGLE = 7,
        EINPUTFIELD = 8,
        ESCROLLDELEGATE = 9,
        ESLIDER = 10,
        EDROPDOWN = 11,
        ECONTAINER = 12,
        ESCROLLERPRO = 13,
        EUI3DDISPLAY = 14,
        EIMAGECTRL=15,
        ETEXTURECTRL = 16,
        EEND,
    }
}