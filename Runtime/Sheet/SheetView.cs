using System;
using System.Collections.Generic;
using com.ktgame.foundation.priorityCollection;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace com.ktgame.manager.ui
{
	[DisallowMultipleComponent]
	public class SheetView : Window
	{
		[SerializeField] private SheetTransitionAnimationContainer _animationContainer = new();
		[SerializeField] private int _renderingOrder;
		
		private Progress<float> _transitionProgressReporter;
		private readonly UniquePriorityList<ISheetLifecycleEvent> _lifecycleEvents = new();

		private Progress<float> TransitionProgressReporter
		{
			get { return _transitionProgressReporter ??= new Progress<float>(SetTransitionProgress); }
		}

		public SheetTransitionAnimationContainer AnimationContainer => _animationContainer;

		public bool IsTransitioning { get; private set; }

		public SheetTransitionAnimationType? TransitionAnimationType { get; private set; }

		public float TransitionAnimationProgress { get; private set; }

		public event Action<float> TransitionAnimationProgressChanged;

		public void AddLifecycleEvent(ISheetLifecycleEvent lifecycleEvent, int priority = 0)
		{
			_lifecycleEvents.Add(lifecycleEvent, priority);
		}

		public void RemoveLifecycleEvent(ISheetLifecycleEvent lifecycleEvent)
		{
			_lifecycleEvents.Remove(lifecycleEvent);
		}

		internal async UniTask AfterLoadAsync(RectTransform parentTransform)
		{
			SetIdentifier();

			Parent = parentTransform;
			RectTransform.FillParent(Parent);
			gameObject.SetActive(false);

			OnAfterLoad();

			GetViews();
			
			var tasks = _lifecycleEvents.Select(x => x.Initialize());
			await WaitForAsync(tasks);
		}

		private void OnAfterLoad()
		{
			RectTransform.FillParent(Parent);

			var siblingIndex = 0;
			for (var i = 0; i < Parent.childCount; i++)
			{
				var child = Parent.GetChild(i);
				var childControl = child.GetComponent<SheetView>();
				siblingIndex = i;

				if (_renderingOrder >= childControl._renderingOrder)
				{
					continue;
				}

				break;
			}

			RectTransform.SetSiblingIndex(siblingIndex);
		}

		internal async UniTask BeforeEnterAsync()
		{
			IsTransitioning = true;
			TransitionAnimationType = SheetTransitionAnimationType.Enter;
			gameObject.SetActive(true);

			OnBeforeEnter();

			var tasks = _lifecycleEvents.Select(x => x.WillEnter());
			await WaitForAsync(tasks);
		}

		private void OnBeforeEnter()
		{
			SetTransitionProgress(0.0f);
			Alpha = 0.0f;
			RectTransform.FillParent(Parent);
		}

		internal async UniTask EnterAsync(bool playAnimation, SheetView partnerControl)
		{
			OnEnter();

			if (playAnimation == false)
			{
				return;
			}

			var anim = GetAnimation(true, partnerControl);

			if (anim == null)
			{
				return;
			}

			if (partnerControl)
			{
				anim.SetPartner(partnerControl.RectTransform);
			}

			anim.Setup(RectTransform);

			await anim.PlayAsync(TransitionProgressReporter);
		}

		protected virtual void OnEnter()
		{
			Alpha = 1.0f;
		}

		internal void AfterEnter()
		{
			foreach (var lifecycleEvent in _lifecycleEvents)
			{
				lifecycleEvent.DidEnter();
			}

			IsTransitioning = false;
			TransitionAnimationType = null;
		}

		internal async UniTask BeforeExitAsync()
		{
			IsTransitioning = true;
			TransitionAnimationType = SheetTransitionAnimationType.Exit;
			gameObject.SetActive(true);

			OnBeforeExit();

			var tasks = _lifecycleEvents.Select(x => x.WillExit());
			await WaitForAsync(tasks);
		}

		protected virtual void OnBeforeExit()
		{
			SetTransitionProgress(0.0f);
			Alpha = 1.0f;
			RectTransform.FillParent(Parent);
		}

		internal async UniTask ExitAsync(bool playAnimation, SheetView partnerControl)
		{
			OnExit();

			if (playAnimation == false)
			{
				return;
			}

			var anim = GetAnimation(false, partnerControl);

			if (anim == null)
			{
				return;
			}

			if (partnerControl)
			{
				anim.SetPartner(partnerControl.RectTransform);
			}

			anim.Setup(RectTransform);

			await anim.PlayAsync(TransitionProgressReporter);
		}

		protected virtual void OnExit()
		{
			Alpha = 0.0f;
			SetTransitionProgress(1.0f);
		}

		internal void AfterExit()
		{
			foreach (var lifecycleEvent in _lifecycleEvents)
			{
				lifecycleEvent.DidExit();
			}

			gameObject.SetActive(false);
		}

		internal async UniTask BeforeReleaseAsync()
		{
			var tasks = _lifecycleEvents.Select(x => x.Cleanup());
			await WaitForAsync(tasks);
		}

		protected void SetTransitionProgress(float progress)
		{
			TransitionAnimationProgress = progress;
			TransitionAnimationProgressChanged?.Invoke(progress);
		}

		private IReadOnlyList<TransitionAnimation> GetTransitions(bool enter)
		{
			return _animationContainer.GetTransitions(enter);
		}

		protected virtual ITransitionAnimation GetAnimation(bool enter, SheetView partner)
		{
			var partnerIdentifier = partner == true ? partner.Identifier : string.Empty;
			return _animationContainer.GetAnimation(enter, partnerIdentifier);
		}
	}
}
