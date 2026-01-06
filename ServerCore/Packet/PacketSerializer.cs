using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace ServerCore.Packet
{
    public ref struct PacketReader
    {
        private ReadOnlySpan<byte> _span;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PacketReader(ReadOnlySpan<byte> buffer)
        {
            _span = buffer;
        }

        public bool ReadBool()
        {
            var value = MemoryMarshal.Read<byte>(_span);
            _span = _span.Slice(1);
            return value != 0;
        }

        public sbyte ReadInt8()
        {
            var value = MemoryMarshal.Read<sbyte>(_span);
            _span = _span.Slice(1);
            return value;
        }

        public byte ReadUInt8()
        {
            var value = MemoryMarshal.Read<byte>(_span);
            _span = _span.Slice(1);
            return value;
        }
        public short ReadInt16()
        {
            var value = BinaryPrimitives.ReadInt16LittleEndian(_span);
            _span = _span.Slice(2);
            return value;
        }

        public ushort ReadUInt16()
        {
            var value = BinaryPrimitives.ReadUInt16LittleEndian(_span);
            _span = _span.Slice(2);
            return value;
        }

        public int ReadInt32()
        {
            var value = BinaryPrimitives.ReadInt32LittleEndian(_span);
            _span = _span.Slice(4);
            return value;
        }

        public uint ReadUInt32()
        {
            var value = BinaryPrimitives.ReadUInt32LittleEndian(_span);
            _span = _span.Slice(4);
            return value;
        }

        public long ReadInt64()
        {
            var value = BinaryPrimitives.ReadInt64LittleEndian(_span);
            _span = _span.Slice(8);
            return value;
        }

        public ulong ReadUInt64()
        {
            var value = BinaryPrimitives.ReadUInt64LittleEndian(_span);
            _span = _span.Slice(8);
            return value;
        }

        public float ReadFloat32()
        {
            var value = BinaryPrimitives.ReadSingleLittleEndian(_span);
            _span = _span.Slice(4);
            return value;
        }

        public double ReadFloat64()
        {
            var value = BinaryPrimitives.ReadDoubleLittleEndian(_span);
            _span = _span.Slice(8);
            return value;
        }

        public Guid ReadGuid()
        {
            var value = MemoryMarshal.Read<Guid>(_span);
            _span = _span.Slice(16);
            return value;
        }

        public DateTime ReadDate()
        {
            var value = BinaryPrimitives.ReadInt64LittleEndian(_span);
            _span = _span.Slice(8);
            return DateTime.FromBinary(value);
        }

        public T[] ReadArray<T>() where T : unmanaged
        {
            var length = ReadInt32();
            if (length == 0) return Array.Empty<T>();

            var byteLength = length * Unsafe.SizeOf<T>();
            var dataSlice = _span.Slice(0, byteLength);
            var result = new T[length];
            MemoryMarshal.Cast<byte, T>(dataSlice).CopyTo(result);
            _span = _span.Slice(byteLength);
            return result;
        }

        public string ReadString()
        {
            var byteLength = ReadInt32();
            if (byteLength == 0) return string.Empty;
            var value = Encoding.UTF8.GetString(_span.Slice(0, byteLength));
            _span = _span.Slice(byteLength);
            return value;
        }
    }

    public ref struct PacketWriter
    {
        private Span<byte> _span;
        private readonly int _initialLength;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PacketWriter(Span<byte> buffer)
        {
            _span = buffer;
            _initialLength = buffer.Length;
        }

        public int Used => _initialLength - _span.Length;

        public void Skip(int size)
        {
            _span = _span.Slice(size);
        }

        public void WriteBool(bool value)
        {
            _span[0] = value ? (byte)1 : (byte)0;
            _span = _span.Slice(1);
        }

        public void WriteInt8(sbyte value)
        {
            _span[0] = (byte)value;
            _span = _span.Slice(1);
        }

        public void WriteUInt8(byte value)
        {
            _span[0] = value;
            _span = _span.Slice(1);
        }

        public void WriteInt16(short value)
        {
            BinaryPrimitives.WriteInt16LittleEndian(_span, value);
            _span = _span.Slice(2);
        }

        public void WriteUInt16(ushort value)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(_span, value);
            _span = _span.Slice(2);
        }

        public void WriteInt32(int value)
        {
            BinaryPrimitives.WriteInt32LittleEndian(_span, value);
            _span = _span.Slice(4);
        }

        public void WriteUInt32(uint value)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(_span, value);
            _span = _span.Slice(4);
        }

        public void WriteInt64(long value)
        {
            BinaryPrimitives.WriteInt64LittleEndian(_span, value);
            _span = _span.Slice(8);
        }

        public void WriteUInt64(ulong value)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(_span, value);
            _span = _span.Slice(8);
        }

        public void WriteFloat32(float value)
        {
            BinaryPrimitives.WriteSingleLittleEndian(_span, value);
            _span = _span.Slice(4);
        }

        public void WriteFloat64(double value)
        {
            BinaryPrimitives.WriteDoubleLittleEndian(_span, value);
            _span = _span.Slice(8);
        }

        public void WriteGuid(Guid value)
        {
            if (!value.TryWriteBytes(_span))
            {
                throw new IndexOutOfRangeException($"Guid Write Failed. Left: {_span.Length}");
            }
            _span = _span.Slice(16);
        }

        public void WriteDate(DateTime value)
        {
            BinaryPrimitives.WriteInt64LittleEndian(_span, value.ToBinary());
            _span = _span.Slice(8);
        }

        public void WriteArray<T>(ReadOnlySpan<T> value) where T : unmanaged
        {
            WriteInt32(value.Length);
            if (value.Length == 0) return;

            var byteLength = value.Length * Unsafe.SizeOf<T>();
            MemoryMarshal.AsBytes(value).CopyTo(_span);

            _span = _span.Slice(byteLength);
        }

        public void WriteString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                WriteInt32(0);
                return;
            }

            var byteCount = Encoding.UTF8.GetByteCount(value);
            WriteInt32(byteCount);

            Encoding.UTF8.GetBytes(value, _span);

            _span = _span.Slice(byteCount);
        }
    }


    // 구버전
    // IL은 적은데 릴리즈 지터로 만들어진거보면 콜 횟수도 많고 더 느림,
    // 잼민이 말론 Span구현은 릴리즈때 대부분 최적화된다고 함
    //public ref struct PacketReader
    //{
    //    private readonly byte[] _buffer;
    //    private readonly int _count;
    //    private readonly int _offset;
    //    private int _cursor;

    //    public PacketReader(ArraySegment<byte> buffer)
    //    {
    //        _buffer = buffer.Array!;
    //        _count = buffer.Count;
    //        _offset = buffer.Offset;
    //        _cursor = buffer.Offset;
    //    }

    //    private void EnsureCanRead(int bytesToRead)
    //    {
    //        if (_cursor + bytesToRead > _count + _offset)
    //        {
    //            throw new IndexOutOfRangeException($"버퍼 읽기 초과! 위치: {_cursor - _offset}, 필요: {bytesToRead}");
    //        }
    //    }

    //    public void Skip(int size)
    //    {
    //        EnsureCanRead(size);
    //        _cursor += size;
    //    }

    //    public bool ReadBool()
    //    {
    //        EnsureCanRead(1);
    //        return _buffer[_cursor++] != 0;
    //    }

    //    public sbyte ReadInt8()
    //    {
    //        EnsureCanRead(1);
    //        return (sbyte)_buffer[_cursor++];
    //    }

    //    public byte ReadUInt8()
    //    {
    //        EnsureCanRead(1);
    //        return _buffer[_cursor++];
    //    }

    //    public short ReadInt16()
    //    {
    //        EnsureCanRead(2);
    //        var value = Unsafe.ReadUnaligned<short>(ref _buffer[_cursor]);
    //        _cursor += 2;
    //        return value;
    //    }

    //    public ushort ReadUInt16()
    //    {
    //        EnsureCanRead(2);
    //        var value = Unsafe.ReadUnaligned<ushort>(ref _buffer[_cursor]);
    //        _cursor += 2;
    //        return value;
    //    }

    //    public int ReadInt32()
    //    {
    //        EnsureCanRead(4);
    //        var value = Unsafe.ReadUnaligned<int>(ref _buffer[_cursor]);
    //        _cursor += 4;
    //        return value;
    //    }

    //    public uint ReadUInt32()
    //    {
    //        EnsureCanRead(4);
    //        var value = Unsafe.ReadUnaligned<uint>(ref _buffer[_cursor]);
    //        _cursor += 4;
    //        return value;
    //    }

    //    public long ReadInt64()
    //    {
    //        EnsureCanRead(8);
    //        var value = Unsafe.ReadUnaligned<long>(ref _buffer[_cursor]);
    //        _cursor += 8;
    //        return value;
    //    }

    //    public ulong ReadUInt64()
    //    {
    //        EnsureCanRead(8);
    //        var value = Unsafe.ReadUnaligned<ulong>(ref _buffer[_cursor]);
    //        _cursor += 8;
    //        return value;
    //    }

    //    public float ReadFloat32()
    //    {
    //        EnsureCanRead(4);
    //        var value = Unsafe.ReadUnaligned<float>(ref _buffer[_cursor]);
    //        _cursor += 4;
    //        return value;
    //    }

    //    public double ReadFloat64()
    //    {
    //        EnsureCanRead(8);
    //        var value = Unsafe.ReadUnaligned<double>(ref _buffer[_cursor]);
    //        _cursor += 8;
    //        return value;
    //    }

    //    public Guid ReadGuid()
    //    {
    //        EnsureCanRead(16);
    //        var value = Unsafe.ReadUnaligned<Guid>(ref _buffer[_cursor]);
    //        _cursor += 16;
    //        return value;
    //    }

    //    public DateTime ReadDate()
    //    {
    //        EnsureCanRead(8);
    //        var value = Unsafe.ReadUnaligned<long>(ref _buffer[_cursor]);
    //        _cursor += 8;
    //        return DateTime.FromBinary(value);
    //    }

    //    public T[] ReadArray<T>() where T : unmanaged
    //    {
    //        var length = ReadInt32();
    //        if (length == 0) return Array.Empty<T>();

    //        var byteLength = length * Unsafe.SizeOf<T>();
    //        EnsureCanRead(byteLength);

    //        var value = new T[length];
    //        ref var src = ref _buffer[_cursor];
    //        ref var dst = ref Unsafe.As<T, byte>(ref value[0]);

    //        Unsafe.CopyBlockUnaligned(ref dst, ref src, (uint)byteLength);
    //        _cursor += byteLength;
    //        return value;
    //    }

    //    public string ReadString()
    //    {
    //        var length = ReadInt32();
    //        if (length == 0) return string.Empty;

    //        EnsureCanRead(length);
    //        var value = System.Text.Encoding.UTF8.GetString(_buffer, _cursor, length);
    //        _cursor += length;
    //        return value;
    //    }
    //}
    //public ref struct PacketWriter
    //{
    //    private readonly byte[] _buffer;
    //    private readonly int _count;
    //    private readonly int _offset;
    //    private int _cursor;

    //    public PacketWriter(ArraySegment<byte> buffer)
    //    {
    //        _buffer = buffer.Array!;
    //        _count = buffer.Count;
    //        _offset = buffer.Offset;
    //        _cursor = buffer.Offset;
    //    }

    //    public int Used => _cursor - _offset;

    //    public void Skip(int size)
    //    {
    //        EnsureCanWrite(size);
    //        _cursor += size;
    //    }

    //    private void EnsureCanWrite(int bytesToWrite)
    //    {
    //        if (_cursor + bytesToWrite > _count + _offset)
    //        {
    //            throw new IndexOutOfRangeException($"버퍼 오버플로우! 위치: {_cursor - _offset}, 크기: {bytesToWrite}");
    //        }
    //    }

    //    public void WriteBool(bool value)
    //    {
    //        EnsureCanWrite(1);
    //        _buffer[_cursor++] = value ? (byte)1 : (byte)0;
    //    }

    //    public void WriteInt8(sbyte value)
    //    {
    //        EnsureCanWrite(1);
    //        _buffer[_cursor++] = (byte)value;
    //    }

    //    public void WriteUInt8(byte value)
    //    {
    //        EnsureCanWrite(1);
    //        _buffer[_cursor++] = value;
    //    }

    //    public void WriteInt16(short value)
    //    {
    //        EnsureCanWrite(2);
    //        Unsafe.WriteUnaligned(ref _buffer[_cursor], value);
    //        _cursor += 2;
    //    }

    //    public void WriteUInt16(ushort value)
    //    {
    //        EnsureCanWrite(2);
    //        Unsafe.WriteUnaligned(ref _buffer[_cursor], value);
    //        _cursor += 2;
    //    }

    //    public void WriteInt32(int value)
    //    {
    //        EnsureCanWrite(4);
    //        Unsafe.WriteUnaligned(ref _buffer[_cursor], value);
    //        _cursor += 4;
    //    }

    //    public void WriteUInt32(uint value)
    //    {
    //        EnsureCanWrite(4);
    //        Unsafe.WriteUnaligned(ref _buffer[_cursor], value);
    //        _cursor += 4;
    //    }

    //    public void WriteInt64(long value)
    //    {
    //        EnsureCanWrite(8);
    //        Unsafe.WriteUnaligned(ref _buffer[_cursor], value);
    //        _cursor += 8;
    //    }

    //    public void WriteUInt64(ulong value)
    //    {
    //        EnsureCanWrite(8);
    //        Unsafe.WriteUnaligned(ref _buffer[_cursor], value);
    //        _cursor += 8;
    //    }

    //    public void WriteFloat32(float value)
    //    {
    //        EnsureCanWrite(4);
    //        Unsafe.WriteUnaligned(ref _buffer[_cursor], value);
    //        _cursor += 4;
    //    }

    //    public void WriteFloat64(double value)
    //    {
    //        EnsureCanWrite(8);
    //        Unsafe.WriteUnaligned(ref _buffer[_cursor], value);
    //        _cursor += 8;
    //    }

    //    public void WriteGuid(Guid value)
    //    {
    //        EnsureCanWrite(16);
    //        Unsafe.WriteUnaligned(ref _buffer[_cursor], value);
    //        _cursor += 16;
    //    }

    //    public void WriteDate(DateTime value)
    //    {
    //        EnsureCanWrite(8);
    //        Unsafe.WriteUnaligned(ref _buffer[_cursor], value.ToBinary());
    //        _cursor += 8;
    //    }

    //    public void WriteArray<T>(T[] value) where T : unmanaged
    //    {
    //        if (value == null || value.Length == 0)
    //        {
    //            WriteInt32(0);
    //            return;
    //        }

    //        WriteInt32(value.Length);

    //        var byteLength = value.Length * Unsafe.SizeOf<T>();
    //        EnsureCanWrite(byteLength);

    //        ref var src = ref Unsafe.As<T, byte>(ref value[0]);
    //        ref var dst = ref _buffer[_cursor];
    //        Unsafe.CopyBlockUnaligned(ref dst, ref src, (uint)byteLength);

    //        _cursor += byteLength;
    //    }

    //    public void WriteString(string value)
    //    {
    //        if (string.IsNullOrEmpty(value))
    //        {
    //            WriteInt32(0);
    //            return;
    //        }

    //        var maxLength = System.Text.Encoding.UTF8.GetMaxByteCount(value.Length);

    //        if (maxLength <= 4096)
    //        {
    //            Span<byte> tmp = stackalloc byte[maxLength];
    //            var actualLength = System.Text.Encoding.UTF8.GetBytes(value, tmp);
    //            WriteInt32(actualLength);
    //            EnsureCanWrite(actualLength);
    //            tmp = tmp[..actualLength];
    //            tmp.CopyTo(_buffer.AsSpan(_cursor));
    //            _cursor += actualLength;
    //        }
    //        else
    //        {
    //            var bytes = System.Text.Encoding.UTF8.GetBytes(value);
    //            WriteInt32(bytes.Length);

    //            EnsureCanWrite(bytes.Length);
    //            Buffer.BlockCopy(bytes, 0, _buffer, _cursor, bytes.Length);
    //            _cursor += bytes.Length;
    //        }
    //    }
    //}
}