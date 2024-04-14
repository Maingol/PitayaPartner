using System.Threading.Channels;
using TouchSocket.Core;

namespace PitayaPartner;

public class TestChannel1 {
    public static void channelStart() {
        var channel = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(1)
        {
            //缓冲区满时删除当前正在写的数据
            FullMode = BoundedChannelFullMode.DropWrite
        });

        Task.Run(async () =>
        {
            for (int i = 0; i < 1; i++)
            {    
                // await Task.Delay(TimeSpan.FromMilliseconds(200));
                await channel.Writer.WriteAsync(TouchSocketBitConverter.BigEndian.GetBytes(i));// 生产者写入消息
                channel.Writer.Complete();
                // if (i > 5)
                // {
                //     channel.Writer.Complete(); //生产者也可以明确告知消费者不会发送任何消息了
                // }
            }

        });
        
        // 同步方式等待并读取数据，支持超时
        var timeout = TimeSpan.FromSeconds(20); // 设置超时时间
        while (!channel.Reader.Completion.IsCompleted)
        {
            Console.WriteLine("~~~~~~1~~~~~");
            Task<bool> waitTask = channel.Reader.WaitToReadAsync().AsTask();

            Console.WriteLine("~~~~~~2~~~~~");
            if (waitTask.Wait(timeout)) // 阻塞等待，直到数据可读或超时。channel中无数据时，主线程会阻塞于此。
            {
                Console.WriteLine("~~~~~~3~~~~~");
                if (waitTask.Result && channel.Reader.TryRead(out byte[] item))
                {
                    Console.WriteLine(bytes.BytesArrayToString(item));
                }
            }
            else
            {
                Console.WriteLine("Reading from channel timed out.");
                break; // 如果超时，则退出循环
            }
        }

        Console.WriteLine("Done reading all data.");
        Console.WriteLine("666");
        Console.ReadKey();
    }
}