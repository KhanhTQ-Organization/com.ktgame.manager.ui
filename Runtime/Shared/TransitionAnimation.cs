using System;
using System.Text.RegularExpressions;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace com.ktgame.manager.ui
{
	[Serializable]
	public class TransitionAnimation
	{
		[SerializeField] private string _partnerIdentifierRegex;
		[SerializeField] private AnimationAssetType _assetType;

		[SerializeField, ShowIf("_assetType", AnimationAssetType.MonoBehaviour)]
		private TransitionAnimationBehaviour _animationBehaviour;

		[SerializeField, ShowIf("_assetType", AnimationAssetType.ScriptableObject)]
		private TransitionAnimationObject _animationObject;

		[SerializeField] private UnityEvent _onAnimationBegin;
		[SerializeField] private UnityEvent _onAnimationComplete;

		private Regex _partnerIdentifierRegexCache;

		public string PartnerIdentifierRegex
		{
			get => _partnerIdentifierRegex;
			set => _partnerIdentifierRegex = value;
		}

		public AnimationAssetType AssetType
		{
			get => _assetType;
			set => _assetType = value;
		}

		public TransitionAnimationBehaviour AnimationBehaviour
		{
			get => _animationBehaviour;
			set => _animationBehaviour = value;
		}

		public TransitionAnimationObject AnimationObject
		{
			get => _animationObject;
			set => _animationObject = value;
		}

		public UnityEvent OnAnimationBegin
		{
			get => _onAnimationBegin;
			set => _onAnimationBegin = value;
		}

		public UnityEvent OnAnimationComplete
		{
			get => _onAnimationComplete;
			set => _onAnimationComplete = value;
		}

		public bool IsValid(string partnerScreenIdentifier)
		{
			if (GetAnimation() == null)
			{
				return false;
			}

			if (string.IsNullOrEmpty(_partnerIdentifierRegex))
			{
				return true;
			}

			if (string.IsNullOrEmpty(partnerScreenIdentifier))
			{
				return false;
			}

			if (_partnerIdentifierRegexCache == null)
			{
				_partnerIdentifierRegexCache = new Regex(_partnerIdentifierRegex);
			}

			return _partnerIdentifierRegexCache.IsMatch(partnerScreenIdentifier);
		}

		public ITransitionAnimation GetAnimation()
		{
			switch (_assetType)
			{
				case AnimationAssetType.MonoBehaviour:
					return _animationBehaviour;
				case AnimationAssetType.ScriptableObject:
					return Object.Instantiate(_animationObject);
				default:
					return null;
			}
		}
	}
}
