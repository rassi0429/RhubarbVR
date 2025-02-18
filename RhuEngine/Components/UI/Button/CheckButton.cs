﻿using RhuEngine.WorldObjects;
using RhuEngine.WorldObjects.ECS;

using RNumerics;
using RhuEngine.Linker;
using System;

namespace RhuEngine.Components
{
	[Category("UI/Button")]
	public class CheckButton : Button
	{
		protected override void OnAttach() {
			base.OnAttach();
			ToggleMode.Value = true;
		}
	}
}
