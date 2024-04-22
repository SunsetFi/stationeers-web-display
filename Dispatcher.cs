
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace StationeersWebDisplay
{

    internal class QueuedTask
    {
        public Func<object> function;
        public TaskCompletionSource<object> completionSource;
    }

    // From: https://answers.unity.com/questions/305882/how-do-i-invoke-functions-on-the-main-thread.html
    public class Dispatcher : MonoBehaviour
    {
        public static Task RunOnMainThread(Action action)
        {
            return Dispatcher.RunOnMainThread<object>(() =>
            {
                action();
                return null;
            });
        }

        public static Task<T> RunOnMainThread<T>(Func<T> function)
        {
            var source = new TaskCompletionSource<object>();
            var queueItem = new QueuedTask()
            {
                function = () => function(),
                completionSource = source
            };

            lock (_backlog)
            {
                _backlog.Add(queueItem);
                Logging.LogTrace($"Dispatcher now has {_backlog.Count} items");
                _queued = true;
            }

            // Sigh...
            return source.Task.ContinueWith(t => (T)t.Result);
        }

        public static void Initialize()
        {
            if (_instance == null)
            {
                Logging.LogTrace("Creating new dispatcher instance.");
                _instance = StationeersWebDisplayPlugin.Instance.gameObject.AddComponent<Dispatcher>();
            }
        }

        private void Update()
        {
            if (_queued)
            {
                lock (_backlog)
                {
                    var tmp = _actions;
                    _actions = _backlog;
                    _backlog = tmp;
                    _queued = false;
                }

                Logging.LogTrace($"Draining {_actions.Count} items from dispatcher");

                foreach (var action in _actions)
                {
                    try
                    {
                        var result = action.function();
                        Logging.LogTrace($"Dispatcher itme completed successfully.");
                        action.completionSource.TrySetResult(result);
                    }
                    catch (Exception e)
                    {
                        Logging.LogTrace($"Dispatcher item failed with exception  {e.GetType().FullName} {e.Message} {e.StackTrace}");
                        action.completionSource.TrySetException(e);
                    }
                }

                _actions.Clear();
            }
        }

        private void OnDestroy()
        {
            _instance = null;
            Logging.LogError("Dispatcher destroyed!");
        }

        static Dispatcher _instance;
        static volatile bool _queued = false;
        static List<QueuedTask> _backlog = new List<QueuedTask>(8);
        static List<QueuedTask> _actions = new List<QueuedTask>(8);
    }
}