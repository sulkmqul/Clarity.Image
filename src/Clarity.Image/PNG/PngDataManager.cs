using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;



namespace Clarity.Image.PNG
{
    /// <summary>
    /// PNG IDAT部の利用クラス
    /// </summary>
    internal class PngDataManager
    {
        /// <summary>
        /// RGBAカラーのオフセット定数
        /// </summary>
        internal const int N_RGBA = 4;

        /// <summary>
        /// Filterタイプ
        /// </summary>
        enum EFilterType : byte
        {
            None = 0,
            Sub,
            Up,
            Average,
            Paeth,
        }

        /// <summary>
        /// Filter解析関数のDelegate
        /// </summary>
        delegate byte[] AnalyzeFilterDelegate(byte[] srcline, byte[] prevline, int width, int height, int colset);
                

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public PngDataManager()
        {
        }

        //--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//
        //--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//
        /// <summary>
        /// RGBAバッファの作成
        /// </summary>
        /// <param name="header">pngヘッダ</param>
        /// <param name="idatavec">IDATchunkデータをまとめたバッファ</param>
        /// <param name="pal">パレット(あるなら)</param>
        /// <returns>RGBAバッファ</returns>
        public byte[] CreateRGBA(IHDR header, byte[] idatavec, ColorPallet? pal = null)
        {
            //BitDepth=8以外は対応しない
            if (header.BitDepth != 8)
            {
                throw new NotSupportedException($"{header.ColorType} BitDepth={header.BitDepth} is not supported");
            }

            //IDAT Bufferの解凍
            byte[] buf = this.DeCompressBuffer(idatavec);

            //色に応じた解析
            byte[] colbuf = { };
            switch (header.ColorType)
            {
                //------------------------------------------------------------------------------------------
                case IHDR.EColorType.Truecolor:                    
                    colbuf = this.AnalyzeTrueColorBuffer(buf, (int)header.Width, (int)header.Height, 3);
                    break;
                //------------------------------------------------------------------------------------------
                case IHDR.EColorType.TrueColorWithAlpha:
                    colbuf = this.AnalyzeTrueColorBuffer(buf, (int)header.Width, (int)header.Height, 4);
                    break;
                //------------------------------------------------------------------------------------------
                case IHDR.EColorType.IndexedColor:
                    if(pal == null)
                    {
                        throw new ArgumentException("Color pallet data does not exists");
                    }
                    colbuf = this.AnalyzeIndexedColorBuffer(buf, (int)header.Width, (int)header.Height, pal);
                    break;
                //------------------------------------------------------------------------------------------
                default:
                    //TrueColorとindexed以外は当面対応しない
                    throw new NotSupportedException($"{header.ColorType} BitDepth={header.BitDepth} is not supported");
            }

            return colbuf;
        }



        
        /// <summary>
        /// RGBAバッファをIDATデータ部形式へ変換する　TrueColorWithAlphaType
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="buf">RGBAバッファ</param>
        /// <remarks>大変すぎるのでFilterはNoneOnly</remarks>
        /// <returns></returns>
        public byte[] CompressRGBA(int width, int height, byte[] buf)        
        {
            //RGBA形式をfilter付の形式へ変換する
            byte[] fbuf = this.CreateFilterBuffer(width, height, buf);
            

            //zlib圧縮済みバッファ
            using MemoryStream compst = new MemoryStream();
            byte[] compbuf;

            //作成したバッファをZlib圧縮する
            using (DeflateStream dst = new DeflateStream(compst, CompressionMode.Compress))            
            //using (DeflateStream dst = new DeflateStream(compst, CompressionLevel.Fastest))
            {                
                dst.Write(fbuf, 0, fbuf.Length);                
            }

            //DeflateStreamをcloseしてから取得する
            compbuf = compst.ToArray();

            //zlibヘッダーを生成する
            //https://datatracker.ietf.org/doc/html/rfc1950
            using MemoryStream anst = new MemoryStream();

            
            //1byte目
            //CMF     0 - 3 圧縮方法 8 = deflate
            //CINFO   4 - 7 圧縮情報 7 = deflateならほぼ固定値            
            anst.WriteByte(0x78);

            //2byte目
            //FCHECK	0-4 1byte目2byteを合わせてushortと扱った時、その値が31で割り切れるようにするための値でチェック用
            //FDICT       5 辞書在り=1 無=0
            //FLEVEL      6 - 7 使用したアルゴリズム  0=最速アルゴリズム 1=速アルゴリズム 2=デフォルト 3=最小サイズ
            anst.WriteByte(0x9C);

            //FCHECKはとりあえず2byte目を辞書なしデフォルトにしたときは
            //0x80となる0x8078=3288で31で割ると28あまるので割り切れるようにするため0x80に28足して0x9Cとする
            //FCHECKは28=0x1Cということになる
            


            //byte[] arr = compbuf.ToArray();
            anst.Write(compbuf);

            
            //Checksum計算Adler32
            Adler32 adck = new Adler32();
            byte[] cksum = adck.CalcuAdler32_ByteArray(compbuf);

            anst.Write(cksum);


            byte[] ans = anst.ToArray();
            return ans;
        }


