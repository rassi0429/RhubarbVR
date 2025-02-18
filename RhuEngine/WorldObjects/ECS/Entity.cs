﻿using System;
using System.Collections.Generic;
using System.Linq;
using RNumerics;
using RhuEngine.Linker;
using RhuEngine.Components;

namespace RhuEngine.WorldObjects.ECS
{
	public sealed class Entity : SyncObject, IOffsetableElement, IWorldBoundingBox, IChangeable
	{
		private uint CompDepth => (InternalParent?.Depth + 1) ?? 0;

		public uint Depth { get; private set; }
		[NoShow]
		public readonly SyncObjList<Entity> children;
		[OnChanged(nameof(ParentChanged))]
		public readonly SyncRef<Entity> parent;

		[Default("Entity")]
		[OnChanged(nameof(NameChange))]
		public readonly Sync<string> name;
		private void NameChange() {
			Changed?.Invoke(this);
		}
		public override string Name => name.Value;

		[OnChanged(nameof(TransValueChange))]
		public readonly Sync<Vector3f> position;
		[OnChanged(nameof(TransValueChange))]
		public readonly Sync<Vector3f> scale;
		[OnChanged(nameof(TransValueChange))]
		public readonly Sync<Quaternionf> rotation;
		[OnChanged(nameof(OnOrderOffsetChange))]
		public readonly Sync<int> orderOffset;
		public int Offset => orderOffset.Value;
		[Default(true)]
		[OnChanged(nameof(OnEnableChange))]
		public readonly Sync<bool> enabled;

		[Default(true)]
		public readonly Sync<bool> persistence;

		public override bool Persistence => persistence.Value;

		[OnChanged(nameof(OnComponentChange))]
		[NoShow]
		public readonly SyncAbstractObjList<IComponent> components;
		[Exposed]
		public AxisAlignedBox3f Bounds
		{
			get {
				var box = AxisAlignedBox3f.Zero;
				for (var i = 0; i < components.Count; i++) {
					var item = components[i];
					if (item is IWorldBoundingBox boundingBox) {
						var scale = boundingBox.Bounds;
						scale.Scale(GlobalTrans.Scale);
						box = BoundsUtil.Combined(box, scale);
					}
				}
				for (var i = 0; i < children.Count; i++) {
					var item = children[i];
					var element = item.Bounds;
					element.Translate(item.GlobalTrans.Translation);
					element.Rotate(item.GlobalTrans.Rotation);
					element.Scale(GlobalTrans.Scale);
					box = BoundsUtil.Combined(box, element);
				}
				return box;
			}
		}

		public void RotateToUpVector(Entity space) {
			var roatation = GlobalToLocal(space.GlobalTrans).Rotation;
			var temp = roatation.AxisY;
			var localDirection = Vector3f.Zero;
			var num = -1f;
			for (var i = 0; i < 6; i++) {
				var a = Vector3f.Zero.SetComponent((i <= 2) ? 1 : (-1), i % 3);
				var num2 = a.Dot(temp);
				if (num2 > num) {
					localDirection = a;
					num = num2;
				}
			}
			var look = Quaternionf.FromToRotation(rotation.Value * localDirection, rotation.Value * temp);
			rotation.Value = look * rotation.Value;
		}


		public void DestroyChildren() {
			children.Clear();
		}



		public event Action<GrabbableHolder, bool, float> OnGrip;
		internal void CallOnGrip(GrabbableHolder obj, bool Laser, float gripForce) {
			OnGrip?.Invoke(obj, Laser, gripForce);
			if (!Grabbable.HITGRABABLE) {
				InternalParent?.CallOnGrip(obj, Laser, gripForce);
			}
			Grabbable.HITGRABABLE = false;
		}
		public event Action<uint, Vector3f, Vector3f, float, float, Handed> OnLazerPyhsics;

		internal void CallOnLazer(uint v, Vector3f hitnormal, Vector3f hitpointworld, float pressForce, float gripForce, Handed handed) {
			CallOnGrip(World.GetGrabHolder(handed), true, gripForce);
			OnLazerPyhsics?.Invoke(v, hitnormal, hitpointworld, pressForce, gripForce, handed);
		}

		public event Action<uint, Vector3f, Vector3f, Handed> OnTouchPyhsics;

