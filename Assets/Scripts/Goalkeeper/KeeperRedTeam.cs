namespace Fusion.Collywobbles.Futsal
{
    using UnityEngine;

    public class KeeperRedTeam : KeeperController
    {
        public override void Spawned()
        {
            base.Spawned();

            // Position keeper in goal
            transform.position = new Vector3(20.6f, 0.5f, 0);
            transform.rotation = Quaternion.Euler(new Vector3(0, -90, 0));

            _serverBall.AddGoalkeeper(transform);
        }
    }
}