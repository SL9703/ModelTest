using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace ModelTest.MeterTest;

/// <summary>
/// 工位通信配置 XML 读写服务。
/// 用于维护 StationTcp 测试模式下 48 个工位的 IP/Port 映射。
/// </summary>
public sealed class MeterTestStationConfigService
{
    /// <summary>
    /// 工位配置序列化器。
    /// </summary>
    private readonly XmlSerializer serializer = new(typeof(MeterTestStationConfig));

    /// <summary>
    /// 加载工位通信配置；如果不存在或工位不完整，会用默认 IP/端口补齐并保存。
    /// </summary>
    public MeterTestStationConfig LoadOrCreate(string configPath, int stationCount, string defaultIp, int defaultStartPort)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(configPath) ?? AppContext.BaseDirectory);

        MeterTestStationConfig config;
        if (File.Exists(configPath))
        {
            using FileStream stream = File.OpenRead(configPath);
            config = serializer.Deserialize(stream) as MeterTestStationConfig ?? new MeterTestStationConfig();
        }
        else
        {
            config = new MeterTestStationConfig();
        }

        EnsureStations(config, stationCount, defaultIp, defaultStartPort);
        Save(configPath, config);
        return config;
    }

    /// <summary>
    /// 保存工位通信配置。
    /// </summary>
    public void Save(string configPath, MeterTestStationConfig config)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(configPath) ?? AppContext.BaseDirectory);

        using FileStream stream = File.Create(configPath);
        serializer.Serialize(stream, config);
    }

    /// <summary>
    /// 确保配置中包含指定数量的工位。
    /// 缺失工位按 defaultStartPort + stationNo - 1 自动分配端口。
    /// </summary>
    private static void EnsureStations(MeterTestStationConfig config, int stationCount, string defaultIp, int defaultStartPort)
    {
        for (int stationNo = 1; stationNo <= stationCount; stationNo++)
        {
            if (config.Stations.Any(station => station.StationNo == stationNo))
                continue;

            config.Stations.Add(new MeterTestStationCommunication
            {
                StationNo = stationNo,
                Ip = defaultIp,
                Port = defaultStartPort + stationNo - 1
            });
        }

        config.Stations = config.Stations
            .Where(station => station.StationNo >= 1 && station.StationNo <= stationCount)
            .OrderBy(station => station.StationNo)
            .ToList();
    }
}
