using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using HalconDotNet;

// The goal of this program is twofold: on the one side show how to work with 
// exported code, and on the other side how to make use of drawing objects in a 
// WPF application.
// The way to proceed is basically the following:
// - Write a prototype in HDevelop (please refer to the HDevelop example program).
// - Group the image processing task and the display of the results
//   in two separate procedures (process_image() and display_results()).
// - Define a callback, HDrawingObjectCallback() that calls the image procesing
//   procedure triggers the UI thread to display the results.
//   See the MSDN documentation on the Dispatcher class.
namespace DrawingObjectsWPF
{
    public delegate int CallBack(long draw_id, long window_handle, IntPtr type);
    public delegate void DisplayResultsDelegate();

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        HDevelopExport hdev_export;
        List<HTuple> drawing_objects;
        DisplayResultsDelegate display_results_delegate;
        CallBack cb;
        HObject ho_EdgeAmplitude;
        HObject background_image = null;
        object image_lock = new object();

        public MainWindow()
        {
            InitializeComponent();
            hdev_export = new HDevelopExport();
            drawing_objects = new List<HTuple>();
        }

        private void hWindowControlWPF1_HInitWindow(object sender, EventArgs e)
        {
            // Initialize window
            // - Read and attach background image
            // - Initialize display results delegate function
            // - Initialize callbacks
            HTuple width, height;
            hdev_export.hv_ExpDefaultWinHandle = hWindowControlWPF1.HalconID;
            HOperatorSet.ReadImage(out background_image, "fabrik");
            HOperatorSet.GetImageSize(background_image, out width, out height);
            hWindowControlWPF1.HalconWindow.SetPart(0, 0, height - 1, width - 1);
            hWindowControlWPF1.HalconWindow.AttachBackgroundToWindow(new HImage(background_image));
            display_results_delegate = new DisplayResultsDelegate(() => {
              lock (image_lock)
              {
                if (ho_EdgeAmplitude != null)
                  hdev_export.display_results(ho_EdgeAmplitude);
                hWindowControlWPF1.HalconWindow.DispCross(-12.0,-12.0,3.0,0);
              }
            });
            cb = new CallBack(HDrawingObjectCallback);
            hWindowControlWPF1.Focus();
        }
      
        private HObject BackgroundImage
        {
          get {
            lock (image_lock)
            {
              return new HObject(background_image);
            }
          }
        }

        private void hWindowControlWPF1_HMouseDown(object sender, HMouseEventArgsWPF e)
        {
            // Open context menu if right mouse button is clicked
            if (e.Button == MouseButton.Right)
            {
                ContextMenu cm = this.FindResource("cmButton") as ContextMenu;
                cm.PlacementTarget = sender as Button;
                cm.IsOpen = true;
            }
        }

        protected int HDrawingObjectCallback(long draw_id, long window_handle, IntPtr type)
        {
            // On callback, process and display image
            lock (image_lock)
            {
              hdev_export.process_image(background_image, out ho_EdgeAmplitude, hWindowControlWPF1.HalconID, draw_id);
            }
            // You need to switch to the UI thread to display the results
            Dispatcher.BeginInvoke(display_results_delegate);
            return 0;
        }

        private void SetCallbacks(HTuple draw_id)
        {
            // Set callbacks for all relevant interactions
            drawing_objects.Add(draw_id);
            IntPtr ptr = Marshal.GetFunctionPointerForDelegate(cb);
            HOperatorSet.SetDrawingObjectCallback(draw_id, "on_resize", ptr);
            HOperatorSet.SetDrawingObjectCallback(draw_id, "on_drag", ptr);
            HOperatorSet.SetDrawingObjectCallback(draw_id, "on_attach", ptr);
            HOperatorSet.SetDrawingObjectCallback(draw_id, "on_select", ptr);
            lock (image_lock)
            {
              HOperatorSet.AttachDrawingObjectToWindow(hWindowControlWPF1.HalconID, draw_id);
            }
        }

        private void OnRectangle1_Click(object sender, RoutedEventArgs e)
        {
            // Execute context menu command:
            // Add new rectangle1 drawing object
            HTuple draw_id;
            hdev_export.add_new_drawing_object("rectangle1", hWindowControlWPF1.HalconID, out draw_id);
            SetCallbacks(draw_id);
        }

        private void OnRectangle2_Click(object sender, RoutedEventArgs e)
        {
            // Execute context menu command:
            // Add new rectangle2 drawing object
            HTuple draw_id;
            hdev_export.add_new_drawing_object("rectangle2", hWindowControlWPF1.HalconID, out draw_id);
            SetCallbacks(draw_id);
        }

        private void OnCircle_Click(object sender, RoutedEventArgs e)
        {
            // Execute context menu command:
            // Add new circle drawing object
            HTuple draw_id;
            hdev_export.add_new_drawing_object("circle", hWindowControlWPF1.HalconID, out draw_id);
            SetCallbacks(draw_id);
        }

        private void OnEllipse_Click(object sender, RoutedEventArgs e)
        {
            // Execute context menu command:
            // Add new ellipse drawing object
            HTuple draw_id;
            hdev_export.add_new_drawing_object("ellipse", hWindowControlWPF1.HalconID, out draw_id);
            SetCallbacks(draw_id);
        }

        private void OnClearAllObjects_Click(object sender, RoutedEventArgs e)
        {
            lock (image_lock)
            {
                foreach (HTuple dobj in drawing_objects)
                {
                    HOperatorSet.ClearDrawingObject(dobj);
                }
                drawing_objects.Clear();
            }
            hWindowControlWPF1.HalconWindow.ClearWindow();
        }
    }
}
