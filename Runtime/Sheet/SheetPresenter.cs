using System;
using System.Collections.Generic;
using com.ktgame.unregister;
using Cysharp.Threading.Tasks;

namespace com.ktgame.manager.ui
{
	public abstract class SheetPresenter<TSheetView> : ViewPresenter<TSheetView>, ISheetPresenter where TSheetView : SheetView
	{
		public IViewConfig ViewConfig { get; private set; }
		public IViewContainer ViewContainer { get; private set; }

		protected readonly List<IViewPresenter> Children = new();
		
		private bool _childrenAdded;

		protected SheetPresenter(IUIManager uiManager, TSheetView view) : base(uiManager, view)
		{
			if (!View.IsInitialized)
			{
				this.UnRegisterWhenGameObjectDestroyed(View.Owner);
				View.IsInitialized = true;
			}
		}

		async UniTask IViewLifecycleEvent.Initialize()
		{
			if (_childrenAdded == false)
			{
				_childrenAdded = true;
				AddChildren();
			}

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

		public override void UnRegister()
		{
			base.UnRegister();
			Children.Clear();
			_childrenAdded = false;
		}

		public override void Dispose()
		{
			base.Dispose();
			foreach (var child in Children)
			{
				child.Dispose();
			}
		}

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

		protected TPresenter AddChild<TPresenter, TView>() where TPresenter : IViewPresenter where TView : View
		{
			var type = typeof(TPresenter);
			var view = View.Owner.GetComponentInChildren<TView>(true);
			var presenter = (TPresenter)Activator.CreateInstance(type, UIManager, view);
			presenter.Parent = this;
			Children.Add(presenter);
			return presenter;
		}

		protected TPresenter AddChild<TPresenter>(string viewName) where TPresenter : IViewPresenter
		{
			var type = typeof(TPresenter);
			if (View.TryGetView(viewName, out var view))
			{
				var presenter = (TPresenter)Activator.CreateInstance(type, UIManager, view);
				Children.Add(presenter);
				presenter.Parent = this;
				return presenter;
			}
			else
			{
				throw new InvalidOperationException($"View {viewName} not found in {View.GetType().Name}");
			}
		}

		protected abstract void AddChildren();

		protected override void Initialize(TSheetView view)
		{
			view.AddLifecycleEvent(this, 1);
		}

		protected override void Dispose(TSheetView view)
		{
			view.RemoveLifecycleEvent(this);
		}
	}
}
