using System;
using System.Collections.Generic;
using System.Linq;
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
            var anim = anims.FirstOrDefault(x => x.IsValid(partnerTransitionIdentifier));
            var result = anim?.GetAnimation();
            return result;
        }
		
        public IReadOnlyList<TransitionAnimation> GetTransitions(bool enter)
        {
            return enter ? _enterAnimations : _exitAnimations;
        }
    }
}