		internal void CallOnTouch(uint v, Vector3f hitnormal, Vector3f hitpointworld, Handed handedSide) {
			OnTouchPyhsics?.Invoke(v, hitnormal, hitpointworld, handedSide);
		}
		public void ParentDepthUpdate() {
			Depth = CompDepth;
			for (var i = 0; i < children.Count; i++) {
				children[i].ParentDepthUpdate();
			}
		}

		internal void SetGlobalMatrixPysics(Matrix matrix) {
			var parentMatrix = Matrix.S(Vector3f.One);
			if (InternalParent != null) {
				parentMatrix = InternalParent.GlobalTrans;
			}
			var newLocal = matrix * parentMatrix.Inverse;
			newLocal.Decompose(out var newtranslation, out var newrotation, out var newscale);
			position.SetValueNoOnChange(newtranslation);
			rotation.SetValueNoOnChange(newrotation);
			scale.SetValueNoOnChange(newscale);
			_cachedGlobalMatrix = matrix;
			_cachedLocalMatrix = newLocal;
			GlobalTransformChange?.Invoke(this, false);
			for (var i = 0; i < children.Count; i++) {
				children[i].GlobalTransMark(false);
			}
		}

		public override void Destroy() {
			if (IsRoot) {
				return;
			}
			base.Destroy();
		}

		[Exposed]
		public Entity GetChildByName(string v) {
			for (var i = 0; i < children.Count; i++) {
				var child = children[i];
				if (child.name.Value == v) {
					return child;
				}
			}
			return null;
		}

		private void OnComponentChange() {
			if (_hasUpdatingComponentSave != HasUpdatingComponent) {
				_hasUpdatingComponentSave = !_hasUpdatingComponentSave;
				UpdateEnableList();
			}
			ViewportUpdate();
			UpdateCanvasItem();
		}

		public event Action ViewportUpdateEvent;
		public event Action CanvasItemUpdateEvent;

		private void UpdateCanvasItem() {
			var oldCanvasItem = CanvasItem;
			CanvasItem = GetFirstComponent<CanvasItem>();
			CanvasItem ??= InternalParent?.CanvasItem;
			CanvasItemUpdateEvent?.Invoke();
			if (oldCanvasItem != CanvasItem) {
				for (var i = 0; i < children.Count; i++) {
					var item = children[i];
					item.UpdateCanvasItem();
				}
			}
		}

		private void ViewportUpdate() {
			var oldViewPort = Viewport;
			Viewport = GetFirstComponent<Viewport>();
			Viewport ??= InternalParent?.Viewport;
			ViewportUpdateEvent?.Invoke();
			if (oldViewPort != Viewport) {
				for (var i = 0; i < children.Count; i++) {
					var item = children[i];
					item.ViewportUpdate();
				}
			}
		}

		private void OnOrderOffsetChange() {
			OffsetChanged?.Invoke();
		}
		[Exposed]
		public IComponent AttachComponent(Type type) {
			if (!typeof(Component).IsAssignableFrom(type)) {
				throw new ArgumentException($"Type {type.GetFormattedName()} is not assignable to {typeof(Component).GetFormattedName()}");
			}
			var comp = components.Add(type);
			comp.RunAttach();
			return comp;
		}
		[Exposed]
		public T AttachComponent<T>(Type type) where T : class, IComponent {
			if (!typeof(T).IsAssignableFrom(type)) {
				throw new ArgumentException($"Type {type.GetFormattedName()} is not assignable to {typeof(T).GetFormattedName()}");
			}
			var comp = components.Add(type);
			comp.RunAttach();
			return (T)comp;
		}

		[Exposed]
		public T AttachComponent<T>() where T : class, IComponent, new() {
			var comp = components.Add<T>();
			comp.RunAttach();
			return comp;
		}
		public T AttachComponent<T>(Action<T> beforeAttach) where T : class, IComponent, new() {
			var comp = components.Add<T>();
			beforeAttach.Invoke(comp);
			comp.RunAttach();
			return comp;
		}

		[Exposed]
		public T GetFirstComponentOrAttach<T>(Type type) where T : class, IComponent {
			for (var i = 0; i < components.Count; i++) {
				var item = components[i];
				if (type.IsAssignableFrom(item.GetType())) {
					return (T)item;
				}
			}
			return AttachComponent<T>(type);
		}

