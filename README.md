# FootballOnline
An open source project to develop an online football game similar in style to the discontinued Football Superstars and Striker Superstars games.

Footballl Superstars and Stiker Superstars were unique online football games played in 3rd person mode. Unlike games such as the FIFA Football and Pro Evolution Soccer, the player only controls their own player - all other players are controlled by other people or AI. The goalkeepers are always AI controlled.

This project is developed in Unity and uses Photon Fusion to provide online gaming functionality. A good knowledge of both of these will be required to enable you to make use of this project.

The project can be run in either Server or Client mode. This is determined at runtime based on parameters provided on execution. A sample .bat file for running in Server mode is provided.

Please note that this is a difficult project to set up and I can only provide minimal details here, so you really need to know what you're doing with Unity and Photon Fusion if you want to get this working...

Basic Installation Instructions:

- Clone the project from GitHub
- Install Photon Fusion and generate an App Id (visit Photon Fusion official website for details)
- Install Photon Fusion KCC Addon (required for online player movement)

Extra Unity Packages:

- Install Unity TextMeshPro package


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

You can see some videos of the game in action here: https://collywobbles.net/category/futsal-game/
Some of the older vidoes contain some extra paid-for visual assets which have been removed from the version here on GitHub. They are just scenery and so they do not affect the actual gameplay functionality. The latest video has the scenery removed and is created from the project stored here on GitHub.

I am not currently actively working on this project as I don't currently have the time or inclination. Therefore, I am providing it as is under the GNU Public Licence v3.0 (read full licence for details) so that anyone can work on it to try and develop it further.
