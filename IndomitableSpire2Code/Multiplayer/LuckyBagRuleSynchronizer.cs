using BaseLib.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Configuration;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Runs;

namespace IndomitableSpire2.IndomitableSpire2Code.Multiplayer;

/// <summary>
/// 按次请求房主配置，避免读档、重连或房主修改选项后沿用旧缓存。
/// 不把房主的值写入客户端配置文件；在一次赠品选择期间，规则保持不变。
/// </summary>
public static class LuckyBagRuleSynchronizer
{
    // 静态字典：键是请求ID(Guid)，值是 (发出请求时的网络服务实例, 等待结果的异步任务控制器)
    private static readonly Dictionary<string, (INetGameService Service, TaskCompletionSource<LuckyBagRelicRule> Completion)> Pending = [];
    
    internal static LuckyBagRelicRule Normalize(LuckyBagRelicRule rule) =>
        Enum.IsDefined(rule) ? rule : LuckyBagRelicRule.Direct;
    
    public static async Task<LuckyBagRelicRule> GetRule()
    {
        var service = RunManager.Instance.NetService;
        if (service.Type != NetGameType.Client) return Normalize(IndomitableConfiguration.LuckyBagRelicRule);
        // 单机和主机直接返回本地配置，客户端逻辑单独处理
        var id = Guid.NewGuid().ToString("N");
        var completion = new TaskCompletionSource<LuckyBagRelicRule>();
        Pending.Add(id, (service, completion));
        // 断线回调：如果网络断开，直接取消等待的任务，防止死锁
        void Disconnected(NetErrorInfo _) => completion.TrySetCanceled();
        service.Disconnected += Disconnected;
        try
        {
            // 向房主发送请求。无响应时明确失败，不能悄悄采用客户端自己的规则。
            CustomMessageWrapper.Send(new LuckyBagRuleRequest { RequestId = id }, service);
            return await completion.Task.WaitAsync(TimeSpan.FromSeconds(15));
        }
        finally
        {
            service.Disconnected -= Disconnected;   // 移除断线监听，防止内存泄漏
            Pending.Remove(id);     // 从字典清理任务
        }
    }
    
    // 接收房主回包的内部方法
    internal static void Receive(string id, LuckyBagRelicRule rule, ulong senderId)
    {
        // 要求：任务存在、网络实例相同、当前使用客户端服务、发送者必须是房主
        if (!Pending.TryGetValue(id, out var pending) || pending.Service != RunManager.Instance.NetService ||
            pending.Service is not INetClientGameService client || client.NetClient?.HostNetId != senderId) return;
        pending.Completion.TrySetResult(Normalize(rule));
    }
}

public sealed class LuckyBagRuleRequest : ICustomMessage
{
    public string RequestId = "";
    public bool ShouldBroadcast => false;   // 点对点发送给房主
    public void Serialize(PacketWriter writer) => writer.WriteString(RequestId);
    public void Deserialize(PacketReader reader) => RequestId = reader.ReadString();
    public void HandleMessage(ulong senderId)
    {
        var service = RunManager.Instance.NetService;
        if (service.Type != NetGameType.Host) return;
        // 由房主处理消息，读取配置，打包成 Response 消息后发送
        service.SendMessage(new CustomMessageWrapper
        {
            Message = new LuckyBagRuleResponse
            {
                RequestId = RequestId,
                Rule = LuckyBagRuleSynchronizer.Normalize(IndomitableConfiguration.LuckyBagRelicRule)
            }
        }, senderId);
    }
}

public sealed class LuckyBagRuleResponse : ICustomMessage
{
    public string RequestId = "";
    public LuckyBagRelicRule Rule;
    public bool ShouldBroadcast => false;
    public void Serialize(PacketWriter writer) { writer.WriteString(RequestId); writer.WriteInt((int)Rule); }
    public void Deserialize(PacketReader reader) { RequestId = reader.ReadString(); Rule = (LuckyBagRelicRule)reader.ReadInt(); }
    // 客户端接受回复后触发，将收到的数据转交给同步器的 Receive 方法，去唤醒正在 await 的任务
    public void HandleMessage(ulong senderId) => LuckyBagRuleSynchronizer.Receive(RequestId, Rule, senderId);
}