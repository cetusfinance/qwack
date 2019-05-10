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

        private static readonly int numThreads = Environment.ProcessorCount * 2;

        private SemaphoreSlim _slimLock = new SemaphoreSlim(numThreads, numThreads);

        private ParallelUtils()
        {

        }

        public bool MultiThreaded { get; set; } = true;

        private BlockingCollection<Task> _taskQueue = new BlockingCollection<Task>();

        public async Task Foreach<T>(IList<T> values, Action<T> code, bool overrideMTFlag = false)
        {
            if (overrideMTFlag || !MultiThreaded)
            {
                RunInSeries(values, code);
                return;
            }

            await RunOptimistically(values, code);
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

        private async Task RunOptimistically<T>(IList<T> values, Action<T> code)
        {
            var taskList = new List<Task>();
            foreach (var v in values)
            {
                if (_slimLock.Wait(0))
                {
                    var t = RunOnThread(v, code);
                    taskList.Add(t);
                    t.Start();
                }
                else
                    code.Invoke(v);
            }

            try
            {
                await Task.WhenAll(taskList.ToArray());
            }
            catch(AggregateException ex)
            {
                throw ex.InnerExceptions.First();
            }
            
        }

        public async Task QueueAndRunTasks(IEnumerable<Task> tasks)
        {
            var taskList = new List<Task>();
            foreach (var t in tasks)
            {
                if (MultiThreaded && _slimLock.Wait(0))
                {
                    taskList.Add(t.ContinueWith((t1) => _slimLock.Release(),TaskContinuationOptions.ExecuteSynchronously));
                    t.Start();
                }
                else
                    t.RunSynchronously();
            }

            await Task.WhenAll(taskList.ToArray());
        }

        private Task RunOnThread<T>(T value, Action<T> code)
        {
            var task = new Task(() =>
            {
                try
                {
                    code.Invoke(value);
                }
                finally
                {
                    _slimLock.Release();
                }
            });

            return task;
        }
    }
}
