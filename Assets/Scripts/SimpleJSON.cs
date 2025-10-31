using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Простой JSON сериализатор для Unity (без внешних зависимостей)
/// </summary>
public static class SimpleJSONParser
{
    public static string ToJSON(object obj)
    {
        if (obj == null) return "null";
        
        if (obj is string str)
        {
            return "\"" + EscapeString(str) + "\"";
        }
        
        if (obj is int || obj is float || obj is double || obj is bool)
        {
            return obj.ToString().ToLower();
        }
        
        if (obj is Dictionary<string, object> dict)
        {
            var sb = new StringBuilder();
            sb.Append("{");
            bool first = true;
            foreach (var kvp in dict)
            {
                if (!first) sb.Append(",");
                sb.Append("\"").Append(kvp.Key).Append("\":").Append(ToJSON(kvp.Value));
                first = false;
            }
            sb.Append("}");
            return sb.ToString();
        }
        
        return "\"" + obj.ToString() + "\"";
    }
    
    public static Dictionary<string, object> FromJSON(string json)
    {
        var result = new Dictionary<string, object>();
        json = json.Trim();
        
        if (!json.StartsWith("{") || !json.EndsWith("}"))
        {
            throw new ArgumentException("Invalid JSON format");
        }
        
        json = json.Substring(1, json.Length - 2); // Remove { }
        
        var parts = SplitJSON(json);
        foreach (var part in parts)
        {
            var keyValue = part.Split(new char[] { ':' }, 2);
            if (keyValue.Length == 2)
            {
                string key = keyValue[0].Trim().Trim('"');
                string value = keyValue[1].Trim();
                result[key] = ParseValue(value);
            }
        }
        
        return result;
    }
    
    private static string EscapeString(string str)
    {
        return str.Replace("\\", "\\\\")
                  .Replace("\"", "\\\"")
                  .Replace("\n", "\\n")
                  .Replace("\r", "\\r")
                  .Replace("\t", "\\t");
    }
    
    private static List<string> SplitJSON(string json)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        int braceCount = 0;
        bool inString = false;
        bool escapeNext = false;
        
        foreach (char c in json)
        {
            if (escapeNext)
            {
                current.Append(c);
                escapeNext = false;
                continue;
            }
            
            if (c == '\\')
            {
                escapeNext = true;
                current.Append(c);
                continue;
            }
            
            if (c == '"')
            {
                inString = !inString;
            }
            
            if (!inString)
            {
                if (c == '{' || c == '[')
                {
                    braceCount++;
                }
                else if (c == '}' || c == ']')
                {
                    braceCount--;
                }
                else if (c == ',' && braceCount == 0)
                {
                    result.Add(current.ToString());
                    current.Clear();
                    continue;
                }
            }
            
            current.Append(c);
        }
        
        if (current.Length > 0)
        {
            result.Add(current.ToString());
        }
        
        return result;
    }
    
    private static object ParseValue(string value)
    {
        value = value.Trim();
        
        if (value == "null") return null;
        if (value == "true") return true;
        if (value == "false") return false;
        
        if (value.StartsWith("\"") && value.EndsWith("\""))
        {
            return value.Substring(1, value.Length - 2).Replace("\\\"", "\"").Replace("\\\\", "\\");
        }
        
        if (int.TryParse(value, out int intVal))
        {
            return intVal;
        }
        
        if (float.TryParse(value, out float floatVal))
        {
            return floatVal;
        }
        
        return value;
    }
}
