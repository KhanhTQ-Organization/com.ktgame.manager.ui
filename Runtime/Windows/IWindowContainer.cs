using UnityEngine;

namespace com.ktgame.manager.ui
{
	public interface IWindowContainer : IViewContainer
	{
		string ContainerName { get; }

		int ViewCount { get; }
		
		WindowContainerType ContainerType { get; }

		IUIManager UIManager { get; }

		Canvas Canvas { get; }
	}
}
