using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Scripts.GameRule.GameLoading
{
   public class GameBoot : MonoBehaviour
    {
         void Awake()
        {
            
        }

        void FixedUpdate()
        {
            if (GameBootBlackBoard.SceneLoaded) {
                SceneManager.LoadScene("Lobby");
            }

        }


    }
}
