using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Tesseract;
using AForge;
using AForge.Imaging.Filters;

namespace OCRAnalysis
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        //Store the imported Image
        Bitmap b = new Bitmap(100, 100);

        //Counter
        int wc = 0;
        int NumInitialSubImages = 0;

        //dynamic clipboard content
        string ClipB = "";

        //Store all Word iterations
        string[,] words = new string[99, 99];

        //Store all confindence levels corresponding to each filtered word
        float[,] Confidence = new float[99, 99];

        //Store bitmaps
        Bitmap[,] images = new Bitmap[99, 99];

        //Array of PictureBox to dynamically display the cropped bitmaps
        PictureBox[] pics = new PictureBox[999];

        //Array of labels to display the tersseract guess
        Label[] labels = new Label[999];

        //Array of labels to display the tersseract confidence
        Label[] labelsG = new Label[999];


        private void button1_Click_1(object sender, EventArgs e)
        {
            getfile();
        }

        public void getfile()
        {
            //Browse for image through OpenFileDialog
            openFileDialog1.InitialDirectory = @"C:\My Documents\My Pictures";
            openFileDialog1.Filter = "JPEG Compressed Image (*.jpg|*.jpg" + "|GIF Image(*.gif|*.gif" + "|Bitmap Image(*.bmp|*.bmp";
            openFileDialog1.AutoUpgradeEnabled = true;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                String Chosen_File = openFileDialog1.FileName;
                String inputExt = (Path.GetExtension(Chosen_File).ToLower());

                b = new Bitmap(System.Drawing.Image.FromFile(@Chosen_File, true));

                if (b != null)
                {
                    InitialSplit(b);
                    button1.Enabled = false;
                    panel1.Enabled = true;
                }
                else
                {
                    getfile();
                }
            }
        }

        //First method that splits initial image
        private void InitialSplit(Bitmap b)
        {
            //Strip image into subimages
            var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);
            engine.DefaultPageSegMode = PageSegMode.SingleBlock;
            Tesseract.Page p = engine.Process(b);
            ClipB = p.GetText();


            //Iterate through all words
            using (var iter = p.GetIterator())
            {
                iter.Begin();
                int counter = 0;
                do
                {
                    Rect bounds; //Bounds of identified words
                    if (iter.TryGetBoundingBox(PageIteratorLevel.Word, out bounds))
                    {
                        //Convert tesseract.Rect into a System.Drawing.Rectangle
                        Rectangle section = new Rectangle(new System.Drawing.Point(bounds.X1, bounds.Y1), new Size(bounds.Width, bounds.Height));

                        //Save Words
                        words[0, wc] = iter.GetText(PageIteratorLevel.Word);

                        //Save confidence level corresponding to bitmap
                        Confidence[0, wc] = iter.GetConfidence(PageIteratorLevel.Word);

                        //Save images
                        images[0, wc] = CropImage(b, section);

                        Display(0 ,wc, true);

                    }
                    counter++;
                    wc++; //Increment word counter
                } while (iter.Next(PageIteratorLevel.Block, PageIteratorLevel.Word));
                //Document how many images where produced
                NumInitialSubImages = counter;
            }

            p.Dispose();
        }

        //method to crop images according to Rectangle input
        public Bitmap CropImage(Bitmap source, Rectangle section)
        {
            // An empty bitmap which will hold the cropped image
            Bitmap bmp = new Bitmap(section.Width, section.Height);

            Graphics g = Graphics.FromImage(bmp);
            g.DrawImage(source, 0, 0, section, GraphicsUnit.Pixel);
            return bmp;
        }

        private void Display(int filter, int num, bool initial = false)
        {
            //DISPLAY////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //Create a new picture box element to hold the image
            pics[num] = new PictureBox();
            int increment = 0; //Allows for spacing between words
            try { increment = pics[num - 1].Location.Y + pics[num - 1].Height + 10; }
            catch { increment = 0; }
            //Setting pictures properties and appending to the form
            pics[num].Location = new System.Drawing.Point(0, increment);
            pics[num].Name = "pic" + num;
            pics[num].BackgroundImage = images[filter, num];
            pics[num].SizeMode = PictureBoxSizeMode.Normal;
            pics[num].BackgroundImageLayout = ImageLayout.Center;
            pics[num].Width = 200;
            pics[num].Height = images[filter, num].Height;
            if (initial) { panel2.Controls.Add(pics[num]);} else{panel13.Controls.Add(pics[num]);}

            //Creating Label elements to hold tesseracts guess of the corresponding bitmap
            labels[num] = new Label();
            int hInc = (pics[num].Location.Y) + 5; //display images adjacent to bitmap
            //Setting properties and appending to the form
            labels[num].Location = new System.Drawing.Point(220, hInc);
            labels[num].Name = "labels" + num;
            labels[num].Text = words[filter, num];
            if (initial) { panel2.Controls.Add(labels[num]); } else { panel13.Controls.Add(labels[num]); }

            //Creating Label elements to hold tesseracts confidence of the corresponding guess
            labelsG[num] = new Label();
            //Setting properties and appending to the form
            labelsG[num].Location = new System.Drawing.Point(350, hInc);
            labelsG[num].Name = "labels" + num;
            labelsG[num].Text = Confidence[filter, num].ToString();
            if (initial) { panel2.Controls.Add(labelsG[num]); } else { panel13.Controls.Add(labelsG[num]); }
            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        }

        //Analyse croped bitmaps under a specific filter
        private void OCR(int lastFilter = 10)
        {
            var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);
            engine.DefaultPageSegMode = PageSegMode.SingleBlock;

            for (int i = 0; i < NumInitialSubImages; i++)//minimum of 1 //loop through words
            {
                for (int f = 1; f < lastFilter; f++)//minimum of 1 //loop through filters
                {
                    Bitmap temp = Filter(images[0, i], f);
                    Tesseract.Page p = engine.Process(temp); //input is image with no filter
                    //SAVE
                    images[f,i] = temp;
                    words[f,i] =  p.GetText();
                    Confidence[f, i] = p.GetMeanConfidence();

                  
                    p.Dispose();

                }
            }
        }

        private Bitmap Filter(Bitmap imagem, int filter)
        {
            imagem = imagem.Clone(new Rectangle(0, 0, imagem.Width, imagem.Height), System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            FiltersSequence seq = new FiltersSequence();
            Bitmap temp = imagem;

            Erosion erosion = new Erosion();
            Dilatation dilatation = new Dilatation();
            ColorFiltering cor = new ColorFiltering();
            cor.Blue = new AForge.IntRange(200, 255);
            cor.Red = new AForge.IntRange(200, 255);
            cor.Green = new AForge.IntRange(200, 255);
            Opening open = new Opening();
            BlobsFiltering bc = new BlobsFiltering();
            GaussianSharpen gs = new GaussianSharpen();
            ContrastCorrection cc = new ContrastCorrection();

            Closing close = new Closing();

            if (filter == 1)
            {
                //Filter 1
                seq = new FiltersSequence(erosion, dilatation, gs);
                temp = seq.Apply(imagem);
            }
            if (filter == 2)
            {
                //Filter 2
                seq = new FiltersSequence(dilatation,gs);
                temp = seq.Apply(imagem);
            }
            if (filter == 3)
            {
                //Filter 3
              /*  Invert inverter = new Invert();
                seq = new FiltersSequence(inverter);
                temp = seq.Apply(imagem);*/
            }
            if (filter == 4)
            {
                //Filter 4
                seq = new FiltersSequence(cor, dilatation, gs);
                temp = seq.Apply(imagem);
            }
            if (filter == 5)
            {
                //Filter 5
                seq = new FiltersSequence(open, gs);
                temp = seq.Apply(imagem);
            }
            if (filter == 6)
            {
                //Filter 6
                bc.MinHeight = 10;
                seq = new FiltersSequence(bc,gs,gs);
                temp = seq.Apply(imagem);
            }
            if (filter == 7)
            {
                //Filter 7
                seq = new FiltersSequence(close, gs, gs);
                temp = seq.Apply(imagem);
            }
            if (filter == 8)
            {
                //Filter 8
                seq = new FiltersSequence(gs,gs,gs);
                temp = seq.Apply(imagem);
            }
            if (filter == 9)
            {
                //Filter 9
                seq = new FiltersSequence(cc,gs,gs);
                temp = seq.Apply(imagem);
            }

            return temp;
        }


        private void button9_Click(object sender, EventArgs e)
        {
            Application.Restart();
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            panel14.Visible = false;

            panel11.Top = 0;
            panel11.Left = 0;
            panel11.Visible = true;

            OCR();
            int ii = -1;
            int ff = -1;

            double max = 0;
            //Display best images
            for (int i = 0; i < NumInitialSubImages; i++)//minimum of 1 //loop through words
            {
                for (int f = 1; f < 10; f++)//minimum of 1 //loop through filters
                {
                    if (Confidence[f, i] > max)
                    {
                        max = Confidence[f, i];
                        ii = i;
                        ff = f;
                    }
                }
                //display
                max = 0;
                Display(ff, ii);
            }


            panel12.Enabled = true;
        }

        private void button4_Click_1(object sender, EventArgs e)
        {
            //Reset array and repopulate
            Array.Clear(words, 0, NumInitialSubImages);
            words[0, 0] = ClipB;
            Array.Clear(pics, 0, NumInitialSubImages);
            images[0, 0] = b;
            Array.Clear(pics, 0, NumInitialSubImages);
            Confidence[0, 0] = 0;
            NumInitialSubImages = 1;

            panel3.Visible = true;
            panel3.Top = 0;
            panel3.Left = 0;
            panel14.Visible = false;

            pictureBox1.Width = b.Width;
            pictureBox1.Height = b.Height;
            pictureBox1.BackgroundImage = b;
            richTextBox1.Text = ClipB;
            panel5.Enabled = true;
        }

        private void button7_Click_1(object sender, EventArgs e)
        {
            panel7.BringToFront();
            panel7.Top = 0;
            panel7.Left = 0;
            panel7.Visible = true;
            panel3.Visible = false;

            OCR();

            double max = 0;
            Bitmap image = null;
            string text = null;

            for (int f = 1; f < 9; f++)//minimum of 1 //loop through filters
            {
                if (Confidence[f, 0] > max)
                {
                    //replace max data
                    max = Confidence[f, 0];
                    image = images[f, 0];
                    text = words[f, 0];
                }
            }

            pictureBox2.Width = b.Width;
            pictureBox2.Height = b.Height;
            pictureBox2.BackgroundImage = image;

            richTextBox2.Text = text;

            panel8.Enabled = true;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Application.Restart();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            Application.Restart();
        }

        private void button10_Click(object sender, EventArgs e)
        {
            Application.Restart();
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            System.Windows.Forms.Clipboard.SetText(ClipB);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Clipboard.SetText(ClipB);
        }

        private void button9_Click_1(object sender, EventArgs e)
        {
            Application.Restart();
        }
    }
}
