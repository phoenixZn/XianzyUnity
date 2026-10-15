namespace Xease.CoreGame
{
    /// <summary>
    /// Buff 生成信息：创建 <see cref="BuffLogic"/> 的运行时初始数据，走对象池，用毕由工厂回收。
    /// </summary>
    public class BuffGenInfo : CustomLogicGenInfo
    {
        public int BuffLevel; // buff层数
        public int BuffMaxLevel; // buff最大层数
        public float DurationAddSeconds; // 生成buff时固定增加的持续时间
        public float SourceDurationAddRate; // 生成buff时来自来源逻辑的持续时间加成比例
        public float DurationAddRate; // 额外的持续时间加成比例，预留用于buff免疫类修正
        public LogicEntity Sourcer; // buff来源实体
        public LogicEntity Owner; // buff宿主实体

        public override void Destroy()
        {
            Clear();
            base.Destroy();
        }

        public void Clear()
        {
            BuffLevel = -1;
            BuffMaxLevel = -1;
            DurationAddSeconds = 0;
            SourceDurationAddRate = 0;
            DurationAddRate = 0;
            Sourcer = null;
            Owner = null;
        }
    }
}
