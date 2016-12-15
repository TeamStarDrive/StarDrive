Notice: we have the stardrive devs publicly and privately stated approval for modifying the game for educational purposes but this software is still under the steam license restrictions. Do not use this for immoral or personal financial gain, donations requests are ok but can not be demanded or required. Do not attempt to circumvent game DRM. Be reasonably respectful of the dev and the original software and steam. 
please read the steam EULA and understand that game modding is a common concept. 
http://steamcommunity.com/app/252450/discussions/0/385428458177062745/#c365163686048069513
http://store.steampowered.com/eula/eula_39190

Discord chat discussion available here:
https://discord.gg/nrMKaWr
feel free to drop in for questions, bug reports, requests and what not. 


[For general mod information, installation instructions, and download please go to the moddb page.](http://www.moddb.com/mods/deveks-mod)


# Stardrive BlackBox#
This is the 15b version of the stardrive.exe decompiled from CIL into a c# project. The code decompilation errors preventing compilation were fixed and released as BlackBox gravity. Features were added much of the bugs were fixed and released as BlackBox RadicalElements. The current version in development is BlackBox Texas.


# Goals#
The current goals of the texas version is code clean up and refactoring.

* 5000k ships in game
* 3x speed at 20 fps minimum when zoomed out on galactic map without force full simulation on. 
* 1000 planets

### How do I get set up? ###
This is a mercurial repository

* It is currently made with [VS 2017 Community](https://www.visualstudio.com/downloads/)

* If you're new to coding, download SourceTree and clone this repository to your local drive.
* Install Visual Studio 2017 community. 
* Copy the game installation to the repository root. The code versioning should ignore the folder. 
* Set the output directory to the stardrive directory in the repository.
* Any missing references should be in the stardrive directory.


### Contribution guidelines ###

* Code in your branch. Make sure SD compiles and loads.
* Comment your code so people can see what you are changing
* Prefix areas of change by \\Added by <your Alias> <whatever changes>
* use clean code as much as possible.
* jetBrains resharper currently recommended. 

### Who do I talk to? ###

* Crunchy gremlin is the current owner of the repositor. Gretman has admin access.
* Any other user or post an issue.