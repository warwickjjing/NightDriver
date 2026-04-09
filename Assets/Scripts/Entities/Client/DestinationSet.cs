using UnityEngine;

namespace NightDriver.Client
{
    /// <summary>
    /// 씬의 목적지(ID → Transform) 목록을 관리합니다.
    /// SceneLocationSet을 상속해 Dictionary 기반 O(1) 조회를 제공합니다.
    ///
    /// Inspector에서 Entries 리스트에 id와 Transform을 등록하면 됩니다.
    /// </summary>
    [AddComponentMenu("NightDriver/Client/Destination Set")]
    public sealed class DestinationSet : SceneLocationSet { }
}
