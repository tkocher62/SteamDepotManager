using System.Net;

namespace SteamDepotInstaller2
{
	class Program
	{
		static void Main(string[] args)
		{
			Install.ftp.Credentials = new NetworkCredential("todd", "gmodsniper");

			if (args.Length > 0)
			{
				if (args[0].Contains("TINserver.ini"))
				{
					Install.InstallIni(args[0]);
				}
				else
				{
					Install.InstallGame(args[0]);
				}
			}
			else
			{
				Get.GetGameFiles();
			}
		}
	}
}
