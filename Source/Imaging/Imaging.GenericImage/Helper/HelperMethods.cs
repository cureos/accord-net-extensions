#region Licence and Terms
// Accord.NET Extensions Framework
// https://github.com/dajuric/accord-net-extensions
//
// Copyright © Darko Jurić, 2014-2015 
// darko.juric2@gmail.com
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU Lesser General Public License as published by
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//   GNU Lesser General Public License for more details.
// 
//   You should have received a copy of the GNU Lesser General Public License
//   along with this program.  If not, see <https://www.gnu.org/licenses/lgpl.txt>.
//
#endregion

using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Accord.Extensions.Imaging
{
    using System.Linq;

    /// <summary>
    /// Contains image helper methods.
    /// </summary>
    public static class HelperMethods
    {
        /// <summary>
        /// Converts color to unmanaged data of type <typeparamref name="TDepth"/>.
        /// </summary>
        /// <typeparam name="TColor">Color type.</typeparam>
        /// <typeparam name="TDepth">Channel type.</typeparam>
        /// <param name="color">Color</param>
        /// <param name="data">Pointer to unmanaged data. Space must be allocated (number of color channels * sizeof(<typeparamref name="TDepth"/>)</param>
        public unsafe static void ColorToPointer<TColor, TDepth>(TColor color, IntPtr data) 
            where TColor : IColor
            where TDepth: struct
        {
            var colorInfo = ColorInfo.GetInfo<TColor, TDepth>();

            TDepth[] arr = ColorToArray<TColor, TDepth>(color);

            GCHandle handle = GCHandle.Alloc(arr, GCHandleType.Pinned);
            AForge.SystemTools.CopyUnmanagedMemory(data, handle.AddrOfPinnedObject(), colorInfo.Size);
            handle.Free();
        }

        /// <summary>
        /// Converts unmanaged data to color representation.
        /// </summary>
        /// <typeparam name="TColor">Color type.</typeparam>
        /// <typeparam name="TDepth">Channel type.</typeparam>
        /// <param name="data">Pointer to unmanaged data of type: <typeparamref name="TDepth"/>.</param>
        /// <returns>Color</returns>
        public unsafe static TColor PointerToColor<TColor, TDepth>(IntPtr data)
            where TColor : IColor
            where TDepth : struct
        {
            TColor color;

            var colorInfo = ColorInfo.GetInfo<TColor, TDepth>();
            using (PinnedArray<TDepth> arr = new PinnedArray<TDepth>(colorInfo.Size))
            {
                AForge.SystemTools.CopyUnmanagedMemory(arr.Data, data, colorInfo.Size);
                color = ArrayToColor<TColor, TDepth>(arr.Array);
            }

            return color;
        }

        /// <summary>
        /// Converts color to array of type <typeparamref name="TDepth"/>.
        /// </summary>
        /// <typeparam name="TColor">Color type.</typeparam>
        /// <typeparam name="TDepth">Channel type.</typeparam>
        /// <param name="color">Color</param>
        /// <returns>Array whose length is the same as color's number of channels.</returns>
        public static TDepth[] ColorToArray<TColor, TDepth>(this TColor color)
            where TColor: IColor
            where TDepth: struct
        {
            var fields = typeof(TColor).GetFields(BindingFlags.Public | ~BindingFlags.Static);

            TDepth[] arr = new TDepth[fields.Length];

            for (int i = 0; i < fields.Length; i++)
			{
                var rawVal = fields[i].GetValue(color);
                arr[i]  = (TDepth)Convert.ChangeType(rawVal, typeof(TDepth));
			}

            return arr;
        }

        /// <summary>
        /// Converts array to color representation. Array length must be the same as color's number of channels.
        /// </summary>
        /// <param name="arr">Input array</param>
        /// <returns>Color</returns>
        public static TColor ArrayToColor<TColor, TDepth>(TDepth[] arr)
            where TColor : IColor
            where TDepth : struct
        {
            var fields = typeof(TColor).GetFields(BindingFlags.Public | ~BindingFlags.Static);

            object color = default(TColor);

            for (int i = 0; i < fields.Length; i++)
            {
                var rawVal = Convert.ChangeType(arr[i], fields[i].FieldType);
                fields[i].SetValue(color, rawVal);
            }

            return (TColor)color;
        }

        internal static Func<IImage> GetGenericImageConstructor(Type objectType, ColorInfo colorInfo)
        {
            return MethodCache.Global.Invoke(getGenericImageConstructor, objectType, colorInfo);
        }

        private static Func<IImage> getGenericImageConstructor(Type objectType, ColorInfo colorInfo)
        {
            var genericClassType = objectType.MakeGenericType(colorInfo.ColorType, colorInfo.ChannelType);
            var ctor = genericClassType.GetTypeInfo().DeclaredConstructors.Single(c => !c.IsStatic && c.GetParameters().Length == 0);
            var ctorInvoker = Expression.Lambda<Func<IImage>>(Expression.New(ctor)).Compile();

            return ctorInvoker;
        }

        /// <summary>
        /// Copies unmanaged data.
        /// </summary>
        /// <param name="srcPtr">Source pointer.</param>
        /// <param name="destPtr">Destination pointer.</param>
        /// <param name="srcStride">Source stride.</param>
        /// <param name="destStride">Destination stride.</param>
        /// <param name="bytesPerRow">Amount of bytes per row to be copied (width for byte fields). (common: image width * color size)</param>
        /// <param name="height">Field's height.</param>
        public unsafe static void CopyImage(IntPtr srcPtr, IntPtr destPtr, int srcStride, int destStride, int bytesPerRow, int height)
        {
            if (srcStride == destStride && srcStride == bytesPerRow)
                AForge.SystemTools.CopyUnmanagedMemory(destPtr, srcPtr, srcStride * height);
            else
            {
                byte* srcImgPtr = (byte*)srcPtr;
                byte* destImgPtr = (byte*)destPtr;

                for (int i = 0; i < height; i++)
                {
                    AForge.SystemTools.CopyUnmanagedMemory(destImgPtr, srcImgPtr, bytesPerRow);

                    srcImgPtr += srcStride;
                    destImgPtr += destStride;
                }
            }
        }

    }
}
