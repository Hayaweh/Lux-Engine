using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SharpVk;
using SharpVk.Interop;
using ApplicationInfo = SharpVk.ApplicationInfo;
using Buffer = System.Buffer;
using CommandBuffer = SharpVk.CommandBuffer;
using DebugReportCallbackCreateInfo = SharpVk.DebugReportCallbackCreateInfo;
using Device = SharpVk.Device;
using DeviceCreateInfo = SharpVk.DeviceCreateInfo;
using DeviceQueueCreateInfo = SharpVk.DeviceQueueCreateInfo;
using ExtensionProperties = SharpVk.ExtensionProperties;
using Framebuffer = SharpVk.Framebuffer;
using GraphicsPipelineCreateInfo = SharpVk.GraphicsPipelineCreateInfo;
using Image = SharpVk.Image;
using ImageView = SharpVk.ImageView;
using ImageViewCreateInfo = SharpVk.ImageViewCreateInfo;
using Instance = SharpVk.Instance;
using InstanceCreateInfo = SharpVk.InstanceCreateInfo;
using PhysicalDevice = SharpVk.PhysicalDevice;
using PipelineShaderStageCreateInfo = SharpVk.PipelineShaderStageCreateInfo;
using Queue = SharpVk.Queue;
using RenderPass = SharpVk.RenderPass;
using ShaderModule = SharpVk.ShaderModule;
using Surface = SharpVk.Surface;
using Swapchain = SharpVk.Swapchain;
using SwapchainCreateInfo = SharpVk.SwapchainCreateInfo;
using Version = SharpVk.Version;
using Win32SurfaceCreateInfo = SharpVk.Win32SurfaceCreateInfo;

namespace Lux.Graphics
{
    public struct QueueFamilyIndices
    {
        public int GraphicsFamily;
        public int PresentationFamily;

        public QueueFamilyIndices(int graphicsFamily = -1, int presentationFamily = -1)
        {
            GraphicsFamily = graphicsFamily;
            PresentationFamily = presentationFamily;
        }

        public bool IsComplete()
        {
            return GraphicsFamily >= 0 && PresentationFamily >= 0;
        }
    }

    public struct SwapChainSupportDetails
    {
        public SurfaceCapabilities Capabilities;
        public List<SurfaceFormat> Formats;
        public List<PresentMode> PresentModes;
    }

    public class GraphicsEngine
    {
        private bool m_enableValidationLayers = false;
        private List<string> m_validationLayers = new List<string>();
        private Instance m_vkInstance = null;
        private IntPtr m_windowHandle;
        private Surface m_surface = null;
        private static readonly DebugReportCallbackDelegate m_debugReportDelegate = DebugReport;
        private PhysicalDevice m_vkPhysicalDevice = null;

        private readonly List<string> m_deviceExtensions = new List<string>()
            {
                KhrSwapchain.ExtensionName
            };

        private Device m_logicalDevice = null;
        private Queue m_graphicsQueue = null;
        private Queue m_presentationQueue = null;
        private Swapchain m_swapchain = null;

        private List<Image> m_swapChainImages = new List<Image>();
        private Format m_swapChainImageFormat;
        private Extent2D m_swapChainExtent2D;
        private List<ImageView> m_swapChainImageViews;

        private ShaderModule m_vertexShader, m_fragmentShader;
        private SharpVk.PipelineLayout m_pipelineLayout;
        private RenderPass m_renderPass = null;
        private SharpVk.Pipeline[] m_graphicsPipeline;

        private List<SharpVk.Framebuffer> m_swapChainFramebuffers = new List<Framebuffer>();

        private SharpVk.CommandPool m_commandPool;
        private List<SharpVk.CommandBuffer> m_commandBuffers;

        private bool m_isRunning;

        public void Run(IntPtr windowHandle)
        {
            m_windowHandle = windowHandle;
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
            CreateSurface();
            SelectPhysicalDevice();
            CreateLogicalDevice();
            CreateSwapChain();
            CreateImageViews();
            CreateGraphicsPipeline();
            CreateFramebuffers();
            CreateCommandPool();
            CreateCommandBuffers();
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
                ApplicationVersion = new Version(1, 0, 0),
                ApiVersion = new Version(1, 0, 0),
                EngineVersion = new Version(1, 0, 0)
            };

