using BepInEx.Configuration;

namespace HarmonyModBuggos
{
    public static class ApConfig
    {
        public static ConfigEntry<string> Server;
        public static ConfigEntry<string> SlotName;
        public static ConfigEntry<string> Password;

        public static void Init(ConfigFile config)
        {
            // 2. Bind each one under a section like "Archipelago".
            //    Defaults: "localhost:38281", "Player1", ""
            Server = config.Bind(
                "Archipelago",                   
                "Server",                         
                "localhost:38281",
                "Archipelago server as host:port"
            );

            Slotname = config.Bind(
                "Archipelago",
                "SlotName",
                "Bug1",
                "Archipelago Slot Name from your YAML"
            );

            Password = config.Bind(
                "Archipelago",
                "Password",
                "",
                "Archipelago Password from your YAML"
            );


        }
    }
}