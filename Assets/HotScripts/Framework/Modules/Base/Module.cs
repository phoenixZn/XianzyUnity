using System;
using System.Collections;
using System.Collections.Generic;

namespace Xease
{
	/// <summary>
	/// 模块基类，提供一些底层的接口和统一处理逻辑
	/// </summary>
    public abstract class Module : IModule
    {
	    private readonly List<object>[] _dataListCache = new List<object>[Enum.GetValues(typeof(ModuleDataType)).Length];
	    
	    public void Init() => OnInit(); // 子模块的自定义初始化逻辑

        protected virtual void OnInit() {}

        public void Start()
        {
	        // 子模块的自定义开始逻辑
	        OnStart();
        }

        protected virtual void OnStart() {}

        public void Shutdown()
        {
	        // 子模块的自定义终止逻辑
            OnShutdown();

            // 将所有仍在注册的数据进行清理处理
            foreach (ModuleDataType dataType in Enum.GetValues(typeof(ModuleDataType)))
            {
	            ClearData(dataType);
	            UnregisterData(dataType);
            }
        }

        protected virtual void OnShutdown() {}
        
        #region 数据层

        /// <summary>
        /// 注册数据实例的生命周期，会在对应的时机进行清理，需要实现IResettable、IDisposable、IList、IDictionary之中任一接口
        /// </summary>
        /// <param name="type">数据生命周期类型</param>
        /// <param name="data">数据实例</param>
        protected void RegisterData(ModuleDataType type, object data)
        {
	        if (data == null)
	        {
		        G.LogError($"[{GetType().Name}] RegisterData failed, data is null");
		        return;
	        }

	        if (CanDataRegister(data))
	        {
		        AddData(type, data);
		        return;
	        }
	        
	        G.LogError($"[{GetType().Name}] RegisterData failed, {data.GetType().Name} does not implement any of IResettable, IDisposable, IList, IDictionary");
        }

        /// <summary>
        /// 检查数据实例是否可注册生命周期
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected bool CanDataRegister(object data)
        {
	        return data is IResettable or IDisposable or IList or IDictionary;
        }

        private void AddData(ModuleDataType type, object data)
        {
	        var dataList = _dataListCache[(int)type];
	        if (dataList == null)
	        {
		        dataList = new List<object>();
		        _dataListCache[(int)type] = dataList;
	        }

	        if (!dataList.Contains(data))
	        {
		        dataList.Add(data);
	        }
        }

        /// <summary>
        /// 反注册数据实例的生命周期
        /// </summary>
        /// <param name="type">数据生命周期类型</param>
        /// <param name="data">数据实例，为空则将对应类型的所有注册数据全部清空</param>
        protected void UnregisterData(ModuleDataType type, object data = null)
        {
	        var dataList = _dataListCache[(int)type];
	        if (dataList == null)
		        return;
	        if (data == null)
	        {
		        dataList.Clear();
		        return;
	        }
	        dataList.Remove(data);
        }
        
        public void InitData(ModuleDataType type)
        {
	        OnInitPlayerData();
        }
        protected virtual void OnInitPlayerData()
        {
        }
        
        public void ClearData(ModuleDataType type)
        {
	        var dataList = _dataListCache[(int)type];
	        if (dataList != null)
	        {
		        for (int i = 0; i < dataList.Count; ++i)
		        {
			        var data = dataList[i];
			        switch (data)
			        {
				        case IResettable resettable:
					        resettable.Reset();
					        break;
				        case IDisposable disposable:
					        disposable.Dispose();
					        break;
				        case IList list:
					        list.Clear();
					        break;
				        case IDictionary dict:
					        dict.Clear();
					        break;
			        }
		        }
		        
		        return;
	        }

	        switch (type)
	        {
		        case ModuleDataType.Player:
			        OnClearPlayerData();
			        break;
	        }
        }

        protected virtual void OnClearPlayerData()
        {
        }
        #endregion
    }
}