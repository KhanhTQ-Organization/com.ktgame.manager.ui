using System;
using UnityEngine;

namespace com.ktgame.manager.ui
{
	public class MonoBehaviourDestroyedEventDispatcher : MonoBehaviour
	{
		public void OnDestroy()
		{
			OnDispatch?.Invoke();
		}

		public event Action OnDispatch;
	}
}
