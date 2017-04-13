using Lux.Core;
using SharpVk;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lux.Graphics
{
    public struct QueueFamilyIndices
    {
        public uint? GraphicsFamily;
        public uint? PresentFamily;

        public IEnumerable<uint> Indices
        {
            get
            {
                if (this.GraphicsFamily.HasValue)
                {
                    yield return this.GraphicsFamily.Value;
                }

                if (this.PresentFamily.HasValue && this.PresentFamily != this.GraphicsFamily)
                {
                    yield return this.PresentFamily.Value;
                }
            }
        }

        public bool IsComplete
        {
            get
            {
                return this.GraphicsFamily.HasValue
                    && this.PresentFamily.HasValue;
            }
        }
    }

    public struct SwapChainSupportDetails
    {
        public SurfaceCapabilities Capabilities;
        public SurfaceFormat[] Formats;
        public PresentMode[] PresentModes;
    }

    public class GraphicsEngine
    {
        private IntPtr m_windowHandle;
        private bool m_usingValidationLayer;
        private string m_applicationName;
        private readonly Logger m_logger;

        private Instance m_vkInstance;

        public GraphicsEngine()
        {
            m_logger = new Logger("VulkanLog");
        }

        public void Run(IntPtr windowHandle, bool usingValidationLayer, string appName = "Vulkan Application")
        {
            m_usingValidationLayer = usingValidationLayer;
            m_windowHandle = windowHandle;
            m_applicationName = appName;
            Initialize();
        }

        public void RecreateSwapChain()
        {
        }

        public void TearDown()
        {
        }

        public void DrawFrame()
        {
        }

        private void Initialize()
        {
            CreateInstance();
        }

        private void CreateInstance()
        {
            ApplicationInfo applicationInfo = VkCreator.CreateApplicationInfo("Lux Graphics", default(SharpVk.Version), m_applicationName);

            List<string> extensions = new List<string>()
            {
                KhrSurface.ExtensionName,
                KhrWin32Surface.ExtensionName,
            };

            CheckInstanceExtensions(extensions);

            List<string> validationLayers = new List<string>();
            m_logger.WriteLine("{0} validation layers", m_usingValidationLayer ? "Using" : "Not using");

            if (m_usingValidationLayer)
            {
                validationLayers.Add("VK_LAYER_LUNARG_standard_validation");

                m_logger.WriteLine("Requested Instance validation layers are:");

                validationLayers.ForEach(m_logger.WriteLine);

                List<LayerProperties> layerProperties = Instance.EnumerateLayerProperties().ToList();

                m_logger.WriteLine("Available Instance validation layers are:");

                layerProperties.Select(layer => layer.LayerName).ToList().ForEach(m_logger.WriteLine);

                if (validationLayers.Except(layerProperties.Select(layer => layer.LayerName)).Any())
                {
                    m_logger.WriteLine("The following requested validation layers are not available:");

                    validationLayers.Except(layerProperties.Select(layer => layer.LayerName)).ToList().ForEach(m_logger.WriteLine);

                    throw new Exception("Instance validation layers not supported!");
                }

                m_logger.WriteLine("All requested Instance validation layers have been found and are supported.");
            }

            InstanceCreateInfo instanceCreateInfo = VkCreator.CreateInstanceCreateInfo(applicationInfo, extensions.ToArray(), validationLayers.ToArray());

            m_vkInstance = Instance.Create(instanceCreateInfo);

            m_logger.WriteLine("Instance created for application \"{0}\"", m_applicationName);
        }

        private void CheckInstanceExtensions(List<string> extensions)
        {
            m_logger.WriteLine("Requested Instance extensions are:");

            extensions.ForEach(m_logger.WriteLine);

            List<ExtensionProperties> extensionProperties = Instance.EnumerateExtensionProperties(null).ToList();

            m_logger.WriteLine("Instance available extensions are:");

            extensionProperties.Select(extension => extension.ExtensionName).ToList().ForEach(m_logger.WriteLine);

            if (extensions.Except(extensionProperties.Select(extensionProperty => extensionProperty.ExtensionName)).Any())
            {
                m_logger.WriteLine("The Following requested extensions are not available:");

                extensions.Except(extensionProperties.Select(extensionProperty => extensionProperty.ExtensionName)).ToList().ForEach(m_logger.WriteLine);

                throw new Exception("Instance extensions not supported!");
            }
            m_logger.WriteLine("All requested Instance extensions have been found and are supported.");
        }
    }
}