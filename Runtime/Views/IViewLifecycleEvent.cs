using Cysharp.Threading.Tasks;

namespace com.ktgame.manager.ui
{
	public interface IViewLifecycleEvent
	{
		UniTask Initialize();

		UniTask WillEnter();

		void DidEnter();

		UniTask WillExit();

		void DidExit();

		UniTask Cleanup();
	}
}
