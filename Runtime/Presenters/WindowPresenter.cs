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
		
		private readonly Dictionary<IViewPresenter, TWindow> _childOwners = new();

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
			var destroyedView = View;
			base.UnRegister();
			RemoveChildrenOf(destroyedView, true);
		}
		
		async UniTask IViewLifecycleEvent.Initialize()
		{
			await ChildLifecycle.RunAsync(Children, c => c.Initialize(), this);
			await ViewDidLoad(View);
		}

		async UniTask IViewLifecycleEvent.WillEnter()
		{
			await ChildLifecycle.RunAsync(Children, c => c.WillEnter(), this);
			await ViewWillEnter(View);
		}

		void IViewLifecycleEvent.DidEnter()
		{
			ChildLifecycle.Run(Children, c => c.DidEnter(), this);
			ViewDidEnter(View);
		}

		async UniTask IViewLifecycleEvent.WillExit()
		{
			await ChildLifecycle.RunAsync(Children, c => c.WillExit(), this);
			await ViewWillExit(View);
		}

		void IViewLifecycleEvent.DidExit()
		{
			ChildLifecycle.Run(Children, c => c.DidExit(), this);
			ViewDidExit(View);
		}

		async UniTask IViewLifecycleEvent.Cleanup()
		{
			await ChildLifecycle.RunAsync(Children, c => c.Cleanup(), this);
			await ViewWillDestroy(View);
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

		private View[] _cachedChildViews;

		protected TPresenter AddChild<TPresenter, TView>() where TPresenter : IViewPresenter where TView : View
		{
			if (_cachedChildViews == null)
			{
				_cachedChildViews = View.Owner.GetComponentsInChildren<View>(true);
			}

			TView view = null;
			foreach (var v in _cachedChildViews)
			{
				if (v is TView tv)
				{
					view = tv;
					break;
				}
			}

			var type = typeof(TPresenter);
			var presenter = (TPresenter)Activator.CreateInstance(type, UIManager, view);
			presenter.Parent = this;
			Children.Add(presenter);
			_childOwners[presenter] = View;
			return presenter;
		}

		protected TPresenter AddChild<TPresenter, TView>(string viewName) where TPresenter : IViewPresenter where TView : View
		{
			if (_cachedChildViews == null)
			{
				_cachedChildViews = View.Owner.GetComponentsInChildren<View>(true);
			}

			TView view = null;
			foreach (var v in _cachedChildViews)
			{
				if (v is TView tv && tv.Name.Equals(viewName))
				{
					view = tv;
					break;
				}
			}

			if (view == null)
			{
				throw new InvalidOperationException($"View {viewName} not found in {View.GetType().Name}");
			}

			var type = typeof(TPresenter);
			var presenter = (TPresenter)Activator.CreateInstance(type, UIManager, view);
			presenter.Parent = this;
			Children.Add(presenter);
			_childOwners[presenter] = View;
			return presenter;
		}

		private void OnViewLoaded(IView view)
		{
			if (!view.IsInitialized)
			{
				_cachedChildViews = null;

				View = (TWindow)view;
				ViewName = view.Name;
				UIManager.Injector.Resolve(View);
				Initialize(View);
				AddChildren();
				new ViewDestroyedHandler(this, View).UnRegisterWhenGameObjectDestroyed(View.Owner);
				view.IsInitialized = true;
			}
		}

		private void OnViewDestroyed(TWindow destroyedView)
		{
			if (ReferenceEquals(View, destroyedView))
			{
				UnRegister();
				return;
			}
			
			try
			{
				Dispose(destroyedView);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}

			RemoveChildrenOf(destroyedView, false);
		}
		
		private void RemoveChildrenOf(TWindow owner, bool includeUnowned)
		{
			for (var i = Children.Count - 1; i >= 0; i--)
			{
				var child = Children[i];
				var hasOwner = _childOwners.TryGetValue(child, out var childOwner);
				var belongsToOwner = hasOwner ? ReferenceEquals(childOwner, owner) : includeUnowned;

				if (belongsToOwner == false)
				{
					continue;
				}

				Children.RemoveAt(i);
				_childOwners.Remove(child);

				try
				{
					child.Dispose();
				}
				catch (Exception e)
				{
					Debug.LogException(e);
				}
			}
		}
		
		private sealed class ViewDestroyedHandler : IUnRegister
		{
			private WindowPresenter<TWindow> _presenter;
			private readonly TWindow _view;

			public ViewDestroyedHandler(WindowPresenter<TWindow> presenter, TWindow view)
			{
				_presenter = presenter;
				_view = view;
			}

			public void UnRegister()
			{
				var presenter = _presenter;
				_presenter = null;
				presenter?.OnViewDestroyed(_view);
			}
		}
	}
	
	internal static class ChildLifecycle
	{
		public static UniTask RunAsync(List<IViewPresenter> children, Func<IViewPresenter, UniTask> step, object owner)
		{
			if (children.Count == 0)
			{
				return UniTask.CompletedTask;
			}
			
			var tasks = new UniTask[children.Count];
			for (var i = 0; i < children.Count; i++)
			{
				tasks[i] = RunOneAsync(children[i], step, owner);
			}

			return UniTask.WhenAll(tasks);
		}

		public static void Run(List<IViewPresenter> children, Action<IViewPresenter> step, object owner)
		{
			if (children.Count == 0)
			{
				return;
			}

			var snapshot = children.ToArray();
			foreach (var child in snapshot)
			{
				try
				{
					step(child);
				}
				catch (Exception e)
				{
					LogChildError(child, owner, e);
				}
			}
		}

		private static async UniTask RunOneAsync(IViewPresenter child, Func<IViewPresenter, UniTask> step, object owner)
		{
			try
			{
				await step(child);
			}
			catch (OperationCanceledException)
			{
				
			}
			catch (Exception e)
			{
				LogChildError(child, owner, e);
			}
		}

		private static void LogChildError(IViewPresenter child, object owner, Exception e)
		{
			Debug.LogError($"[UI] Child presenter {child?.GetType().Name} of {owner?.GetType().Name} failed: {e.Message}");
			Debug.LogException(e);
		}
	}

	public abstract class WindowPresenter<TWindow, TDataSource> : WindowPresenter<TWindow> where TWindow : IWindow where TDataSource : IViewDataSource
	{
		protected TDataSource DataSource { get; set; }

		protected WindowPresenter(IUIManager uiManager, IViewContainer viewContainer, IViewConfig viewConfig) : base(uiManager, viewContainer,
			viewConfig) { }
	}
}
