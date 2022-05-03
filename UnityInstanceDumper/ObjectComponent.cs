using LiveSplit.ComponentUtil;
using System;

namespace UnityInstanceDumper
{
	public class ObjectComponent
	{
		public IntPtr adr;
		public string name;
		public string path;
		public GameObject owningObject;

		public ObjectComponent(IntPtr adr, string name, GameObject owningObject)
		{
			this.adr = adr;
			this.owningObject = owningObject;
			this.name = name == "MonoBehaviour" ? GetMonoBehaviourName() : name;
			this.path = owningObject.path + "." + this.name;
			Game.CompDict.Add(this.adr.ToString("X16"), this);
		}

		string GetMonoBehaviourName()
		{
			string output = "???";
			if (new DeepPointer(this.adr + (Game.is64bit ? 0x28 : 0x18), 0x0, 0x0, (Game.is64bit ? 0x48 : 0x2C), 0x0).DerefOffsets(Game.proc, out IntPtr namePtr))
			{
				output = Game.proc.ReadString(namePtr, 250);
			}
			return output;
		}

		public override string ToString()
		{
			return this.adr.ToString("X16") + " " + this.name + " " + this.path;
		}
	}
}
