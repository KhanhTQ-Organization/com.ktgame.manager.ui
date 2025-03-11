using System;
using UnityEngine;

namespace com.ktgame.manager.ui
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public class ContainerNameAttribute : PropertyAttribute { }
}
