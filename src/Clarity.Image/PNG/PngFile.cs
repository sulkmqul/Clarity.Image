using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clarity.Image.PNG
{
    /// <summary>
    /// PNG画像管理
    /// </summary>
    public class PngFile : PortableGraphicsNetwork
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public PngFile(): base()
        {
            //読込chunkを定義            
            this.AddAnalyzeChunk(new IDAT());
            this.AddAnalyzeChunk(new tEXt());
        }

               

        /// <summary>
        /// RGBAバッファ
        /// </summary>
        public byte[] Data { get; protected set; } = { };
        //--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//


        /// <summary>
        /// chunk読込後の処理
        /// </summary>
        protected override void PostProcessing()
        {
            //IDATを合成
            var datalist = this.GetSelectChunk<IDAT>();
            List<byte> buflist = new List<byte>();
            datalist.ForEach(x => buflist.AddRange(x.Data));

            //RGBA変換
            PngDataManager ana = new PngDataManager();
            this.Data = ana.CreateRGBA(this.Header, buflist.ToArray(), this.Pallet);
        }
    }
}
