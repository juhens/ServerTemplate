// https://github.com/force-net/Crc32.NET/tree/develop/Crc32.NET

namespace ServerCore.Util
{
    public static class Crc32C
    {
        // CRC-32C (Castagnoli) Polynomial
        private const uint Poly = 0x82F63B78;

        private static readonly uint[] Lookup = new uint[16 * 256];

        static Crc32C()
        {
            for (var i = 0u; i < 256u; i++)
            {
                var crc = i;
                for (var j = 0; j < 16; j++)
                {
                    for (var k = 0; k < 8; k++)
                    {
                        crc = (crc & 1) == 1 ? (Poly ^ (crc >> 1)) : (crc >> 1);
                    }
                    Lookup[j * 256 + i] = crc;
                }
            }
        }

        public static uint Compute(byte[] input)
        {
            return Compute(input, 0, input.Length);
        }

        public static uint Compute(byte[] input, int offset, int length)
        {
            return Append(0, input, offset, length);
        }

        public static uint Append(uint initial, byte[] input, int offset, int length)
        {
            var crc = initial ^ 0xFFFFFFFFu;
            var table = Lookup;

            while (length >= 16)
            {
                var a = table[0x300 + input[offset + 12]]
                      ^ table[0x200 + input[offset + 13]]
                      ^ table[0x100 + input[offset + 14]]
                      ^ table[0x000 + input[offset + 15]];

                var b = table[0x700 + input[offset + 8]]
                      ^ table[0x600 + input[offset + 9]]
                      ^ table[0x500 + input[offset + 10]]
                      ^ table[0x400 + input[offset + 11]];

                var c = table[0xB00 + input[offset + 4]]
                      ^ table[0xA00 + input[offset + 5]]
                      ^ table[0x900 + input[offset + 6]]
                      ^ table[0x800 + input[offset + 7]];

                var d = table[0xF00 + ((crc ^ input[offset]) & 0xFF)]
                      ^ table[0xE00 + (((crc >> 8) ^ input[offset + 1]) & 0xFF)]
                      ^ table[0xD00 + (((crc >> 16) ^ input[offset + 2]) & 0xFF)]
                      ^ table[0xC00 + (((crc >> 24) ^ input[offset + 3]) & 0xFF)];

                crc = a ^ b ^ c ^ d;

                offset += 16;
                length -= 16;
            }

            while (length-- > 0)
            {
                crc = table[(crc ^ input[offset++]) & 0xFF] ^ (crc >> 8);
            }

            return crc ^ 0xFFFFFFFFu;
        }
    }
}