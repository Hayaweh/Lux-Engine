using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using Lux.Graphics;
using System.Windows.Forms.Integration;
using HorizontalAlignment = System.Windows.HorizontalAlignment;

namespace Lux
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private GraphicsEngine m_luxGraphicEngine;
        private Thread m_renderingThread = null;
        private UserControl m_form = null;
        private WindowsFormsHost m_host = null;

        private delegate void SynchronizeRenderingThread();

        public MainWindow()
        {
            Activated += InitializeVulkan;
            InitializeComponent();

            Width = 1280;
            Height = 720;
            Title = "Lux Engine Editor";
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            QuitButton.MouseUp += (sender, args) => { Console.WriteLine("Quit Button pressed."); this.Close(); };
        }

        private async void InitializeVulkan(object sender, EventArgs eventArgs)
        {
            Activated -= InitializeVulkan;

            //await Task.Delay(2000);

            m_luxGraphicEngine = new GraphicsEngine();
            m_luxGraphicEngine.Run(UserControl.Handle);
            UserControl.ClientSizeChanged += OnSizeChanged;

            Console.WriteLine("Now Starting Main Loop");
            m_renderingThread = new Thread(MainLoop);
            m_renderingThread.Start();
#pragma warning disable 4014
            //MainLoop();
#pragma warning restore 4014
        }

        private void OnSizeChanged(object sender, EventArgs sizeChangedEventArgs)
        {
            Console.WriteLine("Recreating Swapchain");
            m_luxGraphicEngine.RecreateSwapChain();
        }

        private async void MainLoop()
        {
            while (IsVisible)
            {
                m_luxGraphicEngine.DrawFrame();
                //InvalidateVisual();
                //await Task.Delay(16);
            }
        }

        private void RefreshWindow()
        {
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            m_renderingThread.Abort();
            m_luxGraphicEngine.TearDown();
        }

        public bool IsAmbientPropertyAvailable(string propertyName)
        {
            return true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // m_host = new WindowsFormsHost(); m_host.HorizontalAlignment =
            // HorizontalAlignment.Center; m_host.VerticalAlignment = VerticalAlignment.Center;
            // m_host.Width = 600; m_host.Height = 400; m_form = new UserControl(); m_form.Width =
            // 600; m_form.Height = 400;

            // m_host.Child = m_form; this.AddChild(m_host);

            // InitializeVulkan(this, null);
        }
    }
}