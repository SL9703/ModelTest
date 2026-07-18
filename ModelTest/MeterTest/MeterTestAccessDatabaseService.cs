using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace ModelTest.MeterTest;

/// <summary>
/// MeterTest 本地文件数据库服务。
///
/// 说明：
/// 1. 采用 SQLite 作为便携式本地数据库，不依赖 Access、ACE、Jet 等运行环境。
/// 2. 数据库文件默认保存在程序目录下的 MeterTest/data/MeterTest.db。
/// 3. 启动时自动建库、自动建表，后续按需写入工位配置、控制 PCB 配置和测试结果。
/// </summary>
public sealed class MeterTestAccessDatabaseService
{
    private const string DatabaseFileName = "MeterTest.db";
    private const string RootFolderName = "MeterTest";
    private const string DataFolderName = "data";
    private readonly object syncRoot = new();
    private readonly string databasePath;
    private bool initialized;

    public MeterTestAccessDatabaseService()
    {
        databasePath = BuildDatabasePath();
    }

    /// <summary>
    /// 数据库文件的完整路径。
    /// </summary>
    public string DatabasePath => databasePath;

    /// <summary>
    /// 数据库是否已经完成初始化。
    /// </summary>
    public bool IsAvailable => initialized;

    /// <summary>
    /// 确保数据库文件、目录和表结构都已经准备完成。
    /// </summary>
    public void EnsureInitialized()
    {
        if (initialized)
            return;

        lock (syncRoot)
        {
            if (initialized)
                return;

            Directory.CreateDirectory(Path.GetDirectoryName(databasePath) ?? AppContext.BaseDirectory);
            Batteries_V2.Init();

            using SqliteConnection connection = CreateOpenConnection();
            CreateSchema(connection);
            initialized = true;
        }
    }

    /// <summary>
    /// 保存单个工位的测试展示结果，供界面切换恢复和程序重启后回填。
    /// </summary>
    public void SaveStationResult(
        string runId,
        string schemeName,
        string testItemName,
        string testSubItemName,
        int stationNo,
        StationDisplayStateData state)
    {
        EnsureInitialized();

        using SqliteConnection connection = CreateOpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO MeterTestStationResult
            (
                RunId,
                SchemeName,
                TestItemName,
                TestSubItemName,
                StationNo,
                TestContent,
                MeterAddress,
                Result,
                ResultTime,
                ResultTimeText,
                ToolTip,
                Message,
                ResultColorArgb,
                UpdatedAt
            )
            VALUES
            (
                $RunId,
                $SchemeName,
                $TestItemName,
                $TestSubItemName,
                $StationNo,
                $TestContent,
                $MeterAddress,
                $Result,
                $ResultTime,
                $ResultTimeText,
                $ToolTip,
                $Message,
                $ResultColorArgb,
                $UpdatedAt
            )
            ON CONFLICT(SchemeName, TestItemName, TestSubItemName, StationNo)
            DO UPDATE SET
                RunId = excluded.RunId,
                TestContent = excluded.TestContent,
                MeterAddress = excluded.MeterAddress,
                Result = excluded.Result,
                ResultTime = excluded.ResultTime,
                ResultTimeText = excluded.ResultTimeText,
                ToolTip = excluded.ToolTip,
                Message = excluded.Message,
                ResultColorArgb = excluded.ResultColorArgb,
                UpdatedAt = excluded.UpdatedAt;
            """;

        AddParameter(command, "$RunId", runId);
        AddParameter(command, "$SchemeName", schemeName);
        AddParameter(command, "$TestItemName", testItemName);
        AddParameter(command, "$TestSubItemName", testSubItemName);
        AddParameter(command, "$StationNo", stationNo);
        AddParameter(command, "$TestContent", state.TestContent);
        AddParameter(command, "$MeterAddress", state.MeterAddress);
        AddParameter(command, "$Result", state.Result);
        AddParameter(command, "$ResultTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        AddParameter(command, "$ResultTimeText", state.Time);
        AddParameter(command, "$ToolTip", state.ToolTip);
        AddParameter(command, "$Message", state.Message);
        AddParameter(command, "$ResultColorArgb", state.ResultColor.ToArgb());
        AddParameter(command, "$UpdatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// 读取指定方案、测试项、测试小项下的所有工位结果。
    /// </summary>
    public Dictionary<int, StationDisplayStateData> LoadStationResults(
        string schemeName,
        string testItemName,
        string testSubItemName)
    {
        EnsureInitialized();

        Dictionary<int, StationDisplayStateData> results = new();

        using SqliteConnection connection = CreateOpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                StationNo,
                TestContent,
                MeterAddress,
                Result,
                ResultTimeText,
                ToolTip,
                Message,
                ResultColorArgb
            FROM MeterTestStationResult
            WHERE SchemeName = $SchemeName
              AND TestItemName = $TestItemName
              AND TestSubItemName = $TestSubItemName
            ORDER BY StationNo;
            """;

        AddParameter(command, "$SchemeName", schemeName);
        AddParameter(command, "$TestItemName", testItemName);
        AddParameter(command, "$TestSubItemName", testSubItemName);

        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            int stationNo = reader.GetInt32(0);
            string testContent = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
            string meterAddress = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
            string result = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);
            string time = reader.IsDBNull(4) ? string.Empty : reader.GetString(4);
            string toolTip = reader.IsDBNull(5) ? string.Empty : reader.GetString(5);
            string message = reader.IsDBNull(6) ? string.Empty : reader.GetString(6);
            int resultColorArgb = reader.IsDBNull(7) ? Color.FromArgb(31, 41, 55).ToArgb() : reader.GetInt32(7);

