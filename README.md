# SteamDepotManager
This program was designed to archive and store game files from [Steam](https://store.steampowered.com/) on a remote file host.

### Downloading
The first step is downloading a copy of the game files to be uploaded, ensuring your main files aren't tampered with in any way. This can be done in two ways:

First method, entering the app id of the game you wish to download. This is something that can be found with a quick google search.

![](https://github.com/tkocher62/SteamDepotManager/blob/master/gifs/appid.gif)

Second method, entering the exact name of the game. This will search the Steam database for the matching app id and continue with the process.

![](https://github.com/tkocher62/SteamDepotManager/blob/master/gifs/appname.gif)

After either method is done, the program will begin copying the files to a new folder on your desktop. This folder will contain both the game files itself as well as any necessary data files.

### Uploading
Next, we want to merge the files into one folder. This step isn't necessary, but having all the game files in a single folder makes larger upload significantly easier

![](https://github.com/tkocher62/SteamDepotManager/blob/master/gifs/merge%20games.gif)

Now that we have the files in one place, we drag and drop the folder on to the executable in order to start the upload. One game will be uploaded at a time, immediately afterwards the next game will begin its upload. This not only makes the uploading process easier than using an FTP client, but it places all files in their respective categories on the file host, eliminating the hassle of organizing the file system.

![](https://github.com/tkocher62/SteamDepotManager/blob/master/gifs/upload.gif)

Once the final game is finished its upload, the program will clean up all game file copies.

![](https://github.com/tkocher62/SteamDepotManager/blob/master/gifs/finish.gif)
