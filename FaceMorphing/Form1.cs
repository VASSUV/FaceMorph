using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using AForge.Imaging;
using AForge.Imaging.Filters;
using Emgu.CV;
using Emgu.CV.GPU;
using Emgu.CV.Structure;
using FaceMorphing;
using System.Runtime.InteropServices;
using Emgu.CV.UI;

namespace ppp
{               
    public partial class Form1 : Form
    {
        #region Объявления

        public string FileName = "";    // Имя текущего проекта
        public Form2 f = new Form2();   // Форма для создания нового проекта
        public int morfing = 0;         // Состояние морфинга
        public int pause = 0;           // Состояние паузы при морфинге
        int stop = 0;                   // Состояние остановки морфинга 

        // текущее положение курсора на изображении
        public System.Drawing.Point location;

        // База данных точек и треугольников
        public BData dat = new BData();

        // Пара точек хранятся здесь то помещения их в базу данных точек
        Triangulator.Geometry.Point dot1 = new Triangulator.Geometry.Point(0, 0, 0),
                                    dot2 = new Triangulator.Geometry.Point(0, 0, 0);
        public Bitmap A ; // Первое изображение
        public Bitmap B ; // Второе изображение
        System.Drawing.Image Source1 = new Bitmap(1, 1);// Первое изображение
        System.Drawing.Image Source2 = new Bitmap(1, 1);// Второе изображение

        public int DeletePoint = -1;    // Номер точки подлежащей удалению
        public int YelloPoint = 0;      // Состояние точки закрашенной желтым цветом

        // Списки точек нарисованных на изображениях
        List<Triangulator.Geometry.Point> lst1 = new List<Triangulator.Geometry.Point>();
        List<Triangulator.Geometry.Point> lst2 = new List<Triangulator.Geometry.Point>();
        
        // Путь до текущего запущенного файла программ FaceMorphing.exe
        string path = Process.GetCurrentProcess().MainModule.FileName.Replace("FaceMorphing.exe","");

        double zoom = 1;// 

        int numIter = 0;
        int H, W;
        int add = 1;
        int gn = 0;

        #endregion       

        public Form1()
        {
            InitializeComponent();
            trackBar1.Maximum = 5;
            pictureBox1.MouseUp -= pictureBox1_MouseUp;
            pictureBox2.MouseUp -= pictureBox1_MouseUp;
            pictureBox1.MouseDown -= pictureBox1_MouseDown;
            pictureBox2.MouseDown -= pictureBox1_MouseDown; 
            pictureBox1.Invalidate();
            pictureBox2.Invalidate();
            toolStripStatusLabel1.Text = "";
            AddOwnedForm(f); 
             path = path.Replace("FaceMorphing.vshost.exe", "");

             toolStripMenuItem3.Checked = !(toolStripMenuItem4.Checked = toolStripMenuItem2.Checked = false);
        }

        #region Morphing

        private void playAnimationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (morfing == 0 && A != null && B != null)
            {
                if (numIter == 0)
                {
                    gn = (int)numericUpDown1.Value;
                }
                pause = 0;
                stop = 0;
                pictureBox1.MouseDown -= pictureBox1_MouseDown;
                pictureBox2.MouseDown -= pictureBox1_MouseDown;
                pictureBox1.MouseDown -= pictureBox1_MouseUp;
                pictureBox2.MouseDown -= pictureBox1_MouseUp;
                nextItarationToolStripMenuItem.Click -= nextItarationToolStripMenuItem_Click;
                previosIterationToolStripMenuItem.Click -= previosIterationToolStripMenuItem_Click;
                tabControl1.SelectTab(2);
                toolStripStatusLabel1.Text = "Morphing animation";
                dat.ReTriangle();
                new Thread(play).Start();      // Выполнить Go() в новом потоке
                this.Update();
            }
        }
        
        private void Play1(double Persent)
        {
            //int w = Math.Min(A.Width, B.Width),
            //    h = Math.Min(A.Height, B.Height);
            dat.reTris(Persent);
            Bitmap b1, b2;
            if (dat.Points1.Count == 4)
            {
                b1 = (A as Bitmap).Clone(new Rectangle(new System.Drawing.Point(0, 0), new Size(W, H)), PixelFormat.Format24bppRgb);
                b2 = (B as Bitmap).Clone(new Rectangle(new System.Drawing.Point(0, 0), new Size(W, H)), PixelFormat.Format24bppRgb);
                Morph filter = new Morph(b1);
                filter.SourcePercent = Persent;
                Bitmap resultImage = new Bitmap(b2);
                resultImage = filter.Apply(b2);

                ResizeBilinear filr = new ResizeBilinear((int)(A.Width * zoom), (int)(A.Height * zoom));

                pictureBox3.Image = filr.Apply(resultImage);
                System.Threading.Thread.Sleep(500);
            }

            if (dat.Points1.Count > 4)
            {
                //try
                //{
                    Source1 = drawTriangle(1, dat.Tris1.Count - 1, new Bitmap(A), new Bitmap(A/*Source1*/));
                    Source2 = drawTriangle(2, dat.Tris1.Count - 1, new Bitmap(B), new Bitmap(B/*Source2*/));
                //}
                //catch { }
                b1 = (Source1 as Bitmap).Clone(new Rectangle(new System.Drawing.Point(0, 0), Source1.Size), PixelFormat.Format24bppRgb);
                b2 = (Source2 as Bitmap).Clone(new Rectangle(new System.Drawing.Point(0, 0), Source2.Size), PixelFormat.Format24bppRgb);
                Morph filter = new Morph(b1);
                filter.SourcePercent = Persent;
                Bitmap resultImage = new Bitmap(b2);
                resultImage = filter.Apply(b2);

                ResizeBilinear filr = new ResizeBilinear((int)(A.Width * zoom), (int)(A.Height * zoom));

                pictureBox3.Image = filr.Apply(resultImage);
            }
        }
        
        private void play()
        {
            morfing = 1;
            try
            {
                numericUpDown1.Value = numericUpDown1.Value;
                numericUpDown1.Enabled = false;
                numericUpDown1.ReadOnly = true;
                if (numIter ==0)
                    trackBar1.Value = numIter;
            }
            catch { }
            if (dat.Points1.Count == 4 && numIter == 0)
            {
                DialogResult cv = MessageBox.Show("Выставить контрольные точки?", "Сообщение морфинга", MessageBoxButtons.YesNo);
                if (cv == DialogResult.Yes)
                    autoControlPointsToolStripMenuItem_Click(null, null);
            }
            Source1 = new Bitmap(A);
            Source2 = new Bitmap(B);
            int i = 0;
            for (i = numIter; i <= gn && pause == 0 && stop == 0; i++)
            {
                Play1((double)(i / (double)gn));
                try
                {
                    trackBar1.Value += 1;
                }
                catch { }
            }
            numIter = 0;
            if (pause != 0)
            {
                numIter = i;
                toolStripStatusLabel1.Text = "Animation pause";
            }
            else if (stop != 0)
            {
                toolStripStatusLabel1.Text = "Animation stop";
                ResizeBilinear filr = new ResizeBilinear((int)(A.Width * zoom), (int)(A.Height * zoom));
                pictureBox3.Image = filr.Apply(new Bitmap(A));
                trackBar1.Value = 0;
                numericUpDown1.Enabled = true;
                numericUpDown1.ReadOnly = false;
            }
            else
            {
                toolStripStatusLabel1.Text = "Animation END";
                numericUpDown1.Enabled = true;
                numericUpDown1.ReadOnly = false;
            }
            try
            {
                if (add == 1)
                {
                    pictureBox1.MouseDown += pictureBox1_MouseDown;
                    pictureBox2.MouseDown += pictureBox1_MouseDown;
                }
                else
                {
                    pictureBox1.MouseDown += pictureBox1_MouseUp;
                    pictureBox2.MouseDown += pictureBox1_MouseUp;
                }
                nextItarationToolStripMenuItem.Click += nextItarationToolStripMenuItem_Click;
                previosIterationToolStripMenuItem.Click += previosIterationToolStripMenuItem_Click;
            }
            catch { }
            morfing = 0;
        }