            List<string> instanceExtensions = new List<string>
            {
                KhrSurface.ExtensionName,
                KhrWin32Surface.ExtensionName
            };

#if DEBUG
            m_enableValidationLayers = true;

#else
            EnableValidationLayers = false;
#endif
            m_validationLayers = new List<string>();
            if (m_enableValidationLayers)
            {
                if (Instance.EnumerateLayerProperties().Any(x => x.LayerName == "VK_LAYER_LUNARG_standard_validation"))
                {
                    m_validationLayers.Add("VK_LAYER_LUNARG_standard_validation");
                    instanceExtensions.Add(ExtDebugReport.ExtensionName);
                }
                else
                {
                    throw new Exception("Requested Vulkan Validation Layer is unavailable: VK_LAYER_LUNARG_standard_validation");
                }
            }

            InstanceCreateInfo vkInstanceInfo = new InstanceCreateInfo
            {
                ApplicationInfo = vkAppInfo,
                EnabledExtensionNames = instanceExtensions.ToArray(),
                EnabledLayerNames = m_validationLayers.ToArray()
            };

            m_vkInstance = Instance.Create(vkInstanceInfo, null);

            if (m_vkInstance.Equals(null))
            {
                throw new Exception("Vulkan: Instance Creation failed! :(");
            }

            Console.WriteLine("Vulkan: Successfully Created Vulkan Instance.");
        }

        private void CreateSurface()
        {
            Win32SurfaceCreateInfo surfaceCreateInfo = new Win32SurfaceCreateInfo()
            {
                Hwnd = m_windowHandle
            };

            m_surface = m_vkInstance.CreateWin32Surface(surfaceCreateInfo);

            if (m_surface.Equals(null))
            {
                Console.WriteLine("Vulkan: Could not create surface.");
            }
            else
            {
                Console.WriteLine("Vulkan: Successfully created surface on Handle {0}", m_windowHandle);
            }
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
            QueueFamilyIndices indices = FindQueueFamilies(m_vkPhysicalDevice);

            List<DeviceQueueCreateInfo> queuesCreateInfos = new List<DeviceQueueCreateInfo>();
            List<int> uniqueQueueFamilies = new List<int>()
            {
                indices.GraphicsFamily,
                indices.PresentationFamily
            };

            foreach (int queueFamily in uniqueQueueFamilies)
            {
                queuesCreateInfos.Add(new DeviceQueueCreateInfo
                {
                    QueueFamilyIndex = (uint)queueFamily,
                    QueuePriorities = new[] { 1.0f }
                });
            }

            PhysicalDeviceFeatures deviceFeatures = new PhysicalDeviceFeatures();

            DeviceCreateInfo deviceCreateInfo = new DeviceCreateInfo
            {
                QueueCreateInfos = queuesCreateInfos.ToArray(),
                EnabledFeatures = deviceFeatures,
                EnabledExtensionNames = m_deviceExtensions.ToArray(),
                EnabledLayerNames = m_validationLayers.ToArray()
            };

            m_logicalDevice = m_vkPhysicalDevice.CreateDevice(deviceCreateInfo);

            m_graphicsQueue = m_logicalDevice.GetQueue((uint)indices.GraphicsFamily, 0);
            m_presentationQueue = m_logicalDevice.GetQueue((uint)indices.PresentationFamily, 0);
        }

        private void CreateSwapChain()
        {
            SwapChainSupportDetails swapChainDetails = QuerySwapChainSupport(m_vkPhysicalDevice);

            SurfaceFormat surfaceFormat = ChooseSwapSurfaceFormat(swapChainDetails.Formats);
            PresentMode surfacePresentMode = ChooseSwapPresentMode(swapChainDetails.PresentModes);
            Extent2D surfacExtent2D = ChooseSwapExtent(swapChainDetails.Capabilities);

            uint imageCount = swapChainDetails.Capabilities.MinImageCount + 1;
            if (swapChainDetails.Capabilities.MaxImageCount > 0 && imageCount > swapChainDetails.Capabilities.MaxImageCount)
            {
                imageCount = swapChainDetails.Capabilities.MaxImageCount;
            }

            QueueFamilyIndices indices = FindQueueFamilies(m_vkPhysicalDevice);
            List<uint> queueFamilyIndices = new List<uint>()
            {
                (uint)indices.GraphicsFamily,
                (uint)indices.PresentationFamily
            };

            m_swapChainImageFormat = surfaceFormat.Format;
            m_swapChainExtent2D = surfacExtent2D;

            SwapchainCreateInfo swapchainCreateInfo = new SwapchainCreateInfo()
            {
                Surface = m_surface,
                MinImageCount = imageCount,
                ImageFormat = surfaceFormat.Format,
                ImageColorSpace = surfaceFormat.ColorSpace,
                ImageExtent = surfacExtent2D,
                ImageArrayLayers = 1,
                ImageUsage = ImageUsageFlags.ColorAttachment,
                PreTransform = swapChainDetails.Capabilities.CurrentTransform,
                CompositeAlpha = CompositeAlphaFlags.Opaque,
                PresentMode = surfacePresentMode,
                Clipped = true,
                OldSwapchain = null
            };

            if (indices.GraphicsFamily != indices.PresentationFamily)
            {
                swapchainCreateInfo.ImageSharingMode = SharingMode.Concurrent;
                swapchainCreateInfo.QueueFamilyIndices = queueFamilyIndices.ToArray();
            }
            else
            {
                swapchainCreateInfo.ImageSharingMode = SharingMode.Exclusive;
                swapchainCreateInfo.QueueFamilyIndices = null;
            }

            m_swapchain = m_logicalDevice.CreateSwapchain(swapchainCreateInfo);

            m_swapChainImages.AddRange(m_swapchain.GetImages());
        }

