using System.Collections;
using System.Collections.Generic;
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
    private float _delayTime = 0.5f;

    [SerializeField]
    private int _initialHp;

    [SerializeField]
    private Transform _fireTransform;

    [Networked]
    private TickTimer delay { get; set; }

    private NetworkCharacterControllerPrototype _ccp;

    private Vector3 _forward;

    private Vector3 _up;

    private int _hp;

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

        if(collision.gameObject.TryGetComponent(out PhysxBall physxBall))
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
                    Runner.Spawn(_prefabBall,
                      transform.position + _forward,
                      Quaternion.LookRotation(_forward),
                      Object.InputAuthority,
                      (runner, o) =>
                      {
                          o.GetComponent<Ball>().Init(_fireTransform.position, _fireTransform.forward);
                      });
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
    }

    public static void OnBallSpawned(Changed<Player> changed)
    {
        changed.Behaviour.material.color = Color.white;
    }

    public override void Render()
    {
        material.color = Color.Lerp(material.color, Color.blue, Time.deltaTime);
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
}
