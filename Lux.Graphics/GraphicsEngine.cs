using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SharpVk;
using SharpVk.Interop;
using ApplicationInfo = SharpVk.ApplicationInfo;
using CommandBuffer = SharpVk.CommandBuffer;
using CommandPool = SharpVk.CommandPool;
using Device = SharpVk.Device;
using DeviceCreateInfo = SharpVk.DeviceCreateInfo;
using DeviceQueueCreateInfo = SharpVk.DeviceQueueCreateInfo;
using Framebuffer = SharpVk.Framebuffer;
using GraphicsPipelineCreateInfo = SharpVk.GraphicsPipelineCreateInfo;
using Image = SharpVk.Image;
using ImageView = SharpVk.ImageView;
using ImageViewCreateInfo = SharpVk.ImageViewCreateInfo;
using Instance = SharpVk.Instance;
using InstanceCreateInfo = SharpVk.InstanceCreateInfo;
using PhysicalDevice = SharpVk.PhysicalDevice;
using Pipeline = SharpVk.Pipeline;
using PipelineLayout = SharpVk.PipelineLayout;
using PipelineShaderStageCreateInfo = SharpVk.PipelineShaderStageCreateInfo;
using PresentInfo = SharpVk.PresentInfo;
using Queue = SharpVk.Queue;
using RenderPass = SharpVk.RenderPass;
using Semaphore = SharpVk.Semaphore;
using ShaderModule = SharpVk.ShaderModule;
using SubmitInfo = SharpVk.SubmitInfo;
using SubpassDescription = SharpVk.SubpassDescription;
using Surface = SharpVk.Surface;
using Swapchain = SharpVk.Swapchain;
using SwapchainCreateInfo = SharpVk.SwapchainCreateInfo;
using Version = SharpVk.Version;
using Win32SurfaceCreateInfo = SharpVk.Win32SurfaceCreateInfo;

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
        private Instance m_instance;
        private Surface m_surface;
        private PhysicalDevice m_physicalDevice;
        private Device m_device;
        private Queue m_graphicsQueue;
        private Queue m_presentQueue;
        private Swapchain m_swapChain;
        private Image[] m_swapChainImages;
        private ImageView[] m_swapChainImageViews;
        private RenderPass m_renderPass;
        private PipelineLayout m_pipelineLayout;
        private Pipeline m_pipeline;
        private ShaderModule m_fragShader;
        private ShaderModule m_vertShader;
        private Framebuffer[] m_frameBuffers;
        private CommandPool m_commandPool;
        private CommandBuffer[] m_commandBuffers;
        private Semaphore m_imageAvailableSemaphore;
        private Semaphore m_renderFinishedSemaphore;

        private Format m_swapChainFormat;
        private Extent2D m_swapChainExtent;
        private IntPtr m_windowHandle;

        public void Run(IntPtr windowHandle)
        {
            m_windowHandle = windowHandle;
            this.InitialiseVulkan();
        }

        private void InitialiseVulkan()
        {
            this.CreateInstance();
            this.CreateSurface();
            this.PickPhysicalDevice();
            this.CreateLogicalDevice();
            this.CreateSwapChain();
            this.CreateImageViews();
            this.CreateRenderPass();
            this.CreateShaderModules();
            this.CreateGraphicsPipeline();
            this.CreateFrameBuffers();
            this.CreateCommandPool();
            this.CreateCommandBuffers();
            this.CreateSemaphores();
        }

        public void RecreateSwapChain()
        {
            this.m_device.WaitIdle();

            this.m_commandPool.FreeCommandBuffers(m_commandBuffers);

            foreach (var frameBuffer in this.m_frameBuffers)
            {
                frameBuffer.Dispose();
            }
            this.m_frameBuffers = null;

            this.m_pipeline.Dispose();
            this.m_pipeline = null;

            this.m_pipelineLayout.Dispose();
            this.m_pipelineLayout = null;

            foreach (var imageView in this.m_swapChainImageViews)
            {
                imageView.Dispose();
            }
            this.m_swapChainImageViews = null;

            this.m_renderPass.Dispose();
            this.m_renderPass = null;

            this.m_swapChain.Dispose();
            this.m_swapChain = null;

            this.CreateSwapChain();
            this.CreateImageViews();
            this.CreateRenderPass();
            this.CreateGraphicsPipeline();
            this.CreateFrameBuffers();
            this.CreateCommandBuffers();
        }

        public void TearDown()
        {
            m_device.WaitIdle();

            this.m_renderFinishedSemaphore.Dispose();
            this.m_renderFinishedSemaphore = null;

            this.m_imageAvailableSemaphore.Dispose();
            this.m_imageAvailableSemaphore = null;

            this.m_commandPool.Dispose();
            this.m_commandPool = null;

            foreach (var frameBuffer in this.m_frameBuffers)
            {
                frameBuffer.Dispose();
            }
            this.m_frameBuffers = null;

            this.m_fragShader.Dispose();
            this.m_fragShader = null;

            this.m_vertShader.Dispose();
            this.m_vertShader = null;

            this.m_pipeline.Dispose();
            this.m_pipeline = null;

            this.m_pipelineLayout.Dispose();
            this.m_pipelineLayout = null;

            foreach (var imageView in this.m_swapChainImageViews)
            {
                imageView.Dispose();
            }
            this.m_swapChainImageViews = null;

            this.m_renderPass.Dispose();
            this.m_renderPass = null;

            this.m_swapChain.Dispose();
            this.m_swapChain = null;

            this.m_device.Dispose();
            this.m_device = null;

            this.m_surface.Dispose();
            this.m_surface = null;

            this.m_instance.Dispose();
            this.m_instance = null;
        }

        public void DrawFrame()
        {
            uint nextImage = this.m_swapChain.AcquireNextImage(uint.MaxValue, this.m_imageAvailableSemaphore, null);

            this.m_graphicsQueue.Submit(new SubmitInfo[]
                                        {
                                                new SubmitInfo
                                                {
                                                    CommandBuffers = new CommandBuffer[] { this.m_commandBuffers[nextImage] },
                                                    SignalSemaphores = new[] { this.m_renderFinishedSemaphore },
                                                    WaitDestinationStageMask = new [] { PipelineStageFlags.ColorAttachmentOutput },
                                                    WaitSemaphores = new [] { this.m_imageAvailableSemaphore }
                                                }
                                        }, null);

            this.m_presentQueue.Present(new PresentInfo
            {
                ImageIndices = new uint[] { nextImage },
                Results = new Result[1],
                WaitSemaphores = new[] { this.m_renderFinishedSemaphore },
                Swapchains = new[] { this.m_swapChain }
            });
        }

        private void CreateInstance()
        {
            this.m_instance = Instance.Create(new InstanceCreateInfo
            {
                ApplicationInfo = new ApplicationInfo
                {
                    ApplicationName = "Hello Triangle",
                    ApplicationVersion = new Version(1, 0, 0),
                    EngineName = "SharpVk",
                    EngineVersion = new Version(0, 1, 1)
                },
                EnabledExtensionNames = new[]
                {
                    KhrSurface.ExtensionName,
                    KhrWin32Surface.ExtensionName
                }
            }, null);
        }

        private void CreateSurface()
        {
            this.m_surface = this.m_instance.CreateWin32Surface(new Win32SurfaceCreateInfo
            {
                Hwnd = this.m_windowHandle
            });
        }

        private void PickPhysicalDevice()
        {
            var availableDevices = this.m_instance.EnumeratePhysicalDevices();

            this.m_physicalDevice = availableDevices.First(IsSuitableDevice);
        }

        private void CreateLogicalDevice()
        {
            QueueFamilyIndices queueFamilies = FindQueueFamilies(this.m_physicalDevice);

            this.m_device = m_physicalDevice.CreateDevice(new DeviceCreateInfo
            {
                QueueCreateInfos = queueFamilies.Indices
                                                .Select(index => new DeviceQueueCreateInfo
                                                {
                                                    QueueFamilyIndex = index,
                                                    QueuePriorities = new[] { 1f }
                                                }).ToArray(),
                EnabledExtensionNames = new[] { KhrSwapchain.ExtensionName }
            });

            this.m_graphicsQueue = this.m_device.GetQueue(queueFamilies.GraphicsFamily.Value, 0);
            this.m_presentQueue = this.m_device.GetQueue(queueFamilies.PresentFamily.Value, 0);
        }

        private void CreateSwapChain()
        {
            SwapChainSupportDetails swapChainSupport = this.QuerySwapChainSupport(this.m_physicalDevice);

            uint imageCount = swapChainSupport.Capabilities.MinImageCount + 1;
            if (swapChainSupport.Capabilities.MaxImageCount > 0 && imageCount > swapChainSupport.Capabilities.MaxImageCount)
            {
                imageCount = swapChainSupport.Capabilities.MaxImageCount;
            }

            SurfaceFormat surfaceFormat = this.ChooseSwapSurfaceFormat(swapChainSupport.Formats);

            QueueFamilyIndices queueFamilies = this.FindQueueFamilies(this.m_physicalDevice);

            var indices = queueFamilies.Indices.ToArray();

            Extent2D extent = this.ChooseSwapExtent(swapChainSupport.Capabilities);

            this.m_swapChain = m_device.CreateSwapchain(new SwapchainCreateInfo
            {
                Surface = m_surface,
                Flags = SwapchainCreateFlags.None,
                PresentMode = this.ChooseSwapPresentMode(swapChainSupport.PresentModes),
                MinImageCount = imageCount,
                ImageExtent = extent,
                ImageUsage = ImageUsageFlags.ColorAttachment,
                PreTransform = swapChainSupport.Capabilities.CurrentTransform,
                ImageArrayLayers = 1,
                ImageSharingMode = indices.Length == 1
                                    ? SharingMode.Exclusive
                                    : SharingMode.Concurrent,
                QueueFamilyIndices = indices,
                ImageFormat = surfaceFormat.Format,
                ImageColorSpace = surfaceFormat.ColorSpace,
                Clipped = true,
                CompositeAlpha = CompositeAlphaFlags.Opaque,
                OldSwapchain = this.m_swapChain
            });

            this.m_swapChainImages = this.m_swapChain.GetImages();
            this.m_swapChainFormat = surfaceFormat.Format;
            this.m_swapChainExtent = extent;
        }

        private void CreateImageViews()
        {
            this.m_swapChainImageViews = this.m_swapChainImages.Select(image => m_device.CreateImageView(new ImageViewCreateInfo
            {
                Components = ComponentMapping.Identity,
                Format = this.m_swapChainFormat,
                Image = image,
                Flags = ImageViewCreateFlags.None,
                ViewType = ImageViewType.ImageView2d,
                SubresourceRange = new ImageSubresourceRange
                {
                    AspectMask = ImageAspectFlags.Color,
                    BaseMipLevel = 0,
                    LevelCount = 1,
                    BaseArrayLayer = 0,
                    LayerCount = 1
                }
            })).ToArray();
        }

        private void CreateRenderPass()
        {
            this.m_renderPass = m_device.CreateRenderPass(new SharpVk.RenderPassCreateInfo
            {
                Attachments = new[]
                       {
                        new AttachmentDescription
                        {
                            Format = this.m_swapChainFormat,
                            Samples = SampleCountFlags.SampleCount1,
                            LoadOp = AttachmentLoadOp.Clear,
                            StoreOp = AttachmentStoreOp.Store,
                            StencilLoadOp = AttachmentLoadOp.DontCare,
                            StencilStoreOp = AttachmentStoreOp.DontCare,
                            InitialLayout = ImageLayout.Undefined,
                            FinalLayout = ImageLayout.PresentSource
                        },
                    },
                Subpasses = new[]
                       {
                        new SubpassDescription
                        {
                            DepthStencilAttachment = new AttachmentReference
                            {
                                Attachment = Constants.AttachmentUnused
                            },
                            PipelineBindPoint = PipelineBindPoint.Graphics,
                            ColorAttachments = new []
                            {
                                new AttachmentReference
                                {
                                    Attachment = 0,
                                    Layout = ImageLayout.ColorAttachmentOptimal
                                }
                            }
                        }
                    },
                Dependencies = new[]
                       {
                        new SubpassDependency
                        {
                            SourceSubpass = Constants.SubpassExternal,
                            DestinationSubpass = 0,
                            SourceStageMask = PipelineStageFlags.BottomOfPipe,
                            SourceAccessMask = AccessFlags.MemoryRead,
                            DestinationStageMask = PipelineStageFlags.ColorAttachmentOutput,
                            DestinationAccessMask = AccessFlags.ColorAttachmentRead | AccessFlags.ColorAttachmentWrite
                        },
                        new SubpassDependency
                        {
                            SourceSubpass = 0,
                            DestinationSubpass = Constants.SubpassExternal,
                            SourceStageMask = PipelineStageFlags.ColorAttachmentOutput,
                            SourceAccessMask = AccessFlags.ColorAttachmentRead | AccessFlags.ColorAttachmentWrite,
                            DestinationStageMask = PipelineStageFlags.BottomOfPipe,
                            DestinationAccessMask = AccessFlags.MemoryRead
                        }
                    }
            });
        }

        private void CreateShaderModules()
        {
            int codeSize;
            var vertShaderData = LoadShaderData(@".\Shaders\vert.spv", out codeSize);

            this.m_vertShader = m_device.CreateShaderModule(new SharpVk.ShaderModuleCreateInfo
            {
                Code = vertShaderData,
                CodeSize = codeSize
            });

            var fragShaderData = LoadShaderData(@".\Shaders\frag.spv", out codeSize);

            this.m_fragShader = m_device.CreateShaderModule(new SharpVk.ShaderModuleCreateInfo
            {
                Code = fragShaderData,
                CodeSize = codeSize
            });
        }

        private void CreateGraphicsPipeline()
        {
            this.m_pipelineLayout = m_device.CreatePipelineLayout(new SharpVk.PipelineLayoutCreateInfo());

            this.m_pipeline = m_device.CreateGraphicsPipelines(null, new[]
            {
                    new GraphicsPipelineCreateInfo
                    {
                        Layout = this.m_pipelineLayout,
                        RenderPass = this.m_renderPass,
                        Subpass = 0,
                        VertexInputState = new SharpVk.PipelineVertexInputStateCreateInfo(),
                        InputAssemblyState = new SharpVk.PipelineInputAssemblyStateCreateInfo
                        {
                            PrimitiveRestartEnable = false,
                            Topology = PrimitiveTopology.TriangleList
                        },
                        ViewportState = new SharpVk.PipelineViewportStateCreateInfo
                        {
                            Viewports = new[]
                            {
                                new Viewport
                                {
                                    X = 0f,
                                    Y = 0f,
                                    Width = this.m_swapChainExtent.Width,
                                    Height = this.m_swapChainExtent.Height,
                                    MaxDepth = 1,
                                    MinDepth = 0
                                }
                            },
                            Scissors = new[]
                            {
                                new Rect2D
                                {
                                    Offset = new Offset2D(),
                                    Extent= this.m_swapChainExtent
                                }
                            }
                        },
                        RasterizationState = new SharpVk.PipelineRasterizationStateCreateInfo
                        {
                            DepthClampEnable = false,
                            RasterizerDiscardEnable = false,
                            PolygonMode = PolygonMode.Fill,
                            LineWidth = 1,
                            CullMode = CullModeFlags.Back,
                            FrontFace = FrontFace.Clockwise,
                            DepthBiasEnable = false
                        },
                        MultisampleState = new SharpVk.PipelineMultisampleStateCreateInfo
                        {
                            SampleShadingEnable = false,
                            RasterizationSamples = SampleCountFlags.SampleCount1,
                            MinSampleShading = 1
                        },
                        ColorBlendState = new SharpVk.PipelineColorBlendStateCreateInfo
                        {
                            Attachments = new[]
                            {
                                new PipelineColorBlendAttachmentState
                                {
                                    ColorWriteMask = ColorComponentFlags.R
                                                        | ColorComponentFlags.G
                                                        | ColorComponentFlags.B
                                                        | ColorComponentFlags.A,
                                    BlendEnable = false,
                                    SourceColorBlendFactor = BlendFactor.One,
                                    DestinationColorBlendFactor = BlendFactor.Zero,
                                    ColorBlendOp = BlendOp.Add,
                                    SourceAlphaBlendFactor = BlendFactor.One,
                                    DestinationAlphaBlendFactor = BlendFactor.Zero,
                                    AlphaBlendOp = BlendOp.Add
                                }
                            },
                            LogicOpEnable = false,
                            LogicOp = LogicOp.Copy,
                            BlendConstants = new float[] {0,0,0,0}
                        },
                        Stages = new[]
                        {
                            new PipelineShaderStageCreateInfo
                            {
                                Stage = ShaderStageFlags.Vertex,
                                Module = this.m_vertShader,
                                Name = "main"
                            },
                            new PipelineShaderStageCreateInfo
                            {
                                Stage = ShaderStageFlags.Fragment,
                                Module = this.m_fragShader,
                                Name = "main"
                            }
                        }
                    }
                }).Single();
        }

        private void CreateFrameBuffers()
        {
            this.m_frameBuffers = this.m_swapChainImageViews.Select(imageView => m_device.CreateFramebuffer(new SharpVk.FramebufferCreateInfo
            {
                RenderPass = m_renderPass,
                Attachments = new[] { imageView },
                Layers = 1,
                Height = this.m_swapChainExtent.Height,
                Width = this.m_swapChainExtent.Width
            })).ToArray();
        }

        private void CreateCommandPool()
        {
            QueueFamilyIndices queueFamilies = FindQueueFamilies(this.m_physicalDevice);

            this.m_commandPool = m_device.CreateCommandPool(new SharpVk.CommandPoolCreateInfo
            {
                QueueFamilyIndex = queueFamilies.GraphicsFamily.Value
            });
        }

        private void CreateCommandBuffers()
        {
            this.m_commandBuffers = m_device.AllocateCommandBuffers(new SharpVk.CommandBufferAllocateInfo
            {
                CommandBufferCount = (uint)this.m_frameBuffers.Length,
                CommandPool = this.m_commandPool,
                Level = CommandBufferLevel.Primary
            });

            for (int index = 0; index < this.m_frameBuffers.Length; index++)
            {
                var commandBuffer = this.m_commandBuffers[index];

                commandBuffer.Begin(new SharpVk.CommandBufferBeginInfo
                {
                    Flags = CommandBufferUsageFlags.SimultaneousUse
                });

                commandBuffer.BeginRenderPass(new SharpVk.RenderPassBeginInfo
                {
                    RenderPass = this.m_renderPass,
                    Framebuffer = this.m_frameBuffers[index],
                    RenderArea = new Rect2D
                    {
                        Offset = new Offset2D(),
                        Extent = this.m_swapChainExtent
                    },
                    ClearValues = new ClearValue[]
                    {
                        new ClearColorValue(0f, 0f, 0f, 1f)
                    }
                }, SubpassContents.Inline);

                commandBuffer.BindPipeline(PipelineBindPoint.Graphics, this.m_pipeline);

                commandBuffer.Draw(3, 1, 0, 0);

                commandBuffer.EndRenderPass();

                commandBuffer.End();
            }
        }

        private void CreateSemaphores()
        {
            this.m_imageAvailableSemaphore = m_device.CreateSemaphore(new SharpVk.SemaphoreCreateInfo());
            this.m_renderFinishedSemaphore = m_device.CreateSemaphore(new SharpVk.SemaphoreCreateInfo());
        }

        private QueueFamilyIndices FindQueueFamilies(PhysicalDevice device)
        {
            QueueFamilyIndices indices = new QueueFamilyIndices();

            var queueFamilies = device.GetQueueFamilyProperties();

            for (uint index = 0; index < queueFamilies.Length && !indices.IsComplete; index++)
            {
                if (queueFamilies[index].QueueFlags.HasFlag(QueueFlags.Graphics))
                {
                    indices.GraphicsFamily = index;
                }

                if (device.GetSurfaceSupport(index, this.m_surface))
                {
                    indices.PresentFamily = index;
                }
            }

            return indices;
        }

        private SurfaceFormat ChooseSwapSurfaceFormat(SurfaceFormat[] availableFormats)
        {
            if (availableFormats.Length == 1 && availableFormats[0].Format == Format.Undefined)
            {
                return new SurfaceFormat
                {
                    Format = Format.B8G8R8A8UNorm,
                    ColorSpace = ColorSpace.SrgbNonlinear
                };
            }

            foreach (var format in availableFormats)
            {
                if (format.Format == Format.B8G8R8A8UNorm && format.ColorSpace == ColorSpace.SrgbNonlinear)
                {
                    return format;
                }
            }

            return availableFormats[0];
        }

        private PresentMode ChooseSwapPresentMode(PresentMode[] availablePresentModes)
        {
            return availablePresentModes.Contains(PresentMode.Mailbox)
                    ? PresentMode.Mailbox
                    : PresentMode.Fifo;
        }

        public Extent2D ChooseSwapExtent(SurfaceCapabilities capabilities)
        {
            if (capabilities.CurrentExtent.Width != uint.MaxValue)
            {
                return capabilities.CurrentExtent;
            }
            else
            {
                return new Extent2D
                {
                    Width = Math.Max(capabilities.MinImageExtent.Width, Math.Min(capabilities.MaxImageExtent.Width, uint.MaxValue)),
                    Height = Math.Max(capabilities.MinImageExtent.Height, Math.Min(capabilities.MaxImageExtent.Height, uint.MaxValue))
                };
            }
        }

        private SwapChainSupportDetails QuerySwapChainSupport(PhysicalDevice device)
        {
            return new SwapChainSupportDetails
            {
                Capabilities = device.GetSurfaceCapabilities(this.m_surface),
                Formats = device.GetSurfaceFormats(this.m_surface),
                PresentModes = device.GetSurfacePresentModes(this.m_surface)
            };
        }

        private bool IsSuitableDevice(PhysicalDevice device)
        {
            return device.EnumerateDeviceExtensionProperties(null).Any(extension => extension.ExtensionName == KhrSwapchain.ExtensionName)
                    && FindQueueFamilies(device).IsComplete;
        }

        private static uint[] LoadShaderData(string filePath, out int codeSize)
        {
            var fileBytes = File.ReadAllBytes(filePath);
            var shaderData = new uint[(int)Math.Ceiling(fileBytes.Length / 4f)];

            System.Buffer.BlockCopy(fileBytes, 0, shaderData, 0, fileBytes.Length);

            codeSize = fileBytes.Length;

            return shaderData;
        }
    }
}