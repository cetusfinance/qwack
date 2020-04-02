using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;

namespace Qwack.Utils.Parallel
{
    public sealed class ParallelUtils : IDisposable
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


        private int _activeThreadCount = 0;
        private static readonly int numThreads = Environment.ProcessorCount;

        private ParallelUtils()
        {
            for(var i = 0; i < numThreads;i++)
            {
                StartThread();
            }
        }

        private void StartThread()
        {
            var thread = new Thread(ThreadStart)
            {
                IsBackground = true,
                Name = $"ParallelUtilsThread{_activeThreadCount}"
            };
            Interlocked.Increment(ref _activeThreadCount);
            thread.Start();
        }

        private void ThreadStart()
        {
            foreach(var item in _taskQueue.GetConsumingEnumerable())
            {
                if (item.TaskCompletion != null)
                {
                    try
                    {
                        if(item.IsExploder)
                        {
                            item.ResetEvent.Set();
                            item.TaskCompletion.SetResult(true);
                            break;
                        }
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

                if (_killQueue.TryTake(out var killTask, 0))
                {
                    killTask.ResetEvent.Set();
                    break;
                }
            }
            Interlocked.Decrement(ref _activeThreadCount);
        }

        public bool MultiThreaded { get; set; } = true;

        private BlockingCollection<WorkItem> _taskQueue = new BlockingCollection<WorkItem>();
        private BlockingCollection<WorkItem> _killQueue = new BlockingCollection<WorkItem>();

        public Task Foreach<T>(IList<T> values, Action<T> code, bool overrideMTFlag = false)
        {
            if (!overrideMTFlag && !string.IsNullOrEmpty(Thread.CurrentThread.Name) && Thread.CurrentThread.Name.StartsWith("ParallelUtilsThread"))
            {
                StartThread();
                RunOptimistically(values, code).Wait();
                var exploder = new WorkItem()
                {
                    IsExploder = true,
                    ResetEvent = new ManualResetEvent(false)
                };
                _killQueue.Add(exploder);
                exploder.ResetEvent.WaitOne();
                return Task.CompletedTask;
            }

            if (overrideMTFlag || !MultiThreaded)
            {
                RunInSeries(values, code);
                return Task.CompletedTask;
            }

            return RunOptimistically(values, code);
        }

        private struct WorkItem
        {
            public Action Action;
            public TaskCompletionSource<bool> TaskCompletion;
            public Task TaskToRun;
            public ManualResetEvent ResetEvent;
            public bool IsExploder;
        }

        public Task For(int startInclusive, int endExclusive, int step, Action<int> code, bool overrideMTFlag = false)
        {
            var sequence = new List<int>();
            for (var v = startInclusive; v < endExclusive; v += step)
                sequence.Add(v);

            return Foreach(sequence, code, overrideMTFlag);
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

        public void Dispose()
        {
            _taskQueue.CompleteAdding();
        }
    }
}
