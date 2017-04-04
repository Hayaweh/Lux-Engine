using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Lux.Graphics;

namespace Lux
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private GraphicsEngine LuxGraphicEngine = null;
        private GraphicsEngine m_luxGraphicEngine;

        public MainWindow()
        {
            Activated += InitializeVulkan;
            InitializeComponent();

            Width = 1280;
            Height = 720;
            Title = "Lux Engine Editor";
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        private void InitializeVulkan(object sender, EventArgs eventArgs)
        {
            Activated -= InitializeVulkan;

            m_luxGraphicEngine = LuxGraphicEngine;
            m_luxGraphicEngine = new GraphicsEngine();
            m_luxGraphicEngine.Run(new WindowInteropHelper(this).Handle);

            MainLoop();
        }

        private async void MainLoop()
        {
            await Task.Delay(2000);

            while (IsVisible && m_luxGraphicEngine.IsRunning)
            {
                m_luxGraphicEngine.DrawFrame();
                //await Task.Delay(16);
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            m_luxGraphicEngine.Stop();
        }

        public bool IsAmbientPropertyAvailable(string propertyName)
        {
            return true;
        }
    }
}