using System.Collections.Concurrent;

namespace PitayaPartner.tests;

public class dict
{
    public static readonly ConcurrentDictionary<uint,  int> responses = new();
    
    public static int GetResponseChannelForID(uint id)
    {
        // 使用 GetOrAdd 方法获取或创建一个新的 Channel
        return responses.GetOrAdd(id, (key) => 10);
    }

    public static void test()
    {
        Console.WriteLine(GetResponseChannelForID(1));
    }
}