using System;
using System.IO;
using System.Linq;
using System.Net;

namespace SteamDepotInstaller2
{
	public static class Install
	{
		private static string gamesFilePath;
		private const string contentFilePath = "Server/data/Content/3";
		private static WebClient client = new WebClient();
		private static int lastCount = 0;
		private static float fCount = 0;

		private static void UploadFile(string src, string dest)
		{
			if (client.Credentials != null)
			{
				client.UploadFile($"ftp://108.36.249.161/{dest}", src);
			}
		}

		private static void TransferFiles(string name, string subfolderSrc = "", string subFolderDst = "")
		{
			DirectoryInfo d = new DirectoryInfo($"{gamesFilePath}{(subfolderSrc != "" ? $"/{subfolderSrc}" : "")}");
			FileInfo[] files = d.GetFiles();

			Console.WriteLine($"Transferring {name}...");

			float inc = 100f / files.Length;
			float currInc = 0f;

			foreach (var file in files)
			{
				string de = $"{contentFilePath}{(subFolderDst != "" ? $"/{subFolderDst}" : "")}/{file.Name}";
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
				Console.WriteLine($"Transferring {name} ({file.Name})... [{(int)currInc}%]");
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
			if (!Directory.Exists(destFolder))
			{
				WebRequest request = WebRequest.Create($"ftp://108.36.249.161/{destFolder}");
				request.Method = WebRequestMethods.Ftp.MakeDirectory;
				request.Credentials = new NetworkCredential("todd", "gmodsniper");
				try
				{
					FtpWebResponse a = (FtpWebResponse)request.GetResponse();
				}
				catch (Exception x)
				{
					// Directory already exists, so we do nothing
				};
			}
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
					Console.WriteLine($"-> Transferring sub files ({dirName})... [{progress}%]");
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

		public static void InstallGame(string folder)
		{
			if (!Directory.Exists(folder))
			{
				Console.WriteLine("Invalid directory.");
				return;
			}
			gamesFilePath = folder;

			client.Credentials = new NetworkCredential("todd", "gmodsniper");

			Console.WriteLine("Initializing file transfer...");
			Console.WriteLine("-----------------------------");

			// DEPOTCACHE
			TransferFiles("depotcache", "depotcache", "depotcache");
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
				acfLines[indx] = acfLines[indx].Replace(acfLines[indx].Split('"')[3], "0");
				File.WriteAllLines(file.FullName, acfLines);
				curInc1 += inc;
				Console.WriteLine($"Clearing SteamIDs ({file.Name})... [{(int)curInc1}%]");
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
				MoveFolder(destDir, $"{contentFilePath}/common/{dir}", dir, Utils.CountFiles(destDir));
				curInc2 += inc2;
				Console.WriteLine($"Transferring game files [{dir}]... [{(int)curInc2}%]");
			}
			Console.WriteLine("-----------------------------");

			Console.WriteLine("Cleaning up...");

			Directory.Delete($"{gamesFilePath}/depotcache");
			Directory.Delete($"{gamesFilePath}/steamapps/common");
			Directory.Delete($"{gamesFilePath}/steamapps");
			Directory.Delete(gamesFilePath);

			Console.WriteLine("-----------------------------");

			Console.WriteLine("File transfer finished!");
			Console.WriteLine("Press enter to exit...");

			Console.ReadLine();
		}
	}
}
