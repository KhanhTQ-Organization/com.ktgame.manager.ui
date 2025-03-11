using Cysharp.Threading.Tasks;

namespace com.ktgame.manager.ui
{
	public interface IScreenPresenter : IViewPresenter, IScreenLifecycleEvent
	{
		void Show(bool stack = true);

		UniTask ShowAsync(bool stack = true);

		void Hide(bool playAnimation = true);

		UniTask HideAsync(bool playAnimation = true);
	}

	public interface IScreenPresenter<in TDataSource> : IViewPresenter, IScreenLifecycleEvent where TDataSource : IViewDataSource
	{
		void Show(TDataSource dataSource, bool stack = true);

		UniTask ShowAsync(TDataSource dataSource, bool stack = true);

		void Hide(bool playAnimation = true);

		UniTask HideAsync(bool playAnimation = true);
	}
}
