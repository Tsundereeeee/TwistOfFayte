using Ocelot.Windows;

namespace PluginTemplate.Windows;

[OcelotConfigWindow]
public class ConfigWindow(Plugin plugin, Config config) : OcelotConfigWindow(plugin, config);
