using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mospel.MqttBroker
{
    public class MqttServerOptions
    {
        public Action<MqttSubscriptionInterceptor> SubscriptionInterceptor { get; set; }
        public Action<MqttPublishInterceptor> PublishInterceptor { get; set; }
        public Action<MqttConnectionInterceptor> ConnectionInterceptor { get; set; }

    }
}
