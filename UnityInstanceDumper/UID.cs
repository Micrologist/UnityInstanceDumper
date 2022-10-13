using LiveSplit.ComponentUtil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace UnityInstanceDumper
{
	class UID
	{

		static void Main(string[] args)
		{
			string processName = "";
			if (args.Length > 0)
			{
				processName = args[0];
			}

			while (!Dump(processName))
			{
				Console.Clear();
				Console.Write("Enter the name of the game process: ");
				processName = Console.ReadLine().Replace(".exe", "");
				Console.WriteLine();
			}

			Console.WriteLine("\nFound {0} scenes containing {1} GameObjects with {2} components in {3} ms.", Game.SceneDict.Count, Game.ObjDict.Count, Game.CompDict.Count, (System.DateTime.Now - Game.StartTime).TotalMilliseconds.ToString("0."));

			string gamePath = Path.GetDirectoryName(Game.proc.MainModuleWow64Safe().FileName) + "\\";
			using (StreamWriter writer = new StreamWriter(gamePath + "UID_ComponentDump.txt", false))
			{
				foreach (KeyValuePair<string, ObjectComponent> kvp in Game.CompDict)
				{
					writer.WriteLine(kvp.Value.ToString());
				}
			}

			Console.ReadLine();
		}

		static bool Dump(string processName)
		{
			List<Process> processList = Process.GetProcesses().ToList().FindAll(x => x.ProcessName == processName);
			return processList.Count != 0 && Game.Scan(processList[0]);
		}
	}
}
