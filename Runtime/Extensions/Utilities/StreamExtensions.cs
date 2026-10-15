using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;


namespace AetherNexus.FoundationPlatform.Extensions
{
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
    public static List<string> ReadAllLines(this StreamReader @this, bool closeAfter)
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

    /// <summary>Reads all lines and closes the reader afterwards.</summary>
    public static List<string> ReadAllLines(this StreamReader @this) => ReadAllLines(@this, true);

    /// <summary>
    /// ???????;
    /// </summary>
    /// <param name="this">???</param>
    /// <param name="encoding">??</param>
    /// <param name="closeAfter">?????</param>
    /// <returns>???</returns>
    public static List<string> ReadAllLines(this FileStream @this, Encoding encoding, bool closeAfter)
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

    /// <summary>Reads all lines and closes the stream afterwards.</summary>
    public static List<string> ReadAllLines(this FileStream @this, Encoding encoding) => ReadAllLines(@this, encoding, true);

    /// <summary>
    /// ??????;
    /// </summary>
    /// <param name="this">???</param>
    /// <param name="encoding">??</param>
    /// <param name="closeAfter">?????</param>
    /// <returns>????</returns>
    public static string ReadAllText(this FileStream @this, Encoding encoding, bool closeAfter)
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

    /// <summary>Reads all text and closes the stream afterwards.</summary>
    public static string ReadAllText(this FileStream @this, Encoding encoding) => ReadAllText(@this, encoding, true);

    /// <summary>
    /// ??????;
    /// </summary>
    /// <param name="this">???</param>
    /// <param name="content">??</param>
    /// <param name="encoding">??</param>
    /// <param name="closeAfter">?????</param>
    public static void WriteAllText(this FileStream @this, string content, Encoding encoding, bool closeAfter)
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

    /// <summary>Writes all text and closes the stream afterwards.</summary>
    public static void WriteAllText(this FileStream @this, string content, Encoding encoding) => WriteAllText(@this, content, encoding, true);
}}
