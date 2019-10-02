using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SteamDepotInstaller2
{
	class Program
	{
		static void Main(string[] args)
		{
			if (args.Length > 0)
			{
				Install.InstallGame(args[0]);
			}
			else
			{
				Get.GetGameFiles();
			}
		}
	}
}
