namespace Fusion.Collywobbles.Futsal
{
    using UnityEngine;

    [CreateAssetMenu(fileName = "New Match Type", menuName = "Scriptable Object/Match Type")]
    public class MatchType : ScriptableObject
    {
        public string matchTypeName; // Friendly or Competitive
        public int matchLength; // 10, 20, 30
        public string matchLengthUnit; // minutes
        public int numPlayers; // 4, 8, 12, 20
        public string matchSizeName; // 3-a-side, 5-a-side, 7-a-side, 11-a-side
    }
}
