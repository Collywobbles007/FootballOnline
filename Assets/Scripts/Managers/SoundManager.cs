/***************************************************************
 * Class: SoundManager
 * Created by: Colin Gall-McDaid
 * Date: 9 October, 2022
 * 
 * Description: Used to instantiate and play all the game sound
 * effects.
 ***************************************************************/

namespace Fusion.Collywobbles.Futsal
{
    using Unity.VisualScripting;
    using UnityEngine;

    public class SoundManager : MonoBehaviour
    {
        public enum Effect { Cheer, KickHard, KickSoft, Net, Bounce, Miss, ShortWhistle, LongWhistle }

        public static AudioClip kickHard, kickSoft, hitNet, bounce;
        public static AudioClip shortWhistle, longWhistle;

        private static AudioSource audioSource;
        private static AudioSource backgroundAudio;


        // Start is called before the first frame update
        void Start()
        {
            kickHard = Resources.Load<AudioClip>("Sound Effects/KickBall_Hard");
            kickSoft = Resources.Load<AudioClip>("Sound Effects/KickBall_Soft");
            hitNet = Resources.Load<AudioClip>("Sound Effects/HitNet");
            bounce = Resources.Load<AudioClip>("Sound Effects/BallBounce_Grass");

            shortWhistle = Resources.Load<AudioClip>("Sound Effects/WhistleShort");
            longWhistle = Resources.Load<AudioClip>("Sound Effects/WhistleLong");

            audioSource = GetComponent<AudioSource>();
            backgroundAudio = GetComponent<AudioSource>();
        }

        private void Update()
        {
        }

        public static void PlaySound(Effect sound, float volume)
        {
            switch (sound)
            {
                case Effect.KickHard:
                    audioSource.PlayOneShot(kickHard);
                    break;

                case Effect.KickSoft:
                    audioSource.PlayOneShot(kickSoft);
                    break;

                case Effect.Net:
                    audioSource.PlayOneShot(hitNet);
                    break;

                case Effect.Bounce:
                    audioSource.PlayOneShot(bounce, volume);
                    break;

                case Effect.ShortWhistle:
                    audioSource.PlayOneShot(shortWhistle);
                    break;

                case Effect.LongWhistle:
                    audioSource.PlayOneShot(longWhistle);
                    break;

                default:
                    break;

            }
        }
    }
}

