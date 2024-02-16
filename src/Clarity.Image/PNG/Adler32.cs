using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Clarity.Image.PNG
{

    //https://datatracker.ietf.org/doc/html/rfc1950
    /**
     * ソースコードは一つづつ足しこむロジックのため(s2 << 16) + s1で求めた値をs1 s2に再度分割するロジックが入っているため
     * 分かりにくいが
     * 変数s1の初期化は0xFFFFでandをとることでs1だけ分離
     * s2初期化も>>16で戻して0xFFFFでandをとるのでs2だけを分離している
     * 
     * 初期値はadlerを1Lで初期化しているのでs1=1、s2=0であると思われる。
    */
    /// <summary>
    /// Adler32計算
    /// </summary>
    public class Adler32
    {
        /// <summary>
        /// 65535以内での最大素数
        /// </summary>
        private const int BASE = 65521;


        /// <summary>
        /// Adler32の計算
        /// </summary>
        /// <param name="buf">計算バッファ</param>
        /// <returns>計算値</returns>
        public uint CalcuAdler32(byte[] buf)
        {

            uint s1 = 1;
            uint s2 = 0;

            foreach(byte val in buf)
            {
                s1 = (s1 + val) % Adler32.BASE;
                s2 = (s1 + s2) % Adler32.BASE;
            }

            //一応範囲内にする
            s1 &= 0xFFFF;
            s2 &= 0xFFFF;

            uint ans = (s2 << 16) + s1;
            return ans;
        }


        /// <summary>
        /// Adler32の計算(byte[]返却版)
        /// </summary>
        /// <param name="buf">バッファ</param>
        /// <returns>計算値</returns>
        public byte[] CalcuAdler32_ByteArray(byte[] buf)
        {
            //計算
            uint val = this.CalcuAdler32(buf);

            //byte配列化
            Span<byte> ans = new Span<byte>(new byte[4]);
            BinaryPrimitives.WriteUInt32BigEndian(ans, val);
            
            return ans.ToArray();
        }
    }
    
}
