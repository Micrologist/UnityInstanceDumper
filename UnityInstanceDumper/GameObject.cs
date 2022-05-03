using LiveSplit.ComponentUtil;
using System;
using System.Collections.Generic;

namespace UnityInstanceDumper
{
	public class GameObject
	{
		public IntPtr adr;
		public int childCount;
		public string name;
		public string path;
		public GameObject parent;
		public Scene scene;
		public List<GameObject> children;
		public List<ObjectComponent> components;

		public GameObject(IntPtr ptr, GameObject parent, Scene scene)
		{
			this.adr = ptr;
			this.parent = parent;
			this.scene = scene;
			this.name = GetName();
			this.path = (parent == null ? scene.name : parent.path) + "/" + name;
			this.childCount = GetChildren(out this.children);
			this.components = GetComponents();
			Game.ObjDict.Add(this.adr.ToString("X16"), this);
		}

		string GetName()
		{
			return new DeepPointer(this.adr + (Game.is64bit ? 0x60 : 0x3C), 0x0).DerefString(Game.proc, 250);
		}

		int GetChildren(out List<GameObject> childList)
		{
			childList = new List<GameObject>();
			new DeepPointer(this.adr + (Game.is64bit ? 0x30 : 0x1C), (Game.is64bit ? 0x8 : 0x4), 0x0).DerefOffsets(Game.proc, out IntPtr transform);
			Game.proc.ReadValue<int>(transform + (Game.is64bit ? 0x80 : 0x58), out int childCount);
			IntPtr childListPtr = Game.proc.ReadPointer(transform + (Game.is64bit ? 0x70 : 0x50));
			byte[] childPtrArray = Game.proc.ReadBytes(childListPtr, (Game.is64bit ? 0x8 : 0x4) * childCount);
			for (int i = 0; i < childCount; i++)
			{
				IntPtr childTransformPtr = Game.is64bit ? (IntPtr)BitConverter.ToInt64(childPtrArray, i * 0x8) : (IntPtr)BitConverter.ToInt32(childPtrArray, i * 0x4);
				new DeepPointer(childTransformPtr + (Game.is64bit ? 0x30 : 0x1C), 0x0).DerefOffsets(Game.proc, out IntPtr childPtr);
				childList.Add(new GameObject(childPtr, this, this.scene));
			}
			return childCount;
		}

		List<ObjectComponent> GetComponents()
		{
			List<ObjectComponent> output = new List<ObjectComponent>();
			int numComponents = Game.proc.ReadValue<int>(this.adr + (Game.is64bit ? 0x40 : 0x24));
			IntPtr compListPtr = Game.proc.ReadPointer(this.adr + (Game.is64bit ? 0x30 : 0x1C));
			byte[] componentList = Game.proc.ReadBytes(compListPtr , (numComponents * (Game.is64bit ? 0x10 : 0x8)) + (Game.is64bit ? 0x8 : 0x4));
			for (int i = 0; i < numComponents; i++)
			{
				int type = Game.is64bit ? BitConverter.ToInt32(componentList, i * 0x10) : BitConverter.ToInt32(componentList, i * 0x8);
				IntPtr compPtr = Game.is64bit ? (IntPtr)BitConverter.ToInt64(componentList, (i * 0x10) + 0x8) : (IntPtr)BitConverter.ToInt32(componentList, (i * 0x8) + 0x4);
				
				ObjectComponent component = new ObjectComponent(compPtr, Game.TypeDict[type], this);
				output.Add(component);
			}

			return output;
		}

		public override string ToString()
		{
			return this.adr.ToString("X16") + " " + this.path;
		}
	}
}
