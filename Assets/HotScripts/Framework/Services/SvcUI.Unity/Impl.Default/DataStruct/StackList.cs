using System.Collections.Generic;

namespace Xease.UI
{
    /// <summary>
    /// 同时维护 Stack 与可随机访问 List，供 UI 导航栈按名查找/中间弹出。
    /// </summary>
    public class StackList<T> where T : class
    {
        private readonly Stack<T> DataStack = new Stack<T>();
        public List<T> DataList = new List<T>(); // 与栈同序的列表，供遍历与 Contains

        public int Count => DataStack.Count;

        public void Push(T t)
        {
            DataStack.Push(t);
            DataList.Add(t);
        }

        public T Pop()
        {
            if (DataList.Count == 0)
                return null;
            DataList.RemoveAt(DataList.Count - 1);
            return DataStack.Pop();
        }

        public T Peek()
        {
            return DataStack.Peek();
        }

        public void RemoveData(T t)
        {
            RemoveFromStack(t);
            RemoveFromList(t);
        }

        public bool Contains(T t)
        {
            return DataList.Contains(t);
        }

        public void Clear()
        {
            DataStack.Clear();
            DataList.Clear();
        }

        void RemoveFromStack(T t)
        {
            Queue<T> removeTempQ = null;

            while (DataStack.Count != 0)
            {
                if (DataStack.Peek() == t)
                {
                    Pop();
                    continue;
                }

                if (removeTempQ == null)
                    removeTempQ = new Queue<T>();
                removeTempQ.Enqueue(Pop());
            }

            if (removeTempQ == null)
                return;

            while (removeTempQ.Count != 0)
            {
                Push(removeTempQ.Dequeue());
            }
        }

        void RemoveFromList(T t)
        {
            if (DataList.Contains(t))
                DataList.Remove(t);
        }
    }
}
