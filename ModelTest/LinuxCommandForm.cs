using System.ComponentModel;
using ModelTest.Tools;

namespace ModelTest
{
    public partial class LinuxCommandForm : Form
    {
        private readonly BindingList<LinuxCommandItem> _commands;
        private readonly BindingSource _bindingSource = new BindingSource();

        public LinuxCommandForm()
        {
            InitializeComponent();
            _commands = new BindingList<LinuxCommandItem>(BuildCommandItems());
            BindGrid();
            BindCategories();
            ApplyFilter();
        }

        private void BindCategories()
        {
            lstCategory.Items.Clear();
            lstCategory.Items.Add("全部");

            foreach (string category in _commands.Select(command => command.Category).Distinct().OrderBy(category => category))
            {
                lstCategory.Items.Add(category);
            }

            lstCategory.SelectedIndex = 0;
        }

        private void BindGrid()
        {
            _bindingSource.DataSource = _commands;
            dgvCommands.AutoGenerateColumns = false;
            dgvCommands.DataSource = _bindingSource;
        }

        private void txtKeyword_TextChanged(object sender, EventArgs e)
        {
            ApplyFilter();
        }

        private void lstCategory_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            string category = lstCategory.SelectedItem?.ToString() ?? "全部";
            string keyword = txtKeyword.Text.Trim();

            IEnumerable<LinuxCommandItem> filtered = _commands;

            if (!string.Equals(category, "全部", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(command => command.Category == category);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                filtered = filtered.Where(command =>
                    command.Command.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    command.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    command.Example.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    command.Category.Contains(keyword, StringComparison.OrdinalIgnoreCase));
            }

            List<LinuxCommandItem> result = filtered.ToList();
            _bindingSource.DataSource = new BindingList<LinuxCommandItem>(result);
            lblStatus.Text = $"共 {result.Count} 条命令，双击命令行可复制。";

            if (result.Count > 0 && dgvCommands.Rows.Count > 0)
            {
                dgvCommands.ClearSelection();
                dgvCommands.Rows[0].Selected = true;
                ShowCommandDetail(result[0]);
            }
            else
            {
                txtDetail.Clear();
            }
        }

        private void dgvCommands_SelectionChanged(object sender, EventArgs e)
        {
            if (GetSelectedCommand() is LinuxCommandItem command)
            {
                ShowCommandDetail(command);
            }
        }

        private void dgvCommands_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || GetSelectedCommand() is not LinuxCommandItem command)
            {
                return;
            }