        //--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//
        //--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//
        /// <summary>
        /// RGBAバッファをpngのFilter付形式へ変換する
        /// </summary>
        /// <param name="width">横幅</param>
        /// <param name="height">縦幅</param>
        /// <param name="buf">RGBA</param>
        /// <returns></returns>
        byte[] CreateFilterBuffer(int width, int height, byte[] buf)
        {
            using MemoryStream mst = new MemoryStream();

            int size = width * N_RGBA;

            for (int y=0; y<height; y++)
            {
                //FILTER Typeの書き込み
                //大変すぎるのでFILTER無のみ
                mst.WriteByte((byte)EFilterType.None);

                //一行コピー                
                int offset = y * size;
                mst.Write(buf, offset, size);
                
            }

            return mst.ToArray();
        }


        /// <summary>
        /// IDATのバッファをzlib解凍する
        /// </summary>
        /// <param name="buf">idataのバッファ</param>
        /// <returns>解凍後バッファ</returns>
        public byte[] DeCompressBuffer(byte[] buf)
        {
            byte[] ansvec = { };

            using(MemoryStream inst = new MemoryStream())
            {
                //先頭2byteと後方4byteはzlib形式のヘッダー、フッターのため、解凍時には不要であるため
                //削除する
                inst.Write(buf, 2, buf.Length - 2 - 4);
                
                inst.Position = 0;
                using (DeflateStream dst = new DeflateStream(inst, CompressionMode.Decompress))
                {
                    using (MemoryStream ost = new MemoryStream())
                    {
                        dst.CopyTo(ost);
                        ansvec = ost.ToArray();


                    }
                }
            }

            return ansvec;
        }

        /// <summary>
        /// IndexedColorをRGBAバッファに変換する
        /// </summary>
        /// <param name="ibuf">indexed colorにおける解凍済みIDATバッファ</param>
        /// <param name="width">横幅</param>
        /// <param name="height">縦幅</param>
        /// <param name="pal">使用パレット</param>
        /// <returns>RGBAバッファ</returns>
        private byte[] AnalyzeIndexedColorBuffer(byte[] ibuf, int width, int height, ColorPallet pal)
        {
            //RGBAの結果バッファを作成
            byte[] ansbuf = new byte[width * height * N_RGBA];
            using MemoryStream anst = new MemoryStream(ansbuf);

            using MemoryStream ist = new MemoryStream(ibuf);

            for(int h=0; h<height; h++)
            {
                //流石にNone以外のフィルタは解析する気なし
                EFilterType ft = (EFilterType)ist.ReadByte();
                if(ft != EFilterType.None)
                {
                    throw new NotSupportedException($"Not Support IndexedColorFilter {ft}");
                }

                for(int x=0; x<width; x++)
                {
                    //index読込
                    int index = ist.ReadByte();
                    Color col = pal.Pallet[index];

                    anst.WriteByte(col.R);
                    anst.WriteByte(col.G);
                    anst.WriteByte(col.B);
                    anst.WriteByte(col.A);

                }
            }


            return ansbuf;
        }

        /// <summary>
        /// PNG TrueColorのIDATバッファを変換し、RGBAのバッファとする
        /// </summary>
        /// <param name="ibuf">解凍済みPNG IDATバッファ</param>
        /// <param name="width">横幅</param>
        /// <param name="height">縦幅</param>
        /// <param name="colset">カラーオフセット TrueColro=3 TrueColorWithAlpha=4を指定</param>
        /// <returns>RGBAバッファ</returns>
        private byte[] AnalyzeTrueColorBuffer(byte[] ibuf, int width, int height, int colset)
        {
            List<EFilterType> ftlist = new List<EFilterType>();

            //入力をstream化
            using MemoryStream ist = new MemoryStream(ibuf);

            //RGBAの結果バッファを作成
            byte[] ansbuf = new byte[width * height * N_RGBA];
            using MemoryStream anst = new MemoryStream(ansbuf);
            
            

            //Filter解析処理定義
            Dictionary<EFilterType, AnalyzeFilterDelegate> analyzedic = new Dictionary<EFilterType, AnalyzeFilterDelegate>();
            {
                analyzedic.Add(EFilterType.None, this.AnalyzeFilterNone);
                analyzedic.Add(EFilterType.Sub, this.AnalyzeFilterSub);
                analyzedic.Add(EFilterType.Up, this.AnalyzeFilterUp);
                analyzedic.Add(EFilterType.Average, this.AnalyzeFilterAverage);
                analyzedic.Add(EFilterType.Paeth, this.AnalyzeFilterPaeth);
            }


            byte[] linebuf = { };

            //全行の解析
            for (uint h = 0; h < height; h++)
            {
                //今回の読みだし位置を明示
                ist.Position = (h * width * colset) + h;

                //filter値取得
                EFilterType ft = (EFilterType)ist.ReadByte();

                //解析行を読み出し
                byte[] srcline = new byte[width * colset];
                ist.Read(srcline);


                //Filterに応じた解析を行い、RGBA値を取得する
                bool est = analyzedic.ContainsKey(ft);
                if(est == false)
                {
                    throw new InvalidDataException($"No Analyzed Filter Format={ft}");
                }
                linebuf = analyzedic[ft](srcline, linebuf, width, height, colset);


                //最終結果として書き込み
                anst.Write(linebuf);
                ftlist.Add(ft);

            }

            /*
            //FILTERの種類と適応数を出力
            var dic = ftlist.GroupBy(x => x).ToDictionary(x => x.Key, y => y.Count());
            foreach(var a in dic)
            {
                System.Diagnostics.Trace.WriteLine($"{a.Key}:{a.Value}");
            }*/

            return ansbuf;

        }

