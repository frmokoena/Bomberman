# 2016 Entelect AI Challenge - Bomberman

## Introduction

This is my entry to the 2016 Entelect AI Challenge. The challenge details can be found here [http://challenge.entelect.co.za/](http://challenge.entelect.co.za/).

## Tools and technologies used

**Language:** C#

**Editor:** Visual Studio Community 2015

**Unit-testing Framework:** NUnit Framework 

## Building and running the solution

### Building the solution

Either of the following two methods will do the job.

#### Method I

  1. First, ensure that `MSBuild` is installed on your system, and the path to `MSbuild` is set in Environment Variables. If not follow the steps in [SO answer](http://stackoverflow.com/a/12608705/852243).
  2. Then a package restore is needed before a build can be made. A standalone `nuget.exe` can be found here [here](http://docs.nuget.org/consume/Command-Line-Reference#Restore-command).
  3. The standalone `nuget.exe` can be stored at the root directory of the solution.
  4. Start the Command Prompt.
  5. Change to the root directory of the solution (i.e. where `Bomberman.sln` is).
  6. Run package restore as explained [here](http://docs.nuget.org/consume/package-restore#command-line-package-restore)
  7. Run this command: `msbuild Bomberman.sln /p:Configuration=Debug /p:Platform="Any CPU" /t:Clean,Build`

#### Method II

   1. Open the solution (`Bomberman.sln`) in Visual Studio and select `Build -> Build Solution` from the menus.

### Tests

I have written a number of automated tests (`BomberBotTests.csproj`) to ensure that the solution works as expected. Test data consititutes diferent state files picked from replay files of different matches.

To run the tests, you will need the `NUnit Framework`, and the steps are:

  1. Build the solution in Visual Studio to discover the tests.
  2. Copy the `Sample Data Files` folder from the project `root` folder to the `bin` directory of the test project.
  3. I use `Test Explorer` in Visual Studio to run my tests.
   
  
### Running the application

  1. Download the latest release of the Test Harness from here [https://github.com/EntelectChallenge/2016-Bomberman/releases](https://github.com/EntelectChallenge/2016-Bomberman/releases) and extract it into a location of your choice.
  2. Navigate to the root directory of the Game Engine, and replace one of the bot directories with this bot directory.
  2. Open the Command Prompt and change to the root directory of the Game Engine.
  3. Execute the application by running the following command: `Run.bat`

## Strategy and project structure 

### Strategy

I use lot of DIY hacks for my bot with A* strategy for path finding.

Update procedure:

 1. Stay clear of bombs
 2. Trigger the bomb
 3. Chase power up if near than 3 blocks
 4. Plant bomb
 5. chase afetr power up
 6. Search for next bomb placement spot
		   
### Project Structure

The solution houses two projects. The application project(`BomberBot.csproj`) and the test project (`BomberBotTests.csproj`).

At the core of the application project is bot (`Bot.cs`) and bot helper (`BotHelper.cs`). Reading of the state file and preparing of the game state is delegated to the game service (IGameService.cs).

All the bot decision is carried out by the bot (`Bot.cs`). Bot helper contains some of helper methods used in decision making.