        private void CreateImageViews()
        {
            m_swapChainImageViews = new List<ImageView>(m_swapChainImages.Count);

            foreach (Image image in m_swapChainImages)
            {
                ImageViewCreateInfo imageViewCreateInfo = new ImageViewCreateInfo()
                {
                    Image = image,
                    ViewType = ImageViewType.ImageView2d,
                    Format = m_swapChainImageFormat,
                    Components = ComponentMapping.Identity,
                    SubresourceRange = new ImageSubresourceRange()
                    {
                        AspectMask = ImageAspectFlags.Color,
                        BaseMipLevel = 0,
                        LevelCount = 1,
                        BaseArrayLayer = 0,
                        LayerCount = 1
                    }
                };

                m_swapChainImageViews.Add(m_logicalDevice.CreateImageView(imageViewCreateInfo));
            }
        }

        private void CreateGraphicsPipeline()
        {
            int vertexSize, fragmentSize = 0;
            uint[] vertexShaderData = LoadShaderData(@"./Shaders/vertex.vert.spv", out vertexSize);
            uint[] fragmentShaderData = LoadShaderData(@"./Shaders/fragment.frag.spv", out fragmentSize);

            m_vertexShader = m_logicalDevice.CreateShaderModule(new SharpVk.ShaderModuleCreateInfo()
            {
                Code = vertexShaderData,
                CodeSize = vertexSize
            });

            m_fragmentShader = m_logicalDevice.CreateShaderModule(new SharpVk.ShaderModuleCreateInfo()
            {
                Code = fragmentShaderData,
                CodeSize = fragmentSize
            });

            PipelineShaderStageCreateInfo vertexShaderStageCreateInfo = new PipelineShaderStageCreateInfo()
            {
                Module = m_vertexShader,
                Name = "main",
                Stage = ShaderStageFlags.Vertex
            };

            PipelineShaderStageCreateInfo fragmentShaderStageCreateInfo = new PipelineShaderStageCreateInfo()
            {
                Module = m_fragmentShader,
                Name = "main",
                Stage = ShaderStageFlags.Fragment
            };

            PipelineShaderStageCreateInfo[] shaderStages = { vertexShaderStageCreateInfo, fragmentShaderStageCreateInfo };

            SharpVk.PipelineVertexInputStateCreateInfo pipelineVertexInputStateCreateInfo = new SharpVk.PipelineVertexInputStateCreateInfo()
            {
                VertexAttributeDescriptions = null,
                VertexBindingDescriptions = null
            };

            SharpVk.PipelineInputAssemblyStateCreateInfo pipelineInputAssemblyStateCreateInfo = new SharpVk.PipelineInputAssemblyStateCreateInfo()
            {
                PrimitiveRestartEnable = false,
                Topology = PrimitiveTopology.TriangleList
            };

            Viewport viewport = new Viewport()
            {
                X = 0.0f,
                Y = 0.0f,
                Width = m_swapChainExtent2D.Width,
                Height = m_swapChainExtent2D.Height,
                MaxDepth = 1.0f,
                MinDepth = 0.0f
            };

            Rect2D scissoRect2D = new Rect2D(new Offset2D(0, 0), m_swapChainExtent2D);

            SharpVk.PipelineViewportStateCreateInfo pipelineViewportStateCreateInfo = new SharpVk.PipelineViewportStateCreateInfo()
            {
                Viewports = new[] { viewport },
                Scissors = new[] { scissoRect2D }
            };

            SharpVk.PipelineRasterizationStateCreateInfo pipelineRasterizationStateCreateInfo = new SharpVk.PipelineRasterizationStateCreateInfo()
            {
                DepthClampEnable = false,
                RasterizerDiscardEnable = false,
                PolygonMode = PolygonMode.Fill,
                LineWidth = 1.0f,
                CullMode = CullModeFlags.Back,
                FrontFace = FrontFace.Clockwise,
                DepthBiasEnable = false,
                DepthBiasConstantFactor = 0.0f,
                DepthBiasClamp = 0.0f,
                DepthBiasSlopeFactor = 0.0f
            };

            SharpVk.PipelineMultisampleStateCreateInfo pipelineMultisampleStateCreateInfo = new SharpVk.PipelineMultisampleStateCreateInfo()
            {
                SampleShadingEnable = false,
                RasterizationSamples = SampleCountFlags.SampleCount1,
                MinSampleShading = 1.0f,
                SampleMask = null,
                AlphaToCoverageEnable = false,
                AlphaToOneEnable = false
            };

            SharpVk.PipelineDepthStencilStateCreateInfo pipelineDepthStencilStateCreateInfo = new SharpVk.PipelineDepthStencilStateCreateInfo();

            PipelineColorBlendAttachmentState pipelineColorBlendAttachmentState = new PipelineColorBlendAttachmentState()
            {
                ColorWriteMask = ColorComponentFlags.R | ColorComponentFlags.G | ColorComponentFlags.B | ColorComponentFlags.A,
                BlendEnable = false,
                SourceColorBlendFactor = BlendFactor.One,
                DestinationColorBlendFactor = BlendFactor.Zero,
                ColorBlendOp = BlendOp.Add,
                SourceAlphaBlendFactor = BlendFactor.One,
                DestinationAlphaBlendFactor = BlendFactor.Zero,
                AlphaBlendOp = BlendOp.Add
            };

            SharpVk.PipelineColorBlendStateCreateInfo pipelineColorBlendStateCreateInfo = new SharpVk.PipelineColorBlendStateCreateInfo()
            {
                LogicOpEnable = false,
                LogicOp = LogicOp.Copy,
                Attachments = new[] { pipelineColorBlendAttachmentState },
                BlendConstants = new[] { 0.0f, 0.0f, 0.0f, 0.0f }
            };

            SharpVk.PipelineDynamicStateCreateInfo pipelineDynamicStateCreateInfo = new SharpVk.PipelineDynamicStateCreateInfo()
            {
                DynamicStates = new[] { DynamicState.LineWidth, DynamicState.Viewport }
            };

            m_pipelineLayout = m_logicalDevice.CreatePipelineLayout(new SharpVk.PipelineLayoutCreateInfo());
            Console.WriteLine(m_pipelineLayout.Equals(null) ? "Vulkan: Failed to create pipeline layout." : "Vulkan: Successfully created pipeline layout.");

            CreateRenderPass();

            SharpVk.GraphicsPipelineCreateInfo graphicsPipelineCreateInfo = new SharpVk.GraphicsPipelineCreateInfo()
            {
                Stages = shaderStages,
                VertexInputState = pipelineVertexInputStateCreateInfo,
                InputAssemblyState = pipelineInputAssemblyStateCreateInfo,
                ViewportState = pipelineViewportStateCreateInfo,
                RasterizationState = pipelineRasterizationStateCreateInfo,
                MultisampleState = pipelineMultisampleStateCreateInfo,
                DepthStencilState = pipelineDepthStencilStateCreateInfo,
                ColorBlendState = pipelineColorBlendStateCreateInfo,
                DynamicState = pipelineDynamicStateCreateInfo,
                Layout = m_pipelineLayout,
                Subpass = 0,
                RenderPass = m_renderPass,
                BasePipelineIndex = -1,
                BasePipelineHandle = null
            };

            m_graphicsPipeline = m_logicalDevice.CreateGraphicsPipelines(null, new[] { graphicsPipelineCreateInfo });
            Console.WriteLine(m_graphicsPipeline.Equals(null) ? "Vulkan: Failed to create graphics pipeline." : "Vulkan: Successfully created graphics pipeline.");
        }

