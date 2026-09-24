using System.Collections.Generic;
using UnityEngine;

namespace Script.Editor.Extended.Engine.VfxScan
{
    public static class VfxRuleEvaluator
    {
        public static VfxScanResult Evaluate(
            VfxPrefabMetrics metrics,
            IReadOnlyList<VfxTypeBudget> budgets,
            IReadOnlyList<VfxPathMatchRule> pathRules)
        {
            var result = new VfxScanResult { metrics = metrics };
            VfxEffectType type = VfxTypeResolver.Resolve(
                metrics.labels,
                metrics != null ? metrics.prefabPath : string.Empty,
                budgets,
                pathRules);
            VfxTypeBudget budget = VfxScanBuiltinBudgets.Get(budgets, type);
            result.resolvedType = type;
            result.budgetDisplayName = budget.displayName;
            result.budgetMaxParticles = budget.maxDerivedParticles;
            result.budgetMaxMaterials = budget.maxMaterialCount;
            result.budgetMaxTextureSize = budget.maxTextureSize;

            EvaluateParticles(result, budget);
            EvaluateMaterials(result, budget);
            EvaluateSorting(result);
            EvaluateTextures(result, budget);
            EvaluateHierarchy(result, budget);

            result.status = VfxHealthStatus.Pass;
            for (int i = 0; i < result.issues.Count; i++)
            {
                if (result.issues[i].severity > result.status)
                    result.status = result.issues[i].severity;
            }

            return result;
        }

        public static VfxScanResult Reevaluate(
            VfxScanResult existing,
            IReadOnlyList<VfxTypeBudget> budgets,
            IReadOnlyList<VfxPathMatchRule> pathRules)
        {
            if (existing == null || existing.metrics == null)
                return existing;
            return Evaluate(existing.metrics, budgets, pathRules);
        }

        static void EvaluateParticles(VfxScanResult result, VfxTypeBudget budget)
        {
            VfxPrefabMetrics metrics = result.metrics;
            AddThresholdIssue(
                result,
                VfxIssueCategory.ParticleCount,
                metrics.totalDerivedParticles,
                budget.maxDerivedParticles,
                budget.maxDerivedParticlesError,
                "评估粒子数过大",
                "当前 Prefab 内各粒子系统的评估粒子数合计为 {0}，超过「{2}」预算 {1}。粒子数过多会直接抬高填充率与 Overdraw，建议降低发射速率、缩短生命周期，或拆成更短的子特效。");

            List<VfxParticleSystemMetrics> systems = metrics.particleSystems;
            for (int i = 0; i < systems.Count; i++)
            {
                VfxParticleSystemMetrics ps = systems[i];
                if (ps.derivedParticles <= 0)
                {
                    EvaluateEmptyParticle(result, ps);
                    EvaluateModules(result, budget, ps);
                    EvaluateMeshParticle(result, budget, ps);
                    continue;
                }

                EvaluateMaxParticlesRedundant(result, budget, ps);
                EvaluateModules(result, budget, ps);
                EvaluateMeshParticle(result, budget, ps);
            }
        }

        static void EvaluateEmptyParticle(VfxScanResult result, VfxParticleSystemMetrics ps)
        {
            result.issues.Add(VfxScanIssue.Create(
                VfxHealthStatus.Warning,
                VfxIssueCategory.EmptyParticleSystem,
                "空粒子系统",
                string.Format(
                    "节点「{0}」的评估粒子数为 0，说明发射模块未真正打出粒子（未开启发射、速率与 Burst 都为 0，或系统已禁用）。空粒子系统仍会占节点与组件开销。不要留空的粒子系统，建议删除该组件或节点。",
                    NodeName(ps)),
                ps.hierarchyPath));
        }

