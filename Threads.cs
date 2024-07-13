using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class ThreadPoolInstance
{
    private static readonly int poolSize = (int)(Environment.ProcessorCount * 0.5f);
    private readonly ConcurrentQueue<(Action task, Action callback)> taskQueue = new();
    private readonly ManualResetEvent newTaskEvent = new(false);
    private readonly ManualResetEvent terminateEvent = new(false);
    private readonly WaitHandle[] waitHandles;
    private readonly Thread[] workers;

    public ThreadPoolInstance()
    {
        waitHandles = new WaitHandle[] { newTaskEvent, terminateEvent };
        workers = new Thread[poolSize];

        for (int i = 0; i < poolSize; i++)
        {
            workers[i] = new Thread(Worker);
            workers[i].Start();
        }
    }

    private void Worker()
    {
        while (true)
        {
            int index = WaitHandle.WaitAny(waitHandles);
            if (index == 1) // terminateEvent
            {
                return;
            }

            if (taskQueue.TryDequeue(out var taskPair))
            {
                try
                {
                    taskPair.task.Invoke();
                }
                catch
                {
                    //optional logging here
                }
                finally
                {
                    taskPair.callback?.Invoke();
                    Thread.Sleep(5);
                }
            }
            else
            {
                newTaskEvent.Reset();
            }
        }
    }

    public void Async(Action task)
    {
        taskQueue.Enqueue((task, null));
        newTaskEvent.Set();
    }

    public Task WhenAll(List<Action> tasks)
    {
        int remainingTasks = tasks.Count;
        var tcs = new TaskCompletionSource<bool>();

        Action callback = () =>
        {
            if (Interlocked.Decrement(ref remainingTasks) == 0)
            {
                tcs.SetResult(true);
            }
        };

        foreach (var task in tasks)
        {
            taskQueue.Enqueue((task, callback));
            newTaskEvent.Set();
        }

        return tcs.Task;
    }

    public void Dispose()
    {
        terminateEvent.Set();
        foreach (Thread worker in workers)
        {
            worker.Join();
        }

        newTaskEvent.Dispose();
        terminateEvent.Dispose();
    }
}

public static class Threads
{
    private static readonly ConcurrentDictionary<int, ThreadPoolInstance> threadPools = new();
    private static readonly ConcurrentQueue<Action> syncQueue = new();

    public static void CreatePool(int poolNumber)
    {
        threadPools[poolNumber] = new ThreadPoolInstance();
    }

    public static void Async(Action task)
    {
        threadPools[1].Async(task);
    }
    public static void Async(int poolNumber, Action task)
    {
        threadPools[poolNumber].Async(task);
    }

    public static Task WhenAll(int poolNumber, List<Action> tasks)
    {
        return threadPools[poolNumber].WhenAll(tasks);
    }

    public static void Dispose(int poolNumber)
    {
        threadPools[poolNumber].Dispose();
    }

    public static void Sync(Action task)
    {
        syncQueue.Enqueue(task);
    }

    public static void RunSync(int millisecondsTimeout)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        while (stopwatch.ElapsedMilliseconds < millisecondsTimeout && syncQueue.TryDequeue(out Action task))
        {
            task.Invoke();
        }
    }
}
