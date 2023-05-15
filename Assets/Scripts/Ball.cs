using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class Ball : NetworkBehaviour
{
    public int Power => _power;

    public bool IsBumped => _isBumped;

    [SerializeField]
    private float _lifeTime = 5.0f;

    [SerializeField]
    private int _power;

    private bool _isBumped;

    [Networked]
    private TickTimer life { get; set; }

    public void Init()
    {
        life = TickTimer.CreateFromSeconds(Runner, _lifeTime);
        _isBumped = false;
    }

    public override void FixedUpdateNetwork()
    {
        if (life.Expired(Runner))
            Runner.Despawn(Object);
        else
            transform.position += 5 * transform.forward * Runner.DeltaTime;
    }

    public void Bump()
    {
        _isBumped = true;
    }
}
