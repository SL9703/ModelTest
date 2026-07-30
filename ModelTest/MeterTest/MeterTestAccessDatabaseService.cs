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

    /// <summary>创建 SQLite 本地数据库服务；未指定路径时使用运行目录下的 MeterTest/data。</summary>
    public MeterTestAccessDatabaseService(string? customDatabasePath = null)
    {
        databasePath = string.IsNullOrWhiteSpace(customDatabasePath)
            ? BuildDatabasePath()
            : Path.GetFullPath(customDatabasePath);
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
    /// 一次读取全部方案、测试项、测试小项的工位结果。
    /// 方案树状态图标使用该接口批量恢复历史结果，避免为每个树节点重复打开数据库连接。
    /// </summary>
    public IReadOnlyList<MeterTestStoredStationResultData> LoadAllStationResults()
    {
        EnsureInitialized();

        List<MeterTestStoredStationResultData> results = new();
        using SqliteConnection connection = CreateOpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                SchemeName,
                TestItemName,
                TestSubItemName,
                StationNo,
                TestContent,
                MeterAddress,
                Result,
                ResultTimeText,
                ToolTip,
                Message,
                ResultColorArgb
            FROM MeterTestStationResult
            ORDER BY SchemeName, TestItemName, TestSubItemName, StationNo;
            """;

        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            int resultColorArgb = reader.IsDBNull(10)
                ? Color.FromArgb(31, 41, 55).ToArgb()
                : reader.GetInt32(10);
            StationDisplayStateData state = new(
                reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                Color.FromArgb(resultColorArgb),
                reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                reader.IsDBNull(9) ? string.Empty : reader.GetString(9));

            results.Add(new MeterTestStoredStationResultData(
                reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                reader.GetInt32(3),
                state));
        }

        return results;
    }

    /// <summary>
    /// 清除指定方案的运行态工位结论。
    /// 历史任务快照保存在 MeterTestResultTask/Station/Detail 中，不受该操作影响。
    /// </summary>
    public void ClearStationResultsForScheme(string schemeName)
    {
        if (string.IsNullOrWhiteSpace(schemeName))
            return;

        EnsureInitialized();
        using SqliteConnection connection = CreateOpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "DELETE FROM MeterTestStationResult WHERE SchemeName = $SchemeName;";
        AddParameter(command, "$SchemeName", schemeName.Trim());
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// 保存一条方案运行期测量值。
    /// 用户分别执行日计时、起动、潜动和基本误差时，数值不再仅保存在内存中。
    /// 同方案、工位、测试小项、测量名称和序号重复写入时，以最新结果覆盖。
    /// </summary>
    public void SaveRuntimeMeasurement(
        string runId,
        string schemeName,
        MeterTestMeasurementData measurement)
    {
        ArgumentNullException.ThrowIfNull(measurement);
        EnsureInitialized();

        using SqliteConnection connection = CreateOpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO MeterTestRuntimeMeasurement
            (
                RunId, SchemeName, StationNo, TestItemName, TestSubItemName,
                MeasurementName, SequenceNo, NumericValue, ValueText, Unit,
                AverageValue, LimitText, UpdatedAt
            )
            VALUES
            (
                $RunId, $SchemeName, $StationNo, $TestItemName, $TestSubItemName,
                $MeasurementName, $SequenceNo, $NumericValue, $ValueText, $Unit,
                $AverageValue, $LimitText, $UpdatedAt
            )
            ON CONFLICT
            (
                SchemeName, StationNo, TestItemName, TestSubItemName,
                MeasurementName, SequenceNo
            )
            DO UPDATE SET
                RunId = excluded.RunId,
                NumericValue = excluded.NumericValue,
                ValueText = excluded.ValueText,
                Unit = excluded.Unit,
                AverageValue = excluded.AverageValue,
                LimitText = excluded.LimitText,
                UpdatedAt = excluded.UpdatedAt;
            """;
        AddParameter(command, "$RunId", runId?.Trim() ?? string.Empty);
        AddParameter(command, "$SchemeName", schemeName?.Trim() ?? string.Empty);
        AddParameter(command, "$StationNo", measurement.StationNo);
        AddParameter(command, "$TestItemName", measurement.TestItemName);
        AddParameter(command, "$TestSubItemName", measurement.TestSubItemName);
        AddParameter(command, "$MeasurementName", measurement.MeasurementName);
        AddParameter(command, "$SequenceNo", measurement.SequenceNo);
        AddParameter(command, "$NumericValue", measurement.NumericValue);
        AddParameter(command, "$ValueText", measurement.ValueText);
        AddParameter(command, "$Unit", measurement.Unit);
        AddParameter(command, "$AverageValue", measurement.AverageValue);
        AddParameter(command, "$LimitText", measurement.LimitText);
        AddParameter(command, "$UpdatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        command.ExecuteNonQuery();
    }

    /// <summary>读取指定方案下各次分项执行后保留的最新测量值。</summary>
    public IReadOnlyList<MeterTestMeasurementData> LoadRuntimeMeasurementsForScheme(string schemeName)
    {
        return LoadRuntimeMeasurements("SchemeName = $Filter", schemeName);
    }

    /// <summary>读取指定 RunId 下的测量值，完整方案自动保存时用于隔离历史数据。</summary>
    public IReadOnlyList<MeterTestMeasurementData> LoadRuntimeMeasurementsByRunId(string runId)
    {
        return LoadRuntimeMeasurements("RunId = $Filter", runId);
    }

    /// <summary>清除指定方案的运行期测量值，不影响已冻结的历史任务。</summary>
    public void ClearRuntimeMeasurementsForScheme(string schemeName)
    {
        if (string.IsNullOrWhiteSpace(schemeName))
            return;

        EnsureInitialized();
        using SqliteConnection connection = CreateOpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "DELETE FROM MeterTestRuntimeMeasurement WHERE SchemeName = $SchemeName;";
        AddParameter(command, "$SchemeName", schemeName.Trim());
        command.ExecuteNonQuery();
    }

    /// <summary>执行运行期测量值查询，过滤条件只由本类内部固定调用。</summary>
    private IReadOnlyList<MeterTestMeasurementData> LoadRuntimeMeasurements(string filterSql, string filterValue)
    {
        EnsureInitialized();
        List<MeterTestMeasurementData> measurements = new();
        using SqliteConnection connection = CreateOpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            $"""
            SELECT StationNo, TestItemName, TestSubItemName, MeasurementName,
                   SequenceNo, NumericValue, ValueText, Unit, AverageValue, LimitText
            FROM MeterTestRuntimeMeasurement
            WHERE {filterSql}
            ORDER BY StationNo, TestItemName, TestSubItemName, MeasurementName, SequenceNo;
            """;
        AddParameter(command, "$Filter", filterValue?.Trim() ?? string.Empty);
        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            measurements.Add(new MeterTestMeasurementData(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetInt32(4),
                reader.GetDouble(5),
                reader.GetString(6),
                reader.GetString(7),
                reader.IsDBNull(8) ? null : reader.GetDouble(8),
                reader.GetString(9)));
        }

        return measurements;
    }

    /// <summary>
    /// 读取指定运行编号的所有工位小项结果，用于生成一次不可变的测试任务快照。
    /// </summary>
    public IReadOnlyList<MeterTestStoredStationResultData> LoadStationResultsByRunId(string runId)
    {
        EnsureInitialized();
        List<MeterTestStoredStationResultData> results = new();
        using SqliteConnection connection = CreateOpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                SchemeName,
                TestItemName,
                TestSubItemName,
                StationNo,
                TestContent,
                MeterAddress,
                Result,
                ResultTimeText,
                ToolTip,
                Message,
                ResultColorArgb
            FROM MeterTestStationResult
            WHERE RunId = $RunId
            ORDER BY StationNo, TestItemName, TestSubItemName;
            """;
        AddParameter(command, "$RunId", runId);

        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            int resultColorArgb = reader.IsDBNull(10)
                ? Color.FromArgb(31, 41, 55).ToArgb()
                : reader.GetInt32(10);
            StationDisplayStateData state = new(
                reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                Color.FromArgb(resultColorArgb),
                reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                reader.IsDBNull(9) ? string.Empty : reader.GetString(9));
            results.Add(new MeterTestStoredStationResultData(
                reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                reader.GetInt32(3),
                state));
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
    /// 读取或创建条形码截取配置。
    /// 默认从 0-based 第 8 位开始，到第 20 位结束。
    /// </summary>
    public MeterTestAssetBarcodeSettingData LoadOrCreateAssetBarcodeSetting()
    {
        EnsureInitialized();

        using SqliteConnection connection = CreateOpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT BarcodeStartIndex, BarcodeEndIndex, RuleType,
                   Rule2FirstStart, Rule2FirstLength, Rule2SecondStart, Rule2SecondLength
            FROM MeterTestAssetBarcodeSetting
            WHERE Id = 1;
            """;

        using SqliteDataReader reader = command.ExecuteReader();
        if (reader.Read())
        {
            return new MeterTestAssetBarcodeSettingData(
                reader.IsDBNull(0) ? 8 : reader.GetInt32(0),
                reader.IsDBNull(1) ? 20 : reader.GetInt32(1),
                reader.IsDBNull(2) ? MeterTestBarcodeExtractor.Rule1Range : reader.GetString(2),
                reader.IsDBNull(3) ? 6 : reader.GetInt32(3),
                reader.IsDBNull(4) ? 2 : reader.GetInt32(4),
                reader.IsDBNull(5) ? 10 : reader.GetInt32(5),
                reader.IsDBNull(6) ? 10 : reader.GetInt32(6));
        }

        SaveAssetBarcodeSetting(8, 20, MeterTestBarcodeExtractor.Rule1Range, 6, 2, 10, 10);
        return new MeterTestAssetBarcodeSettingData(8, 20, MeterTestBarcodeExtractor.Rule1Range, 6, 2, 10, 10);
    }

    /// <summary>
    /// 保存条形码截取配置。
    /// </summary>
    public void SaveAssetBarcodeSetting(
        int barcodeStartIndex,
        int barcodeEndIndex,
        string ruleType,
        int rule2FirstStart,
        int rule2FirstLength,
        int rule2SecondStart,
        int rule2SecondLength)
    {
        EnsureInitialized();

        using SqliteConnection connection = CreateOpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO MeterTestAssetBarcodeSetting
            (
                Id,
                BarcodeStartIndex,
                BarcodeEndIndex,
                RuleType,
                Rule2FirstStart,
                Rule2FirstLength,
                Rule2SecondStart,
                Rule2SecondLength,
                UpdatedAt
            )
            VALUES
            (
                1,
                $BarcodeStartIndex,
                $BarcodeEndIndex,
                $RuleType,
                $Rule2FirstStart,
                $Rule2FirstLength,
                $Rule2SecondStart,
                $Rule2SecondLength,
                $UpdatedAt
            )
            ON CONFLICT(Id)
            DO UPDATE SET
                BarcodeStartIndex = excluded.BarcodeStartIndex,
                BarcodeEndIndex = excluded.BarcodeEndIndex,
                RuleType = excluded.RuleType,
                Rule2FirstStart = excluded.Rule2FirstStart,
                Rule2FirstLength = excluded.Rule2FirstLength,
                Rule2SecondStart = excluded.Rule2SecondStart,
                Rule2SecondLength = excluded.Rule2SecondLength,
                UpdatedAt = excluded.UpdatedAt;
            """;

        AddParameter(command, "$BarcodeStartIndex", barcodeStartIndex);
        AddParameter(command, "$BarcodeEndIndex", barcodeEndIndex);
        AddParameter(command, "$RuleType", string.IsNullOrWhiteSpace(ruleType) ? MeterTestBarcodeExtractor.Rule1Range : ruleType);
        AddParameter(command, "$Rule2FirstStart", rule2FirstStart);
        AddParameter(command, "$Rule2FirstLength", rule2FirstLength);
        AddParameter(command, "$Rule2SecondStart", rule2SecondStart);
        AddParameter(command, "$Rule2SecondLength", rule2SecondLength);
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
                CurrentSpecification,
                ActiveClass,
                ActiveConstant,
                ReactiveClass,
                ReactiveConstant,
                Barcode,
                MeterAddress,
                BaudRate
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
                CurrentSpecification,
                ActiveClass,
                ActiveConstant,
                ReactiveClass,
                ReactiveConstant,
                Barcode,
                MeterAddress,
                BaudRate,
                UpdatedAt
            )
            VALUES
            (
                $StationNo,
                $MeterType,
                $AccessMode,
                $Voltage,
                $Current,
                $CurrentSpecification,
                $ActiveClass,
                $ActiveConstant,
                $ReactiveClass,
                $ReactiveConstant,
                $Barcode,
                $MeterAddress,
                $BaudRate,
                $UpdatedAt
            )
            ON CONFLICT(StationNo)
            DO UPDATE SET
                MeterType = excluded.MeterType,
                AccessMode = excluded.AccessMode,
                Voltage = excluded.Voltage,
                Current = excluded.Current,
                CurrentSpecification = excluded.CurrentSpecification,
                ActiveClass = excluded.ActiveClass,
                ActiveConstant = excluded.ActiveConstant,
                ReactiveClass = excluded.ReactiveClass,
                ReactiveConstant = excluded.ReactiveConstant,
                Barcode = excluded.Barcode,
                MeterAddress = excluded.MeterAddress,
                BaudRate = excluded.BaudRate,
                UpdatedAt = excluded.UpdatedAt;
            """;

        AddMeterArchiveParameters(command, archive);
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// 保存一次完整测试任务快照。
    /// 同一 RunId 重复保存时覆盖快照内容，支持自动保存后再手动保存。
    /// </summary>
    public long SaveTestResultTask(MeterTestResultTaskSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        EnsureInitialized();

        using SqliteConnection connection = CreateOpenConnection();
        using SqliteTransaction transaction = connection.BeginTransaction();
        string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

        using (SqliteCommand command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText =
                """
                INSERT INTO MeterTestResultTask
                (
                    RunId, SchemeName, StartedAt, EndedAt, Status, SaveMode,
                    StationCount, ResultSummary, CreatedAt, UpdatedAt
                )
                VALUES
                (
                    $RunId, $SchemeName, $StartedAt, $EndedAt, $Status, $SaveMode,
                    $StationCount, $ResultSummary, $CreatedAt, $UpdatedAt
                )
                ON CONFLICT(RunId)
                DO UPDATE SET
                    SchemeName = excluded.SchemeName,
                    StartedAt = excluded.StartedAt,
                    EndedAt = excluded.EndedAt,
                    Status = excluded.Status,
                    SaveMode = excluded.SaveMode,
                    StationCount = excluded.StationCount,
                    ResultSummary = excluded.ResultSummary,
                    UpdatedAt = excluded.UpdatedAt;
                """;
            AddParameter(command, "$RunId", snapshot.RunId);
            AddParameter(command, "$SchemeName", snapshot.SchemeName);
            AddParameter(command, "$StartedAt", snapshot.StartedAt.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            AddParameter(command, "$EndedAt", snapshot.EndedAt.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            AddParameter(command, "$Status", snapshot.Status);
            AddParameter(command, "$SaveMode", snapshot.SaveMode);
            AddParameter(command, "$StationCount", snapshot.Stations.Count);
            AddParameter(command, "$ResultSummary", snapshot.ResultSummary);
            AddParameter(command, "$CreatedAt", now);
            AddParameter(command, "$UpdatedAt", now);
            command.ExecuteNonQuery();
        }

        long taskId;
        using (SqliteCommand idCommand = connection.CreateCommand())
        {
            idCommand.Transaction = transaction;
            idCommand.CommandText = "SELECT Id FROM MeterTestResultTask WHERE RunId = $RunId;";
            AddParameter(idCommand, "$RunId", snapshot.RunId);
            taskId = Convert.ToInt64(idCommand.ExecuteScalar());
        }

        DeleteTaskChildren(connection, transaction, taskId);
        foreach (MeterTestResultStationData station in snapshot.Stations)
        {
            InsertResultStation(connection, transaction, taskId, station);
        }

        foreach (MeterTestResultDetailData detail in snapshot.Details)
        {
            InsertResultDetail(connection, transaction, taskId, detail);
        }

        transaction.Commit();
        return taskId;
    }

    /// <summary>按时间倒序读取历史测试任务。</summary>
    public IReadOnlyList<MeterTestResultTaskData> LoadTestResultTasks()
    {
        EnsureInitialized();
        List<MeterTestResultTaskData> tasks = new();
        using SqliteConnection connection = CreateOpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Id, RunId, SchemeName, StartedAt, EndedAt, Status, SaveMode,
                   StationCount, ResultSummary
            FROM MeterTestResultTask
            ORDER BY EndedAt DESC, Id DESC;
            """;
        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            tasks.Add(new MeterTestResultTaskData(
                reader.GetInt64(0),
                reader.GetString(1),
                reader.GetString(2),
                ParseDatabaseDateTime(reader.GetString(3)),
                ParseDatabaseDateTime(reader.GetString(4)),
                reader.GetString(5),
                reader.GetString(6),
                reader.GetInt32(7),
                reader.GetString(8)));
        }

        return tasks;
    }

    /// <summary>读取指定任务的工位电表快照。</summary>
    public IReadOnlyList<MeterTestResultStationData> LoadTestResultStations(long taskId)
    {
        EnsureInitialized();
        List<MeterTestResultStationData> stations = new();
        using SqliteConnection connection = CreateOpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT StationNo, Barcode, MeterAddress, MeterType, AccessMode, Voltage,
                   BasicCurrent, CurrentSpecification, ActiveClass, ActiveConstant,
                   ReactiveClass, ReactiveConstant, OverallResult, CompletedAt
            FROM MeterTestResultStation
            WHERE TaskId = $TaskId
            ORDER BY StationNo;
            """;
        AddParameter(command, "$TaskId", taskId);
        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            stations.Add(new MeterTestResultStationData(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetString(5),
                reader.GetString(6),
                reader.GetString(7),
                reader.GetString(8),
                reader.GetString(9),
                reader.GetString(10),
                reader.GetString(11),
                reader.GetString(12),
                ParseDatabaseDateTime(reader.GetString(13))));
        }

        return stations;
    }

    /// <summary>读取指定任务、指定工位的全部测试小项与数值明细。</summary>
    public IReadOnlyList<MeterTestResultDetailData> LoadTestResultDetails(long taskId, int? stationNo = null)
    {
        EnsureInitialized();
        List<MeterTestResultDetailData> details = new();
        using SqliteConnection connection = CreateOpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT StationNo, TestItemName, TestSubItemName, Result, ResultTimeText,
                   Message, MeasurementName, SequenceNo, ValueText, NumericValue,
                   Unit, AverageValue, LimitText
            FROM MeterTestResultDetail
            WHERE TaskId = $TaskId
              AND ($StationNo IS NULL OR StationNo = $StationNo)
            ORDER BY StationNo, Id;
            """;
        AddParameter(command, "$TaskId", taskId);
        AddParameter(command, "$StationNo", stationNo);
        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            details.Add(new MeterTestResultDetailData(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetString(5),
                reader.GetString(6),
                reader.GetInt32(7),
                reader.GetString(8),
                reader.IsDBNull(9) ? null : reader.GetDouble(9),
                reader.GetString(10),
                reader.IsDBNull(11) ? null : reader.GetDouble(11),
                reader.GetString(12)));
        }

        return details;
    }

    /// <summary>在同一事务内删除指定测试任务已有的工位和明细，供覆盖保存使用。</summary>
    private static void DeleteTaskChildren(SqliteConnection connection, SqliteTransaction transaction, long taskId)
    {
        foreach (string tableName in new[] { "MeterTestResultDetail", "MeterTestResultStation" })
        {
            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $"DELETE FROM {tableName} WHERE TaskId = $TaskId;";
            AddParameter(command, "$TaskId", taskId);
            command.ExecuteNonQuery();
        }
    }

    /// <summary>将单个工位的资产快照和汇总结论写入测试结果工位表。</summary>
    private static void InsertResultStation(
        SqliteConnection connection,
        SqliteTransaction transaction,
        long taskId,
        MeterTestResultStationData station)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            INSERT INTO MeterTestResultStation
            (
                TaskId, StationNo, Barcode, MeterAddress, MeterType, AccessMode, Voltage,
                BasicCurrent, CurrentSpecification, ActiveClass, ActiveConstant,
                ReactiveClass, ReactiveConstant, OverallResult, CompletedAt
            )
            VALUES
            (
                $TaskId, $StationNo, $Barcode, $MeterAddress, $MeterType, $AccessMode, $Voltage,
                $BasicCurrent, $CurrentSpecification, $ActiveClass, $ActiveConstant,
                $ReactiveClass, $ReactiveConstant, $OverallResult, $CompletedAt
            );
            """;
        AddParameter(command, "$TaskId", taskId);
        AddParameter(command, "$StationNo", station.StationNo);
        AddParameter(command, "$Barcode", station.Barcode);
        AddParameter(command, "$MeterAddress", station.MeterAddress);
        AddParameter(command, "$MeterType", station.MeterType);
        AddParameter(command, "$AccessMode", station.AccessMode);
        AddParameter(command, "$Voltage", station.Voltage);
        AddParameter(command, "$BasicCurrent", station.BasicCurrent);
        AddParameter(command, "$CurrentSpecification", station.CurrentSpecification);
        AddParameter(command, "$ActiveClass", station.ActiveClass);
        AddParameter(command, "$ActiveConstant", station.ActiveConstant);
        AddParameter(command, "$ReactiveClass", station.ReactiveClass);
        AddParameter(command, "$ReactiveConstant", station.ReactiveConstant);
        AddParameter(command, "$OverallResult", station.OverallResult);
        AddParameter(command, "$CompletedAt", station.CompletedAt.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        command.ExecuteNonQuery();
    }

    /// <summary>将一个测试小项的结果、测量值和允许区间写入结果明细表。</summary>
    private static void InsertResultDetail(
        SqliteConnection connection,
        SqliteTransaction transaction,
        long taskId,
        MeterTestResultDetailData detail)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            INSERT INTO MeterTestResultDetail
            (
                TaskId, StationNo, TestItemName, TestSubItemName, Result, ResultTimeText,
                Message, MeasurementName, SequenceNo, ValueText, NumericValue, Unit,
                AverageValue, LimitText
            )
            VALUES
            (
                $TaskId, $StationNo, $TestItemName, $TestSubItemName, $Result, $ResultTimeText,
                $Message, $MeasurementName, $SequenceNo, $ValueText, $NumericValue, $Unit,
                $AverageValue, $LimitText
            );
            """;
        AddParameter(command, "$TaskId", taskId);
        AddParameter(command, "$StationNo", detail.StationNo);
        AddParameter(command, "$TestItemName", detail.TestItemName);
        AddParameter(command, "$TestSubItemName", detail.TestSubItemName);
        AddParameter(command, "$Result", detail.Result);
        AddParameter(command, "$ResultTimeText", detail.ResultTimeText);
        AddParameter(command, "$Message", detail.Message);
        AddParameter(command, "$MeasurementName", detail.MeasurementName);
        AddParameter(command, "$SequenceNo", detail.SequenceNo);
        AddParameter(command, "$ValueText", detail.ValueText);
        AddParameter(command, "$NumericValue", detail.NumericValue);
        AddParameter(command, "$Unit", detail.Unit);
        AddParameter(command, "$AverageValue", detail.AverageValue);
        AddParameter(command, "$LimitText", detail.LimitText);
        command.ExecuteNonQuery();
    }

    /// <summary>按本数据库统一格式解析时间；异常文本返回 <see cref="DateTime.MinValue"/>。</summary>
    private static DateTime ParseDatabaseDateTime(string value)
    {
        return DateTime.TryParse(value, out DateTime parsed) ? parsed : DateTime.MinValue;
    }

    /// <summary>生成运行目录下 MeterTest/data/MeterTest.db 的默认数据库路径。</summary>
    private static string BuildDatabasePath()
    {
        string baseDirectory = AppContext.BaseDirectory;
        return Path.Combine(baseDirectory, RootFolderName, DataFolderName, DatabaseFileName);
    }

    /// <summary>创建并打开启用外键约束的 SQLite 连接。</summary>
    private SqliteConnection CreateOpenConnection()
    {
        SqliteConnection connection = new($"Data Source={databasePath};Mode=ReadWriteCreate;Cache=Shared");
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = ON;";
        command.ExecuteNonQuery();
        return connection;
    }

    /// <summary>幂等创建 MeterTest 所需全部表、索引和字段迁移。</summary>
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
            CREATE TABLE IF NOT EXISTS MeterTestRuntimeMeasurement
            (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                RunId TEXT NOT NULL,
                SchemeName TEXT NOT NULL,
                StationNo INTEGER NOT NULL,
                TestItemName TEXT NOT NULL,
                TestSubItemName TEXT NOT NULL,
                MeasurementName TEXT NOT NULL,
                SequenceNo INTEGER NOT NULL,
                NumericValue REAL NOT NULL,
                ValueText TEXT NOT NULL,
                Unit TEXT NOT NULL,
                AverageValue REAL,
                LimitText TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                UNIQUE
                (
                    SchemeName, StationNo, TestItemName, TestSubItemName,
                    MeasurementName, SequenceNo
                )
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
            CREATE TABLE IF NOT EXISTS MeterTestAssetBarcodeSetting
            (
                Id INTEGER PRIMARY KEY CHECK (Id = 1),
                BarcodeStartIndex INTEGER NOT NULL,
                BarcodeEndIndex INTEGER NOT NULL,
                RuleType TEXT NOT NULL DEFAULT 'Rule1Range',
                Rule2FirstStart INTEGER NOT NULL DEFAULT 6,
                Rule2FirstLength INTEGER NOT NULL DEFAULT 2,
                Rule2SecondStart INTEGER NOT NULL DEFAULT 10,
                Rule2SecondLength INTEGER NOT NULL DEFAULT 10,
                UpdatedAt TEXT NOT NULL
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS MeterTestAssetOption
            (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Category TEXT NOT NULL,
                Scope TEXT NOT NULL DEFAULT '',
                Value TEXT NOT NULL,
                SortOrder INTEGER NOT NULL,
                IsDefault INTEGER NOT NULL DEFAULT 0,
                Enabled INTEGER NOT NULL DEFAULT 1,
                UNIQUE (Category, Scope, Value)
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS MeterTestPowerFactorAngle
            (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Direction TEXT NOT NULL,
                PowerFactor TEXT NOT NULL,
                LoadType TEXT NOT NULL,
                CurrentAngle REAL NOT NULL,
                Description TEXT NOT NULL DEFAULT '',
                Enabled INTEGER NOT NULL DEFAULT 1,
                UpdatedAt TEXT NOT NULL,
                UNIQUE (Direction, PowerFactor)
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
                CurrentSpecification TEXT NOT NULL DEFAULT '0.25-0.5(60)A',
                ActiveClass TEXT NOT NULL,
                ActiveConstant TEXT NOT NULL,
                ReactiveClass TEXT NOT NULL,
                ReactiveConstant TEXT NOT NULL,
                Barcode TEXT NOT NULL DEFAULT '',
                MeterAddress TEXT NOT NULL,
                BaudRate TEXT NOT NULL DEFAULT '9600-8-E-1',
                UpdatedAt TEXT NOT NULL
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS MeterTestResultTask
            (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                RunId TEXT NOT NULL UNIQUE,
                SchemeName TEXT NOT NULL,
                StartedAt TEXT NOT NULL,
                EndedAt TEXT NOT NULL,
                Status TEXT NOT NULL,
                SaveMode TEXT NOT NULL,
                StationCount INTEGER NOT NULL,
                ResultSummary TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS MeterTestResultStation
            (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                TaskId INTEGER NOT NULL,
                StationNo INTEGER NOT NULL,
                Barcode TEXT NOT NULL,
                MeterAddress TEXT NOT NULL,
                MeterType TEXT NOT NULL,
                AccessMode TEXT NOT NULL,
                Voltage TEXT NOT NULL,
                BasicCurrent TEXT NOT NULL,
                CurrentSpecification TEXT NOT NULL,
                ActiveClass TEXT NOT NULL,
                ActiveConstant TEXT NOT NULL,
                ReactiveClass TEXT NOT NULL,
                ReactiveConstant TEXT NOT NULL,
                OverallResult TEXT NOT NULL,
                CompletedAt TEXT NOT NULL,
                UNIQUE (TaskId, StationNo),
                FOREIGN KEY (TaskId) REFERENCES MeterTestResultTask(Id) ON DELETE CASCADE
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS MeterTestResultDetail
            (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                TaskId INTEGER NOT NULL,
                StationNo INTEGER NOT NULL,
                TestItemName TEXT NOT NULL,
                TestSubItemName TEXT NOT NULL,
                Result TEXT NOT NULL,
                ResultTimeText TEXT NOT NULL,
                Message TEXT NOT NULL,
                MeasurementName TEXT NOT NULL,
                SequenceNo INTEGER NOT NULL,
                ValueText TEXT NOT NULL,
                NumericValue REAL,
                Unit TEXT NOT NULL,
                AverageValue REAL,
                LimitText TEXT NOT NULL,
                FOREIGN KEY (TaskId) REFERENCES MeterTestResultTask(Id) ON DELETE CASCADE
            );
            """
        };

        foreach (string statement in statements)
        {
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = statement;
            command.ExecuteNonQuery();
        }

        EnsureColumnExists(connection, "MeterTestAssetBarcodeSetting", "BarcodeStartIndex", "INTEGER NOT NULL DEFAULT 8");
        EnsureColumnExists(connection, "MeterTestAssetBarcodeSetting", "BarcodeEndIndex", "INTEGER NOT NULL DEFAULT 20");
        EnsureColumnExists(connection, "MeterTestAssetBarcodeSetting", "RuleType", "TEXT NOT NULL DEFAULT 'Rule1Range'");
        EnsureColumnExists(connection, "MeterTestAssetBarcodeSetting", "Rule2FirstStart", "INTEGER NOT NULL DEFAULT 6");
        EnsureColumnExists(connection, "MeterTestAssetBarcodeSetting", "Rule2FirstLength", "INTEGER NOT NULL DEFAULT 2");
        EnsureColumnExists(connection, "MeterTestAssetBarcodeSetting", "Rule2SecondStart", "INTEGER NOT NULL DEFAULT 10");
        EnsureColumnExists(connection, "MeterTestAssetBarcodeSetting", "Rule2SecondLength", "INTEGER NOT NULL DEFAULT 10");
        EnsureColumnExists(connection, "MeterTestMeterArchive", "Barcode", "TEXT NOT NULL DEFAULT ''");
        EnsureColumnExists(connection, "MeterTestMeterArchive", "BaudRate", "TEXT NOT NULL DEFAULT '9600-8-E-1'");
        EnsureColumnExists(connection, "MeterTestMeterArchive", "CurrentSpecification", "TEXT NOT NULL DEFAULT '0.25-0.5(60)A'");

        using SqliteCommand detailIndexCommand = connection.CreateCommand();
        detailIndexCommand.CommandText =
            "CREATE INDEX IF NOT EXISTS idx_MeterTestResultDetail_TaskStation "
            + "ON MeterTestResultDetail (TaskId, StationNo, TestItemName, TestSubItemName);";
        detailIndexCommand.ExecuteNonQuery();

        // 旧版数据库可能已经存在重复明细；先保留最新一条，再建立唯一索引。
        using SqliteCommand deduplicateDetailsCommand = connection.CreateCommand();
        deduplicateDetailsCommand.CommandText =
            """
            DELETE FROM MeterTestResultDetail
            WHERE Id NOT IN
            (
                SELECT MAX(Id)
                FROM MeterTestResultDetail
                GROUP BY TaskId, StationNo, TestItemName, TestSubItemName,
                         MeasurementName, SequenceNo
            );
            """;
        deduplicateDetailsCommand.ExecuteNonQuery();

        using SqliteCommand uniqueDetailIndexCommand = connection.CreateCommand();
        uniqueDetailIndexCommand.CommandText =
            "CREATE UNIQUE INDEX IF NOT EXISTS ux_MeterTestResultDetail_Key "
            + "ON MeterTestResultDetail "
            + "(TaskId, StationNo, TestItemName, TestSubItemName, MeasurementName, SequenceNo);";
        uniqueDetailIndexCommand.ExecuteNonQuery();
        SeedAssetOptions(connection);
        SeedPowerFactorAngles(connection);
    }

    /// <summary>读取启用的资产下拉候选项；Scope用于区分直接式和互感式。</summary>
    public IReadOnlyList<MeterTestAssetOptionData> LoadAssetOptions(string category, string scope = "")
    {
        EnsureInitialized();
        List<MeterTestAssetOptionData> options = new();
        using SqliteConnection connection = CreateOpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            "SELECT Category, Scope, Value, SortOrder, IsDefault "
            + "FROM MeterTestAssetOption WHERE Category=$Category AND Scope=$Scope AND Enabled=1 "
            + "ORDER BY SortOrder, Id;";
        AddParameter(command, "$Category", category);
        AddParameter(command, "$Scope", scope ?? string.Empty);
        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            options.Add(new MeterTestAssetOptionData(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetInt32(3),
                reader.GetInt32(4) != 0));
        }

        return options;
    }

    /// <summary>在候选项表为空或缺项时写入资产下拉框默认基础数据。</summary>
    private static void SeedAssetOptions(SqliteConnection connection)
    {
        (string Category, string Scope, string Value, int SortOrder, bool IsDefault)[] defaults =
        {
            ("MeterType", "", "单相", 1, true),
            ("MeterType", "", "三相三线", 2, false),
            ("MeterType", "", "三相四线", 3, false),
            ("AccessMode", "", "直接式", 1, true),
            ("AccessMode", "", "互感式", 2, false),
            ("Voltage", "", "220V", 1, true),
            ("BasicCurrent", "", "5A", 1, true),
            ("CurrentSpecification", "Direct", "0.25-0.5(60)A", 1, true),
            ("CurrentSpecification", "Direct", "0.25-0.5(100)A", 2, false),
            ("CurrentSpecification", "Direct", "0.5-1(60)A", 3, false),
            ("CurrentSpecification", "Direct", "0.5-1(100)A", 4, false),
            ("CurrentSpecification", "Transformer", "0.015-0.075(6)A", 1, true),
            ("CurrentSpecification", "Transformer", "0.003-0.015(6)A", 2, false),
            ("ActiveClass", "", "A", 1, true),
            ("ActiveClass", "", "B", 2, false),
            ("ActiveClass", "", "C", 3, false),
            ("ActiveClass", "", "D", 4, false),
            ("ActiveConstant", "", "1000", 1, true),
            ("ReactiveClass", "", "2.0", 1, true),
            ("ReactiveClass", "", "3.0", 2, false),
            ("ReactiveClass", "", "1S", 3, false),
            ("ReactiveClass", "", "0.5S", 4, false),
            ("ReactiveConstant", "", "1000", 1, true),
            ("BaudRate", "", "9600-8-E-1", 1, true),
            ("BaudRate", "", "1200-8-E-1", 2, false),
            ("BaudRate", "", "2400-8-E-1", 3, false),
            ("BaudRate", "", "4800-8-E-1", 4, false),
            ("BaudRate", "", "115200-8-E-1", 5, false)
        };

        foreach ((string category, string scope, string value, int sortOrder, bool isDefault) in defaults)
        {
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText =
                "INSERT OR IGNORE INTO MeterTestAssetOption "
                + "(Category, Scope, Value, SortOrder, IsDefault, Enabled) "
                + "VALUES ($Category, $Scope, $Value, $SortOrder, $IsDefault, 1);";
            AddParameter(command, "$Category", category);
            AddParameter(command, "$Scope", scope);
            AddParameter(command, "$Value", value);
            AddParameter(command, "$SortOrder", sortOrder);
            AddParameter(command, "$IsDefault", isDefault ? 1 : 0);
            command.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// 读取基本误差 AnyUIOutput 的功率因数夹角配置。
    /// 测试点使用 Direction + PowerFactor 精确查找，不再使用代码内的硬编码角度。
    /// </summary>
    public IReadOnlyList<MeterTestPowerFactorAngleData> LoadPowerFactorAngles()
    {
        EnsureInitialized();
        List<MeterTestPowerFactorAngleData> results = new();
        using SqliteConnection connection = CreateOpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            "SELECT Direction, PowerFactor, LoadType, CurrentAngle, Description "
            + "FROM MeterTestPowerFactorAngle WHERE Enabled=1 ORDER BY Id;";

        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            results.Add(new MeterTestPowerFactorAngleData(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                Convert.ToDecimal(reader.GetDouble(3), System.Globalization.CultureInfo.InvariantCulture),
                reader.IsDBNull(4) ? string.Empty : reader.GetString(4)));
        }

        return results;
    }

    /// <summary>
    /// 首次建库时写入国网有功基本误差的12组默认FA角度。
    /// INSERT OR IGNORE 保证现场后续修改的角度不会被程序启动覆盖。
    /// </summary>
    private static void SeedPowerFactorAngles(SqliteConnection connection)
    {
        (string Direction, string PowerFactor, string LoadType, decimal CurrentAngle, string Description)[] defaults =
        {
            ("ForwardActive", "1.0", "纯阻性", 0m, "正向有功，电压电流同相"),
            ("ForwardActive", "0.5L", "感性", 60m, "正向有功，电流滞后电压60度"),
            ("ForwardActive", "0.8C", "容性", -36.869898m, "正向有功，电流超前电压36.869898度"),
            ("ForwardActive", "0.25L", "感性", 75.522488m, "正向有功，电流滞后电压75.522488度"),
            ("ForwardActive", "0.5C", "容性", -60m, "正向有功，电流超前电压60度"),
            ("ForwardActive", "0.25C", "容性", -75.522488m, "正向有功，电流超前电压75.522488度"),
            ("ReverseActive", "1.0", "纯阻性", -180m, "反向有功，电压电流反相"),
            ("ReverseActive", "0.5L", "感性", -120m, "反向有功感性，FA角-120度"),
            ("ReverseActive", "0.8C", "容性", 143.130102m, "反向有功容性，FA角143.130102度"),
            ("ReverseActive", "0.25L", "感性", -104.477512m, "反向有功感性，FA角-104.477512度"),
            ("ReverseActive", "0.5C", "容性", 120m, "反向有功容性，FA角120度"),
            ("ReverseActive", "0.25C", "容性", 104.477512m, "反向有功容性，FA角104.477512度")
        };

        foreach ((string direction, string powerFactor, string loadType, decimal currentAngle, string description) in defaults)
        {
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText =
                "INSERT OR IGNORE INTO MeterTestPowerFactorAngle "
                + "(Direction, PowerFactor, LoadType, CurrentAngle, Description, Enabled, UpdatedAt) "
                + "VALUES ($Direction, $PowerFactor, $LoadType, $CurrentAngle, $Description, 1, $UpdatedAt);";
            AddParameter(command, "$Direction", direction);
            AddParameter(command, "$PowerFactor", powerFactor);
            AddParameter(command, "$LoadType", loadType);
            AddParameter(command, "$CurrentAngle", currentAngle);
            AddParameter(command, "$Description", description);
            AddParameter(command, "$UpdatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            command.ExecuteNonQuery();
        }
    }

    /// <summary>向 SQLite 命令添加可空参数，并统一将 null 转换为 DBNull。</summary>
    private static void AddParameter(SqliteCommand command, string name, object? value)
    {
        SqliteParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    /// <summary>为尚无档案的工位插入一条由数据库默认选项组成的初始资产记录。</summary>
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
                CurrentSpecification,
                ActiveClass,
                ActiveConstant,
                ReactiveClass,
                ReactiveConstant,
                Barcode,
                MeterAddress,
                BaudRate,
                UpdatedAt
            )
            VALUES
            (
                $StationNo,
                $MeterType,
                $AccessMode,
                $Voltage,
                $Current,
                $CurrentSpecification,
                $ActiveClass,
                $ActiveConstant,
                $ReactiveClass,
                $ReactiveConstant,
                '',
                '',
                $BaudRate,
                $UpdatedAt
            );
            """;

        AddParameter(command, "$StationNo", stationNo);
        AddParameter(command, "$MeterType", GetDefaultAssetOptionValue(connection, "MeterType"));
        AddParameter(command, "$AccessMode", GetDefaultAssetOptionValue(connection, "AccessMode"));
        AddParameter(command, "$Voltage", GetDefaultAssetOptionValue(connection, "Voltage"));
        AddParameter(command, "$Current", GetDefaultAssetOptionValue(connection, "BasicCurrent"));
        AddParameter(command, "$CurrentSpecification", GetDefaultAssetOptionValue(connection, "CurrentSpecification", "Direct"));
        AddParameter(command, "$ActiveClass", GetDefaultAssetOptionValue(connection, "ActiveClass"));
        AddParameter(command, "$ActiveConstant", GetDefaultAssetOptionValue(connection, "ActiveConstant"));
        AddParameter(command, "$ReactiveClass", GetDefaultAssetOptionValue(connection, "ReactiveClass"));
        AddParameter(command, "$ReactiveConstant", GetDefaultAssetOptionValue(connection, "ReactiveConstant"));
        AddParameter(command, "$BaudRate", GetDefaultAssetOptionValue(connection, "BaudRate"));
        AddParameter(command, "$UpdatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        command.ExecuteNonQuery();
    }

    /// <summary>从当前数据行完整还原电表资产档案对象。</summary>
    private static MeterArchiveData ReadMeterArchive(SqliteDataReader reader)
    {
        return new MeterArchiveData(
            reader.GetInt32(0),
            reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
            reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
            reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
            reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
            reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
            reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
            reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
            reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
            reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
            reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
            reader.IsDBNull(11) ? string.Empty : reader.GetString(11),
            reader.IsDBNull(12) ? string.Empty : reader.GetString(12));
    }

    /// <summary>查询指定资产类别和范围的默认值；没有显式默认项时返回首个启用值。</summary>
    private static string GetDefaultAssetOptionValue(
        SqliteConnection connection,
        string category,
        string scope = "")
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            "SELECT Value FROM MeterTestAssetOption "
            + "WHERE Category=$Category AND Scope=$Scope AND Enabled=1 "
            + "ORDER BY IsDefault DESC, SortOrder, Id LIMIT 1;";
        AddParameter(command, "$Category", category);
        AddParameter(command, "$Scope", scope);
        return Convert.ToString(command.ExecuteScalar()) ?? string.Empty;
    }

    /// <summary>将电表档案所有字段绑定到新增或更新命令的参数集合。</summary>
    private static void AddMeterArchiveParameters(SqliteCommand command, MeterArchiveData archive)
    {
        AddParameter(command, "$StationNo", archive.StationNo);
        AddParameter(command, "$MeterType", archive.MeterType);
        AddParameter(command, "$AccessMode", archive.AccessMode);
        AddParameter(command, "$Voltage", archive.Voltage);
        AddParameter(command, "$Current", archive.Current);
        AddParameter(command, "$CurrentSpecification", archive.CurrentSpecification);
        AddParameter(command, "$ActiveClass", archive.ActiveClass);
        AddParameter(command, "$ActiveConstant", archive.ActiveConstant);
        AddParameter(command, "$ReactiveClass", archive.ReactiveClass);
        AddParameter(command, "$ReactiveConstant", archive.ReactiveConstant);
        AddParameter(command, "$Barcode", archive.Barcode);
        AddParameter(command, "$MeterAddress", archive.MeterAddress);
        AddParameter(command, "$BaudRate", archive.BaudRate);
        AddParameter(command, "$UpdatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
    }

    /// <summary>
    /// 为旧数据库补齐缺失字段。
    /// </summary>
    private static void EnsureColumnExists(SqliteConnection connection, string tableName, string columnName, string columnDefinition)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({tableName});";

        bool columnExists = false;
        using (SqliteDataReader reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                string existingName = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                if (existingName.Equals(columnName, StringComparison.OrdinalIgnoreCase))
                {
                    columnExists = true;
                    break;
                }
            }
        }

        if (columnExists)
            return;

        using SqliteCommand alterCommand = connection.CreateCommand();
        alterCommand.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition};";
        alterCommand.ExecuteNonQuery();
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
/// 方案树批量恢复时使用的单条工位结果，包含完整方案层级定位信息。
/// </summary>
public sealed record MeterTestStoredStationResultData(
    string SchemeName,
    string TestItemName,
    string TestSubItemName,
    int StationNo,
    StationDisplayStateData State);

/// <summary>
/// 单个工位的电表档案数据。
/// </summary>
public sealed record MeterArchiveData(
    int StationNo,
    string MeterType,
    string AccessMode,
    string Voltage,
    string Current,
    string CurrentSpecification,
    string ActiveClass,
    string ActiveConstant,
    string ReactiveClass,
    string ReactiveConstant,
    string Barcode,
    string MeterAddress,
    string BaudRate);

/// <summary>
/// 资产信息中条形码截取规则的持久化数据。
/// </summary>
public sealed record MeterTestAssetBarcodeSettingData(
    int BarcodeStartIndex,
    int BarcodeEndIndex,
    string RuleType,
    int Rule2FirstStart,
    int Rule2FirstLength,
    int Rule2SecondStart,
    int Rule2SecondLength);

/// <summary>资产信息下拉候选项，由SQLite配置表驱动。</summary>
public sealed record MeterTestAssetOptionData(
    string Category,
    string Scope,
    string Value,
    int SortOrder,
    bool IsDefault);

/// <summary>基本误差功率方向和功率因数对应的 AnyUIOutput 有符号FA角度配置。</summary>
public sealed record MeterTestPowerFactorAngleData(
    string Direction,
    string PowerFactor,
    string LoadType,
    decimal CurrentAngle,
    string Description);

/// <summary>一次自动或手动保存的完整测试任务快照。</summary>
public sealed record MeterTestResultTaskSnapshot(
    string RunId,
    string SchemeName,
    DateTime StartedAt,
    DateTime EndedAt,
    string Status,
    string SaveMode,
    string ResultSummary,
    IReadOnlyList<MeterTestResultStationData> Stations,
    IReadOnlyList<MeterTestResultDetailData> Details);

/// <summary>测试结果界面顶部任务列表的一条记录。</summary>
public sealed record MeterTestResultTaskData(
    long Id,
    string RunId,
    string SchemeName,
    DateTime StartedAt,
    DateTime EndedAt,
    string Status,
    string SaveMode,
    int StationCount,
    string ResultSummary);

/// <summary>一次测试任务内的工位电表资产快照与总结论。</summary>
public sealed record MeterTestResultStationData(
    int StationNo,
    string Barcode,
    string MeterAddress,
    string MeterType,
    string AccessMode,
    string Voltage,
    string BasicCurrent,
    string CurrentSpecification,
    string ActiveClass,
    string ActiveConstant,
    string ReactiveClass,
    string ReactiveConstant,
    string OverallResult,
    DateTime CompletedAt);

/// <summary>
/// 工位的一条测试明细。
/// MeasurementName/SequenceNo/NumericValue 用于保存日计时、起动、潜动和基本误差的可计算数值。
/// </summary>
public sealed record MeterTestResultDetailData(
    int StationNo,
    string TestItemName,
    string TestSubItemName,
    string Result,
    string ResultTimeText,
    string Message,
    string MeasurementName,
    int SequenceNo,
    string ValueText,
    double? NumericValue,
    string Unit,
    double? AverageValue,
    string LimitText);

/// <summary>运行期暂存的协议数值，最终会并入 MeterTestResultDetail。</summary>
public sealed record MeterTestMeasurementData(
    int StationNo,
    string TestItemName,
    string TestSubItemName,
    string MeasurementName,
    int SequenceNo,
    double NumericValue,
    string ValueText,
    string Unit,
    double? AverageValue,
    string LimitText);
