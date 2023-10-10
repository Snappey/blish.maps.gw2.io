using GW2IO.Maps.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GW2IO.Maps.Extensions
{
    internal static class WvwTeamExtensions
    {
        public static string GetTopicString(this WvwTeam team)
        {
            switch (team)
            {
                case WvwTeam.Red:
                    return "red";
                case WvwTeam.Green:
                    return "green";
                case WvwTeam.Blue:
                    return "blue";
                default:
                case WvwTeam.Unknown:
                    return "unknown";
            }
        }
    }
}
