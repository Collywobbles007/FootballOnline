namespace Fusion.Collywobbles.Futsal
{
    using UnityEngine;

    [CreateAssetMenu(fileName = "New Pitch", menuName = "Scriptable Object/Pitch Definition")]
    public class PitchDefinitions : ScriptableObject
    {
        public string pitchName;
        public Sprite pitchIcon;
        public int buildIndex;
    }
}
