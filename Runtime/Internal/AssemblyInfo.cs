using System.Runtime.CompilerServices;

// The optional MQTTnet adapter (Runtime/MqttNet) logs and times out the same way as the GDK.
[assembly: InternalsVisibleTo("LongJump.Gdk.MqttNet")]
