using FluentFTP;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace SteamDepotInstaller2
{
	class Program
	{
		static void Main(string[] args)
		{
			Install.ftp.DataConnectionType = FtpDataConnectionType.EPSV;
			Install.ftp.Credentials = new NetworkCredential("ian", "test");

			Get.GetSteamDir(730);

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
