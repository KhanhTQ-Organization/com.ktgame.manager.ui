using System;
using System.Collections.Generic;
using com.ktgame.assets.loader.core;
using com.ktgame.foundation.extensions.unity;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace com.ktgame.manager.ui
{
	[RequireComponent(typeof(RectMask2D), typeof(CanvasGroup))]
	public abstract class ViewContainer : View, IViewContainer
	{
		private IAssetLoader _assetLoader;
		private readonly Dictionary<string, AssetRequest<GameObject>> _assetPathToRequest = new();
		private readonly Dictionary<string, Queue<View>> _assetPathToPool = new();
		
		private readonly Dictionary<string, HashSet<View>> _assetPathToInstances = new();
		
		private readonly Dictionary<string, int> _assetPathToPendingLoads = new();

		private bool _isContainerDestroyed;

		public IAssetLoader AssetLoader
		{
			get => _assetLoader ?? Settings.AssetLoader;
			set => _assetLoader = value ?? throw new ArgumentNullException(nameof(value));
		}

		protected bool EnablePooling => Settings.EnablePooling;

		protected RectTransform PoolTransform { get; private set; }

		protected virtual void InitializePool()
		{
			var parentTransform = transform.parent.GetComponent<RectTransform>();

			var poolGo = new GameObject($"[Pool] {name}", typeof(CanvasGroup), typeof(LayoutElement));
			PoolTransform = poolGo.GetOrAddComponent<RectTransform>();
			PoolTransform.SetParent(parentTransform, false);
			PoolTransform.FillParent(parentTransform);

			var poolCanvasGroup = poolGo.GetComponent<CanvasGroup>();
			poolCanvasGroup.alpha = 0f;
			poolCanvasGroup.blocksRaycasts = false;
			poolCanvasGroup.interactable = false;

			var layoutElement = poolGo.GetComponent<LayoutElement>();
			layoutElement.ignoreLayout = true;
			poolGo.SetActive(false);
		}

		protected override void OnDestroy()
		{
			_isContainerDestroyed = true;

			foreach ((var assetPath, var pool) in _assetPathToPool)
			{
				while (pool.TryDequeue(out var view))
				{
					DestroyAndForget(view, assetPath, PoolingPolicy.DisablePooling).Forget();
				}
			}

			_assetPathToPool.Clear();
			
			ReleaseUnusedRequestsAfterFrame().Forget();
		}

		private async UniTaskVoid ReleaseUnusedRequestsAfterFrame()
		{
			await UniTask.NextFrame();

			var assetPaths = new List<string>(_assetPathToRequest.Keys);
			foreach (var assetPath in assetPaths)
			{
				TryReleaseRequest(assetPath);
			}
		}
		
		private AssetRequest<GameObject> AcquireRequest(string assetPath, bool loadAsync)
		{
			if (_assetPathToRequest.TryGetValue(assetPath, out var request))
			{
				return request;
			}

			request = loadAsync
				? AssetLoader.LoadAsync<GameObject>(assetPath)
				: AssetLoader.Load<GameObject>(assetPath);

			_assetPathToRequest[assetPath] = request;
			return request;
		}

		private void BeginLoad(string assetPath)
		{
			_assetPathToPendingLoads.TryGetValue(assetPath, out var count);
			_assetPathToPendingLoads[assetPath] = count + 1;
		}

		private void EndLoad(string assetPath)
		{
			if (_assetPathToPendingLoads.TryGetValue(assetPath, out var count) == false)
			{
				return;
			}

			if (count <= 1)
			{
				_assetPathToPendingLoads.Remove(assetPath);
			}
			else
			{
				_assetPathToPendingLoads[assetPath] = count - 1;
			}
			
			if (_isContainerDestroyed)
			{
				ReleaseWhenUnusedAfterFrame(assetPath).Forget();
			}
		}

		private async UniTaskVoid ReleaseWhenUnusedAfterFrame(string assetPath)
		{
			await UniTask.NextFrame();
			TryReleaseRequest(assetPath);
		}

		private void TrackInstance(string assetPath, View view)
		{
			if (_assetPathToInstances.TryGetValue(assetPath, out var instances) == false)
			{
				_assetPathToInstances[assetPath] = instances = new HashSet<View>();
			}

			instances.Add(view);
		}

		private void UntrackInstance(string assetPath, View view)
		{
			if (_assetPathToInstances.TryGetValue(assetPath, out var instances))
			{
				instances.Remove(view);
			}
		}
		
		private void DiscardFailedRequest(string assetPath, AssetRequest<GameObject> request)
		{
			if (_assetPathToRequest.TryGetValue(assetPath, out var current) == false || ReferenceEquals(current, request) == false)
			{
				return;
			}

			_assetPathToRequest.Remove(assetPath);
			SafeRelease(assetPath, request);
		}
		
		private void TryReleaseRequest(string assetPath)
		{
			if (_assetPathToRequest.TryGetValue(assetPath, out var request) == false)
			{
				return;
			}

			if (_assetPathToPendingLoads.TryGetValue(assetPath, out var pending) && pending > 0)
			{
				return;
			}

			if (_assetPathToInstances.TryGetValue(assetPath, out var instances))
			{
				instances.RemoveWhere(v => v == null);
				if (instances.Count > 0)
				{
					return;
				}

				_assetPathToInstances.Remove(assetPath);
			}

			if (ContainsInPool(assetPath))
			{
				return;
			}

			_assetPathToRequest.Remove(assetPath);
			SafeRelease(assetPath, request);
		}

		private void SafeRelease(string assetPath, AssetRequest<GameObject> request)
		{
			try
			{
				AssetLoader.Release(request);
			}
			catch (Exception e)
			{
				Debug.LogWarning($"[ViewContainer] Failed to release asset request for `{assetPath}`: {e.Message}");
			}
		}

		public int CountInPool(string assetPath)
		{
			return _assetPathToPool.TryGetValue(assetPath, out var pool) ? pool.Count : 0;
		}

		public bool ContainsInPool(string assetPath)
		{
			return _assetPathToPool.TryGetValue(assetPath, out var pool) && pool.Count > 0;
		}

		public void KeepInPool(string assetPath, int amount)
		{
			KeepInPoolAndForget(assetPath, amount).Forget();
		}

		private async UniTaskVoid KeepInPoolAndForget(string assetPath, int amount)
		{
			await KeepInPoolAsync(assetPath, amount);
		}

		public async UniTask KeepInPoolAsync(string assetPath, int amount)
		{
			if (_assetPathToPool.TryGetValue(assetPath, out var pool) == false)
			{
				return;
			}

			var amountToDestroy = pool.Count - Mathf.Clamp(amount, 0, pool.Count);
			if (amountToDestroy < 1)
			{
				return;
			}

			var doDestroying = false;
			for (var i = 0; i < amountToDestroy; i++)
			{
				if (pool.TryDequeue(out var view))
				{
					UntrackInstance(assetPath, view);
					if (view && view.gameObject)
					{
						Destroy(view.gameObject);
						doDestroying = true;
					}
				}
			}

			if (doDestroying)
			{
				await UniTask.NextFrame();
			}

			TryReleaseRequest(assetPath);
		}

		public void Preload(string assetPath, bool loadAsync = true, int amount = 1)
		{
			PreloadAndForget(assetPath, loadAsync, amount).Forget();
		}

		private async UniTaskVoid PreloadAndForget(string assetPath, bool loadAsync = true, int amount = 1)
		{
			await PreloadAsync(assetPath, loadAsync, amount);
		}

		public async UniTask PreloadAsync(string assetPath, bool loadAsync = true, int amount = 1)
		{
			if (amount < 1)
			{
				Debug.LogWarning($"The amount of preloaded view instances should be greater than 0.");
				return;
			}

			if (_assetPathToPool.TryGetValue(assetPath, out var pool) == false)
			{
				_assetPathToPool[assetPath] = pool = new Queue<View>();
			}

			if (pool.Count >= amount)
			{
				return;
			}
			
			var assetRequest = AcquireRequest(assetPath, loadAsync);

			BeginLoad(assetPath);
			try
			{
				while (assetRequest.IsDone == false)
				{
					await UniTask.NextFrame();
				}
			}
			finally
			{
				EndLoad(assetPath);
			}

			if (assetRequest.Status == AssetRequestStatus.Failed)
			{
				DiscardFailedRequest(assetPath, assetRequest);
				throw assetRequest.OperationException;
			}
			
			if (_isContainerDestroyed)
			{
				return;
			}

			var differentAmount = amount - pool.Count;

			for (var i = 0; i < differentAmount; i++)
			{
				InstantiateToPool(assetPath, assetRequest, pool);
			}
		}

		private void InstantiateToPool(string assetPath, AssetRequest<GameObject> assetRequest, Queue<View> pool)
		{
			var instance = Instantiate(assetRequest.Result);
			instance.name = instance.name.Replace("(Clone)", string.Empty);

			if (instance.TryGetComponent<View>(out var view) == false)
			{
				Debug.LogError($"Cannot find the {nameof(View)} component on the specified resource `{assetPath}`.", instance);
				Destroy(instance);
				return;
			}

			TrackInstance(assetPath, view);
			view.Settings = Settings;
			view.RectTransform.SetParent(PoolTransform);
			view.Parent = PoolTransform;
			view.Owner.SetActive(false);
			pool.Enqueue(view);
		}

		public TView GetView<TView>(IViewConfig config) where TView : View
		{
			var assetPath = config.AssetPath;
			var poolingPolicy = config.PoolingPolicy;

			if (GetFromPool<TView>(assetPath, poolingPolicy, out var existView))
			{
				existView.Settings = Settings;
				return existView;
			}

			if (config.LoadAsync)
			{
				Debug.LogError($"[ViewContainer] Cannot call synchronous GetView for `{assetPath}` when LoadAsync is true. Use GetViewAsync instead.");
				return null;
			}

			AssetRequest<GameObject> assetRequest;
			if (_assetPathToRequest.TryGetValue(assetPath, out var request))
			{
				assetRequest = request;
				if (!assetRequest.IsDone)
				{
					Debug.LogError($"[ViewContainer] An async load request for `{assetPath}` is already in progress. Cannot perform synchronous load.");
					return null;
				}
			}
			else
			{
				assetRequest = AcquireRequest(assetPath, false);
			}

			if (assetRequest.Status == AssetRequestStatus.Failed)
			{
				DiscardFailedRequest(assetPath, assetRequest);
				throw assetRequest.OperationException;
			}

			var instance = Instantiate(assetRequest.Result);
			instance.name = instance.name.Replace("(Clone)", string.Empty);

			if (instance.TryGetComponent<TView>(out var view) == false)
			{
				Debug.LogError($"Cannot find the {typeof(TView).Name} component on the specified resource `{assetPath}`.", instance);
				Destroy(instance);
				ReleaseWhenUnusedAfterFrame(assetPath).Forget();
				return null;
			}

			TrackInstance(assetPath, view);
			view.Settings = Settings;
			return view;
		}

		public async UniTask<T> GetViewAsync<T>(IViewConfig config) where T : View
		{
			var assetPath = config.AssetPath;
			var loadAsync = config.LoadAsync;
			var poolingPolicy = config.PoolingPolicy;

			if (GetFromPool<T>(assetPath, poolingPolicy, out var existView))
			{
				existView.Settings = Settings;
				return existView;
			}
			
			var assetRequest = AcquireRequest(assetPath, loadAsync);

			BeginLoad(assetPath);
			try
			{
				while (assetRequest.IsDone == false)
				{
					await UniTask.NextFrame();
				}
			}
			finally
			{
				EndLoad(assetPath);
			}

			if (assetRequest.Status == AssetRequestStatus.Failed)
			{
				DiscardFailedRequest(assetPath, assetRequest);
				throw assetRequest.OperationException;
			}
			
			if (_isContainerDestroyed)
			{
				throw new OperationCanceledException($"[ViewContainer] Container was destroyed while loading `{assetPath}`.");
			}

			var instance = Instantiate(assetRequest.Result);
			instance.name = instance.name.Replace("(Clone)", string.Empty);

			if (instance.TryGetComponent<T>(out var view) == false)
			{
				Debug.LogError($"Cannot find the {typeof(T).Name} component on the specified resource `{assetPath}`.", instance);
				Destroy(instance);
				ReleaseWhenUnusedAfterFrame(assetPath).Forget();
				return null;
			}

			TrackInstance(assetPath, view);
			view.Settings = Settings;
			return view;
		}

		protected void DestroyAndForget<T>(ViewRef<T> viewRef) where T : View
		{
			DestroyAndForget(viewRef.View, viewRef.AssetPath, viewRef.PoolingPolicy).Forget();
		}

		protected async UniTaskVoid DestroyAndForget<T>(T view, string assetPath, PoolingPolicy poolingPolicy) where T : View
		{
			if (ReturnToPool(view, assetPath, poolingPolicy))
			{
				return;
			}

			UntrackInstance(assetPath, view);

			if (view && view.Owner)
			{
				Destroy(view.Owner);
				await UniTask.NextFrame();
			}
			
			TryReleaseRequest(assetPath);
		}

		protected bool ReturnToPool<T>(ViewRef<T> viewRef) where T : View
		{
			return ReturnToPool(viewRef.View, viewRef.AssetPath, viewRef.PoolingPolicy);
		}

		private bool ReturnToPool<T>(T view, string assetPath, PoolingPolicy poolingPolicy) where T : View
		{
			if (view == false)
			{
				return false;
			}

			if (CanPool(poolingPolicy) == false)
			{
				return false;
			}

			if (_assetPathToPool.TryGetValue(assetPath, out var pool) == false)
			{
				_assetPathToPool[assetPath] = pool = new Queue<View>();
			}

			if (view.Owner == false)
			{
				return false;
			}

			view.RectTransform.SetParent(PoolTransform);
			view.Parent = PoolTransform;
			view.Owner.SetActive(false);
			pool.Enqueue(view);
			return true;
		}

		private bool GetFromPool<T>(string assetPath, PoolingPolicy poolingPolicy, out T view) where T : View
		{
			if (CanPool(poolingPolicy) && _assetPathToPool.TryGetValue(assetPath, out var pool) && TryDequeueAlive(pool, out var typelessView))
			{
				if (typelessView is T typedView)
				{
					view = typedView;
					view.Settings = Settings;
					view.Owner.SetActive(true);
					return true;
				}

				UntrackInstance(assetPath, typelessView);
				if (typelessView && typelessView.gameObject)
				{
					Destroy(typelessView.Owner);
				}
			}

			view = default;
			return false;
		}
		
		private static bool TryDequeueAlive(Queue<View> pool, out View view)
		{
			while (pool.TryDequeue(out view))
			{
				if (view != null && view.Owner != null)
				{
					return true;
				}
			}

			view = null;
			return false;
		}

		private bool CanPool(PoolingPolicy poolingPolicy)
		{
			if (poolingPolicy == PoolingPolicy.DisablePooling)
			{
				return false;
			}

			if (poolingPolicy == PoolingPolicy.EnablePooling)
			{
				return true;
			}

			return EnablePooling;
		}
	}
}