        static void EvaluateMaxParticlesRedundant(VfxScanResult result, VfxTypeBudget budget, VfxParticleSystemMetrics ps)
        {
            int derived = Mathf.Max(ps.derivedParticles, 1);
            if (ps.maxParticles < budget.maxParticlesRedundancyAbsMin)
                return;
            if (ps.maxParticles <= derived * budget.maxParticlesRedundancyRatio)
                return;

            int suggested = Mathf.Max(1, Mathf.CeilToInt(derived * 1.2f));
            result.issues.Add(VfxScanIssue.Create(
                VfxHealthStatus.Warning,
                VfxIssueCategory.MaxParticlesRedundant,
                "Max Particles 冗余过大",
                string.Format(
                    "节点「{0}」的 Max Particles 为 {1}，但按发射速率与生命周期静态推导仅需约 {2} 个。过大的上限会在运行时预分配粒子缓冲、浪费内存。建议改成 {3}（推导值的 1.2 倍）。",
                    NodeName(ps), ps.maxParticles, ps.derivedParticles, suggested),
                ps.hierarchyPath,
                null,
                VfxQuickFixKind.FixMaxParticles,
                suggested));
        }

        static void EvaluateModules(VfxScanResult result, VfxTypeBudget budget, VfxParticleSystemMetrics ps)
        {
            if (budget.forbidCollision && ps.collisionEnabled)
            {
                result.issues.Add(VfxScanIssue.Create(
                    VfxHealthStatus.Error,
                    VfxIssueCategory.HighCostModule,
                    "开启了 Collision 模块",
                    string.Format(
                        "节点「{0}」开启了粒子碰撞。移动端上 Collision 会每帧做物理查询，是典型性能红线，通常可用贴图遮罩或预烘焙动画替代。建议关闭 Collision 模块。",
                        NodeName(ps)),
                    ps.hierarchyPath,
                    null,
                    VfxQuickFixKind.DisableCollision));
            }

            if (budget.forbidTrigger && ps.triggerEnabled)
            {
                result.issues.Add(VfxScanIssue.Create(
                    VfxHealthStatus.Error,
                    VfxIssueCategory.HighCostModule,
                    "开启了 Trigger 模块",
                    string.Format(
                        "节点「{0}」开启了粒子 Trigger。Trigger 依赖碰撞体重叠检测，在粒子数量上来后开销会迅速放大。若只是做表现，建议关闭该模块。",
                        NodeName(ps)),
                    ps.hierarchyPath,
                    null,
                    VfxQuickFixKind.DisableTrigger));
            }

            if (budget.forbidLights && ps.lightsEnabled)
            {
                result.issues.Add(VfxScanIssue.Create(
                    VfxHealthStatus.Error,
                    VfxIssueCategory.HighCostModule,
                    "开启了 Lights 模块",
                    string.Format(
                        "节点「{0}」为粒子开启了实时光照。每个粒子光源都会增加额外的光照计算，移动端几乎无法承受。建议关闭 Lights，改用自发光材质或预烘焙亮斑。",
                        NodeName(ps)),
                    ps.hierarchyPath));
            }

            if (budget.warnNoise && ps.noiseEnabled)
            {
                result.issues.Add(VfxScanIssue.Create(
                    VfxHealthStatus.Warning,
                    VfxIssueCategory.HighCostModule,
                    "开启了 Noise 模块",
                    string.Format(
                        "节点「{0}」开启了 Noise。Noise 会在 CPU/GPU 上做额外扰动采样，粒子一多就会成为热点。若只是轻微抖动，可改用更便宜的速度曲线或预烘焙轨迹。",
                        NodeName(ps)),
                    ps.hierarchyPath));
            }

            if (budget.warnSubEmitters && ps.subEmittersEnabled)
            {
                result.issues.Add(VfxScanIssue.Create(
                    VfxHealthStatus.Warning,
                    VfxIssueCategory.HighCostModule,
                    "开启了 Sub Emitters",
                    string.Format(
                        "节点「{0}」开启了子发射器。子发射器会让粒子数量在运行时成倍增长，静态上限容易被低估。请确认子发射次数与生命周期仍然落在预算内。",
                        NodeName(ps)),
                    ps.hierarchyPath));
            }

            if (budget.warnWorldSpace && ps.simulationSpace == ParticleSystemSimulationSpace.World)
            {
                result.issues.Add(VfxScanIssue.Create(
                    VfxHealthStatus.Warning,
                    VfxIssueCategory.SimulationSpace,
                    "使用 World 模拟空间",
                    string.Format(
                        "节点「{0}」的 Simulation Space 为 World。World 空间粒子无法与 Local 空间粒子合批，同类特效一多就会打断合批、增加 Draw Call。若运动不依赖世界坐标，建议改为 Local。",
                        NodeName(ps)),
                    ps.hierarchyPath));
            }
        }

