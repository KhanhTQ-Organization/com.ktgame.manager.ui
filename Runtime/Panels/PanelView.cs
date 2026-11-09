using System;
using com.ktgame.foundation.priorityCollection;
using com.ktgame.foundation.extensions.unity;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace com.ktgame.manager.ui
{
	[DisallowMultipleComponent]
	public class PanelView : Window
	{
		[SerializeField] private PanelTransitionAnimationContainer _animationContainer = new();
		
		private IProgress<float> _transitionProgressReporter;
		private readonly UniquePriorityList<IPanelLifecycleEvent> _lifecycleEvents = new();

		private IProgress<float> TransitionProgressReporter
		{
			get { return _transitionProgressReporter ??= Cysharp.Threading.Tasks.Progress.Create<float>(ReportTransitionProgress); }
		}

		public PanelTransitionAnimationContainer AnimationContainer => _animationContainer;

		public bool IsTransitioning { get; private set; }

		public PanelTransitionAnimationType? TransitionAnimationType { get; private set; }

		public float TransitionAnimationProgress { get; private set; }

		public event Action<float> TransitionAnimationProgressChanged;

		public void AddLifecycleEvent(IPanelLifecycleEvent lifecycleEvent, int priority = 0)
		{
			_lifecycleEvents.Add(lifecycleEvent, priority);
		}

		public void RemoveLifecycleEvent(IPanelLifecycleEvent lifecycleEvent)
		{
			_lifecycleEvents.Remove(lifecycleEvent);
		}

		public void SetSortingLayer(SortingLayerId? layer, int? sortingOrder)
		{
			if ((layer.HasValue & sortingOrder.HasValue) == false)
			{
				return;
			}

			var canvas = this.GetOrAddComponent<Canvas>();
			this.GetOrAddComponent<GraphicRaycaster>();

			canvas.overrideSorting = true;
			canvas.sortingLayerID = layer.Value.ID;
			canvas.sortingOrder = sortingOrder.Value;
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
				? PanelTransitionAnimationType.Enter
				: PanelTransitionAnimationType.Exit;

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

		internal void AfterEnter(bool show)
		{
			if (show)
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
				return Settings.GetDefaultPanelTransitionAnimation(enter);
			}

			return anim;
		}
	}
}
