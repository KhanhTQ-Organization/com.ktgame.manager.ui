using System;
using System.Collections.Generic;
using com.ktgame.unregister;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace com.ktgame.manager.ui
{
	public abstract class WindowPresenter<TWindow> : ViewPresenter<TWindow>, IWindowPresenter where TWindow : IWindow
	{
		public IViewContainer ViewContainer { get; private set; }

		public IViewConfig ViewConfig { get; private set; }

		[SerializeReference] protected readonly List<IViewPresenter> Children = new();

		protected WindowPresenter(IUIManager uiManager, IViewContainer viewContainer, IViewConfig viewConfig) : base(uiManager)
		{
			ViewContainer = viewContainer;
			ViewConfig = viewConfig;
			ViewConfig.SetViewLoadedCallback(OnViewLoaded);
			Initialize();
		}

		private void Initialize()
		{
			if (IsInitialized)
			{
				throw new InvalidOperationException($"{GetType().Name} is already initialized.");
			}

			if (IsDisposed)
			{
				throw new ObjectDisposedException(nameof(WindowPresenter<TWindow>));
			}

			if (UIManager == null)
			{
				throw new ArgumentNullException(nameof(UIManager));
			}

			if (ViewContainer == null)
			{
				throw new ArgumentNullException(nameof(ViewContainer));
			}

			if (ViewConfig == null)
			{
				throw new ArgumentNullException(nameof(ViewConfig));
			}

			if (ViewConfig.PoolingPolicy == PoolingPolicy.UseSettings || ViewConfig.PoolingPolicy == PoolingPolicy.EnablePooling)
			{
				ViewContainer.Preload(ViewConfig.AssetPath);
			}

			UIManager.AddPresenter(this);
			IsInitialized = true;
		}

		public override void Dispose()
		{
			base.Dispose();
			foreach (var child in Children)
			{
				child.Dispose();
			}

			ViewConfig.SetViewLoadedCallback(null);
		}

		public override void UnRegister()
		{
			base.UnRegister();
			Children.Clear();
		}

		UniTask IViewLifecycleEvent.Initialize()
		{
			foreach (var child in Children)
			{
				child.Initialize();
			}

			return ViewDidLoad(View);
		}

		UniTask IViewLifecycleEvent.WillEnter()
		{
			foreach (var child in Children)
			{
				child.WillEnter();
			}

			return ViewWillEnter(View);
		}

		void IViewLifecycleEvent.DidEnter()
		{
			foreach (var child in Children)
			{
				child.DidEnter();
			}

			ViewDidEnter(View);
		}

		UniTask IViewLifecycleEvent.WillExit()
		{
			foreach (var child in Children)
			{
				child.WillExit();
			}

			return ViewWillExit(View);
		}

		void IViewLifecycleEvent.DidExit()
		{
			foreach (var child in Children)
			{
				child.DidExit();
			}

			ViewDidExit(View);
		}

		UniTask IViewLifecycleEvent.Cleanup()
		{
			foreach (var child in Children)
			{
				child.Cleanup();
			}

			return ViewWillDestroy(View);
		}

		protected abstract void AddChildren();

		public IViewPresenter GetChild(Type type)
		{
			foreach (var child in Children)
			{
				if (child.GetType() == type)
				{
					return child;
				}
			}

			UnityEngine.Debug.LogError("Child does not exist " + type.Name);
			return null;
		}

		public IViewPresenter GetChild(string viewName)
		{
			foreach (var child in Children)
			{
				if (child.ViewName == viewName)
				{
					return child;
				}
			}

			UnityEngine.Debug.LogError("Child does not exist " + viewName);
			return null;
		}

		public bool TryGetChild<TPresenter>(out TPresenter childPresenter) where TPresenter : IViewPresenter
		{
			childPresenter = default;
			var type = typeof(TPresenter);
			foreach (var child in Children)
			{
				if (child.GetType() != type)
				{
					continue;
				}

				childPresenter = (TPresenter)child;
				return true;
			}

			UnityEngine.Debug.LogError("Child does not exist " + type.Name);
			return false;
		}

		public bool TryGetChild<TPresenter>(string viewName, out TPresenter childPresenter) where TPresenter : IViewPresenter
		{
			childPresenter = default;
			foreach (var child in Children)
			{
				if (child.ViewName != viewName)
				{
					continue;
				}

				childPresenter = (TPresenter)child;
				return true;
			}

			UnityEngine.Debug.LogError("Child does not exist " + viewName);
			return false;
		}

		protected TPresenter AddChild<TPresenter, TView>() where TPresenter : IViewPresenter where TView : View
		{
			var type = typeof(TPresenter);
			var view = View.Owner.GetComponentInChildren<TView>(true);
			var presenter = (TPresenter)Activator.CreateInstance(type, UIManager, view);
			presenter.Parent = this;
			Children.Add(presenter);
			return presenter;
		}

		protected TPresenter AddChild<TPresenter, TView>(string viewName) where TPresenter : IViewPresenter where TView : View
		{
			var type = typeof(TPresenter);
			var views = View.Owner.GetComponentsInChildren<TView>(true);
			foreach (var view in views)
			{
				if (view.Name.Equals(viewName))
				{
					var presenter = (TPresenter)Activator.CreateInstance(type, UIManager, view);
					presenter.Parent = this;
					Children.Add(presenter);
					return presenter;
				}
			}

			throw new InvalidOperationException($"View {viewName} not found in {View.GetType().Name}");
		}

		private void OnViewLoaded(IView view)
		{
			if (!view.IsInitialized)
			{
				View = (TWindow)view;
				ViewName = view.Name;
				UIManager.Injector.Resolve(View);
				Initialize(View);
				AddChildren();
				this.UnRegisterWhenGameObjectDestroyed(View.Owner);
				view.IsInitialized = true;
			}
		}
	}

	public abstract class WindowPresenter<TWindow, TDataSource> : WindowPresenter<TWindow> where TWindow : IWindow where TDataSource : IViewDataSource
	{
		protected TDataSource DataSource { get; set; }

		protected WindowPresenter(IUIManager uiManager, IViewContainer viewContainer, IViewConfig viewConfig) : base(uiManager, viewContainer,
			viewConfig) { }
	}
}
