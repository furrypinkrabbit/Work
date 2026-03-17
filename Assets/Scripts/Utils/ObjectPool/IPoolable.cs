using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.Utils.ObjectPool
{
   public interface IPoolable
    {
        void OnCreate();
        void OnGet();
        void OnRelease();
        void OnDestory();
    }
}
