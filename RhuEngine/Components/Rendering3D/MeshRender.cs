﻿using RhuEngine.WorldObjects;
using RhuEngine.WorldObjects.ECS;

using RhuEngine.Linker;
using RNumerics;
using System.Linq;
namespace RhuEngine.Components
{
	public enum ShadowCast
	{
		Off,
		On,
		TwoSided,
		ShadowsOnly
	}

	[Category(new string[] { "Rendering3D" })]
	public class MeshRender : GeometryInstance3D, IWorldBoundingBox
	{
		public readonly AssetRef<RMesh> mesh;

		public readonly SyncObjList<AssetRef<RMaterial>> materials;

		public readonly Sync<Colorf> colorLinear;

		[Default(false)]
		public readonly Sync<bool> RecevieShadows;

		[Default(false)]
		public readonly Sync<bool> ReflectionProbs;

		[Default(false)]
		public readonly Sync<bool> LightProbs;

		public AxisAlignedBox3f Bounds => mesh.Asset?.BoundingBox??AxisAlignedBox3f.CenterZero;
		protected override void FirstCreation() {
			base.FirstCreation();
			colorLinear.Value = Colorf.White;
		}
	}
}
