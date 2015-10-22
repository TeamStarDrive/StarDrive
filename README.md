# Stardrive BlackBox#
So what is black box? its a decompiled recompiled version of stardrive. 
It contains fixes and enhancements to the basic game while trying to keep the game as close to vanilla as possible. 
Ideally you would be hard pressed to tell the difference between regular stardrive and balckbox stardrive but... things might work better in black. Might have more controls. freighters might behave more like expected. might experience less crashes. might have better game performance. And there might not be any "might" about it. it does a lot of these things and more.
Mods can be run all from the mod directory. ( some assembly required as most mods are distrubted with vanilla in mind)
right click enemy planets to land troops, from troop carriers too.
Fighters will return to carriers when out of ammo or badly damaged.
and more.

# Donations#
Donations are gladly accepted. They will be used for non game related actions and stress relief. Certainly dont feel pressured to donate. If you want to toss one of us a few coins. Please do. If you cant decide which to donate to, consider dividing it to all.
Any major contributor can add a donation link here.
Here are the current links.

* paypal donation link for crunchy gremlins contribution to BlackBox
* * https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=VAJQU42QYJY82&lc=US&item_name=Crunchy%20Gremlin&currency_code=USD&bn=PP%2dDonationsBF%3abtn_donateCC_LG%2egif%3aNonHosted 

* Patreon Link for McShooterz Contribution to BlackBox and StarTtrek:Shattered Alliance
* * http://www.patreon.com/McShooterz 

# Feature list #

* **General Game Things**
* * Larger Maps.
* * Many many crash fixes.
* * troop management improvements.
* * troops will autoland on planets you send them to. orbit a planet with a shift right click.
* * planets will auto resupply troop ships.
* * solar system info expansion will show special resource marker and player troop counts.
* * Large Access Aware exe to reduce out of memory issues.
* * Performance increases.
* * more multi threading.
* **AI ship count limite**r
* * allows the game to have more ships for the AI and adjust the ship performance load for your system.
* **AI improvements**
* * AI will chose techs by what it needs and will choose ship techs that it can use and give it the best ship.
* * AI will manage its troops better.
* * AI will use more diverse fleets.
* * AI can use carrier designs.
* * Fighters will return to carrier when out of ammo.
* * Fighters will more effectively return to carrier when warping.
* * Fleets will stay together when formation warping.
* * AI can build defensive platforms
* * ship sensors effect target picking radius
* * fighters use carrier sensor range
* * freighters can flee from combat
* **many mod only additions**
* * mods can run entirely from the mod directory.
* money based on population rather than production.
* Much better ship targeting code.
* Reduced volume when warping and launching troops.
* **More automation controls.**
* * Auto Taxes
* * Auto Technology
* **New Espionage screen**
* * more info about other empires
* * New spy code.

###I just want to play the mod Crunchy!###

* Great!
* The process is simply:
* extract the download into the stardrive program folder
* run stardrive from steam as you normally would.

##### Step by step installation instructions
* Set your game completely back to vanilla. IE if you installed a mod revert back so that vanilla runs.
* if you have never installed a mod you can skip the "How to revert to vanilla" part and go to the "your game should now be vanilla" part.
* How to revert to vanilla or Nuking from orbit... Its the only way to be sure. (warning: This requires basically downloading the game from steam again):
* go to the stardrive program folder. usually something like:
**Program Files(x86)\Steam\SteamApps\common\StarDrive**
* delete the "content" folder.
* go to steam.
* right click stardrive
* properties.
* find the option that lets you "verify content"
* do that.
* your game should be vanilla after it replaces the gigabyte of data in the content folder. 
* When installing normal mods do not overwrite content folder items with the mod files. mods content must exist all in the mod folder for it work right with blackbox. properly formatted it will. all the mods in the mod super pack are formatted this way.
* go to the download section and download the latest release version or the latest monthly version (preferred). Do not download the top option for the repository. You don't need that. Download the test version if your level of patience can take it.
* currently recommended to use the latest test build.
* The downloaded executable is a self extracting EXE that will extract to the default steam installation location. If you have the game in some other location you will need to adjust the extraction path.
* or by using 7zip the exe can be opened like a zip file and extract manually by the steps below.
* extract the zip into the the game directory usually located in Program Files (x86)\Steam\SteamApps\common\StarDrive
* Overwrite files when asked. If you do not get prompted to overwrite files. You are doing it wrong.
* To Uninstall follow the revert to vanilla instructions above.

#### Mod Super Pack ####
This is a blackbox download that includes these mods reformatted to work with black box:

* BlackBox : http://www.stardrivegame.com/forum/viewtopic.php?f=6&t=9910

* Techlevel : http://www.stardrivegame.com/forum/viewtopic.php?f=6&t=7091

* Tims : http://www.stardrivegame.com/forum/viewtopic.php?f=6&t=3163

* ComboMod : http://www.stardrivegame.com/forum/viewtopic.php?f=6&t=7735

* Overdrive : http://www.stardrivegame.com/forum/viewtopic.php?f=6&t=1617

* StarWars : http://www.stardrivegame.com/forum/viewtopic.php?f=6&t=9859

* Babylon 5 : http://www.stardrivegame.com/forum/viewtopic.php?f=6&t=9557

* The Vulpeculans : http://www.stardrivegame.com/forum/viewtopic.php?f=6&t=9849


##### Compatibility ####
* This mod should be compatible with normal content mods. 
* BlackBox is no longer compatible with DeveksMod. There is nt point in using it anyway. If there is something in deveks you are missing in BlackBox make an issue for it.
* THE SAVE GAMES FROM THIS MOD MAY NOT LOAD IN VANILLA SD.
* Vanilla SD saves will load in this mod but saving again may break compatibility with SD vanilla.

###Regular Mods###
* how to install...
* well... ideally the mod would support the mod enhancements in the BlackBox Code mode. Otherwise you should be able to take the mod files and put the them all in the mod folder and any folders that the mod tells you to put in the content folder... Dont. Put them int he mod folder too. If something goes wrong. Post the issue and we hopefully will figure out how to get that mod to work.
* Some modders have put together blackbox compatible downloads for their mods. here is a list. Well.. Its short...
* TechLevel: http://www.stardrivegame.com/forum/viewtopic.php?f=6&t=7091
* if the mod has a "starter ships" folder and crashes on load copy the contents of the starterships folder into the mods ship designs folder and delete the mods starterships folder.

### Yeah thats great but i want to add my own features or change some issue that bugs me###
* even better!!!
* Ill try to describe as best I can how to compile and contribute to this project.
* friend me on steam and request access to the steam SDCodemod forum.
* add your self to hipchat https://www.hipchat.com/invite/307681/f7fea0f83bdcd1c326e925dc9e3863a2

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