            results[stationNo] = new StationDisplayStateData(
                testContent,
                meterAddress,
                result,
                time,
                Color.FromArgb(resultColorArgb),
                toolTip,
                message);
        }

        return results;
    }

    /// <summary>
    /// 保存工位通信配置。
    /// </summary>
    public void SaveStationConfig(int stationNo, string ip, int port, bool enabled)
    {
        EnsureInitialized();

        using SqliteConnection connection = CreateOpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO MeterTestStationConfig
            (
                StationNo,
                Ip,
                Port,
                Enabled,
                UpdatedAt
            )
            VALUES
            (
                $StationNo,
                $Ip,
                $Port,
                $Enabled,
                $UpdatedAt
            )
            ON CONFLICT(StationNo)
            DO UPDATE SET
                Ip = excluded.Ip,
                Port = excluded.Port,
                Enabled = excluded.Enabled,
                UpdatedAt = excluded.UpdatedAt;
            """;

        AddParameter(command, "$StationNo", stationNo);
        AddParameter(command, "$Ip", ip);
        AddParameter(command, "$Port", port);
        AddParameter(command, "$Enabled", enabled ? 1 : 0);
        AddParameter(command, "$UpdatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// 保存控制 PCB 配置。
    /// </summary>
    public void SaveControlPcbConfig(MeterTestControlPcbGroup group)
    {
        EnsureInitialized();

        using SqliteConnection connection = CreateOpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO MeterTestControlPcbConfig
            (
                Name,
                Ip,
                Port,
                ProtocolVersion,
                StationStart,
                StationEnd,
                MeterAddressStart,
                Enabled,
                UpdatedAt
            )
            VALUES
            (
                $Name,
                $Ip,
                $Port,
                $ProtocolVersion,
                $StationStart,
                $StationEnd,
                $MeterAddressStart,
                $Enabled,
                $UpdatedAt
            )
            ON CONFLICT(Name)
            DO UPDATE SET
                Ip = excluded.Ip,
                Port = excluded.Port,
                ProtocolVersion = excluded.ProtocolVersion,
                StationStart = excluded.StationStart,
                StationEnd = excluded.StationEnd,
                MeterAddressStart = excluded.MeterAddressStart,
                Enabled = excluded.Enabled,
                UpdatedAt = excluded.UpdatedAt;
            """;

        AddParameter(command, "$Name", group.Name);
        AddParameter(command, "$Ip", group.Ip);
        AddParameter(command, "$Port", group.Port);
        AddParameter(command, "$ProtocolVersion", group.ProtocolVersion);
        AddParameter(command, "$StationStart", group.StationStart);
        AddParameter(command, "$StationEnd", group.StationEnd);
        AddParameter(command, "$MeterAddressStart", group.MeterAddressStart);
        AddParameter(command, "$Enabled", group.Enabled ? 1 : 0);
        AddParameter(command, "$UpdatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// 读取或创建 1-N 工位的电表档案默认数据。
    /// </summary>
    public Dictionary<int, MeterArchiveData> LoadOrCreateMeterArchives(int stationCount)
    {
        EnsureInitialized();

        using SqliteConnection connection = CreateOpenConnection();
        using SqliteTransaction transaction = connection.BeginTransaction();

        for (int stationNo = 1; stationNo <= stationCount; stationNo++)
        {
            InsertDefaultMeterArchiveIfMissing(connection, transaction, stationNo);
        }

        transaction.Commit();

        Dictionary<int, MeterArchiveData> archives = new();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                StationNo,
                MeterType,
                AccessMode,
                Voltage,
                Current,
                ActiveClass,
                ActiveConstant,
                ReactiveClass,
                ReactiveConstant,
                MeterAddress
            FROM MeterTestMeterArchive
            ORDER BY StationNo;
            """;

        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            MeterArchiveData archive = ReadMeterArchive(reader);
            archives[archive.StationNo] = archive;
        }

        return archives;
    }

    /// <summary>
    /// 保存单个工位的电表档案。
    /// </summary>
    public void SaveMeterArchive(MeterArchiveData archive)
    {
        EnsureInitialized();

        using SqliteConnection connection = CreateOpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO MeterTestMeterArchive
            (
                StationNo,
                MeterType,
                AccessMode,
                Voltage,
                Current,
                ActiveClass,
                ActiveConstant,
                ReactiveClass,
                ReactiveConstant,
                MeterAddress,
                UpdatedAt
            )
            VALUES
            (
                $StationNo,
                $MeterType,
                $AccessMode,
                $Voltage,
                $Current,
                $ActiveClass,
                $ActiveConstant,
                $ReactiveClass,
                $ReactiveConstant,
                $MeterAddress,
                $UpdatedAt
            )
            ON CONFLICT(StationNo)
            DO UPDATE SET
                MeterType = excluded.MeterType,
                AccessMode = excluded.AccessMode,
                Voltage = excluded.Voltage,
                Current = excluded.Current,
                ActiveClass = excluded.ActiveClass,
                ActiveConstant = excluded.ActiveConstant,
                ReactiveClass = excluded.ReactiveClass,
                ReactiveConstant = excluded.ReactiveConstant,
                MeterAddress = excluded.MeterAddress,
                UpdatedAt = excluded.UpdatedAt;
            """;

        AddMeterArchiveParameters(command, archive);
        command.ExecuteNonQuery();
    }

    private static string BuildDatabasePath()
    {
        string baseDirectory = AppContext.BaseDirectory;
        return Path.Combine(baseDirectory, RootFolderName, DataFolderName, DatabaseFileName);
    }

    private SqliteConnection CreateOpenConnection()
    {
        SqliteConnection connection = new($"Data Source={databasePath};Mode=ReadWriteCreate;Cache=Shared");
        connection.Open();
        return connection;
    }

    private static void CreateSchema(SqliteConnection connection)
    {
        string[] statements =
        {
            """
            CREATE TABLE IF NOT EXISTS MeterTestRun
            (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                RunId TEXT NOT NULL,
                SchemeName TEXT NOT NULL,
                StartedAt TEXT NOT NULL,
                EndedAt TEXT,
                Status TEXT NOT NULL,
                Remark TEXT
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS MeterTestStationResult
            (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                RunId TEXT NOT NULL,
                SchemeName TEXT NOT NULL,
                TestItemName TEXT NOT NULL,
                TestSubItemName TEXT NOT NULL,
                StationNo INTEGER NOT NULL,
                TestContent TEXT NOT NULL,
                MeterAddress TEXT NOT NULL,
                Result TEXT NOT NULL,
                ResultTime TEXT NOT NULL,
                ResultTimeText TEXT NOT NULL,
                ToolTip TEXT NOT NULL,
                Message TEXT NOT NULL,
                ResultColorArgb INTEGER NOT NULL,
                UpdatedAt TEXT NOT NULL,
                UNIQUE (SchemeName, TestItemName, TestSubItemName, StationNo)
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS MeterTestStationConfig
            (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                StationNo INTEGER NOT NULL UNIQUE,
                Ip TEXT NOT NULL,
                Port INTEGER NOT NULL,
                Enabled INTEGER NOT NULL,
                UpdatedAt TEXT NOT NULL
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS MeterTestControlPcbConfig
            (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL UNIQUE,
                Ip TEXT NOT NULL,
                Port INTEGER NOT NULL,
                ProtocolVersion TEXT NOT NULL,
                StationStart INTEGER NOT NULL,
                StationEnd INTEGER NOT NULL,
                MeterAddressStart INTEGER NOT NULL,
                Enabled INTEGER NOT NULL,
                UpdatedAt TEXT NOT NULL
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS MeterTestMeterArchive
            (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                StationNo INTEGER NOT NULL UNIQUE,
                MeterType TEXT NOT NULL,
                AccessMode TEXT NOT NULL,
                Voltage TEXT NOT NULL,
                Current TEXT NOT NULL,
                ActiveClass TEXT NOT NULL,
                ActiveConstant TEXT NOT NULL,
                ReactiveClass TEXT NOT NULL,
                ReactiveConstant TEXT NOT NULL,
                MeterAddress TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );
            """
        };

        foreach (string statement in statements)
        {
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = statement;
            command.ExecuteNonQuery();
        }
    }

    private static void AddParameter(SqliteCommand command, string name, object? value)
    {
        SqliteParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private static void InsertDefaultMeterArchiveIfMissing(
        SqliteConnection connection,
        SqliteTransaction transaction,
        int stationNo)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            INSERT OR IGNORE INTO MeterTestMeterArchive
            (
                StationNo,
                MeterType,
                AccessMode,
                Voltage,
                Current,
                ActiveClass,
                ActiveConstant,
                ReactiveClass,
                ReactiveConstant,
                MeterAddress,
                UpdatedAt
            )
            VALUES
            (
                $StationNo,
                '单相',
                '直接式',
                '220V',
                '5A',
                'A',
                '1000',
                '2.0',
                '1000',
                '',
                $UpdatedAt
            );
            """;

        AddParameter(command, "$StationNo", stationNo);
        AddParameter(command, "$UpdatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        command.ExecuteNonQuery();
    }

    private static MeterArchiveData ReadMeterArchive(SqliteDataReader reader)
    {
        return new MeterArchiveData(
            reader.GetInt32(0),
            reader.IsDBNull(1) ? "单相" : reader.GetString(1),
            reader.IsDBNull(2) ? "直接式" : reader.GetString(2),
            reader.IsDBNull(3) ? "220V" : reader.GetString(3),
            reader.IsDBNull(4) ? "5A" : reader.GetString(4),
            reader.IsDBNull(5) ? "A" : reader.GetString(5),
            reader.IsDBNull(6) ? "1000" : reader.GetString(6),
            reader.IsDBNull(7) ? "2.0" : reader.GetString(7),
            reader.IsDBNull(8) ? "1000" : reader.GetString(8),
            reader.IsDBNull(9) ? string.Empty : reader.GetString(9));
    }

    private static void AddMeterArchiveParameters(SqliteCommand command, MeterArchiveData archive)
    {
        AddParameter(command, "$StationNo", archive.StationNo);
        AddParameter(command, "$MeterType", archive.MeterType);
        AddParameter(command, "$AccessMode", archive.AccessMode);
        AddParameter(command, "$Voltage", archive.Voltage);
        AddParameter(command, "$Current", archive.Current);
        AddParameter(command, "$ActiveClass", archive.ActiveClass);
        AddParameter(command, "$ActiveConstant", archive.ActiveConstant);
        AddParameter(command, "$ReactiveClass", archive.ReactiveClass);
        AddParameter(command, "$ReactiveConstant", archive.ReactiveConstant);
        AddParameter(command, "$MeterAddress", archive.MeterAddress);
        AddParameter(command, "$UpdatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
    }
}

/// <summary>
/// MeterTest 工位展示状态的持久化数据。
/// </summary>
public sealed record StationDisplayStateData(
    string TestContent,
    string MeterAddress,
    string Result,
    string Time,
    Color ResultColor,
    string ToolTip,
    string Message);

/// <summary>
/// 单个工位的电表档案数据。
/// </summary>
public sealed record MeterArchiveData(
    int StationNo,
    string MeterType,
    string AccessMode,
    string Voltage,
    string Current,
    string ActiveClass,
    string ActiveConstant,
    string ReactiveClass,
    string ReactiveConstant,
    string MeterAddress);