        /// <summary>
        /// 色の差分処理を行う
        /// </summary>
        /// <param name="a">元色</param>
        /// <param name="b">差分色</param>
        /// <returns>作成色</returns>
        private byte SubtractColor(int a, int b)
        {
            int tt = a + b;
            tt = tt % 256;

            byte ans = Convert.ToByte(tt);
            return ans;
        }



        /// <summary>
        /// Filter値 None行の解析
        /// </summary>
        /// <param name="srcline">解析line値</param>
        /// <param name="prevline">一つ上のRGBA値(空で先頭行)</param>
        /// <param name="width">画像横幅</param>
        /// <param name="height">画像縦幅</param>
        /// <param name="colset">色オフセット値</param>
        /// <returns>RGBAに変換した行</returns>
        private byte[] AnalyzeFilterNone(byte[] srcline, byte[] prevline, int width, int height, int colset)
        {
            //出力バッファの作成
            byte[] ansbuf = new byte[width * N_RGBA];
            Array.Fill<byte>(ansbuf, 255);

            //Noneの場合は生値が入っているのでコピーするだけでよい
            for(int x=0; x<width; x++)
            {   

                for(int i=0; i<colset; i++)
                {
                    int srcpos = x * colset + i;
                    int anspos = x * N_RGBA + i;

                    ansbuf[anspos] = srcline[srcpos];
                }
            }

            return ansbuf;

        }


        /// <summary>
        /// Filter値 Sub行の解析
        /// </summary>
        /// <param name="srcline">解析line値</param>
        /// <param name="prevline">一つ上のRGBA値(空で先頭行)</param>
        /// <param name="width">画像横幅</param>
        /// <param name="height">画像縦幅</param>
        /// <param name="colset">色オフセット値</param>
        /// <returns>RGBAに変換した行</returns>
        private byte[] AnalyzeFilterSub(byte[] srcline, byte[] prevline, int width, int height, int colset)
        {
            //出力バッファの作成
            byte[] ansbuf = new byte[width * N_RGBA];
            Array.Fill<byte>(ansbuf, 255);

            //Subの場合は一つ左との差分値が書かれている。
            //先頭行は0との差分=生データ
            for (int x = 0; x < width; x++)
            {
                for (int i = 0; i < colset; i++)
                {
                    int srcpos = (x * colset) + i;
                    int anspos = (x * N_RGBA) + i;

                    int leftanspos = ((x - 1) * N_RGBA) + i;

                    byte val = srcline[srcpos];
                    //一つ左のpixel色を取得する
                    byte leftval = 0;
                    if(leftanspos >= 0)
                    {
                        leftval = ansbuf[leftanspos];
                    }

                    ansbuf[anspos] = this.SubtractColor(leftval, val);
                }
            }

            return ansbuf;

        }

        /// <summary>
        /// Filter値 Up行の解析
        /// </summary>
        /// <param name="srcline">解析line値</param>
        /// <param name="prevline">一つ上のRGBA値(空で先頭行)</param>
        /// <param name="width">画像横幅</param>
        /// <param name="height">画像縦幅</param>
        /// <param name="colset">色オフセット値</param>
        /// <returns>RGBAに変換した行</returns>
        private byte[] AnalyzeFilterUp(byte[] srcline, byte[] prevline, int width, int height, int colset)
        {
            //出力バッファの作成
            byte[] ansbuf = new byte[width * N_RGBA];
            Array.Fill<byte>(ansbuf, 255);

            //Upの場合は一つ上の行との差分が書かれている
            //先頭行は生データ
            for (int x = 0; x < width; x++)
            {
                for (int i = 0; i < colset; i++)
                {
                    int srcpos = (x * colset) + i;
                    int anspos = (x * N_RGBA) + i;

                    byte val = srcline[srcpos];

                    //一つ上の値をとってくる
                    byte upval = 0;
                    if(prevline.Length > 0)
                    {
                        upval = prevline[anspos];
                    }


                    ansbuf[anspos] = this.SubtractColor(upval, val);
                }
            }

            return ansbuf;

        }


