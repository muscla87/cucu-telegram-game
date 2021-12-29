using System;

namespace Cucu
{
    public static class ConfigurationSettings
    {
        public static string BotApiKey { get { return System.Environment.GetEnvironmentVariable("BOT_TOKEN", EnvironmentVariableTarget.Process);} }
        
    }
}