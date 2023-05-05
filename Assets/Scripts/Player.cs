using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class Player : NetworkBehaviour
{
    [SerializeField]
    private Ball _prefabBall;

    [SerializeField]
    private PhysxBall _prefabPhysxBall;

    [SerializeField]
    private float _delayTime = 0.5f;

    [Networked]
    private TickTimer delay { get; set; }

    private NetworkCharacterControllerPrototype _ccp;

    private Vector3 _forward;

    private Vector3 _up;

    private void Awake()
    {
        _ccp = GetComponent<NetworkCharacterControllerPrototype>();
        _forward = transform.forward;
        _up = transform.up;
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
                          o.GetComponent<Ball>().Init();
                      });
                }
                else if ((data.buttons & NetworkInputData.MOUSEBUTTON2) != 0)
                {
                    delay = TickTimer.CreateFromSeconds(Runner, _delayTime);
                    Runner.Spawn(_prefabPhysxBall,
                      transform.position + _up,
                      Quaternion.LookRotation(_up),
                      Object.InputAuthority,
                      (runner, o) =>
                      {
                          o.GetComponent<PhysxBall>().Init(10 * _up);
                      });
                }
            }
        }
    }
}
