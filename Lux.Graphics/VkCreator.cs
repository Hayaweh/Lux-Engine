using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpVk;
using Version = SharpVk.Version;

namespace Lux.Graphics
{
    public static class VkCreator
    {
        public static ApplicationInfo CreateApplicationInfo(string engineName = "Vulkan Engine",
                                                            SharpVk.Version engineVersion = default(Version),
                                                            string applicationName = "Vulkan Application",
                                                            SharpVk.Version applicationVersion = default(Version),
                                                            SharpVk.Version apiVersion = default(Version))
        {
            return new ApplicationInfo()
            {
                ApplicationName = applicationName,
                EngineName = engineName,
                ApplicationVersion = applicationVersion,
                EngineVersion = engineVersion,
                ApiVersion = apiVersion
            };
        }

        public static InstanceCreateInfo CreateInstanceCreateInfo(ApplicationInfo applicationInfo,
                                                                  string[] enabledExtensionNames = null,
                                                                  string[] enabledLayerNames = null,
                                                                  InstanceCreateFlags flags = InstanceCreateFlags.None)
        {
            return new InstanceCreateInfo()
            {
                ApplicationInfo = applicationInfo,
                Flags = flags,
                EnabledExtensionNames = enabledExtensionNames,
                EnabledLayerNames = enabledLayerNames
            };
        }
    }
}