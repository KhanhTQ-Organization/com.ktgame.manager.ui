using System;
using com.ktgame.foundation.priorityCollection;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace com.ktgame.manager.ui
{
	[DisallowMultipleComponent]
	public class ModalView : Window
	{
		[SerializeField] private ModalTransitionAnimationContainer _animationContainer = new();

		private Progress<float> _transitionProgressReporter;
		private readonly UniquePriorityList<IModalLifecycleEvent> _lifecycleEvents = new();

		private Progress<float> TransitionProgressReporter
		{
			get { return _transitionProgressReporter ??= new Progress<float>(SetTransitionProgress); }
		}

		public ModalTransitionAnimationContainer AnimationContainer => _animationContainer;

		public bool IsTransitioning { get; private set; }

		public ModalTransitionAnimationType? TransitionAnimationType { get; private set; }

		public float TransitionAnimationProgress { get; private set; }

		public event Action<float> TransitionAnimationProgressChanged;

		public void AddLifecycleEvent(IModalLifecycleEvent lifecycleEvent, int priority = 0)
		{
			_lifecycleEvents.Add(lifecycleEvent, priority);
		}

		public void RemoveLifecycleEvent(IModalLifecycleEvent lifecycleEvent)
		{
			_lifecycleEvents.Remove(lifecycleEvent);
		}

		internal async UniTask AfterLoadAsync(RectTransform parentTransform)
		{
			SetIdentifier();

			Parent = parentTransform;
			RectTransform.FillParent(Parent);

			Alpha = 0.0f;

			GetViews();

			var tasks = _lifecycleEvents.Select(x => x.Initialize());
			await WaitForAsync(tasks);
		}

		internal async UniTask BeforeEnterAsync(bool show)
		{
			IsTransitioning = true;
			TransitionAnimationType = show
				? ModalTransitionAnimationType.Enter
				: ModalTransitionAnimationType.Exit;

			gameObject.SetActive(true);
			RectTransform.FillParent(Parent);
			SetTransitionProgress(0.0f);
			Alpha = 0.0f;

			var tasks = show
				? _lifecycleEvents.Select(x => x.WillEnter())
				: _lifecycleEvents.Select(x => x.WillExit());

			await WaitForAsync(tasks);
		}

		internal async UniTask EnterAsync(bool show, bool playAnimation)
		{
			Alpha = 1.0f;

			if (playAnimation)
			{
				var transition = GetTransition(show);
				var anim = GetAnimation(show);
				anim.Setup(RectTransform);
				transition.OnAnimationBegin?.Invoke();
				await anim.PlayAsync(TransitionProgressReporter);
				transition.OnAnimationComplete?.Invoke();
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
					lifecycleEvent.DidEnter();
				}
			}
			else
			{
				foreach (var lifecycleEvent in _lifecycleEvents)
				{
					lifecycleEvent.DidExit();
				}
			}

			IsTransitioning = false;
			TransitionAnimationType = null;
		}

		internal async UniTask BeforeReleaseAsync()
		{
			var tasks = _lifecycleEvents.Select(x => x.Cleanup());
			await WaitForAsync(tasks);
		}

		private void SetTransitionProgress(float progress)
		{
			TransitionAnimationProgress = progress;
			TransitionAnimationProgressChanged?.Invoke(progress);
		}

		private TransitionAnimation GetTransition(bool enter)
		{
			return _animationContainer.GetTransition(enter);
		}

		private ITransitionAnimation GetAnimation(bool enter)
		{
			var anim = _animationContainer.GetAnimation(enter);
			if (anim == null)
			{
				return Settings.GetDefaultModalTransitionAnimation(enter);
			}

			return anim;
		}
	}
}
