namespace Features.Relics.Scripts
{
    public sealed class RelicChestRollService
    {
        public bool IsRolling { get; private set; }

        public bool TryBegin()
        {
            if (IsRolling)
                return false;

            IsRolling = true;
            return true;
        }

        public void Finish() =>
            IsRolling = false;
    }
}
