namespace com.ktgame.manager.ui
{
    public interface ISheetPresenter : IWindowPresenter, ISheetLifecycleEvent { }
    
    public interface ISheetPresenter<in TDataSource> : ISheetPresenter, ISheetLifecycleEvent where TDataSource : IViewDataSource { }
}
