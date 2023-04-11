namespace TecoBridge.GoodWe;

public class ModbusCreator
{
    public static ushort CalculateCrc(byte[] buf,
        int len)
    {
        ushort crc = 0xFFFF;

        for (var pos = 0; pos < len; pos++)
        {
            crc ^= buf[pos]; // XOR byte into least sig. byte of crc

            for (var i = 8; i != 0; i--)
                // Loop over each bit
                if ((crc & 0x0001) != 0)
                {
                    // If the LSB is set
                    crc >>= 1; // Shift right and XOR 0xA001
                    crc ^= 0xA001;
                }
                else // Else LSB is not set
                {
                    crc >>= 1; // Just shift right
                }
        }

        // Note, this number has low and high bytes swapped, so use it accordingly (or swap bytes)
        return crc;
    }
}