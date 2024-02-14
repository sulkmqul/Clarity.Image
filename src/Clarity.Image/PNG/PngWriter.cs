using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clarity.Image.PNG
{
    /// <summary>
    /// PNG書き込み基底
    /// </summary>
    public class PortableGraphicsNetworkWriter
    {
        /// <summary>
        /// 保存ヘッダー情報の作成
        /// </summary>
        /// <param name="width">横幅</param>
        /// <param name="height">縦幅</param>
        /// <returns>作成IHDR</returns>
        protected IHDR CreateHeader(int width, int height)
        {
            IHDR ans = new IHDR();

            ans.Width = (uint)width;
            ans.Height = (uint)height;
            ans.BitDepth = 8;

            //RGBA形式固定
            ans.ColorType = IHDR.EColorType.TrueColorWithAlpha;

            ans.CompressionMethod = 0;
            ans.FilterMethod = 0; ;
            ans.InterlaceMethod = IHDR.EInterlaceMethod.NoInterlace;


            return ans;
        }

    }


    /// <summary>
    /// PNGの書き込み
    /// </summary>
    public class PngWriter : PortableGraphicsNetworkWriter
    {
        public PngWriter()
        {

        }

        /// <summary>
        /// PNG画像の書き込み
        /// </summary>
        /// <param name="st">書き込みstream</param>
        /// <param name="width">画像横幅</param>
        /// <param name="height">縦幅</param>
        /// <param name="buf">書き込みRGBAバッファ</param>
        public async Task Write(Stream st, int width, int height, byte[] buf)
        {
            PngDataManager mana = new PngDataManager();

            //ヘッダーの作成
            IHDR header = this.CreateHeader(width, height);

            //IDATの作成
            IDAT dbuf = new IDAT();
            dbuf.Data = mana.CompressRGBA(width, height, buf);

            //書きこみ
            await PortableGraphicsNetwork.WriteStream(st, header, new List<Chunk> { dbuf });
        }
    }
}
