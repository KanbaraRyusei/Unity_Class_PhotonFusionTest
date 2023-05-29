using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class PhysxBall : NetworkBehaviour
{
    [Networked(OnChanged = nameof(OnBallBumped))]
    public NetworkBool bumped { get; set; }

    public int Power => _power;

    public bool IsBumped => _isBumped;

    [SerializeField]
    private float _lifeTime = 5.0f;

    [SerializeField]
    private int _power;

    [Networked]
    private TickTimer life { get; set; }

    private Rigidbody _rb;

    private bool _isBumped;

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

    private void OnCollisionEnter(Collision collision)
    {
        bumped = true;
    }

    public void Init(Vector3 forward)
    {
        life = TickTimer.CreateFromSeconds(Runner, _lifeTime);
        _rb = GetComponent<Rigidbody>();
        _rb.velocity = forward;
        _isBumped = false;
    }

    public override void FixedUpdateNetwork()
    {
        if (life.Expired(Runner))
            Runner.Despawn(Object);
    }

    public void Bump()
    {
        _isBumped = true;
    }

    public static void OnBallBumped(Changed<PhysxBall> changed)
    {
        changed.Behaviour.material.color = Color.cyan;
    }
}
