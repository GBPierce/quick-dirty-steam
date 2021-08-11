# QuickDirtySteam
[![A Fantastic Image of A Fast, Dirty Fatory](https://eapi.pcloud.com/getpubthumb?code=XZ8VM0Z7yYojx4SpIp4n1fssfeUM8AOUrAV&linkpassword=undefined&size=320x320&crop=0&type=auto)](https://eapi.pcloud.com/getpubthumb?code=XZ8VM0Z7yYojx4SpIp4n1fssfeUM8AOUrAV&linkpassword=undefined&size=320x320&crop=0&type=auto)

## Description:
QuickDirtySteam is a collection of classes I created in an effort to learn more about the ins and outs of the Steamworks API (particularly its P2P multiplayer functionality) as well as to create some boilerplate code I can reuse to quickly create multiplayer game prototypes. 

## Workflow:
I tried to condense it down to as little functions as possible and ended up with a workflow roughly like this:
1. Initialize the API using **SteamManager.Initialize()** and call **SteamManager.RunCallbacks()** once per iteration
2. Inherit the **Server Class** and hook into its event as seen fit
3. Call **Lobby.Create()** on the host's end, set some lobby meta data by calling **Lobby.SetMetaDataPair()** and make the lobby public by calling **Lobby.Open()**
4. Call **Lobby.Enter()** on the client's End
5. Call **Server.Start()** and **Lobby.SetServerActive()** on the host's end
6. Have both the client and the host connect to the server by calling **Client.Connect()**
7. Data packets can now be exchanged by **Calling Client.SendMessage()**

## Dependencies:
This project depends on Steamworks.NET

## Remarks:
I am still at the very beginning of my journey in teaching myself about both programming multiplayer games and the Steamworks API in general.
Therefore, I would hardly be suprised if a great deal of this code is just plain wrong or missing important bits and pieces.
Constructive criticism is always highly appreciated.

#### Credits to my Wife for Creating this absolutely wonderful piece of Pixel Art
