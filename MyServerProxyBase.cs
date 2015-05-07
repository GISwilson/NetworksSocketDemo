using Models;
using NetworkSocket.Fast;
using NetworkSocket.Fast.Attributes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// 此代理代码由Server.ToProxyCode()获得
/// </summary>
public abstract class MyServerProxyBase : FastTcpClientBase
{
    [Service(Implements.Remote, 100)]
    public Task<Boolean> Login(User user, Boolean ifAdmin)
    {
        return this.InvokeRemote<Boolean>(100, user, ifAdmin);
    }

    [Service(Implements.Remote, 101)]
    public Task<Int32> GetSun(Int32 x, Int32 y, Int32 z)
    {
        return this.InvokeRemote<Int32>(101, x, y, z);
    }

    [Service(Implements.Self, 102)]
    public abstract void WarmingClient(String title, String contents);

    [Service(Implements.Self, 103)]
    public abstract List<Int32> SortByClient(List<Int32> list);

    [Service(Implements.Remote, 105)]
    public Task<List<Int32>> GetItself(List<Int32> data)
    {
        return this.InvokeRemote<List<Int32>>(105, data);
    }

    [Service(Implements.Remote, 106)]
    public Task<string> GetItself(string data)
    {
        return this.InvokeRemote<string>(106, data);
    }

    [Service(Implements.Remote, 107)]
    public void ResetServerNum()
    {
        this.InvokeRemote<string>(107);
    }

    [Service(Implements.Remote, 108)]
    public Task<string> SendStrData(string data)
    {
        return this.InvokeRemote<string>(108, data);
    }
}
