using Mospel.MqttSerializer;
using Mospel.Protocol;
using System;
using System.Linq;

namespace Mospel
{
    public class MqttPacket
    {

        public MqttControlPacketType PacketType { get; set; }
        public bool Retain { get; set; }
        public bool Duplicate { get; set; }
        public int QoS { get; set; }
        public Int16 PacketIdentifier { get; set; }
        public int BodyLength { get; set; }
        public byte[] PayLoad { get; set; }

        public static MqttPacket Parse(byte[] data)
        {
            int i = 0;
            MqttPacket mp = new MqttPacket();

            mp.PacketType = (MqttControlPacketType)(data[i] >> 4);
            mp.QoS = ((byte)(data[i] << 5)) >> 6;
            mp.Retain = Convert.ToBoolean(((byte)(data[i] << 7)) >> 7);
            mp.Duplicate = Convert.ToBoolean(((byte)(data[i] << 4)) >> 7);

            i++;
            byte newByte = 0;
            int multiplier = 1;
            do
            {
                newByte = data[i];
                mp.BodyLength += (newByte & 127) * multiplier;
                multiplier *= 128;
                if (multiplier > 128 * 128 * 128)
                    throw new MqttProtocolViolationException("Malformed Remaining Length");
                i++;
            } while ((newByte & 128) != 0);

            data.Skip(i);
            mp.PayLoad = data.Take<byte>(mp.BodyLength).ToArray();

            //if ((mp.PacketType == PACKET_TYPE.PUBLISH && mp.QoS > 0) || mp.PacketType == PACKET_TYPE.PUBACK ||
            //    mp.PacketType == PACKET_TYPE.PUBREC || mp.PacketType == PACKET_TYPE.PUBREL ||
            //    mp.PacketType == PACKET_TYPE.PUBCOMP || mp.PacketType == PACKET_TYPE.SUBSCRIBE ||
            //    mp.PacketType == PACKET_TYPE.SUBACK || mp.PacketType == PACKET_TYPE.UNSUBSCRIBE ||
            //    mp.PacketType == PACKET_TYPE.UNSUBACK)
            //{
            //    // implement later
            //    mp.PacketIdentifier = 0;
            //    i += 2;
            //}
            return mp;
        }

        


    }


}
