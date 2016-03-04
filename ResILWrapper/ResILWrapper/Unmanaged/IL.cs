/*
* Copyright (c) 2012 Nicholas Woodfield
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in
* all copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
* THE SOFTWARE.
*/



using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ResIL.Unmanaged
{
    public static class IL
    {
        const string ILDLL = "ResIL.dll";
        public static bool isInitialised { get; private set; }


        #region IL Methods
        /// <summary>
        /// Initialises ResIL IL subsystem
        /// </summary>
        public static void Initialise()
        {
            if (!isInitialised)
            {
                ilInit();
                isInitialised = true;
            }
        }


        /// <summary>
        /// Destroys ResIL IL subsystem. MUST be initialised again before use.
        /// </summary>
        public static void ShutDown()
        {
            if (isInitialised)
            {
                ilShutDown();
                isInitialised = false;
            }
        }


        /// <summary>
        /// Sets an image as the working image in ResIL. Only 1 image can be modified at any one time. Use this to change images using the ID's.
        /// </summary>
        /// <param name="imageID">ID of image to bind to.</param>
        /// <returns>True if no errors occured.</returns>
        public static bool BindImage(ImageID imageID)
        {
            bool success = false;

            // KFreon: Get ResIL to try to bind the specified ImageID if valid.
            if (imageID > 0)
            {
                ilBindImage(imageID);
                success = GetError() == Unmanaged.ErrorType.NoError;
            }
            return success;
        }


        /// <summary>
        /// Gets the ResIL error code as the ErrorType enum.
        /// </summary>
        /// <returns>ResIL error code.</returns>
        public static ErrorType GetError()
        {
            return (ErrorType)ilGetError();
        }


        /// <summary>
        /// Populates an ImageInfo struct from the working image.
        /// </summary>
        /// <returns></returns>
        public static ImageInfo GetImageInfo()
        {
            ImageInfo info = new ImageInfo();
            info.Format = (DataFormat)ilGetInteger(ILDefines.IL_IMAGE_FORMAT);
            info.DxtcFormat = (CompressedDataFormat)ilGetInteger(ILDefines.IL_DXTC_DATA_FORMAT);
            info.DxtcFormat = (CompressedDataFormat)ilGetInteger(ILDefines.IL_DXTC_FORMAT);
            info.DxtcFormat = (CompressedDataFormat)ilGetDDSSurfaceFormat();
            info.DataType = (DataType)ilGetInteger(ILDefines.IL_IMAGE_TYPE);
            info.PaletteType = (PaletteType)ilGetInteger(ILDefines.IL_PALETTE_TYPE);
            info.PaletteBaseType = (DataFormat)ilGetInteger(ILDefines.IL_PALETTE_BASE_TYPE);
            info.CubeFlags = (CubeMapFace)ilGetInteger(ILDefines.IL_IMAGE_CUBEFLAGS);
            info.Origin = (OriginLocation)ilGetInteger(ILDefines.IL_IMAGE_ORIGIN);
            info.Width = ilGetInteger(ILDefines.IL_IMAGE_WIDTH);
            info.Height = ilGetInteger(ILDefines.IL_IMAGE_HEIGHT);
            info.Depth = ilGetInteger(ILDefines.IL_IMAGE_DEPTH);
            info.BitsPerPixel = ilGetInteger(ILDefines.IL_IMAGE_BITS_PER_PIXEL);
            info.BytesPerPixel = ilGetInteger(ILDefines.IL_IMAGE_BYTES_PER_PIXEL);
            info.Channels = ilGetInteger(ILDefines.IL_IMAGE_CHANNELS);
            info.Duration = ilGetInteger(ILDefines.IL_IMAGE_DURATION);
            info.SizeOfData = ilGetInteger(ILDefines.IL_IMAGE_SIZE_OF_DATA);
            info.OffsetX = ilGetInteger(ILDefines.IL_IMAGE_OFFX);
            info.OffsetY = ilGetInteger(ILDefines.IL_IMAGE_OFFY);
            info.PlaneSize = ilGetInteger(ILDefines.IL_IMAGE_PLANESIZE);
            info.FaceCount = ilGetInteger(ILDefines.IL_NUM_FACES) + 1;
            info.ImageCount = ilGetInteger(ILDefines.IL_NUM_IMAGES) + 1;
            info.LayerCount = ilGetInteger(ILDefines.IL_NUM_LAYERS) + 1;
            info.MipMapCount = ilGetInteger(ILDefines.IL_NUM_MIPMAPS) + 1;
            info.PaletteBytesPerPixel = ilGetInteger(ILDefines.IL_PALETTE_BPP);
            info.PaletteColumnCount = ilGetInteger(ILDefines.IL_PALETTE_NUM_COLS);
            return info;
        }


        /// <summary>
        /// Creates a valid (unused) ImageID and returns it.
        /// </summary>
        /// <returns>Valid ImageID</returns>
        public static ImageID GenerateImage()
        {
            return new ImageID(ilGenImage());
        }


        /// <summary>
        /// Loads an image from a file as the specified type (dds, bmp, etc...)
        /// </summary>
        /// <param name="imageType">Type of image to load as.</param>
        /// <param name="filename">Full path to image file.</param>
        /// <returns>True if loaded successfully.</returns>
        public static bool LoadImage(ImageType imageType, string filename)
        {
            return ilLoad((uint) imageType, filename);
        }


        /// <summary>
        /// Loads image from filename using automatic internal settings for image type.
        /// </summary>
        /// <param name="filename">Path to image file.</param>
        /// <returns>True if loaded successfully.</returns>
        public static bool LoadImage(string filename)
        {
            return ilLoadImage(filename);
        }

        public static void KeepDXTC(bool keep)
        {
            ilSetInteger(ILDefines.IL_KEEP_DXTC_DATA, keep ? 1 : 0);
            /*if (keep)
                //return ilEnable(ILDefines.IL_KEEP_DXTC_DATA);
                return ilSetInteger(ILDefines.IL_KEEP_DXTC_DATA, 1);
            else
                return null;*/
        }


        /// <summary>
        /// Loads image from stream as the specified type (dds, bmp, etc...)
        /// </summary>
        /// <param name="imageType">Type to load as.</param>
        /// <param name="stream">Stream to load from.</param>
        /// <returns>True if successfully loaded, false if stream is unreadable or native load fails.</returns>
        public static bool LoadImageFromStream(ImageType imageType, Stream stream)
        {
            bool retval = false;

            // KFreon: Check that imageType is valid and stream is valid and readable.
            if (imageType != ImageType.Unknown && stream != null && stream.CanRead)
            {
                // KFreon: Get data and size
                byte[] data = stream.ReadStreamFully();
                uint size = (uint)data.LongLength;

                // KFreon: Load image (unsafe for pointer magic)
                unsafe
                {
                    fixed (byte* ptr = data)
                        retval = ilLoadL((uint)imageType, new IntPtr(ptr), size);
                }
            }
            return retval;
        }


        /// <summary>
        /// Loads image from stream using automatic internal options for image type.
        /// </summary>
        /// <param name="stream">Stream to load from.</param>
        /// <returns>True if successful, false if stream is unreadable or native load fails.</returns>
        public static bool LoadImageFromStream(Stream stream)
        {
            bool retval = false;

            // KFreon: Check stream is valid and readable
            if (stream != null && stream.CanRead)
            {
                // KFreon: Get data and size
                byte[] data = stream.ReadStreamFully();
                uint size = (uint)data.LongLength;

                // KFreon: Get image type to pass to load function
                ImageType imageType = DetermineImageType(data);

                // KFreon: Load image (Unsafe for pointer magic)
                unsafe
                {
                    fixed (byte* ptr = data)
                        retval = ilLoadL((uint)imageType, new IntPtr(ptr), size);
                }
            }
            return retval;
        }


        /// <summary>
        /// Determines ImageType from raw data.
        /// </summary>
        /// <param name="imageData">Image data.</param>
        /// <returns>Type of image.</returns>
        public static ImageType DetermineImageType(byte[] imageData)
        {
            ImageType imageType = ImageType.Unknown;

            // KFreon: Check that image data is valid
            if (imageData != null && imageData.Length != 0)
            {
                uint size = (uint)imageData.LongLength;

                // KFreon: Call native magic code to determine image type
                unsafe
                {
                    fixed (byte* ptr = imageData)
                        imageType = (ImageType)ilDetermineTypeL(new IntPtr(ptr), size);
                }
            }
            return imageType;
        }

        /// <summary>
        /// Determines the image type from the specified file extension.
        /// </summary>
        /// <param name="filename">File extension</param>
        /// <returns>ImageType</returns>
        public static ImageType DetermineImageTypeFromFilename(String filename)
        {
            if (String.IsNullOrEmpty(filename))
                return ImageType.Unknown;

            return (ImageType)ilTypeFromExt(filename);
        }

        /// <summary>
        /// Gets the currently set global compressed data format.
        /// </summary>
        /// <returns>Compressed data format</returns>
        public static CompressedDataFormat GetDxtcFormat()
        {
            return (CompressedDataFormat)ilGetInteger((uint)ILDefines.IL_DXTC_FORMAT);
        }


        /// <summary>
        /// Removes specified image from ResIL internal image manager
        /// </summary>
        /// <param name="imageID">ID of image to delete.</param>
        /// <returns>True if removed successfully, false if delete failed or if image is already deleted.</returns>
        public static bool DeleteImage(ImageID imageID)
        {
            bool success = false;

            // KFreon: Proceed only if a valid image.
            if (imageID > 0)
            {
                ilDeleteImage(imageID);
                success = GetError() == ErrorType.NoError;
            }
            return success;
        }


        /// <summary>
        /// Sets DXTc format for current working image.
        /// </summary>
        /// <param name="format">New format to use.</param>
        public static void SetDxtcFormat(CompressedDataFormat format)
        {
            ilSetInteger((uint)ILDefines.IL_DXTC_FORMAT, (int)format);
        }


        /// <summary>
        /// Saves current working image to specified path as the specified type.
        /// </summary>
        /// <param name="type">Type of image to save as. (bmp, dds, etc...)</param>
        /// <param name="savepath">Path to save image to.</param>
        /// <returns>True if save successful.</returns>
        public static bool SaveImage(ImageType type, string savepath)
        {
            ilEnable((uint)Unmanaged.ILEnable.SquishCompression);
            return ilSave((uint)type, savepath);
        }


        /// <summary>
        /// Saves image to specified path.
        /// </summary>
        /// <param name="savepath"></param>
        /// <returns></returns>
        public static bool SaveImage(string savepath)
        {
            ImageType imageType = DetermineImageTypeFromFilename(savepath);
            return SaveImage(imageType, savepath);
        }


        /// <summary>
        /// Saves current working image to stream as specified type.
        /// </summary>
        /// <param name="imageType">Type to save image as.</param>
        /// <param name="stream">Stream to save image to.</param>
        /// <returns>True if no errors occurred.</returns>
        public static bool SaveImageToStream(ImageType imageType, Stream stream)
        {
            // KFreon: Set good compression
            ilEnable((uint)Unmanaged.ILEnable.SquishCompression);
            bool success = false;

            // KFreon: Check that given type is valid and stream is valid and writable.
            if (imageType != ImageType.Unknown && stream != null && stream.CanWrite)
            {
                // KFreon: Get size required for current working image to be saved as specified type.
                uint size = ilDetermineSize((uint)imageType);                

                // KFreon: Proceed if valid size.
                if (size != 0)
                {
                    byte[] buffer = new byte[size];

                    // KFreon: Call native save function to save image to a byte[]
                    unsafe
                    {
                        fixed (byte* ptr = buffer)
                            success = ilSaveL((uint)imageType, new IntPtr(ptr), size) != 0;
                    }

                    // KFreon: If native save successfull, write to stream AT BEGINNING!!
                    if (success)
                        stream.Write(buffer, 0, buffer.Length);
                }
            }
            return success;
        }


        /// <summary>
        /// Gets DXTc data from current working image as a byte[], in the specified format.
        /// </summary>
        /// <param name="dxtcFormat">DXTc format to extract as.</param>
        /// <returns>DXTc data of image.</returns>
        public static byte[] GetDxtcData(CompressedDataFormat dxtcFormat)
        {
            // KFreon: Get image data size.
            uint bufferSize = ilGetDXTCData(IntPtr.Zero, 0, (uint)dxtcFormat);
            if (bufferSize == 0)
                return null;

            // KFreon: Read DXTc Data into buffer and return.
            byte[] buffer = new byte[bufferSize];
            unsafe
            {
                fixed (byte* ptr = buffer)
                    ilGetDXTCData(new IntPtr(ptr), bufferSize, (uint)dxtcFormat);
            }
            return buffer;
        }


        public static List<sbyte> GetV8U8Data(int Width, int Height)
        {
            /*int bufferSize = ilGetInteger(ILDefines.IL_IMAGE_SIZE_OF_DATA);
            byte[] retval = new byte[bufferSize];
            unsafe
            {
                IntPtr start = ilGetData();
                Marshal.Copy(start, retval, 0, bufferSize);
            }
            return retval;*/
            
            /*int bufferSize = ilGetInteger(ILDefines.IL_IMAGE_SIZE_OF_DATA);
            byte[] Data = new byte[bufferSize];

            unsafe
            {
                IntPtr ptr = Marshal.AllocHGlobal(bufferSize);
                ilCopyPixels(0, 0, 0, (uint)Width, (uint)Height, (uint)Depth, (uint)Format, (uint)imgType, ptr);
                Marshal.Copy(ptr, Data, 0, bufferSize);
            }
            return Data;*/

            MemoryStream stream = new MemoryStream();
            if (!SaveImageToStream(ImageType.Bmp, stream))
                Console.WriteLine("Failed for soem reason.");

            List<sbyte> bytes = new List<sbyte>();
            for (int h = 0; h < Height; h++)
            {
                for (int w = 0; w < Width; w++)
                {
                    // KFreon: Get data. NOTE not correct channel names. No idea what the actual names are cos alpha and blue are apparently either red or green.
                    sbyte alpha = (sbyte)stream.ReadByte();
                    sbyte red = (sbyte)stream.ReadByte();
                    sbyte green = (sbyte)stream.ReadByte();
                    sbyte blue = (sbyte)stream.ReadByte();

                    // KFreon: Write data
                    bytes.Add(alpha);
                    bytes.Add(blue);
                }
            }

            return bytes;
        }

        
        /// <summary>
        /// Activates specified subimage of current working image (if subimages exist), and promotes to current working image. 
        /// </summary>
        /// <param name="imageNum">Index of subimage to activate.</param>
        /// <returns>True if successful.</returns>
        public static bool ActiveImage(int imageNum)
        {
            if (imageNum >= 0)
                return ilActiveImage((uint)imageNum);
            return false;
        }


        /// <summary>
        /// Activates specified face of current working image (if face exists), and promotes to current working image.
        /// </summary>
        /// <param name="faceIndex">Index of face to activate.</param>
        /// <returns>True if successful.</returns>
        public static bool ActiveFace(int faceIndex)
        {
            if (faceIndex >= 0)
                return ilActiveFace((uint)faceIndex);
            return false;
        }


        /// <summary>
        /// Activates specified layer of current working image (if layers exist), and promotes to current working image.
        /// </summary>
        /// <param name="layerNum">Index of layer to activate.</param>
        /// <returns>True if successful.</returns>
        public static bool ActiveLayer(int layerNum)
        {
            if (layerNum >= 0)
                return ilActiveLayer((uint)layerNum);
            return false;
        }


        /// <summary>
        /// Activates specified mipmap of current working image (if mips exist), and promotes to current working image.
        /// </summary>
        /// <param name="mipMapNum">Index of mipmap to activate.</param>
        /// <returns>True if successful.</returns>
        public static bool ActiveMipMap(int mipMapNum)
        {
            if (mipMapNum >= 0)
                return ilActiveMipmap((uint)mipMapNum);
            return false;
        }


        /// <summary>
        /// Changes how jpg's are saved. (jfif or jpg)
        /// </summary>
        /// <param name="format">Format to save as.</param>
        public static void SetJpgSaveFormat(JpgSaveFormat format)
        {
            ilSetInteger((uint)ILDefines.IL_JPG_SAVE_FORMAT, (int)format);
        }
        #endregion


        #region IL Native Methods
        [DllImport(ILDLL, EntryPoint = "ilLoadImage", CallingConvention = CallingConvention.StdCall)]
        private static extern bool ilLoadImage([InAttribute()] [MarshalAsAttribute(UnmanagedType.LPWStr)] String FileName);

        [DllImport(ILDLL, EntryPoint = "ilDetermineSize", CallingConvention = CallingConvention.StdCall)]
        private static extern uint ilDetermineSize(uint imageType);

        [DllImportAttribute(ILDLL, EntryPoint = "ilActiveFace", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool ilActiveFace(uint Number);

        [DllImportAttribute(ILDLL, EntryPoint = "ilActiveImage", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool ilActiveImage(uint Number);

        [DllImportAttribute(ILDLL, EntryPoint = "ilActiveLayer", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool ilActiveLayer(uint Number);

        [DllImportAttribute(ILDLL, EntryPoint = "ilActiveMipmap", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool ilActiveMipmap(uint Number);

        ///InProfile: char*
        ///OutProfile: char*
        [DllImportAttribute(ILDLL, EntryPoint = "ilApplyProfile", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool ilApplyProfile(IntPtr InProfile, IntPtr OutProfile);

        [DllImportAttribute(ILDLL, EntryPoint = "ilBindImage", CallingConvention = CallingConvention.StdCall)]
        private static extern void ilBindImage(uint Image);

        [DllImportAttribute(ILDLL, EntryPoint = "ilBlit", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool ilBlit(uint Source, int DestX, int DestY, int DestZ, uint SrcX, uint SrcY, uint SrcZ, uint Width, uint Height, uint Depth);

        [DllImportAttribute(ILDLL, EntryPoint = "ilCloneCurImage", CallingConvention = CallingConvention.StdCall)]
        private static extern uint ilCloneCurImage();

        [DllImportAttribute(ILDLL, EntryPoint = "ilCompressDXT", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr ilCompressDXT(IntPtr Data, uint Width, uint Height, uint Depth, uint DXTCFormat, ref uint DXTCSize);

        [DllImportAttribute(ILDLL, EntryPoint = "ilCompressFunc", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool ilCompressFunc(uint Mode);

        [DllImportAttribute(ILDLL, EntryPoint = "ilConvertImage", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool ilConvertImage(uint DestFormat, uint DestType);

        [DllImportAttribute(ILDLL, EntryPoint = "ilConvertPal", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool ilConvertPal(uint DestFormat);

        [DllImportAttribute(ILDLL, EntryPoint = "ilCopyImage", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool ilCopyImage(uint Src);

        /// Return Type: sizeOfData
        ///Data: void*
        [DllImportAttribute(ILDLL, EntryPoint = "ilCopyPixels", CallingConvention = CallingConvention.StdCall)]
        private static extern uint ilCopyPixels(uint XOff, uint YOff, uint ZOff, uint Width, uint Height, uint Depth, uint Format, uint Type, IntPtr Data);

        /// Looks like creates a subimage @ the num index and type is IL_SUB_* (Next, Mip, Layer), etc
        [DllImportAttribute(ILDLL, EntryPoint = "ilCreateSubImage", CallingConvention = CallingConvention.StdCall)]
        private static extern uint ilCreateSubImage(uint Type, uint Num);

        [DllImportAttribute(ILDLL, EntryPoint = "ilDeleteImage", CallingConvention = CallingConvention.StdCall)]
        private static extern void ilDeleteImage(uint Num);

        /// Num is a Size_t
        [DllImportAttribute(ILDLL, EntryPoint = "ilDeleteImages", CallingConvention = CallingConvention.StdCall)]
        private static extern void ilDeleteImages(UIntPtr Num, uint[] Images);

        /// Return Type: Image Type
        ///FileName: char*
        [DllImportAttribute(ILDLL, EntryPoint = "ilDetermineType", CallingConvention = CallingConvention.StdCall)]
        private static extern uint ilDetermineType([InAttribute()] [MarshalAs(UnmanagedType.LPWStr)] String FileName);

        /// Return Type: Image Type
        ///Lump: void*
        [DllImportAttribute(ILDLL, EntryPoint = "ilDetermineTypeL", CallingConvention = CallingConvention.StdCall)]
        private static extern uint ilDetermineTypeL(IntPtr Lump, uint Size);

        [DllImportAttribute(ILDLL, EntryPoint = "ilDisable", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool ilDisable(uint Mode);

        [DllImportAttribute(ILDLL, EntryPoint = "ilEnable", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool ilEnable(uint Mode);

        [DllImportAttribute(ILDLL, EntryPoint = "ilFormatFunc", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool ilFormatFunc(uint Mode);

        ///Num: ILsizei->size_t->unsigned int
        [DllImportAttribute(ILDLL, EntryPoint = "ilGenImages", CallingConvention = CallingConvention.StdCall)]
        private static extern void ilGenImages(UIntPtr Num, uint[] Images);

        [DllImportAttribute(ILDLL, EntryPoint = "ilGenImage", CallingConvention = CallingConvention.StdCall)]
        private static extern uint ilGenImage();

        /// Return Type: ILubyte*
        ///Type: ILenum->unsigned int (Data type)
        [DllImportAttribute(ILDLL, EntryPoint = "ilGetAlpha", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr ilGetAlpha(uint Type);

        /// Return Type: ILubyte*
        [DllImportAttribute(ILDLL, EntryPoint = "ilGetData", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr ilGetData();

        /// Returns Size of Data, set Zero for BufferSize to get size initially.
        [DllImportAttribute(ILDLL, EntryPoint = "ilGetDXTCData", CallingConvention = CallingConvention.StdCall)]
        private static extern uint ilGetDXTCData(IntPtr Buffer, uint BufferSize, uint DXTCFormat);

        /// Return Type: Error type
        [DllImportAttribute(ILDLL, EntryPoint = "ilGetError", CallingConvention = CallingConvention.StdCall)]
        private static extern uint ilGetError();

        [DllImportAttribute(ILDLL, EntryPoint = "ilGetInteger", CallingConvention = CallingConvention.StdCall)]
        internal static extern int ilGetInteger(uint Mode);

        /// Return Type: ILubyte*, need to find size via current image's pal size
        [DllImportAttribute(ILDLL, EntryPoint = "ilGetPalette", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr ilGetPalette();

        /// Return Type: char*
        ///StringName: ILenum->unsigned int - String type enum
        [DllImportAttribute(ILDLL, EntryPoint = "ilGetString", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr ilGetString(uint StringName);

        ///Target: ILenum->unsigned int --> Type of hint
        ///Mode: ILenum->unsigned int ---> Hint value
        [DllImportAttribute(ILDLL, EntryPoint = "ilHint", CallingConvention = CallingConvention.StdCall)]
        private static extern void ilHint(uint Target, uint Mode);

        [DllImportAttribute(ILDLL, EntryPoint = "ilInit", CallingConvention = CallingConvention.StdCall)]
        private static extern void ilInit();

        /// Format Type
        [DllImportAttribute(ILDLL, EntryPoint = "ilImageToDxtcData", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool ilImageToDxtcData(uint Format);

        //Enable enum
        [DllImportAttribute(ILDLL, EntryPoint = "ilIsDisabled", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool ilIsDisabled(uint Mode);

        //Enable enum
        [DllImportAttribute(ILDLL, EntryPoint = "ilIsEnabled", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool ilIsEnabled(uint Mode);

        ///Checks if valid image - input is image id
        [DllImportAttribute(ILDLL, EntryPoint = "ilIsImage", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool ilIsImage(uint Image);

        ///Type: ILenum->unsigned int -- ImageType
        ///FileName: char*
        [DllImportAttribute(ILDLL, EntryPoint = "ilIsValid", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool ilIsValid(uint Type, [InAttribute()] [MarshalAs(UnmanagedType.LPWStr)] String FileName);

        /// Return Type: ILboolean->unsigned char - Image Type
        ///Lump: void*
        [DllImportAttribute(ILDLL, EntryPoint = "ilIsValidL", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool ilIsValidL(uint Type, IntPtr Lump, uint Size);

        /// Type is Image Type
        [DllImportAttribute(ILDLL, EntryPoint = "ilLoad", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool ilLoad(uint Type, [InAttribute()] [MarshalAs(UnmanagedType.LPWStr)] String FileName);

        /// Type is Image Type
        ///Lump: void*
        [DllImportAttribute(ILDLL, EntryPoint = "ilLoadL", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool ilLoadL(uint Type, IntPtr Lump, uint Size);

        /// Mode is origin type
        [DllImportAttribute(ILDLL, EntryPoint = "ilOriginFunc", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool ilOriginFunc(uint Mode);

        /// SRC image, and coords are the offsets in a blit
        [DllImportAttribute(ILDLL, EntryPoint = "ilOverlayImage", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool ilOverlayImage(uint Source, int XCoord, int YCoord, int ZCoord);

        /// Attribute bit flags
        [DllImportAttribute(ILDLL, EntryPoint = "ilPushAttrib", CallingConvention = CallingConvention.StdCall)]
        private static extern void ilPushAttrib(uint Bits);

        /// Image Type
        [DllImportAttribute(ILDLL, EntryPoint = "ilSave", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool ilSave(uint Type, [InAttribute()] [MarshalAs(UnmanagedType.LPWStr)] String FileName);

        [DllImportAttribute(ILDLL, EntryPoint = "ilSaveImage", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool ilSaveImage([InAttribute()] [MarshalAs(UnmanagedType.LPWStr)] String FileName);

        ///ImageType, similar deal with GetDXTCData - returns size, pass in a NULL for lump to determine size
        [DllImportAttribute(ILDLL, EntryPoint = "ilSaveL", CallingConvention = CallingConvention.StdCall)]
        private static extern uint ilSaveL(uint Type, IntPtr Lump, uint Size);

        ///Data: void*
        [DllImportAttribute(ILDLL, EntryPoint = "ilSetData", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool ilSetData(IntPtr Data);

        [DllImportAttribute(ILDLL, EntryPoint = "ilSetDuration", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool ilSetDuration(uint Duration);

        /// IntegerMode, and param is value
        [DllImportAttribute(ILDLL, EntryPoint = "ilSetInteger", CallingConvention = CallingConvention.StdCall)]
        private static extern void ilSetInteger(uint Mode, int Param);

        ///Data: void*, dataFormat and DataType
        [DllImportAttribute(ILDLL, EntryPoint = "ilSetPixels", CallingConvention = CallingConvention.StdCall)]
        private static extern void ilSetPixels(int XOff, int YOff, int ZOff, uint Width, uint Height, uint Depth, uint Format, uint Type, IntPtr Data);

        /// Return Type: void
        ///Mode: ILenum->unsigned int
        ///String: char*
        [DllImportAttribute(ILDLL, EntryPoint = "ilSetString", CallingConvention = CallingConvention.StdCall)]
        private static extern void ilSetString(uint Mode, [InAttribute()] [MarshalAsAttribute(UnmanagedType.LPStr)] String String);

        [DllImportAttribute(ILDLL, EntryPoint = "ilShutDown", CallingConvention = CallingConvention.StdCall)]
        private static extern void ilShutDown();

        /// compressed DataFormat
        [DllImportAttribute(ILDLL, EntryPoint = "ilSurfaceToDxtcData", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool ilSurfaceToDxtcData(uint Format);

        /// dataFormat and DataType, destroys current data
        /// Bpp (NumChannels) bytes per pixel - e.g. 3 for RGB
        ///Data: void*
        [DllImportAttribute(ILDLL, EntryPoint = "ilTexImage", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool ilTexImage(uint Width, uint Height, uint Depth, byte Bpp, uint Format, uint Type, IntPtr Data);

        ///DxtcForamt is CompressedDataFormat, destroys current data
        [DllImportAttribute(ILDLL, EntryPoint = "ilTexImageDxtc", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool ilTexImageDxtc(int Width, int Height, int Depth, uint DxtFormat, IntPtr Data);

        ///Image type from extension of file
        [DllImportAttribute(ILDLL, EntryPoint = "ilTypeFromExt", CallingConvention = CallingConvention.StdCall)]
        private static extern uint ilTypeFromExt([InAttribute()] [MarshalAsAttribute(UnmanagedType.LPWStr)] String FileName);

        ///Sets the current DataType
        [DllImportAttribute(ILDLL, EntryPoint = "ilTypeFunc", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool ilTypeFunc(uint Mode);

        //Loads raw data from a file, bpp is only valid for 1, 3, 4
        [DllImportAttribute(ILDLL, EntryPoint = "ilLoadData", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool ilLoadData([InAttribute()] [MarshalAsAttribute(UnmanagedType.LPWStr)] String FileName, uint Width, uint Height, uint Depth, byte Bpp);

        //Loads raw data from a lump, bpp is only valid for 1, 3, 4
        [DllImportAttribute(ILDLL, EntryPoint = "ilLoadDataL", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool ilLoadDataL(IntPtr Lump, uint Size, uint Width, uint Height, uint Depth, byte Bpp);


        [DllImport(ILDLL, EntryPoint = "ilGetDDSSurfaceFormat", CallingConvention = CallingConvention.StdCall)]
        private static extern int ilGetDDSSurfaceFormat();
        #endregion
    }
}
