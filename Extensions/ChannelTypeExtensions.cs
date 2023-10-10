using GW2IO.Maps.Structures;
using Blish_HUD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GW2IO.Maps.Extensions
{
    internal static class ChannelTypeExtensions
    {
        public static string GetTopicPrefix(this ChannelType type)
        {
            switch (type)
            {
                default:
                case ChannelType.Global:
                    return "maps.gw2.io/global";
                case ChannelType.Guild:
                    return $"maps.gw2.io/guild/";
                case ChannelType.Custom:
                    return $"maps.gw2.io/custom/";
            }
        }
    }
}
