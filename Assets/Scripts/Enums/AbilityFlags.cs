using System;

namespace NightDriver.Character.Abilities
{
    [Flags]
    public enum AbilityFlags
    {
        None = 0,
        AfterimageSense = 1 << 0,   // 잔상 감지
        DeepAfterimage = 1 << 1,    // 깊은 잔상
        Intuition = 1 << 2,         // 직감
        Foresight = 1 << 3,         // 예지
        Empathy = 1 << 4,           // 공감
        Communion = 1 << 5,         // 교감
    }
}

