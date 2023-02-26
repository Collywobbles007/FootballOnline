# FootballOnline
An open source project to develop an online football game similar in style to the discontinued Football Superstars and Striker Superstars games.

Footballl Superstars and Stiker Superstars were unique online football games played in 3rd person mode. Unlike games such as the FIFA Football and Pro Evolution Soccer, the player only controls their own player - all other players are controlled by other people or AI. The goalkeepers are always AI controlled.

This project is developed in Unity (last tested with version 2021.3.19f1) and uses Photon Fusion to provide online gaming functionality. A good knowledge of both of these will be required to enable you to make use of this project.

The project can be run in either Server or Client mode. This is determined at runtime based on parameters provided on execution. A sample .bat file for running in Server mode is provided.

Please note that this is a difficult project to set up and I can only provide basic details here, so you really need to know what you're doing with Unity and Photon Fusion if you want to get this working...

Unity
=====
1. Create a new Unity Core 3D project
2. Add Input System from Package Manager

Photon Fusion
=============
1. Make sure 'Allow unsafe code' is enabled in Unity Player Setttings or you will get compilation errors when importing Photon Fusion
2. Download Photon Fusion SDK (you will need to set up a free account first)
3. Import the downloaded Photon Fusion Unity package Assets -> Import Package -> Custom Package
4. Create a Fusion App ID on your Dashboard and add it to the Fusion App Id entry box
5. Download the Kinematic Character Controller (KCC) Fusion addon (https://doc.photonengine.com/fusion/current/addons/advanced-kcc/overview)
6. Import the KCC addon package Assets -> Import Package -> Custom Package

Follow the instructions at https://doc.photonengine.com/fusion/current/fusion-100/fusion-101 if you need further details on adding Photon Fusion to your project.

Unity
=====
1. Copy the Assets & Project Settings folders from your GitHub download into your new Unity project
4. Create a Builds folder for your project
5. Open Build Settings and make sure the scenes '0.Launch', 'Lobby' & '2.Game' are in the Build (must be in that order) 
6. Build your project
7. Copy the StartServer.bat file from your GitHub download into your Builds folder
8. Edit the StartServer.bat file to match the name of your build .exe

Running the Game
================
1. Run StartServer.bat - this starts the server which runs in the background so you won't see anything. It will appear in Task manager as the name of your exe
2. Make sure you have the Unity scene 0.Launch open and hit play. This will start the client
3. When you first run it will ask for a player name, it will remember it so it won't ask again on future runs. If you get a missing camera error, just add one to the 0.Launch scene and restart it.
4. To test out multiplayer start one client from the built exe and one from the Unity client (or send a copy of the build to a friend!). Don't click the Ready button until both players are in the room (this is one of the many things that needs to be worked on!)

Note that the server will stop once the last player leaves unless it crashes. In which case, you will need to stop the server manually using Windows Task Manager. If you are unsure if the server has stopped, always check Task Manager as running it twice will cause issues.


Current functionality:

- Online multiplayer support with simple queueing system
- Third person control system using WASD and mouse (mouse look with mouse button 2)
- Dribbling and mouse click to pass with both 1-click and press and hold for powerbar
- Basic range-based tackling
- Some goalkeeper AI including some saves (far from complete)
- Targeting system when aiming at goal, also with 1-click and powerbar
- Ball height is controlled by mouse pitch - low angle will ground pass higher pitch will kick high
- Targeted pass to another player using spacebar, including target movement prediction
- Basic pre-load for passing and shooting
- Futsal style pitch with goals
- Play area boundaries are defined - ball will return to the centre circle after entering the goal or leaving the pitch

Known Issues:
- The screen for entering your name is a bit buggy
- Ball sometimes disappears when shooting close to the keeper

You can see some videos of different versions of the game as it was developed in the development blog: https://collywobbles.net/category/futsal-game/

Some of the older vidoes contain some extra paid-for visual assets (mostly Polygon assets) which have been removed from the version here on GitHub. They are just scenery and so they do not affect the actual gameplay functionality. The latest video has the scenery removed and is created from the project stored here on GitHub.

I am not currently actively working on this project as I don't currently have the time or inclination. Therefore, I am providing it as-is under the GNU Public Licence v3.0 (read full licence for details) so that anyone can work on it to try and develop it further.
