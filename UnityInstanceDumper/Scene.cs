using LiveSplit.ComponentUtil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityInstanceDumper
{
	public class Scene
	{
		public IntPtr adr;
		public string name;
		public string path;
		public List<GameObject> rootObjects;

		public Scene(IntPtr ptr, string name = "")
		{
			this.adr = ptr;
			if (!String.IsNullOrEmpty(name))
			{
				this.path = name;
				this.name = name;
			}
			else
			{
				this.path = GetPath();
				int extension = path.LastIndexOf(".unity");
				this.name = extension == -1 ? path : path.Remove(extension).Split('/').Last();
			}
			this.rootObjects = GetRootObjects();
		}

		string GetPath()
		{
			return new DeepPointer(this.adr + (Game.is64bit ? 0x10 : 0xC), 0x0).DerefString(Game.proc, 250);
		}

		List<GameObject> GetRootObjects()
		{
			List<IntPtr> objects = new List<IntPtr>();
			new DeepPointer(this.adr + (Game.is64bit ? 0xB8 : 0x8C), 0x0).DerefOffsets(Game.proc, out IntPtr objectPtr);
			new DeepPointer(this.adr + (Game.is64bit ? 0xB0 : 0x88), 0x0).DerefOffsets(Game.proc, out IntPtr lastObjectPtr);

			while (objectPtr != lastObjectPtr && objectPtr != IntPtr.Zero)
			{
				new DeepPointer(objectPtr + (Game.is64bit ? 0x8 : 0x4), 0x0).DerefOffsets(Game.proc, out objectPtr);
				objects.Add(objectPtr);
			}
			new DeepPointer(objectPtr + (Game.is64bit ? 0x8 : 0x4), 0x0).DerefOffsets(Game.proc, out objectPtr);
			objects.Add(objectPtr);

			List<GameObject> output = new List<GameObject>();
			foreach (IntPtr oPtr in objects)
			{
				new DeepPointer(oPtr, (Game.is64bit ? 0x10 : 0x8), (Game.is64bit ? 0x30 : 0x1C), 0x0).DerefOffsets(Game.proc, out IntPtr goPtr);
				output.Add(new GameObject(goPtr, null, this));
			}

			return output;
		}
	}
}
