using System.Globalization;
using System.Runtime.InteropServices;

namespace Volatility.Utilities;

public static class HexUtilities
{
    public static void HexDump(ReadOnlySpan<byte> bytes, int bytesPerLine = 0x10, bool showOffset = true, bool showAscii = true)
    {
        if (bytesPerLine <= 0) bytesPerLine = 0x10;

        for (int i = 0; i < bytes.Length; i += bytesPerLine)
        {
            int lineLen = Math.Min(bytesPerLine, bytes.Length - i);

            if (showOffset)
                Console.Write($"{i:X8}: ");

            for (int j = 0; j < bytesPerLine; j++)
            {
                if (j < lineLen) Console.Write($"{bytes[i + j]:X2} ");
                else Console.Write("   ");
                if (j == 7) Console.Write(" ");
            }

            if (showAscii)
            {
                Console.Write(" |");
                for (int j = 0; j < lineLen; j++)
                {
                    byte b = bytes[i + j];
                    char c = (b >= 32 && b <= 126) ? (char)b : '.';
                    Console.Write(c);
                }
                Console.Write('|');
            }

            Console.WriteLine();
        }
    }

    public static void PrintMatrixNumeric(in Matrix44 m, string? label = null, int precision = 6)
    {
        if (!string.IsNullOrWhiteSpace(label))
            Console.WriteLine(label);

        string fmt = "F" + Math.Clamp(precision, 0, 9).ToString(CultureInfo.InvariantCulture);

        string f(float v) => v.ToString(fmt, CultureInfo.InvariantCulture).PadLeft(12);

        Console.WriteLine($"[{f(m.M11)} {f(m.M12)} {f(m.M13)} {f(m.M14)} ]");
        Console.WriteLine($"[{f(m.M21)} {f(m.M22)} {f(m.M23)} {f(m.M24)} ]");
        Console.WriteLine($"[{f(m.M31)} {f(m.M32)} {f(m.M33)} {f(m.M34)} ]");
        Console.WriteLine($"[{f(m.M41)} {f(m.M42)} {f(m.M43)} {f(m.M44)} ]");
    }

    public static void PrintMatrixBytes(in Matrix44 m, string? label = null, int bytesPerLine = 0x10, bool showOffset = true, bool showAscii = true)
    {
        if (!string.IsNullOrWhiteSpace(label))
            Console.WriteLine(label);

        ReadOnlySpan<Matrix44> one = MemoryMarshal.CreateReadOnlySpan(in m, 1);
        ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(one);

        HexDump(bytes, bytesPerLine, showOffset, showAscii);
    }

    public static void PrintMatrix(in Matrix44 m, string? label = null, int precision = 6, int bytesPerLine = 0x10)
    {
        if (!string.IsNullOrWhiteSpace(label))
            Console.WriteLine(label);

        PrintMatrixNumeric(in m, "Numeric:", precision);
        PrintMatrixBytes(in m, "Bytes (hex, 0x10 per line):", bytesPerLine, showOffset: true, showAscii: false);
    }
}