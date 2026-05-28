using System;
using System.Collections.Generic;
using UnityEngine;
using YooAsset.Editor;

public class HybrdiScriptableBuildPipeline : IBuildPipeline
{
    public BuildResult Run(BuildParameters buildParameters, bool enableLog)
    {
        if (buildParameters is HybridScriptableBuildParameters)
        {
            var hybridBuildParameters = buildParameters as HybridScriptableBuildParameters;

            AssetBundleBuilder builder = new AssetBundleBuilder();
            return builder.Run(buildParameters, GetHybridBuildPipeline(),
                enableLog);
        }
        else
        {
            throw new Exception($"Invalid build parameter type : {buildParameters.GetType().Name}");
        }
    }

    private List<IBuildTask> GetHybridBuildPipeline()
    {
        List<IBuildTask> pipeline = new List<IBuildTask>();

        //如果需要同时构建资源和代码
        //需要确保代码在资源构建之前就已经在AssetBundle文件夹中
        pipeline.AddRange(new List<IBuildTask>
        {
            // 构建前准备：输出目录、构建上下文与参数校验等（YooAsset RFBP）
            new TaskPrepare_RFBP(),
            // HybridCLR：编译热更 DLL，并将补丁 AOT / 热更 DLL 拷贝到资源收集路径，供后续打进包体
            new XTaskBuildScript_SBP(),
            // 按收集器规则扫描资源，生成本次构建的 BuildMap
            new TaskGetBuildMap_RFBP(),
            // 执行实际打包（Scriptable Build Pipeline 产出 Bundle / 原始文件）
            new TaskBuilding_RFBP(),
            // 对构建产物做加密处理（若构建参数启用加密）
            new TaskEncryption_RFBP(),
            // 汇总并更新各 Bundle 的版本、哈希、依赖等元数据
            new TaskUpdateBundleInfo_RFBP(),
            // 生成资源清单 Manifest，供运行时加载与版本校验
            new TaskCreateManifest_RFBP(),
            // 输出构建报告（资源统计、错误与警告等）
            new TaskCreateReport_RFBP(),
            // 将构建结果整理为可分发的资源包目录结构
            new TaskCreatePackage_RFBP(),
            // 拷贝首包 / StreamingAssets 等内置资源到目标路径
            new TaskCopyBuildinFiles_RFBP(),
            // 生成 Catalog（资源目录），供运行时查询与寻址
            new TaskCreateCatalog_RFBP()
        });


        // _RFBP: Raw File Build Pipeline 的缩写。
        // 表示这些 Task*_RFBP 属于 YooAsset 的 原始文件构建管线（Raw File Build Pipeline）：资源按 RawFile/收集器规则参与构建，任务名里带 _RFBP 的都是这条管线里的标准步骤。

        // _SBP: Scriptable Build Pipeline 的缩写。
        // 指 Unity 的 可编程构建管线（包名一般是 com.unity.scriptablebuildpipeline），用来执行实际的 AssetBundle 构建。

        return pipeline;
    }
}