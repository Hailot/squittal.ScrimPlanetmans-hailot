USE [PlanetmansDbContext];

GO

CREATE OR ALTER VIEW View_ScrimMatchReportInfantryTeamStats AS

  SELECT match_teams.ScrimMatchId,
       match_teams.TeamOrdinal,
       ( COALESCE(kill_sums.Points, 0) +  COALESCE(adjustment_sums.Points, 0) +  COALESCE(capture_sums.Points, 0) ) Points,
       ( COALESCE(kill_sums.NetScore, 0) + COALESCE(death_sums.NetScore, 0) + COALESCE(adjustment_sums.Points, 0) + COALESCE(capture_sums.Points, 0) ) NetScore,
       COALESCE(adjustment_sums.Points, 0) PointAdjustments,
       COALESCE(capture_sums.Points, 0) FacilityCapturePoints,
       COALESCE(kill_sums.Kills, 0) Kills,
       COALESCE(kill_sums.HeadshotKills, 0) HeadshotKills,
       COALESCE(death_sums.Deaths, 0) Deaths,
       COALESCE(death_sums.HeadshotEnemyDeaths, 0) HeadshotEnemyDeaths,
       COALESCE(kill_sums.TeamKills, 0) TeamKills,
       COALESCE(death_sums.Suicides, 0) Suicides,
       COALESCE(death_sums.ScoredDeaths, 0) ScoredDeaths,
       COALESCE(death_sums.ZeroPointDeaths, 0) ZeroPointDeaths,
       COALESCE(death_sums.TeamKillDeaths, 0) TeamKillDeaths,
       COALESCE(damage_sums.DamageAssists, 0) DamageAssists,
       COALESCE(damage_sums.DamageTeamAssists, 0) DamageTeamAssists,
       COALESCE(kill_sums.DamageAssistedKills, 0) DamageAssistedKills,
       COALESCE(death_sums.DamageAssistedDeaths, 0) DamageAssistedDeaths,
       COALESCE(death_sums.DamageAssistedEnemyDeaths, 0) DamageAssistedEnemyDeaths,
       COALESCE(death_sums.UnassistedEnemyDeaths, 0) UnassistedEnemyDeaths,
       COALESCE(kill_sums.KillsAsHeavyAssault, 0) KillsAsHeavyAssault,
       COALESCE(kill_sums.KillsAsInfiltrator, 0) KillsAsInfiltrator,
       COALESCE(kill_sums.KillsAsLightAssault, 0) KillsAsLightAssault,
       COALESCE(kill_sums.KillsAsMedic, 0) KillsAsMedic,
       COALESCE(kill_sums.KillsAsEngineer, 0) KillsAsEngineer,
       COALESCE(kill_sums.KillsAsMax, 0) KillsAsMax,
       COALESCE(death_sums.DeathsAsHeavyAssault, 0) DeathsAsHeavyAssault,
       COALESCE(death_sums.DeathsAsInfiltrator, 0) DeathsAsInfiltrator,
       COALESCE(death_sums.DeathsAsLightAssault, 0) DeathsAsLightAssault,
       COALESCE(death_sums.DeathsAsMedic, 0) DeathsAsMedic,
       COALESCE(death_sums.DeathsAsEngineer, 0) DeathsAsEngineer,
       COALESCE(death_sums.DeathsAsMax, 0) DeathsAsMax,
       COALESCE(damage_sums.DamageAssistsAsHeavyAssault, 0) DamageAssistsAsHeavyAssault,
       COALESCE(damage_sums.DamageAssistsAsInfiltrator, 0) DamageAssistsAsInfiltrator,
       COALESCE(damage_sums.DamageAssistsAsLightAssault, 0) DamageAssistsAsLightAssault,
       COALESCE(damage_sums.DamageAssistsAsMedic, 0) DamageAssistsAsMedic,
       COALESCE(damage_sums.DamageAssistsAsEngineer, 0) DamageAssistsAsEngineer,
       COALESCE(damage_sums.DamageAssistsAsMax, 0) DamageAssistsAsMax
  FROM ( SELECT match_players.ScrimMatchId,
                match_players.TeamOrdinal
           FROM ScrimMatchParticipatingPlayer match_players
           GROUP BY match_players.ScrimMatchId, match_players.TeamOrdinal ) match_teams
    INNER JOIN ( SELECT kills.ScrimMatchId,
                        kills.AttackerTeamOrdinal TeamOrdinal,
                        SUM( kills.Points ) Points,
                        SUM( CASE WHEN DeathType IN ( 0, 1, 2) THEN kills.Points ELSE 0 END ) NetScore,
                        SUM( CASE WHEN DeathType = 0 THEN 1 ELSE 0 END ) Kills,
                        SUM( CASE WHEN DeathType = 1 THEN 1 ELSE 0 END ) TeamKills,
                        SUM( CASE WHEN DeathType = 2 THEN 1 ELSE 0 END ) Suicides,
                        SUM( CASE WHEN DeathType = 0 AND kills.IsHeadshot = 1 THEN 1 ELSE 0 END ) HeadshotKills,
                        SUM( CASE WHEN DeathType = 0 AND kills.Points > 0 THEN 1 ELSE 0 END ) ScoredKills,
                        SUM( CASE WHEN DeathType = 0 AND kills.Points = 0 THEN 1 ELSE 0 END ) ZeroPointKills,
                        SUM( CASE WHEN DeathType = 0 AND kills.AttackerLoadoutId IN ( 1, 8, 15) THEN 1 ELSE 0 END )  KillsAsInfiltrator,
                        SUM( CASE WHEN DeathType = 0 AND kills.AttackerLoadoutId IN ( 3, 10, 17) THEN 1 ELSE 0 END ) KillsAsLightAssault,
                        SUM( CASE WHEN DeathType = 0 AND kills.AttackerLoadoutId IN ( 4, 11, 18) THEN 1 ELSE 0 END ) KillsAsMedic,
                        SUM( CASE WHEN DeathType = 0 AND kills.AttackerLoadoutId IN ( 5, 12, 19) THEN 1 ELSE 0 END ) KillsAsEngineer,
                        SUM( CASE WHEN DeathType = 0 AND kills.AttackerLoadoutId IN ( 6, 13, 20) THEN 1 ELSE 0 END ) KillsAsHeavyAssault,
                        SUM( CASE WHEN DeathType = 0 AND kills.AttackerLoadoutId IN ( 7, 14, 21) THEN 1 ELSE 0 END ) KillsAsMax,
                        SUM( CASE WHEN DeathType = 0 AND damage_sums.TotalDamages > 0 THEN 1 ELSE 0 END ) DamageAssistedKills,
                        SUM( CASE WHEN DeathType = 0 AND damage_sums.TotalDamages IS NULL THEN 1 ELSE 0 END ) UnassistedKills
                    FROM ScrimDeath kills
                      LEFT OUTER JOIN ( SELECT ScrimMatchId, damages.AttackerTeamOrdinal, VictimCharacterId, damages.Timestamp, COUNT(*) TotalDamages,
                                          SUM( CASE WHEN damages.ActionType = 304 THEN 1 ELSE 0 END ) EnemyDamages,
                                          SUM( CASE WHEN damages.ActionType = 310 THEN 1 ELSE 0 END ) TeamDamages,
                                          SUM( CASE WHEN damages.ActionType = 312 THEN 1 ELSE 0 END ) SelfDamages
                                    FROM ScrimDamageAssist damages
                                    GROUP BY ScrimMatchId, AttackerTeamOrdinal, Timestamp, VictimCharacterId ) damage_sums
                        ON kills.ScrimMatchId = damage_sums.ScrimMatchId
                          AND kills.Timestamp = damage_sums.Timestamp
                          AND kills.AttackerTeamOrdinal = damage_sums.AttackerTeamOrdinal
                          AND kills.VictimCharacterId = damage_sums.VictimCharacterId
                    GROUP BY kills.ScrimMatchId, kills.AttackerTeamOrdinal ) kill_sums
      ON match_teams.ScrimMatchId = kill_sums.ScrimMatchId
         AND match_teams.TeamOrdinal = kill_sums.TeamOrdinal
    LEFT OUTER JOIN ( SELECT Deaths.ScrimMatchId,
                             Deaths.VictimTeamOrdinal TeamOrdinal,
                             SUM( CASE WHEN DeathType = 0 THEN Deaths.Points * -1 ELSE 0 END ) NetScore,
                             SUM( CASE WHEN DeathType IN ( 0, 1, 2) THEN 1 ELSE 0 END ) Deaths,
                             SUM( CASE WHEN DeathType = 1 THEN 1 ELSE 0 END ) TeamKillDeaths,
                             SUM( CASE WHEN DeathType = 2 THEN 1 ELSE 0 END ) Suicides,
                             SUM( CASE WHEN DeathType = 0 AND Deaths.IsHeadshot = 1 THEN 1 ELSE 0 END ) HeadshotEnemyDeaths,
                             SUM( CASE WHEN Deaths.IsHeadshot = 1 THEN 1 ELSE 0 END ) HeadshotDeaths,
                             SUM( CASE WHEN Deaths.Points > 0 THEN 1 ELSE 0 END ) ScoredDeaths,
                             SUM( CASE WHEN Deaths.Points = 0 THEN 1 ELSE 0 END ) ZeroPointDeaths,
                             SUM( CASE WHEN Deaths.VictimLoadoutId IN ( 1, 8, 15) THEN 1 ELSE 0 END )  DeathsAsInfiltrator,
                             SUM( CASE WHEN Deaths.VictimLoadoutId IN ( 3, 10, 17) THEN 1 ELSE 0 END ) DeathsAsLightAssault,
                             SUM( CASE WHEN Deaths.VictimLoadoutId IN ( 4, 11, 18) THEN 1 ELSE 0 END ) DeathsAsMedic,
                             SUM( CASE WHEN Deaths.VictimLoadoutId IN ( 5, 12, 19) THEN 1 ELSE 0 END ) DeathsAsEngineer,
                             SUM( CASE WHEN Deaths.VictimLoadoutId IN ( 6, 13, 20) THEN 1 ELSE 0 END ) DeathsAsHeavyAssault,
                             SUM( CASE WHEN Deaths.VictimLoadoutId IN ( 7, 14, 21) THEN 1 ELSE 0 END ) DeathsAsMax,
                             SUM( CASE WHEN DeathType = 0 AND damage_sums.TotalDamages > 0 THEN 1 ELSE 0 END ) DamageAssistedEnemyDeaths,
                             SUM( CASE WHEN damage_sums.TotalDamages > 0 THEN 1 ELSE 0 END ) DamageAssistedDeaths,
                             SUM( CASE WHEN DeathType = 0 AND damage_sums.TotalDamages IS NULL THEN 1 ELSE 0 END ) UnassistedEnemyDeaths,
                             SUM( CASE WHEN damage_sums.TotalDamages IS NULL THEN 1 ELSE 0 END ) UnassistedDeaths
                         FROM ScrimDeath Deaths
                           LEFT OUTER JOIN ( SELECT ScrimMatchId, damages.VictimTeamOrdinal, VictimCharacterId, damages.Timestamp, COUNT(*) TotalDamages,
                                               SUM( CASE WHEN damages.ActionType = 304 THEN 1 ELSE 0 END ) EnemyDamages,
                                               SUM( CASE WHEN damages.ActionType = 310 THEN 1 ELSE 0 END ) TeamDamages,
                                               SUM( CASE WHEN damages.ActionType = 312 THEN 1 ELSE 0 END ) SelfDamages
                                         FROM ScrimDamageAssist damages
                                         GROUP BY ScrimMatchId, VictimTeamOrdinal, Timestamp, VictimCharacterId ) damage_sums
                             ON Deaths.ScrimMatchId = damage_sums.ScrimMatchId
                               AND Deaths.Timestamp = damage_sums.Timestamp
                               AND Deaths.VictimTeamOrdinal = damage_sums.VictimTeamOrdinal
                               AND Deaths.VictimCharacterId = damage_sums.VictimCharacterId
                         GROUP BY Deaths.ScrimMatchId, Deaths.VictimTeamOrdinal ) death_sums
      ON kill_sums.ScrimMatchId = death_sums.ScrimMatchId
        AND kill_sums.TeamOrdinal = death_sums.TeamOrdinal
        AND match_teams.ScrimMatchId = death_sums.ScrimMatchId
        AND match_teams.TeamOrdinal = death_sums.TeamOrdinal
    LEFT OUTER JOIN ( SELECT match_players.ScrimMatchId,
                             match_players.TeamOrdinal,
                             SUM( CASE WHEN CharacterId = damages.AttackerCharacterId AND damages.ActionType = 304 THEN 1 ELSE 0 END ) DamageAssists,
                             SUM( CASE WHEN CharacterId = damages.AttackerCharacterId AND damages.ActionType = 310 THEN 1 ELSE 0 END ) DamageTeamAssists,
                             SUM( CASE WHEN CharacterId = damages.AttackerCharacterId AND damages.AttackerLoadoutId IN ( 1, 8, 15) THEN 1 ELSE 0 END )  DamageAssistsAsInfiltrator,
                             SUM( CASE WHEN CharacterId = damages.AttackerCharacterId AND damages.AttackerLoadoutId IN ( 3, 10, 17) THEN 1 ELSE 0 END ) DamageAssistsAsLightAssault,
                             SUM( CASE WHEN CharacterId = damages.AttackerCharacterId AND damages.AttackerLoadoutId IN ( 4, 11, 18) THEN 1 ELSE 0 END ) DamageAssistsAsMedic,
                             SUM( CASE WHEN CharacterId = damages.AttackerCharacterId AND damages.AttackerLoadoutId IN ( 5, 12, 19) THEN 1 ELSE 0 END ) DamageAssistsAsEngineer,
                             SUM( CASE WHEN CharacterId = damages.AttackerCharacterId AND damages.AttackerLoadoutId IN ( 6, 13, 20) THEN 1 ELSE 0 END ) DamageAssistsAsHeavyAssault,
                             SUM( CASE WHEN CharacterId = damages.AttackerCharacterId AND damages.AttackerLoadoutId IN ( 7, 14, 21) THEN 1 ELSE 0 END ) DamageAssistsAsMax
                       FROM ScrimMatchParticipatingPlayer match_players
                         INNER JOIN ScrimDamageAssist damages
                           ON match_players.ScrimMatchId = damages.ScrimMatchId
                               AND  match_players.CharacterId = damages.AttackerCharacterId
                       GROUP BY match_players.ScrimMatchId, match_players.TeamOrdinal ) damage_sums
              ON damage_sums.ScrimMatchId = match_teams.ScrimMatchId
                AND damage_sums.TeamOrdinal = match_teams.TeamOrdinal
    LEFT OUTER JOIN ( SELECT adjustments.ScrimMatchId,
                             adjustments.TeamOrdinal,
                             SUM( Points ) Points
                        FROM ScrimMatchTeamPointAdjustment adjustments
                        GROUP BY adjustments.ScrimMatchId, adjustments.TeamOrdinal) adjustment_sums
      ON adjustment_sums.ScrimMatchId = match_teams.ScrimMatchId
         AND adjustment_sums.TeamOrdinal = match_teams.TeamOrdinal
    LEFT OUTER JOIN ( SELECT captures.ScrimMatchId,
                             captures.ControllingTeamOrdinal TeamOrdinal,
                             SUM( captures.Points ) Points
                        FROM ScrimFacilityControl captures
                        GROUP BY captures.ScrimMatchId, captures.ControllingTeamOrdinal ) capture_sums
      ON match_teams.ScrimMatchId = capture_sums.ScrimMatchId
         AND match_teams.TeamOrdinal = capture_sums.TeamOrdinal
