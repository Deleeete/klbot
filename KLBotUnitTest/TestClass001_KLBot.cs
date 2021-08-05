using klbotlib;
using klbotlib.Modules;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KLBotUnitTest
{
    [TestClass]
    public class TestClass001_KLBot
    {
        /// <summary>
        /// ����ģ�������Ƿ񷵻�Ԥ��ֵ
        /// </summary>
        [TestMethod]
        public void TestMethod000_ModuleCount()
        {
            KLBot test_bot = new();
            Assert.AreEqual(test_bot.ModuleCount, 1, "ģ��������ʼ���Ȳ�Ϊ1�����Ŀ����������µ�ģ����");
            new TimeModule().AttachTo(test_bot);
            Assert.AreEqual(test_bot.ModuleCount, 2);
            var tm = new TimeModule();
            tm.AttachTo(test_bot);
            Assert.AreEqual(test_bot.ModuleCount, 3);
            tm.Detach();
            Assert.AreEqual(test_bot.ModuleCount, 2);
        }
    }
}
