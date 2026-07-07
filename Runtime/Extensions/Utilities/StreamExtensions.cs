using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;


public static class StreamExts
{
    /// <summary>
    /// ????;
    /// </summary>
    /// <param name="this">?</param>
    /// <returns>????</returns>
    public static byte[] ToArray(this Stream @this)
    {
        @this.Position = 0;
        // Stream.Read may return fewer bytes than requested; CopyTo reads the whole stream reliably.
        using var ms = new MemoryStream();
        @this.CopyTo(ms);
        @this.Seek(0, SeekOrigin.Begin);
        return ms.ToArray();
    }

    /// <summary>
    ///  ???????;
    /// </summary>
    /// <param name="this">????</param>
    /// <param name="closeAfter">?????</param>
    /// <returns>???</returns>
    public static List<string> ReadAllLines(this StreamReader @this, bool closeAfter = true)
    {
        var stringList = new List<string>();
        string str;
        while ((str = @this.ReadLine()) != null)
        {
            stringList.Add(str);
        }

        if (closeAfter)
        {
            @this.Close();
            @this.Dispose();
        }

        return stringList;
    }

    /// <summary>
    /// ???????;
    /// </summary>
    /// <param name="this">???</param>
    /// <param name="encoding">??</param>
    /// <param name="closeAfter">?????</param>
    /// <returns>???</returns>
    public static List<string> ReadAllLines(this FileStream @this, Encoding encoding, bool closeAfter = true)
    {
        var stringList = new List<string>();
        string str;
        var sr = new StreamReader(@this, encoding);
        while ((str = sr.ReadLine()) != null)
        {
            stringList.Add(str);
        }

        if (closeAfter)
        {
            sr.Close();
            sr.Dispose();
            @this.Close();
            @this.Dispose();
        }

        return stringList;
    }

    /// <summary>
    /// ??????;
    /// </summary>
    /// <param name="this">???</param>
    /// <param name="encoding">??</param>
    /// <param name="closeAfter">?????</param>
    /// <returns>????</returns>
    public static string ReadAllText(this FileStream @this, Encoding encoding, bool closeAfter = true)
    {
        var sr = new StreamReader(@this, encoding);
        var text = sr.ReadToEnd();
        if (closeAfter)
        {
            sr.Close();
            sr.Dispose();
            @this.Close();
            @this.Dispose();
        }

        return text;
    }

    /// <summary>
    /// ??????;
    /// </summary>
    /// <param name="this">???</param>
    /// <param name="content">??</param>
    /// <param name="encoding">??</param>
    /// <param name="closeAfter">?????</param>
    public static void WriteAllText(this FileStream @this, string content, Encoding encoding, bool closeAfter = true)
    {
        var sw = new StreamWriter(@this, encoding);
        @this.SetLength(0);
        sw.Write(content);
        if (closeAfter)
        {
            sw.Close();
            sw.Dispose();
            @this.Close();
            @this.Dispose();
        }
    }


    /// <summary>
    /// ????????????
    /// </summary>
    /// <param name="this">?</param>
    /// <param name="dest">????</param>
    /// <param name="bufferSize">?????,??8MB</param>
    public static void CopyToFile(this Stream @this, string dest, int bufferSize = 1024 * 8 * 1024)
    {
        using (var fsWrite = new FileStream(dest, FileMode.OpenOrCreate, FileAccess.ReadWrite))
        {
            byte[] buf = new byte[bufferSize];
            int len;
            while ((len = @this.Read(buf, 0, buf.Length)) != 0)
            {
                fsWrite.Write(buf, 0, len);
            }
        }
    }

    /// <summary>
    /// ????????????(????)
    /// </summary>
    /// <param name="this">?</param>
    /// <param name="dest">????</param>
    /// <param name="bufferSize">?????,??8MB</param>
    public static async Task CopyToFileAsync(this Stream @this, string dest, int bufferSize = 1024 * 1024 * 8)
    {
        using (var fsWrite = new FileStream(dest, FileMode.OpenOrCreate, FileAccess.ReadWrite))
        {
            await @this.CopyToAsync(fsWrite, bufferSize).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// ?????????
    /// </summary>
    /// <param name="this"></param>
    /// <param name="filename"></param>
    public static void SaveFile(this MemoryStream @this, string filename)
    {
        using (var fs = new FileStream(filename, FileMode.Create, FileAccess.Write))
        {
            byte[] buffer = @this.ToArray(); // ???byte????
            fs.Write(buffer, 0, buffer.Length);
            fs.Flush();
        }
    }

    /// <summary>
    /// ????? MD5 ?
    /// </summary>
    /// <param name="this">????</param>
    /// <returns>MD5 ?16?????</returns>
    public static string GetFileMD5(this FileStream @this)
    {
        return HashFile(@this, "md5");
    }

    /// <summary>
    /// ????? sha1 ?
    /// </summary>
    /// <param name="this">????</param>
    /// <returns>sha1 ?16?????</returns>
    public static string GetFileSha1(this Stream @this)
    {
        return HashFile(@this, "sha1");
    }

    /// <summary>
    /// ????????
    /// </summary>
    /// <param name="fs">????????</param>
    /// <param name="algo">????</param>
    /// <returns>???16?????</returns>
    static string HashFile(Stream fs, string algo)
    {
        HashAlgorithm crypto = default;
        switch (algo)
        {
            case "sha1":
                crypto = new SHA1CryptoServiceProvider();
                break;
            default:
                crypto = new MD5CryptoServiceProvider();
                break;
        }

        byte[] retVal = crypto.ComputeHash(fs);
        StringBuilder sb = new StringBuilder();
        foreach (var t in retVal)
        {
            sb.Append(t.ToString("x2"));
        }

        return sb.ToString();
    }
}