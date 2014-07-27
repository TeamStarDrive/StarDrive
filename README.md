# Stardrive BlackBox#
So what is black box? its a decompiled recompiled version of stardrive. 
It contains fixes and enhancements to the basic game while trying to keep the game as close to vanilla as possible. 
Ideally you would be hard pressed to tell the difference between regular stardrive and balckbox stardrive but... things might work better in black. Might have more controls. freighters might behave more like expected. might experience less crashes. might have better game performance. And there might not be any "might" about it. it does a lot of these things and more.
Mods can be run all from the mod directory. ( some assembly required as most mods are distrubted with vanilla in mind)
right click enemy planets to land troops, from troop carriers too.
Fighters will return to carriers when out of ammo or badly damaged.
and more.

#feature list#
[Version 1](http://bitbucket.org/CrunchyGremlin/sd-idk/issues?component=%21Code+Project+Not+game.&component=%21Mod+Issue&status=closed&status=resolved&version=0.1+Gravity)

###I just want to play the mod Crunchy!###

* Great!
* If you are running an in game mod then unload that before doing the below. Go into the games mod manager and unload any loaded mod. You can load them up after you switch to this.
* go to the download section and download the latest release version. Do not download the top option for the repository. You don't need that. Download the test version if your level of patience can take it.
* backup your original stardrive.exe and stardrive.exe.config. 
open the zip and look at the files inside and back up any folders and files you want to be able to restore. 
You don't have to do this as you can just go to the steam and reverify steam content for the game.
* extract the zip into the the game directory usually located in Program Files (x86)\Steam\SteamApps\common\StarDrive
* Overwrite files when asked.

#### Mod Super Pack ####
This is a blackbox download that includes these mods:

* BlackBox : http://www.stardrivegame.com/forum/viewtopic.php?f=6&t=9910

* Techlevel : http://www.stardrivegame.com/forum/viewtopic.php?f=6&t=7091

* Tims : http://www.stardrivegame.com/forum/viewtopic.php?f=6&t=3163

* ComboMod : http://www.stardrivegame.com/forum/viewtopic.php?f=6&t=7735

* Overdrive : http://www.stardrivegame.com/forum/viewtopic.php?f=6&t=1617

* StarWars : http://www.stardrivegame.com/forum/viewtopic.php?f=6&t=9859




##### Compatibility ####
* This mod should be compatible with normal content mods. 
* It is compatible with Deveks Code mods but... Dont do that. just run this.
* THE SAVE GAMES FROM THIS MOD MAY NOT LOAD IN VANILLA SD.
* Vanilla SD saves will load in this mod but saving again may break compatibility with SD vanilla.

###Regular Mods###
* how to install...
* well... ideally the mod would support the mod enhancements in the BlackBox Code mode. Otherwise you should be able to take the mod files and put the them all in the mod folder and any folders that the mod tells you to put in the content folder... Dont. Put them int he mod folder too. If something goes wrong. Post the issue and we hopefully will figure out how to get that mod to work.
* Some modders have put together blackbox compatible downloads for their mods. here is a list. Well.. Its short...
* TechLevel: https://www.dropbox.com/s/frzdmh1n16g5rap/Tech%20Level%20Mod%20V1.07F.zip
* if the mod has a "starter ships" folder and crashes on load copy the contents of the starterships folder into the mods ship designs folder and delete the mods starterships folder.

#Future Features#
[Future Features](https://bitbucket.org/CrunchyGremlin/sd-idk/issues?component=%21Mod+Issue&status=%21closed&status=%21resolved&version=%210.1+Gravity)

### Yeah thats great but i want to add my own features or change some issue that bugs me###
* even better!!!
* Ill try to describe as best I can how to compile and contribute to this project.
* friend me on steam and request access to the steam SDCodemod forum.

### This is for modifying the StarDrive game 15B for fun and learning###

* Stardrive can be opened up in a variety of .net decompilers which all get the code wrong in different ways. It has to be cleaned up and corrected to work correctly and that is what this repository is.
* Ver. SE 0.1


### How do I get set up? ###

* Setup
* 1. Install Source tree
2. Use bitbucket to download the source on the overview page.
3. Configure Visual Studio for your environment.
* Check the references. Make sure all references are setup. ALl the files are in assemblies or in the stardrive folder.
* You'll need .net4 to compile this version.
* Once you get the game compiled and run load up some saved games and let it run for while in debug mode.
* When ready contact Crunchy Gremlin or General Azure (or ask on the devs normal forums) and we can get you setup with a branch of your own and try to vet your changes.

### Contribution guidelines ###

* Code in your branch. Make sure SD compiles and loads.
* Comment your code so people can see what you are changing
* Prefix areas of change by \\Added by <your Alias> <whatever changes>

### Who do I talk to? ###

* Crunchy gremlin is the current owner of the repositor. General Azure has admin access.
* Any other user or make a post on the SD forums.