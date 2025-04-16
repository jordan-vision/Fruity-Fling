This zip file contains all of the files necessary for the execution of the submission. To run the project, unzip the files, and open the root folder within Unity version 2022.3.26f1 or later. The Assets folder contains the most important files.

-In _Scenes, there are three Unity scenes that are playable individually, and connect with each other through UI.
	The game is intended to start on the MainMenu scene, from where it can go to Level1 or Level2.

- In _Scripts, there is the C# code used by the game objects. 
	LevelGrid.cs contains most of the game logic.
	LevelUI.cs contains most of the interface programming.
	GameManager.cs is a singleton class that allows all objects in the scene to have access to each other without directly referencing each other