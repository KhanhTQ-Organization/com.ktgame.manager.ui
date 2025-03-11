using System;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using Sirenix.OdinInspector;
#endif

namespace com.ktgame.manager.ui
{
	[CreateAssetMenu(fileName = "WindowContainerSettings", menuName = "Unimob/View/Window Container Settings", order = 1)]
	public class WindowContainerSettings : ScriptableObject
	{
		[SerializeField] private WindowContainerConfig[] _containers;

		public WindowContainerConfig[] Containers => _containers;

#if UNITY_EDITOR
		[Button("Generate Container Key"), GUIColor("cyan")]
		private void GenerateContainerKeys()
		{
			const string folderName = "Scripts/Generated/";
			const string fileName = "ViewContainerKey";
			const string fileExtension = ".cs";
			var folderPath = Application.dataPath + "/" + folderName;
			if (!Directory.Exists(folderPath))
			{
				Directory.CreateDirectory(folderPath);
			}

			try
			{
				File.WriteAllText(folderPath + fileName + fileExtension, ConfigKeyClassContent(fileName));
				AssetDatabase.ImportAsset("Assets/" + folderName + fileName + fileExtension, ImportAssetOptions.ForceUpdate);
			}
			catch (Exception)
			{
				Debug.LogError("Generate tags failed");
			}
		}

		private string ConfigKeyClassContent(string className)
		{
			var output = "";
			output += "//This class is manually-generated do not modify (WindowContainerSettings.cs)\n";
			output += "public static class " + className + "\n";
			output += "{\n";

			_containers.GroupBy(x => x.ContainerType).ToList().ForEach(group =>
			{
				output += $"\tpublic static class {group.Key}\n";
				output += "\t{\n";
				foreach (var container in group)
				{
					if (container.Name.Equals(group.Key.ToString()))
					{
						
						output += $"\t\tpublic const string _{container.Name} = \"{container.Name}\";\n";	
					}
					else
					{
						output += $"\t\tpublic const string {container.Name} = \"{container.Name}\";\n";	
					}
				}

				output += "\t}\n\n";
			});

			output += "}";
			return output;
		}
#endif
	}
}
