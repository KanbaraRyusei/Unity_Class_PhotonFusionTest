using UnityEngine;
using Fusion;
using TMPro;

public class Player : NetworkBehaviour
{
    [Networked(OnChanged = nameof(OnBallSpawned))]
    public NetworkBool spawned { get; set; }

    [SerializeField]
    private Ball _prefabBall;

    [SerializeField]
    private PhysxBall _prefabPhysxBall;

    [SerializeField]
    private GameObject _dummyBall;

    [SerializeField]
    private float _delayTime = 0.5f;

    [SerializeField]
    private int _initialHp;

    [SerializeField]
    private Transform _fireTransform;

    [SerializeField]
    private LayerMask _hitMask;

    [Networked]
    private TickTimer delay { get; set; }

    [Networked]
    [Capacity(32)]
    private NetworkArray<ProjectileData> _projectileData { get; }

    private GameObject[] _projectiles = new GameObject[64];

    private NetworkCharacterControllerPrototype _ccp;

    private Vector3 _forward;

    private Vector3 _up;

    private int _hp;

    private int _fireCount;

    private int _visibleFireCount;

    private TMP_Text _messages;

    private Material _material;

    Material material
    {
        get
        {
            if (_material == null)
                _material = GetComponentInChildren<MeshRenderer>().material;
            return _material;
        }
    }

    private void Awake()
    {
        _ccp = GetComponent<NetworkCharacterControllerPrototype>();
        _forward = transform.forward;
        _up = transform.up;
        _hp = _initialHp;
    }

