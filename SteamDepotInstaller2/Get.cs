using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
namespace SteamDepotInstaller2
{
	class GameInfo
	{
		public string gTitle;
		public string gDirTitle;
		public int gID;
	}

	public static class Get
	{
		private static readonly string dPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
		private static readonly string sPath = (string)Registry.GetValue(Environment.Is64BitOperatingSystem ?
			@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Valve\Steam" :
			@"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam", "InstallPath", null);
		private static int lastCount = 0;
		private static float fCount = 0;

		private static bool InitCopy(string sourceFolder, string destFolder)
		{
			fCount = 0;
			int count = Utils.CountFiles(sourceFolder);
			if (count != -1)
			{
				CopyFolder(sourceFolder, destFolder, count);
				return true;
			}
			return false;
		}

		private static void CopyFolder(string sourceFolder, string destFolder, int totalCount)
		{
			if (!Directory.Exists(destFolder))
				Directory.CreateDirectory(destFolder);
			string[] files = Directory.GetFiles(sourceFolder);
			foreach (string file in files)
			{
				string name = Path.GetFileName(file);
				string dest = Path.Combine(destFolder, name);
				File.Copy(file, dest);

				fCount++;
				int progress = (int)(fCount / totalCount * 100);
				if (lastCount != progress && progress > lastCount)
				{
					Console.WriteLine($"Progress: [{progress}%]");
				}
				lastCount = progress;
			}
			string[] folders = Directory.GetDirectories(sourceFolder);
			foreach (string folder in folders)
			{
				string name = Path.GetFileName(folder);
				string dest = Path.Combine(destFolder, name);
				CopyFolder(folder, dest, totalCount);
			}
		}

		private static GameInfo GetAppInfo(string title)
		{
			Console.WriteLine("\nScanning for game...");

			GameInfo gi = new GameInfo();

			WebClient c = new WebClient();
			string t = title.ToLower();
			string data = null;
			try
			{
				data = c.DownloadString("https://api.steampowered.com/ISteamApps/GetAppList/v2/");
			}
			catch
			{
				return null;
			}
			JObject o = JObject.Parse(data);

			foreach (JToken a in o["applist"]["apps"].Where(x => ((string)x["name"]).ToLower() == t))
			{
				if (int.TryParse((string)a["appid"], out int b))
				{
					gi.gID = b;
					gi.gTitle = (string)a["name"];
				}
				else
				{
					Console.WriteLine("Error parsing appid.");
					Console.WriteLine("Press enter to continue...");
					Console.ReadLine();
					GetGameFiles();
				}
			}
			return gi;
		}

