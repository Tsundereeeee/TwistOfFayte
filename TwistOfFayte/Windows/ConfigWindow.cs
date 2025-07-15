using Ocelot.Windows;

namespace TwistOfFayte.Windows;

[OcelotConfigWindow]
public class ConfigWindow(Plugin plugin, Config config) : OcelotConfigWindow(plugin, config);
