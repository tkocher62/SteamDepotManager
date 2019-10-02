using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamDepotInstaller2
{
	public static class Utils
	{
		public static int CountFiles(string sourceFolder)
		{
			try
			{
				return Directory.GetFiles(sourceFolder, "*", SearchOption.AllDirectories).Length;
			}
			catch
			{
				return -1;
			}
		}
	}
}
