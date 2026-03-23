using Assets.Scripts.GameRule.GameLoading;
using Assets.Scripts.GameRule.ResManager;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameEntry : MonoBehaviour
{
    private bool CanLoad = false;
    private float Timer = 0f;
    private float EndTime = 5f;

    private void Start()
    {



    }
    private void Update()
    {
        if (CanLoad) LoadScene();

        Timer += Time.deltaTime;
        if (Timer >= EndTime) {
            CanLoad = true;
            
        }
    }

    private async void LoadScene() {
        AsyncOperation ao = SceneManager.LoadSceneAsync("LoadingScene");
        await ao;
        GameBootBlackBoard.SceneLoaded = ao.isDone;
    }

}
