namespace Fusion.Collywobbles.Futsal
{
    using UnityEngine;

    public class ResourceManager : MonoBehaviour
    {
        public GameUI hudPrefab;
        public PlayerDefinitions[] playerDefinitions;
        public MatchType[] matchTypes;
        public PitchDefinitions[] pitches;

        public static ResourceManager Instance => Singleton<ResourceManager>.Instance;

        private void Awake()
        {
            //DontDestroyOnLoad(gameObject);
        }
    }
}
