namespace ModelTest.Tools
{
    public sealed class SgccOadDefinition
    {
        public SgccOadDefinition(string name, string serviceType, string choice, string oad, string attributeSuffix)
        {
            Name = name;
            ServiceType = serviceType;
            Choice = choice;
            Oad = oad;
            AttributeSuffix = attributeSuffix;
        }

        public string Name { get; }

        public string ServiceType { get; }

        public string Choice { get; }

        public string Oad { get; }

        public string AttributeSuffix { get; }

        public string BuildApdu(string piid)
        {
            return ServiceType + Choice + piid + Oad + AttributeSuffix;
        }
    }

    public static class SGCCOadConfig
    {
        public const string BasicCategory = "基础参数";
        public const string EnergyCategory = "电能量";
        public const string DemandCategory = "需量";

        private const string GetRequest = "05";
        private const string GetRequestNormal = "01";
        private const string DefaultReadAttributeSuffix = "020000";

        private static readonly Dictionary<string, string> OadCatalogSource = new(StringComparer.OrdinalIgnoreCase)
        {
            ["0000"] = "组合有功电能",
            ["0010"] = "正向有功电能",
            ["0011"] = "A相正向有功电能",
            ["0012"] = "B相正向有功电能",
            ["0013"] = "C相正向有功电能",
            ["0020"] = "反向有功电能",
            ["0021"] = "A相反向有功电能",
            ["0022"] = "B相反向有功电能",
            ["0023"] = "C相反向有功电能",
            ["0030"] = "组合无功1电能",
            ["0031"] = "A相组合无功1电能",
            ["0032"] = "B相组合无功1电能",
            ["0033"] = "C相组合无功1电能",
            ["0040"] = "组合无功2电能",
            ["0041"] = "A相组合无功2电能",
            ["0042"] = "B相组合无功2电能",
            ["0043"] = "C相组合无功2电能",
            ["0050"] = "第一象限无功电能",
            ["0051"] = "A相第一象限无功电能",
            ["0052"] = "B相第一象限无功电能",
            ["0053"] = "C相第一象限无功电能",
            ["0060"] = "第二象限无功电能",
            ["0061"] = "A相第二象限无功电能",
            ["0062"] = "B相第二象限无功电能",
            ["0063"] = "C相第二象限无功电能",
            ["0070"] = "第三象限无功电能",
            ["0071"] = "A相第三象限无功电能",
            ["0072"] = "B相第三象限无功电能",
            ["0073"] = "C相第三象限无功电能",
            ["0080"] = "第四象限无功电能",
            ["0081"] = "A相第四象限无功电能",
            ["0082"] = "B相第四象限无功电能",
            ["0083"] = "C相第四象限无功电能",
            ["0090"] = "正向视在电能",
            ["0091"] = "A相正向视在电能",
            ["0092"] = "B相正向视在电能",
            ["0093"] = "C相正向视在电能",
            ["00A0"] = "反向视在电能",
            ["00A1"] = "A相反向视在电能",
            ["00A2"] = "B相反向视在电能",
            ["00A3"] = "C相反向视在电能",
            ["0110"] = "正向有功基波总电能",
            ["0111"] = "A相正向有功基波电能",
            ["0112"] = "B相正向有功基波电能",
            ["0113"] = "C相正向有功基波电能",
            ["0120"] = "反向有功基波总电能",
            ["0121"] = "A相反向有功基波电能",
            ["0122"] = "B相反向有功基波电能",
            ["0123"] = "C相反向有功基波电能",
            ["0210"] = "正向有功谐波总电能",
            ["0211"] = "A相正向有功谐波电能",
            ["0212"] = "B相正向有功谐波电能",
            ["0213"] = "C相正向有功谐波电能",
            ["0220"] = "反向有功谐波总电能",
            ["0221"] = "A相反向有功谐波电能",
            ["0222"] = "B相反向有功谐波电能",
            ["0223"] = "C相反向有功谐波电能",
            ["0300"] = "铜损有功总电能补偿量",
            ["0301"] = "A相铜损有功电能补偿量",
            ["0302"] = "B相铜损有功电能补偿量",
            ["0303"] = "C相铜损有功电能补偿量",
            ["0400"] = "铁损有功总电能补偿量",
            ["0401"] = "A相铁损有功电能补偿量",
            ["0402"] = "B相铁损有功电能补偿量",
            ["0403"] = "C相铁损有功电能补偿量",
            ["0500"] = "关联总电能",
            ["0501"] = "A相关联电能",
            ["0502"] = "B相关联电能",
            ["0503"] = "C相关联电能",
            ["1010"] = "正向有功最大需量",
            ["1011"] = "A相正向有功最大需量",
            ["1012"] = "B相正向有功最大需量",
            ["1013"] = "C相正向有功最大需量",
            ["1020"] = "反向有功最大需量",
            ["1021"] = "A相反向有功最大需量",
            ["1022"] = "B相反向有功最大需量",
            ["1023"] = "C相反向有功最大需量",
            ["1030"] = "组合无功1最大需量",
            ["1031"] = "A相组合无功1最大需量",
            ["1032"] = "B相组合无功1最大需量",
            ["1033"] = "C相组合无功1最大需量",
            ["1040"] = "组合无功2最大需量",
            ["1041"] = "A相组合无功2最大需量",
            ["1042"] = "B相组合无功2最大需量",
            ["1043"] = "C相组合无功2最大需量",
            ["1050"] = "第一象限最大需量",
            ["1051"] = "A相第一象限最大需量",
            ["1052"] = "B相第一象限最大需量",
            ["1053"] = "C相第一象限最大需量",
            ["1060"] = "第二象限最大需量",
            ["1061"] = "A相第二象限最大需量",
            ["1062"] = "B相第二象限最大需量",
            ["1063"] = "C相第二象限最大需量",
            ["1070"] = "第三象限最大需量",
            ["1071"] = "A相第三象限最大需量",
            ["1072"] = "B相第三象限最大需量",
            ["1073"] = "C相第三象限最大需量",
            ["1080"] = "第四象限最大需量",
            ["1081"] = "A相第四象限最大需量",
            ["1082"] = "B相第四象限最大需量",
            ["1083"] = "C相第四象限最大需量",
            ["1090"] = "正向视在最大需量",
            ["1091"] = "A相正向视在最大需量",
            ["1092"] = "B相正向视在最大需量",
            ["1093"] = "C相正向视在最大需量",
            ["10A0"] = "反向视在最大需量",
            ["10A1"] = "A相反向视在最大需量",
            ["10A2"] = "B相反向视在最大需量",
            ["10A3"] = "C相反向视在最大需量",
            ["1110"] = "冻结周期内正向有功最大需量",
            ["1111"] = "冻结周期内A相正向有功最大需量",
            ["1112"] = "冻结周期内B相正向有功最大需量",
            ["1113"] = "冻结周期内C相正向有功最大需量",
            ["1120"] = "冻结周期内反向有功最大需量",
            ["1121"] = "冻结周期内A相反向有功最大需量",
            ["1122"] = "冻结周期内B相反向有功最大需量",
            ["1123"] = "冻结周期内C相反向有功最大需量",
            ["1130"] = "冻结周期内组合无功1最大需量",
            ["1131"] = "冻结周期内A相组合无功1最大需量",
            ["1132"] = "冻结周期内B相组合无功1最大需量",
            ["1133"] = "冻结周期内C相组合无功1最大需量",
            ["1140"] = "冻结周期内组合无功2最大需量",
            ["1141"] = "冻结周期内A相组合无功2最大需量",
            ["1142"] = "冻结周期内B相组合无功2最大需量",
            ["1143"] = "冻结周期内C相组合无功2最大需量",
            ["1150"] = "冻结周期内第一象限最大需量",
            ["1151"] = "冻结周期内A相第一象限最大需量",
            ["1152"] = "冻结周期内B相第一象限最大需量",
            ["1153"] = "冻结周期内C相第一象限最大需量",
            ["1160"] = "冻结周期内第二象限最大需量",
            ["1161"] = "冻结周期内A相第二象限最大需量",
            ["1162"] = "冻结周期内B相第二象限最大需量",
            ["1163"] = "冻结周期内C相第二象限最大需量",
            ["1170"] = "冻结周期内第三象限最大需量",
            ["1171"] = "冻结周期内A相第三象限最大需量",
            ["1172"] = "冻结周期内B相第三象限最大需量",
            ["1173"] = "冻结周期内C相第三象限最大需量",
            ["1180"] = "冻结周期内第四象限最大需量",
            ["1181"] = "冻结周期内A相第四象限最大需量",
            ["1182"] = "冻结周期内B相第四象限最大需量",
            ["1183"] = "冻结周期内C相第四象限最大需量",
            ["1190"] = "冻结周期内正向视在最大需量",
            ["1191"] = "冻结周期内A相正向视在最大需量",
            ["1192"] = "冻结周期内B相正向视在最大需量",
            ["1193"] = "冻结周期内C相正向视在最大需量",
            ["11A0"] = "冻结周期内反向视在最大需量",
            ["11A1"] = "冻结周期内A相反向视在最大需量",
            ["11A2"] = "冻结周期内B相反向视在最大需量",
            ["11A3"] = "冻结周期内C相反向视在最大需量",
            ["F101"] = "安全模式参数",
            ["F201"] = "485属性",
            ["4001"] = "通信地址"
        };

