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
    public static class ILU
    {
        const String ILUDLL = "ILU.dll";
        static bool isInitialised = false;



        #region ILU Methods
        /// <summary>
        /// Initialise ResIL ILU subsystem.
        /// </summary>
        public static void Initialise()
        {
            if (!isInitialised)
            {
                iluInit();
                isInitialised = true;
            }
        }


        /// <summary>
        /// Resizes currently set image in memory. This appears to be permenant.
        /// </summary>
        /// <param name="width">New width.</param>
        /// <param name="height">New height.</param>
        /// <param name="depth">New Depth.</param>
        /// <returns>True if resize succeeded, controlled by native code.</returns>
        public static bool ResizeImage(int width, int height, int depth)
        {
            // KFreon: Set resize filter before saving
            iluImageParameter(ILUDefines.ILU_FILTER, ILUDefines.ILU_SCALE_MITCHELL);
            return iluScale((uint) width, (uint) height, (uint) depth);
        }


        public static bool FlipImage()
        {
            return iluFlipImage();
        }
        #endregion


        #region ILU Native Methods

        [DllImport(ILUDLL, EntryPoint = "iluInit", CallingConvention = CallingConvention.StdCall)]
        private static extern void iluInit();

        [DllImport(ILUDLL, EntryPoint = "iluBlurAvg", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool iluBlurAverage(uint iterations);

        [DllImport(ILUDLL, EntryPoint = "iluBlurGaussian", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool iluBlurGaussian(uint iterations);

        [DllImport(ILUDLL, EntryPoint = "iluCompareImage", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool iluCompareImages(uint otherImage);

        [DllImport(ILUDLL, EntryPoint = "iluCrop", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool iluCrop(uint offsetX, uint offsetY, uint offsetZ, uint width, uint height, uint depth);

        [DllImport(ILUDLL, EntryPoint = "iluEnlargeCanvas", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool iluEnlargeCanvas(uint width, uint height, uint depth);

        [DllImport(ILUDLL, EntryPoint = "iluEnlargeImage", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool iluEnlargeImage(uint xDim, uint yDim, uint zDim);

        [DllImport(ILUDLL, EntryPoint = "iluErrorString", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr iluGetErrorString(uint error);

        [DllImport(ILUDLL, EntryPoint = "iluColoursUsed", CallingConvention = CallingConvention.StdCall)]
        private static extern uint iluColorsUsed();

        [DllImport(ILUDLL, EntryPoint = "iluScale", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool iluScale(uint width, uint height, uint depth);

        [DllImport(ILUDLL, EntryPoint = "iluPixelize", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool iluPixelize(uint pixelSize);

        [DllImport(ILUDLL, EntryPoint = "iluSharpen", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool iluSharpen(float factor, uint iterations);

        [DllImport(ILUDLL, EntryPoint = "iluGetString", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr iluGetString(uint name);

        [DllImport(ILUDLL, EntryPoint = "iluImageParameter", CallingConvention = CallingConvention.StdCall)]
        private static extern void iluImageParameter(uint pName, uint param);

        [DllImport(ILUDLL, EntryPoint = "iluGetInteger", CallingConvention = CallingConvention.StdCall)]
        private static extern int iluGetInteger(uint mode);

        [DllImport(ILUDLL, EntryPoint = "iluRegionfv", CallingConvention = CallingConvention.StdCall)]
        private static extern void iluRegionf(PointF[] points, uint num);

        [DllImport(ILUDLL, EntryPoint = "iluRegioniv", CallingConvention = CallingConvention.StdCall)]
        private static extern void iluRegioni(PointI[] points, uint num);

        [DllImport(ILUDLL, EntryPoint = "iluBuildMipmaps", CallingConvention = CallingConvention.StdCall)]
        //[return: MarshalAs(UnmanagedType.U1)]
        public static extern bool iluBuildMipmaps();

        [DllImport(ILUDLL, EntryPoint = "iluFlipImage", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool iluFlipImage();

        #endregion

        
    }
}
