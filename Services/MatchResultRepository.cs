using Akasha.Contracts;
using Dapper;
using Npgsql;
namespace Akasha.Consumer.Services
{
    public class MatchResultRepository
    {
        private readonly ILogger _logger;
        private readonly string _connectionString;
        public MatchResultRepository(ILogger<MatchResultRepository> logger, IConfiguration config)
        {
            _logger = logger;
            _connectionString = config.GetConnectionString("Postgres") ??
                throw new InvalidOperationException("Connection string missing");
        }
        public async Task ProcessMessageAsync(MatchRecord record, CancellationToken ct = default)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));

            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(ct);
            await using var tx = await conn.BeginTransactionAsync(ct);

            try
            {
                _logger.LogInformation($"Proccessing match {record.MatchId}");

                // matches table
                const string sqlMatch = @"
                    INSERT INTO stats.matches (external_id, server_id, map_name, mission_name,
                        start_time, end_time, winner, duration, primeva_score, boscali_score)
                    VALUES (@MatchId, @ServerId, @MapName, @MissionName, @StartTimeUnix,
                        @EndTimeUnix, @Winner, @Duration, @PrimevaScore, @BoscaliScore)
                    ON CONFLICT (external_id) DO NOTHING";
                await conn.ExecuteAsync(sqlMatch, record, tx);


                
                if(record.Players != null)
                {
                    // players table
                    const string sqlPlayer = @"
                    INSERT INTO stats.players (steam_id, match_id, player_name, faction, score)
                    VALUES (@PlayerId, @MatchId, @PlayerName, @Faction, @Score)
                    ON CONFLICT (steam_id, match_id) DO UPDATE SET
                        player_name = EXCLUDED.player_name, score = EXCLUDED.score";

                    foreach (var player in record.Players)
                    {
                        
                        await conn.ExecuteAsync(sqlPlayer, new
                        {
                            PlayerId = player.PlayerId,
                            MatchId = record.MatchId,
                            PlayerName = player.PlayerName,
                            Faction = player.Faction,
                            Score = player.Score,
                        }, tx);



                        //sorties table
                        if (player.Sorties == null) continue;
                        foreach(var sortie in player.Sorties)
                        {
                            const string sqlSortie = @"
                            INSERT INTO stats.sorties (match_id, sortie_idx, player_steam_id,
                                aircraft, start_time, end_time, end_reason, 
                                jamming_seconds, detected_targets,
                                killed_by_unit, killed_by_weapon, killed_by_player)
                            VALUES (@MatchId, @SortieIdx, @SteamId, @AircraftName, @StartTime,
                                @EndTime, @EndReason, @JammingSeconds, @DetectedTargets,
                                @KilledByUnit, @KilledByWeapon, @KilledByPlayer)
                            ON CONFLICT (match_id, sortie_idx) DO NOTHING";

                            await conn.ExecuteAsync(sqlSortie, new
                            {
                                MatchId = record.MatchId,
                                SortieIdx = sortie.SortieIdx,
                                SteamId = player.PlayerId,
                                AircraftName = sortie.AircraftName,
                                StartTime = sortie.StartTime,
                                EndTime = sortie.EndTime,
                                EndReason = sortie.EndReason,
                                JammingSeconds = sortie.JammingAmount,
                                DetectedTargets = sortie.DetectedTargets,
                                KilledByUnit = sortie.KilledByUnit,
                                KilledByWeapon = sortie.KilledByWeapon,
                                KilledByPlayer = sortie.KilledByPlayer,
                            }, tx);

                            int killIdx = 0;
                            // kills table
                            if(sortie.Kills == null) continue;
                            foreach(var kill in sortie.Kills)
                            {
                                const string sqlKill = @"
                                INSERT INTO stats.kills (match_id, sortie_idx, kill_idx,
                                    killed_steam_id, killed_unit_name, weapon)
                                VALUES (@MatchId, @SortieIdx, @KillIdx,
                                    @KilledUnit, @KilledPlayer, @UsedWeapon)
                                ON CONFLICT (match_id, sortie_idx, kill_idx) DO NOTHING";

                                await conn.ExecuteAsync(sqlKill, new
                                {
                                    MatchId = record.MatchId,
                                    SortieIdx = sortie.SortieIdx,
                                    KillIdx = killIdx++,
                                    KilledUnit = kill.KilledUnit,
                                    KilledPlayer = kill.KilledPlayerId,
                                    UsedWeapon = kill.UsedWeapon,
                                });
                            }
                        }
                    }
                }

                await tx.CommitAsync(ct);
                _logger.LogInformation($"Match {record.MatchId} saved successfully!!");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                _logger.LogError($"Transaction failed and rolled back for match {record.MatchId}");
                throw;
            }
        }
    }
}
