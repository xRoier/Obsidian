﻿using Obsidian.API;
using Obsidian.API.Events;
using Obsidian.Entities;

namespace Obsidian.Events.EventArgs
{
    public class BlockInteractEventArgs : PlayerEventArgs, ICancellable
    {
        public bool Cancel { get; set; }

        /// <summary>
        /// The block that was interacted with.
        /// </summary>
        public SebastiansBlock Block { get; }

        public Position Location { get; }

        public BlockInteractEventArgs(Player who, SebastiansBlock block, Position location) : base(who)
        {
            this.Block = block;
            this.Location = location;
        }
    }
}
