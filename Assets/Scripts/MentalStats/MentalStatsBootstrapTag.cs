using UnityEngine;

namespace SeeAPsychologist.MentalStats
{
    /// <summary>
    /// 标记：该对象由 MentalStatsBootstrapper 在运行时创建。
    /// 用于处理“场景里也放了 MentalStatsManager”时的单例优先级选择：
    /// - 优先保留场景内实例（便于 Inspector 引用稳定）
    /// - 场景内不存在时才保留引导创建的实例
    /// </summary>
    internal sealed class MentalStatsBootstrapTag : MonoBehaviour
    {
    }
}

