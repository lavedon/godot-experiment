using Microsoft.Data.Sqlite;

/// <summary>One finished game, as stored in the database.</summary>
public record RunRecord(long Id, string PlayerName, int Score, int DistanceM, int Bolts, string PlayedAt);

/// <summary>All-time numbers for one player.</summary>
public record PlayerStats(int GamesPlayed, int BestScore, int TotalBolts, int TotalDistanceM, int TotalStomps, int BossesBeaten);

/// <summary>One person's best: their best score, and the farthest they ever rode (in meters).</summary>
public record PlayerBest(string Name, int BestScore, int FarthestM);

/// <summary>
/// Saves everything the player does into a SQLite database file (robotdash.db).
///
/// Tables:
///   runs     - one row for every game played (name, score, distance, bolts, enemies stomped, bosses beaten,
///              spare batteries used, how many worlds of the World Tour, how long, when)
///   settings - simple key/value pairs, like the last player picked ("player_name"), and for each player (in small letters):
///              "look:sam" = the look Sam wears, "looks_seen:sam" = the looks Sam already had an unlock party for
/// </summary>
public sealed class ScoreDatabase : IDisposable
{
    readonly SqliteConnection connection;

    public string FilePath { get; }

    public ScoreDatabase(string filePath)
    {
        FilePath = filePath;
        connection = new SqliteConnection($"Data Source={filePath}");
        connection.Open();
        CreateTables();
    }

    void CreateTables()
    {
        Execute("""
            CREATE TABLE IF NOT EXISTS runs (
                id          INTEGER PRIMARY KEY AUTOINCREMENT,
                player_name TEXT    NOT NULL,
                score       INTEGER NOT NULL,
                distance_m  INTEGER NOT NULL,
                bolts       INTEGER NOT NULL,
                duration_s  REAL    NOT NULL,
                played_at   TEXT    NOT NULL DEFAULT (datetime('now', 'localtime'))
            );
            CREATE INDEX IF NOT EXISTS idx_runs_score ON runs(score DESC);

            CREATE TABLE IF NOT EXISTS settings (
                key   TEXT PRIMARY KEY,
                value TEXT NOT NULL
            );
            """);

        // Columns added later. Older databases get them added here (old games count as 0).
        AddColumnIfMissing("runs", "enemies_stomped", "INTEGER NOT NULL DEFAULT 0");
        AddColumnIfMissing("runs", "bosses_defeated", "INTEGER NOT NULL DEFAULT 0");
        AddColumnIfMissing("runs", "batteries_used", "INTEGER NOT NULL DEFAULT 0");
        // How many worlds the game rode through: 1 = only Sunny Hills, 3 = all the way to Night City, 6+ = all the way around.
        // (Old games count as Sunny Hills only, so postcards are earned by really going there.)
        AddColumnIfMissing("runs", "world_reached", "INTEGER NOT NULL DEFAULT 1");
    }

