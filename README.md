![banner_bb2.jpg](https://bitbucket.org/repo/4AAMq9/images/765859828-banner_bb2.jpg)
[![Build status](https://ci.appveyor.com/api/projects/status/io0yiuypjam8m331?svg=true)](https://ci.appveyor.com/project/RedFox20/stardrive-blackbox)

# Stardrive BlackBox #
This is the 15b version of StarDrive.exe decompiled from CIL into a C# project.
The code decompilation errors preventing compilation were fixed and released as BlackBox gravity.
New features were added, much of the bugs were fixed and released as BlackBox RadicalElements.
The current release is BlackBox - Venus and upcoming version is BlackBox - Hyperion.

Notice: We have StarDrive developer's publicly and privately stated approval for modifying the game for educational purposes but this software is still under the steam license restrictions.
Do not use this for immoral or personal financial gain, donation requests are ok but can not be demanded or required.
Do not attempt to circumvent game DRM. Be reasonably respectful of the dev and the original software and steam.

Please read the steam EULA and understand that game modding is a common concept.
http://steamcommunity.com/app/252450/discussions/0/385428458177062745/#c365163686048069513
http://store.steampowered.com/eula/eula_39190

Feel free to drop in for questions, bug reports, requests and what not. 

[For general mod information, installation instructions, and download please go to the ModDB page.](http://www.moddb.com/mods/deveks-mod)

# BlackBox - Hyperion #
The current goals of BlackBox - Hyperion are:

* More Performance Improvements
* Gas Giant mining operations
* Tradable resources
* Racial planet preferences
* New mesh, texture and shader loading system
* More stability improvements

### How do I get set up for Development? ###

* Install [Visual Studio 2019 Community](https://visualstudio.microsoft.com/vs/community/).
    * Workloads Module: .NET desktop development
    * Workloads Module: Desktop development with C++
    * Workloads Module: Game development with C++
* Install [SourceTree](https://www.sourcetreeapp.com/) or some other GIT client. 
    * Configure SourceTree: Tools->Options->Git: [v] Perform submodule actions recursively
* [Clone](https://confluence.atlassian.com/sourcetreekb/clone-a-repository-into-sourcetree-780870050.html) this repository to a local directory, for example: C:/Projects/BlackBox
    * Advanced Options When cloning: [v] Recurse submodules (Important!)
    * Advanced Options When cloning: Checkout branch: develop  Clone Depth 0
* Switch to DEVELOP branch, which is our main branch for latest ongoing development.
* Copy the original StarDrive game folder to repository root. For example: C:/Projects/BlackBox/StarDrive/StarDrive.exe
* Launch Visual Studio, any missing DLL references should be in BlackBox/StarDrive directory.
* Launch a full build (Build -> Build Solution) to produce the BlackBox StarDrive executable.
    * If you get this build error: "Windows 10 SDK is not installed", then you need to go back to VS2019 installer and enable Desktop development with C++
    * If you get this build error: ".. Cannot open include file: 'corecrt.h': No such file or directory ..", then you are also missing Desktop development with C++

* Install [JetBrains ReSharper](https://www.jetbrains.com/resharper/download/) to enjoy enhanced refactoring capabilities.
* Please join the [discord discussion](https://discord.gg/dfvnfH4).
* Please join the [BlackBox Google Group](https://groups.google.com/forum/#!forum/blackboxmod) for automated bug reports. 
* Please NOTE: if the **default** Release and Debug configurations *do not work* for you then your setup is incorrect. Contact us in Discord #general. 
* A list of new guy tasks can be found here [new guy tasks](https://bitbucket.org/CrunchyGremlin/sd-blackbox/issues?component=New+Guy+Tasks%3A+CleanUp&status=open&status=new) These are low priority low impact tasks. Where changes can be made without breaking the game and causing merge issues in most cases. 

### Contribution Guidelines ###

* Utilize Discord for chat discussions on ideas and refactoring.
* Use [BitBucket issues](https://bitbucket.org/CrunchyGremlin/sd-blackbox/issues/new) to propose new ideas. 
* Check [BitBucket Cards](http://www.bitbucketcards.com/CrunchyGremlin/sd-blackbox#) for current projects and needs
* Code in the **guest branch** at first.
* When given the OK use GIT flow to create Feature branches for your goal. 
* Comment your code so people can see what you are changing
* Use clean code as much as possible.

### Who do I talk to? ###

* In Discord: @CrunchyGremlin is the current owner of the repository, @RedFox and @Fat_Bastard can provide additional source guidance.
* If you have a bug report, post an issue or post a bug in our Discord channel.
* For other feature ideas, you can join our Discord chat and talk with the team!

### Development Cycle.
# For new features, refactors, old bug fixes  (feature) #
* Create a new branch from develop.
* When change is completed sanity test the changes.
* For new utilities, always add NEW unit tests.
* Changes should not crash and should be functional in a basic sense.
* Create a pull request and wait for review. Be ready to make a few tweaks! It is easy to make a bug in this legacy codebase.
* The pull request review should indicate what areas need further testing.
# If no release branch exists after pull is accepted to develop (release)#
* Create a new branch based off of develop named Release.
* Sanity test. should not crash and changed area should be functional.
* Bug fixes should be pull requested back to develop.
* Create a delopyment and upload to bitbucket.
* Indicate in discord testing channel what needs to be looked at.
* If no new issues are found create a pull request to default and check the delete branch option.
# If bugs are found in the default branch (hot fix) #
* Create an issue or mark existing issue as a "Blocker" for current release.
* Post the issue in the dev channel of discord. 
* Create a hotfix branch based on default.
* Create a pull request back to develop and release.
* :p