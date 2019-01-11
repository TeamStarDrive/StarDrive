![banner_bb2.jpg](https://bitbucket.org/repo/4AAMq9/images/765859828-banner_bb2.jpg)

Notice: we have the stardrive devs publicly and privately stated approval for modifying the game for educational purposes but this software is still under the steam license restrictions. Do not use this for immoral or personal financial gain, donations requests are ok but can not be demanded or required. Do not attempt to circumvent game DRM. Be reasonably respectful of the dev and the original software and steam. 
please read the steam EULA and understand that game modding is a common concept. 
http://steamcommunity.com/app/252450/discussions/0/385428458177062745/#c365163686048069513
http://store.steampowered.com/eula/eula_39190

Feel free to drop in for questions, bug reports, requests and what not. 


[For general mod information, installation instructions, and download please go to the ModDB page.](http://www.moddb.com/mods/deveks-mod)


# Stardrive BlackBox#
This is the 15b version of the stardrive.exe decompiled from CIL into a C# project. The code decompilation errors preventing compilation were fixed and released as BlackBox gravity. Features were added much of the bugs were fixed and released as BlackBox RadicalElements. The current version in development is BlackBox Texas.


# Goals#
The current goal of BlackBox Texas is code refactoring and stability improvements.

* 5000 ships in game
* 3x speed at 20 fps minimum when zoomed out on galactic map without force full simulation on. 
* 1000 planets

Once Texas refactor has been complete, improvements to combat, smarter AI and a basic Storyline has been planned.


### How do I get set up? ###

* The game is developed with [VS 2017 Community](https://www.visualstudio.com/downloads/)
* If you're new to coding, please install [SourceTree](https://www.sourcetreeapp.com/) or some other Mercurial client. 
* [Clone](https://confluence.atlassian.com/sourcetreekb/clone-a-repository-into-sourcetree-780870050.html) this repository to a local directory, for example: C:/Projects/BlackBox
* Install Visual Studio 2017 Community.
* Copy the original StarDrive game files to repository root. For example: C:/Projects/BlackBox/StarDrive/StarDrive.exe
* Launch Visual Studio, any missing DLL references should be in BlackBox/StarDrive directory.
* Launch a full build (Build -> Build Solution) to produce the BlackBox StarDrive executable.

* Please install [JetBrains resharper](https://www.jetbrains.com/resharper/download/)
* Please join the [discord discussion](https://discord.gg/dfvnfH4).
* Please join the [BlackBox Google Group](https://groups.google.com/forum/#!forum/blackboxmod) for automated bug reports. 
* Please NOTE: if the **default** Release and Debug configurations *do not work* for you then your setup is incorrect. Contact us. 
* A list of new guy tasks can be found here [new guy tasks](https://bitbucket.org/CrunchyGremlin/sd-blackbox/issues?component=New+Guy+Tasks%3A+CleanUp&status=open&status=new) These are low priority low impact tasks. Where changes can be made without breaking the game and causing merge issues in most cases. 

### Contribution guidelines ###

* Utilize discord for chat discussions on ideas and refactoring.
* Use [BitBucket issues](https://bitbucket.org/CrunchyGremlin/sd-blackbox/issues/new) to propose new ideas. 
* Check [BitBucket Cards](http://www.bitbucketcards.com/CrunchyGremlin/sd-blackbox#)  for current projects and needs
* Code in the **guest branch** at first.
* When given the OK use HG flow to create Feature branches for your goal. 
* Comment your code so people can see what you are changing
* Prefix areas of change by \\Added by <your Alias> <whatever changes>
* Use clean code as much as possible.

### Who do I talk to? ###

* @CrunchyGremlin is the current owner of the repository, Gretman and RedFox can provide additional source guidance.
* If you have a bug report, post an issue.
* For other feature ideas, you can join our Discord chat and talk with the team!

### Development Cycle.
# For new features, refactors, old bug fixes  (feature)
* create a new branch based on develop.  
* when change is completed sanity test the changes.
* changes should not crash and should be functional in a basic sense.
* ceate a pull request back to develop.
* the pull request should indicate what areas need testing.
# if no release branch exists after pull is accepted to develop (release)
* create a new branch based off of develop named Release.
* Sanity test. should not crash and changed area should be functional.
* bug fixes should be pull requested back to develop.
* create a delopyment and upload to bitbucket.
* indicate in discord testing channel what needs to be looked at.
* if no new issues are found create a pull request to default and check the delete branch option.
# if bugs are found in the default branch (hot fix)
* create an issue or mark existing issue as a "Blocker" for current release.
* post the issue in the dev channel of discord. 
* dev will need to decide what to do.
* basically the process should be...
* create a hotfix branch based on default.
* create a  pull request back to develop and release.
