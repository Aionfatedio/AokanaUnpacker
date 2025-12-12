using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class PRead
{
    private FileStream fs;
    public Dictionary<string, PRead.fe> ti;
    private KeySet currentKeys;

    private struct KeySet
    {
        public uint gk_mul, gk_add, gk_mag1, gk_mag2;
        public int dd_mod1, dd_add, dd_mod2, dd_xor;
        public int loop_start_index; 
        public int gk_shift_amount;  
    }

    // 苍之彼方四重奏 本体/EXTRA1 配置
    private static readonly KeySet Keys_Legacy = new KeySet
    {
        gk_mul = 7391,
        gk_add = 42828,
        gk_mag1 = 56,
        gk_mag2 = 239,
        dd_mod1 = 253,
        dd_add = 3,
        dd_mod2 = 89,
        dd_xor = 153,
        loop_start_index = 4,
        gk_shift_amount = 17 
    };

    // 苍之彼方四重奏 EXTRA2 配置
    private static readonly KeySet Keys_Extra2 = new KeySet
    {
        gk_mul = 4892,
        gk_add = 42816,
        gk_mag1 = 156,
        gk_mag2 = 206,
        dd_mod1 = 179,
        dd_add = 3,
        dd_mod2 = 89,
        dd_xor = 119,
        loop_start_index = 3,
        gk_shift_amount = 7  
    };

    public PRead(string fn)
    {
        this.fs = new FileStream(fn, FileMode.Open, FileAccess.Read);

        if (!TryInit(Keys_Legacy))
        {
            if (!TryInit(Keys_Extra2))
            {
                throw new Exception("FATAL");
            }
        }

        if (fn.ToLower().EndsWith("adult.dat") && this.ti.ContainsKey("def/version.txt"))
        {
            this.ti.Remove("def/version.txt");
        }
    }

    private bool TryInit(KeySet keys)
    {
        try
        {
            this.currentKeys = keys;
            this.ti = new Dictionary<string, PRead.fe>();
            this.fs.Position = 0L;

            byte[] array = new byte[1024];
            this.fs.Read(array, 0, 1024);

            int num = 0;
            for (int i = keys.loop_start_index; i < 255; i++)
            {
                num += BitConverter.ToInt32(array, i * 4);
            }

            if (num < 0 || num > 1000000) return false;

            byte[] array2 = new byte[16 * num];
            if (this.fs.Read(array2, 0, array2.Length) != array2.Length) return false;

            this.dd(array2, 16 * num, BitConverter.ToUInt32(array, 212));

            int num2 = BitConverter.ToInt32(array2, 12) - (1024 + 16 * num);
            if (num2 < 0 || num2 > 200 * 1024 * 1024) return false;

            byte[] array3 = new byte[num2];
            if (this.fs.Read(array3, 0, array3.Length) != array3.Length) return false;

            this.dd(array3, num2, BitConverter.ToUInt32(array, 92));

            this.Init2(array2, array3, num);
            return true;
        }
        catch
        {
            return false;
        }
    }

    protected void Init2(byte[] rtoc, byte[] rpaths, int numfiles)
    {
        int num = 0;
        for (int i = 0; i < numfiles; i++)
        {
            int num2 = 16 * i;
            uint l = BitConverter.ToUInt32(rtoc, num2);
            int num3 = BitConverter.ToInt32(rtoc, num2 + 4);
            uint k = BitConverter.ToUInt32(rtoc, num2 + 8);
            uint p = BitConverter.ToUInt32(rtoc, num2 + 12);

            int num4 = num3;
            if (num4 >= rpaths.Length) num4 = rpaths.Length - 1;
            while (num4 < rpaths.Length && rpaths[num4] != 0)
            {
                num4++;
            }

            int len = num4 - num;
            if (len < 0) len = 0;

            string key = Encoding.ASCII.GetString(rpaths, num, len).ToLower();

            PRead.fe value = default(PRead.fe);
            value.p = p;
            value.L = l;
            value.k = k;

            if (!this.ti.ContainsKey(key)) this.ti.Add(key, value);

            num = num4 + 1;
        }
    }

    private void gk(byte[] b, uint k0)
    {
        uint num = k0 * currentKeys.gk_mul + currentKeys.gk_add;

        uint num2 = num << currentKeys.gk_shift_amount ^ num;

        for (int i = 0; i < 256; i++)
        {
            num -= k0;
            num += num2;
            num2 = num + currentKeys.gk_mag1;
            num *= (num2 & currentKeys.gk_mag2);
            b[i] = (byte)num;
            // Extra1代码这里是右移1位 (num >>= 1)，而Extra2代码是右移3位 (num >>= 3)
            // 如果位移是17，则右移1；如果是7(Extra2)，则右移3。
            if (currentKeys.gk_shift_amount == 17)
                num >>= 1;
            else
                num >>= 3;
        }
    }

    protected void dd(byte[] b, int L, uint k)
    {
        byte[] array = new byte[256];
        this.gk(array, k);
        for (int i = 0; i < L; i++)
        {
            byte b2 = b[i];
            b2 ^= array[i % currentKeys.dd_mod1];
            b2 += (byte)currentKeys.dd_add;
            b2 += array[i % currentKeys.dd_mod2];
            b2 ^= (byte)currentKeys.dd_xor;
            b[i] = b2;
        }
    }

    public virtual byte[] Data(string fn)
    {
        PRead.fe fe;
        if (!this.ti.TryGetValue(fn, out fe)) return null;

        this.fs.Position = (long)((ulong)fe.p);
        byte[] array = new byte[fe.L];
        this.fs.Read(array, 0, array.Length);
        this.dd(array, array.Length, fe.k);
        return array;
    }

    public void Release()
    {
        if (this.fs != null) { this.fs.Close(); this.fs = null; }
    }

    ~PRead() { this.Release(); }

    public struct fe
    {
        public uint p;
        public uint L;
        public uint k;
    }
}