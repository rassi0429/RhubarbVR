﻿using RhuEngine.WorldObjects;
using RhuEngine.WorldObjects.ECS;

using RhuEngine.Linker;
using RNumerics;
using DiscordRPC;

namespace RhuEngine.Components
{
	public abstract class LocomotionModule : Component
	{
		public readonly AssetRef<RTexture2D> icon;

		public readonly Sync<string> locmotionName;

		public abstract void ProcessMovement();

		public float MoveSpeed => Engine.inputManager.GetInputAction(InputTypes.MoveSpeed).RawValue();

		public float Jump => (float)(Engine.inputManager.GetInputAction(InputTypes.Jump).RawValue() * RTime.Elapsed);

		public float Forward => (float)(Engine.inputManager.GetInputAction(InputTypes.Forward).RawValue() * RTime.Elapsed);

		public float Back => (float)(Engine.inputManager.GetInputAction(InputTypes.Back).RawValue() * RTime.Elapsed);

		public float Right => (float)(Engine.inputManager.GetInputAction(InputTypes.Right).RawValue() * RTime.Elapsed);

		public float Left => (float)(Engine.inputManager.GetInputAction(InputTypes.Left).RawValue() * RTime.Elapsed);

		public float FlyDown => (float)(Engine.inputManager.GetInputAction(InputTypes.FlyDown).RawValue() * RTime.Elapsed);

		public float FlyUp => (float)(Engine.inputManager.GetInputAction(InputTypes.FlyUp).RawValue() * RTime.Elapsed);

		public float RotateLeft => (float)(Engine.inputManager.GetInputAction(InputTypes.RotateLeft).RawValue() * RTime.Elapsed);

		public float RotateRight => (float)(Engine.inputManager.GetInputAction(InputTypes.RotateRight).RawValue() * RTime.Elapsed);

		public Entity UserRootEnity => World.GetLocalUser()?.userRoot.Target?.Entity;
		public UserRoot UserRoot => World.GetLocalUser()?.userRoot.Target;

		public void ProcessGlobalRotToUserRootMovement(Matrix addingMatrix, Matrix globalmat) {
			if (UserRootEnity is null) {
				return;
			}
			var childM = UserRootEnity.GlobalToLocal(globalmat);
			var targetHeadM = addingMatrix * childM;
			var userRootAdd = childM.Inverse * targetHeadM;
			UserRootEnity.LocalTrans = userRootAdd * UserRootEnity.LocalTrans;
		}
		public void SetUserRootGlobal(Matrix pos) {
			if (UserRootEnity is null) {
				return;
			}
			UserRootEnity.GlobalTrans = pos;
		}
	}
}