        static void EvaluateMeshParticle(VfxScanResult result, VfxTypeBudget budget, VfxParticleSystemMetrics ps)
        {
            if (!ps.isMeshRenderMode)
                return;

            VfxHealthStatus severity = GetThresholdSeverity(ps.meshTriangleCount, budget.maxMeshTriangles, budget.maxMeshTrianglesError);
            if (severity == VfxHealthStatus.Pass)
                return;

            result.issues.Add(VfxScanIssue.Create(
                severity,
                VfxIssueCategory.MeshParticle,
                "Mesh 粒子面数过高",
                string.Format(
                    "节点「{0}」使用 Mesh 粒子，模型「{1}」约 {2} 三角面，超过预算 {3}。每个粒子都会绘制这份网格，面数会按粒子数放大。建议换成低于 {3} 面的低模，或改回 Billboard。",
                    NodeName(ps),
                    string.IsNullOrEmpty(ps.meshName) ? "未命名网格" : ps.meshName,
                    ps.meshTriangleCount,
                    budget.maxMeshTriangles),
                ps.hierarchyPath,
                ps.meshAssetPath));
        }

        static void EvaluateMaterials(VfxScanResult result, VfxTypeBudget budget)
        {
            int count = result.MaterialCount;
            AddThresholdIssue(
                result,
                VfxIssueCategory.MaterialCount,
                count,
                budget.maxMaterialCount,
                budget.maxMaterialCountError,
                "材质球数量过多",
                string.Format(
                    "当前材质球数量为 {{0}}，会导致该特效产生至少 {{0}} 个 Draw Call，超过「{0}」预算 {{1}}。建议把可共用的贴图合进图集，让多个粒子系统使用同一个材质。",
                    result.budgetDisplayName));

            List<VfxMaterialMetrics> materials = result.metrics.materials;
            for (int i = 0; i < materials.Count; i++)
            {
                VfxMaterialMetrics mat = materials[i];
                if (mat.hasGrabPass)
                {
                    result.issues.Add(VfxScanIssue.Create(
                        VfxHealthStatus.Error,
                        VfxIssueCategory.ShaderRisk,
                        "Shader 使用 GrabPass",
                        string.Format(
                            "材质「{0}」引用的 Shader「{1}」包含 GrabPass。GrabPass 会额外拷贝整屏，移动端带宽和填充率都会被打满。请换成不抓屏的扭曲方案，或直接去掉该效果。",
                            mat.materialName, mat.shaderName),
                        null,
                        mat.assetPath));
                }

                if (mat.hasSoftParticles)
                {
                    result.issues.Add(VfxScanIssue.Create(
                        VfxHealthStatus.Warning,
                        VfxIssueCategory.ShaderRisk,
                        "启用了 Soft Particles / 深度纹理",
                        string.Format(
                            "材质「{0}」启用了 Soft Particles 或采样了相机深度。这会强制打开深度纹理并增加每像素开销。UI 特效应关闭；场景特效也请确认是否真的需要软边过渡。",
                            mat.materialName),
                        null,
                        mat.assetPath));
                }

                if (mat.hasDistortion)
                {
                    result.issues.Add(VfxScanIssue.Create(
                        VfxHealthStatus.Warning,
                        VfxIssueCategory.ShaderRisk,
                        "疑似复杂屏幕扭曲",
                        string.Format(
                            "材质「{0}」的 Shader「{1}」名称或关键字包含扭曲相关特征。屏幕扭曲通常依赖抓屏或深度，容易成为移动端热点。请评估能否用更便宜的 UV 扰动替代。",
                            mat.materialName, mat.shaderName),
                        null,
                        mat.assetPath));
                }
            }
        }

