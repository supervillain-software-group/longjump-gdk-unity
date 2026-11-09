using System.Runtime.CompilerServices;

// The optional adapters for libraries a game already has (Runtime/MqttNet, Runtime/SteamworksNet,
// Runtime/Facepunch) log, register and run on the main thread the same way as the GDK.
[assembly: InternalsVisibleTo("LongJump.Gdk.MqttNet")]
[assembly: InternalsVisibleTo("LongJump.Gdk.SteamworksNet")]
[assembly: InternalsVisibleTo("LongJump.Gdk.Facepunch")]
