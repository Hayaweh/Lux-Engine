using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpVk;
using SharpVk.Interop;
using static SharpVk.QueueFlags;
using ApplicationInfo = SharpVk.ApplicationInfo;
using DeviceQueueCreateInfo = SharpVk.DeviceQueueCreateInfo;
using Instance = SharpVk.Instance;
using InstanceCreateInfo = SharpVk.InstanceCreateInfo;
using PhysicalDevice = SharpVk.PhysicalDevice;

namespace Lux.Graphics
{
    public struct QueueFamilyIndices
    {
        public int GraphicsFamily;

        public QueueFamilyIndices(int graphicsFamily = -1)
        {
            GraphicsFamily = graphicsFamily;
        }

        private bool IsComplete()
        {
            return GraphicsFamily >= 0;
        }
    }

    public class GraphicsEngine
    {
        private bool m_enableValidationLayers = false;
        private Instance m_vkInstance = null;
        private static readonly SharpVk.Interop.DebugReportCallbackDelegate m_debugReportDelegate = DebugReport;
        private SharpVk.PhysicalDevice m_vkPhysicalDevice = null;

        private bool m_isRunning = false;

        public async void Run()
        {
            InitVulkan();
            m_isRunning = true;
            MainLoop();
        }

        public void Stop()
        {
            m_isRunning = false;
        }

        private void InitVulkan()
        {
            CreateInstance();
            SetupDebugCallback();
            SelectPhysicalDevice();
            CreateLogicalDevice();
        }

        private async void MainLoop()
        {
            while (m_isRunning)
            {
                await Task.Delay(1);
            }
        }

        private void CreateInstance()
        {
            ApplicationInfo vkAppInfo = new ApplicationInfo
            {
                EngineName = "Lux Engine",
                ApplicationName = "",
                ApplicationVersion = new SharpVk.Version(1, 0, 0),
                ApiVersion = new SharpVk.Version(1, 0, 0),
                EngineVersion = new SharpVk.Version(1, 0, 0),
            };

            List<string> instanceExtensions = new List<string>()
            {
                KhrSurface.ExtensionName,
                KhrWin32Surface.ExtensionName,
            };

#if DEBUG
            m_enableValidationLayers = true;

#else
            EnableValidationLayers = false;
#endif
            List<string> validationLayers = new List<string>();
            if (m_enableValidationLayers)
            {
                if (Instance.EnumerateLayerProperties().Any(x => x.LayerName == "VK_LAYER_LUNARG_standard_validation"))
                {
                    validationLayers.Add("VK_LAYER_LUNARG_standard_validation");
                    instanceExtensions.Add(ExtDebugReport.ExtensionName);
                }
                else
                {
                    throw new Exception("Requested Vulkan Validation Layer is unavailable: VK_LAYER_LUNARG_standard_validation");
                }
            }

            InstanceCreateInfo vkInstanceInfo = new InstanceCreateInfo()
            {
                ApplicationInfo = vkAppInfo,
                EnabledExtensionNames = instanceExtensions.ToArray(),
                EnabledLayerNames = validationLayers.ToArray(),
            };

            m_vkInstance = Instance.Create(vkInstanceInfo, null);

            if (m_vkInstance.Equals(null))
            {
                throw new Exception("Vulkan: Instance Creation failed! :(");
            }

            Console.WriteLine("Vulkan: Successfully Created Vulkan Instance.");
        }

        private void SelectPhysicalDevice()
        {
            PhysicalDevice[] devices = m_vkInstance.EnumeratePhysicalDevices();

            if (devices.Length <= 0)
            {
                throw new IOException("Vulkan: No devices (GPU) with Vulkan support has been found.");
            }

            m_vkPhysicalDevice = devices.First(IsDeviceSuitable);

            if (m_vkPhysicalDevice.Equals(null))
            {
                throw new IOException("Vulkan: Although some devices (GPU) has been found. None passed the suitability test.");
            }
            Console.WriteLine("Vulkan: Successfully selected a physical device.");
        }

        private void CreateLogicalDevice()
        {
            //TODO: That's where we are at. QueueFamilyIndices is something I don't find. Halp. Just dumb. It's a struct I must create lololol.
            //QueueFamilyIndices queueFamilyIndices = Find
        }

        private bool IsDeviceSuitable(PhysicalDevice physicalDevice)
        {
            var properties = physicalDevice.GetProperties();
            var features = physicalDevice.GetFeatures();

            Console.WriteLine("Vulkan: Device Found: " + properties.DeviceName);

            var queueFamilies = physicalDevice.GetQueueFamilyProperties();

            bool hasGraphicsQueue = false;

            foreach (QueueFamilyProperties queueFamily in queueFamilies)
            {
                if ((queueFamily.QueueCount > 0) && ((queueFamily.QueueFlags & QueueFlags.Graphics) == QueueFlags.Graphics))
                {
                    hasGraphicsQueue = true;
                }
            }

            return (properties.DeviceType.Equals(PhysicalDeviceType.DiscreteGpu) || properties.DeviceType.Equals(PhysicalDeviceType.VirtualGpu)) && features.GeometryShader && hasGraphicsQueue;
        }

        private void SetupDebugCallback()
        {
            if (!m_enableValidationLayers)
                return;
            m_vkInstance.CreateDebugReportCallback(new SharpVk.DebugReportCallbackCreateInfo()
            {
                Flags = DebugReportFlags.Error | DebugReportFlags.Warning | DebugReportFlags.PerformanceWarning,
                PfnCallback = m_debugReportDelegate
            });

            Console.WriteLine("Vulkan: Debug Report set up.");
        }

        private static Bool32 DebugReport(DebugReportFlags flags, DebugReportObjectType objectType, ulong o, Size location, int messageCode, string layerPrefix, string message, IntPtr userData)
        {
            Console.WriteLine("Vulkan Code: " + messageCode + " - " + message);
            return false;
        }
    }
}