        private void pauseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(trackBar1.Value != 0)
                pause = 1;
        }

        private void stopanimationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (trackBar1.Value != 0)
            {
                stop = 1;
                numIter = 0;
            }
            if (pause == 1)
            {
                toolStripStatusLabel1.Text = "Animation stop";
                ResizeBilinear filr = new ResizeBilinear((int)(A.Width * zoom), (int)(A.Height * zoom));
                pictureBox3.Image = filr.Apply(new Bitmap(A));
                trackBar1.Value = 0;
                numericUpDown1.Enabled = true;
                numericUpDown1.ReadOnly = false;
            }
        }

        private void nextItarationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            nextItarationToolStripMenuItem.Click -= nextItarationToolStripMenuItem_Click;
            previosIterationToolStripMenuItem.Click -= previosIterationToolStripMenuItem_Click;
            if (trackBar1.Value == trackBar1.Maximum)
            {
                trackBar1.Value = 0;
                Play1(0);
            }
            else
            {
                trackBar1.Value += 1;
                Play1(((double)trackBar1.Value / (double)trackBar1.Maximum));
            }
            nextItarationToolStripMenuItem.Click += nextItarationToolStripMenuItem_Click;
            previosIterationToolStripMenuItem.Click += previosIterationToolStripMenuItem_Click;
        }

        private void previosIterationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            nextItarationToolStripMenuItem.Click -= nextItarationToolStripMenuItem_Click;
            previosIterationToolStripMenuItem.Click -= previosIterationToolStripMenuItem_Click;
            if (trackBar1.Value == 0)
            {
                trackBar1.Value = trackBar1.Maximum;
                Play1(1);
            }
            else
            {
                trackBar1.Value -= 1;
                Play1( ((double)trackBar1.Value / (double)trackBar1.Maximum));
            }
            nextItarationToolStripMenuItem.Click += nextItarationToolStripMenuItem_Click;
            previosIterationToolStripMenuItem.Click += previosIterationToolStripMenuItem_Click;
        }

        private void addControlPointToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (addControlPointToolStripMenuItem.Checked == true)
            {
                deleteControlPointToolStripMenuItem.Checked = false;
                add = 1;
                toolStripStatusLabel1.Text = "Add Contol point in first image";
                tabControl1.SelectTab(0);
                pictureBox1.MouseDown -= pictureBox1_MouseDown;
                pictureBox2.MouseDown -= pictureBox1_MouseDown;
                pictureBox1.MouseDown += pictureBox1_MouseDown;
                pictureBox2.MouseDown += pictureBox1_MouseDown;
                pictureBox1.MouseDown -= pictureBox1_MouseUp;
                pictureBox2.MouseDown -= pictureBox1_MouseUp;
            }
            else
            { 
                pictureBox1.MouseDown -= pictureBox1_MouseDown;
                pictureBox2.MouseDown -= pictureBox1_MouseDown; 
            }
        }

        private void deleteControlPointToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (deleteControlPointToolStripMenuItem.Checked == true)
            {
                addControlPointToolStripMenuItem.Checked = false;
                add = 0;
                pictureBox1.MouseDown -= pictureBox1_MouseUp;
                pictureBox2.MouseDown -= pictureBox1_MouseUp;
                pictureBox1.MouseDown -= pictureBox1_MouseDown;
                pictureBox2.MouseDown -= pictureBox1_MouseDown;
                pictureBox1.MouseDown += pictureBox1_MouseUp;
                pictureBox2.MouseDown += pictureBox1_MouseUp;
            }
            else 
            {
                pictureBox1.MouseDown -= pictureBox1_MouseUp;
                pictureBox2.MouseDown -= pictureBox1_MouseUp;
            }
        }

        #endregion

        #region Триангуляция и интерполяция

        private Bitmap drawTriangle(int typ, int n, Bitmap img, Bitmap cimg)
        {
            int l = 0;

            for (l = 0; l <= n; l++)
            {
                double x1, x2, tmp, xnew = 0, ynew;
                Triangulator.Geometry.Point A, B, C;
                if (typ == 1)
                {
                    A = dat.Points1[dat.Tris1[l].p1];
                    B = dat.Points1[dat.Tris1[l].p2];
                    C = dat.Points1[dat.Tris1[l].p3];
                }
                else if (typ == 2)
                {
                    A = dat.Points2[dat.Tris2[l].p1];
                    B = dat.Points2[dat.Tris2[l].p2];
                    C = dat.Points2[dat.Tris2[l].p3];
                }
                else
                {
                    A = B = C = new Triangulator.Geometry.Point(0, 0, 0);
                }
                Triangulator.Geometry.Point a2 = dat.Points3[dat.Tris[l].p1],
                    b2 = dat.Points3[dat.Tris[l].p2], c2 = dat.Points3[dat.Tris[l].p3];
                #region Сортировка
                if (A.Y > B.Y)
                {
                    var t = A.Clone();
                    A = B.Clone();
                    B = t.Clone();

                    t = a2.Clone();
                    a2 = b2.Clone();
                    b2 = t.Clone();
                }
                if (A.Y > C.Y)
                {
                    var t = A.Clone();
                    A = C.Clone();
                    C = t.Clone();

                    t = a2.Clone();
                    a2 = c2.Clone();
                    c2 = t.Clone();
                }
                if (B.Y > C.Y)
                {
                    var t = B.Clone();
                    B = C.Clone();
                    C = t.Clone();

                    t = b2.Clone();
                    b2 = c2.Clone();
                    c2 = t.Clone();
                }
                #endregion
                List<double> leftx = new List<double>(), rightx = new List<double>();
                for (int sy = (int)A.Y; sy <= C.Y; sy++)
                {
                    x1 = A.X + (sy - A.Y) * (C.X - A.X) / (C.Y - A.Y);
                    if (sy < B.Y)
                    {
                        x2 = A.X + (sy - A.Y) * (B.X - A.X) / (B.Y - A.Y);
                    }
                    else
                    {
                        if (C.Y == B.Y)
                            x2 = B.X;
                        else
                            x2 = B.X + (sy - B.Y) * (C.X - B.X) / (C.Y - B.Y);
                    }
                    if (x1 > x2)
                    {
                        tmp = x1;
                        x1 = x2;
                        x2 = tmp;
                    }
                    leftx.Add(x1);
                    rightx.Add(x2);
                }

                try
                {
                    for (int j = (int)A.Y; j <= C.Y; j++)
                    {
                        for (int i = (int)leftx[(int)(j - A.Y)]; i <= rightx[(int)(j - A.Y)]; i++)
                        {
                            xnew = F(a2.X, b2.X, c2.X, i, j, A.X, B.X, C.X, A.Y, B.Y, C.Y);
                            ynew = F(a2.Y, b2.Y, c2.Y, i, j, A.X, B.X, C.X, A.Y, B.Y, C.Y);

                            var color = (img as Bitmap).GetPixel(i, j);
                            var value = new RGB(color.R, color.G, color.B);
                            cimg.SetPixel((int)xnew, (int)ynew, value.Color);

                        }
                    }
                }
                catch { }
            }
            return cimg;
        }

        double F(double c1, double c2, double c3, double x,
          double y, double x1, double x2, double x3, double y1,
          double y2, double y3)
        {
            double A, B, C, c;
            A = (y2 - y1) * (c3 - c1) - (y3 - y1) * (c2 - c1);
            B = -(x2 - x1) * (c3 - c1) + (x3 - x1) * (c2 - c1);
            C = (x2 - x1) * (y3 - y1) - (x3 - x1) * (y2 - y1);
            c = c1 - (A / C) * (x - x1) - (B / C) * (y - y1);
            return Math.Max(c, 0);
        }

        #endregion

        #region Сохранение и Открытие

        private void NewProgectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            f.ActiveControl = f.textBox1;
            if (f.ShowDialog() == DialogResult.OK)
            {
                dat.Points1.Clear();
                dat.Points2.Clear();
                dat.Points3.Clear();
                dat.Tris.Clear();
                dat.Tris1.Clear();
                dat.Tris2.Clear();
                lst1.Clear();
                lst2.Clear();
                FileName = f.textBox1.Text+".fcm";
                Text = "FaceMorphing - " + FileName;
                pictureBox1.Image = null;
                pictureBox3.Image = null;
                pictureBox2.Image = null;
            }
        }

        private void OpenProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog2.ShowDialog() == DialogResult.OK)
            {
                zoom = 1;
                dat.Points1.Clear();
                dat.Points2.Clear();
                dat.Points3.Clear();
                dat.Tris.Clear();
                dat.Tris1.Clear();
                dat.Tris2.Clear();
                lst1.Clear();
                lst2.Clear();
                string s = openFileDialog2.FileName;
                //if (pictureBox1.Image != null)
                //    pictureBox1.Dispose(); 
                //if (pictureBox2.Image != null)
                //    pictureBox2.Dispose();
                Text = "FaceMorphing - " + s;
                FileName = s;
                int h1, w1, h2, w2;
                System.IO.FileStream st = new FileStream(s, FileMode.Open);
                BinaryReader br = new BinaryReader(st, Encoding.UTF8);
                h1 = (int)br.ReadInt32();
                w1 = (int)br.ReadInt32();
                if (h1 == 0 || w1 == 0)
                {
                    A = null;
                }
                else
                {
                    H = h1;
                    W = w1;
                    Bitmap bm = new Bitmap(w1, h1);
                    for (int i = 0; i < h1; i++)
                    {
                        for (int j = 0; j < w1; j++)
                        {
                            byte[] b = new byte[4];
                            b[0] = br.ReadByte();
                            b[1] = br.ReadByte();
                            b[2] = br.ReadByte();
                            b[3] = br.ReadByte();
                            bm.SetPixel(j, i, new RGB(b[1], b[2], b[3], b[0]).Color);
                        }
                    }
                    A = new Bitmap(bm);
                    pictureBox1.Image = new Bitmap(A);
                    A.Save("Image1.jpg");
                    pictureBox3.Image = new Bitmap(A);
                }
                h2 = (int)br.ReadInt32();
                w2 = (int)br.ReadInt32();
                if (h2 == 0 || w2 == 0)
                {
                    B = null;
                }
                else
                {
                    H = h2;
                    W = w2;
                    Bitmap bm = new Bitmap(w2, h2);
                    for (int i = 0; i < h2; i++)
                    {
                        for (int j = 0; j < w2; j++)
                        {

                            byte[] b = new byte[4];
                            b[0] = br.ReadByte();
                            b[1] = br.ReadByte();
                            b[2] = br.ReadByte();
                            b[3] = br.ReadByte();
                            bm.SetPixel(j, i, new RGB(b[1], b[2], b[3], b[0]).Color);
                        }
                    }
                    B = new Bitmap(bm);
                    pictureBox2.Image = new Bitmap(B);
                    B.Save("Image2.jpg");
                }
                int q1, q2, q3, q4;
                int g = (int)br.ReadInt32();
                for (int i = 0; i < g; i++)
                {
                    q1 = br.ReadInt32();
                    q2 = br.ReadInt32();
                    q3 = br.ReadInt32();
                    q4 = br.ReadInt32();
                    if(i>=4)
                    {
                        lst1.Add(new Triangulator.Geometry.Point(q1, q2));
                        lst2.Add (new Triangulator.Geometry.Point(q3, q4));
                    }
                    dat.addPoint(new Triangulator.Geometry.Point(q1, q2), new Triangulator.Geometry.Point(q3, q4));
                }
                
                saveFileDialog1.FileName = Path.ChangeExtension(s,"fcm");
                openFileDialog2.FileName = "";
                for (int i = 0; i < lst1.Count; i++)
                {
                        Graphics gg1,gg2;
                        gg1 = Graphics.FromImage(pictureBox1.Image);
                        gg2 = Graphics.FromImage(pictureBox2.Image);
                        gg1.FillRectangle(Brushes.Red, (float)lst1[i].X - 2, (float)lst1[i].Y - 2, 5, 5);
                        gg2.FillRectangle(Brushes.Red, (float)lst2[i].X - 2, (float)lst2[i].Y - 2, 5, 5);
                        this.Refresh();
                }
                if (h1 != 0 && w1 != 0)
                {
                    pictureBox1.Image = (A as Bitmap).Clone(new Rectangle(new System.Drawing.Point(0, 0), new Size(W, H)), PixelFormat.Format24bppRgb);
                    A = (A as Bitmap).Clone(new Rectangle(new System.Drawing.Point(0, 0), new Size(W, H)), PixelFormat.Format24bppRgb);
                }
                else
                {
                    A = null;
                    pictureBox1.Image = null;
                    pictureBox3.Image = null;
                }
                if (h2 != 0 && w2 != 0)
                {
                    pictureBox2.Image = (B as Bitmap).Clone(new Rectangle(new System.Drawing.Point(0, 0), new Size(W, H)), PixelFormat.Format24bppRgb);
                    B = (B as Bitmap).Clone(new Rectangle(new System.Drawing.Point(0, 0), new Size(W, H)), PixelFormat.Format24bppRgb);
                }
                else 
                {
                    pictureBox2.Image = null;
                    B = null;
                }
                dat.ReTriangle();
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (FileName == "" || FileName == null)
            { 
                if(saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    string s = saveFileDialog1.FileName;
                    FileName = s;
                    Text = "FaceMorphing - " + s;
                    System.IO.FileStream st = new FileStream(s, FileMode.Create);
                    BinaryWriter br = new BinaryWriter(st, Encoding.UTF8);
                    int h, w;
                    if (A == null)
                    {
                        h = w = 0;
                    }
                    else
                    {
                        h = A.Height;
                        w = A.Width;
                    }
                    br.Write((Int32)h);
                    br.Write((Int32)w);
                    Color rgb = new Color();
                    for (int i = 0; i < h; i++)
                    {
                        for (int j = 0; j < w; j++)
                        {
                            rgb = A.GetPixel(j, i);
                            br.Write(rgb.A);
                            br.Write(rgb.R);
                            br.Write(rgb.G);
                            br.Write(rgb.B);
                        }
                    } 
                    if (B == null)
                    {
                        h = w = 0;
                    }
                    else
                    {
                        h = B.Height;
                        w = B.Width;
                    }
                    br.Write((Int32)h);
                    br.Write((Int32)w);
                    for (int i = 0; i < h; i++)
                    {
                        for (int j = 0; j < w; j++)
                        {
                            rgb = B.GetPixel(j, i);
                            br.Write(rgb.A);
                            br.Write(rgb.R);
                            br.Write(rgb.G);
                            br.Write(rgb.B);
                        }
                    }
                    int d = dat.Points1.Count;
                    br.Write((Int32)d);
                    for (int i = 0; i < d; i++)
                    {
                        br.Write((Int32)dat.Points1[i].X);
                        br.Write((Int32)dat.Points1[i].Y);
                        br.Write((Int32)dat.Points2[i].X);
                        br.Write((Int32)dat.Points2[i].Y);
                    }
                }
            }
            else
            {
                System.IO.FileStream st = new FileStream(FileName, FileMode.Create);
                BinaryWriter br = new BinaryWriter(st, Encoding.UTF8);
                int h, w;
                if (A == null)
                {
                    h = w = 0;
                }
                else
                {
                    h = A.Height;
                    w = A.Width;
                }
                br.Write((Int32)h);
                br.Write((Int32)w);
                Color rgb = new Color();
                for (int i = 0; i < h; i++)
                {
                    for (int j = 0; j <w; j++)
                    {
                        rgb = A.GetPixel(j, i);
                        br.Write(rgb.A);
                        br.Write(rgb.R);
                        br.Write(rgb.G);
                        br.Write(rgb.B);
                    }
                } 
                if (B == null)
                {
                    h = w = 0;
                }
                else
                {
                    h = B.Height;
                    w = B.Width;
                }
                br.Write((Int32)h);
                br.Write((Int32)w);
                for (int i = 0; i < h; i++)
                {
                    for (int j = 0; j < w; j++)
                    {
                        rgb = B.GetPixel(j, i);
                        br.Write(rgb.A);
                        br.Write(rgb.R);
                        br.Write(rgb.G);
                        br.Write(rgb.B);
                    }
                }
                int f = dat.Points1.Count;
                br.Write((Int32)f);
                for (int i = 0; i < f; i++)
                {
                    br.Write((Int32)dat.Points1[i].X);
                    br.Write((Int32)dat.Points1[i].Y);
                    br.Write((Int32)dat.Points2[i].X);
                    br.Write((Int32)dat.Points2[i].Y);
                }
            }
        }

        private void saveAsProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string s = saveFileDialog1.FileName;
                FileName = s;
                Text = "FaceMorphing - " + s;
                System.IO.FileStream st = new FileStream(s, FileMode.Create);
                BinaryWriter br = new BinaryWriter(st, Encoding.UTF8);
                int h, w;
                if (A == null)
                {
                    h = w = 0;
                }
                else
                {
                    h = A.Height;
                    w = A.Width;
                }
                br.Write((Int32)h);
                br.Write((Int32)w);
                Color rgb = new Color();
                for (int i = 0; i < h; i++)
                {
                    for (int j = 0; j < w; j++)
                    {
                        rgb = A.GetPixel(j, i);
                        br.Write(rgb.A);
                        br.Write(rgb.R);
                        br.Write(rgb.G);
                        br.Write(rgb.B);
                    }
                } 
                if (B == null)
                {
                    h = w = 0;
                }
                else
                {
                    h = B.Height;
                    w = B.Width;
                }
                br.Write((Int32)h);
                br.Write((Int32)w);
                for (int i = 0; i < h; i++)
                {
                    for (int j = 0; j < w; j++)
                    {
                        rgb = B.GetPixel(j, i);
                        br.Write(rgb.A);
                        br.Write(rgb.R);
                        br.Write(rgb.G);
                        br.Write(rgb.B);
                    }
                }
                int f = dat.Points1.Count;
                br.Write((Int32)f);
                for (int i = 0; i < f; i++)
                {
                    br.Write((Int32)dat.Points1[i].X);
                    br.Write((Int32)dat.Points1[i].Y);
                    br.Write((Int32)dat.Points2[i].X);
                    br.Write((Int32)dat.Points2[i].Y);
                }
            }
        }

        private void openSecondImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "Loading second Image";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string s = openFileDialog1.FileName;
                B = new Bitmap(s);
                int h = B.Height;
                int w = B.Width;
                if (h >= 3000 || w >= 3000)
                {
                    MessageBox.Show("Размер изображения превышает допустимые нормы\nЗагрузите другое изображение","Ошибка");                       
                }      
                else
                {
                    dat.Clear();
                    lst1.Clear();
                    lst2.Clear();  
                    if (pictureBox1.Image == null)
                    {
                        if (h <= w && w > 500)
                        {
                            h = h * 500 / w;
                            w = 500;
                        }
                        if (h > w && h > 500)
                        {
                            w = w * 500 / h;
                            h = 500;
                        }
                        // create filter
                        ResizeBilinear filter = new ResizeBilinear(w,h);
                        // apply the filter
                        B = filter.Apply(B);
                        H = h;
                        W = w;
                    } 
                    else
                    {
                        // create filter
                        ResizeBilinear filter = new ResizeBilinear(W, H);
                        // apply the filter
                        B = filter.Apply(B);
                        
                        dat.addPoint(new Triangulator.Geometry.Point(0, 0), new Triangulator.Geometry.Point(0, 0));
                        dat.addPoint(new Triangulator.Geometry.Point(0, H - 1), new Triangulator.Geometry.Point(0, H - 1));
                        dat.addPoint(new Triangulator.Geometry.Point(W - 1, 0), new Triangulator.Geometry.Point(W - 1, 0));
                        dat.addPoint(new Triangulator.Geometry.Point(W - 1, H - 1), new Triangulator.Geometry.Point(W - 1, H - 1));
                    }

                    ResizeBilinear fil = new ResizeBilinear((int)(W * zoom), (int)(H * zoom));
                    // apply the filter
                    pictureBox2.Image = fil.Apply(B);
                    pictureBox2.Invalidate();
                    B.Save("Image2.jpg"); 
                    Text = "Image - " + s;
                }
            }
            toolStripStatusLabel1.Text = " ";
        }

        private void openFirstImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "Loading first Image";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string s = openFileDialog1.FileName;
                A = new Bitmap(s);
                int h = A.Height;
                int w = A.Width;
                if (h >= 3000 || w >= 3000)
                {
                    MessageBox.Show("Размер изображения превышает допустимые нормы\nЗагрузите другое изображение", "Ошибка");
                }
                else
                {
                    dat.Clear();
                    lst1.Clear();
                    lst2.Clear();
                    if (pictureBox2.Image == null)
                    {
                        if (h <= w && w > 500)
                        {
                            h = h * 500 / w;
                            w = 500;
                        }
                        if (h > w && h > 500)
                        {
                            w = w * 500 / h;
                            h = 500;
                        }
                        // create filter
                        ResizeBilinear filter = new ResizeBilinear(w, h);
                        // apply the filter
                        A = filter.Apply(A);
                        H = h;
                        W = w;
                    }
                    else
                    {
                        // create filter
                        ResizeBilinear filter = new ResizeBilinear(W, H);
                        // apply the filter
                        A = filter.Apply(A);
                        //for (int i = 0; i < W; i ++ )
                        //{
                        //    for (int j = 0; j < H; j++)
                        //    {
                        //        if (j >= newImage.Height)
                        //        {
                        //            B.SetPixel(i, j, Color.White);
                        //        }
                        //        else 
                        //        {
                        //            B.SetPixel(i, j, newImage.GetPixel(i, j));
                        //        }
                        //    }
                        //}

                        dat.addPoint(new Triangulator.Geometry.Point(0, 0), new Triangulator.Geometry.Point(0, 0));
                        dat.addPoint(new Triangulator.Geometry.Point(0, H - 1), new Triangulator.Geometry.Point(0, H - 1));
                        dat.addPoint(new Triangulator.Geometry.Point(W - 1, 0), new Triangulator.Geometry.Point(W - 1, 0));
                        dat.addPoint(new Triangulator.Geometry.Point(W - 1, H - 1), new Triangulator.Geometry.Point(W - 1, H - 1));
                    }

                    ResizeBilinear fil = new ResizeBilinear((int)(W * zoom), (int)(H * zoom));
                    // apply the filter
                    pictureBox1.Image = fil.Apply(A);
                    pictureBox3.Image = fil.Apply(A);
                    pictureBox1.Invalidate();
                    A.Save("Image1.jpg");
                    Text = "Image - " + s;
                }
                toolStripStatusLabel1.Text = " ";
            }
        }

        #endregion

        #region Help

        private void helpToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            // Новый процесс
            Process p = new Process();
            // Инициализация параметров запуска, передача пути к файлу
            p.StartInfo = new ProcessStartInfo(path + "HelpFaceMorphing.chm");
            // Запуск
            try
            {
                p.Start();
            }
            catch 
            {
                MessageBox.Show("Не удается найти файл справки","Ошибка");
            }
        }

        private void aboutFaceMorphingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
