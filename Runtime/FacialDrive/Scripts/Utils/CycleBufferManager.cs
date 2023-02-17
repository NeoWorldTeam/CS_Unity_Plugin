using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using UnityEngine;


//协议头数据结构体
[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct MessageHeader
{
    public Int64 timeStemp;

    public Int32 messageSize;

    public Int16 messageType;

    public Byte checkCode;
}


public class CycleBuffer
{
    //协议头长度
    public static int HeaderLength = 15;
    //协议尾长度
    public static int EnnderLength = 1;


    public int len;
    public int readSize;
    public byte[] buffer;


    //public MessageHeader messageHeader;
    //public int readHeaderSize;


    public CycleBuffer(int len)
    {
        this.len = len;
        this.readSize = 0;
        //this.readHeaderSize = 0;
        buffer = new byte[len];
    }
}



public abstract class MessageHandler
{

    protected int writeIndex = 0;
    protected int readIndex = 0;
    protected int cycleBufferSize = 3;
    protected List<CycleBuffer> readWriteCycleBuffer = new List<CycleBuffer>();

    public int checkCode;

    public MessageHandler(int bufferSize)
    {
        for (int i = 0; i < cycleBufferSize; i++)
        {
            readWriteCycleBuffer.Add(new CycleBuffer(3000));
        }
    }

    public bool isReadFinish(CycleBuffer cycleBuffer)
    {
        return cycleBuffer.readSize == cycleBuffer.len;
    }

    public abstract void ReadMessageContent(Socket socket);

    internal CycleBuffer getWriteCycleBuffer()
    {
        int writeCycleIndex = writeIndex % cycleBufferSize;
        return readWriteCycleBuffer[writeCycleIndex];
    }

    internal CycleBuffer getReadCycleBuffer()
    {
        if (readIndex >= writeIndex) return null;


        //int readCycleIndex = readIndex % cycleBufferSize;
        int dropNum = (writeIndex - 1) - readIndex;
        if (dropNum > 0)
        {
            //Debug.LogFormat("丢弃{0}个数据", dropNum);
        }

        readIndex = writeIndex - 1;
        int readCycleIndex = readIndex % cycleBufferSize;

        //Debug.LogFormat("读位置:{0}", readCycleIndex);
        var messageContentBuffer = readWriteCycleBuffer[readCycleIndex];
        readIndex = readIndex + 1;
        return messageContentBuffer;
    }

    internal bool hasReadBuffer()
    {
        return readIndex < writeIndex;
    }
}

public class BlendShapeMessageHandler : MessageHandler
{

    public BlendShapeMessageHandler(int bufferSize) : base(bufferSize)
    {
        
    }

    public override void ReadMessageContent(Socket socket)
    {
        CycleBuffer cycleBuffer = getWriteCycleBuffer();
        int needReadSize = cycleBuffer.len - cycleBuffer.readSize;
        int readSize = socket.Receive(cycleBuffer.buffer, cycleBuffer.readSize, needReadSize, SocketFlags.None);
        cycleBuffer.readSize = cycleBuffer.readSize + readSize;

        if (isReadFinish(cycleBuffer))
        {

            //校验
            if (checkCode == cycleBuffer.buffer[cycleBuffer.len - 1])
            {
                //移动写入标记
                writeIndex = writeIndex + 1;
            }
            else
            {
                Debug.LogFormat("错误位置:{0}", writeIndex % cycleBufferSize);
            }

        }
    }
}

public class HeadPoseMessageHander : MessageHandler
{
    public HeadPoseMessageHander(int bufferSize) : base(bufferSize)
    {

    }

    public override void ReadMessageContent(Socket socket)
    {
        CycleBuffer cycleBuffer = getWriteCycleBuffer();
        int needReadSize = cycleBuffer.len - cycleBuffer.readSize;
        int readSize = socket.Receive(cycleBuffer.buffer, cycleBuffer.readSize, needReadSize, SocketFlags.None);
        cycleBuffer.readSize = cycleBuffer.readSize + readSize;

        if (isReadFinish(cycleBuffer))
        {

            //校验
            if (checkCode == cycleBuffer.buffer[cycleBuffer.len - 1])
            {
                //移动写入标记
                writeIndex = writeIndex + 1;
            }
            else
            {
                Debug.LogFormat("错误位置:{0}", writeIndex % cycleBufferSize);
            }

        }
    }
}



public class CycleBufferManager 
{
    Dictionary<int, MessageHandler> messageHandlerDic = new Dictionary<int, MessageHandler>();


    public MessageHandler getMessageHander(int messageType, int messageSize)
    {
        if (messageHandlerDic.ContainsKey(messageType))
        {
            return messageHandlerDic[messageType];
        }
        else
        {
            if(messageSize == 0)
            {
                return null;
            }

            if(messageType == 1)
            {
                MessageHandler messageHander = new BlendShapeMessageHandler(messageSize);
                messageHandlerDic.Add(messageType, messageHander);
                return messageHander;
            }
            else
            {
                MessageHandler messageHander = new HeadPoseMessageHander(messageSize);
                messageHandlerDic.Add(messageType, messageHander);
                return messageHander;
            }

            

        }
    }

    public MessageHandler getMessageHander(int messageType)
    {
        return getMessageHander(messageType, 0);
    }
}




