using Microsoft.Data.Sqlite;

/// <summary>One finished game, as stored in the database.</summary>
public record RunRecord(long Id, string PlayerName, int Score, int DistanceM, int Bolts, string PlayedAt);

/// <summary>All-time numbers for one player.</summary>
public record PlayerStats(int GamesPlayed, int BestScore, int TotalBolts, int TotalDistanceM, int TotalStomps, int BossesBeaten);

/// <summary>
/// Saves everything the player does into a SQLite database file (robotdash.db).
///
/// Tables:
///   runs     - one row for every game played (name, score, distance, bolts, enemies stomped, bosses beaten, how long, when)
///   settings - simple key/value pairs, like the last name typed in
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
                        double durationSeconds)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO runs (player_name, score, distance_m, bolts, enemies_stomped, bosses_defeated, duration_s)
            VALUES ($name, $score, $distance, $bolts, $stomped, $bosses, $duration);
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
        return (long)cmd.ExecuteScalar()!;
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

    public PlayerStats StatsFor(string playerName)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT COUNT(*), COALESCE(MAX(score), 0), COALESCE(SUM(bolts), 0), COALESCE(SUM(distance_m), 0),
                   COALESCE(SUM(enemies_stomped), 0), COALESCE(SUM(bosses_defeated), 0)
            FROM runs
            WHERE player_name = $name;
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
