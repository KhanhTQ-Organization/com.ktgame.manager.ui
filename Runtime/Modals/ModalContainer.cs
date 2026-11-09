using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace com.ktgame.manager.ui
{
	public class ModalContainer : WindowContainer
	{
		public bool IsInTransition { get; private set; }
		
		internal bool HasPendingTransition => _transitionQueue.HasPending;

		public override int ViewCount => _modalViews.Count;

		public IReadOnlyList<ViewRef<ModalView>> ModalViews => _modalViews;

		public IReadOnlyList<ViewRef<ModalBackdrop>> Backdrops => _backdrops;

		public ViewRef<ModalView> CurrentModalView => _modalViews.Count > 0 ? _modalViews[^1] : new ViewRef<ModalView>();

		private readonly List<ViewRef<ModalView>> _modalViews = new();
		private readonly List<ViewRef<ModalBackdrop>> _backdrops = new();
		private readonly TransitionQueue _transitionQueue = new();
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

		public void Show<TModalView>(ModalViewConfig config, bool playAnimation) where TModalView : ModalView
		{
			ShowAndForget<TModalView>(config, playAnimation).Forget();
		}

		private async UniTaskVoid ShowAndForget<TModalView>(ModalViewConfig config, bool playAnimation) where TModalView : ModalView
		{
			await ShowAsyncInternal<TModalView>(config, playAnimation);
		}

		public async UniTask ShowAsync<TModalView>(ModalViewConfig config, bool playAnimation) where TModalView : ModalView
		{
			await ShowAsyncInternal<TModalView>(config, playAnimation);
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

		private async UniTask ShowAsyncInternal<TModalView>(ModalViewConfig config, bool playAnimation) where TModalView : ModalView
		{
			var resourcePath = config.Config.AssetPath;
			if (resourcePath == null)
			{
				throw new ArgumentNullException(nameof(resourcePath));
			}

			var transitionKey = "show:" + resourcePath;
			if (await WaitForTransitionTurnAsync(transitionKey) == null)
			{
				return;
			}

			IsInTransition = true;
			_transitionQueue.MarkRunning(transitionKey);

			if (Settings.EnableInteractionInTransition == false)
			{
				Interactable = false;
			}

			ModalBackdrop backdrop = null;
			var modalAdded = false;
			var lockReleased = false;
			var completed = false;
			try
			{
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
					animTasks.Add(backdrop.EnterAsync(playAnimation));
				}

				animTasks.Add(modalView.EnterAsync(true, playAnimation));

				await UniTask.WhenAll(animTasks);

				_modalViews.Add(new ViewRef<ModalView>(modalView, resourcePath, config.Config.PoolingPolicy));
				modalAdded = true;

				IsInTransition = false;
				lockReleased = true;

				modalView.AfterEnter(true);

				if (Settings.EnableInteractionInTransition == false)
				{
					Interactable = true;
				}

				UIManager.ViewOpened?.Invoke(modalView);

				completed = true;
			}
			finally
			{
				if (completed == false)
				{
					if (modalAdded == false && backdrop)
					{
						RemoveBackdropOf(backdrop);
					}

					RecoverFromFailedTransition(lockReleased);
				}
			}
		}

		private async UniTask HideAsyncInternal(bool playAnimation)
		{
			if (_modalViews.Count == 0)
			{
				Debug.LogError("Cannot transition because there is no modal loaded on the stack.");
				return;
			}

			const string transitionKey = "hide:top";
			var waited = await WaitForTransitionTurnAsync(transitionKey);
			if (waited == null)
			{
				return;
			}
			
			if (waited.Value && _modalViews.Count == 0)
			{
				Debug.LogWarning("[ModalContainer] Hide skipped: the modal stack became empty while waiting for the previous transition.");
				return;
			}

			IsInTransition = true;
			_transitionQueue.MarkRunning(transitionKey);

			if (Settings.EnableInteractionInTransition == false)
			{
				Interactable = false;
			}

			ViewRef<ModalBackdrop>? backdrop = null;
			var backdropIndex = -1;
			var modalRemoved = false;
			var lockReleased = false;
			var completed = false;
			try
			{
				var lastModalIndex = _modalViews.Count - 1;
				var exitModalRef = _modalViews[lastModalIndex];
				var modalView = exitModalRef.View;
				modalView.Settings = Settings;

				if (_disableBackdrop == false)
				{
					var lastBackdropIndex = _backdrops.Count - 1;
					var lastBackdrop = _backdrops[lastBackdropIndex];
					_backdrops.RemoveAt(lastBackdropIndex);
					backdropIndex = lastBackdropIndex;

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
				modalRemoved = true;
				IsInTransition = false;
				lockReleased = true;

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

				completed = true;
			}
			finally
			{
				if (completed == false)
				{
					if (modalRemoved == false && backdrop.HasValue && backdropIndex >= 0 && backdropIndex <= _backdrops.Count)
					{
						_backdrops.Insert(backdropIndex, backdrop.Value);
					}

					RecoverFromFailedTransition(lockReleased);
				}
			}
		}

		private async UniTask HideAsyncInternal(ModalView view, bool playAnimation)
		{
			if (_modalViews.Count == 0)
			{
				Debug.LogError("Cannot transition because there is no modal loaded on the stack.");
				return;
			}

			var transitionKey = "hide:" + (view ? view.GetInstanceID() : 0);
			var waited = await WaitForTransitionTurnAsync(transitionKey);
			if (waited == null)
			{
				return;
			}
			
			var exitModalIndex = _modalViews.FindIndex(x => x.View == view);

			if (exitModalIndex == -1)
			{
				if (waited.Value)
				{
					Debug.LogWarning($"[ModalContainer] Hide skipped: the modal view `{view}` was already closed while waiting for the previous transition.");
				}
				else
				{
					Debug.LogError($"Cannot find the modal view `{view}` on the stack.");
				}

				return;
			}

			IsInTransition = true;
			_transitionQueue.MarkRunning(transitionKey);

			if (Settings.EnableInteractionInTransition == false)
			{
				Interactable = false;
			}

			ViewRef<ModalBackdrop>? backdrop = null;
			var backdropIndex = -1;
			var modalRemoved = false;
			var lockReleased = false;
			var completed = false;
			try
			{
				var exitModalRef = _modalViews[exitModalIndex];
				var modalView = exitModalRef.View;
				modalView.Settings = Settings;

				if (_disableBackdrop == false)
				{
					var lastBackdrop = _backdrops[exitModalIndex];
					_backdrops.RemoveAt(exitModalIndex);
					backdropIndex = exitModalIndex;

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
				modalRemoved = true;
				IsInTransition = false;
				lockReleased = true;

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

				completed = true;
			}
			finally
			{
				if (completed == false)
				{
					if (modalRemoved == false && backdrop.HasValue && backdropIndex >= 0 && backdropIndex <= _backdrops.Count)
					{
						_backdrops.Insert(backdropIndex, backdrop.Value);
					}

					RecoverFromFailedTransition(lockReleased);
				}
			}
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
		
		private async UniTask<bool?> WaitForTransitionTurnAsync(string transitionKey)
		{
			if (IsInTransition == false && _transitionQueue.HasPending == false)
			{
				return false;
			}

			if (_transitionQueue.IsDuplicate(transitionKey, IsInTransition))
			{
				Debug.LogWarning("Cannot transition because there is a modal already in transition.");
				return null;
			}

			var gotTurn = await _transitionQueue.WaitForTurnAsync(transitionKey, () => IsInTransition, this.GetCancellationTokenOnDestroy());
			return gotTurn ? true : (bool?)null;
		}
		
		private void RemoveBackdropOf(ModalBackdrop backdrop)
		{
			var index = _backdrops.FindIndex(x => x.View == backdrop);
			if (index < 0)
			{
				return;
			}

			var backdropRef = _backdrops[index];
			_backdrops.RemoveAt(index);
			DestroyAndForget(backdropRef);
		}
		
		private void RecoverFromFailedTransition(bool lockReleased)
		{
			if (lockReleased == false)
			{
				IsInTransition = false;
			}

			if (Settings.EnableInteractionInTransition == false)
			{
				Interactable = true;
			}

			Debug.LogWarning($"[ModalContainer] Transition in `{ContainerName}` failed, transition state has been restored.");
		}
	}
}