    void AddColumnIfMissing(string table, string column, string definition)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM pragma_table_info('{table}') WHERE name = $column;";
        cmd.Parameters.AddWithValue("$column", column);
        if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
            Execute($"ALTER TABLE {table} ADD COLUMN {column} {definition};");
    }

    /// <summary>Saves a finished game and returns its new id.</summary>
    public long SaveRun(string playerName, int score, int distanceM, int bolts, int enemiesStomped, int bossesDefeated,
                        double durationSeconds, int batteriesUsed = 0, int worldReached = 1)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO runs (player_name, score, distance_m, bolts, enemies_stomped, bosses_defeated, duration_s, batteries_used, world_reached)
            VALUES ($name, $score, $distance, $bolts, $stomped, $bosses, $duration, $batteries, $world);
            SELECT last_insert_rowid();
            """;
        // Parameters ($name etc.) keep the data safe, even if a name has funny characters in it.
        cmd.Parameters.AddWithValue("$name", playerName);
        cmd.Parameters.AddWithValue("$score", score);
        cmd.Parameters.AddWithValue("$distance", distanceM);
        cmd.Parameters.AddWithValue("$bolts", bolts);
        cmd.Parameters.AddWithValue("$stomped", enemiesStomped);
        cmd.Parameters.AddWithValue("$bosses", bossesDefeated);
        cmd.Parameters.AddWithValue("$duration", durationSeconds);
        cmd.Parameters.AddWithValue("$batteries", batteriesUsed);
        cmd.Parameters.AddWithValue("$world", worldReached);
        return (long)cmd.ExecuteScalar()!;
    }

    /// <summary>
    /// The most worlds this player has ever ridden through in one game (1 = only Sunny Hills, or never played).
    /// "COLLATE NOCASE" means "Sam" and "sam" are the same player.
    /// </summary>
    public int FarthestWorld(string playerName)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COALESCE(MAX(world_reached), 1) FROM runs WHERE player_name = $name COLLATE NOCASE;";
        cmd.Parameters.AddWithValue("$name", playerName);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public List<RunRecord> TopScores(int count)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT id, player_name, score, distance_m, bolts, played_at
            FROM runs
            ORDER BY score DESC, id ASC
            LIMIT $count;
            """;
        cmd.Parameters.AddWithValue("$count", count);

        var list = new List<RunRecord>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new RunRecord(
                reader.GetInt64(0), reader.GetString(1), reader.GetInt32(2),
                reader.GetInt32(3), reader.GetInt32(4), reader.GetString(5)));
        }
        return list;
    }

    public int BestScore()
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COALESCE(MAX(score), 0) FROM runs;";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    // ---------- The family race: everyone who plays ----------
    // "COLLATE NOCASE" means "Sam" and "sam" are the same person. Games played without a name count as a person
    // called "Player". A name is always written the way it was typed the last time (the newest spelling).

    /// <summary>Everyone who has played, the one who played most recently first (at most "max" names).</summary>
    public List<string> KnownPlayers(int max)
    {
        using var cmd = connection.CreateCommand();
        // (With just one MAX(...), SQLite takes player_name from the same row as the biggest id: the newest spelling)
        cmd.CommandText = """
            SELECT player_name, MAX(id)
            FROM runs
            GROUP BY player_name COLLATE NOCASE
            ORDER BY MAX(id) DESC
            LIMIT $max;
            """;
        cmd.Parameters.AddWithValue("$max", max);
        var names = new List<string>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) names.Add(reader.GetString(0));
        return names;
    }

    /// <summary>Each person's best score and farthest ride, the best score first (at most "max" people).</summary>
    public List<PlayerBest> PlayerBests(int max)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT (SELECT r2.player_name FROM runs r2 WHERE r2.player_name = r.player_name COLLATE NOCASE ORDER BY r2.id DESC LIMIT 1),
                   MAX(r.score), MAX(r.distance_m)
            FROM runs r
            GROUP BY r.player_name COLLATE NOCASE
            ORDER BY MAX(r.score) DESC, MAX(r.id) DESC
            LIMIT $max;
            """;
        cmd.Parameters.AddWithValue("$max", max);
        var bests = new List<PlayerBest>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) bests.Add(new PlayerBest(reader.GetString(0), reader.GetInt32(1), reader.GetInt32(2)));
        return bests;
    }

    /// <summary>The farthest this player ever rode, in meters (0 if they never played).</summary>
    public int BestDistanceFor(string playerName)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COALESCE(MAX(distance_m), 0) FROM runs WHERE player_name = $name COLLATE NOCASE;";
        cmd.Parameters.AddWithValue("$name", playerName);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    /// <summary>How far this player rode in their last game, in meters (0 if they never played).</summary>
    public int LastDistanceFor(string playerName)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT distance_m FROM runs WHERE player_name = $name COLLATE NOCASE ORDER BY id DESC LIMIT 1;";
        cmd.Parameters.AddWithValue("$name", playerName);
        return cmd.ExecuteScalar() is { } meters and not DBNull ? Convert.ToInt32(meters) : 0;
    }

    /// <summary>Everyone who played today, with their best score today, the best one first.</summary>
    public List<(string Name, int Score)> TodaysBests()
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT (SELECT r2.player_name FROM runs r2 WHERE r2.player_name = r.player_name COLLATE NOCASE ORDER BY r2.id DESC LIMIT 1),
                   MAX(r.score)
            FROM runs r
            WHERE r.played_at >= date('now', 'localtime')
            GROUP BY r.player_name COLLATE NOCASE
            ORDER BY MAX(r.score) DESC, MAX(r.id) DESC;
            """;
        var bests = new List<(string Name, int Score)>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) bests.Add((reader.GetString(0), reader.GetInt32(1)));
        return bests;
    }

    /// <summary>
    /// All-time numbers for one player. TotalBolts is their Bolt Bank: every bolt they ever grabbed.
    /// "COLLATE NOCASE" means "Sam" and "sam" are the same player.
    /// </summary>
    public PlayerStats StatsFor(string playerName)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT COUNT(*), COALESCE(MAX(score), 0), COALESCE(SUM(bolts), 0), COALESCE(SUM(distance_m), 0),
                   COALESCE(SUM(enemies_stomped), 0), COALESCE(SUM(bosses_defeated), 0)
            FROM runs
            WHERE player_name = $name COLLATE NOCASE;
            """;
        cmd.Parameters.AddWithValue("$name", playerName);
        using var reader = cmd.ExecuteReader();
        reader.Read();
        return new PlayerStats(reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetInt32(3),
                               reader.GetInt32(4), reader.GetInt32(5));
    }

    public string? GetSetting(string key)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT value FROM settings WHERE key = $key;";
        cmd.Parameters.AddWithValue("$key", key);
        return cmd.ExecuteScalar() as string;
    }

    public void SetSetting(string key, string value)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO settings (key, value) VALUES ($key, $value)
            ON CONFLICT(key) DO UPDATE SET value = excluded.value;
            """;
        cmd.Parameters.AddWithValue("$key", key);
        cmd.Parameters.AddWithValue("$value", value);
        cmd.ExecuteNonQuery();
    }

    void Execute(string sql)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    public void Dispose() => connection.Dispose();
}
