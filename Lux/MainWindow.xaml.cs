using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
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

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                LuxGraphicEngine = new GraphicsEngine();
                LuxGraphicEngine.Run();
            }
            catch (Exception error)
            {
                Console.WriteLine(error);
                throw;
            }
        }
    }
}