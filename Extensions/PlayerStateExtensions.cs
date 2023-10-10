using GW2IO.Maps.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GW2IO.Maps.Extensions
{
    internal static class PlayerStateExtensions
    {
        public static Microsoft.Xna.Framework.Color GetStatusColor(this PlayerState state)
        {
            switch (state)
            {
                case PlayerState.Connected:
                    return Microsoft.Xna.Framework.Color.LightGreen;
                case PlayerState.Connecting:
                    return Microsoft.Xna.Framework.Color.LightYellow;
                case PlayerState.Disconnected:
                    return Microsoft.Xna.Framework.Color.Red;
                default:
                    return Microsoft.Xna.Framework.Color.White;
            }
        }
    }
}
