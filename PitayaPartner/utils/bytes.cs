namespace PitayaPartner;

public class bytes
{
    //用于打印字节数组
    public static string BytesArrayToString(byte[] byteArray)
    {
        string res = "[";
        for (int i = 0; i < byteArray.Length; i++)
        {
            byte b = byteArray[i];
            res += b.ToString("X2");
            if (i != byteArray.Length - 1)
            {
                res += " ";
            }
        }
        return res + "]";
    }
}