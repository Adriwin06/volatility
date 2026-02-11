using System.Buffers.Binary;

namespace Volatility.Utilities;

public static class EndianUtilities
{
    public static ushort SwapEndian(ushort value)
    {
        return (ushort)(((value & 0x00FF) << 8) | ((value & 0xFF00) >> 8));
    }

    public static short SwapEndian(short value)
    {
        return (short)SwapEndian((ushort)value);
    }

    public static uint SwapEndian(uint value)
    {
        return ((value & 0x000000FF) << 24) |
               ((value & 0x0000FF00) << 8) |
               ((value & 0x00FF0000) >> 8) |
               ((value & 0xFF000000) >> 24);
    }

    public static int SwapEndian(int value)
    {
        return (int)SwapEndian((uint)value);
    }

    public static ulong SwapEndian(ulong value)
    {
        return ((value & 0x00000000000000FFUL) << 56) |
               ((value & 0x000000000000FF00UL) << 40) |
               ((value & 0x0000000000FF0000UL) << 24) |
               ((value & 0x00000000FF000000UL) << 8) |
               ((value & 0x000000FF00000000UL) >> 8) |
               ((value & 0x0000FF0000000000UL) >> 24) |
               ((value & 0x00FF000000000000UL) >> 40) |
               ((value & 0xFF00000000000000UL) >> 56);
    }

    public static long SwapEndian(long value)
    {
        return (long)SwapEndian((ulong)value);
    }

    public static float SwapEndian(float value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        Array.Reverse(bytes);
        return BitConverter.ToSingle(bytes, 0);
    }

    public static double SwapEndian(double value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        Array.Reverse(bytes);
        return BitConverter.ToDouble(bytes, 0);
    }

    public static ushort ReadUInt16(ReadOnlySpan<byte> data, int offset, Endian endianness)
    {
        if (endianness == Endian.Agnostic)
            throw new InvalidOperationException("An agnostic endianness was passed to EndianUtilities.ReadUInt16.");

        return endianness == Endian.BE
            ? BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2))
            : BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));
    }

    public static void WriteUInt16(Span<byte> data, int offset, ushort value, Endian endianness)
    {
        if (endianness == Endian.Agnostic)
            throw new InvalidOperationException("An agnostic endianness was passed to EndianUtilities.WriteUInt16.");

        if (endianness == Endian.BE)
            BinaryPrimitives.WriteUInt16BigEndian(data.Slice(offset, 2), value);
        else
            BinaryPrimitives.WriteUInt16LittleEndian(data.Slice(offset, 2), value);
    }
}
