﻿using RhuEngine.WorldObjects;
using RhuEngine.WorldObjects.ECS;

using RNumerics;
using RhuEngine.Linker;
using System;
using NAudio.SoundFont;

namespace RhuEngine.Components
{
	public enum RButtonActionMode
	{
		Press,
		Relases
	}
	[Flags]
	public enum RButtonMask
	{
		None = 0,
		Primary = 1,
		Secondary = 2,
		Tertiary = 4,
		All = Primary | Secondary | Tertiary
	}

	[Category("UI/Button")]
	public class ButtonBase : UIElement
	{
		public readonly Sync<bool> Disabled;
		public readonly Sync<bool> ToggleMode;
		public readonly Sync<bool> ButtonPressed;
		public readonly Sync<RButtonActionMode> ActionMode;
		public readonly Sync<RButtonMask> ButtonMask;
		public readonly Sync<bool> KeepPressedOutside;

		public Handed LastHanded { get; set; }

		public readonly SyncDelegate ButtonDown;
		public readonly SyncDelegate ButtonUp;
		public readonly SyncDelegate Pressed;
		public readonly SyncDelegate<Action<bool>> Toggled;

		public Vector2f MainPos => GetInputPos(LastHanded);

		public Vector2f GetInputPos(Handed side) {
			return GetPosFunc?.Invoke(side) ?? Vector2f.Zero;
		}

		public Func<Handed, Vector2f> GetPosFunc;

		public event Action PressedAction;

		public void SendPressedAction() {
			PressedAction?.Invoke();
		}

		protected override void OnAttach() {
			base.OnAttach();
			FocusMode.Value = RFocusMode.All;
			CursorShape.Value = RCursorShape.PointingHand;
			ButtonMask.Value = RButtonMask.Primary;
		}
	}
}
