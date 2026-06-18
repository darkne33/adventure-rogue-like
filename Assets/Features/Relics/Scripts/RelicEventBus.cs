using System;
using Features.Enemies.Scripts;
using UnityEngine;

namespace Features.Relics.Scripts
{
    public sealed class RelicEventBus
    {
        public event Action<RelicHitEvent> Hit;
        public event Action<RelicKillEvent> Kill;
        public event Action<RelicDamageTakenEvent> DamageTaken;
        public event Action<RelicHealEvent> Heal;
        public event Action<RelicRoomEvent> RoomStarted;
        public event Action<RelicMoveDistanceEvent> MoveDistance;
        public event Action<RelicBossSpawnEvent> BossSpawned;
        public event Action<Vector3> ChestOpened;
        public event Action<RoomData, Room, Vector3> ChestSpawned;
        public event Action<RoomData, Room> ChestCollected;
        public event Action ChestsCleared;

        public void PublishHit(RelicHitEvent hitEvent) =>
            Hit?.Invoke(hitEvent);

        public void PublishKill(RelicKillEvent killEvent) =>
            Kill?.Invoke(killEvent);

        public void PublishDamageTaken(RelicDamageTakenEvent damageTakenEvent) =>
            DamageTaken?.Invoke(damageTakenEvent);

        public void PublishHeal(RelicHealEvent healEvent) =>
            Heal?.Invoke(healEvent);

        public void PublishRoomStarted(RelicRoomEvent roomEvent) =>
            RoomStarted?.Invoke(roomEvent);

        public void PublishMoveDistance(RelicMoveDistanceEvent moveDistanceEvent) =>
            MoveDistance?.Invoke(moveDistanceEvent);

        public void PublishBossSpawned(RelicBossSpawnEvent bossSpawnEvent) =>
            BossSpawned?.Invoke(bossSpawnEvent);

        public void PublishChestOpened(Vector3 position) =>
            ChestOpened?.Invoke(position);

        public void PublishChestSpawned(RoomData roomData, Room room, Vector3 position) =>
            ChestSpawned?.Invoke(roomData, room, position);

        public void PublishChestCollected(RoomData roomData, Room room) =>
            ChestCollected?.Invoke(roomData, room);

        public void PublishChestsCleared() =>
            ChestsCleared?.Invoke();
    }

    public readonly struct RelicHitEvent
    {
        public CharacterFacade Attacker { get; }
        public EnemyFacade Target { get; }
        public int Damage { get; }
        public bool IsCritical { get; }
        public string WeaponId { get; }
        public Vector3 HitPosition { get; }

        public RelicHitEvent(CharacterFacade attacker, EnemyFacade target, int damage,
            bool isCritical, string weaponId, Vector3 hitPosition)
        {
            Attacker = attacker;
            Target = target;
            Damage = damage;
            IsCritical = isCritical;
            WeaponId = weaponId;
            HitPosition = hitPosition;
        }
    }

    public readonly struct RelicKillEvent
    {
        public CharacterFacade Killer { get; }
        public EnemyFacade Target { get; }
        public Vector3 Position { get; }
        public string SourceId { get; }

        public RelicKillEvent(CharacterFacade killer, EnemyFacade target, Vector3 position,
            string sourceId = null)
        {
            Killer = killer;
            Target = target;
            Position = position;
            SourceId = sourceId;
        }
    }

    public readonly struct RelicDamageTakenEvent
    {
        public CharacterFacade Victim { get; }
        public EnemyFacade Attacker { get; }
        public int Amount { get; }
        public string DamageType { get; }

        public RelicDamageTakenEvent(CharacterFacade victim, EnemyFacade attacker, int amount,
            string damageType)
        {
            Victim = victim;
            Attacker = attacker;
            Amount = amount;
            DamageType = damageType;
        }
    }

    public readonly struct RelicHealEvent
    {
        public CharacterFacade Target { get; }
        public float Amount { get; }

        public RelicHealEvent(CharacterFacade target, float amount)
        {
            Target = target;
            Amount = amount;
        }
    }

    public readonly struct RelicRoomEvent
    {
        public RoomData RoomData { get; }
        public Room Room { get; }
        public Vector3 CharacterPosition { get; }

        public RelicRoomEvent(RoomData roomData, Room room, Vector3 characterPosition)
        {
            RoomData = roomData;
            Room = room;
            CharacterPosition = characterPosition;
        }
    }

    public readonly struct RelicMoveDistanceEvent
    {
        public CharacterFacade Character { get; }
        public float Distance { get; }
        public float TotalDistance { get; }

        public RelicMoveDistanceEvent(CharacterFacade character, float distance, float totalDistance)
        {
            Character = character;
            Distance = distance;
            TotalDistance = totalDistance;
        }
    }

    public readonly struct RelicBossSpawnEvent
    {
        public EnemyFacade Boss { get; }
        public Vector3 Position { get; }

        public RelicBossSpawnEvent(EnemyFacade boss, Vector3 position)
        {
            Boss = boss;
            Position = position;
        }
    }
}