        /// <summary>
        /// Filter値 Average行の解析
        /// </summary>
        /// <param name="srcline">解析line値</param>
        /// <param name="prevline">一つ上のRGBA値(空で先頭行)</param>
        /// <param name="width">画像横幅</param>
        /// <param name="height">画像縦幅</param>
        /// <param name="colset">色オフセット値</param>
        /// <returns>RGBAに変換した行</returns>
        private byte[] AnalyzeFilterAverage(byte[] srcline, byte[] prevline, int width, int height, int colset)
        {
            //出力バッファの作成
            byte[] ansbuf = new byte[width * N_RGBA];
            Array.Fill<byte>(ansbuf, 255);

            //Averageの場合は一つ左の値と一つ上の値の平均、この平均との差分が書かれている。            
            for (int x = 0; x < width; x++)
            {
                for (int i = 0; i < colset; i++)
                {
                    int srcpos = (x * colset) + i;
                    int anspos = (x * N_RGBA) + i;
                    int leftanspos = ((x - 1) * N_RGBA) + i;

                    byte val = srcline[srcpos];

                    //一つ上
                    byte upval = 0;
                    if (prevline.Length > 0)
                    {
                        upval = prevline[anspos];
                    }
                    //一つ左
                    byte leftval = 0;
                    if (leftanspos >= 0)
                    {
                        leftval = ansbuf[leftanspos];
                    }
                    //平均計算
                    int average = 0;
                    {
                        double dave = Math.Floor((upval + leftval) / 2.0);
                        average = Convert.ToInt32(dave);
                    }


                    ansbuf[anspos] = this.SubtractColor(average, val);
                }
            }

            return ansbuf;

        }



        /// <summary>
        /// Filter値 Peath行の解析
        /// </summary>
        /// <param name="srcline">解析line値</param>
        /// <param name="prevline">一つ上のRGBA値(空で先頭行)</param>
        /// <param name="width">画像横幅</param>
        /// <param name="height">画像縦幅</param>
        /// <param name="colset">色オフセット値</param>
        /// <returns>RGBAに変換した行</returns>
        private byte[] AnalyzeFilterPaeth(byte[] srcline, byte[] prevline, int width, int height, int colset)
        {
            //出力バッファの作成
            byte[] ansbuf = new byte[width * N_RGBA];
            Array.Fill<byte>(ansbuf, 255);

            //Peathの場合は、一つ上、一つ左、左上の三個の値を取得し、Peathアルゴリズムで得られた値の差分が書かれている
            for (int x = 0; x < width; x++)
            {
                for (int i = 0; i < colset; i++)
                {
                    int srcpos = (x * colset) + i;
                    int anspos = (x * N_RGBA) + i;
                    int leftanspos = ((x - 1) * N_RGBA) + i;

                    
                    byte val = srcline[srcpos];

                    //一つ上
                    byte upval = 0;
                    if (prevline.Length > 0)
                    {
                        upval = prevline[anspos];
                    }
                    //一つ左
                    byte leftval = 0;
                    if (leftanspos >= 0)
                    {
                        leftval = ansbuf[leftanspos];
                    }
                    //左上
                    byte leftopval = 0;
                    if (prevline.Length > 0 && leftanspos >= 0)
                    {
                        leftopval = prevline[leftanspos];
                    }

                    //Paethの計算
                    int peathval = this.CalcuPaeth(leftval, upval, leftopval);

                    ansbuf[anspos] = this.SubtractColor(peathval, val);
                }
            }

            return ansbuf;

        }



        /// <summary>
        /// Paethロジックで適切な値を求める
        /// </summary>
        /// <param name="left">左の値</param>
        /// <param name="up">上の値</param>
        /// <param name="leftup">左上の値</param>
        /// <returns>求めた値</returns>
        private int CalcuPaeth(int left, int up, int leftup)
        {
            int p = left + up - leftup;

            int pl = Math.Abs(p - left);
            int pu = Math.Abs(p - up);
            int plu = Math.Abs(p - leftup);

            if (pl <= pu && pl <= plu)
            {
                return left;                
            }

            if(pu <= plu)
            {                
                return up;
            }

            return leftup;
            
        }
    }
}
