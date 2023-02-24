namespace Fusion.Collywobbles.Futsal
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class BallBoundary : SimulationBehaviour
    {
        public void OnTriggerEnter(Collider other)
        {
            Debug.Log("Triggered by " + other.name);
        }

        public void OnCollisionEnter(Collision collision)
        {
            Debug.Log("Collision by " + collision.collider.name);
        }
    }
}
