using System.Collections.Generic;
using System.Xml.Serialization;

namespace ModelTest.MeterTest;

[XmlRoot("MeterTestPlanConfig")]
public class MeterTestPlanConfig
{
    [XmlElement("Scheme")]
    public List<MeterTestScheme> Schemes { get; set; } = new();
}

public class MeterTestScheme
{
    [XmlAttribute("name")]
    public string Name { get; set; } = string.Empty;

    [XmlAttribute("description")]
    public string Description { get; set; } = string.Empty;

    [XmlElement("TestItem")]
    public List<MeterTestItem> TestItems { get; set; } = new();
}

public class MeterTestItem
{
    [XmlAttribute("name")]
    public string Name { get; set; } = string.Empty;

    [XmlAttribute("description")]
    public string Description { get; set; } = string.Empty;

    [XmlElement("TestSubItem")]
    public List<MeterTestSubItem> TestSubItems { get; set; } = new();
}

public class MeterTestSubItem
{
    [XmlAttribute("name")]
    public string Name { get; set; } = string.Empty;

    [XmlAttribute("requestHex")]
    public string RequestHex { get; set; } = string.Empty;

    [XmlAttribute("expectedResponse")]
    public string ExpectedResponse { get; set; } = string.Empty;

    [XmlAttribute("responseMatchMode")]
    public string MatchMode { get; set; } = ResponseMatchMode.Contains.ToString();

    [XmlAttribute("timeoutMs")]
    public int TimeoutMs { get; set; } = 3000;

    [XmlAttribute("description")]
    public string Description { get; set; } = string.Empty;

    [XmlAttribute("mockResponse")]
    public string MockResponse { get; set; } = string.Empty;
}

public enum ResponseMatchMode
{
    Exact,
    Contains,
    StartsWith
}
