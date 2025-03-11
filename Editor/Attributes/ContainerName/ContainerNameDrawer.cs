using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace com.ktgame.manager.ui.editor
{
	[DrawerPriority(DrawerPriorityLevel.SuperPriority)]
	public class ContainerNameDrawer : OdinAttributeDrawer<ContainerNameAttribute, string>
	{
		private List<string> _allKeys;
		private List<string> _options;

		protected override void Initialize()
		{
			base.Initialize();

			var type = AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(a => a.GetTypes())
				.FirstOrDefault(t => t.Name == "ViewContainerKey");

			if (type == null)
			{
				_allKeys = null;
				return;
			}

			_options = GetOptionValues(type);
			_allKeys = GetAllStaticStringValues(type);
		}

		protected override void DrawPropertyLayout(GUIContent label)
		{
			if (_allKeys == null)
			{
				ValueEntry.SmartValue = EditorGUILayout.TextField(label, ValueEntry.SmartValue);
				return;
			}

			if (_allKeys.Count == 0)
			{
				EditorGUILayout.LabelField("No View Container Keys Found!");
				return;
			}

			var selectedIndex = Math.Max(0, _allKeys.IndexOf(ValueEntry.SmartValue));
			var newIndex = EditorGUILayout.Popup(label.text, selectedIndex, _options.ToArray());

			if (newIndex != selectedIndex)
			{
				ValueEntry.SmartValue = _allKeys[newIndex];
			}
		}

		private static List<string> GetAllStaticStringValues(Type type)
		{
			var keys = new List<string>();

			keys.AddRange(type
				.GetFields(BindingFlags.Public | BindingFlags.Static)
				.Where(f => f.FieldType == typeof(string))
				.Select(f => (string)f.GetValue(null)));

			foreach (var nestedType in type.GetNestedTypes(BindingFlags.Public))
			{
				keys.AddRange(GetAllStaticStringValues(nestedType));
			}

			return keys;
		}

		private static List<string> GetOptionValues(Type type)
		{
			var keys = new List<string>();
			foreach (var nestedType in type.GetNestedTypes(BindingFlags.Public | BindingFlags.Static))
			{
				var categoryName = nestedType.Name;
				foreach (var field in nestedType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
				{
					if (field.FieldType == typeof(string))
					{
						var value = (string)field.GetValue(null);
						keys.Add($"{value} ({categoryName})");
					}
				}
			}

			return keys;
		}
	}
}
