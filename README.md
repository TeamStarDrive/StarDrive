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

* Install [Visual Studio 2019/2022 Community](https://visualstudio.microsoft.com/vs/community/).
    * Workloads Module: `.NET desktop development` with `.NET Framework 4.8 development tools`
    * Workloads Module: `Desktop development with C++` with `MSVC v142`
    * Workloads Module: `Game development with C++` with `Windows 10 SDK`
* Install [SourceTree](https://www.sourcetreeapp.com/) or some other GIT client. 
    * Configure SourceTree: Tools->Options->Git: [v] Perform submodule actions recursively _(Important!!!)_
    * Configure SourceTree: Tools->Options->Git: [v] Enable the Bitbucket LFS Media Adapter _(Important!!!)_
* [Clone](https://confluence.atlassian.com/sourcetreekb/clone-a-repository-into-sourcetree-780870050.html) this repository to a local directory, for example: C:/Projects/BlackBox
    * Advanced Options When cloning: [v] Recurse submodules _(Important!!!)_
    * Advanced Options When cloning: Checkout branch: develop  Clone Depth 0
* Switch to `develop` branch, which is our main branch for latest ongoing development.
* Launch Visual Studio, any required DLL references should be in `BlackBox/game` directory.
* Launch a full build (Build -> Build Solution) in `Release` configuration to produce the BlackBox StarDrive executable.
    * If you get this build error: "Windows 10 SDK is not installed", then you need to go back to Visual Studio installer and enable Desktop development with C++
    * If you get this build error: ".. Cannot open include file: 'corecrt.h': No such file or directory ..", then you are also missing Desktop development with C++

* Install [JetBrains ReSharper](https://www.jetbrains.com/resharper/download/) to enjoy enhanced refactoring capabilities.
* Please join the [discord discussion](https://discord.gg/dfvnfH4).
* Please join the [BlackBox Google Group](https://groups.google.com/forum/#!forum/blackboxmod) for automated bug reports. 
* Please NOTE: if the **default** Release and Debug configurations *do not work* for you then your setup is incorrect. Contact us in Discord #general. 

### Contribution Guidelines ###

* Utilize Discord for chat discussions on ideas and refactoring.
* Use [BitBucket issues](https://bitbucket.org/CrunchyGremlin/sd-blackbox/issues/new) to propose new ideas. 
* Check [BitBucket Cards](http://www.bitbucketcards.com/CrunchyGremlin/sd-blackbox#) for current projects and needs
* Creating feature branches is always allowed and Pull Requests will be reviewed by the team.
* Comment your code so people can see what you are changing.
* Write clean and easy to understand code.

### Who do I talk to? ###

* In Discord: @RedFox and @Fat_Bastard can provide guidance of this codebase.
* If you have a bug report, post an issue or post a bug in our Discord channel.
* For other feature ideas, you can join our Discord chat and talk with the team!

### Development Cycle.
# For new features, refactors, old bug fixes  (feature) #
* Create a new branch from develop.
* Always add NEW feature unit tests and playtest your changes.
* Create a pull request and wait for review. Be ready to make a few tweaks! It is easy to create unintentional bugs in this legacy codebase.
# If bugs are found in develop branch (hot fix) #
* Create an issue or mark existing issue as a "Blocker" for current release.
* Post the issue in the dev channel of discord. 
* If you can quickly fix it, help us by creating a hotfix pull request.

### Command Line Arguments ###
BlackBox provides a CLI for running certain utilities from Command Prompt
```
C:\Projects\BlackBox\game>StarDrive.exe --help
13:50:43.698ms: Loaded App Settings
13:50:43.768ms:
 ======================================================
 ==== Mars : 1.30.13000 develop-latest             ====
 ==== UTC: 12/13/2021 13:50:43                     ====
 ======================================================

13:50:43.769ms: StarDrive BlackBox Command Line Interface (CLI)
13:50:43.769ms:   --help             Shows this help message
13:50:43.769ms:   --mod="<mod>"    Load the game with the specified <mod>, eg: --mod="Combined Arms"
13:50:43.769ms:   --export-textures  Exports all texture files as PNG and DDS to game/ExportedTextures
13:50:43.769ms:   --export-meshes=obj Exports all mesh files and textures, options: fbx obj fbx+obj
13:50:43.769ms:   --generate-hulls   Generates new .hull files from old XML hulls
13:50:43.769ms:   --generate-ships   Generates new ship .design files from old XML ships
13:50:43.769ms:   --fix-roles        Fixes Role and Category for all .design ships
13:50:43.769ms:   --run-localizer=[0-2] Run localization tool to merge missing translations and generate id-s
13:50:43.769ms:                         0: disabled  1: generate with YAML NameIds  2: generate with C# NameIds
13:50:43.769ms:   --continue         After running CLI tasks, continue to game as normal
13:50:43.769ms: The game exited normally.
13:50:43.769ms: RunCleanupAndExit(0)
```

To convert all legacy XNB textures, you can run `--export-textures`
```
C:\Projects\BlackBox\game>StarDrive.exe --export-textures
```