            CopyCommand(command);
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            if (GetSelectedCommand() is LinuxCommandItem command)
            {
                CopyCommand(command);
            }
        }

        private LinuxCommandItem? GetSelectedCommand()
        {
            return dgvCommands.CurrentRow?.DataBoundItem as LinuxCommandItem;
        }

        private void CopyCommand(LinuxCommandItem command)
        {
            if (command.IsDangerous)
            {
                DialogResult result = MessageBox.Show(
                    $"该命令属于危险操作，可能删除文件、格式化磁盘、修改权限或影响系统运行。\r\n\r\n命令：{command.Command}\r\n\r\n确认复制吗？",
                    "危险命令提示",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);

                if (result != DialogResult.Yes)
                {
                    lblStatus.Text = "已取消复制危险命令。";
                    return;
                }
            }

            Clipboard.SetText(command.Command);
            lblStatus.Text = $"已复制：{command.Command}";
            LogMessage.Debug($"Linux命令复制 | {command.Command}");
        }

        private void ShowCommandDetail(LinuxCommandItem command)
        {
            txtDetail.Text =
                $"分类：{command.Category}{Environment.NewLine}" +
                $"命令：{command.Command}{Environment.NewLine}" +
                $"说明：{command.Description}{Environment.NewLine}" +
                $"用例：{command.Example}{Environment.NewLine}" +
                $"风险：{(command.IsDangerous ? "危险命令，执行前必须确认目标路径、权限和影响范围。" : "常规命令。")}";
        }

        private static List<LinuxCommandItem> BuildCommandItems()
        {
            return new List<LinuxCommandItem>
            {
                new("文件目录", "pwd", "显示当前所在目录的绝对路径。", "pwd"),
                new("文件目录", "ls -lah", "以详细格式列出当前目录文件，包含隐藏文件和可读大小。", "ls -lah"),
                new("文件目录", "cd /path/to/dir", "切换到指定目录。", "cd /var/log"),
                new("文件目录", "mkdir -p /path/to/dir", "递归创建目录，父目录不存在时自动创建。", "mkdir -p /data/app/logs"),
                new("文件目录", "touch file.txt", "创建空文件或更新文件修改时间。", "touch app.log"),
                new("文件目录", "cp source target", "复制文件或目录。", "cp config.yml config.yml.bak"),
                new("文件目录", "cp -r source_dir target_dir", "递归复制目录。", "cp -r /opt/app /opt/app.bak"),
                new("文件目录", "mv source target", "移动文件或目录，也可用于重命名。", "mv old.log new.log"),
                new("文件目录", "rm file.txt", "删除文件，删除后通常无法从终端直接恢复。", "rm old.log", true),
                new("文件目录", "rm -rf /path/to/dir", "强制递归删除目录，路径写错会造成严重数据丢失。", "rm -rf /tmp/test-dir", true),
                new("文件目录", "find /path -name \"*.log\"", "按名称查找文件。", "find /var/log -name \"*.log\""),
                new("文件目录", "du -sh /path", "统计目录或文件占用空间。", "du -sh /var/log"),
                new("文件目录", "df -h", "查看文件系统磁盘容量和使用率。", "df -h"),
                new("文件目录", "ln -s source link_name", "创建软链接。", "ln -s /opt/app/current /usr/local/bin/app"),

                new("文本查看", "cat file.txt", "输出整个文件内容，适合小文件。", "cat /etc/hosts"),
                new("文本查看", "less file.txt", "分页查看文件，适合大文件。", "less /var/log/syslog"),
                new("文本查看", "head -n 100 file.txt", "查看文件前100行。", "head -n 100 app.log"),
                new("文本查看", "tail -n 100 file.txt", "查看文件最后100行。", "tail -n 100 app.log"),
                new("文本查看", "tail -f file.txt", "实时跟踪文件追加内容，常用于看日志。", "tail -f /var/log/app.log"),
                new("文本查看", "grep -n \"keyword\" file.txt", "按关键字搜索文件内容并显示行号。", "grep -n \"ERROR\" app.log"),
                new("文本查看", "grep -R \"keyword\" /path", "递归搜索目录下所有文件内容。", "grep -R \"listen\" /etc"),
                new("文本查看", "sed -n '1,120p' file.txt", "打印指定行范围内容。", "sed -n '1,120p' app.log"),
                new("文本查看", "awk '{print $1}' file.txt", "按列处理文本并输出第一列。", "awk '{print $1}' access.log"),
                new("文本查看", "sort file.txt", "对文本行排序。", "sort names.txt"),
                new("文本查看", "uniq -c file.txt", "合并相邻重复行并统计次数，通常配合sort使用。", "sort access.log | uniq -c"),
                new("文本查看", "wc -l file.txt", "统计文件行数。", "wc -l app.log"),

                new("压缩归档", "tar -czf archive.tar.gz /path", "将目录打包并使用gzip压缩。", "tar -czf logs.tar.gz /var/log/app"),
                new("压缩归档", "tar -xzf archive.tar.gz", "解压gzip格式tar包。", "tar -xzf logs.tar.gz"),
                new("压缩归档", "tar -tf archive.tar.gz", "查看tar包内文件列表。", "tar -tf logs.tar.gz"),
                new("压缩归档", "zip -r archive.zip /path", "递归压缩目录为zip文件。", "zip -r app.zip /opt/app"),
                new("压缩归档", "unzip archive.zip", "解压zip文件。", "unzip app.zip"),
                new("压缩归档", "gzip file.txt", "压缩单个文件，原文件会变为.gz。", "gzip app.log"),
                new("压缩归档", "gunzip file.txt.gz", "解压.gz文件。", "gunzip app.log.gz"),

                new("权限用户", "whoami", "显示当前登录用户。", "whoami"),
                new("权限用户", "id", "显示当前用户UID、GID和用户组。", "id"),
                new("权限用户", "groups", "查看当前用户所属用户组。", "groups"),
                new("权限用户", "chmod 755 file", "修改文件权限。", "chmod 755 startup.sh"),
                new("权限用户", "chmod -R 777 /path", "递归赋予所有用户读写执行权限，可能造成严重安全风险。", "chmod -R 777 /data/upload", true),
                new("权限用户", "chown user:group file", "修改文件所有者和用户组。", "chown app:app app.log"),
                new("权限用户", "sudo command", "以管理员权限执行命令。", "sudo systemctl restart nginx", true),
                new("权限用户", "su - user", "切换到指定用户并加载其登录环境。", "su - oracle"),
                new("权限用户", "passwd user", "修改用户密码。", "passwd appuser", true),
                new("权限用户", "useradd username", "新增系统用户。", "useradd appuser", true),
                new("权限用户", "usermod -aG group user", "将用户追加到用户组。", "usermod -aG docker appuser", true),
                new("权限用户", "userdel -r username", "删除用户并删除其家目录。", "userdel -r olduser", true),

                new("进程服务", "ps aux", "查看系统进程列表。", "ps aux"),
                new("进程服务", "ps aux | grep keyword", "按关键字查找进程。", "ps aux | grep java"),
                new("进程服务", "top", "实时查看系统进程和资源占用。", "top"),
                new("进程服务", "htop", "交互式进程监控工具，部分系统需要安装。", "htop"),
                new("进程服务", "kill PID", "向进程发送终止信号。", "kill 12345", true),
                new("进程服务", "kill -9 PID", "强制杀死进程，可能导致数据未落盘。", "kill -9 12345", true),
                new("进程服务", "pkill process_name", "按进程名结束进程。", "pkill nginx", true),
                new("进程服务", "nohup command &", "后台运行命令并忽略挂断信号。", "nohup java -jar app.jar > app.log 2>&1 &"),
                new("进程服务", "jobs", "查看当前Shell后台任务。", "jobs"),
                new("进程服务", "systemctl status service", "查看systemd服务状态。", "systemctl status nginx"),
                new("进程服务", "systemctl start service", "启动systemd服务。", "systemctl start nginx", true),
                new("进程服务", "systemctl stop service", "停止systemd服务。", "systemctl stop nginx", true),
                new("进程服务", "systemctl restart service", "重启systemd服务。", "systemctl restart nginx", true),
                new("进程服务", "journalctl -u service -f", "实时查看指定systemd服务日志。", "journalctl -u nginx -f"),

                new("网络排查", "ip addr", "查看网卡和IP地址信息。", "ip addr"),
                new("网络排查", "ip route", "查看路由表。", "ip route"),
                new("网络排查", "ping host", "测试网络连通性和延迟。", "ping 20.66.101.200"),
                new("网络排查", "traceroute host", "跟踪网络路由路径，部分系统需要安装。", "traceroute 8.8.8.8"),
                new("网络排查", "curl -I url", "请求HTTP响应头，用于测试Web服务。", "curl -I https://example.com"),
                new("网络排查", "curl -v url", "显示详细HTTP请求过程。", "curl -v http://127.0.0.1:8080/health"),
                new("网络排查", "wget url", "下载网络文件。", "wget https://example.com/file.zip"),
                new("网络排查", "ss -lntp", "查看监听中的TCP端口和进程。", "ss -lntp"),
                new("网络排查", "netstat -lntp", "查看监听端口，旧系统常用。", "netstat -lntp"),
                new("网络排查", "nc -vz host port", "测试TCP端口是否可连接。", "nc -vz 20.66.101.200 11521"),
                new("网络排查", "dig domain", "查询DNS解析结果。", "dig example.com"),
                new("网络排查", "nslookup domain", "查询域名解析，常见于较老环境。", "nslookup example.com"),
                new("网络排查", "tcpdump -i eth0 port 80", "抓取指定网卡端口流量，需要权限。", "tcpdump -i eth0 port 80", true),

                new("网络抓包", "tcpdump -D", "列出当前机器可抓包的网卡编号和名称，抓包前先确认网卡。", "tcpdump -D"),
                new("网络抓包", "tcpdump -i any", "监听所有网卡的流量，适合不知道数据走哪块网卡时快速排查。", "tcpdump -i any", true),
                new("网络抓包", "tcpdump -i eth0", "监听指定网卡 eth0 的全部流量。", "tcpdump -i eth0", true),
                new("网络抓包", "tcpdump -i eth0 -nn", "监听指定网卡并禁止解析主机名和端口名，输出更直接。", "tcpdump -i eth0 -nn", true),
                new("网络抓包", "tcpdump -i eth0 host 192.168.1.10", "只抓取与指定主机相关的流量。", "tcpdump -i eth0 host 192.168.1.10", true),
                new("网络抓包", "tcpdump -i eth0 src host 192.168.1.10", "只抓取源地址为指定主机的流量。", "tcpdump -i eth0 src host 192.168.1.10", true),
                new("网络抓包", "tcpdump -i eth0 dst host 192.168.1.10", "只抓取目标地址为指定主机的流量。", "tcpdump -i eth0 dst host 192.168.1.10", true),
                new("网络抓包", "tcpdump -i eth0 port 80", "只抓取指定端口的流量，常用于查看 HTTP 或业务端口通信。", "tcpdump -i eth0 port 80", true),
                new("网络抓包", "tcpdump -i eth0 tcp port 443", "只抓取 TCP 443 端口流量。", "tcpdump -i eth0 tcp port 443", true),
                new("网络抓包", "tcpdump -i eth0 udp port 53", "只抓取 UDP 53 端口流量，常用于 DNS 排查。", "tcpdump -i eth0 udp port 53", true),
                new("网络抓包", "tcpdump -i eth0 -c 100", "只抓取 100 个包后自动停止，避免长时间刷屏。", "tcpdump -i eth0 -c 100", true),
                new("网络抓包", "tcpdump -i eth0 -w capture.pcap", "将抓包结果写入 pcap 文件，便于后续用 Wireshark 分析。", "tcpdump -i eth0 -w capture.pcap", true),
                new("网络抓包", "tcpdump -r capture.pcap", "读取并在终端查看已有 pcap 抓包文件。", "tcpdump -r capture.pcap"),
                new("网络抓包", "tcpdump -i eth0 -A port 80", "以 ASCII 方式显示数据内容，适合查看明文 HTTP 报文。", "tcpdump -i eth0 -A port 80", true),
                new("网络抓包", "tcpdump -i eth0 -X port 80", "同时以十六进制和 ASCII 显示包内容，便于排查协议细节。", "tcpdump -i eth0 -X port 80", true),
                new("网络抓包", "tcpdump -i eth0 'tcp and port 80 and host 192.168.1.10'", "组合过滤条件，只抓取指定主机的 TCP 80 流量。", "tcpdump -i eth0 'tcp and port 80 and host 192.168.1.10'", true),
                new("网络抓包", "tshark -D", "列出 tshark 可用网卡，命令行版 Wireshark 常用入口。", "tshark -D"),
                new("网络抓包", "tshark -i eth0", "使用 tshark 监听指定网卡流量。", "tshark -i eth0", true),
                new("网络抓包", "tshark -i eth0 -f \"tcp port 80\"", "使用抓包过滤器只抓取 TCP 80 端口流量。", "tshark -i eth0 -f \"tcp port 80\"", true),
                new("网络抓包", "tshark -i eth0 -Y \"http\"", "使用显示过滤器只展示 HTTP 协议相关报文。", "tshark -i eth0 -Y \"http\"", true),
                new("网络抓包", "tshark -i eth0 -w capture.pcap", "使用 tshark 抓包并保存为 pcap 文件。", "tshark -i eth0 -w capture.pcap", true),
                new("网络抓包", "tshark -r capture.pcap", "读取 pcap 文件并在终端解析展示。", "tshark -r capture.pcap"),
                new("网络抓包", "tshark -r capture.pcap -Y \"ip.addr == 192.168.1.10\"", "读取 pcap 文件并按 IP 地址过滤显示。", "tshark -r capture.pcap -Y \"ip.addr == 192.168.1.10\""),
                new("网络抓包", "wireshark capture.pcap", "用图形化 Wireshark 打开 pcap 文件进行分析，需要桌面环境。", "wireshark capture.pcap"),
                new("网络抓包", "sudo tcpdump -i eth0 -nn -s 0 -w capture.pcap", "完整抓取指定网卡报文并保存，-s 0 表示不截断包内容。", "sudo tcpdump -i eth0 -nn -s 0 -w capture.pcap", true),

                new("磁盘系统", "free -h", "查看内存使用情况。", "free -h"),
                new("磁盘系统", "uptime", "查看系统运行时间和负载。", "uptime"),
                new("磁盘系统", "uname -a", "查看内核和系统架构信息。", "uname -a"),
                new("磁盘系统", "hostnamectl", "查看主机名和操作系统信息。", "hostnamectl"),
                new("磁盘系统", "lscpu", "查看CPU信息。", "lscpu"),
                new("磁盘系统", "lsblk", "查看块设备和挂载结构。", "lsblk"),
                new("磁盘系统", "blkid", "查看磁盘分区UUID和文件系统类型。", "blkid"),
                new("磁盘系统", "mount", "查看当前挂载信息。", "mount"),
                new("磁盘系统", "mount /dev/sdb1 /mnt/data", "挂载磁盘分区到目录。", "mount /dev/sdb1 /mnt/data", true),
                new("磁盘系统", "umount /mnt/data", "卸载挂载点。", "umount /mnt/data", true),
                new("磁盘系统", "mkfs.ext4 /dev/sdb1", "格式化分区为ext4，会清空该分区数据。", "mkfs.ext4 /dev/sdb1", true),
                new("磁盘系统", "fdisk -l", "查看磁盘分区信息。", "fdisk -l"),
                new("磁盘系统", "fdisk /dev/sdb", "交互式编辑磁盘分区表，误操作会丢数据。", "fdisk /dev/sdb", true),
                new("磁盘系统", "reboot", "重启系统。", "reboot", true),
                new("磁盘系统", "shutdown -h now", "立即关机。", "shutdown -h now", true),

                new("系统关机", "sudo shutdown -h now", "立即安全关机，shutdown会通知系统用户并按流程停止服务。", "sudo shutdown -h now", true),
                new("系统关机", "sudo poweroff", "立即关闭系统电源，适合明确需要立刻关机的场景。", "sudo poweroff", true),
                new("系统关机", "sudo shutdown -h +5", "延迟5分钟关机，适合给在线用户或正在运行的任务预留处理时间。", "sudo shutdown -h +5", true),
                new("系统关机", "sudo shutdown -c", "取消已经计划的shutdown关机任务。", "sudo shutdown -c", true),
                new("系统关机", "sudo halt", "停止系统运行，部分环境不会直接断电，使用前需确认系统行为。", "sudo halt", true),

                new("包管理", "apt update", "更新Debian/Ubuntu软件源索引。", "apt update", true),
                new("包管理", "apt install package", "安装Debian/Ubuntu软件包。", "apt install nginx", true),
                new("包管理", "apt remove package", "卸载Debian/Ubuntu软件包。", "apt remove nginx", true),
                new("包管理", "yum install package", "安装RHEL/CentOS软件包。", "yum install nginx", true),
                new("包管理", "yum remove package", "卸载RHEL/CentOS软件包。", "yum remove nginx", true),
                new("包管理", "dnf install package", "安装Fedora/RHEL新版本软件包。", "dnf install nginx", true),
                new("包管理", "rpm -qa | grep package", "查询已安装rpm包。", "rpm -qa | grep oracle"),
                new("包管理", "dpkg -l | grep package", "查询已安装deb包。", "dpkg -l | grep nginx"),

                new("Shell脚本", "echo \"text\"", "输出文本或变量。", "echo \"hello\""),
                new("Shell脚本", "export KEY=value", "设置当前Shell及子进程环境变量。", "export JAVA_HOME=/usr/lib/jvm/java-17"),
                new("Shell脚本", "env", "查看当前环境变量。", "env"),
                new("Shell脚本", "source file.sh", "在当前Shell加载脚本内容。", "source ~/.bashrc"),
                new("Shell脚本", "crontab -l", "查看当前用户定时任务。", "crontab -l"),
                new("Shell脚本", "crontab -e", "编辑当前用户定时任务。", "crontab -e", true),
                new("Shell脚本", "date", "查看或格式化当前系统时间。", "date"),
                new("Shell脚本", "history", "查看历史命令。", "history"),
                new("Shell脚本", "alias ll='ls -lah'", "设置当前Shell命令别名。", "alias ll='ls -lah'"),
                new("Shell脚本", "xargs", "把标准输入转换为命令参数。", "cat files.txt | xargs rm", true),

                new("开发运维", "git status", "查看Git工作区状态。", "git status"),
                new("开发运维", "git pull", "拉取远程仓库更新并合并。", "git pull"),
                new("开发运维", "git log --oneline -20", "查看最近20条提交。", "git log --oneline -20"),
                new("开发运维", "docker ps", "查看运行中的Docker容器。", "docker ps"),
                new("开发运维", "docker logs -f container", "实时查看容器日志。", "docker logs -f app"),
                new("开发运维", "docker restart container", "重启指定容器。", "docker restart app", true),
                new("开发运维", "docker rm container", "删除已停止容器。", "docker rm app", true),
                new("开发运维", "docker rmi image", "删除Docker镜像。", "docker rmi app:latest", true),
                new("开发运维", "docker compose up -d", "后台启动compose服务。", "docker compose up -d", true),
                new("开发运维", "docker compose down", "停止并删除compose创建的容器网络。", "docker compose down", true),
                new("开发运维", "java -version", "查看Java版本。", "java -version"),
                new("开发运维", "java -jar app.jar", "运行Java Jar程序。", "java -jar app.jar"),
                new("开发运维", "scp file user@host:/path", "通过SSH复制文件到远程服务器。", "scp app.jar root@192.168.1.10:/opt/app/"),
                new("开发运维", "ssh user@host", "登录远程Linux服务器。", "ssh root@192.168.1.10"),
            };
        }

        private sealed record LinuxCommandItem(
            string Category,
            string Command,
            string Description,
            string Example,
            bool IsDangerous = false)
        {
            public string Risk => IsDangerous ? "危险" : "常规";
        }
    }
}
