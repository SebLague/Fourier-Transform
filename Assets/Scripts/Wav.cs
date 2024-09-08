using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Text;

public static class Wav
{
    const uint WAV_FORMAT_PCM = 0x0001;
    const uint WAV_FORMAT_IEEE_FLOAT = 0x0003;
    const uint WAV_FORMAT_EXTENSIBLE = 0xFFFE;

    public static Signal ReadWav(string path)
    {
        using Stream stream = File.Open(path, FileMode.Open);
        using BinaryReader reader = new(stream);
        return ReadWavFile(reader);
    }

    // Write signal to .wav file (16-bit pcm mono)
    public static void WriteWav(Signal signal, string path)
    {
        using var stream = File.Open(path, FileMode.Create);
        using var writer = new BinaryWriter(stream, Encoding.UTF8, false);

        const int BytesPerSample = 2;
        int numBytes = signal.NumSamples * BytesPerSample;
        // ---- RIFF chunk ('resource interchange file format') ----
        writer.Write(Encoding.UTF8.GetBytes("RIFF"));
        writer.Write(36 + numBytes); // file size
        writer.Write(Encoding.UTF8.GetBytes("WAVE"));
        // ---- Format chunk ----
        writer.Write(Encoding.UTF8.GetBytes("fmt "));
        writer.Write(16); // chunk size
        writer.Write((short)WAV_FORMAT_PCM); // format code
        writer.Write((short)1); // num channels (1 = mono, 2 = stereo)
        writer.Write(signal.SampleRate);
        writer.Write(signal.SampleRate * BytesPerSample); // bytes per second
        writer.Write((short)BytesPerSample);
        writer.Write((short)(BytesPerSample * 8)); // bit depth
        // ---- Data chunk ----
        writer.Write(Encoding.UTF8.GetBytes("data"));
        writer.Write(numBytes);
        for (int i = 0; i < signal.Samples.Length; i++)
        {
            short val16 = (short)(Math.Min(1, Math.Max(-1, signal.Samples[i])) * short.MaxValue);
            writer.Write((byte)(val16 & 0x00FF)); // least significant byte
            writer.Write((byte)(val16 >> 8)); // most significant byte
        }
    }

    public static string GetWavPath(string directory, string fileName)
    {
        return Path.Combine(Application.dataPath, "Wav", directory, $"{fileName}.wav");
    }

    public static string GetWavPath(string fileName)
    {
        return Path.Combine(Application.dataPath, "Wav", $"{fileName}.wav");
    }


    static Signal ReadWavFile(BinaryReader reader)
    {
        Dictionary<string, long> chunkLookup = CreateWavChunkLookup(reader);
        reader.BaseStream.Position = chunkLookup["fmt "] + 8;
        int formatCode = reader.ReadUInt16(); // pcm, float, a-law, mu-law, or extended format
        int numChannels = reader.ReadInt16(); // mono (1) or stereo (2)
        int sampleRate = reader.ReadInt32(); // num samples per second
        int bitsPerSample = Skip(reader, 6).ReadInt16(); // typically 8, 16, 24, or 32
        int bytesPerSample = bitsPerSample / 8;
        if (formatCode == WAV_FORMAT_EXTENSIBLE) formatCode = Skip(reader, 8).ReadUInt16();

        reader.BaseStream.Position = chunkLookup["data"] + 4;
        int numBytesData = reader.ReadInt32();
        byte[] data = reader.ReadBytes(numBytesData);

        // Convert raw bytes to doubles (note: flattening to mono)
        float[] samples = new float[numBytesData / (bytesPerSample * numChannels)];
        float normFactor = 1f / (MathF.Pow(2, bitsPerSample - 1) - 1);
        if (formatCode == WAV_FORMAT_IEEE_FLOAT) normFactor = 1;

        for (int i = 0; i < samples.Length; i++)
        {
            int offset = i * bytesPerSample * numChannels;
            ReadOnlySpan<byte> sampleBytes = data.AsSpan(offset, bytesPerSample);
            samples[i] = BytesToFloat(sampleBytes, formatCode) * normFactor;
        }

        return new Signal(samples, sampleRate);
    }

    static BinaryReader Skip(BinaryReader r, int n)
    {
        r.BaseStream.Position += n;
        return r;
    }

    // Convert an array of bytes (max 4) to a float value based on the given wavFormatCode
    // Supported formats are: 1, 2, 3, and 4-byte PCM data; as well as 4-byte float data.
    static float BytesToFloat(ReadOnlySpan<byte> bytes, int wavFormatCode)
    {
        if (wavFormatCode == WAV_FORMAT_PCM)
        {
            // Convert bytes to int, and then rely on implicit conversion to float
            return bytes.Length switch
            {
                1 => bytes[0] - 128, // 8-bit (unsigned) 
                2 => BitConverter.ToInt16(bytes), // 16-bit
                // 24-bit requires some special handling since there is no native 24-bit type.
                // Essentially, we create a 32-bit int where the upper byte has all bits set to
                // the same value as the msb of the 24-bit value. This ensures that the sign of
                // the value (two's complement representation) is correctly preserved.
                3 => (bytes[2] >> 7) * (0xFF << 24) | bytes[2] << 16 | bytes[1] << 8 | bytes[0],
                4 => BitConverter.ToInt32(bytes), // 32-bit
                _ => throw new Exception($"Unsupported byte count: {bytes.Length}")
            };
        }
        else if (wavFormatCode == WAV_FORMAT_IEEE_FLOAT) return BitConverter.ToSingle(bytes);

        // 'TODO': A-law & Mu-law formats
        throw new Exception($"Unsupported format code: {wavFormatCode}");
    }

    // Create a dictionary to look up the position of each chunk by its ID
    static Dictionary<string, long> CreateWavChunkLookup(BinaryReader reader)
    {
        // Read main 'RIFF' chunk (resource interchange file format)
        string riffID = reader.ReadChars(4).AsSpan().ToString();
        reader.ReadInt32(); // file size
        string waveID = reader.ReadChars(4).AsSpan().ToString();
        Debug.Assert(riffID == "RIFF" && waveID == "WAVE", "Invalid .wav file");

        // Create chunk lookup table
        Dictionary<string, long> chunkLookup = new() { { riffID, 0 } };

        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            // Store chunk id and byte offset in lookup
            long chunkOffset = reader.BaseStream.Position;
            string chunkID = reader.ReadChars(4).AsSpan().ToString();
            chunkLookup.Add(chunkID, chunkOffset);

            // Skip to next chunk
            int chunkSize = reader.ReadInt32();
            reader.BaseStream.Position += chunkSize;
        }

        return chunkLookup;
    }

}

