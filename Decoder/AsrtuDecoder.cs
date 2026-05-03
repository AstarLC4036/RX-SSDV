using RX_SSDV.Base;
using RX_SSDV.IO;
using RX_SSDV.Protocol.CCSDS.RS;
using RX_SSDV.UI;
using RX_SSDV.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace RX_SSDV.Decoder
{
    public class AsrtuDecoder : ITransportDecoder
    {
        public ByteOutput byteOutput;
        public byte[] dataBuffer;

        public readonly string resDirPath = $"{AppDomain.CurrentDomain.BaseDirectory}outputs";
        public readonly string binaryFilePath = $"{AppDomain.CurrentDomain.BaseDirectory}outputs/ssdv-packets.bin";
        public string imageFilePath = $"{AppDomain.CurrentDomain.BaseDirectory}outputs/output-image.jpg";
        public readonly string ssdvDecoderPath = $"{AppDomain.CurrentDomain.BaseDirectory}ssdv.exe";

        public const int RECEIVE_TIMEOUT_BASEBAND = 4000;
        public const int RECEIVE_TIMEOUT_DEFAULT = 6000;

        Process process;
        ProcessStartInfo startInfo;

        private byte[] ssdvStandardHeader =
        {
            0x55, 0x66, 0xDA, 0x4B, 0xF8, 0xEF, 0x00
        };

        private int lastImageId = -1;
        private int lastPacketId = -1;

        Timer? decoderTimer;

        public AsrtuDecoder()
        {
            byteOutput = new ByteOutput(binaryFilePath);
            dataBuffer = new byte[256];

            //SetupDecoder();
            //CheckOutputFiles();

            SampleSource.onStop += () => { byteOutput.CloseStream(); };
            SampleSource.onStart += () => { /*byteOutput.OpenStream(); CheckOutputFiles();*/ };
            SampleSource.onSourceChange += (waveFmt) => { ResetTimer(); };

            ResetTimer();
        }

        private void ResetTimer()
        {
            if(SampleSource.sourceType == SampleSource.DataSourceType.BasebandFile)
            {
                decoderTimer = new Timer(RECEIVE_TIMEOUT_BASEBAND);
            }
            else
            {
                decoderTimer = new Timer(RECEIVE_TIMEOUT_DEFAULT);
            }
            decoderTimer.Elapsed += (object? e, ElapsedEventArgs args) => {
                DecoderTimerElspsed();
            };
        }

        private void DecoderTimerElspsed()
        {
            //lastImageId = -1;
            //lastPacketId = -1;

            if (decoderTimer != null)
            {
                decoderTimer.Close();
                decoderTimer.Dispose();
                decoderTimer = null;

                Logger.LogInfo("[Packet RX][ASTRU-1] Processing SSDV packets...");
                DecodeSSDV();
            }
        }

        public void ProcessPacket(byte[] packet)
        {
            packet.FastCopyTo(dataBuffer, packet.Length, 0, 1);

            if (packet[0] == 0x03)
            {
                if (packet[1] == 0x22) // SSDV packet
                {
                    //Logger.CLogInfo("[Packet RX][ASRTU-1] SSDV packet received.");

                    // Restart timer
                    if (decoderTimer == null)
                    {
                        ResetTimer();
                    }
                    else
                    {
                        decoderTimer.Stop();
                        decoderTimer.Start();
                    }

                    // Do FEC
                    Rs8.Decode(dataBuffer, Span<int>.Empty, true);

                    // Replace with standard SSDV header
                    ReplaceSSDVHeader();

                    // Read packet header
                    int packetId, imageId;
                    (imageId, packetId) = ReadHeaderId(dataBuffer);
                    Logger.CLogInfo($"[Packet RX][ASRTU-1] SSDV packet received. Image id: {imageId}, Packet id: {packetId}");

                    // Is this a new picture?
                    if (imageId != lastImageId || packetId != lastPacketId + 1)
                    {
                        OnReceiveNewImage();

                        Logger.CLogInfo($"[Packet RX][ASRTU-1] New SSDV Image, Image id: {imageId}, Inital packet id: {packetId}");
                        if(packetId != 0)
                            Logger.CLogWarn($"[Packet RX][ASTRU-1] The ID of the new packet is not equal to 0. The SSDV decoder may not be able to output an image, and it will not clear the previous binary data cache.");
                        PostPacket(PacketData.PacketType.Image);
                    }
                    lastImageId = imageId;
                    lastPacketId = packetId;

                    // If we found the packet that its id equals 0x00, it must be the first packet.
                    if (dataBuffer[8] == 0x00)
                    {
                        byteOutput.ClearFile();
                    }
                    
                    // Calculate CRC32
                    uint crc = CalcCRC32(dataBuffer);

                    // crc uint => 4 bytes, write to buffer
                    byte crcByte = 0;
                    for(int i = 0; i < 32; i++)
                    {
                        crcByte <<= 1;
                        crcByte |= (byte)((crc >> (31 - i)) & 1);
                        if((i + 1) % 8 == 0)
                        {
                            dataBuffer[219 + (i + 1) / 8] = crcByte;
                            crcByte = 0;
                        }
                    }

                    byteOutput.WriteBytes(dataBuffer); // Write file
                }
                else if (packet[1] == 0x24) //Telemetry packet
                {
                    Logger.CLogInfo("[Packet RX][ASRTU-1] Telemetry packet received.");
                    PostPacket(PacketData.PacketType.Telemetry);
                }
            }
        }

        private void OnReceiveNewImage()
        {
            SetupDecoder();
            CheckOutputFiles();
        }

        public void PostPacket(PacketData.PacketType type)
        {
            string typeStr = "NULL";
            string additionalMsg = "-";
            PacketData data = new UndefinedPacket(type);
            switch (type)
            {
                case PacketData.PacketType.Unknown:
                    typeStr = "Unknown";
                    break;
                case PacketData.PacketType.Telemetry:
                    typeStr = "Telemetry";
                    data = new TelemPacket(type);
                    break;
                case PacketData.PacketType.Image:
                    typeStr = "SSDV Image";
                    additionalMsg = "BY2HIT";
                    data = new ImagePacket(type, imageFilePath);
                    break;
            }

            PacketInfo packetInfo = new PacketInfo("ASRTU-1(AO-123)", typeStr, additionalMsg, data);
            SatDataUI.RegisterPacket(packetInfo);
        }

        private (int,int) ReadHeaderId(byte[] packet)
        {
            byte imageId = packet[6];
            int packetId = (packet[7] << 8) | packet[8];
            return (imageId, packetId);
        }

        private uint CalcCRC32(byte[] buffer)
        {
            uint result = 0xFFFFFFFF;
            //忽略开头的0x55
            for (int i = 1; i < 220; i++)
            {
                result ^= (uint)buffer[i] & 0xFF;

                for (int j = 0; j < 8; j++)
                {
                    if ((result & 0x1) == 1)
                    {
                        result >>= 1;
                        result &= 0x7FFFFFFF;//清除最高位
                        result ^= 0xEDB88320;
                    }
                    else
                    {
                        result >>= 1;
                        result &= 0x7FFFFFFF;//清除最高位
                    }
                }
            }
            return ~result;
        }

        private void ReplaceSSDVHeader()
        {
            for (int i = 0; i < ssdvStandardHeader.Length; i++)
            {
                dataBuffer[i] = ssdvStandardHeader[i];
            }
        }

        private void SetupDecoder()
        {
            string perfix = DateTime.Now.ToString().Replace(" ", "").Replace(":", "").Replace("/", "");
            string filename = $"output-image_{perfix}.jpg";
            imageFilePath = $"{AppDomain.CurrentDomain.BaseDirectory}outputs/{filename}";

            process = new Process();
            startInfo = new ProcessStartInfo(ssdvDecoderPath, $"-d {binaryFilePath} {imageFilePath}"); // ssdv.exe -d ssdv-packets.bin output-image.jpg
            startInfo.CreateNoWindow = true;
            process.StartInfo = startInfo;
        }

        private void CheckOutputFiles()
        {
            if (!Directory.Exists(resDirPath))
                Directory.CreateDirectory(resDirPath);
            if(!File.Exists(binaryFilePath))
                File.Create(binaryFilePath);
            if(!File.Exists(imageFilePath))
                File.Create(imageFilePath);

            // also, we need to clear the output image file to make sure the ssdv.exe can works properly
            try
            {
                using (FileStream stream = new FileStream(imageFilePath, FileMode.Create, FileAccess.ReadWrite))
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    stream.SetLength(0);
                    stream.Flush();
                    stream.Close();
                    stream.Dispose();
                }
            }
            catch(Exception)
            {
                Logger.CLogWarn("[Packet RX][ASRTU-1] Failed to clear the output image file; it may be in use.");
            }
        }

        private void DecodeSSDV()
        {
            if (!File.Exists(ssdvDecoderPath))
            {
                Logger.LogErr($"Decode SSDV failed, could not found decoder: '{ssdvDecoderPath}'");
                return;
            }
            StartDecoderProcess();
        }

        public bool StartDecoderProcess()
        {
            process.Start();

            Task.Run(() =>
            {
                process.WaitForExit();
                process.Close();

                Logger.CLogInfo("[Packet RX][ASRTU-1] Packet process completed.");

                if (SatDataUI.currentImagePacket != null && SatDataUI.currentImagePacket.imagePath == imageFilePath)
                {
                    SatDataUI.UpdateImage(); //Update UI
                }
            });

            return true;
        }
    }
}
