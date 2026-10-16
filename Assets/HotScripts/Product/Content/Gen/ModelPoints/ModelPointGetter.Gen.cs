using Xease.ModelPointTool;

namespace Xease.ModelPointTool.Gen
{
    /// <summary>
    /// 编辑器生成的挂点路径表。手动修改会在下次生成时丢失。
    /// </summary>
    public static class ModelPointGetterGen
    {
        /// <summary>
        /// 清空并填入 (预制体名, 挂点名) → 相对路径。
        /// </summary>
        public static void RegisterAll()
        {
            ModelPointGetter.Clear();
            ModelPointGetter.Add("ActorCube", "Hp", "Cube/Hp");
            ModelPointGetter.Add("ActorCube", "Head", "Cube/Head");
            ModelPointGetter.Add("ActorSphere", "Hit", "Sphere/Hit");
            ModelPointGetter.Add("TestHero", "Head", "Capsule/Head");
            ModelPointGetter.Add("TestHero", "Foot", "Foot");
        }
    }
}
