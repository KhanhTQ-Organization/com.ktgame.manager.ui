using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace com.ktgame.manager.ui
{
	public class GeneratePerModalModalBackdropHandler : IModalBackdropHandler
	{
		private readonly ModalBackdrop _prefab;

		public GeneratePerModalModalBackdropHandler(ModalBackdrop prefab)
		{
			_prefab = prefab;
		}

		public UniTask BeforeModalEnter(ModalView modal, int modalIndex, bool playAnimation)
		{
			throw new System.NotImplementedException();
		}

		public void AfterModalEnter(ModalView modal, int modalIndex, bool playAnimation)
		{
			throw new System.NotImplementedException();
		}

		public UniTask BeforeModalExit(ModalView modal, int modalIndex, bool playAnimation)
		{
			throw new System.NotImplementedException();
		}

		public void AfterModalExit(ModalView modal, int modalIndex, bool playAnimation)
		{
			throw new System.NotImplementedException();
		}
	}
}
