using System;
using System.IO;

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
