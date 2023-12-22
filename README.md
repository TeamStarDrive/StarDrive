![banner](https://repository-images.githubusercontent.com/576058391/90061a19-c54d-447e-95cd-e633f4ec8146)

[![Build status](https://ci.appveyor.com/api/projects/status/6fjcqtj2w9kuga3f?svg=true)](https://ci.appveyor.com/project/RedFox20/stardrive)

# Stardrive BlackBox
This is the 15b version of StarDrive.exe decompiled from CIL and almost completely rewritten by BlackBox - Mars.
The current release is BlackBox - Mars and upcoming version is BlackBox - Hyperion.

Notice: We have StarDrive developer's [publicly and privately stated approval](http://steamcommunity.com/app/252450/discussions/0/385428458177062745/#c365163686048069513) for modifying the game for educational purposes but this software is still under the steam license restrictions.
Do not use this for immoral or personal financial gain, donation requests are ok but can not be demanded or required.
Do not attempt to circumvent game DRM. Be reasonably respectful of the dev and the original software and steam.

# Downloads
Currently downloadable Releases contain Major and Patch versions. Major versions are big, 700-800MB installs. Patches are relatively small and are always cumulative, which means you only need to install the Major version (1.41) and latest patch (1.41-patch).
Note that the game now has an auto update logic; You can install the Major Release and the game will prompt and install the latest patch for you. Hover over the prompt message to see change log.

[Current Major Release Link (read the instructions!)](https://github.com/TeamStarDrive/StarDrive/releases/tag/mars-1.41.14664)

[Go To Downloads](https://github.com/TeamStarDrive/StarDrive/releases) (for reference)

# Mods
The only mods which currently supports Blackbox are 
* [Combined Arms] (https://github.com/TeamStarDrive/CombinedArms), which is a huge content mod. 
* [Star Trek] (https://github.com/TeamStarDrive/StarTrekShatteredAlliance), which uses Vanilla races with addtion of Star Trek races and ships.

# Community
Feel free to drop in for questions, bug reports, requests and what not. 

* [Discord Discussion](https://discord.gg/dfvnfH4)
* [Patreon](https://www.patreon.com/stardriveblackbox)
* [GitHub Issues](https://github.com/TeamStarDrive/StarDrive/issues) for reporting all types of bugs
* [For information on older versions, visit the ModDB page](http://www.moddb.com/mods/deveks-mod)

# BlackBox - Mars
What we achieved in Mars so far:
* Huge performance improvements
* Huge stability improvements - especially got rid of most OutOfMemory errors
* Racial planet preferences
* Research Stations
* Mining Ops
* Multi Level Research for bonuses/upgrades
* New mesh, texture and shader loading system
* Auto Update for Blackbox versions and mods.

# BlackBox - Hyperion
The future goals of BlackBox - Hyperion are:
* New graphics engine - XNA is extremely limiting
* 64-bit engine - will remove a lot of memory limitations
* Upgraded graphics - a sort of next-gen update would be great
* Steam release - release a remastered version on Steam to reach more players

# How do I get set up for Development?

* Install [Visual Studio 2019/2022 Community](https://visualstudio.microsoft.com/vs/community/).
    * Workloads Module: `.NET desktop development` with `.NET Framework 4.8 development tools`
    * Workloads Module: `Desktop development with C++` with `MSVC v143`
    * Workloads Module: `Game development with C++` with `Windows 10 SDK`
* Install [SourceTree](https://www.sourcetreeapp.com/) or some other GIT client. 
    * Configure SourceTree: Tools->Options->Git: [v] Perform submodule actions recursively _(Important!!!)_
* [Clone](https://confluence.atlassian.com/sourcetreekb/clone-a-repository-into-sourcetree-780870050.html) this repository to a local directory, for example: C:/Projects/BlackBox
    * Advanced Options When cloning: [v] Recurse submodules _(Important!!!)_
* Switch to `develop` branch, which is our main branch for latest ongoing development.
* Launch Visual Studio, any required DLL references should be in `BlackBox/game` directory.
* Launch a full build (Build -> Build Solution) in `Release` configuration to produce the BlackBox StarDrive executable.
    * If you get this build error: "Windows 10 SDK is not installed", then you need to go back to Visual Studio installer and enable Desktop development with C++
    * If you get this build error: ".. Cannot open include file: 'corecrt.h': No such file or directory ..", then you are also missing Desktop development with C++

* Install [JetBrains ReSharper](https://www.jetbrains.com/resharper/download/) to enjoy enhanced refactoring capabilities.
* Please NOTE: if the **default** Release and Debug configurations *do not work* for you then your setup is incorrect. Contact us in Discord #general-discussion. 

### Contribution Guidelines

* Utilize Discord for chat discussions on ideas and refactoring.
* Use [GitHub Issues](https://github.com/TeamStarDrive/StarDrive/issues) to propose new ideas. 
* Creating feature branches is always allowed and Pull Requests will be reviewed by the team.
* Comment your code so people can see what you are changing. Non-documented code will not pass review.
* Write clean code, following current best software practices. #DRY #CleanCode

### Who do I talk to?

* In Discord: @RedFox and @Fat_Bastard can provide guidance of this codebase.
* If you have a bug report, post an issue or post a bug in our Discord channel.
* For other feature ideas, you can join our Discord chat and talk with the team!

# Modding
BlackBox - Mars and later have greatly improved modding capabilities and features, 
contact us in [Discord](https://discord.gg/dfvnfH4) for more information on modding.

## What is moddable?
* Globals.yaml provides access to all global game settings, more detailed than previously available.
* All textures can be replaced, PNG and DDS are supported. The old XNB textures are no longer recommended.
* All meshes can be replaced, we support OBJ and FBX meshes.
* Audio can be modded
* Custom stars
* Custom planets
* Some UI layouts can be modded, mostly MainMenu for now
* All YAML files can be modded and are hotloaded while the game is running, so you can do interactive tweaking
* Feel free to ask for more details in [Discord](https://discord.gg/dfvnfH4) 

# Development Cycle
## For new features, refactors, old bug fixes  (feature)
* Create a new branch from develop.
* Always add NEW feature unit tests and playtest your changes.
* Create a pull request and wait for review. Be ready to make a few tweaks! It is easy to create unintentional bugs in this legacy codebase.
## If bugs are found in develop branch (hotfix)
* Create an issue or mark existing issue as a "Blocker" for current release.
* Post the issue in the dev channel of discord. 
* If you can quickly fix it, help us by creating a hotfix pull request.

# Command Line Arguments
BlackBox provides a CLI for running certain utilities from Command Prompt.
Many of these are developer oriented and not very useful for regular users.
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
13:50:43.769ms:   --resource-debug   Debug logs all resource loading, mainly for Mods to ensure their assets are loaded
13:50:43.769ms:   --asset-debug      Debug logs all asset load events, useful for analyzing the order of assets being loaded
13:50:43.769ms:   --console          Enable the Debug Console which mirrors blackbox.log
13:50:43.769ms:   --continue         After running CLI tasks, continue to game as normal
13:50:43.769ms: The game exited normally.
13:50:43.769ms: RunCleanupAndExit(0)
```

To convert all legacy XNB textures, you can run `--export-textures`
```
C:\Projects\BlackBox\game>StarDrive.exe --export-textures
```
