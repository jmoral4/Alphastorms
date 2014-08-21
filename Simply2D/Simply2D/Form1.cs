using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Simply2D
{
    public partial class Form1 : Form
    {
        private BufferedGraphicsContext context;
        private BufferedGraphics grafx;

        public Form1()
        {
            InitializeComponent();
            // Retrieves the BufferedGraphicsContext for the  
            // current application domain.
            context = BufferedGraphicsManager.Current;

            // Sets the maximum size for the primary graphics buffer 
            // of the buffered graphics context for the application 
            // domain.  Any allocation requests for a buffer larger  
            // than this will create a temporary buffered graphics  
            // context to host the graphics buffer.
            context.MaximumBuffer = new Size(pnlRenderArea.Width + 1, pnlRenderArea.Height + 1);

            // Allocates a graphics buffer the size of this form 
            // using the pixel format of the Graphics created by  
            // the Form.CreateGraphics() method, which returns a  
            // Graphics object that matches the pixel format of the form.
            grafx = context.Allocate(pnlRenderArea.CreateGraphics(),
                 new Rectangle(0, 0, pnlRenderArea.Width, pnlRenderArea.Height));

            grafx.Graphics.Clear(Color.PowderBlue);
            grafx.Render();
           
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            gTimer.Start();
            ;

        }

        private void Form1_Resize(object sender, EventArgs e)
        {

            if (context != null)
            {
                // Re-create the graphics buffer for a new window size.
                context.MaximumBuffer = new Size(pnlRenderArea.Width + 1, pnlRenderArea.Height + 1);
                if (grafx != null)
                {
                    grafx.Dispose();
                    grafx = null;
                }

                //can't recreate if viewing area is 0 or negative
                if (pnlRenderArea.Width > 0 && pnlRenderArea.Height > 0)
                {
                    grafx = context.Allocate(pnlRenderArea.CreateGraphics(),
                        new Rectangle(0, 0, pnlRenderArea.Width, pnlRenderArea.Height));

                    // Cause the background to be cleared and redraw.
                    grafx.Graphics.Clear(Color.RoyalBlue);
                    grafx.Render();
                    if(!gTimer.Enabled)
                        gTimer.Start();
                }
                else
                {
                    gTimer.Stop(); //no more rendering!                    
                }
                //pnlRenderArea.Refresh();
            }
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
           // grafx.Render(e.Graphics);

        }

        private void gTimer_Tick(object sender, EventArgs e)
        {
            grafx.Graphics.Clear(Color.PowderBlue);
            grafx.Render();
        }
    }
}
