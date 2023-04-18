using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class PhysxBall : NetworkBehaviour
{
    [Networked]
    private TickTimer life { get; set; }

    private Rigidbody _rb;

    public void Init(Vector3 forward)
    {
        life = TickTimer.CreateFromSeconds(Runner, 5.0f);
        _rb = GetComponent<Rigidbody>();
        _rb.velocity = forward;
    }

    public override void FixedUpdateNetwork()
    {
        if (life.Expired(Runner))
            Runner.Despawn(Object);
    }
}
