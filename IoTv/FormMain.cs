/*
本示例演示了使用MQTTnet和SunnyUI开发的物联网可视化项目。

1.MQTTnet是一个高性能的.NET库。用于连接MQTT服务器，订阅主题和向主题发布消息。

MQTTnet相关的资源链接：

GitHub：https://github.com/dotnet/MQTTnet
NuGet：https://www.nuget.org/packages/MQTTnet/

2.SunnyUI.NET是基于.NET框架的C# WinForm开源控件库、工具类库、扩展类库、多页面开发框架。用于简化可视化用户界面的设计。 

SunnyUI相关的网络链接：

帮助文档：https://gitee.com/yhuse/SunnyUI/wikis/pages
Gitee：https://gitee.com/yhuse/SunnyUI
GitHub：https://github.com/yhuse/SunnyUI
Nuget：https://www.nuget.org/packages/SunnyUI/

3.SIoT是一个为教育定制的跨平台的开源MQTT服务器程序。建议使用SIoT1.3作为MQTT服务器进行本示例程序的测试。

SIoT相关的网络链接：

使用手册：https://siot.readthedocs.io/zh-cn/latest/
Gitee：https://gitee.com/vvlink/SIoT
GitHub：https://github.com/vvlink/SIoT/
*/

using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using MQTTnet.Server;
using Sunny.UI;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IoTv
{
    public partial class FormMain : UIForm
    {
        public static IMqttClient MqttClient;
        /// <summary>
        /// MQTT服务器的IP地址或域名
        /// </summary>
        public string Mqtt_Server = "127.0.0.1";
        /// <summary>
        /// MQTT服务器的端口，一般为1883
        /// </summary>
        public int Mqtt_Port = 1883;
        /// <summary>
        /// MQTT服务器的用户名
        /// </summary>
        public string Mqtt_UserName = "siot";
        /// <summary>
        /// MQTT服务器的密码
        /// </summary>
        public string Mqtt_PassWord = "dfrobot";
        /// <summary>
        /// MQTT客户端的ID
        /// </summary>
        public string Mqtt_ClientId = "MyClient";

        // 主题风格全局变量
        public UIStyle UiStyle = UIStyle.Blue;

        // 用于产生测试数据的随机数
        private readonly Random ran = new Random();

        /// <summary>
        /// 使用指定的参数连接MQTT服务器
        /// </summary>
        public void MqttClientStart()
        {
            // MQTT连接参数
            var mqttClientOptionsBuilder = new MqttClientOptionsBuilder()
                .WithTcpServer(Mqtt_Server, Mqtt_Port)	// MQTT服务器的IP和端口号
                .WithCredentials(Mqtt_UserName, Mqtt_PassWord)      // MQTT服务器的用户名和密码
                .WithClientId(Mqtt_ClientId + Guid.NewGuid().ToString("N"))	// 自动设置客户端ID的后缀，以免出现重复
                .WithCleanSession();

            var mqttClientOptions = mqttClientOptionsBuilder.Build();
            MqttClient = new MqttFactory().CreateMqttClient();

            // 客户端连接成功事件
            MqttClient.ConnectedAsync += MqttClient_ConnectedAsync;
            // 客户端连接关闭事件
            MqttClient.DisconnectedAsync += MqttClient_DisconnectedAsync;
            // 收到订阅消息事件
            MqttClient.ApplicationMessageReceivedAsync += MqttClient_ApplicationMessageReceivedAsync;
            // 连接MQTT服务器
            try
            {
                MqttClient.ConnectAsync(mqttClientOptions);
            }
            catch (Exception ex)
            {
                this.Text = "物联网可视化示例 - " + ex.Message;
            }
        }

        /// <summary>
        /// 断开已经连接的MQTT服务器
        /// </summary>
        public void MqttClientStop()
        {
            if (MqttClient != null && MqttClient.IsConnected)
            {
                var mqttClientDisconnectOptions = new MqttFactory().CreateClientDisconnectOptionsBuilder().Build();
                MqttClient.DisconnectAsync(mqttClientDisconnectOptions, CancellationToken.None);
                MqttClient.Dispose();
                MqttClient = null;
            }
        }

        /// <summary>
        /// 当MQTT服务器连接被断开时发生的事件
        /// </summary>
        private Task MqttClient_DisconnectedAsync(MqttClientDisconnectedEventArgs arg)
        {
            this.Invoke(new Action(() =>
            {
                this.Text = "物联网可视化示例 - 没有连接到MQTT服务器，请先连接MQTT服务器";
            }));
            return Task.CompletedTask;
        }

        /// <summary>
        /// 当MQTT服务器成功连接时发生的事件
        /// </summary>
        private Task MqttClient_ConnectedAsync(MqttClientConnectedEventArgs arg)
        {
            this.Invoke(new Action(() =>
            {
                this.Text = "物联网可视化示例 - 成功连接到MQTT服务器";
            }));

            // 订阅消息主题
            // MqttQualityOfServiceLevel: （QoS）:
            // AtMostOnce 0: 最多一次，接收者不确认收到消息，并且消息不被发送者存储和重新发送提供与底层 TCP 协议相同的保证。
            // AtLeastOnce 1: 保证一条消息至少有一次会传递给接收方。发送方存储消息，直到它从接收方收到确认收到消息的数据包。一条消息可以多次发送或传递。
            // ExactlyOnce 2: 保证每条消息仅由预期的收件人接收一次。级别2是最安全和最慢的服务质量级别，保证由发送方和接收方之间的至少两个请求/响应（四次握手）。
            //MqttClient.SubscribeAsync("Project/Topic1", MqttQualityOfServiceLevel.AtLeastOnce);
            //MqttClient.SubscribeAsync("Project/Topic2", MqttQualityOfServiceLevel.AtLeastOnce);
            //MqttClient.SubscribeAsync("Project/Topic3", MqttQualityOfServiceLevel.AtLeastOnce);

            MqttClient.SubscribeAsync("IoTv/TopicTest1", MqttQualityOfServiceLevel.AtLeastOnce);
            MqttClient.SubscribeAsync("IoTv/TopicTest2", MqttQualityOfServiceLevel.AtLeastOnce);

            return Task.CompletedTask;
        }

        /// <summary>
        /// 当从MQTT服务器收到订阅消息的事件
        /// </summary>
        private Task MqttClient_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
        {
            this.Invoke(new Action(() =>
            {
                switch (arg.ApplicationMessage.Topic)
                {
                    case "Project/Topic1":
                        //var message1 = arg.ApplicationMessage.ConvertPayloadToString();
                        break;
                    case "Project/Topic2":
                        //var message2 = arg.ApplicationMessage.ConvertPayloadToString();
                        break;
                    case "Project/Topic3":
                        //var message3 = arg.ApplicationMessage.ConvertPayloadToString();
                        break;
                    case "IoTv/TopicTest1":
                        DateTime dateTime1 = DateTime.Now;
                        int temperature = 0;
                        Int32.TryParse(arg.ApplicationMessage.ConvertPayloadToString(), out temperature);
                        this.uiThermometer1.Value = temperature;

                        // 添加折线图数据
                        this.uiLineChart1.Option.AddData("Line1", dateTime1, temperature);
                        // 设置X轴的显示范围为60秒以内
                        this.uiLineChart1.Option.XAxis.SetRange(dateTime1.AddSeconds(-60).ZeroMillisecond(), dateTime1.ZeroMillisecond());
                        // 更新折线图显示
                        this.uiLineChart1.Refresh();

                        // 向数据表添加数据记录
                        string[] temperatureData = { dateTime1.ToString(), "温度", temperature.ToString() + " ℃" };
                        this.uiDataGridView1.AddRow(temperatureData);
                        this.uiDataGridView1.Sort(uiDataGridView1.Columns[0], ListSortDirection.Descending);
                        // 清除数据表的选中状态，以下三句任意一句即可
                        this.uiDataGridView1.ClearSelection();
                        //this.uiDataGridView1.CurrentCell = null;
                        //this.uiDataGridView1.Rows[0].Selected = false;
                        break;
                    case "IoTv/TopicTest2":
                        DateTime dateTime2 = DateTime.Now;
                        int humidity = 0;
                        Int32.TryParse(arg.ApplicationMessage.ConvertPayloadToString(), out humidity);
                        this.uiLedLabel1.Text = humidity.ToString() + "%";
                        this.uiAnalogMeter1.Value = humidity;

                        // 添加折线图数据
                        this.uiLineChart1.Option.AddData("Line2", dateTime2, humidity);
                        // 设置X轴的显示范围为60秒以内
                        this.uiLineChart1.Option.XAxis.SetRange(dateTime2.AddSeconds(-60).ZeroMillisecond(), dateTime2.ZeroMillisecond());
                        // 更新折线图显示
                        this.uiLineChart1.Refresh();

                        // 向数据表添加数据记录
                        string[] humidityData = { dateTime2.ToString(), "湿度", humidity.ToString() + " ％" };
                        this.uiDataGridView1.AddRow(humidityData);
                        this.uiDataGridView1.Sort(uiDataGridView1.Columns[0], ListSortDirection.Descending);
                        // 清除数据表的选中状态，以下三句任意一句即可
                        this.uiDataGridView1.ClearSelection();
                        //this.uiDataGridView1.CurrentCell = null;
                        //this.uiDataGridView1.Rows[0].Selected = false;
                        break;
                    default:
                        break;
                }
            }));
            return Task.CompletedTask;
        }

        /// <summary>
        /// 向MQTT服务器指定的主题发送消息
        /// </summary>
        /// <param name="mqttTopic">发布消息的主题</param>
        /// <param name="mqttMessage">发布消息的内容</param>
        public async void MqttClientPublishAsync(string mqttTopic, string mqttMessage)
        {
            using (var mqttPublishClient = new MqttFactory().CreateMqttClient())
            {
                var mqttPublishClientOptions = new MqttClientOptionsBuilder()
                    .WithTcpServer(Mqtt_Server, Mqtt_Port)    // MQTT服务器的IP和端口号
                    .WithCredentials(Mqtt_UserName, Mqtt_PassWord)      // MQTT服务器的用户名和密码
                    .WithClientId(Mqtt_ClientId + Guid.NewGuid().ToString("N"))   // 自动设置客户端ID的后缀，以免出现重复
                    .WithCleanSession()
                    .Build();

                try
                {
                    await mqttPublishClient.ConnectAsync(mqttPublishClientOptions, CancellationToken.None);

                    var mqttApplicationMessage = new MqttApplicationMessageBuilder()
                        .WithTopic(mqttTopic)           // 消息主题
                        .WithPayload(mqttMessage)       // 消息内容
                        .Build();

                    await mqttPublishClient.PublishAsync(mqttApplicationMessage, CancellationToken.None);

                    await mqttPublishClient.DisconnectAsync();
                }
                catch (Exception ex)
                {
                    this.Text = "物联网可视化示例 - " + ex.Message;
                }
            }
        }

        public FormMain()
        {
            InitializeComponent();
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            // 初始化折线图
            UILineOption optionLineChart = new UILineOption
            {
                // 设置标题
                Title = new UITitle
                {
                    Text = "实时测量折线图",
                    SubText = "（环境温湿度测量数据）"
                },

                // 设置Legend（图例）
                Legend = new UILegend
                {
                    Orient = UIOrient.Horizontal,
                    Top = UITopAlignment.Top,
                    Left = UILeftAlignment.Left,
                    Colors = { UIColor.Blue, UIColor.Orange }
                }
            };

            optionLineChart.Legend.AddData("温度");
            optionLineChart.Legend.AddData("湿度");
            optionLineChart.ToolTip.Visible = true;

            var series1 = optionLineChart.AddSeries(new UILineSeries("Line1"));
            var series2 = optionLineChart.AddSeries(new UILineSeries("Line2"));
            series1.CustomColor = true;
            series2.CustomColor = true;
            series1.Color = UIColor.Blue;
            series2.Color = UIColor.Orange;

            // 设置曲线显示最大点数，超过后自动清理
            series1.SetMaxCount(66);
            series2.SetMaxCount(66);

            // 坐标轴设置
            optionLineChart.XAxis.Name = "时间";
            optionLineChart.YAxis.Name = "数值";
            // 坐标轴显示日期格式
            optionLineChart.XAxisType = UIAxisType.DateTime;
            optionLineChart.XAxis.AxisLabel.DateTimeFormat = "HH:mm:ss";
            // 坐标轴显示小数位数
            optionLineChart.YAxis.AxisLabel.DecimalPlaces = 0;

            this.uiLineChart1.SetOption(optionLineChart);

            // SunnyUI开源项目相关网络链接
            // 帮助文档：https://gitee.com/yhuse/SunnyUI/wikis/pages
            // Gitee：https://gitee.com/yhuse/SunnyUI
            // GitHub：https://github.com/yhuse/SunnyUI
            // Nuget：https://www.nuget.org/packages/SunnyUI/

            // 设置默认的主题风格
            UiStyle = UIStyle.Blue;
            this.Style = UiStyle;
            this.uiLineChart1.ChartStyleType = UIChartStyleType.Plain;
            this.uiLineChart1.Style = UiStyle;
            this.uiDataGridView1.Style = UiStyle;
            this.uiContextMenuStrip1.Style = UiStyle;
            this.uiLine1.Style = UiStyle;
            this.uiLine2.Style = UiStyle;
            this.uiLine3.Style = UiStyle;
            this.uiLine4.Style = UiStyle;
            this.uiLedLabel1.Style = UiStyle;
            this.uiLedLabel1.ForeColor = Color.DarkOrange;
            this.uiAnalogMeter1.Style = UiStyle;
            this.uiThermometer1.Style = UiStyle;
            this.uiLineChart1.Refresh();

            // 建议使用SIoT1.3作为MQTT服务器进行测试
            // 使用手册：https://siot.readthedocs.io/zh-cn/latest/
            // Gitee：https://gitee.com/vvlink/SIoT
            // GitHub：https://github.com/vvlink/SIoT/

            // 可以在任意位置重新设置需要连接的MQTT服务参数
            //Mqtt_Server = "127.0.0.1";
            //Mqtt_Port = 1883;
            //Mqtt_UserName = "siot";
            //Mqtt_PassWord = "dfrobot";
            //Mqtt_ClientId = "MyClient";

            // MQTTnet相关资源链接
            // GitHub：https://github.com/dotnet/MQTTnet
            // NuGet：https://www.nuget.org/packages/MQTTnet/

            // 增加连接MQTT服务器的对话框，不再自动连接MQTT服务器
            // 请在标题栏现的扩展按钮链接的下拉菜单中进行相关操作         

            // 使用MQTTnet连接MQTT服务器
            //MqttClientStop();
            //MqttClientStart();

            // 启动产生模拟数据的计时器Timer1
            //timer1.Enabled = true; 
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            MqttClientStop();
        }

        private void FormMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            // 产生温湿度的随机数据
            // 真实环境下，可使用开源硬件的传感器获取环境温湿度数据并通过主控板发布至MQTT服务器相关主题
            int temperature = ran.Next(10, 30);
            int humidity = ran.Next(10, 85);
            // 模拟传感器向MQTT服务器发布数据
            MqttClientPublishAsync("IoTv/TopicTest1", temperature.ToString());
            MqttClientPublishAsync("IoTv/TopicTest2", humidity.ToString());
        }

        private void ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            // 设置浅色主题风格
            UiStyle = UIStyle.Blue;
            this.Style = UiStyle;
            this.uiLineChart1.ChartStyleType = UIChartStyleType.Plain;
            this.uiLineChart1.Style = UiStyle;
            this.uiDataGridView1.Style = UiStyle;
            this.uiContextMenuStrip1.Style = UiStyle;
            this.uiLine1.Style = UiStyle;
            this.uiLine2.Style = UiStyle;
            this.uiLine3.Style = UiStyle;
            this.uiLine4.Style = UiStyle;
            this.uiLedLabel1.Style = UiStyle;
            this.uiLedLabel1.ForeColor = Color.DarkOrange;
            this.uiAnalogMeter1.Style = UiStyle;
            this.uiThermometer1.Style = UiStyle;
            this.uiLineChart1.Refresh();
        }

        private void ToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            // 设置深色主题风格
            UiStyle = UIStyle.DarkBlue;
            this.Style = UiStyle;
            this.uiLineChart1.ChartStyleType = UIChartStyleType.LiveChart;
            this.uiLineChart1.Style = UiStyle;
            this.uiDataGridView1.Style = UiStyle;
            this.uiContextMenuStrip1.Style = UiStyle;
            this.uiLine1.Style = UiStyle;
            this.uiLine2.Style = UiStyle;
            this.uiLine3.Style = UiStyle;
            this.uiLine4.Style = UiStyle;
            this.uiLedLabel1.Style = UiStyle;
            this.uiLedLabel1.ForeColor = Color.DarkOrange;
            this.uiAnalogMeter1.Style = UiStyle;
            this.uiThermometer1.Style = UiStyle;
            this.uiLineChart1.Refresh();
        }

        private void ToolStripMenuItem3_Click(object sender, EventArgs e)
        {
            FormMqtt formMqtt = new FormMqtt(this);
            formMqtt.ShowDialog();
        }
    }
}
