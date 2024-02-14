using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace Clarity.Image.PNG
{
    


    /// <summary>
    /// APNGの書き込み
    /// </summary>
    public class APngWriter : PortableGraphicsNetworkWriter
    {
        public APngWriter()
        {

        }

        /// <summary>
        /// ファイル書き込み
        /// </summary>
        /// <param name="filepath">ファイルパス</param>
        /// <param name="flist">書き込みフレーム情報</param>
        /// <returns></returns>
        public async Task Save(string filepath, List<APngFrame> flist)
        {
            using(FileStream fp = new FileStream(filepath, FileMode.Create))
            {
                await this.Save(fp, flist);
            }
        }

        /// <summary>
        /// stream書き込み
        /// </summary>
        /// <param name="st">書き込みstream</param>
        /// <param name="flist">書き込みフレーム情報</param>
        /// <returns></returns>
        public async Task Save(Stream st, List<APngFrame> flist)
        {
            //エラーチェック
            this.CheckError(flist);

            //フレーム画像サイズ取得
            int width = flist[0].Width;
            int height = flist[0].Height;

            //ヘッダーの作成
            IHDR header = this.CreateHeader(width, height);

            //書き込みchunk一式
            List<Chunk> wclist = new List<Chunk>();

            //ACTLの作成
            ACTL actl = new ACTL();
            {
                actl.NumFrames = (uint)flist.Count;
                actl.NumPlays = 0;
            }
            wclist.Add(actl);

            //フレームの作成
            var aclist = this.AddAnimeFrameChunk(flist);
            wclist.AddRange(aclist);


            //書き込み
            await PortableGraphicsNetwork.WriteStream(st, header, wclist);

        }
        //--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//
        //--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//
        /// <summary>
        /// 書き込み前エラーチェック
        /// </summary>
        /// <param name="flist"></param>
        /// <exception cref="ArgumentException"></exception>
        private void CheckError(List<APngFrame> flist)
        {
            //書き込むものがなかった
            if(flist.Count <= 0)
            {
                throw new ArgumentException("frame list size 0");
            }

            //全部同じフレームサイズでないなら対応しない
            int wc = flist.Select(x => x.Width).Distinct().Count();
            int hc = flist.Select(x => x.Height).Distinct().Count();
            if(wc != 1 || hc != 1)
            {
                throw new ArgumentException("invalid frame size");
            }
        }



        /// <summary>
        /// フレーム画像をchunkにする
        /// </summary>
        /// <param name="flist">フレーム一覧</param>
        /// <returns>作成フレームchunk一式</returns>
        private List<Chunk> AddAnimeFrameChunk(List<APngFrame> flist)
        {
            List<Chunk> anslist = new List<Chunk>();

            uint seq = 0;

            PngDataManager mana = new PngDataManager();

            foreach (APngFrame frame in flist)
            {
                //FCTLの作成
                FCTL fctl = new FCTL();
                {
                    fctl.SequenceNumber = seq;
                    fctl.Width = (uint)frame.Width;
                    fctl.Height = (uint)frame.Height;
                    //フレーム画像は全部同じサイズ前提で作成するためオフセットは0固定
                    fctl.XOffset = 0;
                    fctl.YOffset = 0;

                    //ミリ秒で設定するようにしたので分母は1000固定
                    fctl.DelayNum = (ushort)frame.Time;
                    fctl.DelayDen = 1000;

                    //背景クリアして全描画
                    fctl.DisposeOp = FCTL.EDisposeOp.BackGround;
                    fctl.BlendOp = FCTL.EBlendOp.SOURCE;
                }
                seq++;
                anslist.Add(fctl);

                //fdAT
                byte[] databuf = mana.CompressRGBA(frame.Width, frame.Height, frame.FrameData);

                //1フレーム目はデフォルト画像にするべくIDATchunkとする
                if(seq == 1)
                {
                    IDAT data = new IDAT();
                    data.Data = databuf;
                    anslist.Add(data);
                    continue;
                }
                FDAT fdata = new FDAT();
                {
                    fdata.SequenceNumber = seq;
                    fdata.Data = databuf;
                }
                seq++;
                anslist.Add(fdata);
            }

            return anslist;

        }

    }
}
