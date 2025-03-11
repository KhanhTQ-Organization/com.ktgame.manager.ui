using Cysharp.Threading.Tasks;

namespace com.ktgame.manager.ui
{
	public interface IModalBackdropHandler
	{
		UniTask BeforeModalEnter(ModalView modal, int modalIndex, bool playAnimation);

		void AfterModalEnter(ModalView modal, int modalIndex, bool playAnimation);

		UniTask BeforeModalExit(ModalView modal, int modalIndex, bool playAnimation);

		void AfterModalExit(ModalView modal, int modalIndex, bool playAnimation);
	}
}
