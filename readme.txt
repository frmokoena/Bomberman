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

To run the tests, you will need the `NUnit Framework`, and the steps are:

  1. Build the solution in Visual Studio to discover the tests.
  2. Copy the `states` folder from the project `root` folder to the `Debug` directory of the test project.
  3. I use `Test Explorer` in Visual Studio to run my tests.
   
  
### Running the application

  1. Download the latest release of the Game Engine from here [https://github.com/EntelectChallenge/2016-Bomberman/releases](https://github.com/EntelectChallenge/2016-Bomberman/releases) and extract it into a location of your choice.
  2. Navigate to the root directory of the Game Engine (`Game Engine/`), and open the `Run.bat` file.
  3. Replace one of the bot directories from the `Run.bat` file with the root directory of this bot.
  2. Open the Command Prompt and change to the root directory of the Game Engine.
  3. Execute the application by running the following command: `Run.bat`

## Strategy and project structure 

### Strategy

I use DIY hacks for my bot with A* search for path finding.

During each round, any one of the following actions (in order of importance) can happen:

 1. Stay clear of bombs
 2. Trigger the bomb if the timer it's not already 2.
 3. Chase nearby power ups based on thier importance.
 4. Plant bomb if we can score and find hiding block.
 5. Chase after power up
 6. Search for next bomb placement block
 7. Chase opponent if all walls have been destroyed
 8. Do nothing if we can't do anything fruitful.
		   
### Project Structure

The solution houses two projects. The application project (`BomberBot.csproj`) and the test project (`BomberBotTests.csproj`).


At the core of the application project is bot strategy interface (`IStrategy.cs`). All the move decisions are carried out by the bot strategy class (`Strategy.cs` which implements `IStrategy`). Bot helper class (`BotHelper.cs`) contains some of helper methods used by strategy class.


Reading of the state file and preparing of the game state is delegated to the game service (`IGameService.cs`).

