namespace Backend.Object.UI
{
    /// <summary>
    /// 로비 상단 네비게이션 바. Navigation 레이어에 상주하는 관리형 패널.
    /// 내용 구현은 후속 작업.
    /// </summary>
    public class TopNavBar : UIPanel
    {
        public override UILayer Layer => UILayer.Navigation;
    }
}
