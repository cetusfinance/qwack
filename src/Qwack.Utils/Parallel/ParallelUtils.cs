using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;

namespace Qwack.Utils.Parallel
{
    public sealed class ParallelUtils
    {
        private static readonly Lazy<ParallelUtils> lazy =
            new Lazy<ParallelUtils>(() => new ParallelUtils());

        public static ParallelUtils Instance => lazy.Value;

        private ParallelUtils()
        {

        }

        public bool MultiThreaded { get; } = false;

        private BlockingCollection<Task> _taskQueue = new BlockingCollection<Task>();

        public async Task Foreach<T>(IList<T> values, Action<T> code, bool overrideMTFlag=false)
        {
            if (!MultiThreaded && !overrideMTFlag)
            {
                foreach (var v in values)
                    code.Invoke(v);

                return;
            }

            var queue = new ConcurrentQueue<T>(values);
            var tasks = new Task[values.Count()];

            for (var i = 0; i < tasks.Length; i++)
            {
                tasks[i] = new Task(()=>code.Invoke(values[i]));
                tasks[i].Start();
            }

            for (var i = 0; i < tasks.Length; i++)
            {
                await tasks[i];
            }
            Task.WaitAll(tasks);
        }

        public async Task For(int startInclusive, int endExclusive, int step, Action<int> code, bool overrideMTFlag = false)
        {
            if (!MultiThreaded && !overrideMTFlag)
            {
                for (var v = startInclusive; v < endExclusive; v += step)
                    code.Invoke(v);

                return;
            }

            var sequence = new List<int>();
            for (var v = startInclusive; v < endExclusive; v += step)
                sequence.Add(v);

            await Foreach(sequence, code);
        }
    }
}
