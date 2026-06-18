using System;
using Core.Sounds;

namespace Infrastructure.SaveSystem
{
    [Serializable]
    public class PlayerGlobalSaveData : GlobalSaveData
    {
        public SoundsSaveData SoundsSaveData;
    }
}
