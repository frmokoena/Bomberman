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

#### Method I: Command Line

  1. First, ensure that `MSBuild` is installed on your system, and the path to `MSbuild` is set in `Environment Variables`. If the path is not set, follow the steps in this [SO answer](http://stackoverflow.com/a/12608705/852243).
  2. Then a package restore is needed before a build can be made. A standalone `nuget.exe` can be found here [here](http://docs.nuget.org/consume/Command-Line-Reference#Restore-command).
  3. The standalone `nuget.exe` can be stored at the root directory of the solution.
  4. Start the Command Prompt.
  5. Change to the root directory of the solution (i.e. where `Bomberman.sln` is located).
  6. Perform the package restore by running this commonad: `nuget.exe restore Bomberman.sln`
  7. Build the solution by running this command: `msbuild Bomberman.sln /p:Configuration=Release /p:Platform="Any CPU" /t:Clean,Build`

#### Method II: Visual Studio

   1. Open the solution (`Bomberman.sln`) in Visual Studio and select `Build -> Build Solution` from the menus.

### Tests

I have written a number of automated tests (`BomberBotTests.csproj`) to ensure that the solution works as expected. Test data consititutes diferent state files picked from replay files of different matches.

To run the tests, I use `NUnit 3 Test Adapter` nuget package, and the steps are:

  1. Build the solution in Visual Studio to discover available tests.
  2. Copy the `testData` folder from the project `root` folder to the `Debug` directory of the test project.
  3. I use `Test Explorer` in Visual Studio to view and run my tests.
  4. Click `Run All` in the Test Explorer.   
  
### Running the application

  1. Download the latest release of the Game Engine from here [https://github.com/EntelectChallenge/2016-Bomberman/releases](https://github.com/EntelectChallenge/2016-Bomberman/releases) and extract it into a location of your choice.
  2. Navigate to the root directory of the Game Engine (`Game Engine`), and open the `Run.bat` file.
  3. Replace one of the bot directories from the `Run.bat` file with the root directory of this bot.
  2. Open the Command Prompt and change to the root directory of the Game Engine.
  3. Execute the application by running the following command: `Run.bat`

## Strategy and project structure 

### Strategy

I use DIY hacks for my bot with A* search for path finding.

Due to time limit (2 seconds), I use a state machine approach. During each round, any one of the following actions can happen:

 1. Stay clear of bombs
 2. Plant bomb if we have any in our bomb bag, and can score points (or block an opponent) and find hiding block
 3. Block an opponent if we can destroy her
 4. Chase power up
 5. Search for next bomb placement block
 6. Chase opponent if all walls have been destroyed, and we are losing on points.
 7. Expore the map if we haven't visited every block
 8. Do nothing if we can't do anything fruitful.
		   
### Project Structure

The solution houses two projects. The application project (`BomberBot.csproj`) and the test project (`BomberBotTests.csproj`).

At the core of the application project is bot strategy (`IStrategy.cs` implemented by `Strategy.cs`), which makes move decisions. Bot helper class (`BotHelper.cs`) houses helper methods used by strategy class.

Reading of the state file and preparing of the game state is delegated to the game service (`IGameService.cs` implemented by `GameService.cs`).