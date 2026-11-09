using System;
using System.Collections.Generic;
using com.ktgame.foundation.priorityCollection;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace com.ktgame.manager.ui
{
	[DisallowMultipleComponent]
	public class ScreenView : Window
	{
		[SerializeField] private int _renderingOrder;
		[SerializeField] private ScreenTransitionAnimationContainer _animationContainer = new();

		public ScreenTransitionAnimationContainer AnimationContainer => _animationContainer;

		public bool IsTransitioning { get; private set; }

		public ScreenTransitionAnimationType? TransitionAnimationType { get; private set; }

		public float TransitionAnimationProgress { get; private set; }

		public event Action<float> TransitionAnimationProgressChanged;

		private readonly UniquePriorityList<IScreenLifecycleEvent> _lifecycleEvents = new();
		
		private IProgress<float> _transitionProgressReporter;

		private IProgress<float> TransitionProgressReporter
		{
			get { return _transitionProgressReporter ??= Cysharp.Threading.Tasks.Progress.Create<float>(ReportTransitionProgress); }
		}

		public void AddLifecycleEvent(IScreenLifecycleEvent lifecycleEvent, int priority = 0)
		{
			_lifecycleEvents.Add(lifecycleEvent, priority);
		}

		public void RemoveLifecycleEvent(IScreenLifecycleEvent lifecycleEvent)
		{
			_lifecycleEvents.Remove(lifecycleEvent);
		}

		internal async UniTask AfterLoadAsync(RectTransform parentTransform)
		{
			SetIdentifier();

			Parent = parentTransform;
			RectTransform.FillParent(Parent);

			var siblingIndex = 0;
			for (var i = 0; i < Parent.childCount; i++)
			{
				var child = Parent.GetChild(i);
				var childScreen = child.GetComponent<ScreenView>();

				siblingIndex = i;

				if (_renderingOrder >= childScreen._renderingOrder)
				{
					continue;
				}

				break;
			}

			RectTransform.SetSiblingIndex(siblingIndex);
			Alpha = 0.0f;

			GetViews();

			await RunLifecycleAsync(_lifecycleEvents, x => x.Initialize());
		}

		internal async UniTask BeforeEnterAsync(bool push)
		{
			IsTransitioning = true;
			TransitionAnimationType = push ? ScreenTransitionAnimationType.PushEnter : ScreenTransitionAnimationType.PopEnter;
			gameObject.SetActive(true);
			RectTransform.FillParent(Parent);
			SetTransitionProgress(0.0f);

			Alpha = 0.0f;

			if (push)
			{
				await RunLifecycleAsync(_lifecycleEvents, x => x.WillPushEnter());
			}
			else
			{
				await RunLifecycleAsync(_lifecycleEvents, x => x.WillPopEnter());
			}
		}

		internal async UniTask EnterAsync(bool push, bool playAnimation, ScreenView partnerScreen)
		{
			Alpha = 1.0f;

			if (playAnimation)
			{
				var transitions = GetTransitions(push, true);
				var anim = GetAnimation(push, true, partnerScreen);
				if (partnerScreen)
				{
					anim.SetPartner(partnerScreen.RectTransform);
				}

				anim.Setup(RectTransform);
				for (var i = 0; i < transitions.Count; i++)
				{
					transitions[i].OnAnimationBegin?.Invoke();
				}

				await anim.PlayAsync(this.GetCancellationTokenOnDestroy(), TransitionProgressReporter);

				for (var i = 0; i < transitions.Count; i++)
				{
					transitions[i].OnAnimationComplete?.Invoke();
				}

				if (anim is ScriptableObject so)
				{
					UnityEngine.Object.Destroy(so);
				}
			}

			RectTransform.FillParent(Parent);
			SetTransitionProgress(1.0f);
		}

		internal void AfterEnter(bool push)
		{
			if (push)
			{
				foreach (var lifecycleEvent in _lifecycleEvents)
				{
					lifecycleEvent.DidPushEnter();
				}
			}
			else
			{
				foreach (var lifecycleEvent in _lifecycleEvents)
				{
					lifecycleEvent.DidPopEnter();
				}
			}

			IsTransitioning = false;
			TransitionAnimationType = null;
		}

		internal async UniTask BeforeExitAsync(bool push)
		{
			IsTransitioning = true;
			TransitionAnimationType = push
				? ScreenTransitionAnimationType.PushExit
				: ScreenTransitionAnimationType.PopExit;

			gameObject.SetActive(true);
			RectTransform.FillParent(Parent);
			SetTransitionProgress(0.0f);

			Alpha = 1.0f;

			if (push)
			{
				await RunLifecycleAsync(_lifecycleEvents, x => x.WillPushExit());
			}
			else
			{
				await RunLifecycleAsync(_lifecycleEvents, x => x.WillPopExit());
			}
		}

		internal async UniTask ExitAsync(bool push, bool playAnimation, ScreenView partnerScreen)
		{
			if (playAnimation)
			{
				var transitions = GetTransitions(push, false);
				var anim = GetAnimation(push, false, partnerScreen);
				if (partnerScreen)
				{
					anim.SetPartner(partnerScreen.RectTransform);
				}

				anim.Setup(RectTransform);
				for (var i = 0; i < transitions.Count; i++)
				{
					transitions[i].OnAnimationBegin?.Invoke();
				}

				await anim.PlayAsync(this.GetCancellationTokenOnDestroy(), TransitionProgressReporter);

				for (var i = 0; i < transitions.Count; i++)
				{
					transitions[i].OnAnimationComplete?.Invoke();
				}

				if (anim is ScriptableObject so)
				{
					UnityEngine.Object.Destroy(so);
				}
			}

			Alpha = 0.0f;
			SetTransitionProgress(1.0f);
		}

		internal void AfterExit(bool push)
		{
			if (push)
			{
				foreach (var lifecycleEvent in _lifecycleEvents)
				{
					lifecycleEvent.DidPushExit();
				}
			}
			else
			{
				foreach (var lifecycleEvent in _lifecycleEvents)
				{
					lifecycleEvent.DidPopExit();
				}
			}

			gameObject.SetActive(false);
			IsTransitioning = false;
			TransitionAnimationType = null;
		}

		internal UniTask BeforeReleaseAsync()
		{
			return RunLifecycleAsync(_lifecycleEvents, x => x.Cleanup());
		}
		
		private void ReportTransitionProgress(float progress)
		{
			try
			{
				SetTransitionProgress(progress);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}

		private void SetTransitionProgress(float progress)
		{
			TransitionAnimationProgress = progress;
			TransitionAnimationProgressChanged?.Invoke(progress);
		}

		private IReadOnlyList<TransitionAnimation> GetTransitions(bool push, bool enter)
		{
			return _animationContainer.GetTransitions(push, enter);
		}

		private ITransitionAnimation GetAnimation(bool push, bool enter, ScreenView partner)
		{
			var partnerIdentifier = partner == true ? partner.Identifier : string.Empty;
			var anim = _animationContainer.GetAnimation(push, enter, partnerIdentifier);

			if (anim == null)
			{
				return Settings.GetDefaultScreenTransitionAnimation(push, enter);
			}

			return anim;
		}
	}
}
