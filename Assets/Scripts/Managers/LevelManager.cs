namespace Fusion.Collywobbles.Futsal
{
    using Fusion;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.SceneManagement;


    public class LevelManager : NetworkSceneManagerBase
    {
        [Header("Single Peer Options")]
        public int PostLoadDelayFrames = 1;

        //[SerializeField] private UIScreen _dummyScreen;
        //[SerializeField] private UIScreen _lobbyScreen;

        public static LevelManager Instance => Singleton<LevelManager>.Instance;


        public static void LoadMatch(int sceneIndex)
        {
            Instance.Runner.SetActiveScene(sceneIndex);
        }

        protected virtual YieldInstruction LoadSceneAsync(SceneRef sceneRef, LoadSceneParameters parameters, Action<Scene> loaded)
        {
            //Debug.Log("[LevelManager] LoadSceneAsync start...");

            if (!TryGetScenePathFromBuildSettings(sceneRef, out var scenePath))
            {
                throw new InvalidOperationException($"Not going to load {sceneRef}: unable to find the scene name");
            }

            var op = SceneManager.LoadSceneAsync(scenePath, parameters);
            Assert.Check(op);

            bool alreadyHandled = false;

            // if there's a better way to get scene struct more reliably I'm dying to know
            UnityAction<Scene, LoadSceneMode> sceneLoadedHandler = (scene, _) => {
                if (IsScenePathOrNameEqual(scene, scenePath))
                {
                    Assert.Check(!alreadyHandled);
                    alreadyHandled = true;
                    loaded(scene);
                }
            };

            SceneManager.sceneLoaded += sceneLoadedHandler;

            op.completed += _ => {
                SceneManager.sceneLoaded -= sceneLoadedHandler;
            };

            return op;
        }

        protected virtual YieldInstruction UnloadSceneAsync(Scene scene)
        {
            return SceneManager.UnloadSceneAsync(scene);
        }

        protected override IEnumerator SwitchScene(SceneRef prevScene, SceneRef newScene, FinishedLoadingDelegate finished)
        {
            Scene loadedScene;
            Scene activeScene = SceneManager.GetActiveScene();

            List<NetworkObject> sceneObjects = new List<NetworkObject>();

            bool canTakeOverActiveScene = prevScene == default && IsScenePathOrNameEqual(activeScene, newScene);

            if (canTakeOverActiveScene)
            {
                Debug.Log($"[LevelManager] Not going to load initial scene {newScene} as this is the currently active scene");
                loadedScene = activeScene;
            }
            else if (newScene > (int)SceneDefs.LOBBY)
            {
                Debug.Log($"[LevelManager] Start loading scene {newScene} in single peer mode");
                LoadSceneParameters loadSceneParameters = new LoadSceneParameters(LoadSceneMode.Single);

                loadedScene = default;
                //Debug.Log($"[LevelManager] Loading scene {newScene} with parameters: {JsonUtility.ToJson(loadSceneParameters)}");

                yield return LoadSceneAsync(newScene, loadSceneParameters, scene => loadedScene = scene);

                //Debug.Log($"[LevelManager] Loaded scene {newScene} with parameters: {JsonUtility.ToJson(loadSceneParameters)}: {loadedScene}");

                if (!loadedScene.IsValid())
                {
                    throw new InvalidOperationException($"Failed to load scene {newScene}: async op failed");
                }

                sceneObjects = FindNetworkObjects(loadedScene, disable: true);
            }

            finished(sceneObjects);

            for (int i = PostLoadDelayFrames; i > 0; --i)
            {
                yield return null;
            }
        }



    }
}
