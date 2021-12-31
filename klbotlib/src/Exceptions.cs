﻿using klbotlib.Modules;
using System;

namespace klbotlib.Exceptions
{
    /// <summary>
    /// KLBot初始化失败异常
    /// </summary>
    public class KLBotInitializationException : Exception
    {
        internal KLBotInitializationException(string msg) : base($"KLBot初始化失败：{msg}") { }
    }
    /// <summary>
    /// 模块配置异常
    /// </summary>
    public class ModuleSetupException : Exception
    {
        internal ModuleSetupException(Module source, string msg) : base($"{source}模块配置异常：{msg}") { }
    }
    /// <summary>
    /// 模块状态异常
    /// </summary>
    public class ModuleStatusException : Exception
    {
        internal ModuleStatusException(Module source, string msg) : base($"{source}模块状态异常：{msg}") { }
    }
    /// <summary>
    /// 找不到模块异常
    /// </summary>
    public class ModuleMissingException : Exception
    {
        /// <summary>
        /// 新建一个找不到模块异常的实例
        /// </summary>
        /// <param name="msg">异常信息</param>
        public ModuleMissingException(string msg) : base($"找不到模块：{msg}") { }
    }
    internal class ModuleException : Exception
    {
        public ModuleException(Module source, string msg) : base($"模块{source}出现异常：{msg}") { }
    }
    internal class MsgMarkerException : Exception
    {
        public MsgMarkerException(string msg) : base($"MsgMarker文本解析异常：{msg}") { }
    }
    internal class MiraiException : Exception
    {
        public MiraiException(int code, string msg) : base($"Mirai服务器错误[{code}]：{msg}") { }
    }
}
