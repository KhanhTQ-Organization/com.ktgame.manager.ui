using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace com.ktgame.manager.ui
{
	[Serializable]
	public class WindowContainerConfig
	{
		[SerializeField, FoldoutGroup("$_name")] private string _name;
		[SerializeField, FoldoutGroup("$_name")] private WindowContainerType _containerType;
		[SerializeField, FoldoutGroup("$_name")] private bool _overrideSorting;

		[SerializeField, FoldoutGroup("$_name"), ShowIf("_overrideSorting")]
		private SortingLayerId _sortingLayer;

		[SerializeField, FoldoutGroup("$_name"), ShowIf("_overrideSorting")]
		private int _orderInLayer = 0;

		public string Name => _name;

		public WindowContainerType ContainerType => _containerType;

		public bool OverrideSorting => _overrideSorting;

		public SortingLayerId SortingLayer => _sortingLayer;

		public int OrderInLayer => _orderInLayer;
	}
}
