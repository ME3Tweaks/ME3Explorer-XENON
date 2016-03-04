using ResILWrapper;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageEngine
{
    /// <summary>
    /// Provides an object oriented way to interact with the ImageEngine.
    /// </summary>
    public class KFreonImage : IDisposable
    {
        #region Image Properties
        // KFreon: Get DevIL image ID
        ResIL.Unmanaged.ImageID ID = ImageEngine.GenerateImage();

        // KFreon: Normal image format, or DXT format for DDS's
        private string format = null;
        public string Format
        {
            get
            {
                return format;
            }
            private set
            {
                if (value == null)
                    format = ImageEngine.GetSurfaceFormat(info.DxtcFormat);
                else
                    format = value;
            }
        }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int BitsPerPixel { get; private set; }
        public string MemoryFormat { get; private set; }  // KFreon: Format as loaded by ResIL
        public int Channels { get; private set; }
        public int DataSize { get; private set; }
        public ResIL.Unmanaged.ImageInfo info { get; private set; }
        private bool isV8U8
        {
            get
            {
                return Format.ToUpperInvariant() == "V8U8";
            }
        }

        int mips = -1;
        public int Mips
        {
            get
            {
                if (V8U8Mips != null)
                    return V8U8Mips.Count();
                else
                    return mips;
            }
            private set
            {
                mips = value;
            }
        }

        public ImageEngine.MipMap[] V8U8Mips;
        #endregion


        #region Constructors
        /// <summary>
        /// Builds an image object in ResIL from a filename.
        /// </summary>
        /// <param name="filename">File to load as image.</param>
        public KFreonImage(string filename)
        {
            ConstructorHelper(filename, true);
        }


        /// <summary>
        /// Builds an image in ResIL from a stream.
        /// </summary>
        /// <param name="stream">Stream to load image from.</param>
        public KFreonImage(MemoryStream stream)
        {
            ConstructorHelper(stream, false);
        }


        /// <summary>
        /// Builds an image in ResIL from raw data.
        /// </summary>
        /// <param name="imgData">Array of data to load image from.</param>
        public KFreonImage(byte[] imgData)
        {
            ConstructorHelper(imgData, null);
        }


        /// <summary>
        /// Constructs an ResIL image from a data source.
        /// </summary>
        /// <param name="loader">Object to get data from. Can be path, stream, or byte[]. Specify with type.</param>
        /// <param name="type">True = path, false = stream, null = byte[]</param>
        private void ConstructorHelper(object loader, bool? type)
        {
            // KFreon: Check if already disposed
            if (ID == 0)
                throw new ObjectDisposedException("Current image has been disposed/deleted.");

            // KFreon: Load image and set its surface format and extension
            int width = -1;
            int height = -1;

            string tempformat = null;

            if (type == true)
                tempformat = ImageEngine.LoadImage(ID, (string)loader, out width, out height, out V8U8Mips);
            else if(type == false)
                tempformat = ImageEngine.LoadImage(ID, (Stream)loader, out width, out height, out V8U8Mips);
            else
            {
                using (MemoryTributary tempstream = new MemoryTributary((byte[])loader))
                {
                    try
                    {
                        tempformat = ImageEngine.LoadImage(ID, tempstream, out width, out height, out V8U8Mips);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }

            // KFreon: Set some properties
            info = GetInfo(width, height);
            if (tempformat == null)  // TODO: This is a hack
                Format = null; // KFreon: SETS PROPER VALUE IN SETTER. Messy I know...
            else
                Format = tempformat;
        }
        #endregion


        #region Methods
        /// <summary>
        /// Loads image in ResIL, gets image info, and sets some to this image's properties.
        /// </summary>
        /// <param name="width">A possible width. For most images = -1, but for V8U8 = true width.</param>
        /// <param name="height">A possible height. For most images = -1, but for V8U8 = true height.</param>
        private ResIL.Unmanaged.ImageInfo GetInfo(int width, int height)
        {
            // KFreon: Check if disposed
            if (ID == 0)
                throw new ObjectDisposedException("CurrentImage");

            // KFreon: Get and set some properties
            ResIL.Unmanaged.ImageInfo info = ImageEngine.GetImageInfo(ID);
            Width = width == -1 ? info.Width : width;
            Height = height == -1 ? info.Height : height;
            BitsPerPixel = info.BitsPerPixel;
            Channels = info.Channels;
            MemoryFormat = info.Format.ToString();
            DataSize = info.SizeOfData;
            Mips = info.MipMapCount;
            return info;
        }

        /// <summary>
        /// Loads image into ResIL and permenantly resizes.
        /// </summary>
        /// <param name="width">New width.</param>
        /// <param name="height">New height.</param>
        /// <returns>True if successful.</returns>
        public bool ResizeImage(int width, int height)
        {
            // KFreon: Resize and check result
            if (!ImageEngine.ResizeImage(width, height))
                return false;

            // KFreon: Update properties
            Width = width;
            Height = height;
            return true;
        }


        /// <summary>
        /// Converts image to a Bitmap object with a different size to loaded image. DOES NOT affect this image size in ResIL.
        /// </summary>
        /// <param name="width">New width for Bitmap.</param>
        /// <param name="height">New height for Bitmap.</param>
        /// <returns>Image as a Bitmap.</returns>
        public Bitmap ToBitmap(int width = -1, int height = -1)
        {
            Bitmap bmp = null;
            
            if ((width != -1 && height == -1) || (width == -1 && height != -1))
                Console.WriteLine("Resize information incomplete.");
            else
            {
                bool resize = width != -1 && height != -1;

                // KFreon: Get fullsize bitmap from ResIL and resize Bitmap
                bmp = ImageEngine.ToBitmap(ID);

                if (resize)
                    bmp = (Bitmap)bmp.GetThumbnailImage(width, height, null, IntPtr.Zero);
            }
            return bmp;
        }


        /// <summary>
        /// Disposes of resources
        /// </summary>
        public void Dispose()
        {
            // KFreon: Check if already disposed.
            if (ID == 0)
                throw new ObjectDisposedException("CurrentImage");

            // KFreon: Delete image.
            ImageEngine.Delete(ID);
        }


        /// <summary>
        /// Saves image with currently set extension and surface format (if dds)
        /// </summary>
        /// <param name="savepath">Path to save image to.</param>
        public bool Save(string savepath, bool mips)
        {
            // KFreon: Check if disposed
            if (ID == 0)
                throw new ObjectDisposedException("CurrentImage");

            return ImageEngine.ConvertandSave(savepath, ID, Format, info, mips);
        }

        public bool ConvertAndSave(string savepath, bool mips)
        {
            return Save(savepath, mips);
        }

        /// <summary>
        /// Set surface format of current image. Returns true if valid change.
        /// </summary>
        /// <param name="newFormat">Format to change to.</param>
        /// <returns>True if newformat is a valid surface format.</returns>
        public bool ChangeFormat(string newFormat)
        {
            if (ImageEngine.isValidFormat(newFormat))
                Format = newFormat;
            else
                return false;

            return true;
        }
        #endregion
    }
}
