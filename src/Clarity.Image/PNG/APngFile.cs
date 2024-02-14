using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clarity.Image.PNG
{

    /// <summary>
    /// AnimatedPNGフレーム情報
    /// </summary>
    public class APngFrame
    {
        public APngFrame(int w, int h, int time, byte[] img)
        {
            this.Width = w;
            this.Height = h;
            this.Time = time;
            this.FrameData = img;
        }

        /// <summary>
        /// 画像横幅
        /// </summary>
        public int Width { get; private set; } = 0;

        /// <summary>
        /// 画像縦幅
        /// </summary>
        public int Height { get; private set; } = 0;


        /// <summary>
        /// フレーム画像 RGBA
        /// </summary>
        public byte[] FrameData { get; private set; }

        /// <summary>
        /// これの表示時間ms
        /// </summary>
        public int Time { get; private set; } = 0;
    }

    /// <summary>
    /// アニメーション付きPNG画像管理
    /// </summary>
    public class APngFile : PortableGraphicsNetwork
    {
        public APngFile() : base()
        {

            this.AddAnalyzeChunk(new ACTL());
            this.AddAnalyzeChunk(new FCTL());
            this.AddAnalyzeChunk(new FDAT());
            this.AddAnalyzeChunk(new IDAT());

            

        }

        /// <summary>
        /// 既存画像RGBAバッファ
        /// </summary>
        public byte[] DefaultImage { get; set; } = { };


        /// <summary>
        /// フレーム情報
        /// </summary>
        public List<APngFrame> FrameList { get; protected set; } = new List<APngFrame>();

        /// <summary>
        /// フレーム映像の取得
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        public APngFrame this[int frame]
        {
            get
            {
                return this.FrameList[frame];
            }
        }

        /// <summary>
        /// フレーム数
        /// </summary>
        public int FrameCount
        {
            get
            {
                return this.FrameList.Count;
            }
        }

        //--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//

        

        //--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//
        /// <summary>
        /// 読み込み処理
        /// </summary>
        protected override void PostProcessing()
        {
            base.PostProcessing();

            this.FrameList = new List<APngFrame>();

            //アニメコントロールchunkの取得
            var actl = this.GetSelectChunkFirst<ACTL>();
            if (actl == null)
            {                
                throw new FormatException("Loading data is not APNG format.");
            }

            //フレーム番号ごとの情報に並べ変え
            var oflist = this.OrderingFrame();
            if (oflist.Count != actl.NumFrames)
            {
                throw new InvalidDataException("invalid frame count");
            }

            //アニメフレームの描画を行う
            APngRenderer render = new APngRenderer();
            FrameTempInfo? prev = null;
            oflist.ForEach(x =>
            {
                //元画像の作成
                x.CreateSrcImage(this.Header, this.Pallet);

                //フレーム画像の作成
                x.FarmeData = render.RenderFrame(this.Header, x, prev);

                //フレーム情報を作成
                //APngFrame f = new APngFrame((int)x.FrameControl.Width, (int)x.FrameControl.Height, x.FrameTime, x.SrcData);
                APngFrame f = new APngFrame((int)this.Header.Width, (int)this.Header.Height, x.FrameTime, x.FarmeData);
                this.FrameList.Add(f);

                prev = x;
            });


            
        }

        //--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//
        //--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//
        /// <summary>
        /// フレームごとに情報を並べ替え
        /// </summary>
        /// <returns></returns>
        /// <remarks>FCTL、IDAT、FDATのタグをフレームごとの情報になるように整理する</remarks>
        private List<FrameTempInfo> OrderingFrame()
        {
            List<IAPngSeq> seqlist = new List<IAPngSeq>();
            {
                //フレーム定義を取得
                seqlist.AddRange(this.GetSelectChunk<FCTL>());
                seqlist.AddRange(this.GetSelectChunk<FDAT>());

                //seq no順へ並べ替え
                seqlist = seqlist.OrderBy(x => x.SequenceNumber).ToList();
            }


            List<FrameTempInfo> anslist = new List<FrameTempInfo>();

            FrameTempInfo? ans = null;
            foreach (var seq in seqlist)
            {
                //FCTLを発見した
                FCTL? fc = seq as FCTL;
                if (fc != null)
                {
                    //次のフレームと解釈する、既存があればADDする
                    if (ans != null)
                    {
                        anslist.Add(ans);
                    }

                    ans = new FrameTempInfo(fc);
                    ans.FrameNo = anslist.Count;
                }

                //DATAフレームなら既存に追加
                FDAT? fdat = seq as FDAT;
                if (fdat != null)
                {
                    ans?.DataList.AddRange(fdat.Data);
                }
            }

            //最後のデータをADDする
            if (ans != null)
            {
                anslist.Add(ans);
            }

            //フレームが一つもない
            if (anslist.Count <= 0)
            {
                throw new InvalidDataException("Frame data is not contained");
            }

            //1フレーム目にデータがないならIDATのデフォルト画像があるはずなのでそれを1フレーム目とする
            if (anslist[0].DataList.Count <= 0)
            {
                //全IDATを取得
                var idatlist = this.GetSelectChunk<IDAT>();
                if (idatlist.Count <= 0)
                {
                    throw new InvalidDataException("First frame data is not contained");
                }
                idatlist.ForEach(x => anslist[0].DataList.AddRange(x.Data));
            }


            return anslist;
        }
    }
}
