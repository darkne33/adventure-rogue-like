using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CustomPackages.Package.Extensions
{
    public static class UniTaskExtensions
    {
        public static async UniTask WaitForClick(this Button button, CancellationToken token)
        {
            var utc = new UniTaskCompletionSource();
            button.onClick.AddListener(Call());
            token.Register(() => { utc.TrySetCanceled(); });
            await utc.Task;
            button.onClick.RemoveListener(Call());
            return;

            UnityAction Call()
            {
                return () => utc.TrySetResult();
            }
        }

        public static UniTask WaitForClick(this List<Button> buttons, CancellationToken token) =>
            UniTask.WhenAny(Enumerable.Select(buttons, button => button.WaitForClick(token)));

        public static void SetInput(this List<Button> buttons, bool state)
        {
            foreach (var button in buttons)
            {
                button.interactable = state;
            }
        }
    }
}