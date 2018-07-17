using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;

namespace codecs
{
    public interface INetworkChatCodec :IDisposable
    {
        string Name { get; }
        bool IsAvailable { get; }
        int BitsPerSecond { get; }
        WaveFormat RecordFormat { get; }

        byte[] Encode(byte[] data, int offset, int length);
        byte[] Decode(byte[] data, int offset, int length);

    }
}
