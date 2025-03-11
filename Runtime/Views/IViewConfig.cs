using System;

namespace com.ktgame.manager.ui
{
	public interface IViewConfig
	{
		bool LoadAsync { get; }
		bool PlayAnimation { get; }
		string AssetPath { get; }
		PoolingPolicy PoolingPolicy { get; }
		void SetViewLoadedCallback(Action<IView> callback);
	}
}
