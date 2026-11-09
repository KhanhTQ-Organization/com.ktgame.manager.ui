using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace com.ktgame.manager.ui
{
	public class PanelContainer : WindowContainer
	{
		[ShowInInspector, ReadOnly] private readonly Dictionary<IWindowPresenter, ViewRef<PanelView>> _panelViews = new();
		
		private readonly HashSet<IWindowPresenter> _showingPresenters = new();
		private readonly HashSet<IWindowPresenter> _hidingPresenters = new();

		public override int ViewCount => _panelViews.Count;
		
		public IReadOnlyDictionary<IWindowPresenter, ViewRef<PanelView>> PanelViews => _panelViews;

		protected override void OnDestroy()
		{
			base.OnDestroy();

			foreach ((var panelView, var assetPath) in _panelViews.Values)
			{
				DestroyAndForget(panelView, assetPath, PoolingPolicy.DisablePooling).Forget();
			}

			_panelViews.Clear();
		}

		public void Show<TPanelView>(IWindowPresenter presenter, PanelViewConfig config, bool playAnimation) where TPanelView : PanelView
		{
			ShowAndForget<TPanelView>(presenter, config, playAnimation).Forget();
		}

		private async UniTask ShowAndForget<TPanelView>(IWindowPresenter presenter, PanelViewConfig config, bool playAnimation) where TPanelView : PanelView
		{
			await ShowAsyncInternal<TPanelView>(presenter, config, playAnimation);
		}

		public async UniTask ShowAsync<TPanelView>(IWindowPresenter presenter, PanelViewConfig config, bool playAnimation = true) where TPanelView : PanelView
		{
			await ShowAsyncInternal<TPanelView>(presenter, config, playAnimation);
		}

		public void Hide(IWindowPresenter presenter, bool playAnimation = true)
		{
			HideAndForget(presenter, playAnimation).Forget();
		}

		private async UniTaskVoid HideAndForget(IWindowPresenter presenter, bool playAnimation)
		{
			await HideAsyncInternal(presenter, playAnimation);
		}

		public async UniTask HideAsync(IWindowPresenter presenter, bool playAnimation = true)
		{
			await HideAsyncInternal(presenter, playAnimation);
		}

		public void HideAll(bool playAnimation = true)
		{
			HideAllAndForget(playAnimation).Forget();
		}

		private async UniTaskVoid HideAllAndForget(bool playAnimation = true)
		{
			await HideAllAsyncInternal(playAnimation);
		}

		public async UniTask HideAllAsync(bool playAnimation = true)
		{
			await HideAllAsyncInternal(playAnimation);
		}

		private async UniTask ShowAsyncInternal<TPanelView>(IWindowPresenter presenter, PanelViewConfig config, bool playAnimation) where TPanelView : PanelView
		{
			var assetPath = config.Config.AssetPath;
			if (assetPath == null)
			{
				throw new ArgumentNullException(nameof(assetPath));
			}
			
			if (_showingPresenters.Contains(presenter))
			{
				Debug.LogWarning($"[PanelContainer] {typeof(TPanelView).Name} at `{assetPath}` is already being shown, waiting for it instead of showing again.");
				await WaitWhileAsync(() => _showingPresenters.Contains(presenter));
				return;
			}
			
			if (_panelViews.TryGetValue(presenter, out var viewRef))
			{
				Debug.LogWarning($"Cannot transition because the {typeof(TPanelView).Name} at `{assetPath}` is already showing.", viewRef.View);
				return;
			}

			_showingPresenters.Add(presenter);
			
			if (Settings.EnableInteractionInTransition == false)
			{
				Interactable = false;
			}

			var completed = false;
			try
			{
				var panelView = await GetViewAsync<TPanelView>(config.Config);
				config.Config.ViewLoadedCallback?.Invoke(panelView);

				await panelView.AfterLoadAsync(RectTransform);

				panelView.SetSortingLayer(config.SortingLayer, config.OrderInLayer);

				UIManager.ViewWillOpen?.Invoke(panelView);
				
				await panelView.BeforeEnterAsync(true);

				await panelView.EnterAsync(true, playAnimation);

				_panelViews.Add(presenter, new ViewRef<PanelView>(panelView, presenter.ViewConfig.AssetPath, config.Config.PoolingPolicy));
				
				panelView.AfterEnter(true);

				if (Settings.EnableInteractionInTransition == false)
				{
					Interactable = true;
				}

				UIManager.ViewOpened?.Invoke(panelView);

				completed = true;
			}
			finally
			{
				_showingPresenters.Remove(presenter);

				if (completed == false)
				{
					RecoverFromFailedTransition();
				}
			}
		}

		private async UniTask HideAsyncInternal(IWindowPresenter presenter, bool playAnimation)
		{
			if (_panelViews.ContainsKey(presenter) == false && _showingPresenters.Contains(presenter))
			{
				if (await WaitWhileAsync(() => _showingPresenters.Contains(presenter)))
				{
					return;
				}
			}
			
			if (_hidingPresenters.Contains(presenter))
			{
				Debug.LogWarning($"[PanelContainer] Panel of `{presenter}` is already being hidden, waiting for it instead of hiding again.");
				await WaitWhileAsync(() => _hidingPresenters.Contains(presenter));
				return;
			}

			if (_panelViews.TryGetValue(presenter, out var viewRef) == false)
			{
				Debug.LogError($"Cannot transition because there is no panel loaded " + $"on the stack by the asset path `{presenter}`");
				return;
			}

			_hidingPresenters.Add(presenter);

			if (Settings.EnableInteractionInTransition == false)
			{
				Interactable = false;
			}

			var completed = false;
			try
			{
				var panelView = viewRef.View;
				panelView.Settings = Settings;

				UIManager.ViewWillClose?.Invoke(panelView);
				
				await panelView.BeforeEnterAsync(false);

				await panelView.EnterAsync(false, playAnimation);

				Remove(presenter);

				panelView.AfterEnter(false);

				await panelView.BeforeReleaseAsync();

				DestroyAndForget(panelView, presenter.ViewConfig.AssetPath, viewRef.PoolingPolicy).Forget();

				if (Settings.EnableInteractionInTransition == false)
				{
					Interactable = true;
				}

				UIManager.ViewClosed?.Invoke(panelView);

				completed = true;
			}
			finally
			{
				_hidingPresenters.Remove(presenter);

				if (completed == false)
				{
					RecoverFromFailedTransition();
				}
			}
		}
		
		private async UniTask<bool> WaitWhileAsync(Func<bool> predicate)
		{
			return await UniTask.WaitWhile(predicate, cancellationToken: this.GetCancellationTokenOnDestroy()).SuppressCancellationThrow();
		}
		
		private void RecoverFromFailedTransition()
		{
			if (Settings.EnableInteractionInTransition == false)
			{
				Interactable = true;
			}

			Debug.LogWarning($"[PanelContainer] Transition in `{ContainerName}` failed, transition state has been restored.");
		}

		private async UniTask HideAllAsyncInternal(bool playAnimation = true)
		{
			var keys = new List<IWindowPresenter>(_panelViews.Count);
			var tasks = new List<UniTask>(_panelViews.Count);

			keys.AddRange(_panelViews.Keys);

			var count = keys.Count;
			for (var i = 0; i < count; i++)
			{
				var task = HideAsyncInternal(keys[i], playAnimation);
				tasks.Add(task);
			}

			await UniTask.WhenAll(tasks);
		}

		private bool Remove(IWindowPresenter presenter)
		{
			if (presenter == null)
			{
				throw new ArgumentNullException(nameof(presenter));
			}

			if (_panelViews.TryGetValue(presenter, out var panelView))
			{
				if (panelView.TryGetTransform(out var trans))
				{
					transform.RemoveChild(trans);
				}

				return _panelViews.Remove(presenter);
			}

			return false;
		}
	}
}
