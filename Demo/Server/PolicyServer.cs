using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetworkSocket;

namespace Server
{
    /// <summary>
    /// SilverLight授权服务封包协议
    /// </summary>
    public class PolicyPacket : PacketBase
    {
        public Byte[] Buffer { get; private set; }

        public PolicyPacket(byte[] bytes)
        {
            this.Buffer = bytes;
        }

        public override byte[] ToByteArray()
        {
            return this.Buffer;
        }

        public static PolicyPacket GetPacket(ByteBuilder builder)
        {
            if (builder.Length == 0) return null;

            var bytes = builder.ToArray();
            builder.Clear();
            return new PolicyPacket(bytes);
        }
    }

    /// <summary>
    /// SilverLight授权服务
    /// </summary>
    public class PolicyServer : TcpServerBase<PolicyPacket>
    {
        public void Start()
        {
            const int port = 943;
            this.StartListen(new System.Net.IPEndPoint(System.Net.IPAddress.Any, port));
        }

        protected override PolicyPacket OnReceive(ByteBuilder recvBuilder)
        {
            return PolicyPacket.GetPacket(recvBuilder);
        }

        protected override void OnRecvComplete(SocketAsync<PolicyPacket> client, PolicyPacket packet)
        {
            var xml = new StringBuilder();
            xml.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
            xml.AppendLine("<access-policy>");
            xml.AppendLine("<cross-domain-access>");
            xml.AppendLine("<policy>");
            xml.AppendLine("<allow-from>");
            xml.AppendLine("<domain uri=\"*\"/>");
            xml.AppendLine("</allow-from>");
            xml.AppendLine("<grant-to>");
            xml.AppendLine("<socket-resource port=\"4502-4534\" protocol=\"tcp\"/>");
            xml.AppendLine("</grant-to>");
            xml.AppendLine("</policy>");
            xml.AppendLine("</cross-domain-access>");
            xml.AppendLine("</access-policy>");
            packet = new PolicyPacket(Encoding.UTF8.GetBytes(xml.ToString()));
            client.Send(packet);
            this.CloseClient(client);
        }
    }
}
