using System;
using com.ktgame.utils.class_type_reference;
using Sirenix.OdinInspector;
using UnityEngine;

namespace com.ktgame.manager.ui
{
	[CreateAssetMenu(menuName = "Ktgame/View/ViewConfigObject", fileName = "ViewConfigObject")]
	public class ViewConfigObject : ScriptableObject, IViewConfig
	{
		[SerializeField, ClassImplements(typeof(IViewPresenter)), FoldoutGroup("$_viewPresenterType")]
		private ClassTypeReference _viewPresenterType;

		[SerializeField, FoldoutGroup("$_viewPresenterType")] private bool _loadAsync = false;
		[SerializeField, FoldoutGroup("$_viewPresenterType")] private bool _playAnimation = false;
		[SerializeField, FoldoutGroup("$_viewPresenterType"), AssetPath] private string _assetPath;
		[SerializeField, FoldoutGroup("$_viewPresenterType"), ContainerName] private string _containerName;
		[SerializeField, FoldoutGroup("$_viewPresenterType")] private PoolingPolicy _poolingPolicy = PoolingPolicy.UseSettings;

		public Type ViewPresenterType => _viewPresenterType.Type;

		public bool LoadAsync => _loadAsync;

		public bool PlayAnimation => _playAnimation;

		public string AssetPath => _assetPath;

		public string ContainerName => _containerName;

		public PoolingPolicy PoolingPolicy => _poolingPolicy;

		public void SetViewLoadedCallback(Action<IView> callback) { }
	}
}
