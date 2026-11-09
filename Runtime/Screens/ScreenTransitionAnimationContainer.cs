using System;
using System.Collections.Generic;
using UnityEngine;

namespace com.ktgame.manager.ui
{
	[Serializable]
	public class ScreenTransitionAnimationContainer
	{
		[SerializeField] private List<TransitionAnimation> _pushEnterAnimations = new();
		[SerializeField] private List<TransitionAnimation> _pushExitAnimations = new();
		[SerializeField] private List<TransitionAnimation> _popEnterAnimations = new();
		[SerializeField] private List<TransitionAnimation> _popExitAnimations = new();

		public List<TransitionAnimation> PushEnterAnimations => _pushEnterAnimations;
		public List<TransitionAnimation> PushExitAnimations => _pushExitAnimations;
		public List<TransitionAnimation> PopEnterAnimations => _popEnterAnimations;
		public List<TransitionAnimation> PopExitAnimations => _popExitAnimations;
		
		public ITransitionAnimation GetAnimation(bool push, bool enter, string partnerTransitionIdentifier)
		{
			var anims = GetTransitions(push, enter);
			for (var i = 0; i < anims.Count; i++)
			{
				var anim = anims[i];
				if (anim != null && anim.IsValid(partnerTransitionIdentifier))
				{
					return anim.GetAnimation();
				}
			}

			return null;
		}

		public IReadOnlyList<TransitionAnimation> GetTransitions(bool push, bool enter)
		{
			if (push)
			{
				return enter ? _pushEnterAnimations : _pushExitAnimations;
			}

			return enter ? _popEnterAnimations : _popExitAnimations;
		}
	}
}
