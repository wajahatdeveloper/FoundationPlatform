using System;
using UnityEngine;

namespace FoundationPlatform.Attributes
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public class LayerAttribute : PropertyAttribute
	{
	}
}