        private void CreateRenderPass()
        {
            AttachmentDescription colorAttachmentDescription = new AttachmentDescription()
            {
                Format = m_swapChainImageFormat,
                Samples = SampleCountFlags.SampleCount1,
                LoadOp = AttachmentLoadOp.Clear,
                StoreOp = AttachmentStoreOp.Store,
                StencilLoadOp = AttachmentLoadOp.DontCare,
                StencilStoreOp = AttachmentStoreOp.DontCare,
                InitialLayout = ImageLayout.Undefined,
                FinalLayout = ImageLayout.PresentSource
            };

            AttachmentReference attachmentReference = new AttachmentReference()
            {
                Attachment = 0,
                Layout = ImageLayout.ColorAttachmentOptimal
            };

            SharpVk.SubpassDescription subpassDescription = new SharpVk.SubpassDescription()
            {
                PipelineBindPoint = PipelineBindPoint.Graphics,
                ColorAttachments = new[] { attachmentReference },
            };

            m_renderPass = m_logicalDevice.CreateRenderPass(new SharpVk.RenderPassCreateInfo()
            {
                Attachments = new[] { colorAttachmentDescription },
                Subpasses = new[] { subpassDescription }
            });

            Console.WriteLine(m_renderPass.Equals(null) ? "Vulkan: Failed to create render pass." : "Vulkan: Successfully created render pass.");
        }

