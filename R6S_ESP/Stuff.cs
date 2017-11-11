using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace R6S_ESP
{
    class Stuff
    {
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern int ReadProcessMemory(IntPtr hProcess, IntPtr BaseAddress, byte[] Buffer, int size, int BytesRead);

        [DllImport("psapi.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        public static extern bool EnumProcessModules(IntPtr hProcess, [Out] IntPtr lphModule, uint cb, out uint lpcbNeeded);

        [DllImport("psapi.dll")]
        static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, [Out] StringBuilder lpBaseName, [In] [MarshalAs(UnmanagedType.U4)] int nSize);

        public struct Data
        {
            public static readonly int GameManager = 0x473A3D0;
            public static readonly int GameRenderer = 0x46F0800;
            public static readonly int EntityList = 0xD0;
            public static readonly int EntityPositionOffset = 0x190;
            public static readonly int HeadPositionOffset = 0x130;
            public static readonly int EngineLink = 0xD8;
            public static readonly int Engine = 0x218;
            public static readonly int Camera = 0x38;
            public static readonly int ViewRight = 0x170;
            public static readonly int ViewUp = 0x180;
            public static readonly int ViewForward = 0x190;
            public static readonly int ViewTranslation = 0x1A0;
            public static readonly int ViewFOVX = 0x1B8;
            public static readonly int ViewFOVY = 0x1C4;
            public static readonly int EntityInfo = 0x18;
            public static readonly int MainComp = 0xB8;
            public static readonly int ChildComp = 0x8;
            public static readonly int EntityHealth = 0x108;
            public static readonly int PlayerInfo = 0x270;
            public static readonly int TeamID = 0x140;
            public static readonly int[] CheckIngame = { 0x0470F6D0, 0x1B8, 0x48, 0x80, 0x3E8 };
            public static readonly int[] GameMode = { 0x04702DC8, 0x18 };
            public static IntPtr CameraPtr;
            public static IntPtr GameHandle;
            public static List<Int64> View = new List<long>();
            public static Vector3 ViewTrans = new Vector3();
            public static List<IntPtr> entities = new List<IntPtr>();
            public static List<Vector3> EntityPositions = new List<Vector3>();
            public static List<Vector3> HeadPositions = new List<Vector3>();
            public static IntPtr BaseAddress;
            public static int EntityCount;
        }

        public static void OpenProc()
        {
            Process GameProc = Process.GetProcessesByName("RainbowSix")[0];
            Data.BaseAddress = GameProc.MainModule.BaseAddress;
            int pid = GameProc.Id;
            IntPtr handle = OpenProcess(0x0010, false, pid);
            Data.GameHandle = handle;
        }

        public static void GetBaseAddress()
        {
            IntPtr[] hMods = new IntPtr[1024];
            GCHandle gch = GCHandle.Alloc(hMods, GCHandleType.Pinned);
            IntPtr pModules = gch.AddrOfPinnedObject();
            uint uiSize = (uint)(Marshal.SizeOf(typeof(IntPtr)) * (hMods.Length));
            uint cbNeeded = 0;
            string FileName;
            if (EnumProcessModules(Data.GameHandle, pModules, uiSize, out cbNeeded))
            {
                int uiTotalNumberofModules = (int)(cbNeeded / (Marshal.SizeOf(typeof(IntPtr))));

                for (int i = 0; i < uiTotalNumberofModules; i++)
                {
                    StringBuilder str = new StringBuilder(1024);

                    GetModuleFileNameEx(Data.GameHandle, hMods[i], str, (str.Capacity));
                    FileName = str.ToString().Substring(str.ToString().Length-14);
                    if (FileName == "RainbowSix.exe")
                    {
                        Data.BaseAddress = hMods[i];
                        break;
                    }
                }
            }
            gch.Free();
        }

        public static void GetCamera()
        {
            byte[] buffer = new byte[8];
            IntPtr BaseAddress = Data.BaseAddress;
            IntPtr CurPtr = new IntPtr();
            for(int i = 0; i <= 4; i++)
            {
                switch (i)
                {
                    case 0:
                        CurPtr = IntPtr.Add(BaseAddress, Data.GameRenderer);
                        break;

                    case 1:
                        CurPtr = IntPtr.Add(CurPtr, 0x0);
                        break;

                    case 2:
                        CurPtr = IntPtr.Add(CurPtr, Data.EngineLink);
                        break;

                    case 3:
                        CurPtr = IntPtr.Add(CurPtr, Data.Engine);
                        break;

                    case 4:
                        CurPtr = IntPtr.Add(CurPtr, Data.Camera);
                        break;
                }
                ReadProcessMemory(Data.GameHandle, CurPtr, buffer, buffer.Length, 0);
                CurPtr = new IntPtr(BitConverter.ToInt64(buffer, 0));
            }
            Data.CameraPtr = CurPtr;
        }

        public static Vector3 GetViewShit(int ViewOffset)
        {
            byte[] buffer = new byte[8];
            Vector3 data = new Vector3();
            IntPtr CurPtr = IntPtr.Add(Data.CameraPtr, ViewOffset);
            ReadProcessMemory(Data.GameHandle, CurPtr, buffer, buffer.Length, 0);
            data.x = BitConverter.ToSingle(buffer, 0);
            CurPtr = IntPtr.Add(Data.CameraPtr, ViewOffset + 0x4);
            ReadProcessMemory(Data.GameHandle, CurPtr, buffer, buffer.Length, 0);
            data.y = BitConverter.ToSingle(buffer, 0);
            CurPtr = IntPtr.Add(Data.CameraPtr, ViewOffset + 0x8);
            ReadProcessMemory(Data.GameHandle, CurPtr, buffer, buffer.Length, 0);
            data.z = BitConverter.ToSingle(buffer, 0);
            return data;
        }

        public static float GetViewFOV(int coordinate)
        {
            byte[] buffer = new byte[8];
            IntPtr CurPtr = IntPtr.Add(Data.CameraPtr, coordinate);
            ReadProcessMemory(Data.GameHandle, CurPtr, buffer, buffer.Length, 0);
            return BitConverter.ToSingle(buffer, 0);
        }

        public static void GetEntities()
        {
            IntPtr GameBaseAddress = Data.BaseAddress;
            IntPtr GameManager = IntPtr.Add(GameBaseAddress, Data.GameManager);
            byte[] buffer = new byte[8];
            ReadProcessMemory(Data.GameHandle, GameManager, buffer, buffer.Length, 0);
            GameManager = new IntPtr(BitConverter.ToInt64(buffer, 0));
            IntPtr EntityList = IntPtr.Add(GameManager, Data.EntityList);
            ReadProcessMemory(Data.GameHandle, EntityList, buffer, buffer.Length, 0);
            EntityList = new IntPtr(BitConverter.ToInt64(buffer, 0));
            Int64 entityint = 1;
            int i = 0;
            while(true)
            {
                IntPtr entity = IntPtr.Add(EntityList, i*0x8);
                ReadProcessMemory(Data.GameHandle, entity, buffer, buffer.Length, 0);
                entity = new IntPtr(BitConverter.ToInt64(buffer, 0));
                entityint = entity.ToInt64();
                if (i > Data.EntityCount) break;
                Data.entities.Add(entity);
                i++;
            }
        }

        public static void GetEntityPosition()
        {
            byte[] buffer = new byte[8];
            List<Vector3> EntityPositions = new List<Vector3>();
            for (int i=0; i<Data.entities.Count; i++)
            {
                float posx = 0;
                float posy = 0;
                float posz = 0;
                for (int offset = Data.EntityPositionOffset; offset <= Data.EntityPositionOffset + 0x8; offset += 0x4)
                {
                    IntPtr CurEntity = Data.entities[i];
                    CurEntity = IntPtr.Add(CurEntity, offset);
                    ReadProcessMemory(Data.GameHandle, CurEntity, buffer, buffer.Length, 0);
                    float position = BitConverter.ToSingle(buffer, 0);
                    if(offset == Data.EntityPositionOffset)
                        posx = position;
                    else if (offset == Data.EntityPositionOffset + 0x4)
                        posy = position;
                    else if (offset == Data.EntityPositionOffset + 0x8)
                        posz = position;
                }
                Vector3 CurPosition = new Vector3();
                CurPosition.x = posx;
                CurPosition.y = posy;
                CurPosition.z = posz;
                EntityPositions.Add(CurPosition);
            }
            Data.EntityPositions = EntityPositions;
        }

        public static void GetHeadPosition()
        {
            byte[] buffer = new byte[8];
            List<Vector3> HeadPositions = new List<Vector3>();
            for (int i = 0; i < Data.entities.Count; i++)
            {
                float posx = 0;
                float posy = 0;
                float posz = 0;
                for (int offset = Data.HeadPositionOffset; offset <= Data.HeadPositionOffset + 0x8; offset += 0x4)
                {
                    IntPtr CurEntity = Data.entities[i];
                    CurEntity = IntPtr.Add(CurEntity, offset);
                    ReadProcessMemory(Data.GameHandle, CurEntity, buffer, buffer.Length, 0);
                    float position = BitConverter.ToSingle(buffer, 0);
                    if (offset == Data.HeadPositionOffset)
                        posx = position;
                    else if (offset == Data.HeadPositionOffset + 0x4)
                        posy = position;
                    else if (offset == Data.HeadPositionOffset + 0x8)
                        posz = position;
                }
                Vector3 CurPosition = new Vector3();
                CurPosition.x = posx;
                CurPosition.y = posy;
                CurPosition.z = posz;
                HeadPositions.Add(CurPosition);
            }
            Data.HeadPositions = HeadPositions;
        }

        public static Vector3 World2Screen(Vector3 position)
        {
            Vector3 temp = new Vector3();
            temp.x = position.x - GetViewShit(Data.ViewTranslation).x;
            temp.y = position.y - GetViewShit(Data.ViewTranslation).y;
            temp.z = position.z - GetViewShit(Data.ViewTranslation).z;

            Vector3 ViewForward = new Vector3();
            ViewForward.x = GetViewShit(Data.ViewForward).x * -1;
            ViewForward.y = GetViewShit(Data.ViewForward).y * -1;
            ViewForward.z = GetViewShit(Data.ViewForward).z * -1;

            float x = temp.Dot(GetViewShit(Data.ViewRight));
            float y = temp.Dot(GetViewShit(Data.ViewUp));
            float z = temp.Dot(ViewForward);

            Vector3 DrawDisSheit = new Vector3();
            float fovx = GetViewFOV(Data.ViewFOVX);
            float fovy = GetViewFOV(Data.ViewFOVY);
            if (fovx == 0)
            {
                fovx = 1.78f;
                fovy = 1f;
            }
            DrawDisSheit.x = (float)((1920 / 2) * (1 + x / fovx / z));
            DrawDisSheit.y = (1080 / 2) * (1 - y / fovy / z);
            DrawDisSheit.z = z;
            return DrawDisSheit;
        }

        public static int GetEntityHealth(int entity)
        {
            byte[] buffer = new byte[8];
            IntPtr CurPtr = IntPtr.Add(Data.entities[entity], Data.EntityInfo);
            ReadProcessMemory(Data.GameHandle, CurPtr, buffer, buffer.Length, 0);
            CurPtr = new IntPtr(BitConverter.ToInt64(buffer, 0));
            CurPtr = IntPtr.Add(CurPtr, Data.MainComp);
            ReadProcessMemory(Data.GameHandle, CurPtr, buffer, buffer.Length, 0);
            CurPtr = new IntPtr(BitConverter.ToInt64(buffer, 0));
            CurPtr = IntPtr.Add(CurPtr, Data.ChildComp);
            ReadProcessMemory(Data.GameHandle, CurPtr, buffer, buffer.Length, 0);
            CurPtr = new IntPtr(BitConverter.ToInt64(buffer, 0));
            CurPtr = IntPtr.Add(CurPtr, Data.EntityHealth);
            ReadProcessMemory(Data.GameHandle, CurPtr, buffer, buffer.Length, 0);
            return BitConverter.ToInt32(buffer, 0);
        }

        public static bool IsIngame()
        {
            byte[] buffer = new byte[8];
            IntPtr CurPtr = Data.BaseAddress;
            for (int i = 0; i < Data.CheckIngame.Length; i++)
            {
                CurPtr = IntPtr.Add(CurPtr, Data.CheckIngame[i]);
                ReadProcessMemory(Data.GameHandle, CurPtr, buffer, buffer.Length, 0);
                CurPtr = new IntPtr(BitConverter.ToInt64(buffer, 0));
            }
            if (CurPtr == IntPtr.Zero)
                return false;
            else
                return true;
        }

        public static int GetGameMode()
        {
            byte[] buffer = new byte[8];
            IntPtr CurPtr = IntPtr.Add(Data.BaseAddress, Data.GameMode[0]);
            ReadProcessMemory(Data.GameHandle, CurPtr, buffer, buffer.Length, 0);
            CurPtr = new IntPtr(BitConverter.ToInt64(buffer, 0));
            CurPtr = IntPtr.Add(CurPtr, Data.GameMode[1]);
            ReadProcessMemory(Data.GameHandle, CurPtr, buffer, buffer.Length, 0);
            return BitConverter.ToInt32(buffer, 0);
        }

        public static int GetPlayerTeam(int CurPlayer)
        {
            byte[] buffer = new byte[8];
            IntPtr CurPtr = IntPtr.Add(Data.entities[CurPlayer], Data.PlayerInfo);
            ReadProcessMemory(Data.GameHandle, CurPtr, buffer, buffer.Length, 0);
            CurPtr = new IntPtr(BitConverter.ToInt64(buffer, 0));
            ReadProcessMemory(Data.GameHandle, CurPtr, buffer, buffer.Length, 0);
            CurPtr = new IntPtr(BitConverter.ToInt64(buffer, 0));
            CurPtr = IntPtr.Add(CurPtr, Data.TeamID);
            ReadProcessMemory(Data.GameHandle, CurPtr, buffer, buffer.Length, 0);
            return BitConverter.ToInt32(buffer, 0);
        }
    }
}
