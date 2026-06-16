using UnityEngine;

namespace Xease
{
    /// <summary>
    /// 输入设备基类
    /// </summary>
    public abstract class InputDeviceBase
    {
        /// <summary>
        /// 是否存在虚拟轴线
        /// </summary>
        /// <param name="name">轴线名称</param>
        /// <returns>是否存在</returns>
        protected bool IsExistVirtualAxis(string name)
        {
            return G.Input.IsExistVirtualAxis(name);
        }
        /// <summary>
        /// 是否存在虚拟按钮
        /// </summary>
        /// <param name="name">按钮名称</param>
        /// <returns>是否存在</returns>
        protected bool IsExistVirtualButton(string name)
        {
            return G.Input.IsExistVirtualButton(name);
        }
        /// <summary>
        /// 注册虚拟轴线
        /// </summary>
        /// <param name="name">轴线名称</param>
        protected void RegisterVirtualAxis(string name)
        {
            G.Input.RegisterVirtualAxis(name);
        }
        /// <summary>
        /// 注册虚拟按钮
        /// </summary>
        /// <param name="name">按钮名称</param>
        protected void RegisterVirtualButton(string name)
        {
            G.Input.RegisterVirtualButton(name);
        }
        /// <summary>
        /// 取消注册虚拟轴线
        /// </summary>
        /// <param name="name">轴线名称</param>
        protected void UnRegisterVirtualAxis(string name)
        {
            G.Input.UnRegisterVirtualAxis(name);
        }
        /// <summary>
        /// 取消注册虚拟按钮
        /// </summary>
        /// <param name="name">按钮名称</param>
        protected void UnRegisterVirtualButton(string name)
        {
            G.Input.UnRegisterVirtualButton(name);
        }

        /// <summary>
        /// 设置虚拟鼠标位置
        /// </summary>
        /// <param name="x">x值</param>
        /// <param name="y">y值</param>
        /// <param name="z">z值</param>
        protected void SetVirtualMousePosition(float x, float y, float z)
        {
            G.Input.SetVirtualMousePosition(new Vector3(x, y, z));
        }
        /// <summary>
        /// 设置虚拟鼠标位置
        /// </summary>
        /// <param name="value">鼠标位置</param>
        protected void SetVirtualMousePosition(Vector3 value)
        {
            G.Input.SetVirtualMousePosition(value);
        }
        /// <summary>
        /// 设置按钮按下
        /// </summary>
        /// <param name="name">按钮名称</param>
        protected void SetButtonDown(string name)
        {
            G.Input.SetButtonDown(name);
        }
        /// <summary>
        /// 设置按钮抬起
        /// </summary>
        /// <param name="name">按钮名称</param>
        protected void SetButtonUp(string name)
        {
            G.Input.SetButtonUp(name);
        }
        /// <summary>
        /// 设置轴线值为正方向1
        /// </summary>
        /// <param name="name">轴线名称</param>
        protected void SetAxisPositive(string name)
        {
            G.Input.SetAxisPositive(name);
        }
        /// <summary>
        /// 设置轴线值为负方向-1
        /// </summary>
        /// <param name="name">轴线名称</param>
        protected void SetAxisNegative(string name)
        {
            G.Input.SetAxisNegative(name);
        }
        /// <summary>
        /// 设置轴线值为0
        /// </summary>
        /// <param name="name">轴线名称</param>
        protected void SetAxisZero(string name)
        {
            G.Input.SetAxisZero(name);
        }
        /// <summary>
        /// 设置轴线值
        /// </summary>
        /// <param name="name">轴线名称</param>
        /// <param name="value">值</param>
        protected void SetAxis(string name, float value)
        {
            G.Input.SetAxis(name, value);
        }

        /// <summary>
        /// 设备启动
        /// </summary>
        public abstract void OnStartUp();
        /// <summary>
        /// 设备运作
        /// </summary>
        public abstract void OnRun();
        /// <summary>
        /// 设备关闭
        /// </summary>
        public abstract void OnShutdown();
    }
}