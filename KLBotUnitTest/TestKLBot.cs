using klbotlib;
using klbotlib.MessageServer.Debug;
using klbotlib.Modules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace KLBotUnitTest;

[TestClass]
public class TestKLBot
{
    /// <summary>
    /// 测试模块数量是否返回预期值
    /// </summary>
    [TestMethod]
    public void TestModuleCount()
    {
        KLBot bot = new(new DebugMessageServer(TestConst.NullAction, TestConst.NullAction, TestConst.NullAction), "config/unit_test_config.json");
        Assert.AreEqual(TestConst.CoreModuleCount, bot.ModuleCount);
        bot.AddModule(new TimeModule());
        Assert.AreEqual(TestConst.CoreModuleCount + 1, bot.ModuleCount);
        var tm = new TimeModule();
        bot.AddModule(tm);
        Assert.AreEqual(TestConst.CoreModuleCount + 2, bot.ModuleCount);
    }
    /// <summary>
    /// 测试模块统计结果是否符合预期
    /// </summary>
    [TestMethod]
    public void TestKLBotDiagData()
    {
        DebugMessageServer server = new(TestConst.NullAction, TestConst.NullAction, TestConst.NullAction);
        KLBot bot = new(server, "config/unit_test_config.json");
        Assert.AreEqual(0, bot.DiagData.ReceivedMessageCount);
        Assert.AreEqual(0, bot.DiagData.ProcessedMessageCount);
        Assert.AreEqual(0, bot.DiagData.SuccessPackageCount);
        MessagePlain msg = new(-1, -1, "some non-sense");
        msg.Context = MessageContext.Group;
        server.AddReceivedMessage(msg);
        bot.ProcessMessages(bot.FetchMessages());
        Assert.AreEqual(1, bot.DiagData.ReceivedMessageCount);
        Assert.AreEqual(0, bot.DiagData.ProcessedMessageCount);
        Assert.AreEqual(1, bot.DiagData.SuccessPackageCount);
        msg = new(-1, -1, "##help");
        msg.Context = MessageContext.Group;
        server.AddReceivedMessage(msg);
        bot.ProcessMessages(bot.FetchMessages());
        Assert.AreEqual(2, bot.DiagData.ReceivedMessageCount);
        Assert.AreEqual(1, bot.DiagData.ProcessedMessageCount);
        Assert.AreEqual(2, bot.DiagData.SuccessPackageCount);
    }
}