        static void EvaluateSorting(VfxScanResult result)
        {
            List<VfxParticleSystemMetrics> systems = result.metrics.particleSystems;
            var groups = new Dictionary<string, List<VfxParticleSystemMetrics>>();
            for (int i = 0; i < systems.Count; i++)
            {
                VfxParticleSystemMetrics ps = systems[i];
                if (string.IsNullOrEmpty(ps.materialAssetPath))
                    continue;
                if (!groups.TryGetValue(ps.materialAssetPath, out List<VfxParticleSystemMetrics> list))
                {
                    list = new List<VfxParticleSystemMetrics>();
                    groups.Add(ps.materialAssetPath, list);
                }

                list.Add(ps);
            }

            foreach (KeyValuePair<string, List<VfxParticleSystemMetrics>> pair in groups)
            {
                List<VfxParticleSystemMetrics> list = pair.Value;
                if (list.Count < 2)
                    continue;

                string layer = list[0].sortingLayer;
                int order = list[0].sortingOrder;
                int queue = list[0].renderQueue;
                bool mismatch = false;
                for (int i = 1; i < list.Count; i++)
                {
                    if (list[i].sortingLayer != layer || list[i].sortingOrder != order || list[i].renderQueue != queue)
                    {
                        mismatch = true;
                        break;
                    }
                }

                if (!mismatch)
                    continue;

                result.issues.Add(VfxScanIssue.Create(
                    VfxHealthStatus.Warning,
                    VfxIssueCategory.Sorting,
                    "同材质粒子层级不一致",
                    string.Format(
                        "材质「{0}」被多个粒子系统共用，但 Sorting Layer / Order / Render Queue 不一致。渲染队列被穿插后无法合批，Draw Call 会按系统数拆开。建议把同一材质的粒子统一到相同排序层级。",
                        pair.Key),
                    list[0].hierarchyPath,
                    pair.Key));
            }
        }

        static void EvaluateTextures(VfxScanResult result, VfxTypeBudget budget)
        {
            List<VfxTextureMetrics> textures = result.metrics.textures;
            for (int i = 0; i < textures.Count; i++)
            {
                VfxTextureMetrics tex = textures[i];
                int maxDim = Mathf.Max(tex.width, tex.height);
                VfxHealthStatus sizeSeverity = GetThresholdSeverity(maxDim, budget.maxTextureSize, budget.maxTextureSizeError);
                if (sizeSeverity != VfxHealthStatus.Pass)
                {
                    result.issues.Add(VfxScanIssue.Create(
                        sizeSeverity,
                        VfxIssueCategory.TextureSize,
                        "贴图尺寸超标",
                        string.Format(
                            "贴图「{0}」分辨率为 {1}x{2}，超过「{3}」预算 {4}。过大贴图会占用更多显存并增加采样带宽。请按特效类型缩小到不超过 {4}，或把多张小图合进图集。",
                            DisplayTexName(tex), tex.width, tex.height, result.budgetDisplayName, budget.maxTextureSize),
                        null,
                        tex.assetPath));
                }

                if (budget.requirePot && !tex.isPot)
                {
                    result.issues.Add(VfxScanIssue.Create(
                        VfxHealthStatus.Warning,
                        VfxIssueCategory.TexturePot,
                        "贴图不是 2 的幂次方",
                        string.Format(
                            "贴图「{0}」尺寸为 {1}x{2}，不是 2 的幂次方。移动端上非 POT 贴图往往无法使用 ASTC/ETC2 等硬件压缩，最终会变成更大的未压缩纹理。请改为 64/128/256/512 这类尺寸。",
                            DisplayTexName(tex), tex.width, tex.height),
                        null,
                        tex.assetPath));
                }

                if (tex.importerFound)
                {
                    if (budget.mipmapPolicy == VfxMipmapPolicy.MustOff && tex.mipmapEnabled)
                    {
                        result.issues.Add(VfxScanIssue.Create(
                            VfxHealthStatus.Warning,
                            VfxIssueCategory.TextureMipmap,
                            "UI 贴图不应开启 Mipmap",
                            string.Format(
                                "贴图「{0}」开启了 Mipmap。UI 特效通常以固定像素大小显示，Mipmap 只会多占约 33% 显存，却几乎看不到收益。建议关闭 Mipmap。",
                                DisplayTexName(tex)),
                            null,
                            tex.assetPath,
                            VfxQuickFixKind.DisableMipmap));
                    }
                    else if (budget.mipmapPolicy == VfxMipmapPolicy.MustOn && !tex.mipmapEnabled)
                    {
                        result.issues.Add(VfxScanIssue.Create(
                            VfxHealthStatus.Warning,
                            VfxIssueCategory.TextureMipmap,
                            "场景贴图应开启 Mipmap",
                            string.Format(
                                "贴图「{0}」未开启 Mipmap。场景常驻特效会在不同距离下显示，缺少 Mipmap 会产生闪烁并增加远距离采样开销。建议开启 Mipmap。",
                                DisplayTexName(tex)),
                            null,
                            tex.assetPath));
                    }
                }

                if (budget.forbidUncompressedTexture && tex.isUncompressed)
                {
                    result.issues.Add(VfxScanIssue.Create(
                        VfxHealthStatus.Error,
                        VfxIssueCategory.TextureCompression,
                        "贴图未压缩",
                        string.Format(
                            "贴图「{0}」在 Android/iOS 平台使用了未压缩格式（当前 Android: {1}，iOS: {2}）。一张 1024 的 RGBA32 会占用数 MB 显存。请改为 ASTC 或 ETC2，并避免 Crunch 以外的未压缩设置。",
                            DisplayTexName(tex),
                            string.IsNullOrEmpty(tex.androidFormat) ? "未知" : tex.androidFormat,
                            string.IsNullOrEmpty(tex.iosFormat) ? "未知" : tex.iosFormat),
                        null,
                        tex.assetPath));
                }
            }
        }