@"FaceMorphing  -  Профессиональная утилита анимирующая морфинг 
человеческих лиц

Версия - 1.1.1.1

FaceMorphing   распространяется   бесплатно  и  может  свободно 
использоваться. Коммерческое использование программы запрещено. 

Автор  идеи  -  Петрухин  Алексей  Владимирович,   доцент  кафедры 
САПРиПК Волгоградского Государственного Технического Университета.

Разработчики - Сомов Василий, Рогудеев Антон, Константинов Василий, 
Лобачева Ольга, Корникова Виолетта

Поддержка - vassuv@mail.ru", "About FaceMorphing",MessageBoxButtons.OK);
        }

        #endregion

        #region Coбытия

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            if (tabControl1.SelectedIndex == 0)
            {
                openFirstImageToolStripMenuItem_Click(pictureBox1, null);
            }
            else if (tabControl1.SelectedIndex == 1)
            {
                openSecondImageToolStripMenuItem_Click(pictureBox2, null);
            }
        }
      
        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            if (numericUpDown1.Enabled)
                trackBar1.Maximum = (int)numericUpDown1.Value + 1;
            else
                numericUpDown1.Value = gn;
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            location = e.Location;
            about.Text = e.Location.ToString();
            YelloPoint = 0;
          
            if (tabControl1.SelectedIndex == 0)
            {
                for(int i = 0 ; i <  lst1.Count; i ++)
                {
                    if (i != DeletePoint)
                    {
                        Graphics g;
                        g = Graphics.FromImage(pictureBox1.Image);
                        g.FillRectangle(Brushes.Red, (float)lst1[i].X - 2, (float)lst1[i].Y - 2, 5, 5);
                        this.Refresh();
                    }
                    if (lst1[i].X >= location.X-2 && lst1[i].X <= location.X+2 && 
                        lst1[i].Y >= location.Y-2 && lst1[i].Y <= location.Y+2)
                    {
                        DeletePoint = i;
                        YelloPoint = 1;
                        Graphics g;
                        g = Graphics.FromImage(pictureBox1.Image);
                        g.FillRectangle(Brushes.Yellow, (float)lst1[i].X - 2, (float)lst1[i].Y - 2, 5, 5);
                        this.Refresh();
                    }
                }
            }
            else if (tabControl1.SelectedIndex == 1)
            { 
                for (int i = 0; i < lst2.Count; i++)
                {
                    if (i != DeletePoint)
                    {
                        Graphics g;
                        g = Graphics.FromImage(pictureBox2.Image);
                        g.FillRectangle(Brushes.Red, (float)lst2[i].X - 2, (float)lst2[i].Y - 2, 5, 5);
                        this.Refresh();
                    }
                    if (lst2[i].X >= location.X-2 && lst2[i].X <= location.X+2 && 
                        lst2[i].Y >= location.Y-2 && lst2[i].Y <= location.Y+2)
                    {
                        DeletePoint = i;
                        YelloPoint = 1;
                        Graphics g;
                        g = Graphics.FromImage(pictureBox2.Image);
                        g.FillRectangle(Brushes.Yellow, (float)lst2[i].X - 2, (float)lst2[i].Y - 2, 5, 5);
                        this.Refresh();
                    }
                    else
                        YelloPoint = 0;
                }
            } 
            //for (int lk = 0; lk < dat.Tris1.Count; lk++)
            //{
            //    Graphics gr = pictureBox1.CreateGraphics();
            //    Pen p = new Pen(Color.Blue, 5);// цвет линии и ширина
            //    System.Drawing.Point p1 = new System.Drawing.Point((int)dat.Points1[dat.Tris1[lk].p1].X, (int)dat.Points1[dat.Tris1[lk].p1].Y);// первая точка
            //    System.Drawing.Point p2 = new System.Drawing.Point((int)dat.Points1[dat.Tris1[lk].p2].X, (int)dat.Points1[dat.Tris1[lk].p2].Y);// 2 точка 
            //    gr.DrawLine(p, p1, p2);// рисуем линию
            //    p1 = new System.Drawing.Point((int)dat.Points1[dat.Tris1[lk].p1].X, (int)dat.Points1[dat.Tris1[lk].p1].Y);// первая точка
            //    p2 = new System.Drawing.Point((int)dat.Points1[dat.Tris1[lk].p3].X, (int)dat.Points1[dat.Tris1[lk].p3].Y);// 2 точка 
            //    gr.DrawLine(p, p1, p2);// рисуем линию
            //    p1 = new System.Drawing.Point((int)dat.Points1[dat.Tris1[lk].p3].X, (int)dat.Points1[dat.Tris1[lk].p3].Y);// первая точка
            //    p2 = new System.Drawing.Point((int)dat.Points1[dat.Tris1[lk].p2].X, (int)dat.Points1[dat.Tris1[lk].p2].Y);// 2 точка
            //    gr.DrawLine(p, p1, p2);// рисуем линию
            //    gr.Dispose();// освобождаем все ресурсы, связанные с отрисовкой
            //}
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            for (int i = 0; i < lst1.Count; i++)
            {
                Graphics gg1, gg2;
                gg1 = Graphics.FromImage(pictureBox1.Image);
                gg2 = Graphics.FromImage(pictureBox2.Image);
                gg1.FillRectangle(Brushes.Red, (float)lst1[i].X - 2, (float)lst1[i].Y - 2, 5, 5);
                gg2.FillRectangle(Brushes.Red, (float)lst2[i].X - 2, (float)lst2[i].Y - 2, 5, 5);
                this.Refresh();
            }
            PictureBox x = (sender as PictureBox);
            if(x.Image != null){
                Graphics g;
                about.Text = x.ImageLocation;
                int _x = location.X - 2;
                int _y = location.Y - 2;


                if (tabControl1.SelectedIndex == 0)
                {
                    if (dot1.ID == 0)
                    {
                        //pictureBox2_MouseDown(sender, e);
                        dot1 = new Triangulator.Geometry.Point(location.X, location.Y, 1);
                        g = Graphics.FromImage(x.Image);
                        g.FillRectangle(Brushes.Red, _x, _y, 5, 5);
                        this.Refresh();
                        if (dot1.ID != 0 && dot2.ID != 0)
                        {
                            lst1.Add(dot1);
                            lst2.Add(dot2);
                            dat.addPoint(new Triangulator.Geometry.Point(dot1.X, dot1.Y),
                                         new Triangulator.Geometry.Point(dot2.X, dot2.Y));
                            dot1 = new Triangulator.Geometry.Point(0, 0, 0);
                            dot2 = new Triangulator.Geometry.Point(0, 0, 0);
                            dat.ReTriangle();
              
                        }
                    }
                    tabControl1.SelectTab(1);
                    toolStripStatusLabel1.Text = "Add Control Point in Second Image";
                }
                else if (tabControl1.SelectedIndex == 1)
                {
                    if (dot2.ID == 0)
                    {
                        dot2 = new Triangulator.Geometry.Point(location.X, location.Y, 1);
                        g = Graphics.FromImage(x.Image);
                        g.FillRectangle(Brushes.Red, _x, _y, 5, 5);
                        this.Refresh();
                        if (dot1.ID != 0 && dot2.ID != 0)
                        {
                            lst1.Add(dot1);
                            lst2.Add(dot2);
                            dat.addPoint(new Triangulator.Geometry.Point(dot1.X, dot1.Y), 
                                         new Triangulator.Geometry.Point(dot2.X, dot2.Y));
                            dot1 = new Triangulator.Geometry.Point(0, 0, 0);
                            dot2 = new Triangulator.Geometry.Point(0, 0, 0); 
                            dat.ReTriangle();
                        }
                    }
                    toolStripStatusLabel1.Text = "";
                    tabControl1.SelectTab(0);
                }
            }
        }  

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            for (int i = 0; i < lst1.Count; i++)
            {
                Graphics gg1, gg2;
                gg1 = Graphics.FromImage(pictureBox1.Image);
                gg2 = Graphics.FromImage(pictureBox2.Image);
                gg1.FillRectangle(Brushes.Red, (float)lst1[i].X - 2, (float)lst1[i].Y - 2, 5, 5);
                gg2.FillRectangle(Brushes.Red, (float)lst2[i].X - 2, (float)lst2[i].Y - 2, 5, 5);
                this.Refresh();
            }
            if (YelloPoint != 0)
            {
                if (tabControl1.SelectedIndex == 0)
                {
                    dat.delPoint(new Triangulator.Geometry.Point(lst1[DeletePoint].X / zoom, lst1[DeletePoint].Y / zoom), 1, 1);
                    lst1.RemoveAt(DeletePoint);
                    lst2.RemoveAt(DeletePoint);
                    for (int i = 0; i < lst1.Count; i++)
                    {
                        Graphics g;
                        g = Graphics.FromImage(pictureBox2.Image);
                        g.FillRectangle(Brushes.Yellow, (float)lst1[i].X - 2, (float)lst1[i].Y - 2, 5, 5);
                        this.Refresh();
                    }
                    YelloPoint = 0;

                    ResizeBilinear filter = new ResizeBilinear((int)(A.Width * zoom), (int)(A.Height * zoom));
                    // apply the filter
                    pictureBox1.Image = filter.Apply(A);
                    // apply the filter
                    pictureBox2.Image = filter.Apply(B);
                    // apply the filter
                    pictureBox3.Image = filter.Apply((Bitmap)pictureBox3.Image);
                }
                else if (tabControl1.SelectedIndex == 1)
                {
                    dat.delPoint(new Triangulator.Geometry.Point(lst2[DeletePoint].X / zoom, lst2[DeletePoint].Y / zoom), 2, 2);
                    lst2.RemoveAt(DeletePoint);
                    lst1.RemoveAt(DeletePoint);
                    for (int i = 0; i < lst2.Count; i++)
                    {
                        Graphics g;
                        g = Graphics.FromImage(pictureBox2.Image);
                        g.FillRectangle(Brushes.Yellow, (float)lst1[i].X - 2, (float)lst2[i].Y - 2, 5, 5);
                        this.Refresh();
                    }
                    YelloPoint = 0;

                    ResizeBilinear filter = new ResizeBilinear((int)(A.Width * zoom), (int)(A.Height * zoom));
                    // apply the filter
                    pictureBox1.Image = filter.Apply(A);
                    // apply the filter
                    pictureBox2.Image = filter.Apply(B);
                    // apply the filter
                    pictureBox3.Image = filter.Apply((Bitmap)pictureBox3.Image);
                }  
            }
        }
      
        #endregion

        #region AutoControlPoints

        private void autoControlPointsToolStripMenuItem_Click(object sender, EventArgs ev)
        {

            if (A == null || B == null)
            {
                MessageBox.Show("Отсутствует первое и/или второе изображение","Ошибка");
                return;
            } dat.Points1.Clear();
            dat.Points2.Clear();
            dat.Points3.Clear();
            dat.Tris1.Clear();
            dat.Tris2.Clear();
            dat.Tris.Clear();
            lst1.Clear();
            lst2.Clear(); 
            
            dat.addPoint(new Triangulator.Geometry.Point(0, 0), new Triangulator.Geometry.Point(0, 0));
            dat.addPoint(new Triangulator.Geometry.Point(0, A.Height - 1), new Triangulator.Geometry.Point(0, B.Height - 1));
            dat.addPoint(new Triangulator.Geometry.Point(A.Width - 1, 0), new Triangulator.Geometry.Point(B.Width - 1, 0));
            dat.addPoint(new Triangulator.Geometry.Point(A.Width - 1, A.Height - 1), new Triangulator.Geometry.Point(B.Width - 1, B.Height - 1));

            Triangulator.Geometry.Point[] ps1 = new Triangulator.Geometry.Point[3];
            Triangulator.Geometry.Point[] ps2 = new Triangulator.Geometry.Point[3];
            A.Save("Image1.jpg");
            B.Save("Image2.jpg");
            pictureBox1.Image = new Bitmap(A);
            pictureBox2.Image = new Bitmap(B);
            bool flag = true;
            int Count = 0, i = 0;
            Stopwatch watch;
            String faceFileName = path + "haarcascade_frontalface_default.xml";
            String eyeFileName = path + "haarcascade_eye.xml";
            {
                Image<Bgr, Byte> image = new Image<Bgr, byte>("Image1.jpg"); //Read the files as an 8-bit Bgr image  
                #region HasCuda Image1
                if (GpuInvoke.HasCuda)
                {
                    using (GpuCascadeClassifier face = new GpuCascadeClassifier(faceFileName))
                    using (GpuCascadeClassifier eye = new GpuCascadeClassifier(eyeFileName))
                    {
                        watch = Stopwatch.StartNew();
                        using (GpuImage<Bgr, Byte> gpuImage = new GpuImage<Bgr, byte>(image))
                        using (GpuImage<Gray, Byte> gpuGray = gpuImage.Convert<Gray, Byte>())
                        {
                            Rectangle[] f = null;
                            i = 100;
                            while (Count != 1 && i != 0 )
                            {
                                f = face.DetectMultiScale(gpuGray, 1 + (double)((double)i/200), 10, Size.Empty);
                                Count = f.Length;
                                i--;
                            }
                            if (f.Length == 1)
                            {
                                ps1[0] = new Triangulator.Geometry.Point(f[0].X + f[0].Width / 2, f[0].Height + f[0].Y);
                                //draw the face detected in the 0th (gray) channel with blue color
                                using (GpuImage<Gray, Byte> faceImg = gpuGray.GetSubRect(f[0]))
                                {
                                    //For some reason a clone is required.
                                    //Might be a bug of GpuCascadeClassifier in opencv
                                    using (GpuImage<Gray, Byte> clone = faceImg.Clone())
                                    {
                                        Rectangle[] e = null;
                                        i = 100;
                                        while (Count != 2 && i != 0)
                                        {
                                            e = eye.DetectMultiScale(clone, 1+(double)((double)i/200), 10, Size.Empty);
                                            Count = e.Length;
                                            i--;
                                        }

                                        if (e.Length == 2)
                                        {
                                            e[0].Offset(f[0].X, f[0].Y);
                                            e[1].Offset(f[0].X, f[0].Y);
                                            if (e[0].X < e[1].X)
                                            {
                                                ps1[1] = new Triangulator.Geometry.Point(e[0].X + e[0].Width / 2, e[0].Height / 2 + e[0].Y);
                                                ps1[2] = new Triangulator.Geometry.Point(e[1].X + e[1].Width / 2, e[1].Height / 2 + e[1].Y);
                                            }
                                            else
                                            {
                                                ps1[1] = new Triangulator.Geometry.Point(e[1].X + e[1].Width / 2, e[1].Height / 2 + e[1].Y);
                                                ps1[2] = new Triangulator.Geometry.Point(e[0].X + e[0].Width / 2, e[0].Height / 2 + e[0].Y);
                                            }
                                        }
                                        else flag = false;
                                    }
                                }
                            }
                            else return;
                        }
                        watch.Stop();
                    }
                }
                #endregion
                #region Image1
                else
                {
                    //Read the HaarCascade objects
                    using (HaarCascade face = new HaarCascade(faceFileName))
                    using (HaarCascade eye = new HaarCascade(eyeFileName))
                    {
                        watch = Stopwatch.StartNew();
                        using (Image<Gray, Byte> gray = image.Convert<Gray, Byte>()) //Convert it to Grayscale
                        {
                            //normalizes brightness and increases contrast of the image
                            gray._EqualizeHist();

                            //Detect the faces  from the gray scale image and store the locations as rectangle
                            //The first dimensional is the channel
                            //The second dimension is the index of the rectangle in the specific channel
                            MCvAvgComp[] f = null;
                            i = 100;
                            Count = 0;
                            while (Count != 1 && i != 0)
                            {
                                f = face.Detect(
                                           gray,1+(double)((double)i/200),10,
                                           Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.SCALE_IMAGE,
                                           new Size(10, 10));
                                Count = f.Length;
                                i--;
                            }

                            if (f.Length == 1)
                            {
                                ps1[0] = new Triangulator.Geometry.Point(f[0].rect.X + f[0].rect.Width / 2, f[0].rect.Height + f[0].rect.Y);
                                //Set the region of interest on the faces
                                gray.ROI = f[0].rect;
                                MCvAvgComp[] e = null;
                                i = 100;
                                Count = 0;
                                while (Count != 2 && i != 0)
                                {
                                    e = eye.Detect(
                                               gray, 1 + (double)((double)i/200), 10,
                                               Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.SCALE_IMAGE,
                                               new Size(10, 10));
                                    Count = e.Length;
                                    i--;
                                } 
                                gray.ROI = Rectangle.Empty;

                                if (e.Length == 2)
                                {
                                    Rectangle[] ee = new Rectangle[] { e[0].rect, e[1].rect };
                                    ee[0].Offset(f[0].rect.X, f[0].rect.Y);
                                    ee[1].Offset(f[0].rect.X, f[0].rect.Y);
                                    if (ee[0].X < ee[1].X)
                                    {
                                        ps1[1] = new Triangulator.Geometry.Point(ee[0].X + ee[0].Width / 2, ee[0].Height / 2 + ee[0].Y);
                                        ps1[2] = new Triangulator.Geometry.Point(ee[1].X + ee[1].Width / 2, ee[1].Height / 2 + ee[1].Y);
                                    }
                                    else
                                    {
                                        ps1[1] = new Triangulator.Geometry.Point(ee[1].X + ee[1].Width / 2, ee[1].Height / 2 + ee[1].Y);
                                        ps1[2] = new Triangulator.Geometry.Point(ee[0].X + ee[0].Width / 2, ee[0].Height / 2 + ee[0].Y);
                                    }
                                }
                                else
                                    flag = false;
                            }
                            else

                                return;
                        }
                        watch.Stop();
                    }
                }
                #endregion
            }
            {
                Image<Bgr, Byte> image = new Image<Bgr, byte>("Image2.jpg"); //Read the files as an 8-bit Bgr image  
                #region HasCuda Image2
                if (GpuInvoke.HasCuda)
                {
                    using (GpuCascadeClassifier face = new GpuCascadeClassifier(faceFileName))
                    using (GpuCascadeClassifier eye = new GpuCascadeClassifier(eyeFileName))
                    {
                        watch = Stopwatch.StartNew();
                        using (GpuImage<Bgr, Byte> gpuImage = new GpuImage<Bgr, byte>(image))
                        using (GpuImage<Gray, Byte> gpuGray = gpuImage.Convert<Gray, Byte>())
                        {
                            Rectangle[] f = null;
                            i = 100;
                            while (Count != 1 && i != 0)
                            {
                                f = face.DetectMultiScale(gpuGray, 1.1, 10, Size.Empty);
                                Count = f.Length;
                                i--;
                            } 
                            if (f.Length == 1)
                            {
                                ps2[0] = new Triangulator.Geometry.Point(f[0].X + f[0].Width / 2, f[0].Height + f[0].Y);
                                //draw the face detected in the 0th (gray) channel with blue color
                                using (GpuImage<Gray, Byte> faceImg = gpuGray.GetSubRect(f[0]))
                                {
                                    //For some reason a clone is required.
                                    //Might be a bug of GpuCascadeClassifier in opencv
                                    using (GpuImage<Gray, Byte> clone = faceImg.Clone())
                                    {
                                        Rectangle[] e = null;
                                        i = 100;
                                        while (Count != 2 && i != 0)
                                        {
                                            e = eye.DetectMultiScale(clone, 1.1, 10, Size.Empty);
                                            Count = e.Length;
                                            i--;
                                        }
                                        if (e.Length == 2  && flag)
                                        {
                                            e[0].Offset(f[0].X, f[0].Y);
                                            e[1].Offset(f[0].X, f[0].Y);
                                            if (e[0].X < e[1].X)
                                            {
                                                ps2[1] = new Triangulator.Geometry.Point(e[0].X + e[0].Width / 2, e[0].Height / 2 + e[0].Y);
                                                ps2[2] = new Triangulator.Geometry.Point(e[1].X + e[1].Width / 2, e[1].Height / 2 + e[1].Y);
                                            }
                                            else
                                            {
                                                ps2[1] = new Triangulator.Geometry.Point(e[1].X + e[1].Width / 2, e[1].Height / 2 + e[1].Y);
                                                ps2[2] = new Triangulator.Geometry.Point(e[0].X + e[0].Width / 2, e[0].Height / 2 + e[0].Y);
                                            }
                                        }
                                        else flag = false;
                                    }
                                }
                            }
                            else return;
                        }
                        watch.Stop();
                    }
                }
                #endregion
                #region Image2
                else
                {
                    //Read the HaarCascade objects
                    using (HaarCascade face = new HaarCascade(faceFileName))
                    using (HaarCascade eye = new HaarCascade(eyeFileName))
                    {
                        watch = Stopwatch.StartNew();
                        using (Image<Gray, Byte> gray = image.Convert<Gray, Byte>()) //Convert it to Grayscale
                        {
                            //normalizes brightness and increases contrast of the image
                            gray._EqualizeHist();

                            //Detect the faces  from the gray scale image and store the locations as rectangle
                            //The first dimensional is the channel
                            //The second dimension is the index of the rectangle in the specific channel
                            MCvAvgComp[] f = null;
                            i = 100;
                            Count = 0;
                            while (Count != 1 && i != 0)
                            {
                                f = face.Detect(
                                           gray, 1 + (double)((double)i/200), 10,
                                           Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.SCALE_IMAGE,
                                           new Size(10, 10));
                                Count = f.Length;
                                i--;
                            }
                            if (f.Length == 1)
                            {
                                ps2[0] = new Triangulator.Geometry.Point(f[0].rect.X + f[0].rect.Width / 2, f[0].rect.Height + f[0].rect.Y);

                                //Set the region of interest on the faces
                                gray.ROI = f[0].rect;
                                MCvAvgComp[] e = null;
                                i = 100;
                                Count = 0;
                                while (Count != 2 && i != 0)
                                {
                                    e = eye.Detect(
                                               gray, 1 + (double)((double)i/200), 10,
                                               Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.SCALE_IMAGE,
                                               new Size(10, 10));
                                    Count = e.Length;
                                    i--;
                                } 
                                gray.ROI = Rectangle.Empty;

                                if (e.Length == 2 && flag)
                                {
                                    Rectangle[] ee = new Rectangle[] { e[0].rect, e[1].rect };
                                    ee[0].Offset(f[0].rect.X, f[0].rect.Y);
                                    ee[1].Offset(f[0].rect.X, f[0].rect.Y);
                                    if (ee[0].X < ee[1].X)
                                    {
                                        ps2[1] = new Triangulator.Geometry.Point(ee[0].X + ee[0].Width / 2, ee[0].Height / 2 + ee[0].Y);
                                        ps2[2] = new Triangulator.Geometry.Point(ee[1].X + ee[1].Width / 2, ee[1].Height / 2 + ee[1].Y);
                                    }
                                    else
                                    {
                                        ps2[1] = new Triangulator.Geometry.Point(ee[1].X + ee[1].Width / 2, ee[1].Height / 2 + ee[1].Y);
                                        ps2[2] = new Triangulator.Geometry.Point(ee[0].X + ee[0].Width / 2, ee[0].Height / 2 + ee[0].Y);
                                    }
                                }
                                else flag = false;
                            }
                            else return;
                        }
                        watch.Stop();
                    }
                }
                #endregion
            }
            dat.addPoint(ps1[0], ps2[0]);
            lst1.Add(ps1[0]);
            lst2.Add(ps2[0]);
            if (flag)
            {
                dat.addPoint(ps1[1], ps2[1]);
                lst1.Add(ps1[1]);
                lst2.Add(ps2[1]);

                dat.addPoint(ps1[2], ps2[2]);
                lst1.Add(ps1[2]);
                lst2.Add(ps2[2]);
            }
            if (zoom == 0.5)
                toolStripMenuItem4_Click(null, null);
            if (zoom == 1)
                toolStripMenuItem3_Click(null, null);
            if (zoom == 2)
                toolStripMenuItem2_Click(null, null); 

            for (i = 0; i < lst2.Count; i++)
            {
                    Graphics g;
                    g = Graphics.FromImage(pictureBox2.Image);
                    g.FillRectangle(Brushes.Red, (float)lst2[i].X - 2, (float)lst2[i].Y - 2, 5, 5);
                
                    g = Graphics.FromImage(pictureBox1.Image);
                    g.FillRectangle(Brushes.Red, (float)lst1[i].X - 2, (float)lst1[i].Y - 2, 5, 5);
                  
            }

            dat.ReTriangle();
        }
        
        #endregion

        #region Зуммирование

        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            if (A == null && B == null) return;
            toolStripMenuItem4.Checked = !(toolStripMenuItem3.Checked = toolStripMenuItem2.Checked = false);
            double x = 1;
            if (zoom == 1)
                x = 0.5;
            else
                if (zoom == 2)
                    x = 0.25;
            zoom = 0.5;// create filter
            for (int i = 0; i < lst1.Count; i++)
            {
                lst1[i].X *= x;
                lst1[i].Y *= x;
                lst2[i].X *= x;
                lst2[i].Y *= x;
            }
            ResizeBilinear filter = new ResizeBilinear((int)(W * zoom), (int)(H * zoom));
            // apply the filter

            if (A != null)
                pictureBox1.Image = filter.Apply(new Bitmap(A));
            if (B != null)
                pictureBox2.Image = filter.Apply(new Bitmap(B));
            if (pictureBox3.Image != null)
                pictureBox3.Image = filter.Apply((Bitmap)pictureBox3.Image);

            for (int i = 0; i < lst2.Count; i++)
            {
                Graphics g;
                g = Graphics.FromImage(pictureBox2.Image);
                g.FillRectangle(Brushes.Red, (float)lst2[i].X - 2, (float)lst2[i].Y - 2, 5, 5);

                g = Graphics.FromImage(pictureBox1.Image);
                g.FillRectangle(Brushes.Red, (float)lst1[i].X - 2, (float)lst1[i].Y - 2, 5, 5);

            }
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            if (A == null && B == null) return;
            toolStripMenuItem3.Checked = !(toolStripMenuItem4.Checked = toolStripMenuItem2.Checked = false);
            double x = 1;
            if (zoom == 0.5)
                x = 2;
            else
                if (zoom == 2)
                    x = 0.5;
            zoom = 1;
            for (int i = 0; i < lst1.Count; i++)
            {
                lst1[i].X *= x;
                lst1[i].Y *= x;
                lst2[i].X *= x;
                lst2[i].Y *= x;
                
            }
            // create filter
            ResizeBilinear filter = new ResizeBilinear((int)(W * zoom), (int)(H * zoom));
            // apply the filter

            if (A != null)
                pictureBox1.Image = filter.Apply(new Bitmap(A));
            if (B != null)
                pictureBox2.Image = filter.Apply(new Bitmap(B));
            if (pictureBox3.Image != null)
                pictureBox3.Image = filter.Apply((Bitmap)pictureBox3.Image);
            for (int i = 0; i < lst2.Count; i++)
            {
                Graphics g;
                g = Graphics.FromImage(pictureBox2.Image);
                g.FillRectangle(Brushes.Red, (float)lst2[i].X - 2, (float)lst2[i].Y - 2, 5, 5);

                g = Graphics.FromImage(pictureBox1.Image);
                g.FillRectangle(Brushes.Red, (float)lst1[i].X - 2, (float)lst1[i].Y - 2, 5, 5);

            }
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            if (A == null && B == null) return;
            toolStripMenuItem2.Checked = !(toolStripMenuItem4.Checked = toolStripMenuItem3.Checked = false);
            double x = 1;
            if (zoom == 0.5)
                x = 4;
            else
                if (zoom == 1)
                    x = 2;
            zoom = 2;
            for (int i = 0; i < lst1.Count; i++)
            {
                
                lst1[i].X *= x;
                lst1[i].Y *= x;
                lst2[i].X *= x;
                lst2[i].Y *= x;
            }
            // create filter
            ResizeBilinear filter = new ResizeBilinear((int)(W*zoom), (int)(H * zoom));
            // apply the filter
            if (A != null)
                pictureBox1.Image = filter.Apply(new Bitmap(A));
            if (B != null)
                pictureBox2.Image = filter.Apply(new Bitmap(B));
            if (pictureBox3.Image != null)
                pictureBox3.Image = filter.Apply((Bitmap)pictureBox3.Image);
            for (int i = 0; i < lst2.Count; i++)
            {
                Graphics g;
                g = Graphics.FromImage(pictureBox2.Image);
                g.FillRectangle(Brushes.Red, (float)lst2[i].X - 2, (float)lst2[i].Y - 2, 5, 5);

                g = Graphics.FromImage(pictureBox1.Image);
                g.FillRectangle(Brushes.Red, (float)lst1[i].X - 2, (float)lst1[i].Y - 2, 5, 5);

            }
           
        }
    
        #endregion

        private void toolStripStatusLabel2_Click(object sender, EventArgs e)
        {
            dat.Clear();
            pictureBox1.Image = null;
            pictureBox2.Image = null;
            pictureBox3.Image = null;
            lst1.Clear();
            lst2.Clear();
        }

    }
}
