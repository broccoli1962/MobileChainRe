namespace Backend.Object.UI
{
    /// <summary>
    /// MVP 패턴의 Presenter 비제네릭 베이스. View 와의 결합을 담당.
    /// Model 은 추후 DataManager 로 이관 예정이라 현재는 미도입.
    /// </summary>
    public abstract class UIPresenter
    {
        protected UIBase BaseView { get; private set; }

        internal void AttachView(UIBase view)
        {
            BaseView = view;
            OnAttached();
        }

        protected virtual void OnAttached() { }

        public virtual void OnOpen() { }
        public virtual void OnClose() { }
    }

    /// <summary>
    /// 강타입 View 참조를 가지는 Presenter 베이스.
    /// </summary>
    public abstract class UIPresenter<TView> : UIPresenter where TView : UIBase
    {
        protected TView View => BaseView as TView;
    }
}
