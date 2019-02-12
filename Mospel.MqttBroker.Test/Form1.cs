using Mospel.MqttBroker;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Mospel
{
    public partial class Form1 : Form
    {
        MqttServer server;
        bool Started = false;
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!Started)
            {
                server = new MqttServer("0.0.0.0", 8181);
                server.ServerOptions = new MqttServerOptions()
                {
                    ConnectionInterceptor = context =>
                    {
                        if (context.Request.Username == "tariq" && context.Request.Password == "shiwani")
                        {
                            context.ReturnCode = Protocol.MqttConnectReturnCode.ConnectionAccepted;
                        }
                        else
                        {
                            context.ReturnCode = Protocol.MqttConnectReturnCode.ConnectionRefusedBadUsernameOrPassword;
                        }
                    },
                    SubscriptionInterceptor = context =>
                    {
                        context.Allowed = true;
                    },
                    PublishInterceptor = context =>
                    {
                        context.Allowed = true;
                    }
                };

                server.Start();
                Started = true;
                button1.Text = "Stop";
            }
            else
            {
                server.Stop();
                Started = false;
                button1.Text = "Start";
            }
        }
    }
}
