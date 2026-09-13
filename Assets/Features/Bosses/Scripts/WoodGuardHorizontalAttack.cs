using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace Features.Bosses.Scripts
{
    public sealed class WoodGuardHorizontalAttack : IBossAttack
    {
        private readonly WoodGuardHorizontalAttackConfiguration _configuration;
        private readonly BossFacade _boss;
        private readonly CharacterFacade _character;

        public WoodGuardHorizontalAttack(WoodGuardHorizontalAttackConfiguration configuration,
            BossFacade boss, CharacterFacade character)
        {
            _configuration = configuration;
            _boss = boss;
            _character = character;
        }

        public async UniTask Execute(CancellationToken cancellationToken, bool animateBoss = true)
        {
            WoodGuardHorizontalAttackPiece prefab = _configuration.PiecePrefab;
            if (prefab == null || !prefab.HasHitCollider || _configuration.IndicatorMaterial == null)
            {
                Debug.LogError("Horizontal wood attack needs a piece prefab with an assigned Hit Collider " +
                               "and an indicator material.", _boss);
                return;
            }

            WoodGuardHorizontalAttackPiece.Geometry geometry = _configuration.GetPieceGeometry();
            if (geometry.Size.x <= 0f || geometry.Size.y <= 0f || geometry.Size.z <= 0f)
            {
                Debug.LogError("Horizontal wood attack piece collider must have a non-zero size on every axis.",
                    prefab);
                return;
            }
            var lines = new Line[_configuration.Count];
            Vector3 origin = _boss.AttackOrigin.position;
            Quaternion rotation = _boss.AttackRotation;
            float elapsed = 0f;
            float warningEnd = _configuration.TelegraphDuration;
            float attackEnd = warningEnd + _configuration.RootLifetime;
            if (animateBoss)
                _boss.AnimationSystem.BeginAttack(warningEnd);

            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (_boss == null || _boss.IsDead || !_boss.isActiveAndEnabled ||
                        _character == null || _character.HealthSystem.IsDead || _character.IsTransitionPaused)
                        return;

                    float timeScale = _boss.RelicTimeScale;
                    if (animateBoss)
                        _boss.AnimationSystem.SetTimeScale(timeScale);
                    if (timeScale > 0f && _boss.CanAttack)
                    {
                        for (int i = 0; i < lines.Length; i++)
                        {
                            float lineTime = Mathf.Min(elapsed, attackEnd) -
                                             i * _configuration.SafeLineStartInterval;
                            if (lineTime < 0f)
                                continue;
                            Line line = lines[i] ??= new Line
                            {
                                Center = _configuration.GetLineCenter(origin, rotation, i, geometry.Footprint),
                                Pieces = new Piece[_configuration.PieceCount]
                            };
                            for (int j = 0; j < line.Pieces.Length; j++)
                            {
                                float pieceTime = lineTime - _configuration.GetPieceStartDelay(j);
                                if (pieceTime < 0f)
                                    continue;
                                Piece piece = line.Pieces[j] ??= CreatePiece(line.Center, rotation, i, j, geometry);
                                UpdatePiece(piece, line, pieceTime, rotation, prefab, geometry);
                                if (_boss == null || _boss.IsDead || _character == null ||
                                    _character.HealthSystem.IsDead || cancellationToken.IsCancellationRequested)
                                    return;
                            }
                        }

                        if (animateBoss && elapsed >= warningEnd)
                            _boss.AnimationSystem.IdleAnimation();
                        if (elapsed >= attackEnd)
                            break;
                        elapsed += Time.deltaTime * timeScale;
                    }
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }
            }
            finally
            {
                foreach (Line line in lines)
                {
                    if (line == null)
                        continue;
                    foreach (Piece piece in line.Pieces)
                    {
                        if (piece == null)
                            continue;
                        DestroyObject(piece.Indicator);
                        if (piece.Roots != null)
                            DestroyObject(piece.Roots.gameObject);
                    }
                }
            }
        }

        private Piece CreatePiece(Vector3 lineCenter, Quaternion rotation, int lineIndex, int pieceIndex,
            WoodGuardHorizontalAttackPiece.Geometry geometry)
        {
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
            indicator.name = $"WoodGuard_Line_{lineIndex + 1}_Piece_{pieceIndex + 1}_Warning";
            Collider collider = indicator.GetComponent<Collider>();
            collider.enabled = false;
            Object.Destroy(collider);
            MeshRenderer renderer = indicator.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = _configuration.IndicatorMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            Vector3 center = _configuration.GetPieceCenter(lineCenter, rotation, pieceIndex, geometry.Footprint);
            var piece = new Piece
            {
                Center = center,
                SurfacePosition = center - rotation * geometry.CenterOffset,
                BoxRotation = rotation * geometry.BoxRotation,
                Indicator = indicator
            };
            indicator.transform.rotation = piece.BoxRotation;
            UpdateWarning(piece, 0f, geometry);
            return piece;
        }

        private void UpdateWarning(Piece piece, float progress, WoodGuardHorizontalAttackPiece.Geometry geometry)
        {
            float scale = Mathf.Lerp(Mathf.Clamp(_configuration.InitialWarningScale, 0.01f, 1f),
                1f, Mathf.Clamp01(progress));
            piece.Indicator.transform.localScale = geometry.Size * scale;
            // Keep the bottom on the ground while growing upwards and outwards.
            piece.Indicator.transform.position = piece.Center -
                Vector3.up * (geometry.Footprint.y * (1f - scale) * 0.5f);
        }

        private void UpdatePiece(Piece piece, Line line, float pieceTime, Quaternion rotation,
            WoodGuardHorizontalAttackPiece prefab, WoodGuardHorizontalAttackPiece.Geometry geometry)
        {
            float warningDuration = _configuration.SafeWarningDuration;
            if (pieceTime < warningDuration)
            {
                UpdateWarning(piece, pieceTime / warningDuration, geometry);
                return;
            }

            float undergroundDepth = Mathf.Max(Mathf.Max(0.01f, _configuration.UndergroundDepth),
                geometry.Footprint.y + 0.01f);
            if (!piece.HasEmerged)
            {
                piece.HasEmerged = true;
                DestroyObject(piece.Indicator);
                piece.Roots = Object.Instantiate(prefab, piece.SurfacePosition + Vector3.down * undergroundDepth,
                    rotation * geometry.RootRotation);
                piece.Roots.EnableObstacle();
                if (!line.HasHitCharacter)
                    line.HasHitCharacter = ApplyHit(piece, rotation, geometry.Size);
            }

            if (piece.Roots == null)
                return;
            float rootTime = pieceTime - warningDuration;
            float riseDuration = Mathf.Max(0.01f, _configuration.RiseDuration);
            float holdEnd = riseDuration + Mathf.Max(0f, _configuration.HoldDuration);
            float visibleProgress = rootTime < riseDuration ? Mathf.Clamp01(rootTime / riseDuration) :
                rootTime <= holdEnd ? 1f :
                1f - Mathf.Clamp01((rootTime - holdEnd) / Mathf.Max(0.01f, _configuration.SinkDuration));
            piece.Roots.transform.position = piece.SurfacePosition +
                Vector3.down * (undergroundDepth * (1f - visibleProgress));
            if (rootTime >= _configuration.RootLifetime)
                DestroyObject(piece.Roots.gameObject);
        }

        private bool ApplyHit(Piece piece, Quaternion rotation, Vector3 size)
        {
            Collider[] hits = Physics.OverlapBox(piece.Center, size * 0.5f, piece.BoxRotation,
                Physics.AllLayers, QueryTriggerInteraction.Collide);
            foreach (Collider hit in hits)
            {
                if (hit.GetComponentInParent<CharacterFacade>() != _character)
                    continue;
                if (_character.ReceiveDamage(_configuration.Damage, _boss) &&
                    _character.Rigidbody != null && !_character.Rigidbody.isKinematic)
                {
                    Vector3 direction = rotation * Vector3.forward;
                    if (Vector3.Dot(_character.transform.position - piece.Center, direction) < 0f)
                        direction = -direction;
                    _character.Rigidbody.AddForce(direction * Mathf.Max(0f, _configuration.KnockbackForce) +
                        Vector3.up * Mathf.Max(0f, _configuration.KnockbackUpwardForce), ForceMode.Impulse);
                }
                // Multiple character colliders and neighbouring pieces still allow only one hit per line.
                return true;
            }
            return false;
        }

        private static void DestroyObject(GameObject instance)
        {
            if (instance == null)
                return;
            instance.SetActive(false);
            Object.Destroy(instance);
        }

        private sealed class Line
        {
            public Vector3 Center;
            public Piece[] Pieces;
            public bool HasHitCharacter;
        }

        private sealed class Piece
        {
            public Vector3 Center;
            public Vector3 SurfacePosition;
            public Quaternion BoxRotation;
            public GameObject Indicator;
            public WoodGuardHorizontalAttackPiece Roots;
            public bool HasEmerged;
        }
    }
}
