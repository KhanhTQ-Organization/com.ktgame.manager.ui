using System;
using System.Collections.Generic;
using UnityEngine;

namespace com.ktgame.manager.ui
{
    [Serializable]
    public class SheetTransitionAnimationContainer
    {
        [SerializeField] private List<TransitionAnimation> _enterAnimations = new();
        [SerializeField] private List<TransitionAnimation> _exitAnimations = new();

        public List<TransitionAnimation> EnterAnimations => _enterAnimations;

        public List<TransitionAnimation> ExitAnimations => _exitAnimations;

        public ITransitionAnimation GetAnimation(bool enter, string partnerTransitionIdentifier)
        {
            var anims = enter ? _enterAnimations : _exitAnimations;
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
		
        public IReadOnlyList<TransitionAnimation> GetTransitions(bool enter)
        {
            return enter ? _enterAnimations : _exitAnimations;
        }
    }
}
