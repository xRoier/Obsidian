﻿using Obsidian.Utilities.Converters;
using System.Text.Json.Serialization;

namespace Obsidian.Utilities.Registry.Codecs.Dimensions
{

    public class DimensionElement
    {
        /// <summary>
        /// Whether piglins shake and transform to zombified piglins.
        /// </summary>
        [JsonConverter(typeof(DefaultObjectConverter))]
        public bool PiglinSafe { get; set; }

        /// <summary>
        /// When false, compasses spin randomly. When true, nether portals can spawn zombified piglins.
        /// </summary>
        [JsonConverter(typeof(DefaultObjectConverter))]
        public bool Natural { get; set; }

        /// <summary>
        /// How much light the dimension has. 0.0 to 1.0
        /// </summary>
        [JsonConverter(typeof(DefaultObjectConverter))]
        public float AmbientLight { get; set; } = 0.0f;

        /// <summary>
        /// If this is set to a number, the time of the day is the specified value. 
        /// false, or 0 to 24000
        /// </summary>
        [JsonConverter(typeof(DefaultObjectConverter))]
        public long? FixedTime { get; set; }

        /// <summary>
        /// A resource location defining what block tag to use for infiniburn.
        /// </summary>
        public string Infiniburn { get; set; }

        /// <summary>
        /// Whether players can charge and use respawn anchors.
        /// </summary>
        [JsonConverter(typeof(DefaultObjectConverter))]
        public bool RespawnAnchorWorks { get; set; }

        /// <summary>
        /// Whether the dimension has skylight access or not.
        /// </summary>
        [JsonConverter(typeof(DefaultObjectConverter))]
        public bool HasSkylight { get; set; }

        /// <summary>
        /// Whether players can use a bed to sleep.
        /// </summary>
        [JsonConverter(typeof(DefaultObjectConverter))]
        public bool BedWorks { get; set; }

        /// <summary>
        /// ?
        /// </summary>
        public string Effects { get; set; }

        /// <summary>
        /// Whether players with the Bad Omen effect can cause a raid.
        /// </summary>
        [JsonConverter(typeof(DefaultObjectConverter))]
        public bool HasRaids { get; set; }

        /// <summary>
        /// The maximum height to which chorus fruits and nether portals can bring players within this dimension.
        /// </summary>
        public int LogicalHeight { get; set; }

        /// <summary>
        /// The multiplier applied to coordinates when traveling to the dimension.
        /// </summary>
        public float CoordinateScale { get; set; }

        /// <summary>
        /// Whether the dimensions behaves like the nether (water evaporates and sponges dry) or not.
        /// Also causes lava to spread thinner.
        /// </summary>
        [JsonConverter(typeof(DefaultObjectConverter))]
        public bool Ultrawarm { get; set; }

        /// <summary>
        /// Whether the dimension has a bedrock ceiling or not. When true, causes lava to spread faster.
        /// </summary>
        [JsonConverter(typeof(DefaultObjectConverter))]
        public bool HasCeiling { get; set; }
    }
}
