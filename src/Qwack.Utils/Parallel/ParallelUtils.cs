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
        private static object _lock = new object();
        private static ParallelUtils _instance;
        public static ParallelUtils Instance
        {
            get
            {
                if(_instance==null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new ParallelUtils();
                        }
                    }
                }
                return _instance;
            }
        }
            


        private static readonly int numThreads = Environment.ProcessorCount;

        private ParallelUtils()
        {
            for(var i = 0; i < numThreads;i++)
            {
                var thread = new Thread(ThreadStart);
                thread.IsBackground = true;
                thread.Name = $"ParallelUtilsThread{i}";
                thread.Start();
            }
        }

        private void ThreadStart()
        {
            foreach(var item in _taskQueue.GetConsumingEnumerable())
            {
                if (item.TaskCompletion != null)
                {
                    try
                    {
                        item.Action();
                        item.TaskCompletion.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        item.TaskCompletion.SetException(ex);
                    }
                }
                else
                {
                    try
                    {
                        item.TaskToRun.RunSynchronously();
                    }
                    catch
                    {
                    }
                }
            }
        }

        public bool MultiThreaded { get; set; } = true;

        private BlockingCollection<WorkItem> _taskQueue = new BlockingCollection<WorkItem>(numThreads);

        public async Task Foreach<T>(IList<T> values, Action<T> code, bool overrideMTFlag = false)
        {
            if (overrideMTFlag || !MultiThreaded)
            {
                RunInSeries(values, code);
                return;
            }

            await RunOptimistically(values, code);
        }

        private struct WorkItem
        {
            public Action Action;
            public TaskCompletionSource<bool> TaskCompletion;
            public Task TaskToRun;
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
                code(v);

        }

        private async Task RunOptimistically<T>(IList<T> values, Action<T> code)
        {
            var taskList = new List<Task>();
            foreach (var v in values)
            {
                var tcs = new TaskCompletionSource<bool>();
                var task = tcs.Task;
                Action action = () => code(v);
                var workItem = new WorkItem() { Action = action, TaskCompletion = tcs };
                if (_taskQueue.TryAdd(workItem))
                {
                    taskList.Add(tcs.Task);
                }
                else
                {
                    action();
                }
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
                var workItem = new WorkItem() { TaskToRun = t };
                if (MultiThreaded && _taskQueue.TryAdd(workItem))
                {
                    taskList.Add(t);
                }
                else
                    t.RunSynchronously();
            }

            await Task.WhenAll(taskList.ToArray());
        }
    }
}
