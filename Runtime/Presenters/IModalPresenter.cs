using Cysharp.Threading.Tasks;

namespace com.ktgame.manager.ui
{
	public interface IModalPresenter : IViewPresenter, IModalLifecycleEvent
	{
		void Show(float? backdropAlpha = null, bool? closeWhenClickOnBackdrop = null, string backdropAssetPath = "");

		UniTask ShowAsync(float? backdropAlpha = null, bool? closeWhenClickOnBackdrop = null, string backdropAssetPath = "");

		void Hide(bool playAnimation = true);

		UniTask HideAsync(bool playAnimation = true);
	}

	public interface IModalPresenter<in TDataSource> : IViewPresenter, IModalLifecycleEvent where TDataSource : IViewDataSource
	{
		void Show(TDataSource dataSource, float? backdropAlpha = null, bool? closeWhenClickOnBackdrop = null, string backdropAssetPath = "");

		UniTask ShowAsync(TDataSource dataSource, float? backdropAlpha = null, bool? closeWhenClickOnBackdrop = null, string backdropAssetPath = "");

		void Hide(bool playAnimation = true);

		UniTask HideAsync(bool playAnimation = true);
	}
}
