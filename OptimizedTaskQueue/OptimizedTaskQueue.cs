using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OptimizedTaskQueue
{
    /// <summary>
    /// This class maintains list of tasks that are ordered dynamically to maintain completion order. 
    /// Purpose of this class is to replace Task.WhenAny for massive task operations. Regular .net core implementation uses O(N*N) continuation for waiting N tasks
    /// that become expensive when number of tasks reach thousands. This queue supposed to work in O(N) continuations and outputs tasks in completed order minimizing
    /// process latency
    /// </summary>
    public class OptimizedTaskQueue<T>
    {
        private readonly ConcurrentQueue<TaskCompletionSource<T>> pendingBuckets = new ConcurrentQueue<TaskCompletionSource<T>>();
        private readonly ConcurrentQueue<TaskCompletionSource<T>> completedBuckets = new ConcurrentQueue<TaskCompletionSource<T>>();
        private readonly ILog log;

        public OptimizedTaskQueue(ILog log)
        {
            this.log = log;
        }

        public void Enqueue(IEnumerable<Task<T>> tasks)
        {
            foreach (var task in tasks)
            {
                var bucket = new TaskCompletionSource<T>();
                pendingBuckets.Enqueue(bucket);
                task.ContinueWith(MoveToCompletedAndPropagateToBucket, 
                    CancellationToken.None,
                    // todo are we really need to use sync continuation in TrySetResult? 
                    // ExecuteSynchronously Specifies that the continuation task should be executed synchronously.
                    // With this option specified, the continuation will be run on the same thread that causes the antecedent task to transition into its final state.
                    // If the antecedent is already complete when the continuation is created, the continuation will run on the thread creating the continuation.
                    // Only very short-running continuations should be executed synchronously.
                    //  that can introduce some issues
                    TaskContinuationOptions.ExecuteSynchronously, 
                    // todo if we managed not to use sync above, .Default should be replaced to .Current
                    TaskScheduler.Default);
            }
        }

        /// <summary>
        /// Output of the list. Returns oldest completed task. If there are not completed tasks, returns the pole position of pending tasks.
        /// If there were not task added to the list at all, returns null.
        /// This method id not re-enterable from other threads.
        /// </summary>
        public Task<T> Peek =>
            // Note the possible race condition: task can be completed between the check of completedBuckets and pendingBucket
            // If the race condition met, we can end up with situation when first pending slipped to completed and we exposed second pending
            // In worst case we will wait until next available event, keep already completed in a line. Not critical, but probably we could come
            // up with some smart locking and more straightforward behavior.
            completedBuckets.TryPeek(out var result) || pendingBuckets.TryPeek(out result) ? result.Task : null;

        /// <summary>
        /// Remove recently peeked task. 
        /// </summary>
        public void Dequeue(Task<T> taskToRemove)
        {
            // Search in the completed queue. Task supposed ot be on the pole position, but could be tasks that slipped ahead because of race condition.
            var slippedCompleted = new List<TaskCompletionSource<T>>();
            TaskCompletionSource<T> completed;
            while (completedBuckets.TryDequeue(out completed) && completed.Task != taskToRemove)
                slippedCompleted.Add(completed);

            // Requeue slippers at the end if any
            foreach (var slipper in slippedCompleted) 
                completedBuckets.Enqueue(slipper);


            if (completed?.Task != taskToRemove)
            {
                // Task was not found in the completed list. it can be double call to Remove or calling Remove before task got awaited.
                log.Error(new Exception($"Task #{taskToRemove.Id} not found in the completed list."), null);
            }

        }

        private void MoveToCompletedAndPropagateToBucket(Task<T> completed)
        {
            //log.Info($"task #{completed.Id} completed, putting result into bucket");
            if (!pendingBuckets.TryDequeue(out var bucket))
            {
                // Something gone wrong in the queue logic, we should have bucket available.
                // Since nobody awaits us, exception can be propagated
                // Just logging error
                log.Error(new Exception("No available bucket for the completed task"), null);
                // Miss the task completion. Retry logic should take care of this.
                return;
            }

            //log.Info($"task #{bucket.Task.Id} moved from pending to completed");
            completedBuckets.Enqueue(bucket);

            if (!PropagateResult(completed, bucket))
            {
                // No ideas what can be wrong here. async context mismatch?
                log.Error(new Exception("can't set the task result"), null);
                
                Dequeue(bucket.Task);
            }

        }

        /// <summary> 
        /// Propagates the status of the given task (which must be completed) to a task completion source (which should not be). 
        /// </summary> 
        private bool PropagateResult(Task<T> task, TaskCompletionSource<T> bucket)
        {
            switch (task.Status)
            {
                case TaskStatus.Canceled:
                    return bucket.TrySetCanceled();

                case TaskStatus.Faulted:
                    return bucket.TrySetException(task.Exception?.InnerExceptions);

                case TaskStatus.RanToCompletion:
                    return bucket.TrySetResult(task.Result);

                case TaskStatus.Created:
                case TaskStatus.Running:
                case TaskStatus.WaitingForActivation:
                case TaskStatus.WaitingForChildrenToComplete:
                case TaskStatus.WaitingToRun:
                    throw new ArgumentException($"Task has status {task.Status} but expected to be in one of the completed states.");
                default:
                    throw new ArgumentException($"Task has unexpected status {task.Status}.");
            }
        }

    }
}