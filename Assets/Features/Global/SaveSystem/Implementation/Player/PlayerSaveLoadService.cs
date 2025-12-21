using Infrastructure.SaveSystem.Implementation;

namespace Infrastructure.SaveSystem
{
    public class PlayerSaveLoadService : SaveLoadServiceBase<PlayerGlobalSaveData>, IPlayerSaveLoadService
    {
        protected override string SaveKey => "Player";
        protected override string CollectionName => "PlayerSaveData";
    }
}