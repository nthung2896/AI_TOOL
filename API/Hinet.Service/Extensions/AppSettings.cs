using Microsoft.Extensions.Configuration;
using System.Collections;
using System.Reflection;

namespace Hinet.Extensions
{
    public static class AppSettingsExtensions
    {
        public static void UseAppSettings(this IConfiguration configuration)
        {
            SetProperties(configuration, typeof(AppSettings), null);
        }

        private static void SetProperties(IConfiguration configuration, Type type, object? instance, string parentKey = "")
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);

            foreach (var property in properties)
            {
                string configKey = string.IsNullOrEmpty(parentKey) ? property.Name : $"{parentKey}:{property.Name}";

                if (property.PropertyType == typeof(string))
                {
                    property.SetValue(instance, configuration[configKey] ?? "");
                }
                else if (property.PropertyType == typeof(int) || property.PropertyType == typeof(int?))
                {
                    string value = configuration[configKey] ?? "";
                    if (!string.IsNullOrEmpty(value))
                    {
                        property.SetValue(instance, Convert.ToInt32(value));
                    }
                }
                else if (property.PropertyType == typeof(bool) || property.PropertyType == typeof(bool?))
                {
                    string value = configuration[configKey] ?? "";
                    property.SetValue(instance, value == "true");
                }
                else if (property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var elementType = property.PropertyType.GetGenericArguments()[0];
                    var listInstance = Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType)) as IList;
                    if (listInstance != null)
                    {
                        string valueString = configuration[configKey] ?? "";
                        var values = valueString.Split(",");
                        foreach (var value in values)
                        {
                            listInstance.Add(Convert.ChangeType(value, elementType));
                        }
                    }
                    property.SetValue(instance, listInstance);
                }
                else if (property.PropertyType.IsClass && property.PropertyType != typeof(string))
                {
                    var nestedInstance = Activator.CreateInstance(property.PropertyType);
                    SetProperties(configuration, property.PropertyType, nestedInstance, configKey);
                    property.SetValue(instance, nestedInstance);
                }
            }
        }
    }

#pragma warning disable CS8618

    public static class AppSettings
    {
        public static Connections Connections { get; set; }
        public static AuthSetting AuthSetting { get; set; }
        public static Mail Mail { get; set; }
        public static FileSetting FileSetting { get; set; }
        public static MoitSso MoitSso { get; set; }
    }

    public class MoitSso
    {
        // Base URL của Cổng dịch vụ công - Bộ Công Thương, ví dụ: http://sw.moit.gov.vn/
        public string BaseUrl { get; set; }
        // SecretKey do Cổng cấp cho dịch vụ công (clientkey)
        public string ClientKey { get; set; }
        // URL backend nhận callback từ Cổng (đầy đủ): ví dụ http://localhost:5111/api/MoitSso/Callback
        public string ReceiverUrl { get; set; }
        // URL FE để redirect sau khi đăng nhập SSO thành công, ví dụ http://localhost:3000/auth/sso-callback
        public string FrontendCallbackUrl { get; set; }
    }

    public class Connections
    {
        public string DefaultConnection { get; set; }
        public string DistCacheConnectionString { get; set; }
        public MongoDBConnection MongoDBConnection { get; set; }
    }

    public class MongoDBConnection
    {
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
    }

    public class AuthSetting
    {
        public string Key { get; set; }
        public string Audience { get; set; }
        public string Issuer { get; set; }
        public int SecondsExpires { get; set; }
        public int DaysRefreshTokenExpires { get; set; }
    }

    public class Mail
    {
        public string From { get; set; }
        public string Host { get; set; }
        public string Alias { get; set; }
        public int Port { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool EnableSsl { get; set; }
        public bool AllowSendMail { get; set; }
    }

    public class FileSetting
    {
        public List<string> AllowExtensions { get; set; }
        public int MaxSize { get; set; }
    }

#pragma warning restore CS8618
}
