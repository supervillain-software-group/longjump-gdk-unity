using System;
using System.Threading.Tasks;
using UnityEngine;

namespace LongJump
{
    /// <summary>Turns any call that returns a Task into something a coroutine can wait on.</summary>
    public static class CoroutineExtensions
    {
        /// <summary>
        /// Wraps a call for a coroutine: <c>yield return</c> it, then read <see cref="Operation.Error"/>.
        /// It never throws into the coroutine: the GDK's calls report every failure through their Task.
        /// </summary>
        /// <example>
        /// <code>
        /// var report = LongJump.Issues.ReportBugAsync(description).AsCoroutine();
        /// yield return report;
        /// if (report.Error != null) Debug.LogWarning(report.Error.Message);
        /// </code>
        /// </example>
        public static Operation AsCoroutine(this Task task) => new Operation(task ?? Task.CompletedTask);

        /// <summary>Wraps a call with a result for a coroutine; read <see cref="Operation{T}.Result"/> once it's done.</summary>
        public static Operation<T> AsCoroutine<T>(this Task<T> task) => new Operation<T>(task ?? Task.FromResult<T>(default));
    }

    /// <summary>
    /// A call a coroutine is waiting on, from <see cref="CoroutineExtensions.AsCoroutine(Task)"/>:
    /// <c>yield return</c> it, then read <see cref="Error"/>.
    /// </summary>
    public class Operation : CustomYieldInstruction
    {
        private protected readonly Task task;

        internal Operation(Task task)
        {
            this.task = task;
            // Marks a failure as seen, so a game that never reads Error doesn't get an unobserved
            // task exception in its log, which crash capture would report.
            task.ContinueWith(t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
        }

        public override bool keepWaiting => !task.IsCompleted;

        public bool IsDone => task.IsCompleted;

        public bool Succeeded => task.Status == TaskStatus.RanToCompletion;

        /// <summary>Why it failed, or null. An ApiException carries the server's status and error.</summary>
        public Exception Error => task.IsFaulted ? Unwrap(task.Exception) : task.IsCanceled ? new OperationCanceledException() : null;

        static Exception Unwrap(AggregateException e) =>
            e == null ? null : e.InnerExceptions.Count == 1 ? e.InnerException : e;
    }

    /// <summary>A GDK call with a result, for coroutines.</summary>
    public class Operation<T> : Operation
    {
        internal Operation(Task<T> task) : base(task) { }

        /// <summary>The result once it succeeded; otherwise the default.</summary>
        public T Result => Succeeded ? ((Task<T>)task).Result : default;
    }
}