        static void EvaluateHierarchy(VfxScanResult result, VfxTypeBudget budget)
        {
            VfxHierarchyMetrics hierarchy = result.metrics.hierarchy;
            AddThresholdIssue(
                result,
                VfxIssueCategory.Hierarchy,
                hierarchy.maxDepth,
                budget.maxHierarchyDepth,
                budget.maxHierarchyDepthError,
                "节点层级过深",
                "当前 Prefab 最大层级深度为 {0}，超过预算 {1}。过深的空物体嵌套会增加 Transform 更新与实例化开销。建议把无逻辑的中间节点拍平。");

            AddThresholdIssue(
                result,
                VfxIssueCategory.Hierarchy,
                hierarchy.nodeCount,
                budget.maxNodeCount,
                budget.maxNodeCountError,
                "节点数量过多",
                "当前 Prefab 节点总数为 {0}，超过预算 {1}。每个 GameObject 都有固定开销，建议删除无用空节点，合并可共用的粒子系统。");

            List<VfxRedundantComponentInfo> redundants = hierarchy.redundants;
            for (int i = 0; i < redundants.Count; i++)
            {
                VfxRedundantComponentInfo item = redundants[i];
                result.issues.Add(VfxScanIssue.Create(
                    VfxHealthStatus.Warning,
                    VfxIssueCategory.RedundantComponent,
                    "存在冗余组件",
                    string.Format(
                        "节点「{0}」挂有 {1}（{2}）。禁用粒子、空 Animator、遗留 AudioSource/Camera 都会在实例化时带来额外开销。若确认无用，请直接删掉该组件。",
                        string.IsNullOrEmpty(item.hierarchyPath) ? result.PrefabName : item.hierarchyPath,
                        item.componentType,
                        item.reason),
                    item.hierarchyPath));
            }
        }

        static void AddThresholdIssue(
            VfxScanResult result,
            VfxIssueCategory category,
            int value,
            int warn,
            int error,
            string title,
            string adviceFormat)
        {
            VfxHealthStatus severity = GetThresholdSeverity(value, warn, error);
            if (severity == VfxHealthStatus.Pass)
                return;

            result.issues.Add(VfxScanIssue.Create(
                severity,
                category,
                title,
                string.Format(adviceFormat, value, warn, result.budgetDisplayName)));
        }

        static VfxHealthStatus GetThresholdSeverity(int value, int warn, int error)
        {
            if (error > 0 && value > error)
                return VfxHealthStatus.Error;
            if (warn > 0 && value > warn)
                return VfxHealthStatus.Warning;
            return VfxHealthStatus.Pass;
        }

        static string NodeName(VfxParticleSystemMetrics ps)
        {
            if (!string.IsNullOrEmpty(ps.hierarchyPath))
                return ps.hierarchyPath;
            return string.IsNullOrEmpty(ps.objectName) ? "未命名粒子" : ps.objectName;
        }

        static string DisplayTexName(VfxTextureMetrics tex)
        {
            if (!string.IsNullOrEmpty(tex.textureName))
                return tex.textureName;
            return string.IsNullOrEmpty(tex.assetPath) ? "未命名贴图" : tex.assetPath;
        }
    }
}