        private void CreateFramebuffers()
        {
            m_swapChainFramebuffers.Clear();

            foreach (ImageView swapChainImageView in m_swapChainImageViews)
            {
                SharpVk.FramebufferCreateInfo framebufferCreateInfo = new SharpVk.FramebufferCreateInfo()
                {
                    RenderPass = m_renderPass,
                    Attachments = new[] { swapChainImageView },
                    Width = m_swapChainExtent2D.Width,
                    Height = m_swapChainExtent2D.Height,
                    Layers = 1
                };

                Framebuffer framebuffer = m_logicalDevice.CreateFramebuffer(framebufferCreateInfo);

                if (!framebuffer.Equals(null))
                {
                    m_swapChainFramebuffers.Add(framebuffer);
                    Console.WriteLine("Vulkan: Successfully created Framebuffer");
                }
                else
                {
                    Console.WriteLine("Vulkan: Failed to create Framebuffer");
                }
            }
        }

        private void CreateCommandPool()
        {
            QueueFamilyIndices indices = FindQueueFamilies(m_vkPhysicalDevice);

            SharpVk.CommandPoolCreateInfo commandPoolCreateInfo = new SharpVk.CommandPoolCreateInfo()
            {
                QueueFamilyIndex = (uint)indices.GraphicsFamily
            };

            m_commandPool = m_logicalDevice.CreateCommandPool(commandPoolCreateInfo);

            if (m_commandPool.Equals(null))
            {
                throw new Exception("Vulkan: Initialization of Command Pool failed.");
            }
            Console.WriteLine("Vulkan: Successfully created Command Pool");
        }

        private void CreateCommandBuffers()
        {
            m_commandBuffers = new List<CommandBuffer>();

            SharpVk.CommandBufferAllocateInfo commandBufferAllocateInfo = new SharpVk.CommandBufferAllocateInfo()
            {
                CommandPool = m_commandPool,
                Level = CommandBufferLevel.Primary,
                CommandBufferCount = (uint)m_swapChainFramebuffers.Count
            };

            m_commandBuffers.Clear();
            m_commandBuffers.AddRange(m_logicalDevice.AllocateCommandBuffers(commandBufferAllocateInfo));

            if (m_commandBuffers.Count < 1)
            {
                throw new Exception("Vulkan: No Command Buffers initialized");
            }
            Console.WriteLine("Vulkan: Command Buffers successfully created");
        }

        private static uint[] LoadShaderData(string filePath, out int codeSize)
        {
            if (File.Exists(filePath))
            {
                var fileBytes = File.ReadAllBytes(filePath);
                var shaderData = new uint[(int)Math.Ceiling(fileBytes.Length / 4f)];

                Buffer.BlockCopy(fileBytes, 0, shaderData, 0, fileBytes.Length);

                codeSize = fileBytes.Length;

                return shaderData;
            }
            else
            {
                Console.WriteLine("Vulkan: Could not find Shader file at: {0}", filePath);
                codeSize = 0;
                return new uint[0];
            }
        }

