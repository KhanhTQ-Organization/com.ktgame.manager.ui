using com.ktgame.foundation.animation;
using UnityEngine;

namespace com.ktgame.manager.ui
{
    public interface ITransitionAnimation : IAnimation
    {
        void SetPartner(RectTransform partnerRectTransform);
        
        void Setup(RectTransform rectTransform);
    }
}
