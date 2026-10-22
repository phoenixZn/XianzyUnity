using System.Text;
using Entitas;
using Xease;
using Xease.CoreGame;

namespace PureGameEnv
{
    /// <summary>
    /// 把带 Transform 的逻辑实体投影成 ASCII 网格。不创建 ViewWrapper。
    /// </summary>
    internal static class EntityCanvas
    {
        // 网格列数
        const int Cols = 40;
        // 网格行数
        const int Rows = 20;
        // Demo 出生与移动所在平面的半宽（世界单位）。量化用 x/y，z 不参与
        const float PlaneHalfExtent = 10f;

        /// <summary>
        /// 没有世界时返回占位。有世界时画出点阵，单实体为 e，重叠格为个数。
        /// </summary>
        public static string Build(out int entityCount)
        {
            entityCount = 0;
            var world = CurrentWorld();
            if (world == null)
                return "(no world)";

            var cells = new int[Rows * Cols];
            var entities = Group(world).GetEntities();
            for (var i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                if (!entity.isEnabled || !entity.hasComTransform)
                    continue;

                entityCount++;
                var pos = entity.position;
                var col = Quantize(pos.x, Cols, false);
                var row = Quantize(pos.y, Rows, true);
                cells[row * Cols + col]++;
            }

            var sb = new StringBuilder(Rows * (Cols + 1));
            for (var row = 0; row < Rows; row++)
            {
                for (var col = 0; col < Cols; col++)
                    sb.Append(Glyph(cells[row * Cols + col]));
                if (row + 1 < Rows)
                    sb.Append('\n');
            }

            return sb.ToString();
        }

        static LogicWorld _world;
        static IGroup<LogicEntity> _group;

        static LogicWorld CurrentWorld()
        {
            if (GEnv.Inst == null)
                return null;
            return G.Module<ModuleWorlds>()?.MainWorld?.LogicWorld;
        }

        static IGroup<LogicEntity> Group(LogicWorld world)
        {
            if (!ReferenceEquals(_world, world))
            {
                _world = world;
                _group = world.GetGroup(LogicMatcher.AllOf(LogicComponentsLookup.ComTransform));
            }

            return _group;
        }

        // flip 时数值越大越靠上，用来把 y 画成上为正
        static int Quantize(float value, int cells, bool flip)
        {
            var t = (value + PlaneHalfExtent) / (PlaneHalfExtent * 2f);
            if (flip)
                t = 1f - t;
            var index = (int)(t * cells);
            if (index < 0)
                return 0;
            if (index >= cells)
                return cells - 1;
            return index;
        }

        static char Glyph(int count)
        {
            if (count <= 0)
                return '.';
            if (count == 1)
                return 'e';
            if (count < 10)
                return (char)('0' + count);
            return '*';
        }
    }
}
