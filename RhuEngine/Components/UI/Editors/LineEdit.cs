﻿using RhuEngine.WorldObjects;
using RhuEngine.WorldObjects.ECS;

using RNumerics;
using RhuEngine.Linker;
using System;

namespace RhuEngine.Components
{
	public enum RVirtualKeyboardType {
		Default,
		MultiLine,
		Number,
		Decimal,
		Phone,
		Email,
		Passowrd,
		URL,
	}

	[Category("UI/Editors")]
	[UpdateLevel(UpdateEnum.Normal)]
	public class LineEdit : UIElement
	{
		public override string EditString => Text.Value;

		public readonly SyncDelegate<Action<string>> TextChange;
		public readonly SyncDelegate TextSubmitted;

		public readonly Sync<string> Text;
		public readonly Sync<string> PlaceholderText;
		public readonly Sync<RHorizontalAlignment> Alignment;
		[Default(true)]public readonly Sync<bool> Editable;
		public readonly Sync<bool> Secret;
		[Default("•")]
		public readonly Sync<string> SecretCharacter;
		public readonly Sync<bool> ExpandToTextLength;
		[Default(false)]
		public readonly Sync<bool> ContextMenuEnabled;
		[Default(true)]
		public readonly Sync<bool> VirtualKeyboardEnabled;
		public readonly Sync<RVirtualKeyboardType> VirtualKeyboardType;
		public readonly Sync<bool> ClearButtonEnabled;
		[Default(true)]
		public readonly Sync<bool> ShortcutKeysEnabled;
		[Default(true)]
		public readonly Sync<bool> MiddleMousePasteEnabled;
		[Default(true)]
		public readonly Sync<bool> SelectingEnabled;
		[Default(true)]
		public readonly Sync<bool> DeselectingOnFocusLossEnabled;
		public readonly AssetRef<RTexture2D> RightIcon;
		public readonly Sync<bool> Flat;
		public readonly Sync<bool> DrawControlChars;
		public readonly Sync<bool> CaretBlink;
		public readonly Sync<int> CaretColumn;
		public readonly Sync<bool> CaretForceDisplay;
		[Default(true)]
		public readonly Sync<bool> CaretMidGrapheme;
		public readonly Sync<RTextDirection> TextDir;
		public readonly Sync<string> Language;
		[Default(true)]
		public readonly Sync<bool> FocusLossOnEnter;
		[Default(true)]
		public readonly Sync<bool> SelectAllOnFocus;
		protected override void Step() {
			base.Step();
			if(Engine.KeyboardInteraction == this) {
				if (Engine.inputManager.KeyboardSystem.IsKeyJustDown(Key.Enter) && !Engine.inputManager.KeyboardSystem.IsKeyDown(Key.Shift)) {
					KeyboardUnBind();
				}
			}
		}

		protected override void OnAttach() {
			base.OnAttach();
			FocusMode.Value = RFocusMode.All;
			CursorShape.Value = RCursorShape.Ibeam;
		}
	}
}
