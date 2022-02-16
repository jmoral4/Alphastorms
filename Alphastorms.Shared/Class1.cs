using Microsoft.Xna.Framework;
using System.Threading;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Alphastorms.Shared
{
    public enum PlayerActions
    {
        PRESS_DOWN, PRESS_UP, PRESS_LEFT, PRESS_RIGHT, PRESS_FIRE
    }

    //keep it simple
    public class PlayerData
    {
        //NOTE to self, we'll have to track movement speed on the server..
        public PlayerData(Point size)
        {
            Size = size;
            MovementSpeed = 5;
        }
        // just a simple number to identify the player in the game (players are player 1-4)
        public int PlayerId { get; set; }
        public string TextureName { get; set; }
        public int Health { get; set; }
        public Point Location;
        public Rectangle BoundingBox { get; set; }
        public Vector2 DirectionVector { get; set; }
        public int MovementSpeed { get; set; }
        public readonly Point Size;
        public bool IsPresent { get; set; }

    }




    //trying to create a very simple snapshot message
    // expected number of players plus ever location
    public class ServerSnapshotPacket
    {
        public short Players { get; set; }
        public int[] X { get; set; }
        public int[] Y { get; set; }

    }

    public class UpdatePacket
    {
        public int PlayerAction { get; set; }
    }

    // sent to player on initial connection
    public class WelcomePacket
    {
        public string PlayerId { get; set; }
        public int PlayerNumber { get; set; }
        public int XStart { get; set; }
        public int YStart { get; set; }
    }


    //test bouncing this back and forth
    public class EchoPacket
    {
        public int ClientDirection { get; set; }

    }
}