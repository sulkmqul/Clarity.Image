using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clarity.Image.PNG
{
    /// <summary>
    /// フレーム画像整理情報
    /// </summary>
    internal class FrameTempInfo
    {
        internal FrameTempInfo(FCTL fc)
        {
            this.FrameControl = fc;
        }

        /// <summary>
        /// フレーム番号
        /// </summary>
        public int FrameNo { get; internal set; } = 0;

        /// <summary>
        /// このフレームの描画時間(ミリsec)
        /// </summary>
        public float FrameTime
        {
            get
            {
                return this.FrameControl.FrameTimeMiliSec;
            }
        }

        /// <summary>
        /// フレーム映像 RGBA
        /// </summary>
        public byte[] FarmeData { get; set; } = { };


        /// <summary>
        /// フレーム元映像 RGBA
        /// </summary>
        public byte[] SrcData { get; private set; } = { };

        //----
        #region メンバ変数

        /// <summary>
        /// フレーム制御値
        /// </summary>
        internal FCTL FrameControl { get; set; }

        /// <summary>
        /// データ部
        /// </summary>
        internal List<byte> DataList { get; set; } = new List<byte>();


        #endregion

        //--//

        /// <summary>
        /// フレームの元画像の作成
        /// </summary>
        /// <param name="header"></param>
        /// <param name="pal"></param>
        public void CreateSrcImage(IHDR header, ColorPallet? pal)
        {
            IHDR fh = (IHDR)header.Clone();
            fh.Width = this.FrameControl.Width;
            fh.Height = this.FrameControl.Height;

            PngDataManager ana = new PngDataManager();
            this.SrcData = ana.CreateRGBA(fh, this.DataList.ToArray(), pal);
        }

    }

    /// <summary>
    /// AnimePingの画像作成
    /// </summary>
    internal class APngRenderer
    {
        public APngRenderer()
        {
        }

        /// <summary>
        /// 色数
        /// </summary>
        int COLSET
        {
            get
            {
                return PngDataManager.N_RGBA;
            }
        }

        
        class ImageBuffer
        {
            public ImageBuffer(int width, int height, int coset)
            {
                this.Width = width;
                this.Height = height;
                this.Colset = coset;

                this.Data = new byte[width * height * coset];
                Array.Fill<byte>(this.Data, 0);
            }

            public int Width;
            public int Height;
            public int Colset = PngDataManager.N_RGBA;
            public byte[] Data;
            

            public int LineSize
            {
                get
                {
                    return this.Width * this.Colset;
                }
            }
        }


        /// <summary>
        /// フレーム描画
        /// </summary>
        /// <param name="header">画像ヘッダー情報</param>
        /// <param name="frame">今回のフレーム情報</param>
        /// <param name="prevframe">前回のフレーム情報</param>
        /// <returns>作成RGBAフレーム画像</returns>
        public byte[] RenderFrame(IHDR header, FrameTempInfo frame, FrameTempInfo? prevframe)
        {
            //出力バッファの作成
            ImageBuffer ans = new ImageBuffer((int)header.Width, (int)header.Height, COLSET);

            //クリア処理
            this.Clear(ans, frame, prevframe);

            //フレーム描画処理
            this.RenderFrameData(ans, frame);

            return ans.Data;
        }


        //--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//
        //--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//
        /// <summary>
        /// 描画前バッファクリア
        /// </summary>
        /// <param name="buf">描画バッファ</param>
        /// <param name="frame">処理フレーム情報</param>
        /// <param name="prev">前フレーム情報</param>
        /// <exception cref="InvalidOperationException"></exception>
        private void Clear(ImageBuffer buf, FrameTempInfo frame, FrameTempInfo? prev)
        {
            switch(frame.FrameControl.DisposeOp)
            {
                //None:前のフレームを描画してからなにも変更せずにそのまま利用する
                //よって前のフレームを描画すれば良いと思われる。
                case FCTL.EDisposeOp.None:
                    {
                        if(prev != null)
                        {
                            Array.Copy(prev.FarmeData, buf.Data, buf.Data.Length);
                        }
                    }
                    break;
                //BackGround:0クリア
                case FCTL.EDisposeOp.BackGround:
                    {
                        Array.Fill<byte>(buf.Data, 0);
                    }
                    break;
                //Previous:前のフレームを復元、Noneとの違いは不明
                case FCTL.EDisposeOp.Previous:
                    {
                        if (prev != null)
                        {
                            Array.Copy(prev.FarmeData, buf.Data, buf.Data.Length);
                        }
                    }
                    break;
                default:
                    throw new InvalidOperationException($"Invalid DisposeOp {frame.FrameControl.DisposeOp}");
            }
        }


        /// <summary>
        /// フレーム描画本体
        /// </summary>
        /// <param name="buf">描画バッファ</param>
        /// <param name="frame">処理フレーム情報</param>
        private void RenderFrameData(ImageBuffer buf, FrameTempInfo frame)
        {
            //コピー先領域を作成
            System.Drawing.Rectangle crect = new System.Drawing.Rectangle(
                (int)frame.FrameControl.XOffset,
                (int)frame.FrameControl.YOffset,
                (int)frame.FrameControl.Width,
                (int)frame.FrameControl.Height
            );

            //データコピー
            this.CopyROI(buf, frame.SrcData, crect, frame.FrameControl.BlendOp);
        }

        /// <summary>
        /// bufにデータ領域をコピーする
        /// </summary>
        /// <param name="buf">描画バッファ</param>
        /// <param name="data">処理データRGBA</param>
        /// <param name="rc">コピー先領域</param>
        private void CopyROI(ImageBuffer buf, byte[] data, System.Drawing.Rectangle rc, FCTL.EBlendOp bop)
        {
            for(int y=0; y<rc.Height; y++)
            {
                for(int x=0;  x<rc.Width; x++)
                {
                    //元領域位置
                    int srcpos = (y * rc.Width * COLSET) + (x * COLSET);

                    //書き込み位置
                    int dx = rc.X + x;
                    int dy = rc.Y + y;                    
                    int destpos = (dy * buf.Width * COLSET) + (dx * COLSET);

                    //SOURCEの時はそのままコピー
                    if (bop == FCTL.EBlendOp.SOURCE)
                    {
                        for (int i = 0; i < COLSET; i++)
                        {
                            buf.Data[destpos + i] = data[srcpos + i];
                        }
                    }
                    //OVERの時はアルファ値を考慮して描画 png仕様準ずる
                    //output = alpha * foreground + (1-alpha) * background
                    else if (bop == FCTL.EBlendOp.OVER)
                    {
                        byte fa = data[srcpos + 3];
                        
                        double far = fa / 255;
                        double bar = 1.0 - far;

                        for (int i = 0; i < COLSET; i++)
                        {
                            double v = ((double)buf.Data[destpos + i] * bar) +  ((double)data[srcpos + i] * far);
                            if(v > 255)
                            {
                                v = 255;
                            }     
                            buf.Data[destpos + i] = Convert.ToByte(v);
                        }
                        buf.Data[destpos + 3] = 255;
                    }
                    else
                    {
                        throw new InvalidOperationException($"Invalid BlendOp {bop}");
                    }
                }
            }
        }
        
    }
}