        private QueueFamilyIndices FindQueueFamilies(PhysicalDevice device)
        {
            QueueFamilyIndices indices = new QueueFamilyIndices(-1);

            QueueFamilyProperties[] families = device.GetQueueFamilyProperties();

            var queueFamilies = device.GetQueueFamilyProperties();

            int i = 0;
            foreach (QueueFamilyProperties queueFamily in queueFamilies)
            {
                if ((queueFamily.QueueCount > 0) && ((queueFamily.QueueFlags & QueueFlags.Graphics) == QueueFlags.Graphics))
                {
                    indices.GraphicsFamily = i;
                }

                if ((queueFamily.QueueCount > 0) && device.GetWin32PresentationSupport((uint)i))
                {
                    indices.PresentationFamily = i;
                }

                if (indices.IsComplete())
                {
                    break;
                }

                i++;
            }

            return indices;
        }

        private bool IsDeviceSuitable(PhysicalDevice physicalDevice)
        {
            var properties = physicalDevice.GetProperties();
            var features = physicalDevice.GetFeatures();

            Console.WriteLine("Vulkan: Device Found: " + properties.DeviceName);

            QueueFamilyIndices indices = FindQueueFamilies(physicalDevice);

            if (!(properties.DeviceType.Equals(PhysicalDeviceType.DiscreteGpu) || properties.DeviceType.Equals(PhysicalDeviceType.VirtualGpu)))
            {
                return false;
            }

            if (!features.GeometryShader)
            {
                return false;
            }

            if (!indices.IsComplete())
            {
                return false;
            }

            if (!CheckDeviceExtensionsSupport(physicalDevice))
            {
                return false;
            }

            SwapChainSupportDetails swapChainSupportDetails = QuerySwapChainSupport(physicalDevice);

            if (swapChainSupportDetails.Formats.Count <= 0 || swapChainSupportDetails.PresentModes.Count <= 0)
            {
                return false;
            }

            return true;
        }

        private bool CheckDeviceExtensionsSupport(PhysicalDevice device)
        {
            ExtensionProperties[] extensions = device.EnumerateDeviceExtensionProperties(null);

            bool contained = false;
            foreach (string extension in m_deviceExtensions)
            {
                contained = false;

                foreach (ExtensionProperties extensionProperties in extensions)
                {
                    if (extensionProperties.ExtensionName.Equals(extension))
                        contained = true;
                }

                if (!contained)
                {
                    return false;
                }
            }

            return true;
        }

        private SwapChainSupportDetails QuerySwapChainSupport(PhysicalDevice device)
        {
            SwapChainSupportDetails details = new SwapChainSupportDetails();
            details.Capabilities = device.GetSurfaceCapabilities(m_surface);
            details.Formats = new List<SurfaceFormat>(device.GetSurfaceFormats(m_surface));
            details.PresentModes = new List<PresentMode>(device.GetSurfacePresentModes(m_surface));

            return details;
        }

        private SurfaceFormat ChooseSwapSurfaceFormat(List<SurfaceFormat> formats)
        {
            if (formats.Count == 1 && formats[0].Format == Format.Undefined)
            {
                return new SurfaceFormat(Format.B8G8R8A8UNorm, ColorSpace.SrgbNonlinear);
            }

            foreach (SurfaceFormat surfaceFormat in formats)
            {
                if (surfaceFormat.Format == Format.B8G8R8A8UNorm && surfaceFormat.ColorSpace == ColorSpace.SrgbNonlinear)
                {
                    return surfaceFormat;
                }
            }

            return formats[0];
        }

        private PresentMode ChooseSwapPresentMode(List<PresentMode> presentModes)
        {
            PresentMode bestMode = PresentMode.Fifo;

            foreach (PresentMode presentMode in presentModes)
            {
                if (presentMode == PresentMode.Mailbox)
                {
                    return presentMode;
                }
                else if (presentMode == PresentMode.Immediate)
                {
                    bestMode = presentMode;
                }
            }

            return bestMode;
        }

        private Extent2D ChooseSwapExtent(SurfaceCapabilities capabilities)
        {
            if (capabilities.CurrentExtent.Width != UInt32.MaxValue)
            {
                return capabilities.CurrentExtent;
            }
            else
            {
                Extent2D actualExtent2D = new Extent2D(UInt32.MaxValue, UInt32.MaxValue);

                actualExtent2D.Width = Math.Max(capabilities.MinImageExtent.Width, Math.Min(capabilities.MaxImageExtent.Width, actualExtent2D.Width));
                actualExtent2D.Height = Math.Max(capabilities.MinImageExtent.Height, Math.Min(capabilities.MaxImageExtent.Height, actualExtent2D.Height));

                return actualExtent2D;
            }
        }

        private void SetupDebugCallback()
        {
            if (!m_enableValidationLayers)
                return;
            m_vkInstance.CreateDebugReportCallback(new DebugReportCallbackCreateInfo
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