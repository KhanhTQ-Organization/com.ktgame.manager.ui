using System;

namespace com.ktgame.manager.ui
{
	public interface IViewPresenter : IDisposable, IViewLifecycleEvent
	{
		bool IsDisposed { get; }

		bool IsInitialized { get; }

		string ViewName { get; }
		
		IWindowPresenter Parent { get; set; }
	}
}
