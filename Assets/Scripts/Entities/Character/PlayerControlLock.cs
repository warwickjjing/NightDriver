namespace NightDriver.Character
{
    /// <summary>
    /// 플레이어 이동/시야 잠금 (차량 탑승 등).
    /// FirstPersonMover / FirstPersonCamera / PhoneUI 와 함께 사용합니다.
    /// </summary>
    public static class PlayerControlLock
    {
        public static bool VehicleSeated { get; set; }
    }
}
