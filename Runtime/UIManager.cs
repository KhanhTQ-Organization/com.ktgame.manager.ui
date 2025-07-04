using System;
using System.Collections.Generic;
using com.ktgame.core.di;
using com.ktgame.foundation.extensions.unity;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace com.ktgame.manager.ui
{
	public abstract class UIManager : MonoBehaviour, IUIManager
	{
		[SerializeField] private UISettings _uiSettings;
		[SerializeField] private WindowContainerSettings _containerSettings;
		[SerializeField] private List<ViewConfigObject> _viewConfigs;

		public int Priority => 0;

		public bool IsInitialized { get; private set; }

		[Inject] public IInjector Injector { get; private set; }

		public IReadOnlyList<IWindowContainer> Containers => _containers;
		public IReadOnlyList<IViewPresenter> Presenters => _presenters;
		public Action<IView> ViewWillOpen { get; set; }
		public Action<IView> ViewOpened { get; set; }
		public Action<IView> ViewWillClose { get; set; }
		public Action<IView> ViewClosed { get; set; }

		protected abstract UniTask OnInitialize();
		protected abstract void OnDispose();

		[SerializeReference] private readonly List<IWindowContainer> _containers = new();
		[SerializeReference] private readonly List<IViewPresenter> _presenters = new();
		private readonly Dictionary<Type, IViewPresenter> _presenterLookup = new();

		public async UniTask Initialize()
		{
			if (_uiSettings == null)
			{
				throw new ArgumentNullException(nameof(_uiSettings));
			}

			if (_containerSettings == null)
			{
				throw new ArgumentNullException(nameof(_containerSettings));
			}

			if (IsInitialized)
			{
				throw new InvalidOperationException($"{GetType().Name} is already initialized.");
			}

			UISettings.DefaultSettings = _uiSettings;

			var configs = _containerSettings.Containers;
			foreach (var config in configs)
			{
				switch (config.ContainerType)
				{
					case WindowContainerType.Modal:
						CreateContainer<ModalContainer>(this, config, _uiSettings);
						break;
					case WindowContainerType.Screen:
						CreateContainer<ScreenContainer>(this, config, _uiSettings);
						break;
					case WindowContainerType.Panel:
						CreateContainer<PanelContainer>(this, config, _uiSettings);
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			foreach (var config in _viewConfigs)
			{
				if (TryGetContainer<IWindowContainer>(config.ContainerName, out var viewContainer))
				{
					var viewConfig = new ViewConfig(config.AssetPath, config.PlayAnimation, config.LoadAsync, config.PoolingPolicy);
					var viewPresenter = Activator.CreateInstance(config.ViewPresenterType, this, viewContainer, viewConfig);
					Injector?.Resolve(viewPresenter);
				}
			}

			await OnInitialize();
			IsInitialized = true;
		}

		public void AddContainer(IWindowContainer container)
		{
			if (container == null)
			{
				throw new ArgumentNullException(nameof(container));
			}

			if (_containers.Contains(container))
			{
				return;
			}

			_containers.Add(container);

			if (container.TryGetTransform(out var layerTransform))
			{
				transform.AddChild(layerTransform);
			}
		}

		public bool RemoveContainer(IWindowContainer container)
		{
			if (container == null)
			{
				throw new ArgumentNullException(nameof(container));
			}

			return _containers.Remove(container);
		}

		public TContainer GetContainer<TContainer>() where TContainer : IWindowContainer
		{
			if (TryGetContainer<TContainer>(out var container))
			{
				return container;
			}

			Debug.LogError($"Cannot find layer of type {typeof(TContainer).Name}");
			return default;
		}

		public TContainer GetContainer<TContainer>(string containerName) where TContainer : IWindowContainer
		{
			if (TryGetContainer<TContainer>(containerName, out var container))
			{
				return container;
			}

			Debug.LogError($"Cannot find layer {containerName}");
			return default;
		}

		public bool TryGetContainer<TContainer>(out TContainer container) where TContainer : IWindowContainer
		{
			container = default;

			var count = _containers.Count;

			for (var i = 0; i < count; i++)
			{
				if (_containers[i] is TContainer containerT)
				{
					container = containerT;
					break;
				}
			}

			return container != null;
		}

		public bool TryGetContainer<TContainer>(string containerName, out TContainer container) where TContainer : IWindowContainer
		{
			container = default;

			var count = _containers.Count;

			for (var i = 0; i < count; i++)
			{
				if (_containers[i] is TContainer containerT && string.Equals(containerT.ContainerName, containerName))
				{
					container = containerT;
					break;
				}
			}

			return container != null;
		}

		public bool HasPresenter<TPresenter>() where TPresenter : IViewPresenter
		{
			return _presenterLookup.ContainsKey(typeof(TPresenter));
		}

		public void AddPresenter(IViewPresenter viewPresenter)
		{
			var presenterType = viewPresenter.GetType();
			if (_presenterLookup.ContainsKey(presenterType))
			{
				Debug.LogError("Duplicate adding view presenter " + presenterType);
				return;
			}

			_presenterLookup.Add(viewPresenter.GetType(), viewPresenter);
			_presenters.Add(viewPresenter);
		}

		public void RemovePresenter(IViewPresenter viewPresenter)
		{
			_presenterLookup.Remove(viewPresenter.GetType());
			_presenters.Remove(viewPresenter);
		}

		public TPresenter GetPresenter<TPresenter>() where TPresenter : IViewPresenter
		{
			if (TryGetPresenter<TPresenter>(out var viewPresenter))
			{
				return viewPresenter;
			}

			Debug.LogError($"View presenter type not found {typeof(TPresenter).Name}");
			return default;
		}

		public IViewPresenter GetPresenter(Type presenterType)
		{
			if (TryGetPresenter(presenterType, out var viewPresenter))
			{
				return viewPresenter;
			}

			Debug.LogError($"View presenter type not found {presenterType.Name}");
			return default;
		}

		public bool TryGetPresenter<TPresenter>(out TPresenter viewPresenter) where TPresenter : IViewPresenter
		{
			viewPresenter = default;
			if (_presenterLookup.TryGetValue(typeof(TPresenter), out var presenter))
			{
				viewPresenter = (TPresenter)presenter;
			}

			return viewPresenter != null;
		}

		public bool TryGetPresenter(Type presenterType, out IViewPresenter viewPresenter)
		{
			viewPresenter = default;
			if (_presenterLookup.TryGetValue(presenterType, out var presenter))
			{
				viewPresenter = presenter;
			}

			return viewPresenter != null;
		}

		public void Dispose()
		{
			foreach (var presenter in _presenters)
			{
				presenter.Dispose();
			}

			OnDispose();
		}

		private static void CreateContainer<TContainer>(IUIManager uiManager, WindowContainerConfig config, UISettings settings)
			where TContainer : WindowContainer
		{
			var root = CreateContainerObject(config.Name);
			var container = root.AddComponent<TContainer>();
			container.Initialize(uiManager, config, settings);
		}

		private static GameObject CreateContainerObject(string containerName)
		{
			var containerObject = new GameObject(containerName, typeof(Canvas), typeof(GraphicRaycaster), typeof(CanvasGroup));
			var rectTransform = containerObject.GetOrAddComponent<RectTransform>();
			rectTransform.anchorMin = Vector2.zero;
			rectTransform.anchorMax = Vector2.one;
			rectTransform.offsetMax = Vector2.zero;
			rectTransform.offsetMin = Vector2.zero;
			rectTransform.pivot = new Vector2(0.5f, 0.5f);
			rectTransform.localPosition = Vector3.zero;
			return containerObject;
		}
	}
}
