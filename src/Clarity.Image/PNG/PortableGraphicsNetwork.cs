using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clarity.Image.PNG
{

    /// <summary>
    /// PNG形式の解析 基底
    /// </summary>
    public abstract class PortableGraphicsNetwork
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public PortableGraphicsNetwork()
        {
            //基本解析対象をADD
            this.AddAnalyzeChunk(new IHDR());
            this.AddAnalyzeChunk(new PLTE());
            this.AddAnalyzeChunk(new tRNS());
            this.AddAnalyzeChunk(new IEND());
        }

        /// <summary>
        /// PNGのシグネチャ
        /// </summary>
        internal static readonly byte[] PNG_SIGNATURE = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
                
        #region メンバ変数

        /// <summary>
        /// Chunk一式
        /// </summary>
        private List<Chunk> ChunkList { get; set; } = new List<Chunk>();

        /// <summary>
        /// 解析chunk   [chunk type, chunk]
        /// </summary>
        private Dictionary<string, Type> AnalyzeTypeDic = new Dictionary<string, Type>();

        /// <summary>
        /// Header
        /// </summary>
        private IHDR? _Header = null;

        /// <summary>
        /// Header
        /// </summary>
        protected IHDR Header
        {
            get
            {
                if (this._Header == null)
                {
                    throw new InvalidOperationException("png image not loading");
                }
                return this._Header;
            }
        }

        /// <summary>
        /// ColorPallet
        /// </summary>
        public ColorPallet? Pallet { get; protected set; } = null;

        #endregion


        /// <summary>
        /// 画像横幅
        /// </summary>
        public int Width
        {
            get
            {
                return (int?)this._Header?.Width ?? 0;
            }
        }

        /// <summary>
        /// 画像縦幅
        /// </summary>
        public int Height
        {
            get
            {
                return (int?)this._Header?.Height ?? 0;
            }
        }
        //--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//

        /// <summary>
        /// Png画像の読み込み
        /// </summary>
        /// <param name="filepath">読み込みファイルパス</param>        
        public async Task Load(string filepath)
        {
            using (MemoryStream mst = new MemoryStream())
            {
                using (FileStream fp = new FileStream(filepath, FileMode.Open, FileAccess.Read))
                {
                    await fp.CopyToAsync(mst);
                }

                this.Analyze(mst);
            }
        }

        /// <summary>
        /// Pngの読み込み
        /// </summary>
        /// <param name="st">読み込みストリーム</param>
        public async Task Load(Stream st)
        {
            using (MemoryStream mst = new MemoryStream())
            {
                await st.CopyToAsync(mst);

                this.Analyze(mst);
            }
        }


        /// <summary>
        /// 書き込み
        /// </summary>
        /// <param name="st">書き込み場所</param>
        /// <param name="header">ヘッダー</param>
        /// <param name="wrlist">書き込みchunk IHDR,IENDは不要</param>
        public static async Task WriteStream(Stream st, IHDR header, List<Chunk> wrlist)
        {
            //書き込みリストの先頭にヘッダーを追加
            wrlist.Insert(0, header);

            //シグネチャの書き込み
            await st.WriteAsync(PNG_SIGNATURE);
                        
            //全chunkの書き込み
            foreach (Chunk ck in wrlist)
            {
                byte[] chunk = ck.CreateChunkBinary();
                await st.WriteAsync(chunk);
            }

            //最後にEND
            var end = new IEND();
            byte[] iebi = end.CreateChunkBinary();
            await st.WriteAsync(iebi);
        }

        //--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//
        /// <summary>
        /// 後始末処理
        /// </summary>
        protected virtual void PostProcessing()
        {
            //チャンク一覧表示
            this.ChunkList.ForEach(x => System.Diagnostics.Trace.WriteLine(x));
        }


        /// <summary>
        /// 解析chunkの追加
        /// </summary>
        /// <param name="chu">解析chunkクラス実体</param>
        protected bool AddAnalyzeChunk(Chunk chu)
        {            
            string ct = chu.Type;
            Type t = chu.GetType();

            //既存が追加済みだった
            bool f = this.AnalyzeTypeDic.ContainsKey(ct);
            if( f == true)
            {
                return false;
            }

            this.AnalyzeTypeDic.Add(ct, t);

            return true;
        }

        /// <summary>
        /// chunkを解析対象外にする
        /// </summary>
        /// <param name="ct">chunkタイプ</param>
        /// <returns></returns>
        protected void RemoveAnalyzeChunk(string ct)
        {
            this.AnalyzeTypeDic.Remove(ct);
        }


        /// <summary>
        /// 対象のchunkを取得
        /// </summary>        
        /// <returns></returns>
        protected List<T> GetSelectChunk<T>() where T : Chunk
        {
            return  this.ChunkList.Where(x => ((x as T) != null)).Select(x => (T)x).ToList();
        }

        /// <summary>
        /// 解析対象外のchunkを取得する
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        protected List<Chunk> GetNonAnalyzedChunk<T>() where T : Chunk
        {
            return this.ChunkList.Where(x => ((x as T) != null)).ToList();
        }
        /// <summary>
        /// 対象のchunk取得
        /// </summary>
        /// <param name="ctype">取得chunk type</param>
        /// <returns></returns>
        protected List<Chunk> GetSelectChunk(string ctype)
        {
            return this.ChunkList.Where(x => x.Type == ctype).ToList();
        }

        /// <summary>
        /// 対象のchunkを取得
        /// </summary>        
        /// <returns></returns>
        protected T? GetSelectChunkFirst<T>() where T :Chunk
        {
            return this.ChunkList.Where(x => ((x as T) != null)).FirstOrDefault() as T;
        }

        /// <summary>
        /// 対象のchunkを取得
        /// </summary>       
        /// <param name="ctype">取得chunk type</param>
        /// <returns></returns>
        protected Chunk? GetSelectChunkFirst(string ctype)
        {
            return this.ChunkList.Where(x => x.Type == ctype).FirstOrDefault();
        }
        //--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//
        //--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//

        /// <summary>
        /// 解析と読み込み
        /// </summary>
        /// <param name="mst">ファイルバッファ</param>        
        private void Analyze(MemoryStream mst)
        {
            //シグネチャのチェック
            bool sigret = this.CheckSignature(mst);
            if (sigret == false)
            {
                throw new InvalidDataException("Loading data is not PNG format.");
            }

            //全chunkの読みこみ
            this.ChunkList = this.AnalyzeChunk(mst);

            //headerの保存
            this._Header = this.GetSelectChunkFirst<IHDR>();
            if(this._Header == null)
            {
                throw new InvalidDataException("png header is not contained.");
            }


            //Palletの作成            
            PLTE? palc = this.GetSelectChunkFirst<PLTE>();
            tRNS? trac = this.GetSelectChunkFirst<tRNS>();
            this.Pallet = ColorPallet.Craete(palc, trac);


            //後処理
            this.PostProcessing();

            //読込後、メモリ節約のためchunkはクリア
            this.ChunkList.Clear();
        }

        /// <summary>
        /// シグネチャの確認
        /// </summary>
        /// <param name="mst">png画像</param>
        /// <returns>true=png画像である</returns>
        private bool CheckSignature(MemoryStream mst)
        {
            //読み込み
            Span<byte> sig = new Span<byte>(new byte[PNG_SIGNATURE.Length]);
            mst.Position = 0;
            mst.Read(sig);

            //signature確認
            int eq = sig.SequenceCompareTo(PNG_SIGNATURE);
            if (eq == 0)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Chunkの読み込み
        /// </summary>
        /// <param name="mst">読み込み画像</param>
        private List<Chunk> AnalyzeChunk(MemoryStream mst)
        {
            List<Chunk> anslist = new List<Chunk>();

            //読み取り開始位置設定
            mst.Position = PNG_SIGNATURE.Length;

            while (true)
            {
                var chu = this.ReadChunk(mst);
                anslist.Add(chu);


                //最終チャンクであった
                if (chu.Type == IEND.CHUNK_TYPE)
                {
                    break;
                }

                //最終チャンクなしにstreamの末尾まで行ってしまったらpng出ないと解釈する
                if (mst.Position >= mst.Length)
                {
                    throw new InvalidDataException("Loading data is not PNG format");
                }
            }

            return anslist;

        }

        /// <summary>
        /// chunkの読み込み
        /// </summary>
        /// <param name="mst"></param>
        /// <returns></returns>
        private Chunk ReadChunk(MemoryStream mst)
        {
            uint length;
            string ctype;
            byte[]? data = null;
            uint crc;

            //length
            {
                Span<byte> len = new Span<byte>(new byte[4]);
                mst.Read(len);                
                length = BinaryPrimitives.ReadUInt32BigEndian(len);
            }

            //type
            {
                Span<byte> tp = new Span<byte>(new byte[4]);
                mst.Read(tp);                
                ctype = System.Text.Encoding.ASCII.GetString(tp);
            }
            //data
            {

                if (length != 0)
                {
                    Span<byte> ds = new Span<byte>(new byte[length]);
                    mst.Read(ds);
                    data = ds.ToArray();
                }
            }
            //CRC
            {
                Span<byte> cr = new Span<byte>(new byte[4]);
                mst.Read(cr);
                crc = BinaryPrimitives.ReadUInt32BigEndian(cr);
            }

            //解析クラスの作成
            Chunk pp = this.CreateReadClass(ctype);
            pp.Length = length;
            pp.CRC = crc;

            //読み込み
            if (data != null)
            {
                pp.ReadChunk(data);
            }

            return pp;
        }

        /// <summary>
        /// 読み込み解析クラスの作成
        /// </summary>
        /// <param name="type">作成要求chunk type</param>
        /// <returns>作成クラス</returns>
        private Chunk CreateReadClass(string ctype)
        {
            //解析対象外なら汎用クラスを作成
            bool f = this.AnalyzeTypeDic.ContainsKey(ctype);
            if (f == false)
            {
                return new VoidChunk(ctype);
            }

            //対象を起動
            Type ct = this.AnalyzeTypeDic[ctype];
            Chunk? data = Activator.CreateInstance(ct) as Chunk;
            if (data == null)
            {
                return new VoidChunk(ctype);
            }

            return data;

        }

    }
}
