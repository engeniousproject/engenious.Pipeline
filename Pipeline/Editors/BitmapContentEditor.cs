using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using engenious.Content.Pipeline;
using System.Drawing;
using System.Windows.Forms;

namespace engenious.Pipeline.Pipeline.Editors
{
    [ContentEditor(".bmp",".png",".jpg" )]
    public class BitmapContentEditor : IContentEditor<Bitmap, TextureContent>
    {
        private PictureBox pictureBox;
        public BitmapContentEditor()
        {
            pictureBox=new PictureBox();
            pictureBox.Dock = DockStyle.Fill;
            
        }

        public Control MainControl
        {
            get
            {
               return pictureBox;
            }
        }

        public void Open(Bitmap importerInput, TextureContent processorOutput)
        {
            pictureBox.Image = importerInput;
            pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
        }
    }
}
