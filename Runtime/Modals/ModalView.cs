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
		
		private IProgress<float> _transitionProgressReporter;
		private readonly UniquePriorityList<IModalLifecycleEvent> _lifecycleEvents = new();

		private IProgress<float> TransitionProgressReporter
		{
			get { return _transitionProgressReporter ??= Cysharp.Threading.Tasks.Progress.Create<float>(ReportTransitionProgress); }
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

			await RunLifecycleAsync(_lifecycleEvents, x => x.Initialize());
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

			if (show)
			{
				await RunLifecycleAsync(_lifecycleEvents, x => x.WillEnter());
			}
			else
			{
				await RunLifecycleAsync(_lifecycleEvents, x => x.WillExit());
			}
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
				await anim.PlayAsync(this.GetCancellationTokenOnDestroy(), TransitionProgressReporter);
				transition.OnAnimationComplete?.Invoke();

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
