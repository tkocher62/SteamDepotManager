using FluentFTP;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SteamDepotInstaller2
{
	public static class Install
	{
		private static string gamesFilePath;
		private const string contentFilePath = "Server/data/Content/3";
		public static FtpClient ftp = new FtpClient("steam.gameslab.ca");
		private static int lastCount = 0;
		private static float fCount = 0;

		private static void UploadFile(string src, string dest)
		{
			if (ftp.Credentials != null)
			{
				ftp.UploadFile(src, dest);
			}
		}

		private static void TransferFiles(string name, string subfolderSrc = "", string subFolderDst = "", bool useContentFolder = true)
		{
			DirectoryInfo d = new DirectoryInfo($"{gamesFilePath}{(subfolderSrc != "" ? $"/{subfolderSrc}" : "")}");
			FileInfo[] files = d.GetFiles();

			Console.WriteLine($"Transferring {name}...");

			float inc = 100f / files.Length;
			float currInc = 0f;

			foreach (var file in files)
			{
				string de = $"{(useContentFolder ? contentFilePath : "")}{(subFolderDst != "" ? $"/{subFolderDst}" : "")}/{file.Name}";
				if (!File.Exists(de))
				{
					UploadFile(file.FullName, de);
					File.Delete(file.FullName);
				}
				else
				{
					File.Delete(file.FullName);
				}
				currInc += inc;
				Console.WriteLine($"-> Transferring {name} ({file.Name})... [{(int)currInc}%]");
			}
			Console.WriteLine($"Finished transferring {name}!");
		}

		private static int GetIndexArray(string[] array, string text)
		{
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].Contains(text)) return i;
			}
			return -1;
		}

		private static void MoveFolder(string sourceFolder, string destFolder, string dirName, int totalCount)
		{
			try
			{
				if (ftp.DirectoryExists(destFolder)) ftp.DeleteDirectory(destFolder);
				ftp.CreateDirectory(destFolder);
				DirectoryInfo d = new DirectoryInfo(sourceFolder);
				FileInfo[] files = d.GetFiles();
				foreach (FileInfo file in files)
				{
					string name = Path.GetFileName(file.FullName);
					string dest = Path.Combine(destFolder, name);
					if (!File.Exists(dest))
					{
						UploadFile(file.FullName, dest);
						File.Delete(file.FullName);
					}
					else File.Delete(file.FullName);

					fCount++;
					int progress = (int)(fCount / totalCount * 100);
					if (lastCount != progress && progress > lastCount)
					{
						Console.WriteLine($"-> Transferring sub files ({dirName})... [{progress}%] [{fCount}/{totalCount}]");
					}
					lastCount = progress;
				}
				string[] folders = Directory.GetDirectories(sourceFolder);
				foreach (string folder in folders)
				{
					string name = Path.GetFileName(folder);
					string dest = Path.Combine(destFolder, name);
					MoveFolder(folder, dest, dirName, totalCount);
				}

				if (Directory.Exists(sourceFolder)) Directory.Delete(sourceFolder);
			}
			catch (Exception x)
			{
				Console.WriteLine($"[FTP ERROR] {x.Message}");
				Console.WriteLine("Press enter to attempt to continue transfer...");
				MoveFolder(sourceFolder, destFolder, dirName, totalCount);
				Console.ReadLine();
			}
		}

		public static void InstallGame(string folder)
		{
			if (!Directory.Exists(folder))
			{
				Console.WriteLine("Invalid directory.");
				return;
			}
			gamesFilePath = folder;

			Stopwatch stopWatch = new Stopwatch();
			stopWatch.Start();

			Console.WriteLine("Initializing file transfer...");
			Console.WriteLine("-----------------------------");

			// DEPOTCACHE
			TransferFiles("depotcache", "depotcache", "depotcache");
			Console.WriteLine("-----------------------------");

			// ACHIEVEMENTS
			TransferFiles("achievements", "", $"Server/data/GameStats/schemas", false);
			Console.WriteLine("-----------------------------");

			// STEAMIDS
			Console.WriteLine("Clearing SteamIDs...");

			DirectoryInfo d = new DirectoryInfo($"{gamesFilePath}/steamapps");
			FileInfo[] files = d.GetFiles("*.acf");

			float inc = 100f / files.Length;
			float curInc1 = 0f;

			foreach (var file in files)
			{
				string[] acfLines = File.ReadAllLines(file.FullName);
				int indx = GetIndexArray(acfLines, "LastOwner");
				if (indx != -1)
				{
					acfLines[indx] = acfLines[indx].Replace(acfLines[indx].Split('"')[3], "0");
					File.WriteAllLines(file.FullName, acfLines);
					curInc1 += inc;
					Console.WriteLine($"-> Clearing SteamIDs ({file.Name})... [{(int)curInc1}%]");
				}
			}
			Console.WriteLine("Finished clearing SteamIDs!");
			Console.WriteLine("-----------------------------");

			// ACF
			TransferFiles("acf", "steamapps");
			Console.WriteLine("-----------------------------");

			// GAME FILES
			Console.WriteLine("Transferring game files...");
			string[] dirs = Directory.GetDirectories($"{gamesFilePath}/steamapps/common").Select(Path.GetFileName).ToArray();
			float inc2 = 100f / dirs.Length;
			float curInc2 = 0f;
			foreach (string dir in dirs)
			{
				fCount = 0;
				lastCount = 0;
				string destDir = $"{gamesFilePath}/steamapps/common/{dir}";
				int totalfiles = Utils.CountFiles(destDir);
				Console.WriteLine($"Initializing transfer of {totalfiles} files for {dir}...");
				MoveFolder(destDir, $"{contentFilePath}/common/{dir}", dir, totalfiles);
				curInc2 += inc2;
				Console.WriteLine($"File transfer for {dir} complete... [{(int)curInc2}%]");
			}
			Console.WriteLine("-----------------------------");

			Console.WriteLine("Cleaning up...");

			Directory.Delete($"{gamesFilePath}/depotcache");
			Directory.Delete($"{gamesFilePath}/steamapps/common");
			Directory.Delete($"{gamesFilePath}/steamapps");
			Directory.Delete(gamesFilePath);

			Console.WriteLine("-----------------------------");

			stopWatch.Stop();
			Console.WriteLine($"File transfer finished!");
			Console.WriteLine($"Elapsed time: {String.Format("{0}h {1}m {2}s", stopWatch.Elapsed.Hours, stopWatch.Elapsed.Minutes, stopWatch.Elapsed.Seconds)}");
			Console.WriteLine("-----------------------------");
			Console.WriteLine("Press enter to exit...");

			Console.ReadLine();
		}

		public static void InstallIni(string filepath)
		{
			UploadFile(filepath, "Server/TINserver.ini");
			Console.WriteLine("Finished transfer of ini file. Press enter to exit.");
			Console.ReadLine();
		}
	}
}
