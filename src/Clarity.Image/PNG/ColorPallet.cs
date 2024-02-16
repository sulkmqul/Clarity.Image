using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clarity.Image.PNG
{
    /// <summary>
    /// 内部パレット定義(PLTEとtRNSをまとめて整理するもの)
    /// </summary>
    public class ColorPallet
    {
        /// <summary>
        /// パレット情報
        /// </summary>
        public List<Color> Pallet { get; private set; } = new List<Color>();

        //--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//--//
        /// <summary>
        /// パレット情報の作成
        /// </summary>
        /// <param name="pal">Color Pallet chunk</param>
        /// <param name="trans">Transparency chunk</param>
        /// <returns>作成パレット</returns>
        /// <remarks>PLTE情報をもとにtransを割り当ててアルファ付きの色にする</remarks>
        internal static ColorPallet? Craete(PLTE? pal, tRNS? trans)
        {
            //Pallet
            if (pal == null)
            {
                return null;
            }

            ColorPallet ans = new ColorPallet();

            int i = 0;
            foreach (Color pc in pal.Pallet)
            {
                //透明度情報あるなら取得
                byte transval = 255;
                if (trans != null)
                {
                    if (i < trans.TransList.Count)
                    {
                        transval = trans.TransList[i];
                    }
                }

                //パレット色を作成
                Color col = Color.FromArgb(transval, pc.R, pc.G, pc.B);
                ans.Pallet.Add(col);
                i++;
            }

            return ans;
        }

    }
}
