using Assets.Scripts.GameRule.ResManager;
using UnityEngine;

public class TestUILoader : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var uiCtrl = UIController.Instance;
        uiCtrl.LoadUIAsync("backpack", true,null,(obj)=> { });
     //   uiCtrl.ShowUI("backpack");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
