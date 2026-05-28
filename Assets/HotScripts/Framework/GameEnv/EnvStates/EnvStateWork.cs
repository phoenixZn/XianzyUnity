using System.Collections.Generic;

namespace HotUpdate
{
    /// <summary>
    /// Env State沟通指令
    /// 上个状态需求关闭界面，如果下个界面希望继承这个界面过去怎么办
    /// </summary>
    public interface IEnvWork
    {
        void Run(EnvStateBase curState);
    }
    
    /// <summary>
    /// Env State沟通上下文
    /// 上一个状态不宜立刻处理的活儿，交接给下一个状态代为处理
    /// </summary>
    public class EnvTransferWorks
    {
        public string FromState { get; set; }
        public List<IEnvWork> Works { get; set; }

        public EnvTransferWorks(string fromState)
        {
            FromState = fromState;  //来源
            Works = new (); //交接的活儿
        }
    }
}