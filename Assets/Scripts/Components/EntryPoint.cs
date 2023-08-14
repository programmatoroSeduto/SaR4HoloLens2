using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;


namespace Project.Scripts.Components
{
    public class EntryPoint : MonoBehaviour
    {
        
        public string SceneName = "";

        public bool UseDelay = false;

        public int DelaySeconds = 3;
        
        private bool isLoadingScene = false;
        private Coroutine COR_SwichScene;

        // Start is called before the first frame update
        void Start()
        {
            COR_SwichScene = StartCoroutine(ORCOR_SwitchSceneAdditive("", this.SceneName));
        }

        public void EVENT_ChangeScene(string sceneName)
        {
            Debug.Log($"EVENT_ChangeScene(sceneName = {sceneName})");
            if (SceneName == "")
            {
                Debug.LogError($"Not given a scene to load!");
                return;
            }
            if (sceneName == this.SceneName)
            {
                Debug.LogError($"Trying to reload the same scene");
                return;
            }
            if (!isLoadingScene)
                COR_SwichScene = StartCoroutine(ORCOR_SwitchSceneAdditive(this.SceneName, sceneName));
        }

        private IEnumerator ORCOR_SwitchSceneAdditive(string fromScene, string toScene)
        {
            isLoadingScene = true;
            yield return null;

            if (UseDelay)
                yield return new WaitForSecondsRealtime((float)DelaySeconds);

            if (fromScene != "")
            {
                fromScene = (fromScene.StartsWith("Scene") ? fromScene : $"Scene{fromScene}");
                Debug.Log($"Unloading scene '{fromScene}' ... ");
                UnityEngine.AsyncOperation op = SceneManager.UnloadSceneAsync(fromScene);
                if (op == null)
                {
                    Debug.LogError($"Cannot unload scene '{fromScene}'");
                    yield break;
                }
                yield return op;
                Debug.Log($"Unloading scene '{fromScene}' ... OK");
            }
            if (toScene != "")
            {
                toScene = (toScene.StartsWith("Scene") ? toScene : $"Scene{toScene}");
                Debug.Log($"Loading scene '{toScene}' ... ");
                UnityEngine.AsyncOperation op = SceneManager.LoadSceneAsync(toScene, LoadSceneMode.Additive);
                if (op == null)
                {
                    Debug.LogError($"Cannot load scene '{fromScene}'");
                    yield break;
                }
                yield return op;
                Debug.Log($"Loading scene '{toScene}' ... OK");
            }
            this.SceneName = toScene;
            yield return null;
            isLoadingScene = false;
        }
    }
}
