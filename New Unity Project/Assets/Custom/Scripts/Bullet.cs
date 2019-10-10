using UnityEngine;
using System.Collections;
using UnityEngine.Networking;


public class Bullet : NetworkBehaviour
{

    void OnCollisionEnter()
    {
        //Need to enable different behaviors for entering player colliders and environment colliders
        //on entering player controller, enter death
        //on entering environment collider, destroy bullet
        Destroy(gameObject);
    }
}

