﻿using squittal.ScrimPlanetmans.ScrimMatch.Models;

namespace squittal.ScrimPlanetmans.ScrimMatch.Events
{
    public class MatchTimerTickMessage
    {
        public MatchTimerStatus MatchTimerStatus { get; set; }

        public string Info { get; set; } = string.Empty;
        
        public MatchTimerTickMessage(MatchTimerStatus matchTimerStatus)
        {
            MatchTimerStatus = matchTimerStatus;
        }
    }
}
