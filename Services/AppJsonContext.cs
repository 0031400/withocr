using System.Text.Json.Serialization;

namespace withocr.Models;

[JsonSerializable(typeof(AppConfig))]
public partial class AppJsonContext : JsonSerializerContext
{
}