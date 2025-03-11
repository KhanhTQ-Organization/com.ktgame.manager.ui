using System;
using com.ktgame.unregister;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;

namespace com.ktgame.manager.ui
{
	public abstract class ViewPresenter<TView> : IViewPresenter, IUnRegister where TView : IView
	{
		public bool IsDisposed { get; private set; }

		public bool IsInitialized { get; protected set; }

		[ShowInInspector, ReadOnly] public string ViewName { get; protected set; }

		public TView View { get; set; }

		public IWindowPresenter Parent { get; set; }

		protected IUIManager UIManager { get; private set; }

		protected ViewPresenter(IUIManager uiManager)
		{
			UIManager = uiManager;
		}

		protected ViewPresenter(IUIManager uiManager, TView view) : this(uiManager)
		{
			View = view;
			UIManager.Injector.Resolve(View);
			UIManager.Injector.Resolve(this);
			Initialize();
		}

		UniTask IViewLifecycleEvent.Initialize()
		{
			return ViewDidLoad(View);
		}

		UniTask IViewLifecycleEvent.WillEnter()
		{
			return ViewWillEnter(View);
		}

		void IViewLifecycleEvent.DidEnter()
		{
			ViewDidEnter(View);
		}

		UniTask IViewLifecycleEvent.WillExit()
		{
			return ViewWillExit(View);
		}

		void IViewLifecycleEvent.DidExit()
		{
			ViewDidExit(View);
		}

		UniTask IViewLifecycleEvent.Cleanup()
		{
			return ViewWillDestroy(View);
		}

		private void Initialize()
		{
			if (IsInitialized)
			{
				throw new InvalidOperationException($"{GetType().Name} is already initialized.");
			}

			if (IsDisposed)
			{
				throw new ObjectDisposedException(nameof(ViewPresenter<TView>));
			}

			if (UIManager == null)
			{
				throw new ArgumentNullException(nameof(UIManager));
			}

			ViewName = View.Name;
			Initialize(View);
			IsInitialized = true;
		}

		public virtual void Dispose()
		{
			if (!IsInitialized)
			{
				return;
			}

			if (IsDisposed)
			{
				return;
			}

			IsDisposed = true;
		}

		public virtual void UnRegister()
		{
			if (View != null)
			{
				Dispose(View);
			}
		}

		protected virtual UniTask ViewDidLoad(TView view)
		{
			return UniTask.CompletedTask;
		}

		protected virtual UniTask ViewWillEnter(TView view)
		{
			return UniTask.CompletedTask;
		}

		protected virtual void ViewDidEnter(TView view) { }

		protected virtual UniTask ViewWillExit(TView view)
		{
			return UniTask.CompletedTask;
		}

		protected virtual void ViewDidExit(TView view) { }

		protected virtual UniTask ViewWillDestroy(TView view)
		{
			return UniTask.CompletedTask;
		}

		protected abstract void Initialize(TView view);

		protected abstract void Dispose(TView view);
	}

	public abstract class ViewPresenter<TView, TDataSource> : ViewPresenter<TView> where TView : IView where TDataSource : IViewDataSource
	{
		public TDataSource DataSource { get; set; }

		protected ViewPresenter(IUIManager uiManager, TView view) : base(uiManager, view) { }
	}
}
