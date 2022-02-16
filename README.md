# Alphatorms
Alphastorms is a C# .NET 6.0 cross-platform MMO engine with a Monogame test client. 

## About the Author
As a former developer on MMO servers (Ultima Online shards, Shadowrun MMO [cancelled]), I've had a lifelong passion for creating online games and scaleable simulations. Between my dayjob as a Cloud Engineer and side job as a engineer for a massively parallel trading platform, I find that I'm always trying to force distributed and parallel computing into my projects. Alphastorms is my attempt to channel that energy into something that might be useful for the game dev commnunity. The engine is intended to be used to power a 2D front-end (which will be provided) powered by Monogame or any C# capable platform. In theory, a 3D client could be created but, I'm not a 3D dev so that's not my area of focus at the moment.

## Goal
The goal is to deliver a self-contained MMO engine with client and server powered by .NET 6.0 and easily run on any platform. Each release will contain the server, the client, any tools, data generation scripts for the datastore and any open-source or free-to-use-and-distribute assets used in the Client. 

It should ultimately be as simple as "dotnet alphastorms.server.dll" to start the server and "dotnet alphastorms.client.dll" to start the client. 

Below is a bit of free-flowing design and thinking.


# Alphastorms Components
## Alphastorms Downloader
The client side download tool used to download files and executables from the download server

## Alphastorms Website
This is the landing page for the game as well as where the downloader targets to authenticate and download the latest game version

## Alphastorms Engine
This is the game client which is downloaded via the downloader and allows connection to the game server

## Alphastorms Game Server
This is the backend game server which processes commands from the game engine using a combination of TCP and UDP. 

## Alphastorms Manifest Generator
This is a tool that generates a manifest based on the latest game client which includes zipping large files, zipping assets (or asset collections), generating thumbprints

## Alphastorms.Utility
This is a library for basic utilities that can be shared and are general enough that they can be shared across all the various tools.
This is NOT for models or client/server shared code/etc. It is only for basic OS utilities and primitives. 



# Thoughts and Planning 
* The downloader will require credentials in order to "login" to it. That login delivers a secure token good for a slice of time (perhaps 4 hours)
* The downloader will update the game client and then pass it that token when launching it in order to provide authentication
    - The downloader will pass 2 tokens. 
    - The first token is a downloader to client validation token, this is a simple security measure to prevent the client from launching directly. The client can check and shutdown if this token is missing instantly.
    - The second token is the action login token that will be verified on the server
* Launching the game client by directly will fail as "unauthenticated" or "invalid" depending on the method used. 

## Steps for Download to Web API
### Handle Login
* Handle LoginAsync() and return result or OK() + Token
* Store login token for X period of time

### Handle Download
* Select manifest file, determine latest version, set the directory path for the download
* On call to CheckLatestVersion()
    - compare delivered values against manifest file
    - deliver manifest or OK()    

* On call to GetFile(manifestguid, filename, clienttoken ) 
    - verify the clienttoken against in-memory db
    - verify the manifestguid is correct
    - check if the file exists and if so, deliver it
```
[HttpGet("{id}")]
public IActionResult GetDocumentBytes(int id)
{
    // FileContentResult.FileDownloadName to control the filename
    byte[] byteArray = GetDocumentByteArray(id);
    return new FileContentResult(byteArray, "application/octet-stream");
}
```
    -- or...download the files using C#/.NET filedownload and a direct filepath (i.e., normal HTTP download). The previous option is mainly to prevent arbitrary people from downloading/wasting bandwidth without the client

## Storage/db client
* Will probably go with Sqlite on the client for simplicity
* server storage hasn't been nailed down but it may be either something Dapper powered (to give flexibility) or perhaps more specific if NoSql seems like the best option for certain workloads (or for developer speed/sanity)


## Steps for downloader to Client Launch
* Hash the user name and password, send to server, get response and login token if successful
* Read local manifest version, internal guid, and general SHA-1 thumbprint and send to server
* Receive response with path for files to download or noop if server thinks we're good
   > optional... verify all files every so often by comparing md5 hashes against listing we have (SLOW)
> If client up to date
  * Show "Start" button and wire up as "launch game client and pass tokens parameters"
> If client not up to date
  * download manifest of files that will be patched
  * download files in manifest  (filename::FALSE::<HASH>)
  * unzip any files which were flagged as zipped (filename::TRUE::<HASH>)
  * Verify the hash of every file against every file in the manifest

* client launches
    - verify downloader token is valid (delivered on success of patching) ---> this is a token encrypted using the current client key? Or using a hash based on the current client?
    - verify login token is valid   
    - show splash screen
    - connect to server (in parallel with splash)
    - send basic client info (stats about the machine the client is running on - current resolution/cpus/gpu/free space/etc)
    - download initial snapshot of player cache info (stuff we will keep local like what the player is wearing, their name, etc)
    - download initial gameplay snapshot (the snapshot of the players current state in the world, their health, etc as the server sees it)
    - download initial world snapshot (any information about the world which the server controls like important objects around them/etc)
    > optional?: perhaps download and verify an encrypted token for each of the snapshots to verify they're legit from the server
    - load resources according to snapshots delivered
    - begin gameplay loop
    - check for heartbeat from server (otherwise we show 'disconnected') - throw disconnected message and terminate on 'ok'
    - listen for and process server snapshots
    - send client actions as snapshots to the server for validation


## Steps for GameServer Launch
* Load up gameserver loop 
* begin listening for client connection requests
* query TableStore to verify client tokens during auth/connect
* deliver gameworld snapshots to every connected player every X milliseconds
* simulate any actions taken by players
* check actions being taken by players for validity
* check whether players are inactive and log them off after X minutes, terminate their login token when this happens
> Other, determine max connections
