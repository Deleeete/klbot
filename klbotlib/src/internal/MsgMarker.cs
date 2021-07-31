﻿using klbotlib.Exceptions;
using klbotlib.Json;
using klbotlib.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace klbotlib.Internal
{
    // 用来解析MessageMarker文本
    internal static class MsgMarker
    {
        static Exception ParseMessageMarkerException = new Exception("解析MsgMarker文本时发生错误");
        static Regex prefix_pattern = new Regex(@"\\(\w+?):(.+)");
        static Regex code_pat = new Regex(@"{([^{\\]*(?:\\.[^}\\]*)*)}");   //匹配{} 但是排除转义\{\}
        static Regex face_pat = new Regex(@"face:(\w+)");
        static Regex proto_pat = new Regex(@"^\w+://");
        private static bool TryParsePrefix(string content, out string prefix, out string body, bool start_only = false)
        {
            prefix = "";
            body = content;
            if (!prefix_pattern.IsMatch(content))
                return false;
            var groups = prefix_pattern.Match(content).Groups;
            if (start_only && groups[0].Index != 0)
                return false;
            prefix = groups[1].Value.ToLower(); //prefix不区分大小写
            body = groups[2].Value;
            return true;
        }

        //按类型分类的helper
        //把文本类型的MsgMarker编译为MessageChain
        private static string CompilePlainChainJson(string content)
        {
            List<string> elements = new List<string>();
            var matches = code_pat.Matches(content);
            int lhs_start = 0;
            foreach (Match match in matches)
            {
                string code = match.Groups[1].Value;
                int lhs_end = match.Index;  //不包括该位本身 即[ , )
                string lhs = content.Substring(lhs_start, lhs_end - lhs_start);    //到上一个表情之间的文本消息
                if (lhs.Length != 0)
                    elements.Add(JsonHelper.MessageElementBuilder.BuildPlainElement(lhs));    //先添加文本消息
                if (TryParsePrefix(code, out string prefix, out string body))
                {
                    if (prefix == "face")   //表情消息。格式：{\face:face_name}
                        elements.Add(JsonHelper.MessageElementBuilder.BuildFaceElement(body));
                    else if (prefix == "tag")     //@消息。格式：{\tag:目标id}
                    {
                        if (long.TryParse(body, out long id))
                            throw new MsgMarkerException($"无法将{body}转换为目标ID");
                        elements.Add(JsonHelper.MessageElementBuilder.BuildTagElement(id));
                    }
                    else
                        throw new MsgMarkerException($"未知或不支持的嵌入消息类型\"{prefix}\"");
                }
                else
                    elements.Add(JsonHelper.MessageElementBuilder.BuildPlainElement(match.Value)); //如果无法匹配prefix语法，则当作笔误处理，按照纯文本输出
                lhs_start = lhs_end + match.Value.Length;
            }
            //处理最后可能剩下的文本
            if (lhs_start != content.Length)
                elements.Add(JsonHelper.MessageElementBuilder.BuildPlainElement(content.Substring(lhs_start)));
            return string.Join(",", elements);
        }
        //把图像类型的MsgMarker编译为MessageChain
        private static string CompileImageChainJson(Module module, string content)
        {
            if (!TryParsePrefix(content, out string key, out string value))
                throw new MsgMarkerException($"无法解析图像消息\"{content}\"");
            if (key != "url" &&  key != "base64")
                throw new MsgMarkerException($"不支持的图像来源类型\"{key}\"");
            if (key == "url" && !proto_pat.IsMatch(value))  //来源类型为url且未指定协议类型
                value = $"file://{Path.Combine(module.GetModuleCacheDir(), value)}";
            return JsonHelper.MessageElementBuilder.BuildImageElement(key, value);
        }
        //把语音类型的MsgMarker编译为MessageChain
        private static string CompileVoiceChainJson(Module module, string content)
        {
            if (!TryParsePrefix(content, out string key, out string value))
                throw new MsgMarkerException($"无法解析音频消息\"{content}\"");
            if (key != "url" && key != "base64")
                throw new MsgMarkerException($"不支持的音频来源类型\"{key}\"");
            if (key == "url" && !proto_pat.IsMatch(value))  //来源类型为url且未指定协议类型
                value = $"file://{Path.Combine(module.GetModuleCacheDir(), value)}";
            return JsonHelper.MessageElementBuilder.BuildVoiceElement(key, value);
        }

        //把任意类型MsgMarker转换成相应的MessageChain
        internal static string CompileMessageChainJson(Module module, string content)
        {
            if (!TryParsePrefix(content, out string type, out string body, start_only: true))
            {
                //无prefix语法则默认当作纯文本
                type = "plain"; 
                body = content;
            }
            switch (type)
            {
                case "plain":
                    return CompilePlainChainJson(body);
                case "image":
                    return CompileImageChainJson(module, body);
                case "voice":
                    return CompileVoiceChainJson(module, body);
            }
            throw new MsgMarkerException($"不支持的消息类型\"{type}\"");
        }
    }
}
