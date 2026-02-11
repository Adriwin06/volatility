using System.Numerics;
using Volatility.Utilities;

namespace Volatility.Resources;

public static class PackedOobb
{
    static float Snorm31FromRep8(byte b)
    {
        uint u = b * 0x01010101u;
        int s = unchecked((int)u);
        return s * (1.0f / 2147483648.0f);
    }

    static float Snorm31FromRep16(ushort beU16)
    {
        uint u = ((uint)beU16 << 16) | beU16;
        int s = unchecked((int)u);
        return s * (1.0f / 2147483648.0f);
    }

    static float Unorm24FromRep8(byte b)
    {
        uint u = b * 0x010101u;
        return u * (1.0f / 16777216.0f);
    }

    public static Matrix44 ToMatrix(ReadOnlySpan<byte> packed16)
    {
        if (packed16.Length != 16) throw new ArgumentException("PackedOobb must be 16 bytes.");

        uint posExpBits = ((uint)packed16[0] << 24) | ((uint)packed16[1] << 16);
        float posExp = BitConverter.UInt32BitsToSingle(posExpBits);

        float pmx = Snorm31FromRep16(EndianUtilities.ReadUInt16(packed16, 2, Endian.BE));
        float pmy = Snorm31FromRep16(EndianUtilities.ReadUInt16(packed16, 4, Endian.BE));
        float pmz = Snorm31FromRep16(EndianUtilities.ReadUInt16(packed16, 6, Endian.BE));
        Vector3 pos = new Vector3(pmx, pmy, pmz) * posExp;

        float scaleExp = BitConverter.UInt32BitsToSingle((uint)packed16[8] << 23);
        float sx = Unorm24FromRep8(packed16[9]) * scaleExp;
        float sy = Unorm24FromRep8(packed16[10]) * scaleExp;
        float sz = Unorm24FromRep8(packed16[11]) * scaleExp;

        float qx = Snorm31FromRep8(packed16[12]);
        float qy = Snorm31FromRep8(packed16[13]);
        float qz = Snorm31FromRep8(packed16[14]);
        float qw = Snorm31FromRep8(packed16[15]);

        float lenSq = qx * qx + qy * qy + qz * qz + qw * qw;
        if (lenSq > 0f)
        {
            float invLen = 1.0f / MathF.Sqrt(lenSq);
            qx *= invLen; qy *= invLen; qz *= invLen; qw *= invLen;
        }

        const float sqrt2 = 1.41421356237f;
        qx *= sqrt2; qy *= sqrt2; qz *= sqrt2; qw *= sqrt2;

        float xx = qx * qx, yy = qy * qy, zz = qz * qz;
        float xy = qx * qy, xz = qx * qz, yz = qy * qz;
        float wx = qw * qx, wy = qw * qy, wz = qw * qz;

        Vector3 r0 = new Vector3(1f - (yy + zz), xy + wz, xz - wy) * sx;
        Vector3 r1 = new Vector3(xy - wz, 1f - (xx + zz), yz + wx) * sy;
        Vector3 r2 = new Vector3(xz + wy, yz - wx, 1f - (xx + yy)) * sz;

        return new Matrix44(
            r0.X, r0.Y, r0.Z, 0f,
            r1.X, r1.Y, r1.Z, 0f,
            r2.X, r2.Y, r2.Z, 0f,
            pos.X, pos.Y, pos.Z, 1f
        );
    }

    static float NextPosExpCoarse(float exp)
    {
        uint bits = BitConverter.SingleToUInt32Bits(exp);
        uint top16 = bits >> 16;
        uint nextTop16 = top16 + 1;
        return BitConverter.UInt32BitsToSingle(nextTop16 << 16);
    }

    static float QuantizePosExpCoarseUp(float minExp)
    {
        if (!(minExp > 0f)) minExp = 1f;
        uint bits = BitConverter.SingleToUInt32Bits(minExp);
        uint top16 = bits >> 16;
        float exp = BitConverter.UInt32BitsToSingle(top16 << 16);
        if (!(exp > 0f)) exp = 1f;
        while (exp < minExp) exp = NextPosExpCoarse(exp);
        return exp;
    }

    static float QuantizeUnitToRep16(float u, out ushort beU16)
    {
        if (u <= -1f) u = -0.99999994f;
        if (u >= 1f) u = 0.99999994f;

        const float k = 65537.0f / 2147483648.0f; // 0x00010001 / 2^31

        int s16 = (int)MathF.Round(u / k);
        if (s16 < short.MinValue) s16 = short.MinValue;
        if (s16 > short.MaxValue) s16 = short.MaxValue;

        ushort u16 = unchecked((ushort)(short)s16);
        beU16 = u16;
        return Snorm31FromRep16(u16);
    }

    static float QuantizeUnitToRep8(float u, out byte b)
    {
        if (u <= -1f) u = -0.99999994f;
        if (u >= 1f) u = 0.99999994f;

        byte best = 0;
        float bestErr = float.PositiveInfinity;

        for (int i = 0; i < 256; i++)
        {
            float v = Snorm31FromRep8((byte)i);
            float e = MathF.Abs(v - u);
            
            if (!(e < bestErr)) 
                continue;
            
            bestErr = e;
            best = (byte)i;
        }

        b = best;
        return Snorm31FromRep8(best);
    }

    static float QuantizeUnormToRep8(float u, out byte b)
    {
        if (u <= 0f) { b = 0; return 0f; }
        if (u >= 1f) { b = 255; return Unorm24FromRep8(255); }

        const float k = 65793.0f / 16777216.0f; // 0x010101 / 2^24

        int bi = (int)MathF.Round(u / k);
        if (bi < 0) bi = 0;
        if (bi > 255) bi = 255;

        b = (byte)bi;
        return Unorm24FromRep8(b);
    }