		[Exposed]
		public T GetFirstComponentOrAttach<T>() where T : class, IComponent, new() {
			for (var i = 0; i < components.Count; i++) {
				var item = components[i];
				if (typeof(T).IsAssignableFrom(item.GetType())) {
					return (T)item;
				}
			}
			return AttachComponent<T>();
		}
		[Exposed]
		public T GetFirstComponent<T>() where T : class, IComponent {
			for (var i = 0; i < components.Count; i++) {
				var item = components[i];
				if (typeof(T).IsAssignableFrom(item.GetType())) {
					return (T)item;
				}
			}
			return null;
		}
		[Exposed]
		public IEnumerable<T> GetAllComponents<T>() where T : class, IComponent {
			for (var i = 0; i < components.Count; i++) {
				var item = components[i];
				if (typeof(T).IsAssignableFrom(item.GetType())) {
					yield return (T)item;
				}
			}
		}
		[Exposed]
		public Matrix GlobalToLocal(in Matrix point, bool Child = true) {
			var parentMatrix = Child ? GlobalTrans : parent.Target?.GlobalTrans ?? Matrix.Identity;
			var newLocal = point * parentMatrix.Inverse;
			return newLocal;
		}
		public void GlobalToLocal(in Matrix point, bool Child, out Vector3f translation, out Quaternionf rotation, out Vector3f scale) {
			GlobalToLocal(point, Child).Decompose(out translation, out rotation, out scale);
		}
		[Exposed]
		public Vector3f GlobalPointToLocal(in Vector3f point, bool Child = true) {
			GlobalToLocal(Matrix.T(point), Child, out var newTranslation, out _, out _);
			return newTranslation;
		}
		[Exposed]
		public Vector3f GlobalScaleToLocal(in Vector3f Scale, bool Child = true) {
			GlobalToLocal(Matrix.S(Scale), Child, out _, out _, out var newScale);
			return newScale;
		}
		[Exposed]
		public Quaternionf GlobalRotToLocal(in Quaternionf Rot, bool Child = true) {
			GlobalToLocal(Matrix.R(Rot), Child, out _, out var newRotation, out _);
			return newRotation;
		}
		[Exposed]
		public Matrix LocalToGlobal(in Matrix point, bool Child = true) {
			return point * (Child ? GlobalTrans : InternalParent?.GlobalTrans ?? Matrix.Identity);
		}
		public void LocalToGlobal(in Matrix point, bool Child, out Vector3f translation, out Quaternionf rotation, out Vector3f scale) {
			LocalToGlobal(point, Child).Decompose(out translation, out rotation, out scale);
		}
		[Exposed]
		public Quaternionf LocalRotToGlobal(in Quaternionf Rot, bool Child = true) {
			LocalToGlobal(Matrix.R(Rot), Child, out _, out var newRotation, out _);
			return newRotation;
		}
		[Exposed]
		public Vector3f LocalScaleToGlobal(in Vector3f scale, bool Child = true) {
			LocalToGlobal(Matrix.S(scale), Child, out _, out _, out var newScale);
			return newScale;
		}
		[Exposed]
		public Vector3f LocalPosToGlobal(in Vector3f pos, bool Child = true) {
			LocalToGlobal(Matrix.T(pos), Child, out var newPos, out _, out _);
			return newPos;
		}



		[NoShow]
		[NoSave]
		[NoSync]
		[NoLoad]
		[UnExsposed]
		public Entity InternalParent { get; private set; }

		private Matrix _cachedGlobalMatrix = Matrix.S(1);

		private Matrix _cachedLocalMatrix = Matrix.S(1);

		public bool CheckIfParented(Entity entity) {
			return entity == this || (InternalParent?.CheckIfParented(entity) ?? false);
		}

		public bool parentEnabled = true;

		public event Action EnabledChanged;

		private bool _hasUpdatingComponentSave;
		[Exposed]
		public bool HasUpdatingComponent
		{
			get {
				if (components.Count == 0) {
					return false;
				}
				for (var i = 0; i < components.Count; i++) {
					var item = components[i].GetType().GetCustomAttributes(true);
					for (var i2 = 0; i2 < item.Length; i2++) {
						if (typeof(UpdatingComponentAttribute).IsAssignableFrom(item[i2].GetType())) {
							return true;
						}
					}
				}
				return false;
			}
		}

