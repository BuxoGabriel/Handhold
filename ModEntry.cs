using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Network;

namespace Handhold
{
    internal sealed class ModEntry: Mod
    {
        const float MAX_DIST = 1f;
        // TODO use getter setters with Farmer type get
        private long? requesterId;
        private long? holderId;
        private bool holderOut = false;

        public override void Entry(IModHelper helper)
        {
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.Multiplayer.ModMessageReceived += this.onModMessageReceived;
            helper.Events.GameLoop.UpdateTicked += this.onUpdateTicked;
        }

        private void onUpdateTicked(object sender, EventArgs e)
        {
            if (this.holderId == null) return;

            Farmer holder = Game1.getFarmer((long) this.holderId);
            if (holder.currentLocation.Equals(Game1.currentLocation))
            {
                if(!this.holderOut)
                {
                    this.holderOut = true;
                    this.Monitor.Log("Player moved out of location", LogLevel.Debug);
                    this.Monitor.Log("Player moved to " + holder.currentLocation, LogLevel.Debug);
                    this.Monitor.Log("You are in " + Game1.currentLocation, LogLevel.Debug);
                }
                this.holderId = null;
                return;
            }

            Vector2 displacement = holder.Position - Game1.player.Position;
            float length = displacement.Length();
            displacement.Normalize();
            displacement *= Math.Max(0, length - MAX_DIST);
            Vector2 newPosition = Game1.player.position + displacement;
                
            // check for collision and move
            Rectangle newBoundingBox = new Rectangle(newPosition.ToPoint(), new Point(Game1.tileSize, Game1.tileSize));
            if(!Game1.currentLocation.isCollidingPosition(newBoundingBox, Game1.viewport, true)) Game1.player.position.Set(Game1.player.Position + displacement);
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady) return;
            string buttonPressed = e.Button.ToString();
            switch(buttonPressed)
            {
                case "H":
                    this.playerPressH();
                    break;
                default: break;
            }
        }

        private void onModMessageReceived(object sender, ModMessageReceivedEventArgs e)
        {
            // if (e.FromModID != this.ModManifest.UniqueID) return;
            switch (e.Type)
            {
                case "Handhold Request":
                    this.handleHandholdRequest(e);
                    break;
                case "Handhold Accept":
                    this.handleHandholdAccepted(e);
                    break;
                default: break;
            }
        }

        private void playerPressH()
        {
            this.Monitor.Log("Player pressed handhold", LogLevel.Debug);
            // If H is pressed while holding someones hand, let go
            if (this.holderId != null)
            {
                this.holderId = null;
                return;
            }
            // If H is pressed while handhold request exists, accept.
            else if (this.requesterId != null)
            {
                this.holderId = this.requesterId;
                this.requesterId = null;
                return;
            }
            // Otherwise, look for nearby player to request handhold
            var player = Game1.player;
            foreach (Farmer farmer in Game1.currentLocation.farmers)
            {
                float xDistSq = (farmer.position.X - player.position.X) * (farmer.position.X - player.position.X);
                float yDistSq = (farmer.position.Y - player.position.Y) * (farmer.position.Y - player.position.Y);
                if (xDistSq + yDistSq <= MAX_DIST * MAX_DIST)
                {
                    this.Helper.Multiplayer.SendMessage(player.UniqueMultiplayerID, "Handhold Request");
                }
            }
        }

        private void handleHandholdRequest(ModMessageReceivedEventArgs e)
        {
            long farmerId = e.ReadAs<long>();
            Farmer farmer = Game1.getFarmer(farmerId);
            this.requesterId = farmerId;
            Game1.addHUDMessage(new HUDMessage($"{farmer.Name} would like to hold your hand. Press H to accept.", ""));
        }

        private void handleHandholdAccepted(ModMessageReceivedEventArgs e)
        {
            long farmerId = e.ReadAs<long>();
            Farmer farmer = Game1.getFarmer(farmerId);
            Game1.addHUDMessage(new HUDMessage($"{farmer.Name} accepted your handhold request!", ""));
        }
    }
}
