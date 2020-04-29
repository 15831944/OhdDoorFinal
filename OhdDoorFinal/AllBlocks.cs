using System;
using System.Windows.Forms;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace OhdDoorFinal
{
    public partial class AllBlocks : Form
    {
        private int desiredStartLocationX;
        private int desiredStartLocationY;
        public AllBlocks()
        {
            InitializeComponent();
        }
        /// <summary>
        /// Start the form on mouse cursor position
        /// https://stackoverflow.com/questions/11552667/c-sharp-how-to-show-a-form-at-a-specific-mouse-position-on-the-screen
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public AllBlocks(int x, int y):this()
        {
            // here store the value for x & y into instance variables
            this.desiredStartLocationX = x;
            this.desiredStartLocationY = y;

            Load += AllBlocks_Load;

            //InitializeComponent();
        }

        private void AllBlocks_Load(object sender, EventArgs e)
        {
            this.SetDesktopLocation(desiredStartLocationX, desiredStartLocationY);
        }

        private void BtnBlank_Click(object sender, EventArgs e)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            doc.SendStringToExecute("OBLNK ",false,false,false);
            Close();
            Dispose();
        }

        private void BtnGenDoor_Click(object sender, EventArgs e)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            doc.SendStringToExecute("ODR ", false, false, false);
            Close();
            Dispose();
        }

        private void BtnSldDoor_Click(object sender, EventArgs e)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            doc.SendStringToExecute("OSLD ", false, false, false);
            Close();
            Dispose();
        }

        private void BtnPktDoor_Click(object sender, EventArgs e)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            doc.SendStringToExecute("OPKD ", false, false, false);
            Close();
            Dispose();
        }

        private void BtnGarageDoor_Click(object sender, EventArgs e)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            doc.SendStringToExecute("OGRD ", false, false, false);
            Close();
            Dispose();
        }

        private void BtnFldDoor_Click(object sender, EventArgs e)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            doc.SendStringToExecute("OFLD ", false, false, false);
            Close();
            Dispose();
        }

        private void BtnBiFldDoor_Click(object sender, EventArgs e)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            doc.SendStringToExecute("OBFLD ", false, false, false);
            Close();
            Dispose();
        }
    }
}