        public static IReadOnlyDictionary<string, string> OadCatalog => OadCatalogSource;

        public static IReadOnlyList<string> OadCategories { get; } =
        [
            EnergyCategory,
            DemandCategory,
            BasicCategory
        ];

        public static IReadOnlyDictionary<string, SgccOadDefinition> OadDefinitions { get; } = BuildOadDefinitions();

        public static IReadOnlyList<string> GetServiceNamesByCategory(string category)
        {
            IEnumerable<SgccOadDefinition> definitions = OadDefinitions.Values;
            definitions = category switch
            {
                EnergyCategory => definitions.Where(item => item.Oad.StartsWith('0')),
                DemandCategory => definitions.Where(item => item.Oad.StartsWith('1')),
                BasicCategory => definitions.Where(item => !item.Oad.StartsWith('0') && !item.Oad.StartsWith('1')),
                _ => definitions
            };

            return definitions.Select(item => item.Name).ToList();
        }

        private static IReadOnlyDictionary<string, SgccOadDefinition> BuildOadDefinitions()
        {
            Dictionary<string, SgccOadDefinition> definitions = new(StringComparer.OrdinalIgnoreCase)
            {
                ["读取终端或电表485属性"] = CreateReadDefinition("读取终端或电表485属性", "F201"),
                ["广播读取终端或电表地址"] = CreateReadDefinition("广播读取终端或电表地址", "4001"),
                ["读取终端或电表安全模式"] = CreateReadDefinition("读取终端或电表安全模式", "F101")
            };

            foreach (KeyValuePair<string, string> item in OadCatalogSource)
            {
                string name = $"读取终端或电表{item.Value}";
                definitions.TryAdd(name, CreateReadDefinition(name, item.Key));
            }

            return definitions;
        }

        public static void RegisterOadDescription(string oad, string description)
        {
            string normalizedOad = NormalizeOad(oad);
            if (string.IsNullOrWhiteSpace(description))
            {
                throw new ArgumentException("OAD描述不能为空", nameof(description));
            }

            OadCatalogSource[normalizedOad] = description.Trim();
        }

        private static SgccOadDefinition CreateReadDefinition(string name, string oad)
        {
            return new SgccOadDefinition(name, GetRequest, GetRequestNormal, oad, DefaultReadAttributeSuffix);
        }

        private static string NormalizeOad(string oad)
        {
            if (string.IsNullOrWhiteSpace(oad))
            {
                throw new ArgumentException("OAD码不能为空", nameof(oad));
            }

            string normalizedOad = new string(oad.Where(Uri.IsHexDigit).Select(char.ToUpperInvariant).ToArray());
            if (normalizedOad.Length != 4)
            {
                throw new ArgumentException("OAD码必须是2个字节", nameof(oad));
            }

            return normalizedOad;
        }
    }
}
