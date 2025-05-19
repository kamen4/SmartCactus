using System.Text.Json.Nodes;

namespace Utils;

public static class JsonUtils
{
    public static string? GetJsonDataWithSelector(string json, string selector)
    {
        var selectorSplited = selector.Split(':');
        var obj = JsonNode.Parse(json);
        for (int i = 0; i <  selectorSplited.Length; i++)
        {
            var newObj = obj?[selectorSplited[i]];
            if (newObj is null)
            {
                return null;
            }
            obj = newObj;
        }
        return obj?.AsValue().ToString();
    }
}
