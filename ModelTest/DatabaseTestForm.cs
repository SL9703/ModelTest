using ModelTest.Tools;
using MySqlConnector;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Data.Common;
using System.Net.Sockets;

namespace ModelTest
{
    public partial class DatabaseTestForm : Form
    {
        private const string DatabaseSection = "Database";
        private const string OracleDatabaseType = "Oracle";
        private const string MySqlDatabaseType = "MySQL";
        private const string ServiceNameMode = "SERVICE_NAME";
        private const string SidMode = "SID";
        private const string MySqlMode = "DATABASE";
        private const string DefaultIp = "20.66.101.200";
        private const string DefaultPort = "11521";
        private const string DefaultDatabase = "jlgkdb";
        private const string DefaultDatabaseMode = SidMode;
        private const string DefaultDatabaseType = OracleDatabaseType;
        private const string DefaultUser = "sxykjd";
        private const string DefaultPassword = "hJM%67L$";

        private static readonly string ConfigPath = Path.Combine(Application.StartupPath, "XCKJcomfig.ini");
        private bool _isConnected;
        private string _activeConnectionString = string.Empty;
        private string _activeConnectionKey = string.Empty;

        public DatabaseTestForm()
        {
            InitializeComponent();
            LoadDatabaseConfig();
        }

        private void LoadDatabaseConfig()
        {
            string ip = Confighelper.ReadIni(DatabaseSection, "ip", "", 255, ConfigPath).Trim();
            string port = Confighelper.ReadIni(DatabaseSection, "port", "", 255, ConfigPath).Trim();
            string database = Confighelper.ReadIni(DatabaseSection, "database", "", 255, ConfigPath).Trim();
            string databaseType = NormalizeDatabaseType(Confighelper.ReadIni(DatabaseSection, "databaseType", "", 255, ConfigPath).Trim());
            string databaseMode = NormalizeDatabaseMode(Confighelper.ReadIni(DatabaseSection, "databaseMode", "", 255, ConfigPath).Trim());
            string user = Confighelper.ReadIni(DatabaseSection, "user", "", 255, ConfigPath).Trim();
            string password = Confighelper.ReadIni(DatabaseSection, "password", "", 255, ConfigPath);

            if (!IsSupportedDatabaseType(databaseType))
            {
                databaseType = DefaultDatabaseType;
                Confighelper.WriteIni(DatabaseSection, "databaseType", databaseType, ConfigPath);
            }

            if (string.IsNullOrWhiteSpace(ip))
            {
                ip = DefaultIp;
                Confighelper.WriteIni(DatabaseSection, "ip", ip, ConfigPath);
            }

            if (string.IsNullOrWhiteSpace(port))
            {
                port = DefaultPort;
                Confighelper.WriteIni(DatabaseSection, "port", port, ConfigPath);
            }

            if (string.IsNullOrWhiteSpace(database))
            {
                database = DefaultDatabase;
                Confighelper.WriteIni(DatabaseSection, "database", database, ConfigPath);
            }

            if (!IsSupportedDatabaseMode(databaseMode))
            {
                databaseMode = DefaultDatabaseMode;
                Confighelper.WriteIni(DatabaseSection, "databaseMode", databaseMode, ConfigPath);
            }

            if (string.IsNullOrWhiteSpace(user))
            {
                user = DefaultUser;
                Confighelper.WriteIni(DatabaseSection, "user", user, ConfigPath);
            }

            if (string.IsNullOrEmpty(password))
            {
                password = DefaultPassword;
                Confighelper.WriteIni(DatabaseSection, "password", password, ConfigPath);
            }

            tbxIp.Text = ip;
            tbxPort.Text = port;
            tbxDatabase.Text = database;
            cmbDatabaseType.Text = databaseType;
            cmbDatabaseMode.Text = databaseMode;
            tbxUser.Text = user;
            tbxPassword.Text = password;
            UpdateDatabaseModeState();
            lblStatus.Text = "未连接";
            lblQueryStatus.Text = "未查询";
            AddLog("初始化", $"已读取配置，类型：{databaseType}，地址：{ip}:{port}/{database}，模式：{databaseMode}，账户：{user}");
        }

