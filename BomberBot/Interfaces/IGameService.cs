﻿using BomberBot.Common;
using BomberBot.Enums;
using System.Collections.Generic;

namespace BomberBot.Interfaces
{
    public interface IGameService<T>
    {
        string HomeKey { get; set; }
        T GameState { get; }
        void WriteMove(Move move);
        HashSet<Location> ToExploreLocations { get; }
        void UpdateToExploreLocations(Location location);
    }
}