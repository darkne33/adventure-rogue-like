using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace CustomPackages.Package.Extensions.Solo.Services
{
    public class UniTaskAwaiter
    {
        private readonly List<UniTask> _tasks = new();

        public bool IsWorking => _tasks.Count > 0;

        public async void Add(UniTask task)
        {
            _tasks.Add(task);
            await task;
            _tasks.Remove(task);
        }
    }
}