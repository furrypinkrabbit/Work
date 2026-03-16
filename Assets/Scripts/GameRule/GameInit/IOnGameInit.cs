using UnityEngine;
using System.Threading.Tasks;

namespace GameRule.GameInit
{
public interface IOnGameInit
{
    Task InitAsync();
}
}
