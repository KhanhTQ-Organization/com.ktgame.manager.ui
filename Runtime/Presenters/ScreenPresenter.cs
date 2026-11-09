using Cysharp.Threading.Tasks;

namespace com.ktgame.manager.ui
{
	public abstract class ScreenPresenter<TScreenView> : WindowPresenter<TScreenView>, IScreenPresenter where TScreenView : ScreenView
	{
		protected ScreenPresenter(IUIManager uiManager, IViewContainer viewContainer, IViewConfig viewConfig) : base(uiManager, viewContainer,
			viewConfig) { }

		async UniTask IScreenLifecycleEvent.WillPushEnter()
		{
			await ChildLifecycle.RunAsync(Children, c => c.WillEnter(), this);
			await ViewWillPushEnter(View);
		}

		void IScreenLifecycleEvent.DidPushEnter()
		{
			ChildLifecycle.Run(Children, c => c.DidEnter(), this);
			ViewDidPushEnter(View);
		}

		UniTask IScreenLifecycleEvent.WillPushExit()
		{
			return ViewWillPushExit(View);
		}

		void IScreenLifecycleEvent.DidPushExit()
		{
			ViewDidPushExit(View);
		}

		UniTask IScreenLifecycleEvent.WillPopEnter()
		{
			return ViewWillPopEnter(View);
		}

		void IScreenLifecycleEvent.DidPopEnter()
		{
			ViewDidPopEnter(View);
		}

		async UniTask IScreenLifecycleEvent.WillPopExit()
		{
			await ChildLifecycle.RunAsync(Children, c => c.WillExit(), this);
			await ViewWillPopExit(View);
		}

		void IScreenLifecycleEvent.DidPopExit()
		{
			ChildLifecycle.Run(Children, c => c.DidExit(), this);
			ViewDidPopExit(View);
		}

		public bool FindIndexOfRecentlyPushed(string assetPath, out int index)
		{
			return ViewContainer.As<ScreenContainer>().FindIndexOfRecentlyPushed(assetPath, out index);
		}

		public void BringToFront(ScreenViewConfig config, bool ignoreFront)
		{
			ViewContainer.As<ScreenContainer>().BringToFront(config, ignoreFront);
		}

		public UniTask BringToFrontAsync(ScreenViewConfig config, bool ignoreFront)
		{
			return ViewContainer.As<ScreenContainer>().BringToFrontAsync(config, ignoreFront);
		}

		public void Show(bool stack = true)
		{
			var config = new ScreenViewConfig((ViewConfig)ViewConfig, stack: stack);
			ViewContainer.As<ScreenContainer>().Push<TScreenView>(config);
		}

		public UniTask ShowAsync(bool stack = true)
		{
			var config = new ScreenViewConfig((ViewConfig)ViewConfig, stack: stack);
			return ViewContainer.As<ScreenContainer>().PushAsync<TScreenView>(config);
		}

		public void Hide(bool playAnimation = true)
		{
			ViewContainer.As<ScreenContainer>().Pop(playAnimation);
		}

		public UniTask HideAsync(bool playAnimation = true)
		{
			return ViewContainer.As<ScreenContainer>().PopAsync(playAnimation);
		}

		protected virtual UniTask ViewWillPushEnter(TScreenView view)
		{
			return UniTask.CompletedTask;
		}

		protected virtual void ViewDidPushEnter(TScreenView view) { }

		protected virtual UniTask ViewWillPushExit(TScreenView view)
		{
			return UniTask.CompletedTask;
		}

		protected virtual void ViewDidPushExit(TScreenView view) { }

		protected virtual UniTask ViewWillPopEnter(TScreenView view)
		{
			return UniTask.CompletedTask;
		}

		protected virtual void ViewDidPopEnter(TScreenView view) { }

		protected virtual UniTask ViewWillPopExit(TScreenView view)
		{
			return UniTask.CompletedTask;
		}

		protected virtual void ViewDidPopExit(TScreenView view) { }

		protected override void Initialize(TScreenView view)
		{
			view.AddLifecycleEvent(this, 1);
		}

		protected override void Dispose(TScreenView view)
		{
			view.RemoveLifecycleEvent(this);
		}
	}

