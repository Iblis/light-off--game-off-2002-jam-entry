# Light Off - Game Off 2022 Jam Entry

The theme for the Game Off 2022 is *cliché*, the cliché picked for this entry is

## Let's split up!
Probably the most reasonable thing to do, when the lights go off... ;)
...or is it?
Let's find out in this asymmetric multiplayer game based on [Luigi's Ghost Mansion](https://www.mariowiki.com/Luigi%27s_Ghost_Mansion)
One player takes the role of the ghost who tries to catch the other players who got split up in a spooky maze.
Of course, evading the ghost is not an easy feat, because the ghost is invisible when the lights go off!


## Open Source Software used in this project
In order of when they were added to the project

### Client (Unity)
* [hadashiA/VContainer](https://github.com/hadashiA/VContainer)
* [ashoulson/RailgunNet](https://github.com/ashoulson/RailgunNet), adapted for being used as an UPM package using the fork from [araex/RailgunNet](https://github.com/araex/RailgunNet)
* [Tjstretchalot/SharpMath2](https://github.com/Tjstretchalot/SharpMath2), adapted for being used as an UPM package
* [benjitrosch/spatial-hash](https://github.com/benjitrosch/spatial-hash) with small modifications (generic SpatialHash, find neighbours of Cell )
* [Iblis/UniTaskWebSocket](https://github.com/Iblis/UniTaskWebSocket) my own helper library to use WebSockets with async/await pattern
* [Cysharp/UniTask](https://github.com/Cysharp/UniTask)

### Server (Asp.Net 6)
* [Cysharp/LogicLooper/](https://github.com/Cysharp/LogicLooper)
* [AArnott/Nerdbank.Streams](https://github.com/AArnott/Nerdbank.Streams)