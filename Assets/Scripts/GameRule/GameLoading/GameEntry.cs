using UnityEngine;
using UnityEngine.SceneManagement;

public class GameEntry : MonoBehaviour
{

    private async void Start()
    {
        await SceneManager.LoadSceneAsync("LoadingScene");
    }


}
