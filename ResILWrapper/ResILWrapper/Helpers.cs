using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResIL
{
    public static class StreamBitConverter
    {
        public static UInt32 ToUInt32(Stream stream, int offset)
        {
            // KFreon: Seek to specified offset
            byte[] fourBytes = new byte[4];
            stream.Seek(offset, SeekOrigin.Begin);

            // KFreon: Read 4 bytes from stream at offset and convert to UInt32
            stream.Read(fourBytes, 0, 4);
            UInt32 retval = BitConverter.ToUInt32(fourBytes, 0);

            // KFreon: Clear array and reset stream position
            fourBytes = null;
            return retval;
        }
    }



    public static class Helpers
    {
        public static bool is64Bit()
        {
            return Environment.Is64BitOperatingSystem;
        }


        

        /// <summary>
        /// KFreon: Borrowed this from the DevIL C# Wrapper found here: https://code.google.com/p/devil-net/
        /// 
        /// Reads a stream until the end is reached into a byte array. Based on
        /// <a href="http://www.yoda.arachsys.com/csharp/readbinary.html">Jon Skeet's implementation</a>.
        /// It is up to the caller to dispose of the stream.
        /// </summary>
        /// <param name="stream">Stream to read all bytes from</param>
        /// <param name="initialLength">Initial buffer length, default is 32K</param>
        /// <returns>The byte array containing all the bytes from the stream</returns>
        public static byte[] ReadStreamFully(this Stream stream, int initialLength = 32768)
        {
            stream.Seek(0, SeekOrigin.Begin);
            if (initialLength < 1)
            {
                initialLength = 32768; //Init to 32K if not a valid initial length
            }

            byte[] buffer = new byte[initialLength];
            int position = 0;
            int chunk;

            while ((chunk = stream.Read(buffer, position, buffer.Length - position)) > 0)
            {
                position += chunk;

                //If we reached the end of the buffer check to see if there's more info
                if (position == buffer.Length)
                {
                    int nextByte = stream.ReadByte();

                    //If -1 we reached the end of the stream
                    if (nextByte == -1)
                    {
                        return buffer;
                    }

                    //Not at the end, need to resize the buffer
                    byte[] newBuffer = new byte[buffer.Length * 2];
                    Array.Copy(buffer, newBuffer, buffer.Length);
                    newBuffer[position] = (byte)nextByte;
                    buffer = newBuffer;
                    position++;
                }
            }

            //Trim the buffer before returning
            byte[] toReturn = new byte[position];
            Array.Copy(buffer, toReturn, position);
            return toReturn;
        }
    }
}
