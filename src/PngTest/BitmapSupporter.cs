using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Clarity.Image.PNG;

namespace PngTest
{
    /// <summary>
    /// Windows以外での使用を一応考慮し、System.Drawing.Bitmapに関する処理を分離する
    /// </summary>
    internal static class BitmapSupporter
    {

        /// <summary>
        /// Bitmap画像の作成
        /// </summary>
        /// <param name="pfile"></param>
        /// <returns></returns>
        public static Bitmap CreateBitmap(PngFile pfile)
        {
            //RGBA領域をBitmap画像に変換する
            Bitmap ans = BitmapSupporter.CreateBitmapFromRGBA(pfile.Width, pfile.Height, pfile.Data);

            return ans;
        }

        /// <summary>
        /// Apngフレームをbitmapに変換する
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        public static Bitmap CreateBitmap(APngFrame frame)
        {
            //RGBA領域をBitmap画像に変換する
            Bitmap ans = BitmapSupporter.CreateBitmapFromRGBA(frame.Width, frame.Height, frame.FrameData);

            return ans;
        }

        /// <summary>
        /// RGBAバッファからBitmap画像を生成する
        /// </summary>
        /// <param name="width">画像横幅</param>
        /// <param name="height">画像縦幅</param>
        /// <param name="colbuf">RGBAバッファ</param>
        /// <returns>作成bitmap</returns>
        public static Bitmap CreateBitmapFromRGBA(int width, int height, byte[] colbuf)
        {
            //RGBAのバッファをBGRAに変換
            byte[] bbuf = BitmapSupporter.ConvertBGRA_RGBA(width, height, colbuf);

            //bitmapを作成してBGRAバッファを流し込む
            Bitmap ansbit = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Rectangle rect = new Rectangle(0, 0, width, height);
            BitmapData bdata = ansbit.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, ansbit.PixelFormat);
            try
            {
                //一括コピー
                Marshal.Copy(bbuf, 0, bdata.Scan0, colbuf.Length);
            }
            finally
            {
                ansbit.UnlockBits(bdata);
            }

            return ansbit;
        }


        /// <summary>
        /// BitmapをRGBAバッファに変換する
        /// </summary>
        /// <param name="bit">作成元bitmap</param>
        /// <returns>作成したRGBAバッファ</returns>
        public static byte[] CreateRGBABuffer(Bitmap bit)
        {
            int w = bit.Width;
            int h = bit.Height;
            byte[] bgrabuf = new byte[w * h * 4];

                        
            Rectangle rect = new Rectangle(0, 0, w, h);
            BitmapData bdata = bit.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            try
            {
                //一括コピー
                Marshal.Copy(bdata.Scan0, bgrabuf, 0, bgrabuf.Length);
            }
            finally
            {
                bit.UnlockBits(bdata);
            }


            //BGRAをRGBAに変換する
            byte[] ans = ConvertBGRA_RGBA(w, h, bgrabuf);

            return ans;
        }


        //--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//
        /// <summary>
        /// RGBAのバッファをbitmapBGRA形式に変換する、逆もそのまま使えます
        /// </summary>
        /// <param name="width">画像横幅</param>
        /// <param name="height">画像縦幅</param>
        /// <param name="colbuf">RGBAバッファ</param>
        /// <returns>BGRAバッファ</returns>
        private static byte[] ConvertBGRA_RGBA(int width, int height, byte[] colbuf)
        {
            int colset = 4;

            byte[] ansbuf = new byte[width * height * colset];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int bpos = (y * width * colset) + (x * colset);

                    ansbuf[bpos] = colbuf[bpos + 2];
                    ansbuf[bpos + 1] = colbuf[bpos + 1];
                    ansbuf[bpos + 2] = colbuf[bpos];
                    ansbuf[bpos + 3] = colbuf[bpos + 3];
                }
            }

            return ansbuf;
        }

        //--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//

        /// <summary>
        /// Bitmap画像の作成
        /// </summary>
        /// <param name="fp"></param>
        /// <returns></returns>
        public static Bitmap CreateBitmapImage(this PngFile fp)
        {
            return BitmapSupporter.CreateBitmap(fp);
        }


        /// <summary>
        /// Bitmap画像の作成
        /// </summary>
        /// <param name="fp"></param>
        /// <returns></returns>
        public static Bitmap CreateBitmapImage(this APngFrame frame)
        {
            return BitmapSupporter.CreateBitmap(frame);
        }
    }
}
