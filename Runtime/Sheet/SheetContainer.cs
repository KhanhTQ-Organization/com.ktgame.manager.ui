using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace com.ktgame.manager.ui
{
	[RequireComponent(typeof(RectMask2D), typeof(CanvasGroup))]
	public class SheetContainer : ViewContainer
	{
		[SerializeField] private string _name;
		[SerializeField] private UISettings _settings;
		public string ContainerName => _name;
		
		[ShowInInspector, ReadOnly] private readonly Dictionary<int, ViewRef<SheetView>> _sheetViews = new();
		private int? _activeSheetId;

		public IReadOnlyDictionary<int, ViewRef<SheetView>> SheetViews => _sheetViews;

		public override UISettings Settings
		{
			get
			{
				if (_settings == false)
				{
					_settings = UISettings.DefaultSettings;
				}

				return _settings;
			}

			set
			{
				if (value == false)
				{
					throw new ArgumentNullException(nameof(value));
				}

				_settings = value;
			}
		}

		protected override void Awake()
		{
			var _ = CanvasGroup;
			InitializePool();
		}
		
		public int? ActiveSheetId => _activeSheetId;

		public SheetView ActiveSheet
		{
			get
			{
				if (ActiveSheetId.HasValue == false)
				{
					return null;
				}

				return _sheetViews[ActiveSheetId.Value].View;
			}
		}

		public bool IsInTransition { get; private set; }

		protected override void OnDestroy()
		{
			base.OnDestroy();

			var controls = _sheetViews;
			foreach (var controlRef in controls.Values)
			{
				var (control, resourcePath) = controlRef;
				DestroyAndForget(control, resourcePath, PoolingPolicy.DisablePooling).Forget();
			}

			controls.Clear();
		}

		public void Cleanup()
		{
			CleanupAndForget().Forget();
		}

		public async UniTask CleanupAsync()
		{
			await CleanupAsyncInternal();
		}

		private async UniTaskVoid CleanupAndForget()
		{
			await CleanupAsyncInternal();
		}

		public void Register(SheetView sheetView)
		{
			RegisterAndForget(sheetView).Forget();
		}

		public async UniTask<int> RegisterAsync(SheetView sheetView)
		{
			return await RegisterAsyncInternal(sheetView);
		}

		private async UniTaskVoid RegisterAndForget<TSheetView>(TSheetView sheetView) where TSheetView : SheetView
		{
			await RegisterAsyncInternal(sheetView);
		}

		private async UniTask<int> RegisterAsyncInternal<TSheetView>(TSheetView sheetView) where TSheetView : SheetView
		{
			var sheetId = sheetView.GetInstanceID();

			_sheetViews[sheetId] = new ViewRef<SheetView>(sheetView, string.Empty, PoolingPolicy.DisablePooling);

			await sheetView.AfterLoadAsync((RectTransform)transform);

			return sheetId;
		}

		public void Show(int sheetId, bool playAnimation)
		{
			ShowAndForget(sheetId, playAnimation).Forget();
		}

		public async UniTask ShowAsync(int sheetId, bool playAnimation)
		{
			await ShowAsyncInternal(sheetId, playAnimation);
		}

		private async UniTaskVoid ShowAndForget(int sheetId, bool playAnimation)
		{
			await ShowAsyncInternal(sheetId, playAnimation);
		}

		private async UniTask ShowAsyncInternal(int sheetId, bool playAnimation)
		{
			if (IsInTransition)
			{
				Debug.LogError("Cannot transition because there is a sheet already in transition.");
				return;
			}

			if (ActiveSheetId.HasValue && ActiveSheetId.Value.Equals(sheetId))
			{
				Debug.LogWarning($"Cannot transition because the sheet {sheetId} is already active.");
				return;
			}

			IsInTransition = true;

			if (Settings.EnableInteractionInTransition == false)
			{
				Interactable = false;
			}

			var enterSheet = _sheetViews[sheetId].View;
			enterSheet.Settings = Settings;

			ViewRef<SheetView>? exitSheetRef = ActiveSheetId.HasValue ? _sheetViews[ActiveSheetId.Value] : null;
			var exitSheet = exitSheetRef?.View;

			if (exitSheet)
			{
				exitSheet.Settings = Settings;
			}

			if (exitSheet)
			{
				await exitSheet.BeforeExitAsync();
			}

			await enterSheet.BeforeEnterAsync();

			if (exitSheet)
			{
				await exitSheet.ExitAsync(playAnimation, enterSheet);
			}

			await enterSheet.EnterAsync(playAnimation, exitSheet);

			_activeSheetId = sheetId;
			IsInTransition = false;

			if (exitSheet)
			{
				exitSheet.AfterExit();
			}

			enterSheet.AfterEnter();

			if (Settings.EnableInteractionInTransition == false)
			{
				Interactable = true;
			}
		}

		public void Hide(bool playAnimation)
		{
			HideAndForget(playAnimation).Forget();
		}

		public async UniTask HideAsync(bool playAnimation)
		{
			await HideAsyncInternal(playAnimation);
		}

		private async UniTaskVoid HideAndForget(bool playAnimation)
		{
			await HideAsyncInternal(playAnimation);
		}

		private async UniTask HideAsyncInternal(bool playAnimation)
		{
			if (IsInTransition)
			{
				Debug.LogError("Cannot transition because there is a sheet already in transition.");
				return;
			}

			if (ActiveSheetId.HasValue == false)
			{
				Debug.LogWarning("Cannot transition because there is no active sheet.");
				return;
			}

			IsInTransition = true;

			if (Settings.EnableInteractionInTransition == false)
			{
				Interactable = false;
			}

			var exitSheet = _sheetViews[ActiveSheetId.Value].View;
			exitSheet.Settings = Settings;

			await exitSheet.BeforeExitAsync();

			await exitSheet.ExitAsync(playAnimation, null);

			_activeSheetId = null;
			IsInTransition = false;

			exitSheet.AfterExit();
			
			if (Settings.EnableInteractionInTransition == false)
			{
				Interactable = true;
			}
		}

		private async UniTask CleanupAsyncInternal()
		{
			_activeSheetId = null;
			IsInTransition = false;

			var sheets = _sheetViews;
			foreach (var sheetRef in sheets.Values)
			{
				await sheetRef.View.BeforeReleaseAsync();
				DestroyAndForget(sheetRef);
			}

			sheets.Clear();
		}
	}
}