		public static void GetGameFiles()
		{
			Console.WriteLine("Enter the steam game id, or the game title exactly as shown in steam wrapped in quotes.\nThe game must be installed on this computer.");
			string gameTitle = Console.ReadLine();
			GameInfo gInfo = new GameInfo();

			if (gameTitle.Contains("\""))
			{
				gameTitle = gameTitle.Replace("\"", "");
				gInfo = GetAppInfo(gameTitle);
				if (gInfo == null)
				{
					Console.WriteLine("Error connecting to steam API.");
					Console.WriteLine("Press enter to continue...");
					Console.ReadLine();
					GetGameFiles();
				}
			}
			else if (int.TryParse(gameTitle, out int a))
			{
				gInfo.gID = a;
				try
				{
					gInfo.gTitle = File.ReadAllLines($"{sPath}/steamapps/appmanifest_{gInfo.gID}.acf").ToList()[4].Replace("\"name\"", "").Split('"')[1];
				}
				catch
				{
					Console.WriteLine($"Couldn't find a game with id {gInfo.gID}.");
					Console.WriteLine("Press enter to continue...");
					Console.ReadLine();
					GetGameFiles();
				}
			}
			else
			{
				Console.WriteLine("Unexpected parsing error.");
			}

			string acf = $"appmanifest_{gInfo.gID}.acf";

			List<string> lines = File.ReadAllLines($"{sPath}/steamapps/{acf}").ToList();
			gInfo.gDirTitle = lines[6].Replace("\"installdir\"", "").Split('"')[1];
			if (Directory.Exists($"{dPath}/{gInfo.gDirTitle}"))
			{
				Console.WriteLine("A backup of this game already exists!");
				Console.WriteLine("Press enter to continue...");
				Console.ReadLine();
				GetGameFiles();
			}

			Console.WriteLine($"Game found [Title: {gInfo.gTitle}] [ID: {gInfo.gID}]");
			Console.WriteLine("----------------------------------");
			Console.WriteLine("Transferring files, please wait...");

			if (!File.Exists($"{sPath}/steamapps/appmanifest_{gInfo.gID}.acf"))
			{
				Console.WriteLine("Couldn't locate game files. Make sure it's installed on your computer.");
				Console.WriteLine("Press enter to continue...");
				Console.ReadLine();
				GetGameFiles();
			}

			Console.WriteLine("Creating directory...");

			Directory.CreateDirectory($"{dPath}/{gInfo.gDirTitle}");
			Directory.CreateDirectory($"{dPath}/{gInfo.gDirTitle}/steamapps");
			Directory.CreateDirectory($"{dPath}/{gInfo.gDirTitle}/steamapps/common");
			Directory.CreateDirectory($"{dPath}/{gInfo.gDirTitle}/depotcache");

			// Copy acf
			Console.WriteLine("Copying data files...");
			File.Copy($"{sPath}/steamapps/{acf}", $"{dPath}/{gInfo.gDirTitle}/steamapps/{acf}");

			// Copy achievements
			Console.WriteLine("Copying achievements...");
			string file = $"UserGameStatsSchema_{gInfo.gID}.bin";
			string getFile = $"{sPath}/appcache/stats/{file}";
			if (File.Exists(getFile)) File.Copy(getFile, $"{dPath}/{gInfo.gDirTitle}/{file}");

			// Copy mounted manifests
			int index = lines.FindIndex(x => x.Contains("MountedDepots")) + 2;
			for (int i = index; i < lines.Count; i++)
			{
				string line = lines[i];
				if (line.Contains("}")) break;
				else
				{
					string[] split = line.Split('"');
					string fName = $"{split[1]}_{split[3]}.manifest";
					if (!File.Exists($"{dPath}/{gInfo.gDirTitle}/depotcache/{fName}"))
					{
						File.Copy($"{sPath}/depotcache/{fName}", $"{dPath}/{gInfo.gDirTitle}/depotcache/{fName}");
					}
				}
			}

			// Copy shared manifests
			int index2 = lines.FindIndex(x => x.Contains("SharedDepots"));
			if (index2 != -1)
			{
				for (int i = index2 + 2; i < lines.Count; i++)
				{
					string line = lines[i];
					if (line.Contains("}")) break;
					else
					{
						string[] split = line.Split('"');

						DirectoryInfo hdDirectoryInWhichToSearch = new DirectoryInfo($"{sPath}/depotcache/");
						FileInfo[] filesInDir = hdDirectoryInWhichToSearch.GetFiles("*" + split[1] + "*.*");

						foreach (FileInfo foundFile in filesInDir)
						{
							string fName = foundFile.FullName.Replace($@"{sPath}\depotcache\", "");

							if (!File.Exists($"{dPath}/{gInfo.gDirTitle}/depotcache/{fName}"))
							{
								File.Copy($"{sPath}/depotcache/{fName}", $"{dPath}/{gInfo.gDirTitle}/depotcache/{fName}");
							}
						}
					}
				}
			}

			// Copy game files
			Console.WriteLine("Copying game files...");
			bool success = InitCopy($"{sPath}/steamapps/common/{gInfo.gDirTitle}", $"{dPath}/{gInfo.gDirTitle}/steamapps/common/{gInfo.gDirTitle}");
			Console.WriteLine("----------------------------------");
			if (!success)
			{
				Console.WriteLine("Error copying files. Try running as an administrator.");
				Console.WriteLine("Press enter to continue...");
				Console.ReadLine();
			}

			Console.WriteLine("File transfer completed. The files have been placed on your desktop.");
			Console.WriteLine();
			GetGameFiles();
			Console.ReadLine();
		}
	}
}
