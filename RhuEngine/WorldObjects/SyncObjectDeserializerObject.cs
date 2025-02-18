﻿using System;
using System.Collections.Generic;
using System.Reflection;

using RhuEngine.DataStructure;
using RhuEngine.Datatypes;
using RhuEngine.Linker;
using RhuEngine.WorldObjects.ECS;


namespace RhuEngine.WorldObjects
{
	public sealed class SyncObjectDeserializerObject
	{
		public List<Action> onLoaded = new();
		public bool hasNewRefIDs = false;
		public Dictionary<ulong, ulong> newRefIDs = new();
		public Dictionary<ulong, List<Action<NetPointer>>> toReassignLater = new();

		public SyncObjectDeserializerObject(bool hasNewRefIDs) {
			this.hasNewRefIDs = hasNewRefIDs;
		}

		public void BindPointer(DataNodeGroup data, IWorldObject @object) {
			if (hasNewRefIDs) {
				if (newRefIDs == null) {
					RLog.Warn($"Problem with {@object.GetType().FullName}");
					return;
				}
				newRefIDs.Add(((DataNode<NetPointer>)data.GetValue("Pointer")).Value.GetID(), @object.Pointer.GetID());
				if (toReassignLater.ContainsKey(((DataNode<NetPointer>)data.GetValue("Pointer")).Value.GetID())) {
					foreach (var func in toReassignLater[((DataNode<NetPointer>)data.GetValue("Pointer")).Value.GetID()]) {
						func.Invoke(@object.Pointer);
					}
				}
			}
			else {
				@object.Pointer = ((DataNode<NetPointer>)data.GetValue("Pointer")).Value;
				if (@object.Pointer._id == new NetPointer(0)._id) {
					RLog.Warn($"RefID of {@object.GetType().FullName} is null");
				}
				else {
					@object.World.RegisterWorldObject(@object);
				}
			}
		}

		public void RefDeserialize(DataNodeGroup data, ISyncRef @object) {
			if (data == null) {
				RLog.Warn("Node did not exist when loading SyncRef");
				return;
			}
			@object.RawPointer = ((DataNode<NetPointer>)data.GetValue("targetPointer")).Value;
			if (hasNewRefIDs) {
				newRefIDs.Add(((DataNode<NetPointer>)data.GetValue("Pointer")).Value.GetID(), @object.Pointer.GetID());
				if (toReassignLater.ContainsKey(((DataNode<NetPointer>)data.GetValue("Pointer")).Value.GetID())) {
					foreach (var func in toReassignLater[((DataNode<NetPointer>)data.GetValue("Pointer")).Value.GetID()]) {
						func(@object.Pointer);
					}
				}
				if (newRefIDs.ContainsKey(@object.NetValue.GetID())) {
					@object.RawPointer = new NetPointer(newRefIDs[@object.NetValue.GetID()]);
				}
				else {
					if (!toReassignLater.ContainsKey(@object.NetValue.GetID())) {
						toReassignLater.Add(@object.NetValue.GetID(), new List<Action<NetPointer>>());
					}
					toReassignLater[@object.NetValue.GetID()].Add((value) => @object.RawPointer = value);
				}

			}
			else {
				@object.Pointer = ((DataNode<NetPointer>)data.GetValue("Pointer")).Value;
				@object.World.RegisterWorldObject(@object);
			}
			onLoaded.Add(@object.RunOnLoad);
		}

		public bool ValueDeserialize<T>(DataNodeGroup data, IWorldObject @object, out T value) {
			if (data == null) {
				RLog.Warn($"Node did not exist when loading Sync value {@object.GetType().FullName}");
				value = default;
				return false;
			}
			BindPointer(data, @object);
			if (typeof(ISyncObject).IsAssignableFrom(@object.GetType())) {
				onLoaded.Add(((ISyncObject)@object).RunOnLoad);
			}
			if (typeof(T) == typeof(Type)) {
				value = ((DataNode<string>)data.GetValue("Value")).Value is null
					? (T)(object)null
					: (T)(object)Type.GetType(((DataNode<string>)data.GetValue("Value")).Value, false, false);
				return true;
			}
			else if (typeof(T) == typeof(Uri)) {
				value = ((DataNode<string>)data.GetValue("Value")).Value is null
					? (T)(object)null
					: (T)(object)new Uri(((DataNode<string>)data.GetValue("Value")).Value);
				return true;
			}
			else {
				if (typeof(T).IsEnum) {
					var unType = typeof(T).GetEnumUnderlyingType();
					if (unType == typeof(int)) {
						value = (T)(object)((DataNode<int>)data.GetValue("Value")).Value;
						return true;
					}
					else if (unType == typeof(uint)) {
						value = (T)(object)((DataNode<uint>)data.GetValue("Value")).Value;
						return true;
					}
					else if (unType == typeof(bool)) {
						value = (T)(object)((DataNode<bool>)data.GetValue("Value")).Value;
						return true;
					}
					else if (unType == typeof(byte)) {
						value = (T)(object)((DataNode<byte>)data.GetValue("Value")).Value;
						return true;
					}
					else if (unType == typeof(sbyte)) {
						value = (T)(object)((DataNode<sbyte>)data.GetValue("Value")).Value;
						return true;
					}
					else if (unType == typeof(short)) {
						value = (T)(object)((DataNode<short>)data.GetValue("Value")).Value;
						return true;
					}
					else if (unType == typeof(ushort)) {
						value = (T)(object)((DataNode<ushort>)data.GetValue("Value")).Value;
						return true;
					}
					else if (unType == typeof(long)) {
						value = (T)(object)((DataNode<long>)data.GetValue("Value")).Value;
						return true;
					}
					else if (unType == typeof(ulong)) {
						value = (T)(object)((DataNode<ulong>)data.GetValue("Value")).Value;
						return true;
					}
					else {
						throw new NotSupportedException();
					}
				}
				else {
					value = ((DataNode<T>)data.GetValue("Value")).Value;
					return true;
				}
			}
		}