		private void UpdateEnableList() {
			if (IsRemoved || IsDestroying) {
				return;
			}
			for (var i = 0; i < components.Count; i++) {
				((Component)components[i]).ListObjectUpdate(IsEnabled);
			}
			if (IsEnabled && _hasUpdatingComponentSave) {
				try {
					World.RegisterUpdatingEntity(this);
				}
				catch { }
			}
			else {
				try {
					World.UnregisterUpdatingEntity(this);
				}
				catch { }
			}
		}

		protected override void OnLoaded() {
			base.OnLoaded();
			UpdateEnableList();
			TransValueChange();
			ViewportUpdate();
			UpdateCanvasItem();
		}

		public void ParentEnabledChange(bool _parentEnabled) {
			if (_parentEnabled != parentEnabled) {
				parentEnabled = _parentEnabled;
				for (var i = 0; i < children.Count; i++) {
					children[i].ParentEnabledChange(_parentEnabled);
				}
			}
			EnabledChanged?.Invoke();
			UpdateEnableList();
		}
		private void OnEnableChange() {
			if (!enabled.Value && (World.RootEntity == this)) {
				enabled.Value = true;
			};
			for (var i = 0; i < children.Count; i++) {
				children[i].ParentEnabledChange(enabled.Value);
			}
			EnabledChanged?.Invoke();
			UpdateEnableList();
		}
		[Exposed]
		public bool IsEnabled => parentEnabled && (enabled?.Value ?? false) && !IsDestroying && !IsRemoved;

		private void GoBackToOld() {
			if (parent.Target != InternalParent) {
				parent.SetTargetNoNetworkOrChange(InternalParent);
			}
		}

		private bool IsParrent(Entity check) {
			return check.Depth <= Depth && ((check == this) || (InternalParent?.IsParrent(check) ?? false));
		}

		private void ParentChanged() {
			try {
				if (World.RootEntity == this) {
					GoBackToOld();
					return;
				}

				if (parent.Target == InternalParent) {
					return;
				}

				if (parent.Target == null) {
					parent.Target = World.RootEntity;
					OnParentChanged?.Invoke();
					return;
				}

				if (parent.Target.IsParrent(this)) {
					GoBackToOld();
					return;
				}
				if (World != parent.Target.World) {
					RLog.Warn("tried to set parent from another world");
					GoBackToOld();
					OnParentChanged?.Invoke();
					return;
				}
				if (InternalParent == null) {
					InternalParent = parent.Target;
					ParentDepthUpdate();
					TransValueChange();
					OnParentChanged?.Invoke();
					return;
				}
				parent.Target.children.AddInternal(this);
				InternalParent.children.RemoveInternal(this);
				InternalParent = parent.Target;
				ParentDepthUpdate();
				ParentEnabledChange(InternalParent.IsEnabled);
				TransValueChange();
				ViewportUpdate();
				OnParentChanged?.Invoke();
			}
			catch {

			}
		}

		public event Action OnParentChanged;

		[Exposed]
		public void SetParent(Entity entity, bool preserverGlobal = true, bool resetPos = false) {
			var mach = GlobalTrans;
			parent.Target = entity;
			if (preserverGlobal) {
				GlobalTrans = mach;
			}
			else if (resetPos) {
				GlobalTrans = entity.GlobalTrans;
			}
		}
		[Exposed]
		public bool IsRoot => World?.RootEntity == this;

		public event Action<Entity, bool> GlobalTransformChange;

		public event Action OffsetChanged;
		public event Action<IChangeable> Changed;

