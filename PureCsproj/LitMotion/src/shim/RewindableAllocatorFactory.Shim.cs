using Unity.Collections;

namespace LitMotion
{
    // CLI：不创建 Persistent Native 分配器，也不订阅 Application.quitting
    internal static class RewindableAllocatorFactory
    {
        public static AllocatorHelper<RewindableAllocator> CreateAllocator()
        {
            return new AllocatorHelper<RewindableAllocator>(Allocator.Persistent);
        }
    }
}