    private void Update()
    {
        if (Object.HasInputAuthority && Input.GetKeyDown(KeyCode.R))
        {
            RPC_SendMessage("Hey Mate!");
        }

        if (Object.HasInputAuthority && Input.GetKeyDown(KeyCode.H))
        {
            RPC_SendMessage("Hello");
        }

        if (Object.HasInputAuthority && Input.GetKeyDown(KeyCode.B))
        {
            RPC_SendMessage("Bye");
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent(out Ball ball))
        {
            if (ball.IsBumped) return;

            ball.Bump();

            if (!RecieveDamage(ball.Power))
            {
                Runner.Despawn(Object);
            }
        }

        if (collision.gameObject.TryGetComponent(out PhysxBall physxBall))
        {
            if (physxBall.IsBumped) return;

            physxBall.Bump();

            if (!RecieveDamage(physxBall.Power))
            {
                Runner.Despawn(Object);
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (IsProxy == true)
            return;

        if (GetInput(out NetworkInputData data))
        {
            data.direction.Normalize();
            _ccp.Move(5 * data.direction * Runner.DeltaTime);

            if (data.direction.sqrMagnitude > 0)
                _forward = data.direction;

            if (delay.ExpiredOrNotRunning(Runner))
            {
                if ((data.buttons & NetworkInputData.MOUSEBUTTON1) != 0)
                {
                    delay = TickTimer.CreateFromSeconds(Runner, _delayTime);
                    //Runner.Spawn(_prefabBall,
                    //  transform.position + _forward,
                    //  Quaternion.LookRotation(_forward),
                    //  Object.InputAuthority,
                    //  (runner, o) =>
                    //  {
                    //      o.GetComponent<Ball>().Init(_fireTransform.position, _fireTransform.forward);
                    //  });
                    Fire();

                    spawned = !spawned;
                }
                else if ((data.buttons & NetworkInputData.MOUSEBUTTON2) != 0)
                {
                    delay = TickTimer.CreateFromSeconds(Runner, _delayTime);
                    Runner.Spawn(_prefabPhysxBall,
                      transform.position + _forward,
                      Quaternion.LookRotation(_forward),
                      Object.InputAuthority,
                      (runner, o) =>
                      {
                          o.GetComponent<PhysxBall>().Init(10 * _forward);
                      });
                    spawned = !spawned;
                }
            }
        }

        int tick = Runner.Tick;

        for (int i = 0; i < _projectileData.Length; i++)
        {
            var pData = _projectileData[i];

            if (pData.IsActive == false)
                continue;
            if (pData.FinishTick <= tick)
                continue;

            UpdateProjectile(ref pData, tick);

            _projectileData.Set(i, pData);
        }
    }

    public static void OnBallSpawned(Changed<Player> changed)
    {
        changed.Behaviour.material.color = Color.white;
    }

    public override void Spawned()
    {
        _visibleFireCount = _fireCount;
    }

    public override void Render()
    {
        material.color = Color.Lerp(material.color, Color.blue, Time.deltaTime);

        for (int i = _visibleFireCount; i < _fireCount; i++)
        {
            int index = i % _projectileData.Length;
            var data = _projectileData[index];

            var previousProjectile = _projectiles[index];
            if (previousProjectile != null)
            {
                Destroy(previousProjectile.gameObject);
            }

            var projectile = Instantiate(_dummyBall, data.FirePosition, Quaternion.LookRotation(data.FireVelocity));

            Runner.MoveToRunnerScene(projectile);

            _projectiles[index] = projectile;

            //var dummyProjectile = Instantiate(_prefabBall, _fireTransform.position, _fireTransform.rotation);

            //dummyProjectile.SetHitPosition(data.HitPos);

            //Runner.MoveToRunnerScene(dummyProjectile);
        }

        float renderTime = Object.IsProxy == true ? Runner.InterpolationRenderTime : Runner.SimulationRenderTime;
        float floatTick = renderTime / Runner.DeltaTime;

        // Update projectile visuals
        for (int i = 0; i < _projectiles.Length; i++)
        {
            var index = i % _projectileData.Length;

            var projectile = _projectileData[index];
            var projectileObject = _projectiles[index];

            if (projectile.IsActive == false || projectile.FinishTick < floatTick)
            {
                if (projectileObject != null)
                {
                    Destroy(projectileObject.gameObject);
                }

                continue;
            }

            if (projectile.HitPos != Vector3.zero)
            {
                projectileObject.transform.position = projectile.HitPos;
                //projectileObject.ShowHitEffect();
            }
            else
            {
                projectileObject.transform.position = GetMovePosition(ref projectile, floatTick);
            }
        }

        _visibleFireCount = _fireCount;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_SendMessage(string message, RpcInfo info = default)
    {
        if (_messages == null)
            _messages = FindObjectOfType<TMP_Text>();

        if (info.IsInvokeLocal)
            message = $"You said: {message}\n";
        else
            message = $"Some other player said: {message}\n";
        _messages.text += message;
    }

    private bool RecieveDamage(int damage)
    {
        _hp -= damage;
        return _hp > 0;
    }

    private void Fire()
    {
        //var hitPosition = Vector3.zero;

        //var hitOptions = HitOptions.IncludePhysX | HitOptions.IgnoreInputAuthority;

        //if (Runner.LagCompensation.Raycast(_fireTransform.position, _fireTransform.position - transform.position, 100f,
        //        Object.InputAuthority, out var hit, _hitMask, hitOptions) == true)
        //{
        //    if (hit.Collider != null && hit.Collider.attachedRigidbody != null)
        //    {
        //        hit.Collider.attachedRigidbody.AddForce(_fireTransform.forward * _prefabBall.HitImpulse, ForceMode.Impulse);
        //    }

        //    hitPosition = hit.Point;
        //}

        _projectileData.Set(_fireCount % _projectileData.Length, new ProjectileData()
        {
            FireTick = Runner.Tick,
            FirePosition = _fireTransform.position,
            FireVelocity = _fireTransform.forward * _prefabBall.Speed,
            FinishTick = Runner.Tick + Mathf.RoundToInt(_prefabBall.LifeTime / Runner.DeltaTime)
        });

        _fireCount++;
    }

    private void UpdateProjectile(ref ProjectileData projectileData, int tick)
    {
        if (projectileData.HitPos != Vector3.zero)
            return;

        var previousPosition = GetMovePosition(ref projectileData, tick - 1f);
        var nextPosition = GetMovePosition(ref projectileData, tick);

        var direction = nextPosition - previousPosition;

        float distance = direction.magnitude;
        direction /= distance;

        var hitOptions = HitOptions.IncludePhysX | HitOptions.IgnoreInputAuthority;

        if (Runner.LagCompensation.Raycast(previousPosition, direction, distance,
                Object.InputAuthority, out var hit, _hitMask, hitOptions) == true)
        {
            projectileData.HitPos = hit.Point;
            projectileData.FinishTick = tick + Mathf.RoundToInt(_prefabBall.LifeTimeAfterHit / Runner.DeltaTime);

            if (hit.Collider != null && hit.Collider.attachedRigidbody != null)
            {
                hit.Collider.attachedRigidbody.AddForce(direction * _prefabBall.HitImpulse, ForceMode.Impulse);

                if(hit.Collider.TryGetComponent(out Player player))
                {
                    player.RecieveDamage(_prefabBall.Power);
                }
            }
        }
    }

    private Vector3 GetMovePosition(ref ProjectileData data, float currentTick)
    {
        float time = (currentTick - data.FireTick) * Runner.DeltaTime;

        if (time <= 0f)
            return data.FirePosition;

        return data.FirePosition + data.FireVelocity * time;
    }

    private struct ProjectileData : INetworkStruct
    {
        public bool IsActive => FireTick > 0;

        public int FireTick;
        public int FinishTick;

        public Vector3 FirePosition;
        public Vector3 FireVelocity;

        [Networked, Accuracy(0.01f)]
        public Vector3 HitPos { get; set; }
    }
}
