# Gambetta Networked Demo
Unity Demo showcasing networking concepts including prediction, interpolation and reconciliation in a networked (multiplayer) environment.

# Description
This is a minimal demo project made in Unity 2017.2.0f3 (but it should work in older versions as well). 
The demo project is a very close implementation of the ["Gambetta Demo" on Network Architecture](http://www.gabrielgambetta.com/client-server-game-architecture.html) - All credits to Gabriel Gambetta for that.

The demo is basically a `C#` Unity implementation of [Gambetta's Live Demo](http://www.gabrielgambetta.com/client-side-prediction-live-demo.html). 

Only this time it is actually networked, using [Lidgren's Network Library](https://github.com/lidgren/lidgren-network-gen3). All credits to Lidgren for his excellent network library!

# Instructions
1. Open up the scene "GambettaNetworkedDemoScene"
2. Hit 'Play' in Unity
3. Have a friend, build a Unity executable or use another computer to connect to you/the host by managing the settings on the "World" game object in the scene
4. Play around with settings on the "World" game object in the scene

# Local Demo
See [this repository](https://github.com/RamiAhmed/Gambetta_LocalDemo) for a local (same machine) demo version of this project.

License: MIT