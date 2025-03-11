using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace com.ktgame.manager.ui.editor
{
	[DrawerPriority(DrawerPriorityLevel.SuperPriority)]
	public class AssetPathDrawer : OdinAttributeDrawer<AssetPathAttribute, string>
	{
		private List<string> _allKeys;

		protected override void Initialize()
		{
			base.Initialize();

			var type = AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(a => a.GetTypes())
				.FirstOrDefault(t => t.Name == "AssetPathKey");

			if (type == null)
			{
				_allKeys = null;
				return;
			}

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
				EditorGUILayout.LabelField("No Asset Path Keys Found!");
				return;
			}

			var selectedIndex = Math.Max(0, _allKeys.IndexOf(ValueEntry.SmartValue));
			var newIndex = EditorGUILayout.Popup(label.text, selectedIndex, _allKeys.ToArray());

			if (newIndex != selectedIndex)
			{
				ValueEntry.SmartValue = _allKeys[newIndex];
			}
		}

		private static List<string> GetAllStaticStringValues(Type type)
		{
			var keys = new List<string>();

			keys.AddRange(type
				.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
				.Where(f => f.FieldType == typeof(string))
				.Select(f => (string)f.GetValue(null)));

			foreach (var nestedType in type.GetNestedTypes(System.Reflection.BindingFlags.Public))
			{
				keys.AddRange(GetAllStaticStringValues(nestedType));
			}

			return keys;
		}
	}
}
