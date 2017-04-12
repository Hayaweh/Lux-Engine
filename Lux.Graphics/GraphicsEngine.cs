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
        private IntPtr m_windowHandle;

        public void Run(IntPtr windowHandle)
        {
            m_windowHandle = windowHandle;
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
    }
}