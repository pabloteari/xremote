using System;
using System.Collections.Generic;
using SmartGlass.Nano.Packets;

namespace SmartGlass.Nano.Consumer
{
    public enum AACProfile
    {
        MAIN = 0,
        LC = 1,
        SSR = 2,
        LTP = 3
    }

    public class AacAdtsAssembler
    {
        private const byte ADTS_HEADER_LENGTH = 7;
        private const int ADTS_HEADER_ID = 0; // MPEG4
        private readonly static Dictionary<int, byte> SamplingFrequencyIndex =
            new Dictionary<int, byte>{
                {96000, 0},
                {88200, 1},
                {64000, 2},
                {48000, 3},
                {44100, 4},
                {32000, 5},
                {24000, 6},
                {22050, 7},
                {16000, 8},
                {12000, 9},
                {11025, 10},
                {8000,  11},
                {7350,  12}
        };

        public static byte GetSamplingFrequencyIndex(int sampleRate)
        {
            byte result;
            bool success = SamplingFrequencyIndex.TryGetValue(sampleRate,
                                                              out result);
            if (!success)
            {
                throw new ArgumentException("Invalid sampleRate requested");
            }
            return result;
        }

        public static byte[] AssembleAudioFrame(byte[] data, AACProfile profile,
                                                int samplingFreq, byte channels)
        {
            byte[] adtsHeader = new byte[ADTS_HEADER_LENGTH];
            int frameSize = data.Length + ADTS_HEADER_LENGTH;
            byte sampling_index = GetSamplingFrequencyIndex(samplingFreq);

            adtsHeader[0] = 0xFF;
            adtsHeader[1] = 0xF0 | (ADTS_HEADER_ID << 3) | 0x1;
            adtsHeader[2] = (byte)(((byte)profile << 6) | (sampling_index << 2) | 0x2 | (channels & 0x4));
            adtsHeader[3] = (byte)(((channels & 0x3) << 6) | 0x30 | (frameSize >> 11));
            adtsHeader[4] = (byte)((frameSize >> 3) & 0x00FF);
            adtsHeader[5] = (byte)(((frameSize & 0x0007) << 5) + 0x1F);
            adtsHeader[6] = 0xFC;

            return adtsHeader;
        }
    }

    public class AACFrame
    {
        public readonly uint Flags;
        public readonly ulong TimeStamp;
        public readonly uint FrameId;
        public readonly AACProfile Profile;
        public readonly int SampleRate;
        public readonly byte Channels;
        public readonly byte[] RawData;

        public AACFrame(byte[] data, ulong timeStamp, uint frameId, uint flags,
                        AACProfile profile, int sampleRate, byte channels)
        {
            RawData = data;
            TimeStamp = timeStamp;
            FrameId = frameId;
            Flags = flags;
            Profile = profile;
            SampleRate = sampleRate;
            Channels = channels;
        }

        public byte[] GetSamplesWithHeader()
        {
            return AacAdtsAssembler.AssembleAudioFrame(
                RawData, Profile, SampleRate, Channels);
        }

        public byte[] GetCodecSpecificData()
        {
            byte sampleIndex = AacAdtsAssembler.GetSamplingFrequencyIndex(SampleRate);
            byte[] csd0 = new byte[2];
            csd0[0] = (byte)(((byte)Profile << 3) | (sampleIndex >> 1));
            csd0[1] = (byte)((byte)((sampleIndex << 7) & 0x80) | (Channels << 3));

            return csd0;
        }
    }
}