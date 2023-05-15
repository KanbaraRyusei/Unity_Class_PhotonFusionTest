using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class PhysxBox : NetworkBehaviour
{
    [SerializeField]
    private int _hp;

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.TryGetComponent(out PhysxBall ball))
        {
            if (ball.IsBumped) return;

            ball.Bump();

            if(!RecieveDamage(ball.Power))
            {
                Runner.Despawn(Object);
            }
        }
    }

    private bool RecieveDamage(int damage)
    {
        _hp -= damage;
        return _hp > 0;
    }
}
