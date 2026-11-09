using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace com.ktgame.manager.ui
{
	public class ScreenContainer : WindowContainer
	{
		private readonly List<ViewRef<ScreenView>> _screens = new();

		private bool _isActiveScreenStacked;

		private readonly TransitionQueue _transitionQueue = new();

		public bool IsInTransition { get; private set; }

		public override int ViewCount => _screens.Count;

		public IReadOnlyList<ViewRef<ScreenView>> Screens => _screens;

		public ViewRef<ScreenView> Current => _screens[^1];

		protected override void OnDestroy()
		{
			base.OnDestroy();

			var count = _screens.Count;
			for (var i = 0; i < count; i++)
			{
				(var screen, var resourcePath) = _screens[i];
				DestroyAndForget(screen, resourcePath, PoolingPolicy.DisablePooling).Forget();
			}

			_screens.Clear();
		}

		public bool FindIndexOfRecentlyPushed(string assetPath, out int index)
		{
			if (assetPath == null)
			{
				throw new ArgumentNullException(nameof(assetPath));
			}

			for (var i = _screens.Count - 1; i >= 0; i--)
			{
				if (string.Equals(assetPath, _screens[i].AssetPath))
				{
					index = i;
					return true;
				}
			}

			index = -1;
			return false;
		}

		public void BringToFront(ScreenViewConfig config, bool ignoreFront)
		{
			BringToFrontAndForget(config, ignoreFront).Forget();
		}

		private async UniTaskVoid BringToFrontAndForget(ScreenViewConfig config, bool ignoreFront)
		{
			await BringToFrontAsyncInternal(config, ignoreFront);
		}

		public async UniTask BringToFrontAsync(ScreenViewConfig config, bool ignoreFront)
		{
			await BringToFrontAsyncInternal(config, ignoreFront);
		}

		public void Push<TScreenView>(ScreenViewConfig config) where TScreenView : ScreenView
		{
			PushAndForget<TScreenView>(config).Forget();
		}

		private async UniTaskVoid PushAndForget<TScreenView>(ScreenViewConfig config) where TScreenView : ScreenView
		{
			await PushAsyncInternal<TScreenView>(config);
		}

		public async UniTask PushAsync<TScreenView>(ScreenViewConfig config) where TScreenView : ScreenView
		{
			await PushAsyncInternal<TScreenView>(config);
		}

		public void Pop(bool playAnimation)
		{
			PopAndForget(playAnimation).Forget();
		}

		private async UniTaskVoid PopAndForget(bool playAnimation)
		{
			await PopAsyncInternal(playAnimation);
		}

		public async UniTask PopAsync(bool playAnimation)
		{
			await PopAsyncInternal(playAnimation);
		}

		private async UniTask BringToFrontAsyncInternal(ScreenViewConfig config, bool ignoreFront)
		{
			var assetPath = config.Config.AssetPath;
			if (assetPath == null)
			{
				throw new ArgumentNullException(nameof(assetPath));
			}

			var frontIndex = _screens.Count - 1;
			if (FindIndexOfRecentlyPushed(assetPath, out var index) == false)
			{
				return;
			}

			if (ignoreFront && frontIndex == index)
			{
				return;
			}

			var enterScreen = _screens[index].View;
			enterScreen.Settings = Settings;

			_screens.RemoveAt(index);

			RectTransform.RemoveChild(enterScreen.transform);

			config.Config.ViewLoadedCallback?.Invoke(enterScreen);

			await enterScreen.AfterLoadAsync(RectTransform);

			ViewRef<ScreenView>? exitScreenRef = _screens.Count == 0 ? null : _screens[^1];
			var exitScreen = exitScreenRef?.View;
			int? exitScreenId = null;

			if (exitScreen)
			{
				exitScreenId = exitScreen.GetInstanceID();
				exitScreen.Settings = Settings;
			}

			if (exitScreen)
			{
				await exitScreen.BeforeExitAsync(true);
			}

			await enterScreen.BeforeEnterAsync(true);

			var animTasks = new List<UniTask>();

			if (exitScreen)
			{
				animTasks.Add(exitScreen.ExitAsync(true, config.Config.PlayAnimation, enterScreen));
			}

			animTasks.Add(enterScreen.EnterAsync(true, config.Config.PlayAnimation, exitScreen));

			await UniTask.WhenAll(animTasks);

			if (_isActiveScreenStacked == false && exitScreenId.HasValue)
			{
				_screens.RemoveAt(_screens.Count - 1);
			}

			_screens.Add(new ViewRef<ScreenView>(enterScreen, assetPath, config.Config.PoolingPolicy));
			IsInTransition = false;

			if (exitScreen)
			{
				exitScreen.AfterExit(true);
			}

			enterScreen.AfterEnter(true);

			if (_isActiveScreenStacked == false && exitScreenRef.HasValue)
			{
				await exitScreen.BeforeReleaseAsync();
				DestroyAndForget(exitScreenRef.Value);
			}

			_isActiveScreenStacked = config.Stack;

			if (Settings.EnableInteractionInTransition == false)
			{
				Interactable = true;
			}
		}

		private async UniTask PushAsyncInternal<TScreenView>(ScreenViewConfig config) where TScreenView : ScreenView
		{
			var resourcePath = config.Config.AssetPath;
			if (resourcePath == null)
			{
				throw new ArgumentNullException(nameof(resourcePath));
			}

			var transitionKey = "push:" + resourcePath;
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

			var lockReleased = false;
			var completed = false;
			try
			{
				var enterScreen = await GetViewAsync<TScreenView>(config.Config);
				config.Config.ViewLoadedCallback?.Invoke(enterScreen);

				await enterScreen.AfterLoadAsync(RectTransform);

				ViewRef<ScreenView>? exitScreenRef = _screens.Count == 0 ? null : _screens[^1];
				var exitScreen = exitScreenRef?.View;
				var exitScreenId = exitScreen == null ? (int?)null : exitScreen.GetInstanceID();

				if (exitScreen)
				{
					exitScreen.Settings = Settings;
				}

				if (exitScreen)
				{
					await exitScreen.BeforeExitAsync(true);
				}

				UIManager.ViewWillOpen?.Invoke(enterScreen);
			
				await enterScreen.BeforeEnterAsync(true);

				var animTasks = new List<UniTask>();

				if (exitScreen)
				{
					animTasks.Add(exitScreen.ExitAsync(true, config.Config.PlayAnimation, enterScreen));
				}

				animTasks.Add(enterScreen.EnterAsync(true, config.Config.PlayAnimation, exitScreen));

				await UniTask.WhenAll(animTasks);

				if (_isActiveScreenStacked == false && exitScreenId.HasValue)
				{
					_screens.RemoveAt(_screens.Count - 1);
				}

				_screens.Add(new ViewRef<ScreenView>(enterScreen, resourcePath, config.Config.PoolingPolicy));
				IsInTransition = false;
				lockReleased = true;

				if (exitScreen)
				{
					exitScreen.AfterExit(true);
				}

				enterScreen.AfterEnter(true);

				if (_isActiveScreenStacked == false && exitScreenRef.HasValue)
				{
					await exitScreen.BeforeReleaseAsync();
					DestroyAndForget(exitScreenRef.Value);
				}

				_isActiveScreenStacked = config.Stack;

				if (Settings.EnableInteractionInTransition == false)
				{
					Interactable = true;
				}

				UIManager.ViewOpened?.Invoke(enterScreen);

				completed = true;
			}
			finally
			{
				if (completed == false)
				{
					RecoverFromFailedTransition(lockReleased);
				}
			}
		}

		private async UniTask PopAsyncInternal(bool playAnimation)
		{
			if (_screens.Count == 0)
			{
				Debug.LogError("Cannot transition because there is no screen loaded on the stack.");
				return;
			}

			const string transitionKey = "pop";
			var waited = await WaitForTransitionTurnAsync(transitionKey);
			if (waited == null)
			{
				return;
			}
			
			if (waited.Value && _screens.Count == 0)
			{
				Debug.LogWarning("[ScreenContainer] Pop skipped: the screen stack became empty while waiting for the previous transition.");
				return;
			}

			IsInTransition = true;
			_transitionQueue.MarkRunning(transitionKey);

			if (Settings.EnableInteractionInTransition == false)
			{
				Interactable = false;
			}

			var lockReleased = false;
			var completed = false;
			try
			{
				var lastScreen = _screens.Count - 1;
				var exitScreenRef = _screens[lastScreen];
				var exitScreen = exitScreenRef.View;
				exitScreen.Settings = Settings;

				var enterScreen = _screens.Count == 1 ? null : _screens[^2].View;

				if (enterScreen)
				{
					enterScreen.Settings = Settings;
				}

				UIManager.ViewWillClose?.Invoke(exitScreen);
			
				await exitScreen.BeforeExitAsync(false);

				if (enterScreen)
				{
					await enterScreen.BeforeEnterAsync(false);
				}

				var animTasks = new List<UniTask> { exitScreen.ExitAsync(false, playAnimation, enterScreen) };

				if (enterScreen)
				{
					animTasks.Add(enterScreen.EnterAsync(false, playAnimation, exitScreen));
				}

				await UniTask.WhenAll(animTasks);

				_screens.RemoveAt(lastScreen);
				IsInTransition = false;
				lockReleased = true;

				exitScreen.AfterExit(false);

				if (enterScreen)
				{
					enterScreen.AfterEnter(false);
				}

				await exitScreen.BeforeReleaseAsync();

				DestroyAndForget(exitScreenRef);

				_isActiveScreenStacked = true;

				if (Settings.EnableInteractionInTransition == false)
				{
					Interactable = true;
				}

				UIManager.ViewClosed?.Invoke(exitScreen);

				completed = true;
			}
			finally
			{
				if (completed == false)
				{
					RecoverFromFailedTransition(lockReleased);
				}
			}
		}
		
		private async UniTask<bool?> WaitForTransitionTurnAsync(string transitionKey)
		{
			if (IsInTransition == false && _transitionQueue.HasPending == false)
			{
				return false;
			}

			if (_transitionQueue.IsDuplicate(transitionKey, IsInTransition))
			{
				Debug.LogWarning("Cannot transition because there is a screen already in transition.");
				return null;
			}

			var gotTurn = await _transitionQueue.WaitForTurnAsync(transitionKey, () => IsInTransition, this.GetCancellationTokenOnDestroy());
			return gotTurn ? true : (bool?)null;
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

			Debug.LogWarning($"[ScreenContainer] Transition in `{ContainerName}` failed, transition state has been restored.");
		}
	}
}
