# Mospel
 
## .Net MQTT Broker

**Description**

this broker was built with the intention to use with your other .net application, in my case I had to add a chat and some real-time data exchange in my application, and I wanted to go with a standard way of doing this, so instead of creating a typical socket application, I chose to go with MQTT Broker, which is really handy when handling messages for different users.


**Features**

Authentication and Authorization, I have added some interceptors in this library so whenver you have clients connecting to broker, you can validate the credentials from your database, on top of it also allows you to intercept the publish and subscribe requests so you can control who should be able to subscribe to specific topics and publish to them.


**Limitations**

1. for now this broker works with websocket only, I will add regular TCP socket support to it.
2. it doesn't work with TLS yet.


**Dependencies**

[Fleck](https://www.nuget.org/packages/Fleck/) is a nuget packate that is being used to communicate over WebSocket

**Compatibility**

this broker is compatible with any WebSocket based MQTT client, I've tested with [Paho MQTT Client](https://www.eclipse.org/paho/clients/js/)
