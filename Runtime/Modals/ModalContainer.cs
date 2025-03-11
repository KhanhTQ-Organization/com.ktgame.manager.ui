using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace com.ktgame.manager.ui
{
	public class ModalContainer : WindowContainer
	{
		public bool IsInTransition { get; private set; }

		public override int ViewCount => _modalViews.Count;

		public IReadOnlyList<ViewRef<ModalView>> ModalViews => _modalViews;

		public IReadOnlyList<ViewRef<ModalBackdrop>> Backdrops => _backdrops;

		public ViewRef<ModalView> CurrentModalView => _modalViews.Count > 0 ? _modalViews[^1] : new ViewRef<ModalView>();

		private readonly List<ViewRef<ModalView>> _modalViews = new();
		private readonly List<ViewRef<ModalBackdrop>> _backdrops = new();
		private bool _disableBackdrop;

		protected override void OnInitialize()
		{
			_disableBackdrop = Settings.DisableModalBackdrop;
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			var modalCount = _modalViews.Count;
			for (var i = 0; i < modalCount; i++)
			{
				(var modal, var resourcePath) = _modalViews[i];
				DestroyAndForget(modal, resourcePath, PoolingPolicy.DisablePooling).Forget();
			}

			_modalViews.Clear();

			var backdropCount = _backdrops.Count;
			for (var i = 0; i < backdropCount; i++)
			{
				(var backdrop, var resourcePath) = _backdrops[i];
				DestroyAndForget(backdrop, resourcePath, PoolingPolicy.DisablePooling).Forget();
			}

			_backdrops.Clear();
		}

		public bool FindIndexOfRecentlyPushed(string assetPath, out int index)
		{
			if (assetPath == null)
			{
				throw new ArgumentNullException(nameof(assetPath));
			}

			for (var i = _modalViews.Count - 1; i >= 0; i--)
			{
				if (string.Equals(assetPath, _modalViews[i].AssetPath))
				{
					index = i;
					return true;
				}
			}

			index = -1;
			return false;
		}

		public void DestroyRecentlyPushed(string assetPath, bool ignoreFront = true)
		{
			if (assetPath == null)
			{
				throw new ArgumentNullException(nameof(assetPath));
			}

			var frontIndex = _modalViews.Count - 1;
			if (FindIndexOfRecentlyPushed(assetPath, out var index) == false)
			{
				return;
			}

			if (ignoreFront && frontIndex == index)
			{
				return;
			}

			var modal = _modalViews[index];
			_modalViews.RemoveAt(index);

			ViewRef<ModalBackdrop>? backdrop = null;
			if (_disableBackdrop == false)
			{
				backdrop = _backdrops[index];
				_backdrops.RemoveAt(index);
			}

			DestroyAndForget(modal);
			if (backdrop.HasValue)
			{
				DestroyAndForget(backdrop.Value);
			}
		}

		public void Show<TModalView>(ModalViewConfig config) where TModalView : ModalView
		{
			ShowAndForget<TModalView>(config).Forget();
		}

		private async UniTaskVoid ShowAndForget<TModalView>(ModalViewConfig config) where TModalView : ModalView
		{
			await ShowAsyncInternal<TModalView>(config);
		}

		public async UniTask ShowAsync<TModalView>(ModalViewConfig config) where TModalView : ModalView
		{
			await ShowAsyncInternal<TModalView>(config);
		}

		public void Hide(bool playAnimation)
		{
			HideAndForget(playAnimation).Forget();
		}

		private async UniTaskVoid HideAndForget(bool playAnimation)
		{
			await HideAsyncInternal(playAnimation);
		}

		public async UniTask HideAsync(bool playAnimation)
		{
			await HideAsyncInternal(playAnimation);
		}

		public void Hide(ModalView view, bool playAnimation)
		{
			HideAndForget(view, playAnimation).Forget();
		}

		public async UniTask HideAsync(ModalView view, bool playAnimation)
		{
			await HideAsyncInternal(view, playAnimation);
		}

		private async UniTaskVoid HideAndForget(ModalView view, bool playAnimation)
		{
			await HideAsyncInternal(view, playAnimation);
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

		private async UniTask ShowAsyncInternal<TModalView>(ModalViewConfig config) where TModalView : ModalView
		{
			var resourcePath = config.Config.AssetPath;
			if (resourcePath == null)
			{
				throw new ArgumentNullException(nameof(resourcePath));
			}

			if (IsInTransition)
			{
				Debug.LogWarning("Cannot transition because there is a modal already in transition.");
				return;
			}

			IsInTransition = true;

			if (Settings.EnableInteractionInTransition == false)
			{
				Interactable = false;
			}

			ModalBackdrop backdrop = null;

			if (_disableBackdrop == false)
			{
				var backdropAssetPath = GetBackdropResourcePath(config.ModalBackdropAssetPath);
				var backdropConfig = new ViewConfig(assetPath: backdropAssetPath, playAnimation: config.Config.PlayAnimation,
					loadAsync: config.Config.LoadAsync, poolingPolicy: PoolingPolicy.UseSettings);

				backdrop = await GetViewAsync<ModalBackdrop>(backdropConfig);
				backdrop.Setup(RectTransform, config.BackdropAlpha, config.CloseWhenClickOnBackdrop);
				_backdrops.Add(new ViewRef<ModalBackdrop>(backdrop, backdropAssetPath, backdropConfig.PoolingPolicy));
			}

			var modalView = await GetViewAsync<TModalView>(config.Config);
			config.Config.ViewLoadedCallback?.Invoke(modalView);

			await modalView.AfterLoadAsync(RectTransform);

			UIManager.ViewWillOpen?.Invoke(modalView);

			await modalView.BeforeEnterAsync(true);

			var animTasks = new List<UniTask>();

			if (backdrop)
			{
				animTasks.Add(backdrop.EnterAsync(config.Config.PlayAnimation));
			}

			animTasks.Add(modalView.EnterAsync(true, config.Config.PlayAnimation));

			await UniTask.WhenAll(animTasks);

			_modalViews.Add(new ViewRef<ModalView>(modalView, resourcePath, config.Config.PoolingPolicy));

			IsInTransition = false;

			modalView.AfterEnter(true);

			if (Settings.EnableInteractionInTransition == false)
			{
				Interactable = true;
			}

			UIManager.ViewOpened?.Invoke(modalView);
		}

		private async UniTask HideAsyncInternal(bool playAnimation)
		{
			if (_modalViews.Count == 0)
			{
				Debug.LogError("Cannot transition because there is no modal loaded on the stack.");
				return;
			}

			if (IsInTransition)
			{
				Debug.LogWarning("Cannot transition because there is a modal already in transition.");
				return;
			}

			IsInTransition = true;

			if (Settings.EnableInteractionInTransition == false)
			{
				Interactable = false;
			}

			var lastModalIndex = _modalViews.Count - 1;
			var exitModalRef = _modalViews[lastModalIndex];
			var modalView = exitModalRef.View;
			modalView.Settings = Settings;

			ViewRef<ModalBackdrop>? backdrop = null;

			if (_disableBackdrop == false)
			{
				var lastBackdropIndex = _backdrops.Count - 1;
				var lastBackdrop = _backdrops[lastBackdropIndex];
				_backdrops.RemoveAt(lastBackdropIndex);

				lastBackdrop.View.Settings = Settings;
				backdrop = lastBackdrop;
			}

			UIManager.ViewWillClose?.Invoke(modalView);

			await modalView.BeforeEnterAsync(false);

			var animTask = new List<UniTask> { modalView.EnterAsync(false, playAnimation) };

			if (backdrop.HasValue && backdrop.Value.View)
			{
				animTask.Add(backdrop.Value.View.ExitAsync(playAnimation));
			}

			await UniTask.WhenAll(animTask);

			_modalViews.RemoveAt(lastModalIndex);
			IsInTransition = false;

			modalView.AfterEnter(false);

			await modalView.BeforeReleaseAsync();

			DestroyAndForget(exitModalRef);

			if (backdrop.HasValue)
			{
				DestroyAndForget(backdrop.Value);
			}

			if (Settings.EnableInteractionInTransition == false)
			{
				Interactable = true;
			}

			UIManager.ViewClosed?.Invoke(modalView);
		}

		private async UniTask HideAsyncInternal(ModalView view, bool playAnimation)
		{
			if (_modalViews.Count == 0)
			{
				Debug.LogError("Cannot transition because there is no modal loaded on the stack.");
				return;
			}

			if (IsInTransition)
			{
				Debug.LogWarning("Cannot transition because there is a modal already in transition.");
				return;
			}

			IsInTransition = true;

			if (Settings.EnableInteractionInTransition == false)
			{
				Interactable = false;
			}

			var exitModalIndex = _modalViews.FindIndex(x => x.View == view);

			if (exitModalIndex == -1)
			{
				Debug.LogError($"Cannot find the modal view `{view}` on the stack.");
				return;
			}

			var exitModalRef = _modalViews[exitModalIndex];
			var modalView = exitModalRef.View;
			modalView.Settings = Settings;

			ViewRef<ModalBackdrop>? backdrop = null;

			if (_disableBackdrop == false)
			{
				var lastBackdrop = _backdrops[exitModalIndex];
				_backdrops.RemoveAt(exitModalIndex);

				lastBackdrop.View.Settings = Settings;
				backdrop = lastBackdrop;
			}

			UIManager.ViewWillClose?.Invoke(modalView);

			await modalView.BeforeEnterAsync(false);

			var animTask = new List<UniTask> { modalView.EnterAsync(false, playAnimation) };

			if (backdrop.HasValue && backdrop.Value.View)
			{
				animTask.Add(backdrop.Value.View.ExitAsync(playAnimation));
			}

			await UniTask.WhenAll(animTask);

			_modalViews.RemoveAt(exitModalIndex);
			IsInTransition = false;

			modalView.AfterEnter(false);

			await modalView.BeforeReleaseAsync();

			DestroyAndForget(exitModalRef);

			if (backdrop.HasValue)
			{
				DestroyAndForget(backdrop.Value);
			}

			if (Settings.EnableInteractionInTransition == false)
			{
				Interactable = true;
			}

			UIManager.ViewClosed?.Invoke(modalView);
		}

		private async UniTask HideAllAsyncInternal(bool playAnimation = true)
		{
			var tasks = new List<UniTask>(_modalViews.Count);
			for (var i = _modalViews.Count - 1; i >= 0; i--)
			{
				var modalViewRef = _modalViews[i];
				if (playAnimation)
				{
					await HideAsyncInternal(modalViewRef.View, true);
				}
				else
				{
					var task = HideAsyncInternal(modalViewRef.View, false);
					tasks.Add(task);
				}
			}

			if (tasks.Count > 0)
			{
				await UniTask.WhenAll(tasks);
			}
		}

		private string GetBackdropResourcePath(string assetPath)
		{
			return string.IsNullOrWhiteSpace(assetPath)
				? Settings.ModalBackdropResourcePath
				: assetPath;
		}
	}
}
