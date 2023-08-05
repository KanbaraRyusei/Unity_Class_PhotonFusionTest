using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class Ball : NetworkBehaviour, IPredictedSpawnBehaviour
{
    public float Speed => _speed;

    public int Power => _power;

    public bool IsBumped => _isBumped;

    public LayerMask HitMask => _hitMask;

    public float HitImpulse => _hitImpulse;

    public float LifeTime => _lifeTime;

    public float LifeTimeAfterHit => _lifeTimeAfterHit;

    [SerializeField]
    private float _speed;

    [SerializeField]
    private float _lifeTime = 5.0f;

    [SerializeField]
    private int _power;

    [SerializeField]
    private LayerMask _hitMask;

    [SerializeField]
    private float _hitImpulse = 5f;

    [SerializeField]
    private float _lifeTimeAfterHit = 2f;

    private bool _isBumped;

    [Networked(OnChanged = nameof(OnDestroyChanged))]
    private NetworkBool _isDestroyed { get; set; }

    [Networked]
    private int _fireTick { get; set; }

    [Networked]
    private Vector3 _firePosition { get; set; }

    [Networked]
    private Vector3 _fireVelocity { get; set; }

    [Networked]
    private TickTimer _life { get; set; }

    [Networked]
    private Vector3 _hitPosition { get; set; }

    public void Init(Vector3 position, Vector3 direction)
    {
        _fireTick = Runner.Tick;
        _firePosition = position;
        _fireVelocity = direction * _speed;

        _life = TickTimer.CreateFromSeconds(Runner, _lifeTime);
        _isBumped = false;
    }

    public override void Spawned()
    {
        transform.rotation = Quaternion.LookRotation(_fireVelocity);
    }

    public override void FixedUpdateNetwork()
    {
        bool isProxy = IsProxy && !Object.IsPredictedSpawn;

        if (isProxy) return;

        if (_life.Expired(Runner) && _life.IsRunning)
        {
            Runner.Despawn(Object);
            return;
        }

        if (_isDestroyed) return;

        var previousPosition = GetMovePosition(Runner.Tick - 1);
        var nextPosition = GetMovePosition(Runner.Tick);

        var direction = nextPosition - previousPosition;

        float distance = direction.magnitude;
        direction /= distance;

        var hitOptions = HitOptions.IncludePhysX | HitOptions.IgnoreInputAuthority;
        if (Runner.LagCompensation.Raycast(previousPosition, direction, distance,
                Object.InputAuthority, out var hit, _hitMask, hitOptions))
        {
            _isDestroyed = true;
            _life = TickTimer.CreateFromSeconds(Runner, _lifeTimeAfterHit);

            _hitPosition = hit.Point;

            if (hit.Collider != null && hit.Collider.attachedRigidbody != null)
            {
                //hit.Collider.attachedRigidbody.AddForce(direction * _hitImpulse, ForceMode.Impulse);
            }
        }
    }

    public override void Render()
    {
        if (_isDestroyed)
        {
            ShowDestroyEffect();
            return;
        }

        float renderTime = Object.IsProxy ? Runner.InterpolationRenderTime : Runner.SimulationRenderTime;
        float tick = renderTime / Runner.DeltaTime;

        transform.position = GetMovePosition(tick);
    }

    public void Bump()
    {
        _isBumped = true;
    }

    public static void OnDestroyChanged(Changed<Ball> changed)
    {
        changed.Behaviour.ShowDestroyEffect();
    }

    private Vector3 GetMovePosition(float tick)
    {
        float time = (tick - _fireTick) * Runner.DeltaTime;

        return time <= 0f ? _firePosition : _firePosition + _fireVelocity * time;
    }

    private void ShowDestroyEffect()
    {
        transform.position = _hitPosition;

        Runner.Despawn(Object);
    }

    public void PredictedSpawnSpawned()
    {
        Spawned();
    }

    public void PredictedSpawnUpdate()
    {
        FixedUpdateNetwork();
    }

    public void PredictedSpawnRender()
    {
        Render();
    }

    public void PredictedSpawnFailed()
    {
        Despawned(Runner, false);

        Runner.Despawn(Object, true);
    }

    public void PredictedSpawnSuccess()
    {
        // ‘‚­‚±‚Æ–³‚¢
    }
}