    static Quaternion QuaternionFromRotationRows(Vector3 r0, Vector3 r1, Vector3 r2)
    {
        float m00 = r0.X, m01 = r0.Y, m02 = r0.Z;
        float m10 = r1.X, m11 = r1.Y, m12 = r1.Z;
        float m20 = r2.X, m21 = r2.Y, m22 = r2.Z;

        float trace = m00 + m11 + m22;

        if (trace > 0f)
        {
            float s = MathF.Sqrt(trace + 1f) * 2f;
            float qw = 0.25f * s;
            float qx = (m21 - m12) / s;
            float qy = (m02 - m20) / s;
            float qz = (m10 - m01) / s;
            return new Quaternion(qx, qy, qz, qw);
        }
        else if (m00 > m11 && m00 > m22)
        {
            float s = MathF.Sqrt(1f + m00 - m11 - m22) * 2f;
            float qw = (m21 - m12) / s;
            float qx = 0.25f * s;
            float qy = (m01 + m10) / s;
            float qz = (m02 + m20) / s;
            return new Quaternion(qx, qy, qz, qw);
        }
        else if (m11 > m22)
        {
            float s = MathF.Sqrt(1f + m11 - m00 - m22) * 2f;
            float qw = (m02 - m20) / s;
            float qx = (m01 + m10) / s;
            float qy = 0.25f * s;
            float qz = (m12 + m21) / s;
            return new Quaternion(qx, qy, qz, qw);
        }
        else
        {
            float s = MathF.Sqrt(1f + m22 - m00 - m11) * 2f;
            float qw = (m10 - m01) / s;
            float qx = (m02 + m20) / s;
            float qy = (m12 + m21) / s;
            float qz = 0.25f * s;
            return new Quaternion(qx, qy, qz, qw);
        }
    }

    static byte ChooseScaleExpByte(float maxScale)
    {
        if (!(maxScale > 0f)) maxScale = 1f;
        int e = (int)MathF.Ceiling(MathF.Log2(maxScale));
        int expField = e + 127;
        if (expField < 1) expField = 1;
        if (expField > 254) expField = 254;
        return (byte)expField;
    }

    public static byte[] ToPackedOobb(Matrix44 m)
    {
        Vector3 row0 = new(m.M11, m.M12, m.M13);
        Vector3 row1 = new(m.M21, m.M22, m.M23);
        Vector3 row2 = new(m.M31, m.M32, m.M33);
        Vector3 pos = new(m.M41, m.M42, m.M43);

        float sx = row0.Length();
        float sy = row1.Length();
        float sz = row2.Length();

        Vector3 r0 = sx > 0f ? row0 / sx : Vector3.UnitX;
        Vector3 r1 = sy > 0f ? row1 / sy : Vector3.UnitY;
        Vector3 r2 = sz > 0f ? row2 / sz : Vector3.UnitZ;

        Quaternion q = QuaternionFromRotationRows(r0, r1, r2);
        float qLenSq = q.X * q.X + q.Y * q.Y + q.Z * q.Z + q.W * q.W;
        if (qLenSq > 0f)
        {
            float inv = 1.0f / MathF.Sqrt(qLenSq);
            q = new Quaternion(q.X * inv, q.Y * inv, q.Z * inv, q.W * inv);
        }

        float maxAbsPos = MathF.Max(MathF.Abs(pos.X), MathF.Max(MathF.Abs(pos.Y), MathF.Abs(pos.Z)));
        float posExp = QuantizePosExpCoarseUp(maxAbsPos);
        if (!(posExp > 0f)) posExp = 1f;

        float ux = pos.X / posExp;
        float uy = pos.Y / posExp;
        float uz = pos.Z / posExp;

        ushort px16, py16, pz16;
        QuantizeUnitToRep16(ux, out px16);
        QuantizeUnitToRep16(uy, out py16);
        QuantizeUnitToRep16(uz, out pz16);

        uint posExpBits = BitConverter.SingleToUInt32Bits(posExp);
        byte b0 = (byte)(posExpBits >> 24);
        byte b1 = (byte)(posExpBits >> 16);

        float maxScale = MathF.Max(sx, MathF.Max(sy, sz));
        byte b8 = ChooseScaleExpByte(maxScale);
        float scaleExp = BitConverter.UInt32BitsToSingle((uint)b8 << 23);

        float sux = sx / scaleExp;
        float suy = sy / scaleExp;
        float suz = sz / scaleExp;

        byte bx, by, bz;
        QuantizeUnormToRep8(sux, out bx);
        QuantizeUnormToRep8(suy, out by);
        QuantizeUnormToRep8(suz, out bz);

        byte qbx, qby, qbz, qbw;
        QuantizeUnitToRep8(q.X, out qbx);
        QuantizeUnitToRep8(q.Y, out qby);
        QuantizeUnitToRep8(q.Z, out qbz);
        QuantizeUnitToRep8(q.W, out qbw);

        byte[] packed = new byte[16];

        packed[0] = b0;
        packed[1] = b1;

        EndianUtilities.WriteUInt16(packed, 2, px16, Endian.BE);
        EndianUtilities.WriteUInt16(packed, 4, py16, Endian.BE);
        EndianUtilities.WriteUInt16(packed, 6, pz16, Endian.BE);

        packed[8] = b8;
        packed[9] = bx;
        packed[10] = by;
        packed[11] = bz;

        packed[12] = qbx;
        packed[13] = qby;
        packed[14] = qbz;
        packed[15] = qbw;

        return packed;
    }
}
