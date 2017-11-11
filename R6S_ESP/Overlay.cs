using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using System.ServiceProcess;

namespace R6S_ESP
{
    public partial class Overlay : Form
    {
        [DllImport("user32.dll")]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        public const string window = "Rainbow Six"; 
        IntPtr handle = FindWindow(null, window);
        static RECT rect;

        private static Factory factory;
        private static HwndRenderTargetProperties RenderProperties;
        private static WindowRenderTarget device;
        private static SolidColorBrush BoxBrush;
        System.Threading.Thread thread;

        private bool BEbypass; 

        public struct RECT
        {
            public int left, top, right, bottom;
        }

        public Overlay(bool BE)
        {
            InitializeComponent();
            BEbypass = BE;
        }

        private void Overlay_Load(object sender, EventArgs e)
        {
            if (!BEbypass)
            {
                ServiceController sc = new ServiceController();
                sc.ServiceName = "BEService";
                if (sc.Status == ServiceControllerStatus.Running || sc.Status == ServiceControllerStatus.Paused) // Check if the Battleye service is running, if it is, stop it
                {
                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped);
                }
                Stuff.OpenProc();
            }
            else
            {
                Stuff.GetBaseAddress();
            }

            BackColor = Color.White;
            TransparencyKey = Color.White;
            TopMost = true;
            FormBorderStyle = FormBorderStyle.None;
            DoubleBuffered = true;

            GetWindowRect(handle, out rect);
            Size = new Size(rect.right - rect.left, rect.bottom - rect.top);
            Top = rect.top;
            Left = rect.left;

            factory = new Factory();
            RenderProperties = new HwndRenderTargetProperties
            {
                Hwnd = Handle,
                PixelSize = new SharpDX.Size2(Size.Width, Size.Height),
                PresentOptions = PresentOptions.None
            };
            device = new WindowRenderTarget(factory, new RenderTargetProperties(new PixelFormat(SharpDX.DXGI.Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied)), RenderProperties);
            BoxBrush = new SolidColorBrush(device, new RawColor4(255, 0, 0, 2f));

            thread = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(DrawingThread))
            {
                Priority = System.Threading.ThreadPriority.Highest,
                IsBackground = true
            };
            thread.Start();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            GetWindowRect(handle, out rect);
            Size = new Size(rect.right - rect.left, rect.bottom - rect.top);
            Top = rect.top;
            Left = rect.left;
            Stuff.GetEntityPosition();
            Stuff.GetHeadPosition();
        }

        public static void DrawingThread(object sender)
        {
            while (true)
            {
                while (!Stuff.IsIngame()) { }
                Task.Delay(3000).Wait();
                int gMode = Stuff.GetGameMode();
                int CurrentPlayer;
                if (gMode == 1 || gMode == 2)
                {
                    CurrentPlayer = 1;
                    Stuff.Data.EntityCount = 23;
                }
                else
                {
                    CurrentPlayer = 2;
                    Stuff.Data.EntityCount = 12;
                }
                Stuff.GetEntities();
                Stuff.GetCamera();
                while (Stuff.IsIngame())
                {
                    try
                    {
                        device.BeginDraw();
                        device.Clear(new RawColor4(Color.Transparent.R, Color.Transparent.G, Color.Transparent.B, 1));
                        device.AntialiasMode = AntialiasMode.PerPrimitive;
                        if (gMode == 1 || gMode == 2)
                        {
                            CurrentPlayer = 1;
                            Stuff.Data.EntityCount = 23;
                        }
                        else
                        {
                            CurrentPlayer = 2;
                            Stuff.Data.EntityCount = 12;
                        }
                        int SubstractAmount = CurrentPlayer;
                        int LocalPlayerTeam = Stuff.GetPlayerTeam(CurrentPlayer - 1);
                        Vector3 PlayerPos;
                        Vector3 HeadPos;
                        float width;
                        Debug.WriteLine(Stuff.Data.entities.Count);
                        for (int i = 0; i < Stuff.Data.entities.Count - SubstractAmount; i++)
                        {
                            if (Stuff.GetPlayerTeam(CurrentPlayer) != LocalPlayerTeam || gMode == 1 || gMode == 2)
                            {
                                if (Stuff.GetEntityHealth(CurrentPlayer) > 0)
                                {
                                    PlayerPos = Stuff.World2Screen(Stuff.Data.EntityPositions[CurrentPlayer]);
                                    HeadPos = Stuff.World2Screen(Stuff.Data.HeadPositions[CurrentPlayer]);
                                    width = (PlayerPos.y - HeadPos.y) / 2;
                                    if (PlayerPos.z >= 1)
                                    {
                                        device.DrawLine(new RawVector2(PlayerPos.x - width / 2, PlayerPos.y), new RawVector2(PlayerPos.x - width / 2, HeadPos.y), BoxBrush, 1f); // Left
                                        device.DrawLine(new RawVector2(PlayerPos.x - width / 2, HeadPos.y), new RawVector2(PlayerPos.x + width / 2, HeadPos.y), BoxBrush, 1f); // Top
                                        device.DrawLine(new RawVector2(PlayerPos.x + width / 2, HeadPos.y), new RawVector2(PlayerPos.x + width / 2, PlayerPos.y), BoxBrush, 1f); // Right
                                        device.DrawLine(new RawVector2(PlayerPos.x + width / 2, PlayerPos.y), new RawVector2(PlayerPos.x - width / 2, PlayerPos.y), BoxBrush, 1f); // Bottom
                                    }
                                }
                            }
                            CurrentPlayer++;
                        }
                        CurrentPlayer = 0;
                        device.EndDraw();
                    }
                    catch { }
                }
                device.BeginDraw();
                device.Clear(new RawColor4(Color.Transparent.R, Color.Transparent.G, Color.Transparent.B, 1));
                device.EndDraw();
                GC.Collect();
            }
        }
    }
}
