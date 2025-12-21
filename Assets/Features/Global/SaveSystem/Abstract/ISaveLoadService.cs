using Cysharp.Threading.Tasks;

namespace Infrastructure.SaveSystem
{
    public interface ISaveLoadService
    {
        public void Register(object subject);
        public void Unregister(object subject);
        public UniTask Save();
        public void Load();
        void Load(object subject);
        public UniTask Clear();
        UniTask InitializeAndLoad();
        void RewriteExistSaveData();
    }
}