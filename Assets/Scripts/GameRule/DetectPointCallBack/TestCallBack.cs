using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.GameRule.DetectPointCallBack
{
   public class TestCallBack : MonoBehaviour, IDetectCallBack
    {
        public void OnDetectCallBack()
        {
            Debug.Log("回调测试。。。");
        }
    }
}
