using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace Clarity.Image.PNG
{
    /// <summary>
    /// chunk基底
    /// </summary>
    public abstract class Chunk
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="type">chunk type文字列</param>
        public Chunk(string type)
        {
            if(type.Length != 4)
            {
                throw new ArgumentException("invalid chunk type.");
            }

            this.Type = type;            
        }
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="type">chunk type配列</param>
        public Chunk(char[] type)
        {
            if (type.Length != 4)
            {
                throw new ArgumentException("invalid chunk type.");
            }

            this.Type = new string(type);
        }


        /// <summary>
        /// chunkデータ部のサイズ
        /// </summary>
        public uint Length { get; internal set; } = 0;

        /// <summary>
        /// chunk種類 4文字
        /// </summary>
        public string Type { get; init; } = "";

        /// <summary>
        /// chunk種類byteの取得
        /// </summary>
        public byte[] TypeByte
        {
            get
            {
                return Encoding.ASCII.GetBytes(this.Type);
            }
        }

        /// <summary>
        /// CRC破損チェック用
        /// </summary>
        public uint CRC { get; internal set; } = 0;


        /// <summary>
        /// chunkの読み込み
        /// </summary>
        /// <param name="data">data部</param>
        public abstract void ReadChunk(byte[] data);


        /// <summary>
        /// chunkバイナリの作成
        /// </summary>
        /// <returns>作成chunkデータ</returns>
        public byte[] CreateChunkBinary()
        {
            //data部の取得                
            byte[]? data = this.CreateDataArray();
            //lenght
            uint length = 0;
            if (data != null)
            {
                length = Convert.ToUInt32(data.Length);
            }
            //crcの計算
            uint crc = this.CalcuCRC(this.TypeByte, data);

            //書き込み
            using (MemoryStream mst = new MemoryStream())
            {
                //length
                this.WriteUInt32(mst, length);
                //type
                mst.Write(this.TypeByte);
                //data
                if (data != null)
                {
                    mst.Write(data);
                }
                //crc
                this.WriteUInt32(mst, crc);

                return mst.ToArray();
            }
        }

        /// <summary>
        /// Data部に相当する部分の配列を作成する
        /// </summary>
        /// <returns>データ部配列 null=data部なし</returns>
        protected virtual byte[]? CreateDataArray()
        {
            return null;
        }

        /// <summary>
        /// 文字列表記
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{this.GetType().Name}:{this.Type}:{this.Length}";
        }

        /// <summary>
        /// Uint32の読み込み
        /// </summary>
        /// <param name="mst">読み込み場所</param>
        /// <returns>読み込み値</returns>
        protected uint ReadUInt32(MemoryStream mst)
        {
            Span<byte> size = new Span<byte>(new byte[4]);
            mst.Read(size);
            return BinaryPrimitives.ReadUInt32BigEndian(size);
        }

        /// <summary>
        /// Uint32の読み込み
        /// </summary>
        /// <param name="mst">読み込み場所</param>
        /// <returns>読み込み値</returns>
        protected ushort ReadUInt16(MemoryStream mst)
        {
            Span<byte> size = new Span<byte>(new byte[2]);
            mst.Read(size);
            return BinaryPrimitives.ReadUInt16BigEndian(size);
        }

        /// <summary>
        /// byte値の読み込み
        /// </summary>
        /// <param name="mst">読み込み場所</param>
        /// <returns>読み込み値</returns>
        protected byte ReadByte(MemoryStream mst)
        {
            //width
            Span<byte> buf = new Span<byte>(new byte[1]);
            mst.Read(buf);

            return buf[0];
        }

        /// <summary>
        /// uint32値の書き込み
        /// </summary>
        /// <param name="st">書き込み場所</param>
        /// <param name="val">書き込み値</param>
        protected void WriteUInt32(Stream st, uint val)
        {
            Span<byte> data = new Span<byte>(new byte[4]);
            BinaryPrimitives.WriteUInt32BigEndian(data, val);

            st.Write(data);
        }

        /// <summary>
        /// uint16値の書き込み
        /// </summary>
        /// <param name="st"></param>
        /// <param name="val"></param>
        protected void WriteUInt16(Stream st, ushort val)
        {
            Span<byte> data = new Span<byte>(new byte[2]);
            BinaryPrimitives.WriteUInt16BigEndian(data, val);

            st.Write(data);
        }


        /// <summary>
        /// CRC値の計算
        /// </summary>
        /// <param name="ctype">chunk type</param>
        /// <param name="data">データ部</param>
        /// <returns></returns>
        private uint CalcuCRC(byte[] ctype, byte[]? data)
        {
            using (MemoryStream mst = new MemoryStream())
            {
                mst.Write(ctype);
                if (data != null)
                {
                    mst.Write(data);
                }

                return System.IO.Hashing.Crc32.HashToUInt32(mst.ToArray());
            }
        }

    }

    //--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//
    /// <summary>
    /// 汎用chunk(解析対象外chunk)
    /// </summary>
    internal class VoidChunk : Chunk
    {
        public VoidChunk(string s) : base(s)
        {

        }

        /// <summary>
        /// データ
        /// </summary>
        public byte[]? Data { get; set; } = null;
                
        /// <summary>
        /// chunkの読み込み
        /// </summary>
        /// <param name="data"></param>
        public override void ReadChunk(byte[] data)
        {
            this.Data = data;            
        }

        /// <summary>
        /// data chunkの作成
        /// </summary>
        /// <returns></returns>
        protected override byte[]? CreateDataArray()
        {
            return this.Data;
        }
    }

    //--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//
    /// <summary>
    /// ImageHeader
    /// </summary>
    public class IHDR : Chunk, ICloneable
    {
        public static readonly string CHUNK_TYPE = "IHDR";

        public IHDR() : base(CHUNK_TYPE)
        {

        }

        /// <summary>
        /// ColorType列挙
        /// </summary>
        public enum EColorType : byte
        {
            GreyScale = 0,
            Truecolor = 2,
            IndexedColor = 3,
            GreyScaleWithAlpha = 4,
            TrueColorWithAlpha = 6,
        }

        /// <summary>
        /// Interlace
        /// </summary>
        public enum EInterlaceMethod : byte
        {
            NoInterlace = 0,
            Adam7Interlace = 1,
        }

        #region メンバ変数
        /// <summary>
        /// Image Width
        /// </summary>
        public uint Width { get; set; } = 0;
        /// <summary>
        /// ImageHeight
        /// </summary>
        public uint Height { get; set; } = 0;
        /// <summary>
        /// Bit幅
        /// </summary>
        public byte BitDepth { get; set; } = 0;

        /// <summary>
        /// ColorType
        /// </summary>
        public EColorType ColorType { get; set; } = EColorType.Truecolor;

        /// <summary>
        /// 圧縮方式(0 only)
        /// </summary>
        public byte CompressionMethod { get; set; } = 0;
        /// <summary>
        /// Filter方式(0 only)
        /// </summary>
        public byte FilterMethod { get; set; } = 0;

        /// <summary>
        /// Interlace
        /// </summary>
        public EInterlaceMethod InterlaceMethod { get; set; } = EInterlaceMethod.NoInterlace;

        #endregion

        /// <summary>
        /// Chunkの読み込み
        /// </summary>
        /// <param name="data">データ配列</param>
        public override void ReadChunk(byte[] data)
        {
            using (MemoryStream mst = new MemoryStream(data))
            {
                //width
                this.Width = this.ReadUInt32(mst);

                //height
                this.Height = this.ReadUInt32(mst);

                //bit depth                
                this.BitDepth = this.ReadByte(mst);

                //ColourType
                this.ColorType = (EColorType)this.ReadByte(mst);

                //compression method
                this.CompressionMethod = this.ReadByte(mst);

                //filter method
                this.FilterMethod = this.ReadByte(mst);

                //interace method
                this.InterlaceMethod = (EInterlaceMethod)this.ReadByte(mst);

            }
        }

        /// <summary>
        /// データ部バイナリの作成
        /// </summary>
        /// <returns></returns>
        protected override byte[]? CreateDataArray()
        {
            using(MemoryStream mst = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(mst))
                {
                    //width
                    this.WriteUInt32(mst, this.Width);

                    //height
                    this.WriteUInt32(mst, this.Height);

                    //bit depth                
                    bw.Write(this.BitDepth);

                    //ColourType
                    bw.Write((byte)this.ColorType);

                    //compression method
                    bw.Write(this.CompressionMethod);

                    //filter method
                    bw.Write(this.FilterMethod);

                    //interace method
                    bw.Write((byte)this.InterlaceMethod);
                }

                return mst.ToArray();
            }
        }

        /// <summary>
        /// ihdrのコピー
        /// </summary>
        /// <returns></returns>        
        public object Clone()
        {
            IHDR ans = new IHDR();

            ans.CRC = this.CRC;
            ans.Length = this.Length;

            ans.Width = this.Width;
            ans.Height = this.Height;
            ans.BitDepth = this.BitDepth;
            ans.ColorType = this.ColorType;
            ans.CompressionMethod = this.CompressionMethod;
            ans.FilterMethod = this.FilterMethod;
            ans.InterlaceMethod = this.InterlaceMethod;

            return ans;
        }
    }

    //--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//
    /// <summary>
    /// IENDチャンク
    /// </summary>
    internal class IEND : Chunk
    {
        public static readonly string CHUNK_TYPE = "IEND";

        public IEND() : base(CHUNK_TYPE)
        {

        }

        public override void ReadChunk(byte[] data)
        {
            //特になし
        }
    }

    //--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//
    /// <summary>
    /// tEXTチャンク
    /// </summary>
    public class tEXt : Chunk
    {
        static readonly string CHUNK_TYPE = "tEXt";

        public tEXt() : base(CHUNK_TYPE)
        {

        }
        /// <summary>
        /// キーワード
        /// </summary>
        public string KeyWord { get; set; } = "";
        /// <summary>
        /// text
        /// </summary>
        public string TextString { get; set; } = "";

        public override void ReadChunk(byte[] data)
        {

            //null spacer indexの取得
            int nsi = 0;
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] == 0)
                {
                    nsi = i;
                    break;
                }
            }

            //前半部のkeyword取得
            Span<byte> ks = new Span<byte>(data, 0, nsi);
            this.KeyWord = System.Text.Encoding.ASCII.GetString(ks);

            //null spacer分移動
            nsi += 1;

            //残りを取得
            Span<byte> tx = new Span<byte>(data, nsi, data.Length - nsi);
            this.TextString = System.Text.Encoding.ASCII.GetString(tx);

            //

        }


        /// <summary>
        /// データ配列の作成
        /// </summary>
        /// <returns></returns>
        protected override byte[]? CreateDataArray()
        {
            using (MemoryStream mst = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(mst))
                {
                    //keyword
                    bw.Write(Encoding.ASCII.GetBytes(this.KeyWord));
                    //null spacer
                    bw.Write((byte)0);
                    //text
                    bw.Write(Encoding.ASCII.GetBytes(this.TextString));

                }

                return mst.ToArray();
            }
        }

        public override string ToString()
        {
            return $"{this.Type}:{this.KeyWord}-{this.TextString}";
        }

    }

    //--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//
    /// <summary>
    /// IDAT chunk
    /// </summary>
    public class IDAT : Chunk
    {
        public static readonly string CHUNK_TYPE = "IDAT";
        public IDAT() : base(CHUNK_TYPE)
        {

        }

        public byte[] Data { get; set; } = { };

        public override void ReadChunk(byte[] data)
        {
            this.Data = data;
        }


        protected override byte[]? CreateDataArray()
        {
            return this.Data;
        }

    }

    //--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//
    /// <summary>
    /// PLTEチャンク
    /// </summary>
    internal class PLTE : Chunk
    {
        static readonly string CHUNK_TYPE = "PLTE";

        public PLTE() : base(CHUNK_TYPE)
        {

        }

        /// <summary>
        /// 色情報
        /// </summary>
        public List<Color> Pallet { get; protected set; } = new List<Color>();

        /// <summary>
        /// chunkの読込
        /// </summary>
        /// <param name="data"></param>
        public override void ReadChunk(byte[] data)
        {
            //pallet数の計算
            uint count = this.Length / 3;

            this.Pallet = new List<Color>();

            for (int i = 0; i < count; i++)
            {
                int pos = i * 3;

                byte r = data[pos];
                byte g = data[pos + 1];
                byte b = data[pos + 2];

                //
                this.Pallet.Add(Color.FromArgb(r, g, b));
            }
        }

        /// <summary>
        /// palletのchunk dataを作成
        /// </summary>
        /// <returns></returns>
        protected override byte[]? CreateDataArray()
        {
            List<byte> anslist = new List<byte>();
            this.Pallet.ForEach(x =>
            {
                anslist.Add(x.R);
                anslist.Add(x.G);
                anslist.Add(x.B);
            });


            return anslist.ToArray();
        }
    }

    //--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//
    /// <summary>
    /// PLTEの透明度情報
    /// </summary>
    internal class tRNS : Chunk
    {
        static readonly string CHUNK_TYPE = "tRNS";

        public tRNS() : base(CHUNK_TYPE)
        {

        }

        /// <summary>
        /// 透明度情報
        /// </summary>
        public List<byte> TransList { get; protected set; } = new List<byte>();

        /// <summary>
        /// chunkの読込
        /// </summary>
        /// <param name="data"></param>
        public override void ReadChunk(byte[] data)
        {
            //pallet数の計算
            uint count = this.Length;

            this.TransList = new List<byte>();

            for (int i = 0; i < count; i++)
            {
                this.TransList.Add(data[i]);
            }
        }

        /// <summary>
        /// chunk dataを作成
        /// </summary>
        /// <returns></returns>
        protected override byte[]? CreateDataArray()
        {
            return this.TransList.ToArray();
        }
    }
    //--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//
    //--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//
    /// <summary>
    /// ACTLチャンク
    /// </summary>
    class ACTL : Chunk
    {
        internal static readonly string CHUNK_TYPE = "acTL";

        public ACTL() : base(CHUNK_TYPE)
        {

        }

        /// <summary>
        /// 全体フレーム数
        /// </summary>
        public uint NumFrames { get; set; }
        /// <summary>
        /// ループ回数 0=無限
        /// </summary>
        public uint NumPlays { get; set; }


        /// <summary>
        /// chunk読み込み
        /// </summary>
        /// <param name="data"></param>
        public override void ReadChunk(byte[] data)
        {
            using (MemoryStream mst = new MemoryStream(data))
            {
                Span<byte> buf = new Span<byte>(new byte[4]);

                mst.Read(buf);
                this.NumFrames = BinaryPrimitives.ReadUInt32BigEndian(buf);

                mst.Read(buf);
                this.NumPlays = BinaryPrimitives.ReadUInt32BigEndian(buf);
            }
        }

        /// <summary>
        /// chunk書き込みデータ作成
        /// </summary>
        /// <returns></returns>
        protected override byte[]? CreateDataArray()
        {
            using MemoryStream mst = new MemoryStream();
            this.WriteUInt32(mst, this.NumFrames);
            this.WriteUInt32(mst, this.NumPlays);

            return mst.ToArray();
        }

    }

    /// <summary>
    /// Apng用chunk IF
    /// </summary>
    internal interface IAPngSeq
    {
        public uint SequenceNumber { get; set; }
    }
    //--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//
    //--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//
    /// <summary>
    /// フレーム情報
    /// </summary>
    internal class FCTL : Chunk, IAPngSeq
    {
        public static readonly string CHUNK_TYPE = "fcTL";

        public FCTL() : base(CHUNK_TYPE)
        {
        }

        public enum EDisposeOp : byte
        {
            None = 0,
            BackGround,
            Previous,
        }

        public enum EBlendOp : byte
        {
            SOURCE = 0,
            OVER,
        }

        /// <summary>
        /// シーケンス番号
        /// </summary>
        public uint SequenceNumber { get; set; }

        /// <summary>
        /// フレーム画像横幅
        /// </summary>
        public uint Width { get; set; }
        /// <summary>
        /// フレーム画像縦幅
        /// </summary>
        public uint Height { get; set; }

        /// <summary>
        /// フレーム画像描画位置X
        /// </summary>
        public uint XOffset { get; set; }
        /// <summary>
        /// フレーム画像描画位置Y
        /// </summary>
        public uint YOffset { get; set; }

        /// <summary>
        /// 表示秒数分子(単位：秒)
        /// </summary>
        public ushort DelayNum { get; set; }
        /// <summary>
        /// 表示秒数分母(単位：秒)
        /// </summary>
        public ushort DelayDen { get; set; }

        /// <summary>
        /// フレーム表示秒数(ミリsec)
        /// </summary>
        public float FrameTimeMiliSec
        {
            get
            {
                //double f = ((double)this.DelayNum / (double)this.DelayDen) * 1000.0;
                //int ans = Convert.ToInt32(f);

                float ans = ((float)this.DelayNum / (float)this.DelayDen) * 1000.0f;
                return ans;

            }
        }
        /// <summary>
        /// 描画時の前データ削除方法
        /// </summary>
        public EDisposeOp DisposeOp { get; set; }
        /// <summary>
        /// 描画方法
        /// </summary>
        public EBlendOp BlendOp { get; set; }

        /// <summary>
        /// 読み込み
        /// </summary>
        /// <param name="data"></param>
        public override void ReadChunk(byte[] data)
        {
            using (MemoryStream mst = new MemoryStream(data))
            {
                //フレーム連番
                this.SequenceNumber = this.ReadUInt32(mst);
                //
                this.Width = this.ReadUInt32(mst);
                //
                this.Height = this.ReadUInt32(mst);
                //
                this.XOffset = this.ReadUInt32(mst);
                //
                this.YOffset = this.ReadUInt32(mst);
                //
                this.DelayNum = this.ReadUInt16(mst);
                //
                this.DelayDen = this.ReadUInt16(mst);
                //
                this.DisposeOp = (EDisposeOp)this.ReadByte(mst);
                //
                this.BlendOp = (EBlendOp)this.ReadByte(mst);

            }
        }
        /// <summary>
        /// Data配列作成
        /// </summary>
        /// <returns></returns>
        protected override byte[]? CreateDataArray()
        {
            using MemoryStream mst = new MemoryStream();
            this.WriteUInt32(mst, this.SequenceNumber);
            this.WriteUInt32(mst, this.Width);
            this.WriteUInt32(mst, this.Height);
            this.WriteUInt32(mst, this.XOffset);
            this.WriteUInt32(mst, this.YOffset);
            this.WriteUInt16(mst, this.DelayNum);
            this.WriteUInt16(mst, this.DelayDen);
            mst.WriteByte((byte)this.DisposeOp);
            mst.WriteByte((byte)this.BlendOp);

            return mst.ToArray();
        }


        public override string ToString()
        {
            return $"{this.GetType().Name}:{this.SequenceNumber}";
        }
    }
    //--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//
    /// <summary>
    /// FDATチャンク
    /// </summary>
    class FDAT : Chunk, IAPngSeq
    {
        public static readonly string CHUNK_TYPE = "fdAT";

        public FDAT() : base(CHUNK_TYPE)
        {

        }

        /// <summary>
        /// シーケンス番号
        /// </summary>
        public uint SequenceNumber { get; set; }

        /// <summary>
        /// フレームデータ
        /// </summary>
        public byte[] Data { get; set; } = { };

        /// <summary>
        /// chunk読み込み
        /// </summary>
        /// <param name="data"></param>
        public override void ReadChunk(byte[] data)
        {
            using (MemoryStream mst = new MemoryStream(data))
            {
                //番号
                this.SequenceNumber = this.ReadUInt32(mst);

                Span<byte> buf = new Span<byte>(new byte[data.Length - 4]);
                mst.Read(buf);

                //データ
                this.Data = buf.ToArray();
            }
        }

        protected override byte[]? CreateDataArray()
        {
            using MemoryStream mst = new MemoryStream();
            this.WriteUInt32(mst, this.SequenceNumber);
            mst.Write(this.Data); 
            return mst.ToArray();
        }

        public override string ToString()
        {
            return $"{this.GetType().Name}:{this.SequenceNumber}:{this.Data.Length}";
        }
    }    
    //--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//

}
