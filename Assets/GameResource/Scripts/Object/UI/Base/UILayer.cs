namespace Backend.Object.UI
{
    /// <summary>
    /// UI 가 배치될 Canvas 레이어 구분.
    /// - HUD: 인게임 상시 UI
    /// - Panel: 메인 화면/메뉴 패널
    /// - Popup: 모달 팝업 (최상위)
    /// </summary>
    public enum UILayer
    {
        HUD,
        Panel,
        Popup,
    }
}
