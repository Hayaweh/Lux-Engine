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

        public MainWindow()
        {
            Loaded += InitializeVulkan;
            InitializeComponent();

            Width = 1280;
            Height = 720;
            Title = "Lux Engine Editor";
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            QuitButton.Click += (sender, args) => { this.Close(); };
        }

        private void InitializeVulkan(object sender, EventArgs eventArgs)
        {
            Activated -= InitializeVulkan;

            m_luxGraphicEngine = new GraphicsEngine();
            m_luxGraphicEngine.Run(UserControl.Handle);
            UserControl.ClientSizeChanged += OnSizeChanged;

            Console.WriteLine("Now Starting Main Loop");
            m_renderingThread = new Thread(MainLoop);
            m_renderingThread.Start();
        }

        private void OnSizeChanged(object sender, EventArgs sizeChangedEventArgs)
        {
            lock (m_luxGraphicEngine)
            {
                m_luxGraphicEngine.RecreateSwapChain();
            }
        }

        private async void MainLoop()
        {
            while (IsVisible)
            {
                lock (m_luxGraphicEngine)
                {
                    m_luxGraphicEngine.DrawFrame();
                }

                await Task.Delay(0);
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            Visibility = Visibility.Hidden;

            if (m_luxGraphicEngine != null)
            {
                lock (m_luxGraphicEngine)
                {
                    m_luxGraphicEngine.TearDown();
                }
            }
        }
    }
}