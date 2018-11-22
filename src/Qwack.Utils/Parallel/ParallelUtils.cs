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

        private SemaphoreSlim _slimLock2 = new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount);

        private ParallelUtils()
        {

        }

        public bool MultiThreaded { get; } = true;

        private BlockingCollection<Task> _taskQueue = new BlockingCollection<Task>();

        public async Task Foreach<T>(IList<T> values, Action<T> code, bool overrideMTFlag = false)
        {
            if (!MultiThreaded && !overrideMTFlag)
            {
                RunInSeries(values, code);
                return;
            }

            RunOptimistically(values, code);
        }

        public async Task For(int startInclusive, int endExclusive, int step, Action<int> code, bool overrideMTFlag = false)
        {
            var sequence = new List<int>();
            for (var v = startInclusive; v < endExclusive; v += step)
                sequence.Add(v);

            await Foreach(sequence, code, overrideMTFlag);
        }

        private static void RunInSeries<T>(IList<T> values, Action<T> code)
        {
            foreach (var v in values)
                code.Invoke(v);
        }

        private void RunOptimistically<T>(IList<T> values, Action<T> code)
        {
            var taskList = new List<Task>();
            foreach (var v in values)
            {
                if (_slimLock2.Wait(0))
                {
                    var t = RunOnThread(v, code);
                    taskList.Add(t);
                    t.Start();
                }
                else
                    code.Invoke(v);
            }

            Task.WaitAll(taskList.ToArray());
        }

        public void QueueAndRunTasks(IEnumerable<Task> tasks)
        {
            var taskList = new List<Task>();
            foreach (var t in tasks)
            {
                if (_slimLock2.Wait(0))
                {
                    taskList.Add(t);
                    t.ContinueWith((t1) => _slimLock2.Release());
                    t.Start();
                }
                else
                    t.RunSynchronously();
            }

            Task.WaitAll(taskList.ToArray());
        }

        private Task RunOnThread<T>(T value, Action<T> code)
        {
            var task = new Task(() => code.Invoke(value));
            task.ContinueWith((t) => _slimLock2.Release());

            return task;
        }
    }
}
