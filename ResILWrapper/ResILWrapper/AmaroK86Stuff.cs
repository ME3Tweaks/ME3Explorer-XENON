using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResIL.AmaroK86
{
    public enum FourCC : uint
    {
        DXT1 = 0x31545844,
        DXT3 = 0x33545844,
        DXT5 = 0x35545844,
        ATI2 = 0x32495441
    }


    public class ImageSize : IComparable
    {
        public readonly uint width;
        public readonly uint height;

        public ImageSize(uint width, uint height)
        {
            if (!checkIsPower2(width))
                new FormatException("Invalid width value, must be power of 2");
            if (!checkIsPower2(width))
                new FormatException("Invalid height value, must be power of 2");
            if (width == 0)
                width = 1;
            if (height == 0)
                height = 1;
            this.width = width;
            this.height = height;
        }

        private bool checkIsPower2(uint val)
        {
            uint power = 1;
            while (power < val)
            {
                power *= 2;
            }
            return val == power;
        }

        public int CompareTo(object obj)
        {
            if (obj is ImageSize)
            {
                ImageSize temp = (ImageSize)obj;
                if ((temp.width * temp.height) == (this.width * this.height))
                    return 0;
                if ((temp.width * temp.height) > (this.width * this.height))
                    return -1;
                else
                    return 1;
            }
            throw new ArgumentException();
        }

        public override string ToString()
        {
            return this.width + "x" + this.height;
        }

        public override bool Equals(System.Object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

            // If parameter cannot be cast to Point return false.
            ImageSize p = obj as ImageSize;
            if ((System.Object)p == null)
            {
                return false;
            }

            // Return true if the fields match:
            return (this.width == p.width) && (this.height == p.height);
        }

        public bool Equals(ImageSize p)
        {
            // If parameter is null return false:
            if ((object)p == null)
            {
                return false;
            }

            // Return true if the fields match:
            return (this.width == p.width) && (this.height == p.height);
        }

        public override int GetHashCode()
        {
            return (int)(width ^ height);
        }

        public static bool operator ==(ImageSize a, ImageSize b)
        {
            // If both are null, or both are same instance, return true.
            if (System.Object.ReferenceEquals(a, b))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            // Return true if the fields match:
            return a.width == b.width && a.height == b.height;
        }

        public static bool operator !=(ImageSize a, ImageSize b)
        {
            return !(a == b);
        }

        public static ImageSize operator /(ImageSize a, int b)
        {
            return new ImageSize((uint)(a.width / b), (uint)(a.height / b));
        }

        public static ImageSize operator *(ImageSize a, int b)
        {
            return new ImageSize((uint)(a.width * b), (uint)(a.height * b));
        }

        public static ImageSize stringToSize(string input)
        {
            string[] parsed = input.Split('x');
            if (parsed.Length != 2)
                throw new FormatException();
            uint width = Convert.ToUInt32(parsed[0]);
            uint height = Convert.ToUInt32(parsed[1]);
            return new ImageSize(width, height);
        }
    }

    public enum DDSFormat
    {
        DXT1, DXT3, DXT5, V8U8, ATI2, G8, ARGB
    }

    public class DDS_HEADER
    {
        public int dwSize;
        public int dwFlags;
        /*	DDPF_ALPHAPIXELS   0x00000001 
            DDPF_ALPHA   0x00000002 
            DDPF_FOURCC   0x00000004 
            DDPF_RGB   0x00000040 
            DDPF_YUV   0x00000200 
            DDPF_LUMINANCE   0x00020000 
         */
        public int dwHeight;
        public int dwWidth;
        public int dwPitchOrLinearSize;
        public int dwDepth;
        public int dwMipMapCount;
        public int[] dwReserved1 = new int[11];
        public DDS_PIXELFORMAT ddspf = new DDS_PIXELFORMAT();
        public int dwCaps;
        public int dwCaps2;
        public int dwCaps3;
        public int dwCaps4;
        public int dwReserved2;
    }

    public class DDS_PIXELFORMAT
    {
        public int dwSize;
        public int dwFlags;
        public int dwFourCC;
        public int dwRGBBitCount;
        public int dwRBitMask;
        public int dwGBitMask;
        public int dwBBitMask;
        public int dwABitMask;

        public DDS_PIXELFORMAT()
        {
        }
    }
}
