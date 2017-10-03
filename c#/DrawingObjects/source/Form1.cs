using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using HalconDotNet;

namespace HALCONDrawingObjects
{
    /// <summary>
    /// This example shows how to use drawing objects in HALCON/C#.
    /// It shows how to use callbacks and uses a simple graphics stack
    /// implementation for visualization.
    /// 
    /// In this case, a Sobel filter is applied to the selected ROI 
    /// as callback function.
    /// 
    /// So far, the HALCON window does not offer the possibility of having a graphic
    /// stack that allows to pack as many objects as desired and then show them at once.
    /// This example shows how to easily add such a stack, and more importantly, how to
    /// perform the drawing within the UI thread. Concretely, refer to the method 
    /// DisplayResults() below.
    /// 
    /// Furthermore, as noted in the documentation of set_draw_object_callback, no 
    /// graphical operators should be called within a callback of a draw object.
    /// Using the graphic stack additionally avoids this problem.
    /// </summary>
    public interface IHWindowGraphicStack
    {
        void DisplayResults();
        void AddToStack(HObject o);
    }

    public partial class HALCONDialog : Form, IHWindowGraphicStack
    {
        private List<HDrawingObject> drawing_objects = new List<HDrawingObject>();
        private UserActions user_actions;
        private Stack<HObject> graphic_stack = new Stack<HObject>();
        private HDrawingObject selected_drawing_object = new HDrawingObject(250,250,100);
        private HImage background_image = null;
        private object stack_lock = new object();

        public HALCONDialog()
        {
            InitializeComponent();

            HTuple width, height;
            background_image = new HImage("fabrik");
            background_image.GetImageSize(out width, out height);
            halconWindow.HalconWindow.SetPart(0, 0, height.I - 1, width.I - 1);
            halconWindow.HalconWindow.AttachBackgroundToWindow(background_image);
            user_actions = new UserActions(this);
            this.AttachDrawObj(selected_drawing_object);
        }

        public HImage BackgroundImage { get { return background_image; } }

        private void OnSelectDrawingObject(HDrawingObject dobj, HWindow hwin, string type)
        {
            selected_drawing_object = dobj;
            user_actions.SobelFilter(dobj, hwin, type);
        }

        private void AttachDrawObj(HDrawingObject obj)
        {
            drawing_objects.Add(obj);
            // The HALCON/C# interface offers convenience methods that
            // encapsulate the set_drawing_object_callback operator.
            obj.OnDrag(user_actions.SobelFilter);
            obj.OnAttach(user_actions.SobelFilter);
            obj.OnResize(user_actions.SobelFilter);
            obj.OnSelect(OnSelectDrawingObject);
            obj.OnAttach(user_actions.SobelFilter);
            halconWindow.HalconWindow.AttachDrawingObjectToWindow(obj);
        }

        private void DisplayGraphicStack()
        {
            lock (stack_lock)
            {
                HOperatorSet.SetSystem("flush_graphic", "false");
                halconWindow.HalconWindow.ClearWindow();
                while (graphic_stack.Count > 0)
                {
                    halconWindow.HalconWindow.DispObj(graphic_stack.Pop());
                }
                HOperatorSet.SetSystem("flush_graphic", "true");
            }
            halconWindow.HalconWindow.DispCross(-10.0, -10.0, 3.0, 0.0);
        }

        /// <summary>
        /// Forces a context switch, so that objects are display in UI thread
        /// </summary>
        public void DisplayResults()
        {
            try
            {
                halconWindow.BeginInvoke((MethodInvoker)delegate() { DisplayGraphicStack(); });
            }
            catch (HalconException hex)
            {
                MessageBox.Show(hex.GetErrorMessage(), "HALCON error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "HALCON error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void AddToStack(HObject obj)
        {
            lock (stack_lock)
            {
                graphic_stack.Push(obj);
            }
        }

        private void dashedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (selected_drawing_object != null)
                selected_drawing_object.SetDrawingObjectParams(new HTuple("line_style"), new HTuple(20, 5));
        }

        private void continuousToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (selected_drawing_object != null)
                selected_drawing_object.SetDrawingObjectParams(new HTuple("line_style"), new HTuple());
        }

        private void rectangle1ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            HDrawingObject rect1 = HDrawingObject.CreateDrawingObject(
              HDrawingObject.HDrawingObjectType.RECTANGLE1, 100, 100, 210, 210);
            rect1.SetDrawingObjectParams("color", "green");
            AttachDrawObj(rect1);
        }

        private void rectangle2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            HDrawingObject rect2 = HDrawingObject.CreateDrawingObject(
              HDrawingObject.HDrawingObjectType.RECTANGLE2, 100, 100, 0, 100, 50);
            rect2.SetDrawingObjectParams("color", "yellow");
            AttachDrawObj(rect2);
        }

        private void circleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            HDrawingObject circle = HDrawingObject.CreateDrawingObject(
              HDrawingObject.HDrawingObjectType.CIRCLE, 200, 200, 70);
            circle.SetDrawingObjectParams("color", "magenta");
            AttachDrawObj(circle);
        }

        private void ellipseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            HDrawingObject ellipse = HDrawingObject.CreateDrawingObject(
              HDrawingObject.HDrawingObjectType.ELLIPSE, 50, 50, 0, 100, 50);
            ellipse.SetDrawingObjectParams("color", "blue");
            AttachDrawObj(ellipse);
        }

        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
                contextMenuStrip1.Show(this, new Point(e.X, e.Y));
        }

        private void redToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (selected_drawing_object != null)
                selected_drawing_object.SetDrawingObjectParams("color", "red");
        }

        private void yellowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (selected_drawing_object != null)
                selected_drawing_object.SetDrawingObjectParams("color", "yellow");
        }

        private void greenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (selected_drawing_object != null)
                selected_drawing_object.SetDrawingObjectParams("color", "green");
        }

        private void blueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (selected_drawing_object != null)
                selected_drawing_object.SetDrawingObjectParams("color", "blue");
        }

        private void halconWindow_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
                contextMenuStrip1.Show(this, new Point(e.X, e.Y));
        }

        private void clearAllObjectsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            lock (stack_lock)
            {
                foreach (HDrawingObject dobj in drawing_objects)
                {
                    dobj.Dispose();
                }
                drawing_objects.Clear();
                graphic_stack.Clear();
            }
            DisplayGraphicStack();
        }
    }
}