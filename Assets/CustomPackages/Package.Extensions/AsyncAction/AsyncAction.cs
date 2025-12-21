using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;

namespace CustomPackages.Package.Extensions.AsyncAction
{
    public class AsyncAction<T>
    {
        private readonly List<Func<T, UniTask>> _handlers = new();

        public UniTask Invoke(T arg) =>
            UniTask.WhenAll(Enumerable.Select(_handlers, handler => handler.Invoke(arg)));

        public async UniTask InvokeSequentially(T arg)
        {
            foreach (var handler in _handlers)
            {
                await handler.Invoke(arg);
            }
        }

        public static AsyncAction<T> operator +(AsyncAction<T> asyncEvent, Func<T, UniTask> handler)
        {
            asyncEvent.AddHandler(handler);
            return asyncEvent;
        }

        public static AsyncAction<T> operator -(AsyncAction<T> asyncEvent, Func<T, UniTask> handler)
        {
            asyncEvent.RemoveHandler(handler);
            return asyncEvent;
        }

        private void AddHandler(Func<T, UniTask> handler) =>
            _handlers.Add(handler);

        private void RemoveHandler(Func<T, UniTask> handler) =>
            _handlers.Remove(handler);
    }
}