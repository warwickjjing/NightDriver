using UnityEngine;

namespace NightDriver.Client
{
    /// <summary>
    /// 씬의 하차/퇴장 포인트(ID → Transform) 목록을 관리합니다.
    /// SceneLocationSet을 상속해 Dictionary 기반 O(1) 조회를 제공합니다.
    /// </summary>
    [AddComponentMenu("NightDriver/Client/Exit Point Set")]
    public sealed class ExitPointSet : SceneLocationSet { }
}