		[Exposed]
		public Matrix GlobalTrans
		{
			get {
				if (IsDestroying) {
					return _cachedGlobalMatrix;
				}
				if (_dirtyGlobal) {
					_cachedGlobalMatrix = LocalTrans * InternalParent?.GlobalTrans ?? Matrix.Identity;
					_dirtyGlobal = false;
				}
				return _cachedGlobalMatrix;
			}
			set {
				var parentMatrix = Matrix.S(Vector3f.One);
				if (InternalParent != null) {
					parentMatrix = InternalParent.GlobalTrans;
				}
				var newLocal = value * parentMatrix.Inverse;
				newLocal.Decompose(out var newtranslation, out var newrotation, out var newscale);
				position.SetValueNoOnChange(newtranslation);
				rotation.SetValueNoOnChange(newrotation);
				scale.SetValueNoOnChange(newscale);
				_cachedGlobalMatrix = value;
				_cachedLocalMatrix = newLocal;
				_dirtyGlobal = false;
				_dirtyLocal = false;
				GlobalTransformChange?.Invoke(this, true);
				for (var i = 0; i < children.Count; i++) {
					children[i].GlobalTransMark();
				}
			}
		}
		[Exposed]
		public Matrix LocalTrans
		{
			get {
				if (IsDestroying) {
					return _cachedLocalMatrix;
				}
				if (_dirtyLocal) {
					_cachedLocalMatrix = Matrix.TRS(position.Value, rotation.Value, scale.Value);
					_dirtyLocal = false;
				}
				return _cachedLocalMatrix;
			}
			set {
				value.Decompose(out var newtranslation, out var newrotation, out var newscale);
				position.SetValueNoOnChange(newtranslation);
				rotation.SetValueNoOnChange(newrotation);
				scale.SetValueNoOnChange(newscale);
				_cachedGlobalMatrix = value * InternalParent?.GlobalTrans ?? Matrix.Identity;
				_cachedLocalMatrix = value;
				_dirtyGlobal = false;
				_dirtyLocal = false;
				GlobalTransformChange?.Invoke(this, true);
				for (var i = 0; i < children.Count; i++) {
					children[i].GlobalTransMark();
				}
			}
		}

		private bool _dirtyGlobal;
		private bool _dirtyLocal;
		public event Action<Grabbable> OnGrabbed;
		public event Action OnDroped;
		public void CallOnGrabbed(Grabbable grabbable) {
			OnGrabbed?.Invoke(grabbable);
		}
		public void CallOnDroped() {
			OnDroped?.Invoke();
		}
		private void TransValueChange() {
			if (IsRoot) {
				return;
			}
			_dirtyLocal = true;
			GlobalTransMark();
		}

		internal void GlobalTransMark(bool nonePhysics = true) {
			if (IsRoot) {
				return;
			}
			if (!_dirtyGlobal) {
				for (var i = 0; i < children.Count; i++) {
					children[i].GlobalTransMark(nonePhysics);
				}
				GlobalTransformChange?.Invoke(this, nonePhysics);
				_dirtyGlobal = true;
			}
		}

		protected override void FirstCreation() {
			base.FirstCreation();
			rotation.Value = Quaternionf.Identity;
			scale.Value = Vector3f.One;
		}

		public void RenderStep() {
			for (var i = 0; i < components.Count; i++) {
				var comp = (Component)components[i];
				comp.RunRenderStep(comp.Enabled.Value && IsEnabled);
			}
		}

		public void Step() {
			for (var i = 0; i < components.Count; i++) {
				var comp = (Component)components[i];
				comp.RunStep(comp.Enabled.Value && IsEnabled);
			}
		}
		[Exposed]
		public Entity FindChildByName(string name = "Entity") {
			for (var i = 0; i < children.Count; i++) {
				if (children[i].IsDestroying || children[i].IsRemoved) {
					continue;
				}
				if (children[i].name.Value == name) {
					return children[i];
				}
			}
			return null;
		}

		[Exposed]
		public Entity FindChildOrAddChild(string name = "Entity") {
			return FindChildByName(name) ?? AddChild(name);
		}

		[Exposed]
		public Entity AddChild(string name = "Entity") {
			var entity = children.Add();
			entity.name.Value = name;
			return entity;
		}

		protected override void OnInitialize() {
			World.RegisterEntity(this);
			if (Parent?.Parent is Entity par) {
				parent.NetValue = par.Pointer;
				if (par.Depth >= 10000) {
					throw new Exception("Max Entity Depth Reached");
				}
			}
		}
		public override void Dispose() {
			World.UnregisterEntity(this);
			if (HasUpdatingComponent) {
				World.UnregisterUpdatingEntity(this);
			}
			base.Dispose();
		}


		[NoShow]
		[NoSave]
		[NoSync]
		[NoLoad]
		[UnExsposed]
		public Viewport Viewport { get; private set; }

		[NoShow]
		[NoSave]
		[NoSync]
		[NoLoad]
		[UnExsposed]
		public CanvasItem CanvasItem { get; private set; }
		public Entity() {
		}
	}
}
