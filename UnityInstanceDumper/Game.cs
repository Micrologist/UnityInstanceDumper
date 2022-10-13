using LiveSplit.ComponentUtil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace UnityInstanceDumper
{
	public static class Game
	{
		public static Process proc;
		public static bool is64bit;
		public static Dictionary<string, GameObject> ObjDict;
		public static Dictionary<string, Scene> SceneDict;
		public static Dictionary<string, ObjectComponent> CompDict;
		public static Dictionary<int, string> TypeDict;
		public static Dictionary<IntPtr, string> KlassNames;
		public static DateTime StartTime;

		private static IntPtr SceneManager, RuntimeTypesArray;

		public static bool Scan(Process proc)
		{
			StartTime = System.DateTime.Now;
			Game.proc = proc;
			Game.is64bit = Game.proc.Is64Bit();

			ObjDict = new Dictionary<string, GameObject>();
			SceneDict = new Dictionary<string, Scene>();
			CompDict = new Dictionary<string, ObjectComponent>();
			TypeDict = new Dictionary<int, string>();
			KlassNames = new Dictionary<IntPtr, string>();

			if (!GetSceneManager())
			{
				Console.WriteLine("\nError: Couldn't find SceneManager");
				Console.ReadLine();
				return false;
			}

			if (!DumpRuntimeTypes())
			{
				Console.WriteLine("\nError: Couldn't dump RuntimeTypes");
				Console.ReadLine();
				return false;
			}

			Console.WriteLine("Found SceneManager at 0x{0}", SceneManager.ToString("X16"));


			_ = new DeepPointer(SceneManager, (Game.is64bit ? 0x70 : 0x40)).DerefOffsets(Game.proc, out IntPtr dontDestroyScenePtr);
			SceneDict.Add(dontDestroyScenePtr.ToString("X16"), new Scene(dontDestroyScenePtr, "DontDestroyOnLoad"));
			Console.WriteLine("Found \"{0}\" scene at 0x{1}", SceneDict[dontDestroyScenePtr.ToString("X16")].name, dontDestroyScenePtr.ToString("X16"));

			new DeepPointer(SceneManager, Game.is64bit ? 0x18 : 0x10).Deref<int>(Game.proc, out int numLoadedScenes);
			for (int i = 0; i < numLoadedScenes; i++)
			{
				new DeepPointer(SceneManager, 0x8, i * (Game.is64bit ? 0x8 : 0x4), 0x0).DerefOffsets(Game.proc, out IntPtr scenePtr);
				SceneDict.Add(scenePtr.ToString("X16"), new Scene(scenePtr));
				Console.WriteLine("Found \"{0}\" scene at 0x{1}", SceneDict[scenePtr.ToString("X16")].name, scenePtr.ToString("X16"));
			}
			return true;
		}

		private static bool DumpRuntimeTypes()
		{

			if (!GetRuntimeTypesArray())
			{
				return false;	
			}

			Console.WriteLine("Found RuntimeTypes array at 0x" + RuntimeTypesArray.ToString("X16"));

			int numTypes = Game.proc.ReadValue<int>(RuntimeTypesArray - (Game.is64bit ? 0x8 : 0x4));
			IntPtr typePtr = RuntimeTypesArray; 

			for (int i = 0; i < numTypes; i++)
			{
				IntPtr type = Game.proc.ReadPointer(typePtr);
				string name = Game.proc.ReadString(Game.proc.ReadPointer(type + (Game.is64bit ? 0x10 : 0x8)), 255);
				int id = Game.proc.ReadValue<int>(type + (Game.is64bit ? 0x30 : 0x1C));
				TypeDict.Add(id, name);
				typePtr += (Game.is64bit ? 0x8 : 0x4);
			}

			return true;
		}

		private static bool GetRuntimeTypesArray()
		{
			SigScanTarget RuntimeTypesTarget = new SigScanTarget();
			SignatureScanner scanner;

			if (Game.is64bit)
			{
				RuntimeTypesTarget.AddSignature(3, "48 8D 0D ?? ?? ?? ?? 48 8B DA 4C 8B 76 ?? 48 8B 04 C1 49 8D 6E ?? 44 8B 78 ?? 48 8B 46");
				RuntimeTypesTarget.AddSignature(3, "48 8D 0D ?? ?? ?? ?? 48 C1 E8 15 48 8B 0C C1 33 C0 48 8B 51");
				RuntimeTypesTarget.AddSignature(3, "48 8D 15 ?? ?? ?? ?? 48 C1 E9 15 4C 8D 4C 24");
				RuntimeTypesTarget.AddSignature(3, "48 8D 15 ?? ?? ?? ?? 48 C1 E8 15 45 33 C9 48 8B 14 C2 41 8B C1 4C 8B 42 ?? 4C 8B 52 ?? 4D 85 C0");
				RuntimeTypesTarget.AddSignature(3, "48 8D 15 ?? ?? ?? ?? 8B 48 0C 48 C1 E9 15 48 8B 0C CA 8B 51 ?? 8B CA ");
				RuntimeTypesTarget.OnFound = (f_proc, f_scanner, f_ptr) =>
				{
					return IntPtr.Add(f_ptr + 4, Game.proc.ReadValue<int>(f_ptr));
				};
			}
			else
			{
				RuntimeTypesTarget.AddSignature(3, "8B 04 85 ?? ?? ?? ?? 89 55 FC 8B 40 1C 89 45 F8 8B 43 ?? ");
				RuntimeTypesTarget.AddSignature(3, "8B 04 85 ?? ?? ?? ?? 8B 50 ?? 8B 40 2C 85 C0");
				RuntimeTypesTarget.OnFound = (f_proc, f_scanner, f_ptr) =>
				{
					return (Game.proc.ReadPointer(f_ptr));
				};
			}

			var UnityPlayer = Game.proc.ModulesWow64Safe().FirstOrDefault(m => m.ModuleName == "UnityPlayer.dll");
			if (UnityPlayer == null) return false;
			scanner = new SignatureScanner(Game.proc, UnityPlayer.BaseAddress, UnityPlayer.ModuleMemorySize);
			return ((RuntimeTypesArray = scanner.Scan(RuntimeTypesTarget)) != IntPtr.Zero);
		}

		private static bool GetSceneManager()
		{
			SigScanTarget SceneManagerTarget = new SigScanTarget();
			SignatureScanner scanner;

			if (Game.is64bit)
			{
				SceneManagerTarget.AddSignature(3, "48 8B 0D ?? ?? ?? ?? 48 8D 55 ?? 89 45 ?? 0F");
				SceneManagerTarget.AddSignature(3, "4C 8B 3D ?? ?? ?? ?? 4C 89 7C 24");
				SceneManagerTarget.AddSignature(3, "48 8B 1D ?? ?? ?? ?? 0F 57 C0 41 83 3E 01 4C 89 A4 24");
				SceneManagerTarget.AddSignature(3, "4C 8B 05 ?? ?? ?? ?? 48 8D 4D B0 8B 15");
				SceneManagerTarget.AddSignature(3, "4C 8B 3D ?? ?? ?? ?? 0F 11 45 ?? 4C 89 7D");
				SceneManagerTarget.OnFound = (f_proc, f_scanner, f_ptr) =>
				{
					return IntPtr.Add(f_ptr + 4, Game.proc.ReadValue<int>(f_ptr));
				};
			}
			else
			{
				SceneManagerTarget.AddSignature(1, "A1 ?? ?? ?? ?? 53 33 DB 89 45");
				SceneManagerTarget.OnFound = (f_proc, f_scanner, f_ptr) =>
				{
					return !f_proc.ReadPointer(f_ptr, out f_ptr) ? IntPtr.Zero : f_ptr;
				};
			}
			var UnityPlayer = Game.proc.ModulesWow64Safe().FirstOrDefault(m => m.ModuleName == "UnityPlayer.dll");
			if (UnityPlayer == null) return false;
			scanner = new SignatureScanner(Game.proc, UnityPlayer.BaseAddress, UnityPlayer.ModuleMemorySize);
			return ((SceneManager = scanner.Scan(SceneManagerTarget)) != IntPtr.Zero);
		}
	}
}