		public void ListDeserialize<T>(DataNodeGroup data, ISyncObjectList<T> @object) where T : ISyncObject, new() {
			if (data == null) {
				RLog.Warn("Node did not exist when loading SyncObjList");
				return;
			}
			BindPointer(data, @object);
			foreach (var val in (DataNodeList)data.GetValue("list")) {
				@object.Add(!hasNewRefIDs, true).Deserialize(val, this);
			}
			if (typeof(ISyncObject).IsAssignableFrom(@object.GetType())) {
				onLoaded.Add(@object.RunOnLoad);
			}
		}

		public void AbstractListDeserialize<T>(DataNodeGroup data, IAbstractObjList<T> @object) where T : ISyncObject {
			if (data == null) {
				RLog.Warn("Node did not exist when loading SyncAbstractObjList");
				return;
			}
			BindPointer(data, @object);
			var list = (DataNodeList)data.GetValue("list");
			for (var i = 0; i < list._nodeGroup.Count; i++) {
				if (list._nodeGroup[i] is not DataNodeGroup val) {
					throw new Exception("Node group not found");
				}
				var ty = Type.GetType(((DataNode<string>)val.GetValue("Type")).Value);
				if (ty == typeof(MissingComponent)) {
					ty = Type.GetType(((DataNode<string>)((DataNodeGroup)val.GetValue("Value")).GetValue("type")).Value, false);
					if (ty == null) {
						RLog.Warn("Component still not found " + ((DataNode<string>)val.GetValue("Type")).Value);
						@object.Add(typeof(MissingComponent), !hasNewRefIDs, true).Deserialize((DataNodeGroup)val.GetValue("Value"), this);
					}
					else if (ty != typeof(MissingComponent)) {
						if (ty.IsAssignableFrom(typeof(T))) {
							@object.Add(ty, !hasNewRefIDs, true).Deserialize((DataNodeGroup)((DataNodeGroup)val.GetValue("Value")).GetValue("Data"), this);
						}
						else {
							RLog.Err("Something is broken or someone is messing with things");
						}
					}
					else {
						@object.Add(ty, !hasNewRefIDs, true).Deserialize((DataNodeGroup)val.GetValue("Value"), this);
					}
				}
				else {
					if (ty == null) {
						RLog.Warn($"Type {((DataNode<string>)val.GetValue("Type")).Value} not found");
						if (typeof(T) == typeof(Component)) {
							@object.Add(typeof(MissingComponent), !hasNewRefIDs, true).Deserialize((DataNodeGroup)val.GetValue("Value"), this);
						}
					}
					else {
						@object.Add(ty, !hasNewRefIDs, true).Deserialize((DataNodeGroup)val.GetValue("Value"), this);
					}
				}
			}
			if (typeof(ISyncObject).IsAssignableFrom(@object.GetType())) {
				onLoaded.Add(((ISyncObject)@object).RunOnLoad);
			}
		}

		public void Deserialize(DataNodeGroup data, IWorldObject @object) {
			if (data == null) {
				RLog.Warn("Node did not exist when loading Node: " + @object.GetType().FullName);
				return;
			}
			BindPointer(data, @object);
			var fields = @object.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
			foreach (var field in fields) {
				if ((field.GetCustomAttribute<NoLoadAttribute>(true) is null) && typeof(IWorldObject).IsAssignableFrom(field.FieldType) && ((field.GetCustomAttributes(typeof(NoSaveAttribute), true).Length <= 0) || (!hasNewRefIDs && (field.GetCustomAttributes(typeof(NoSyncAttribute), true).Length <= 0)))) {
					if (((IWorldObject)field.GetValue(@object)) == null) {
						throw new Exception($"Sync not initialized on field {field.Name} of {@object.GetType().FullName}");
					}
					try {
						var filedData = (DataNodeGroup)data.GetValue(field.Name);
						if (filedData is null) {
							if (field.GetCustomAttributes(typeof(NoSaveAttribute), false).Length <= 0) {
								((ISyncObject)field.GetValue(@object)).Deserialize(filedData, this);
							}
						}
						else {
							((ISyncObject)field.GetValue(@object)).Deserialize(filedData, this);
						}
					}
					catch (Exception e) {
						throw new Exception($"Failed to deserialize field {field.Name}", e);
					}
				}
			}
			if (typeof(ISyncObject).IsAssignableFrom(@object.GetType())) {
				onLoaded.Add(((ISyncObject)@object).RunOnLoad);
			}
		}
	}
}