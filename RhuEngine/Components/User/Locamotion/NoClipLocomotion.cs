﻿using RhuEngine.Linker;
using RhuEngine.WorldObjects;
using RhuEngine.WorldObjects.ECS;

using RNumerics;

namespace RhuEngine.Components
{
	[Category(new string[] { "User" })]
	public sealed class NoClipLocomotion : LocomotionModule
	{
		[Default(1f)]
		public readonly Sync<float> MovementSpeed;
		[Default(50f)]
		public readonly Sync<float> RotationSpeed;
		[Default(2f)]
		public readonly Sync<float> MaxSprintSpeed;
		[Default(80f)]
		public readonly Sync<float> MaxSprintRotationSpeed;
		[Default(true)]
		public readonly Sync<bool> AllowMultiplier;

		protected override void OnAttach() {
			base.OnAttach();
			locmotionName.Value = "No Clip";
		}

		private void ProcessController(bool isMain) {
			if (UserRoot.head.Target is null) {
				return;
			}
			if (Engine.inputManager.GetHand(isMain) == Handed.Right) {
				if (WorldManager.PrivateSpaceManager.Right.IsAnyLaserGrabbed) {
					return;
				}
			}
			else {
				if (WorldManager.PrivateSpaceManager.Left.IsAnyLaserGrabbed) {
					return;
				}
			}
			var speed = AllowMultiplier ? MathUtil.Lerp(MovementSpeed, MaxSprintSpeed, MoveSpeed) : MovementSpeed;
			var tempRight = InputManager.GetInputAction(InputTypes.Right).HandedValue(InputManager.GetHand(isMain)) * RTime.Elapsed;
			var tempLeft = InputManager.GetInputAction(InputTypes.Left).HandedValue(InputManager.GetHand(isMain)) * RTime.Elapsed;
			var tempFlyUp = InputManager.GetInputAction(InputTypes.FlyUp).HandedValue(InputManager.GetHand(isMain)) * RTime.Elapsed;
			var tempFlyDown = InputManager.GetInputAction(InputTypes.FlyDown).HandedValue(InputManager.GetHand(isMain)) * RTime.Elapsed;
			var tempForward = InputManager.GetInputAction(InputTypes.Forward).HandedValue(InputManager.GetHand(isMain)) * RTime.Elapsed;
			var tempBack = InputManager.GetInputAction(InputTypes.Back).HandedValue(InputManager.GetHand(isMain)) * RTime.Elapsed;
			var pos = new Vector3f(0, 0, -tempForward + tempBack) * speed;
			var Rotspeed = AllowMultiplier ? MathUtil.Lerp(RotationSpeed, MaxSprintRotationSpeed, MoveSpeed) : RotationSpeed;
			var tempRotateRight = InputManager.GetInputAction(InputTypes.RotateLeft).HandedValue(InputManager.GetHand(isMain)) * RTime.Elapsed;
			var tempRotateLeft = InputManager.GetInputAction(InputTypes.RotateRight).HandedValue(InputManager.GetHand(isMain)) * RTime.Elapsed;
			var AddToMatrix = Matrix.T(pos);
			var handPos = InputManager.XRInputSystem.GetHand(Engine.inputManager.GetHand(isMain))[Input.XRInput.TrackerPos.Aim];
			ProcessGlobalRotToUserRootMovement(AddToMatrix, Matrix.TR(handPos.Position, handPos.Rotation) * UserRootEnity.GlobalTrans);
			var posHead = new Vector3f(tempRight - tempLeft, tempFlyUp - tempFlyDown, 0) * speed;
			ProcessGlobalRotToUserRootMovement(Matrix.T(posHead), UserRoot.head.Target.GlobalTrans);
			var otherHandNotUsable = !InputManager.XRInputSystem.GetHand(Engine.inputManager.GetHand(!isMain))[Input.XRInput.TrackerPos.Default].HasPos;
			if (Engine.inputManager.GetHand(isMain) == Handed.Right) {
				otherHandNotUsable |= WorldManager.PrivateSpaceManager.Left.IsAnyLaserGrabbed;
			}
			else {
				otherHandNotUsable |= WorldManager.PrivateSpaceManager.Right.IsAnyLaserGrabbed;
			}
			if (isMain || otherHandNotUsable) {
				var headPos = Matrix.T(UserRoot.head.Target.position.Value) * UserRootEnity.GlobalTrans;
				var headLocal = headPos * UserRootEnity.GlobalTrans.Inverse;
				var newHEadPos = Matrix.R((Quaternionf)Quaterniond.CreateFromEuler((tempRotateRight - tempRotateLeft) * RotationSpeed, 0, 0)) * headPos;
				SetUserRootGlobal(headLocal.Inverse * newHEadPos);
			}
		}

		private void ProcessHeadBased() {
			var speed = AllowMultiplier ? MathUtil.Lerp(MovementSpeed, MaxSprintSpeed, MoveSpeed) : MovementSpeed;
			var pos = new Vector3f(Right - Left, FlyUp - FlyDown, Back - Forward) * speed;
			var Rotspeed = AllowMultiplier ? MathUtil.Lerp(RotationSpeed, MaxSprintRotationSpeed, MoveSpeed) : RotationSpeed;
			var rot = Quaternionf.CreateFromEuler((RotateRight - RotateLeft) * RotationSpeed, 0, 0);
			var AddToMatrix = Matrix.T(pos);
			ProcessGlobalRotToUserRootMovement(AddToMatrix, LocalUser.userRoot?.Target.head.Target?.GlobalTrans ?? UserRootEnity.GlobalTrans);
			if (!WorldManager.PrivateSpaceManager.Head.IsAnyLaserGrabbed) {
				UserRootEnity.rotation.Value *= rot;
			}
		}

		public override void ProcessMovement() {
			if (UserRootEnity is null) {
				return;
			}
			if (!Engine.IsInVR) {
				ProcessHeadBased();
			}
			else {
				if (Engine.MainSettings.InputSettings.MovmentSettings.HeadBasedMovement) {
					ProcessHeadBased();
				}
				else {
					ProcessController(false);
					ProcessController(true);
				}
			}

		}
	}
}
