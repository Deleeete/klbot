using klbotlib;
using klbotlib.Modules;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KLBotUnitTest
{
    [TestClass]
    public class TestKLBot
    {
        /// <summary>
        /// ����ģ�������Ƿ񷵻�Ԥ��ֵ
        /// </summary>
        [TestMethod]
        public void TestModuleCount()
        {
            KLBot bot = new("config/unit_test_config.json");
            Assert.AreEqual(TestConst.CoreModuleCount, bot.ModuleCount);
            bot.AddModule(new TimeModule());
            Assert.AreEqual(TestConst.CoreModuleCount + 1, bot.ModuleCount);
            var tm = new TimeModule();
            bot.AddModule(tm);
            Assert.AreEqual(TestConst.CoreModuleCount + 2, bot.ModuleCount);
        }
        /// <summary>
        /// ����ģ��ͳ�ƽ���Ƿ����Ԥ��
        /// </summary>
        [TestMethod]
        public void TestKLBotDiagData()
        {
            KLBot bot = new("config/unit_test_config.json");
            Assert.AreEqual(0, bot.DiagData.ReceivedMessageCount);
            Assert.AreEqual(0, bot.DiagData.ProcessedMessageCount);
            Assert.AreEqual(0, bot.DiagData.SuccessPackageCount);
            bot.SimulateMessagePlainInput(MessageContext.Group, -1, -1, "some non-sense");
            Assert.AreEqual(1, bot.DiagData.ReceivedMessageCount);
            Assert.AreEqual(0, bot.DiagData.ProcessedMessageCount);
            Assert.AreEqual(1, bot.DiagData.SuccessPackageCount);
            bot.SimulateMessagePlainInput(MessageContext.Group, -1, -1, "##help");
            Assert.AreEqual(2, bot.DiagData.ReceivedMessageCount);
            Assert.AreEqual(1, bot.DiagData.ProcessedMessageCount);
            Assert.AreEqual(2, bot.DiagData.SuccessPackageCount);
        }
    }
}
