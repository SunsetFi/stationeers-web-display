
using System.IO;
using StationeersMods.Interface;
using StationeersWebDisplay.Cef;

namespace StationeersWebDisplay
{
    public class StationeersWebDispay : ModBehaviour
    {
        public static StationeersWebDispay Instance;

        public static string AssemblyDirectory
        {
            get
            {
                var assemblyLocation = typeof(StationeersWebDispay).Assembly.Location;
                var assemblyDir = Path.GetDirectoryName(assemblyLocation);
                return assemblyDir;
            }
        }

        void Awake()
        {
            // Test code for diagnosing assembly load failures.
            // AppDomain.CurrentDomain.AssemblyResolve += (_, e) =>
            // {
            //     Logging.LogTrace($"Assembly resolve for {e.Name} from {e.RequestingAssembly.FullName}");
            //     throw new Exception("Last ditch assembly resolve failed.");
            // };

            StationeersWebDispay.Instance = this;
        }

        public override void OnLoaded(ContentHandler contentHandler)
        {
            CefHost.Initialize();
        }
    }
}