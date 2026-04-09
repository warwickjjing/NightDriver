namespace NightDriver.Client
{
    /// <summary>
    /// 택시 콜 플로우의 공개 진입점(폰 수락, 목적지 확정 등).
    /// 씬에서는 <see cref="CallFlowController"/>를 구현체로 둡니다.
    /// </summary>
    public interface ICallFlow
    {
        /// <summary>콜 수락: 검증 후 손님/차량 스폰. 성공 시 true.</summary>
        bool TryAcceptCall();

        /// <summary>Yarn에서 목적지가 확정된 뒤 차량 탑승을 허용합니다.</summary>
        void NotifyDestinationChosen();

        /// <summary>목적지 해제 시 탑승을 다시 막습니다.</summary>
        void NotifyDestinationCleared();

        /// <summary>
        /// Yarn 픽업 대화에서 &lt;&lt;pickupComplete&gt;&gt; 호출 시. 목적지 확정과 함께 있어야 차량 탑승이 허용됩니다.
        /// </summary>
        void NotifyPickupDialogueComplete();
    }
}
