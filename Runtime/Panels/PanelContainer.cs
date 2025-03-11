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

		public void Show<TPanelView>(IWindowPresenter presenter, PanelViewConfig config) where TPanelView : PanelView
		{
			ShowAndForget<TPanelView>(presenter, config).Forget();
		}

		private async UniTask ShowAndForget<TPanelView>(IWindowPresenter presenter, PanelViewConfig config) where TPanelView : PanelView
		{
			await ShowAsyncInternal<TPanelView>(presenter, config);
		}

		public async UniTask ShowAsync<TPanelView>(IWindowPresenter presenter, PanelViewConfig config) where TPanelView : PanelView
		{
			await ShowAsyncInternal<TPanelView>(presenter, config);
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

		private async UniTask ShowAsyncInternal<TPanelView>(IWindowPresenter presenter, PanelViewConfig config) where TPanelView : PanelView
		{
			var assetPath = config.Config.AssetPath;
			if (assetPath == null)
			{
				throw new ArgumentNullException(nameof(assetPath));
			}
			
			if (_panelViews.TryGetValue(presenter, out var viewRef))
			{
				Debug.LogWarning($"Cannot transition because the {typeof(TPanelView).Name} at `{assetPath}` is already showing.", viewRef.View);
				return;
			}
			
			if (Settings.EnableInteractionInTransition == false)
			{
				Interactable = false;
			}

			var panelView = await GetViewAsync<TPanelView>(config.Config);
			config.Config.ViewLoadedCallback?.Invoke(panelView);

			await panelView.AfterLoadAsync(RectTransform);

			panelView.SetSortingLayer(config.SortingLayer, config.OrderInLayer);

			UIManager.ViewWillOpen?.Invoke(panelView);
			
			await panelView.BeforeEnterAsync(true);

			await panelView.EnterAsync(true, config.Config.PlayAnimation);

			_panelViews.Add(presenter, new ViewRef<PanelView>(panelView, presenter.ViewConfig.AssetPath, config.Config.PoolingPolicy));
			
			panelView.AfterEnter(true);

			if (Settings.EnableInteractionInTransition == false)
			{
				Interactable = true;
			}

			UIManager.ViewOpened?.Invoke(panelView);
		}

		private async UniTask HideAsyncInternal(IWindowPresenter presenter, bool playAnimation)
		{
			if (_panelViews.TryGetValue(presenter, out var viewRef) == false)
			{
				Debug.LogError($"Cannot transition because there is no panel loaded " + $"on the stack by the asset path `{presenter}`");
				return;
			}

			if (Settings.EnableInteractionInTransition == false)
			{
				Interactable = false;
			}

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
