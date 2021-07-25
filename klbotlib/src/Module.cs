﻿using Gleee.Consoleee;
using klbotlib.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace klbotlib.Modules
{
    /// <summary>
    /// 消息处理模块基类.
    /// 这是KLBot功能实现的基本单位
    /// </summary>
    public abstract class Module
    {
        /// <summary>
        /// 模块名. 是模块种类的唯一标识. 直接等于模块在源码中的类名.
        /// </summary>
        public string ModuleName { get => GetType().Name; }
        /// <summary>
        /// 模块ID. 是模块对象的唯一标识. 等于“模块类名[在同类模块中的排位]”
        /// </summary>
        public string ModuleID { get; set; }
        /// <summary>
        /// 决定此模块是否是透明模块(默认为否).
        /// 透明模块处理消息之后会继续向后传递，以使得Bot内部在它之后的模块能继续处理这条消息.
        /// 非透明模块处理消息之后会销毁消息.
        /// </summary>
        public virtual bool IsTransparent { get; } = false;
        /// <summary>
        /// 决定KLBot实例发送此模块的输出时，是否在前面自动加上模块签名"[模块ID]"（默认开启）
        /// </summary>
        public virtual bool UseSignature { get; } = true;
        /// <summary>
        /// 过滤器(Message -> bool). 模块通过这个函数判断是否要处理某一条消息. 
        /// 当模块总开关开启时，结果为true的消息会被处理，结果为false的函数会忽略.
        /// </summary>
        /// <param name="msg">待判断消息</param>
        public abstract bool Filter(Message msg);
        /// <summary>
        /// 处理器(Message -> string). 模块通过这个函数处理所有(通过了过滤器的)消息. 
        /// </summary>
        /// <param name="msg">待处理消息</param>
        /// <returns>用字符串表示的处理结果. 如果你的命令不输出处理结果，返回null或空字符串</returns>
        public abstract string Processor(Message msg);
        /// <summary>
        /// 综合过滤器和开关的影响, 返回一条消息是否应被处理
        /// </summary>
        /// <param name="msg">待判断消息</param>
        public bool ShouldProcess(Message msg) => Enabled && Filter(msg);
        /// <summary>
        /// 模块所属的Bot
        /// </summary>
        public KLBot HostBot { get; }

        /// <summary>
        /// 模块的总开关. 默认开启. 此开关关闭时任何消息都会被忽略.
        /// </summary>
        [ModuleStatus]    //模块属性Attribute. 只有打上这个标记的属性能被Module.ImportPropertiesDict()和Module.ExportPropertiesDict()读取或保存.
        public bool Enabled { get; set; } = true;

        public Module(KLBot host_bot)
        {
            HostBot = host_bot;
        }

        //打印消息到控制台的标准方法
        public void ModulePrint(string message, ConsoleMessageType msg_type = ConsoleMessageType.Info, string prefix = "") 
            => HostBot.ModulePrint(this, message, msg_type, prefix);

        //配置和状态的存读档
        /// <summary>
        /// 从字典中导入模块属性(ModuleProperty)
        /// </summary>
        /// <param name="status_dict">要导入的属性字典</param>
        public void ImportDict(Dictionary<string, object> status_dict)
        {
            Type type = GetType();
            foreach (var kvp in status_dict)
            {
                PropertyInfo property = type.GetProperty_All(kvp.Key);
                if (property != null)
                {
                    if (!property.CanWrite)
                        ModulePrint($"配置文件或状态存档中包含模块{ModuleID}中的\"{property.Name}\"字段，但该字段没有set访问器，无法赋值", ConsoleMessageType.Warning);
                    else if (kvp.Value == null)
                    {
                        ModulePrint($"键值对导入失败: 配置文件中的\"{kvp.Key}\"字段值为null。请修改成非空值", ConsoleMessageType.Error);
                        throw new ModuleSetupException(this, "配置字段中出现null值，此行为不符合模块开发规范");
                    }
                    property.SetValue(this, RestoreType(property.PropertyType, kvp.Value));
                    continue;
                }
                else
                {
                    FieldInfo field = type.GetField_All(kvp.Key);
                    if (field != null)
                    {
                        if (kvp.Value == null)
                        {
                            ModulePrint($"键值对导入失败: 配置文件中的\"{kvp.Key}\"字段值为null。请修改成非空值", ConsoleMessageType.Error);
                            throw new ModuleSetupException(this, "配置字段中出现null值，此行为不符合模块开发规范");
                        }
                        field.SetValue(this, RestoreType(field.FieldType, kvp.Value));
                        continue;
                    }
                    else 
                        ModulePrint($"键值对导入失败: 模块中不存在字段\"{kvp.Key}\"", ConsoleMessageType.Warning);
                }
            }
        }
        /// <summary>
        /// 把模块的所有模块状态(ModuleStatus)导出到字典
        /// </summary>
        public Dictionary<string, object> ExportStatusDict() => ExportMemberWithAttribute(typeof(ModuleStatusAttribute));
        /// <summary>
        /// 把模块的所有模块配置(ModuleStatus)导出到字典
        /// </summary>
        public Dictionary<string, object> ExportSetupDict() => ExportMemberWithAttribute(typeof(ModuleSetupAttribute));


        //存读模块自定义文件
        /// <summary>
        /// 保存文本到模块私有目录
        /// </summary>
        /// <param name="relative_path">对模块私有目录的相对路径</param>
        /// <param name="text">保存的内容</param>
        public void SaveFileAsString(string relative_path, string text)
        {
            string path = Path.Combine(HostBot.GetModuleCacheDir(this), relative_path);
            HostBot.ModulePrint(this, $"正在保存文件\"{Path.GetFileName(path)}\"到\"{Path.GetDirectoryName(path)}\"...", ConsoleMessageType.Task);
            if (File.Exists(path))
                HostBot.ModulePrint(this, $"文件\"{path}\"已经存在，将直接覆盖", ConsoleMessageType.Warning);
            File.WriteAllText(path, text);
        }
        /// <summary>
        /// 保存二进制到模块私有目录
        /// </summary>
        /// <param name="relative_path">对模块私有目录的相对路径</param>
        /// <param name="bin">保存的内容</param>
        public void SaveFileAsBinary(string relative_path, byte[] bin)
        {
            string path = Path.Combine(HostBot.GetModuleCacheDir(this), relative_path);
            HostBot.ModulePrint(this, $"Saving \"{Path.GetFileName(path)}\" to \"{Path.GetDirectoryName(path)}\"...", ConsoleMessageType.Task);
            if (File.Exists(path))
                HostBot.ModulePrint(this, $"文件\"{path}\"已经存在，将直接覆盖", ConsoleMessageType.Warning);
            File.WriteAllBytes(path, bin);
        }
        /// <summary>
        /// 从模块私有目录里读取文本
        /// </summary>
        /// <param name="relative_path">要读取的文件对模块文件夹的相对路径</param>
        public string ReadFileAsString(string relative_path)
        {
            string path = Path.Combine(HostBot.GetModuleCacheDir(this), relative_path);
            HostBot.ModulePrint(this, $"正在保存文件\"{Path.GetFileName(path)}\"到\"{Path.GetDirectoryName(path)}\"...", ConsoleMessageType.Task);
            if (!File.Exists(path))
            {
                HostBot.ModulePrint(this, $"文件\"{path}\"不存在，无法读取", ConsoleMessageType.Error);
                throw new ModuleException(this, $"文件\"{path}\"不存在，无法读取");
            }
            return File.ReadAllText(path);
        }
        /// <summary>
        /// 从模块私有目录里读取二进制
        /// </summary>
        /// <param name="relative_path">要读取的文件对模块文件夹的相对路径</param>
        public byte[] ReadFileAsBinary(string relative_path)
        {
            string path = Path.Combine(HostBot.GetModuleCacheDir(this), relative_path);
            HostBot.ModulePrint(this, $"正在保存文件\"{Path.GetFileName(path)}\"到\"{Path.GetDirectoryName(path)}\"...", ConsoleMessageType.Task);
            if (!File.Exists(path))
            {
                HostBot.ModulePrint(this, $"文件\"{path}\"不存在，无法读取", ConsoleMessageType.Error);
                throw new ModuleException(this, $"文件\"{path}\"不存在，无法读取");
            }
            return File.ReadAllBytes(path);
        }

        //helper 
        /// <summary>
        /// NewtonSoft.JsonConvert会把一切整数变成int64，一切浮点数变成double
        /// 丫这么整虽然源码赋值没事(会自动转换)，但反射赋值时会出问题，所以需要手动恢复原本的类型
        /// v0.5更新：加入自动用泛型反序列化进一步处理其他未知类型的功能
        /// </summary>
        /// <param name="original_type">原始类型</param>
        /// <param name="value">待处理对象</param>
        /// <returns>转换为原始类型后的对象（如果无需转换则原样返回）</returns>
        private object RestoreType(Type original_type, object value)
        {
            if (value.GetType() == original_type)
                return value;
            else if (original_type == typeof(byte) ||
                original_type == typeof(short) ||
                original_type == typeof(int) ||
                original_type == typeof(float))
                return Convert.ChangeType(value, original_type);
            else if (value is JObject)
            {
                MethodInfo[] methods = typeof(JsonConvert).GetMethods();
                foreach (var method in methods)
                {
                    if (method.Name == "DeserializeObject" && method.IsGenericMethod)
                    {
                        var deserialize = method.MakeGenericMethod(original_type);
                        string json = value.ToString();
                        return deserialize.Invoke(null, new object[] { json });
                    }
                }
                throw new Exception("意外遇到反射异常：无法找到相应的方法。Newtonsoft.Json的API是否有所更改？");
            }
            else
                throw new Exception("遇到无法自动匹配转换的结果");
        }
        /// <summary>
        /// 把模块中的所有含有attribute_type标记的成员导出到字典
        /// </summary>
        private Dictionary<string, object> ExportMemberWithAttribute(Type attribute_type)
        {
            Dictionary<string, object> properties_dict = new Dictionary<string, object>();
            Type type = GetType();
            //export C# properties
            PropertyInfo[] properties = type.GetProperties_All().Where(x => x.GetCustomAttribute(attribute_type) != null).ToArray();
            foreach (var property in properties)
            {
                properties_dict.Add(property.Name, property.GetValue(this));
            }
            //export C# fields
            FieldInfo[] fields = type.GetFields_All().Where(x => x.GetCustomAttribute(attribute_type) != null).ToArray();
            foreach (var field in fields)
            {
                properties_dict.Add(field.Name, field.GetValue(this));
            }
            return properties_dict;
        }
        /// <summary>
        /// ToString()函数返回模块的ID
        /// </summary>
        public override string ToString() => ModuleID;
    }

    /// <summary>
    /// 方便实现只处理单个种类的Message的模块的基类
    /// 如果你的模块只处理单种消息（例如只处理文本消息），继承这玩意可以少写很多类型匹配的废话
    /// </summary>
    /// <typeparam name="T">模块所处理的特定消息类型</typeparam>
    public abstract class SingleTypeModule<T> : Module where T : Message
    {
        /// <summary>
        /// 过滤器(Message -> bool). 模块通过这个函数判断是否要处理某一条消息. 
        /// </summary>
        /// <param name="msg">待判断消息</param>
        public abstract bool Filter(T msg);
        /// <summary>
        /// 处理器(Message -> string). 模块通过这个函数处理所有(通过了过滤器的)消息. 
        /// </summary>
        /// <param name="msg">待处理消息</param>
        /// <returns>用字符串表示的处理结果</returns>
        public abstract string Processor(T msg);

        public sealed override bool Filter(Message msg)
        {
            if (msg is T tmsg)
                return Filter(tmsg);
            else
                return false;
        }
        public sealed override string Processor(Message msg)
        {
            if (msg is T tmsg)
                return Processor(tmsg);
            else
            {
                ModulePrint("意外遇到无法处理的消息类型", ConsoleMessageType.Error);
                throw new Exception("意外遇到无法处理的消息类型");
            }
        }

        public SingleTypeModule(KLBot host_bot) : base(host_bot) { }
    }
}
