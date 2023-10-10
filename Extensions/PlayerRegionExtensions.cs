using GW2IO.Maps.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GW2IO.Maps.Extensions
{
    internal static class PlayerRegionExtensions
    {
        public static string GetTopicString(this PlayerRegion region)
        {
            switch (region)
            {
                case PlayerRegion.EU:
                    return "eu";
                case PlayerRegion.US:
                    return "us";
                default:
                case PlayerRegion.Unknown:
                    return "unknown";
            }
        }
    }
}
