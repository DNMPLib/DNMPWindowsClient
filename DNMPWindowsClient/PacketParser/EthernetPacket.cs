﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Net.NetworkInformation;

namespace DNMPWindowsClient.PacketParser
{
    internal class EthernetPacket : IPacket
    {
        internal enum PacketType : ushort
        {
            IPv4 = 0x0800,
            Arp = 0x0806
        }

        internal PhysicalAddress DestinationAddress;
        internal PhysicalAddress SourceAddress;
        internal PacketType Type;
        internal IPacket PayloadPacket;

        internal EthernetPacket(Stream packetStream)
        {
            var reader = new BinaryReader(packetStream);
            DestinationAddress = new PhysicalAddress(reader.ReadBytes(6));
            SourceAddress = new PhysicalAddress(reader.ReadBytes(6));
            Type = (PacketType) reader.ReadUInt16();
            switch (Type)
            {
                case PacketType.Arp:
                    break;
                case PacketType.IPv4:
                    break;
                default:
                    PayloadPacket = new DummyPacket(packetStream);
                    break;
            }
        }
        internal static EthernetPacket Parse(byte[] bytes)
        {
            var packetStream = new MemoryStream(bytes);
            return new EthernetPacket(packetStream);
        }

        public byte[] Payload => PayloadPacket.ToBytes();

        public byte[] ToBytes()
        {
            var stream = new MemoryStream();
            ToStream(stream);
            return stream.ToArray();
        }

        public void ToStream(Stream streamTo)
        {
            var writer = new BinaryWriter(streamTo);
            writer.Write(DestinationAddress.GetAddressBytes());
            writer.Write(SourceAddress.GetAddressBytes());
            writer.Write((ushort) Type);
            PayloadPacket.ToStream(streamTo);
        }
    }
}
