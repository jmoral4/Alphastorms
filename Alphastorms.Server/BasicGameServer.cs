﻿using Alphastorms.Shared;
using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alphastorms.Server
{
    class Loc
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    class ServerPlayerData
    {
        public double MillisecondsSinceLastHeard { get; set; }
        public System.Net.IPAddress IPAddress { get; set; }
        public int Port { get; set; }
        public string ClientID { get; set; }   // proxy for player
        public string GameSessionId { get; set; } //what specific game we're in
        public NetPeer _clientRef { get; set; }  //determine if we need this..
        public Loc PlayerLocation { get; set; }
        public int PlayerNumber { get; set; } //server assigns them as player 1-4 according to order of connection        

    }


    // we'll get back to the whole game session concept after we've proven comms are working..

    class GameSession
    {
        // unique key used to lookup this particular game
        string GameID { get; set; }
        // list of all players in the game
        public List<PlayerData> Players { get; set; }
        // used to purge this game from the list of active games
        bool InProgress { get; set; }

    }
    internal class BasicGameServer : BackgroundService
    {
        private readonly ILogger<BasicGameServer> _logger;

        Random r = new Random();
        //store a list of player data which we will use to determine who is playing whom and the state of the game
        ConcurrentDictionary<string, ServerPlayerData> _knownPlayers;
        //active games
        ConcurrentDictionary<string, GameSession> _activeGames;
        //players who are unmatched and waiting to get put into a game (dynamically created lobby)
        List<string> SearchForUnmatchedPlayers() => _knownPlayers.Where(x => x.Value.GameSessionId == string.Empty).Select(x => x.Value.GameSessionId).ToList();

        readonly NetSerializer _netSerializer = new NetSerializer();
        readonly NetPacketProcessor _packetProcessor = new NetPacketProcessor();

        short _nextPlayerId = 0;
        private int GetNextPlayerId() => _nextPlayerId++;

        public BasicGameServer(ILogger<BasicGameServer> logger)
        {
            _logger = logger;
            _knownPlayers = new ConcurrentDictionary<string, ServerPlayerData>();
            _activeGames = new ConcurrentDictionary<string, GameSession>();
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {            
            Start();
            _logger.LogInformation("Server Shutdown at: {time}", DateTimeOffset.Now);
            //pause before exit
            //Console.ReadKey();
        }


        public void Start()
        {
            _logger.LogInformation("GAME SERVER STARTED");
            EventBasedNetListener listener = new EventBasedNetListener();
            NetManager server = new NetManager(listener);
            server.Start(9050 /* port */);

            _netSerializer.Register<WelcomePacket>();
            _netSerializer.Register<UpdatePacket>();
            _netSerializer.Register<ServerSnapshotPacket>();
            _netSerializer.Register<EchoPacket>();

            _packetProcessor.SubscribeReusable<UpdatePacket, NetPeer>(HandleClientUpdate);



            listener.ConnectionRequestEvent += request =>
            {                
                //we will change this when we create 'game sessions' for now we only support the 4 players.
                if (server.ConnectedPeersCount < 4 /* max connections */)
                {
                    request.AcceptIfKey("SomeConnectionKey");

                }
                else
                {
                    request.Reject();
                }
            };

            listener.PeerConnectedEvent += peer =>
            {
                //server stores by ipaddress+port
                string key = peer.EndPoint.ToString();
                if (!_knownPlayers.ContainsKey(key))
                {
                    // new unknown player connected, let's start tracking them
                    ServerPlayerData pd = new ServerPlayerData
                    {
                        //generate a unique ID for the peer connecting which will be referenced in the future (by other peers as an intermediary (vs telling peers specifically who they're connected to IP/POrt or whatever)
                        // let's condense this down by 1/3 or so..  (also we know the final characters are ==
                        ClientID = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("=", ""),
                        GameSessionId = string.Empty,
                        IPAddress = peer.EndPoint.Address,
                        Port = peer.EndPoint.Port,
                        MillisecondsSinceLastHeard = 0,
                        PlayerNumber = GetNextPlayerId() + 1,
                        PlayerLocation = new Loc { X = 0, Y = 0 },
                        _clientRef = peer
                    };
                    bool success = _knownPlayers.TryAdd(key, pd);
                    pd.PlayerLocation.X = pd.PlayerNumber * 15;
                    pd.PlayerLocation.Y = pd.PlayerNumber * 15;
                }
                else
                {
                    //player must have disconnected during a game, let's try to reconnect to their game if it's still playing
                    //TODO: look up the GameSessionID, then start sending this player's packets to all players in the session
                }


                var pdat = _knownPlayers[key];
                _logger.LogInformation("PLAYER JOINED: {0} as PLAYER {1} with ID {2}", peer.EndPoint, pdat.PlayerNumber, pdat.ClientID);
                //send our info in a welcome packet
                WelcomePacket wp = new WelcomePacket
                {
                    PlayerId = pdat.ClientID,
                    PlayerNumber = pdat.PlayerNumber,
                    XStart = pdat.PlayerLocation.X,
                    YStart = pdat.PlayerLocation.Y
                };

                _packetProcessor.Send<WelcomePacket>(peer, wp, DeliveryMethod.ReliableOrdered);
                _logger.LogInformation("Packet Processor Sent Packet!");
                //peer.Send(_netSerializer.Serialize<WelcomePacket>(wp), DeliveryMethod.ReliableOrdered);





            };

            listener.NetworkReceiveEvent += Listener_NetworkReceiveEvent;

            // hold the server thread from exiting until we've "pressed any key
            while (!Console.KeyAvailable)
            {
                server.PollEvents();
                UpdateAllClients(); //sends a snapshot of all client locations every 15ms
                Thread.Sleep(15);
            }
            server.Stop();

        }

        private void UpdateAllClients()
        {

            if (_knownPlayers.Count < 1)
                return; // no need to update a single player on his own location (or should we? perhaps later).
            //create the snapshot once and then send to all players
            ServerSnapshotPacket snapshot = new ServerSnapshotPacket();
            snapshot.Players = _nextPlayerId;
            snapshot.X = new int[snapshot.Players];
            snapshot.Y = new int[snapshot.Players];

            //suboptimal LINQ query for snapshot data.. fill the snapshot
            for (int i = 0; i < snapshot.Players; i++)
            {
                var p = _knownPlayers.Values.Where(x => x.PlayerNumber == (i + 1)).FirstOrDefault();
                if (p == null)
                    break; ;
                snapshot.X[i] = p.PlayerLocation.X;
                snapshot.Y[i] = p.PlayerLocation.Y;
            }

            foreach (var client in _knownPlayers.Values)
            {
                //send a snapshot to every player
                if (client._clientRef.ConnectionState == ConnectionState.Connected)
                {
                    _packetProcessor.Send<ServerSnapshotPacket>(client._clientRef, snapshot, DeliveryMethod.ReliableOrdered);

                }
            }

        }




        private void HandleClientUpdate(UpdatePacket update, NetPeer peer)
        {
            //process and echo it back..
            // let's echo back the fact we received the message

            if (update != null)
            {

                //we got a client update so let's go ahead and update them...
                //first find them
                string key = peer.EndPoint.ToString();
                var playerRecord = _knownPlayers[key]; // temp record
                if (update.PlayerAction == 0)
                    playerRecord.PlayerLocation.Y += 5; // = new Point(playerRecord.PlayerLocation.X, playerRecord.PlayerLocation.Y - 5);                    
                if (update.PlayerAction == 1)
                    playerRecord.PlayerLocation.Y -= 5;
                if (update.PlayerAction == 2)
                    playerRecord.PlayerLocation.X -= 5;
                if (update.PlayerAction == 3)
                    playerRecord.PlayerLocation.X += 5;

                _logger.LogInformation($"Updated Player {playerRecord.PlayerNumber} Location to { playerRecord.PlayerLocation.X},{playerRecord.PlayerLocation.Y}");
                _logger.LogInformation($"Echo'd Peer:{peer.EndPoint.ToString()} with {update.PlayerAction}");
                EchoPacket ep = new EchoPacket() { ClientDirection = update.PlayerAction };
                _packetProcessor.Send<EchoPacket>(peer, ep, DeliveryMethod.ReliableOrdered);

            }

        }


        private void Listener_NetworkReceiveEvent(LiteNetLib.NetPeer peer, NetDataReader reader, DeliveryMethod deliveryMethod)
        {

            _packetProcessor.ReadAllPackets(reader, peer);

            // check if we even know this peer
            var key = peer.EndPoint.ToString();
            if (_knownPlayers.ContainsKey(key))
            {
            }
            else
            {
                Debug.Assert(false, "The Connected Peer was Unknown to us! (Did not exist in KnownPlayer list)");
                // TODO: should assert or at least log this properly for future debugging
                _logger.LogInformation($"Unknown Client connected as:{key}!");
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
        }
    }
}
