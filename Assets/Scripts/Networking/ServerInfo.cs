namespace Fusion.Collywobbles.Futsal
{
    using UnityEngine;

    public static class ServerInfo
    {
        public const int UserCapacity = 8; //the actual hard limit

        public static string LobbyName;
        public static string PitchName => ResourceManager.Instance.pitches[PitchId].pitchName;

        public static int GameMode
        {
            get => PlayerPrefs.GetInt("S_GameMode", 0);
            set => PlayerPrefs.SetInt("S_GameMode", value);
        }

        public static int PitchId
        {
            get => PlayerPrefs.GetInt("S_PitchId", 0);
            set => PlayerPrefs.SetInt("S_PitchId", value);
        }

        public static int MaxUsers
        {
            get => PlayerPrefs.GetInt("S_MaxUsers", 4);
            set => PlayerPrefs.SetInt("S_MaxUsers", Mathf.Clamp(value, 1, UserCapacity));
        }
    }
}
