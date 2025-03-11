using Cysharp.Threading.Tasks;

namespace com.ktgame.manager.ui
{
	public abstract class PanelPresenter<TPanelView> : WindowPresenter<TPanelView>, IPanelPresenter where TPanelView : PanelView
	{
		protected PanelPresenter(IUIManager uiManager, IViewContainer viewContainer, IViewConfig viewConfig) : base(uiManager, viewContainer,
			viewConfig) { }

		public void Show(SortingLayerId? sortingLayer = null, int? orderInLayer = null)
		{
			var config = new PanelViewConfig((ViewConfig)ViewConfig, sortingLayer: sortingLayer, orderInLayer: orderInLayer);
			ViewContainer.As<PanelContainer>().Show<TPanelView>(this, config);
		}

		public UniTask ShowAsync(SortingLayerId? sortingLayer = null, int? orderInLayer = null)
		{
			var config = new PanelViewConfig((ViewConfig)ViewConfig, sortingLayer: sortingLayer, orderInLayer: orderInLayer);
			return ViewContainer.As<PanelContainer>().ShowAsync<TPanelView>(this, config);
		}

		public void Hide(bool playHideAnimation = true)
		{
			ViewContainer.As<PanelContainer>().Hide(this, playHideAnimation);
		}

		public UniTask HideAsync(bool playHideAnimation = true)
		{
			return ViewContainer.As<PanelContainer>().HideAsync(this, playHideAnimation);
		}

		protected override void Initialize(TPanelView view)
		{
			view.AddLifecycleEvent(this, 1);
		}

		protected override void Dispose(TPanelView view)
		{
			view.RemoveLifecycleEvent(this);
		}
	}

	public abstract class PanelPresenter<TPanelView, TDataSource> : WindowPresenter<TPanelView, TDataSource>, IPanelPresenter<TDataSource>
		where TPanelView : PanelView where TDataSource : IViewDataSource
	{
		protected PanelPresenter(IUIManager uiManager, IViewContainer viewContainer, IViewConfig viewConfig) : base(uiManager, viewContainer, viewConfig) { }

		public void Show(TDataSource dataSource, SortingLayerId? sortingLayer = null, int? orderInLayer = null)
		{
			DataSource = dataSource;
			var config = new PanelViewConfig((ViewConfig)ViewConfig, sortingLayer: sortingLayer, orderInLayer: orderInLayer);
			ViewContainer.As<PanelContainer>().Show<TPanelView>(this, config);
		}

		public UniTask ShowAsync(TDataSource dataSource, SortingLayerId? sortingLayer = null, int? orderInLayer = null)
		{
			DataSource = dataSource;
			var config = new PanelViewConfig((ViewConfig)ViewConfig, sortingLayer: sortingLayer, orderInLayer: orderInLayer);
			return ViewContainer.As<PanelContainer>().ShowAsync<TPanelView>(this, config);
		}

		public void Hide(bool playHideAnimation = true)
		{
			ViewContainer.As<PanelContainer>().Hide(this, playHideAnimation);
		}

		public UniTask HideAsync(bool playHideAnimation = true)
		{
			return ViewContainer.As<PanelContainer>().HideAsync(this, playHideAnimation);
		}

		protected override void Initialize(TPanelView view)
		{
			view.AddLifecycleEvent(this, 1);
		}

		protected override void Dispose(TPanelView view)
		{
			view.RemoveLifecycleEvent(this);
		}
	}
}