	public abstract class ScreenPresenter<TScreenView, TDataSource> : WindowPresenter<TScreenView, TDataSource>, IScreenPresenter<TDataSource>
		where TScreenView : ScreenView where TDataSource : IViewDataSource
	{
		protected ScreenPresenter(IUIManager uiManager, IViewContainer viewContainer, IViewConfig viewConfig) : base(uiManager, viewContainer,
			viewConfig) { }

		async UniTask IScreenLifecycleEvent.WillPushEnter()
		{
			await ChildLifecycle.RunAsync(Children, c => c.WillEnter(), this);
			await ViewWillPushEnter(View);
		}

		void IScreenLifecycleEvent.DidPushEnter()
		{
			ChildLifecycle.Run(Children, c => c.DidEnter(), this);
			ViewDidPushEnter(View);
		}

		UniTask IScreenLifecycleEvent.WillPushExit()
		{
			return ViewWillPushExit(View);
		}

		void IScreenLifecycleEvent.DidPushExit()
		{
			ViewDidPushExit(View);
		}

		UniTask IScreenLifecycleEvent.WillPopEnter()
		{
			return ViewWillPopEnter(View);
		}

		void IScreenLifecycleEvent.DidPopEnter()
		{
			ViewDidPopEnter(View);
		}

		async UniTask IScreenLifecycleEvent.WillPopExit()
		{
			await ChildLifecycle.RunAsync(Children, c => c.WillExit(), this);
			await ViewWillPopExit(View);
		}

		void IScreenLifecycleEvent.DidPopExit()
		{
			ChildLifecycle.Run(Children, c => c.DidExit(), this);
			ViewDidPopExit(View);
		}

		public bool FindIndexOfRecentlyPushed(string assetPath, out int index)
		{
			return ViewContainer.As<ScreenContainer>().FindIndexOfRecentlyPushed(assetPath, out index);
		}

		public void BringToFront(ScreenViewConfig config, bool ignoreFront)
		{
			ViewContainer.As<ScreenContainer>().BringToFront(config, ignoreFront);
		}

		public UniTask BringToFrontAsync(ScreenViewConfig config, bool ignoreFront)
		{
			return ViewContainer.As<ScreenContainer>().BringToFrontAsync(config, ignoreFront);
		}

		public void Show(TDataSource dataSource, bool stack = true)
		{
			DataSource = dataSource;
			var config = new ScreenViewConfig((ViewConfig)ViewConfig, stack: stack);
			ViewContainer.As<ScreenContainer>().Push<TScreenView>(config);
		}

		public UniTask ShowAsync(TDataSource dataSource, bool stack = true)
		{
			DataSource = dataSource;
			var config = new ScreenViewConfig((ViewConfig)ViewConfig, stack: stack);
			return ViewContainer.As<ScreenContainer>().PushAsync<TScreenView>(config);
		}

		public void Hide(bool playAnimation = true)
		{
			ViewContainer.As<ScreenContainer>().Pop(playAnimation);
		}

		public UniTask HideAsync(bool playAnimation = true)
		{
			return ViewContainer.As<ScreenContainer>().PopAsync(playAnimation);
		}

		protected virtual UniTask ViewWillPushEnter(TScreenView view)
		{
			return UniTask.CompletedTask;
		}

		protected virtual void ViewDidPushEnter(TScreenView view) { }

		protected virtual UniTask ViewWillPushExit(TScreenView view)
		{
			return UniTask.CompletedTask;
		}

		protected virtual void ViewDidPushExit(TScreenView view) { }

		protected virtual UniTask ViewWillPopEnter(TScreenView view)
		{
			return UniTask.CompletedTask;
		}

		protected virtual void ViewDidPopEnter(TScreenView view) { }

		protected virtual UniTask ViewWillPopExit(TScreenView view)
		{
			return UniTask.CompletedTask;
		}

		protected virtual void ViewDidPopExit(TScreenView view) { }

		protected override void Initialize(TScreenView view)
		{
			view.AddLifecycleEvent(this, 1);
		}

		protected override void Dispose(TScreenView view)
		{
			view.RemoveLifecycleEvent(this);
		}
	}
}