        private void DatabaseConnectionInfo_TextChanged(object sender, EventArgs e)
        {
            UpdateDatabaseModeState();
            if (_isConnected)
            {
                ResetConnectionState();
                lblStatus.Text = "连接参数已变更";
                AddLog("连接", "连接参数已变更，需要重新连接数据库。");
            }
        }

        private async void btnConnect_Click(object sender, EventArgs e)
        {
            string databaseType = cmbDatabaseType.Text.Trim();
            string ip = tbxIp.Text.Trim();
            string portText = tbxPort.Text.Trim();
            string database = tbxDatabase.Text.Trim();
            string databaseMode = cmbDatabaseMode.Text.Trim();
            string user = tbxUser.Text.Trim();
            string password = tbxPassword.Text;

            if (string.IsNullOrWhiteSpace(ip))
            {
                MessageBox.Show("请输入数据库IP。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(portText, out int port) || port < 1 || port > 65535)
            {
                MessageBox.Show("请输入有效的端口号。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(database))
            {
                MessageBox.Show("请输入数据库名称。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!IsSupportedDatabaseType(databaseType))
            {
                MessageBox.Show("请选择有效的数据库类型。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (IsOracleDatabase(databaseType) && !IsSupportedDatabaseMode(databaseMode))
            {
                MessageBox.Show("请选择有效的数据库模式。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(user))
            {
                MessageBox.Show("请输入数据库账户。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                MessageBox.Show("请输入数据库密码。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnConnect.Enabled = false;
            lblStatus.Text = "连接中...";
            ResetConnectionState();
            AddLog("连接", $"开始连接{databaseType}数据库，地址：{ip}:{portText}/{database}，模式：{databaseMode}，账户：{user}");

            try
            {
                string connectionString = BuildConnectionString(databaseType, ip, port, database, databaseMode, user, password);
                AddLog("连接", $"{databaseType}连接串：{MaskPassword(databaseType, connectionString)}");

                await using DbConnection connection = CreateConnection(databaseType, connectionString);
                await connection.OpenAsync();

                SaveDatabaseConfig(databaseType, ip, portText, database, databaseMode, user, password);
                _isConnected = true;
                _activeConnectionString = connectionString;
                _activeConnectionKey = BuildConnectionKey(databaseType, ip, portText, database, databaseMode, user);
                lblStatus.Text = "连接成功";
                btnQuery.Enabled = true;
                btnExecuteSql.Enabled = true;
                AddLog("连接", $"{databaseType}数据库连接成功。");
                MessageBox.Show($"{databaseType}数据库连接成功。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ResetConnectionState();
                lblStatus.Text = "连接失败";
                AddExceptionLog($"{databaseType}数据库连接失败", ex);
                MessageBox.Show($"{databaseType}数据库连接失败：{ex.Message}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnConnect.Enabled = true;
            }
        }

        private async void btnTestPort_Click(object sender, EventArgs e)
        {
            string ip = tbxIp.Text.Trim();
            string portText = tbxPort.Text.Trim();

            if (string.IsNullOrWhiteSpace(ip))
            {
                MessageBox.Show("请输入数据库IP。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(portText, out int port) || port < 1 || port > 65535)
            {
                MessageBox.Show("请输入有效的端口号。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnTestPort.Enabled = false;
            AddLog("端口", $"开始测试端口：{ip}:{port}");

            try
            {
                using TcpClient tcpClient = new TcpClient();
                using CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await tcpClient.ConnectAsync(ip, port, cts.Token);
                AddLog("端口", $"端口测试成功：{ip}:{port} 可连接。");
                MessageBox.Show("端口测试成功。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                AddLog("端口", $"端口测试失败：{ip}:{port}，{ex.Message}");
                MessageBox.Show($"端口测试失败：{ex.Message}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnTestPort.Enabled = true;
            }
        }

        private async void btnQuery_Click(object sender, EventArgs e)
        {
            string taskNo = tbxTaskNo.Text.Trim();
            if (string.IsNullOrWhiteSpace(taskNo))
            {
                MessageBox.Show("请输入任务编号。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!_isConnected || string.IsNullOrWhiteSpace(_activeConnectionString))
            {
                MessageBox.Show("数据库未连接，请先连接成功后再查询。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                AddLog("查询", "查询被拦截：数据库未连接。");
                return;
            }

            string currentConnectionKey = BuildConnectionKey(
                cmbDatabaseType.Text.Trim(),
                tbxIp.Text.Trim(),
                tbxPort.Text.Trim(),
                tbxDatabase.Text.Trim(),
                cmbDatabaseMode.Text.Trim(),
                tbxUser.Text.Trim());

            if (!string.Equals(currentConnectionKey, _activeConnectionKey, StringComparison.Ordinal))
            {
                ResetConnectionState();
                MessageBox.Show("连接参数已变更，请重新连接数据库后再查询。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                AddLog("查询", "查询被拦截：连接参数已变更，需要重新连接。");
                return;
            }

            if (!TryBuildConnectionString(out _, out string errorMessage))
            {
                MessageBox.Show(errorMessage, "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                AddLog("查询", $"查询参数校验失败：{errorMessage}");
                return;
            }

            btnQuery.Enabled = false;
            lblQueryStatus.Text = "查询中...";
            AddLog("查询", $"开始查询任务编号：{taskNo}");

            try
            {
                DataTable resultTable = await QueryDetectTerminalResultAsync(cmbDatabaseType.Text.Trim(), _activeConnectionString, taskNo);
                dgvResult.DataSource = resultTable;
                lblQueryStatus.Text = $"查询完成，共 {resultTable.Rows.Count} 条";
                AddLog("查询", $"查询完成，返回 {resultTable.Rows.Count} 条记录。");
            }
            catch (Exception ex)
            {
                lblQueryStatus.Text = "查询失败";
                AddExceptionLog("查询失败", ex);
                MessageBox.Show($"查询失败：{ex.Message}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnQuery.Enabled = true;
            }
        }

        private async void btnExecuteSql_Click(object sender, EventArgs e)
        {
            string sql = tbxCustomSql.Text.Trim();
            if (string.IsNullOrWhiteSpace(sql))
            {
                MessageBox.Show("请输入要执行的SQL语句。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!ValidateActiveConnection("自定义SQL"))
            {
                return;
            }

            btnExecuteSql.Enabled = false;
            btnQuery.Enabled = false;
            lblQueryStatus.Text = "SQL执行中...";
            AddLog("自定义SQL", $"开始执行SQL：{sql}");

            try
            {
                SqlExecutionResult result = await ExecuteCustomSqlAsync(cmbDatabaseType.Text.Trim(), _activeConnectionString, sql);
                if (result.ResultTable != null)
                {
                    dgvResult.DataSource = result.ResultTable;
                    lblQueryStatus.Text = $"SQL查询完成，共 {result.ResultTable.Rows.Count} 条";
                    AddLog("自定义SQL", $"查询完成，返回 {result.ResultTable.Rows.Count} 条记录。");
                }
                else
                {
                    dgvResult.DataSource = null;
                    lblQueryStatus.Text = $"SQL执行完成，影响 {result.AffectedRows} 行";
                    AddLog("自定义SQL", $"执行完成，影响 {result.AffectedRows} 行。");
                }
            }
            catch (Exception ex)
            {
                lblQueryStatus.Text = "SQL执行失败";
                AddExceptionLog("自定义SQL执行失败", ex);
                MessageBox.Show($"SQL执行失败：{ex.Message}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnExecuteSql.Enabled = _isConnected;
                btnQuery.Enabled = _isConnected;
            }
        }

        private void ResetConnectionState()
        {
            _isConnected = false;
            _activeConnectionString = string.Empty;
            _activeConnectionKey = string.Empty;
            if (btnQuery != null)
            {
                btnQuery.Enabled = false;
            }

            if (btnExecuteSql != null)
            {
                btnExecuteSql.Enabled = false;
            }
        }

        private bool ValidateActiveConnection(string operationName)
        {
            if (!_isConnected || string.IsNullOrWhiteSpace(_activeConnectionString))
            {
                MessageBox.Show("数据库未连接，请先连接成功后再执行。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                AddLog(operationName, "执行被拦截：数据库未连接。");
                return false;
            }

            string currentConnectionKey = BuildConnectionKey(
                cmbDatabaseType.Text.Trim(),
                tbxIp.Text.Trim(),
                tbxPort.Text.Trim(),
                tbxDatabase.Text.Trim(),
                cmbDatabaseMode.Text.Trim(),
                tbxUser.Text.Trim());

            if (!string.Equals(currentConnectionKey, _activeConnectionKey, StringComparison.Ordinal))
            {
                ResetConnectionState();
                MessageBox.Show("连接参数已变更，请重新连接数据库后再执行。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                AddLog(operationName, "执行被拦截：连接参数已变更，需要重新连接。");
                return false;
            }

            if (!TryBuildConnectionString(out _, out string errorMessage))
            {
                MessageBox.Show(errorMessage, "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                AddLog(operationName, $"参数校验失败：{errorMessage}");
                return false;
            }

            return true;
        }

        private static string BuildConnectionKey(string databaseType, string ip, string port, string database, string databaseMode, string user)
        {
            return $"{databaseType}|{ip}|{port}|{database}|{databaseMode}|{user}";
        }

        private static void SaveDatabaseConfig(string databaseType, string ip, string port, string database, string databaseMode, string user, string password)
        {
            Confighelper.WriteIni(DatabaseSection, "databaseType", databaseType, ConfigPath);
            Confighelper.WriteIni(DatabaseSection, "ip", ip, ConfigPath);
            Confighelper.WriteIni(DatabaseSection, "port", port, ConfigPath);
            Confighelper.WriteIni(DatabaseSection, "database", database, ConfigPath);
            Confighelper.WriteIni(DatabaseSection, "databaseMode", databaseMode, ConfigPath);
            Confighelper.WriteIni(DatabaseSection, "user", user, ConfigPath);
            Confighelper.WriteIni(DatabaseSection, "password", password, ConfigPath);
        }

        private void AddLog(string category, string message)
        {
            string log = $"{category} | {message}";
            LogMessage.Debug(log);
            rtbLog.AppendText($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {log}{Environment.NewLine}");
            rtbLog.SelectionStart = rtbLog.TextLength;
            rtbLog.ScrollToCaret();
        }

        private void AddExceptionLog(string title, Exception ex)
        {
            LogMessage.Error(title, ex);
            AddLog("异常", $"{title}：{ex.GetType().FullName} - {ex.Message}");
            if (ex is OracleException oracleException)
            {
                AddLog("异常", $"Oracle错误号：{oracleException.Number}，ErrorCode：{oracleException.ErrorCode}");
                for (int i = 0; i < oracleException.Errors.Count; i++)
                {
                    OracleError error = oracleException.Errors[i];
                    AddLog("异常", $"Oracle错误明细[{i}]：Number={error.Number}，Message={error.Message}");
                }
            }
            else if (ex is MySqlException mySqlException)
            {
                AddLog("异常", $"MySQL错误号：{mySqlException.Number}，ErrorCode：{mySqlException.ErrorCode}，SqlState：{mySqlException.SqlState}");
            }

            if (ex.InnerException != null)
            {
                AddLog("异常", $"InnerException：{ex.InnerException.GetType().FullName} - {ex.InnerException.Message}");
            }
        }

        private bool TryBuildConnectionString(out string connectionString, out string errorMessage)
        {
            connectionString = string.Empty;
            errorMessage = string.Empty;

            string ip = tbxIp.Text.Trim();
            string portText = tbxPort.Text.Trim();
            string database = tbxDatabase.Text.Trim();
            string databaseType = cmbDatabaseType.Text.Trim();
            string databaseMode = cmbDatabaseMode.Text.Trim();
            string user = tbxUser.Text.Trim();
            string password = tbxPassword.Text;

            if (!IsSupportedDatabaseType(databaseType))
            {
                errorMessage = "请选择有效的数据库类型。";
                return false;
            }

            if (string.IsNullOrWhiteSpace(ip))
            {
                errorMessage = "请输入数据库IP。";
                return false;
            }

            if (!int.TryParse(portText, out int port) || port < 1 || port > 65535)
            {
                errorMessage = "请输入有效的端口号。";
                return false;
            }

            if (string.IsNullOrWhiteSpace(database))
            {
                errorMessage = "请输入数据库名称。";
                return false;
            }

            if (IsOracleDatabase(databaseType) && !IsSupportedDatabaseMode(databaseMode))
            {
                errorMessage = "请选择有效的数据库模式。";
                return false;
            }

            if (string.IsNullOrWhiteSpace(user))
            {
                errorMessage = "请输入数据库账户。";
                return false;
            }

            if (string.IsNullOrEmpty(password))
            {
                errorMessage = "请输入数据库密码。";
                return false;
            }

            connectionString = BuildConnectionString(databaseType, ip, port, database, databaseMode, user, password);
            return true;
        }

        private static string BuildConnectionString(string databaseType, string ip, int port, string database, string databaseMode, string user, string password)
        {
            return IsMySqlDatabase(databaseType)
                ? BuildMySqlConnectionString(ip, port, database, user, password)
                : BuildOracleConnectionString(ip, port, database, databaseMode, user, password);
        }

        private static string BuildOracleConnectionString(string ip, int port, string database, string databaseMode, string user, string password)
        {
            string connectData = string.Equals(databaseMode, SidMode, StringComparison.OrdinalIgnoreCase)
                ? $"(SID={database})"
                : $"(SERVICE_NAME={database})";

            string dataSource =
                $"(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={ip})(PORT={port}))" +
                $"(CONNECT_DATA={connectData}))";

            OracleConnectionStringBuilder builder = new OracleConnectionStringBuilder
            {
                DataSource = dataSource,
                UserID = user,
                Password = password
            };

            return builder.ConnectionString;
        }

        private static string BuildMySqlConnectionString(string ip, int port, string database, string user, string password)
        {
            MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder
            {
                Server = ip,
                Port = (uint)port,
                Database = database,
                UserID = user,
                Password = password,
                ConnectionTimeout = 15,
                CharacterSet = "utf8mb4"
            };

            return builder.ConnectionString;
        }

        private static DbConnection CreateConnection(string databaseType, string connectionString)
        {
            return IsMySqlDatabase(databaseType)
                ? new MySqlConnection(connectionString)
                : new OracleConnection(connectionString);
        }

        private static bool IsSupportedDatabaseType(string databaseType)
        {
            return IsOracleDatabase(databaseType) || IsMySqlDatabase(databaseType);
        }

        private static bool IsOracleDatabase(string databaseType)
        {
            return string.Equals(databaseType, OracleDatabaseType, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsMySqlDatabase(string databaseType)
        {
            return string.Equals(databaseType, MySqlDatabaseType, StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeDatabaseType(string databaseType)
        {
            if (IsOracleDatabase(databaseType))
            {
                return OracleDatabaseType;
            }

            if (IsMySqlDatabase(databaseType))
            {
                return MySqlDatabaseType;
            }

            return databaseType;
        }

        private static bool IsSupportedDatabaseMode(string databaseMode)
        {
            return string.Equals(databaseMode, SidMode, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(databaseMode, ServiceNameMode, StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeDatabaseMode(string databaseMode)
        {
            if (string.Equals(databaseMode, SidMode, StringComparison.OrdinalIgnoreCase))
            {
                return SidMode;
            }

            if (string.Equals(databaseMode, ServiceNameMode, StringComparison.OrdinalIgnoreCase))
            {
                return ServiceNameMode;
            }

            return databaseMode;
        }

        private void UpdateDatabaseModeState()
        {
            bool isOracle = IsOracleDatabase(cmbDatabaseType.Text.Trim());
            cmbDatabaseMode.Enabled = isOracle;
            lblDatabaseMode.Enabled = isOracle;
            if (!isOracle)
            {
                cmbDatabaseMode.Text = MySqlMode;
            }
            else if (!IsSupportedDatabaseMode(cmbDatabaseMode.Text.Trim()))
            {
                cmbDatabaseMode.Text = DefaultDatabaseMode;
            }

            tbxSql.Text = isOracle
                ? "select t.detect_equip_no as \"检测装置编号\" from MT_DETECT_TMNL_RSLT t where t.detect_task_no = :taskNo"
                : "select t.detect_equip_no as `检测装置编号` from MT_DETECT_TMNL_RSLT t where t.detect_task_no = @taskNo";
        }

        private static string MaskPassword(string databaseType, string connectionString)
        {
            try
            {
                if (IsMySqlDatabase(databaseType))
                {
                    MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder(connectionString)
                    {
                        Password = "***"
                    };

                    return builder.ConnectionString;
                }

                OracleConnectionStringBuilder oracleBuilder = new OracleConnectionStringBuilder(connectionString)
                {
                    Password = "***"
                };

                return oracleBuilder.ConnectionString;
            }
            catch
            {
                return connectionString;
            }
        }

        private static async Task<DataTable> QueryDetectTerminalResultAsync(string databaseType, string connectionString, string taskNo)
        {
            const string oracleSql = """
                select t.detect_equip_no as "检测装置编号"
                from MT_DETECT_TMNL_RSLT t
                where t.detect_task_no = :taskNo
                """;
            const string mySqlSql = """
                select t.detect_equip_no as `检测装置编号`
                from MT_DETECT_TMNL_RSLT t
                where t.detect_task_no = @taskNo
                """;
            DataTable table = new DataTable();

            await using DbConnection connection = CreateConnection(databaseType, connectionString);
            await connection.OpenAsync();

            await using DbCommand command = connection.CreateCommand();
            if (IsMySqlDatabase(databaseType))
            {
                command.CommandText = mySqlSql;
                command.Parameters.Add(new MySqlParameter("@taskNo", MySqlDbType.VarChar) { Value = taskNo });
            }
            else
            {
                command.CommandText = oracleSql;
                if (command is OracleCommand oracleCommand)
                {
                    oracleCommand.BindByName = true;
                    oracleCommand.Parameters.Add(new OracleParameter("taskNo", OracleDbType.Varchar2, taskNo, ParameterDirection.Input));
                }
            }

            await using DbDataReader reader = await command.ExecuteReaderAsync(CommandBehavior.CloseConnection);
            table.Load(reader);
            return table;
        }

        private static async Task<SqlExecutionResult> ExecuteCustomSqlAsync(string databaseType, string connectionString, string sql)
        {
            string executableSql = NormalizeExecutableSql(sql);

            await using DbConnection connection = CreateConnection(databaseType, connectionString);
            await connection.OpenAsync();

            await using DbCommand command = connection.CreateCommand();
            command.CommandText = executableSql;

            if (IsQuerySql(executableSql))
            {
                DataTable table = new DataTable();
                await using DbDataReader reader = await command.ExecuteReaderAsync(CommandBehavior.CloseConnection);
                table.Load(reader);
                return new SqlExecutionResult(table, 0);
            }

            if (IsOracleDatabase(databaseType))
            {
                await using DbTransaction transaction = await connection.BeginTransactionAsync();
                command.Transaction = transaction;
                int affectedRows = await command.ExecuteNonQueryAsync();
                await transaction.CommitAsync();
                return new SqlExecutionResult(null, affectedRows);
            }

            return new SqlExecutionResult(null, await command.ExecuteNonQueryAsync());
        }

        private static bool IsQuerySql(string sql)
        {
            string trimmedSql = sql.TrimStart();
            return trimmedSql.StartsWith("select", StringComparison.OrdinalIgnoreCase) ||
                trimmedSql.StartsWith("with", StringComparison.OrdinalIgnoreCase) ||
                trimmedSql.StartsWith("show", StringComparison.OrdinalIgnoreCase) ||
                trimmedSql.StartsWith("desc", StringComparison.OrdinalIgnoreCase) ||
                trimmedSql.StartsWith("describe", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeExecutableSql(string sql)
        {
            string executableSql = sql.Trim();
            return executableSql.EndsWith(';') ? executableSql[..^1].TrimEnd() : executableSql;
        }

        private sealed record SqlExecutionResult(DataTable? ResultTable, int AffectedRows);

    }
}
