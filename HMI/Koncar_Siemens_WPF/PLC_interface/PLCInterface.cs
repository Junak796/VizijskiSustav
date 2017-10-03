using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Snap7;

namespace VizijskiSustavWPF 
{



    public class PLCInterfaceEventArgs : EventArgs
    {
        private Control controlData;

        public Control ControlData
        {
            get { return controlData; }
            set { controlData = value; }
        }

        private Status statusData;

        public Status StatusData
        {
            get { return statusData; }
            set { statusData = value; }
        }

        public byte[]  CyclicStatusBuffer { get; set; } 
        public byte[] CyclicControlBuffer { get; set; }
    }

    public class OnlineMarkerEventArgs : EventArgs
    {
        bool onlineMark;

        public bool OnlineMark
        {
            get { return onlineMark; }
            set { onlineMark = value; }
        }
    }

    public class PLCInterface : DependencyObject
    {
        private int activeScreen;
        public int ActiveScreen
        {
            get { return activeScreen; }
            set { activeScreen = value; }
        }

        public Control CONTROL { get; set; } = new Control();
        public Status STATUS { get; set; } = new Status();

        public static object StatusControlLock = new object();
        public static object TimerLock = new object();
        
        public delegate void UpdateHandler(PLCInterface sender, PLCInterfaceEventArgs e);
        public delegate void OnlineMarker(PLCInterface sender, OnlineMarkerEventArgs e);
        
        public event UpdateHandler Update_1_s;
        public event UpdateHandler Update_100_ms;
        public event OnlineMarker Update_Online_Flag; 


        public bool OnlineMark;
        public int Errorcode;
        public S7Client Client;

        System.Timers.Timer Clock_100_ms;
        System.Timers.Timer WatchDogTimer;

        private byte[] CyclicStatusBuffer = new byte[65536];
        private byte[] ReadBuffer = new byte[65536];
        private byte[] CyclicControlBuffer = new byte[65536];
        private byte[] WriteBuffer = new byte[65536];
        private byte[] WatchdogBuffer = new byte[2];
        private short updateCounter = 0;

        public PLCInterface()
        {
            Client = new S7Client();

            Clock_100_ms = new System.Timers.Timer(100); 
            Clock_100_ms.Elapsed += onClock100msTick;
            Clock_100_ms.AutoReset = false;

            WatchDogTimer = new System.Timers.Timer(2000);
            WatchDogTimer.Elapsed += onClockWatchdogTick;
            WatchDogTimer.AutoReset = false;
        }
      
        public void StartCyclic()
        {
            Clock_100_ms.Start();
            WatchDogTimer.Start();
        }

        void StopCyclic()
        {
            Clock_100_ms.Stop();
            WatchDogTimer.Stop();
        }

        public void RestartInterface()
        {
            lock (PLCInterface.TimerLock)
            {
                Client = new S7Client();
                Clock_100_ms.Stop();
                Thread.Sleep(1000);
                while (!Client.Connected())
                {
                    Client.ConnectTo("192.168.0.1", 0, 1);
                    Thread.Sleep(200);
                    if (Client.Connected())
                    {
                        Clock_100_ms.Start();
                        WatchDogTimer.Start();
                    }
                }
            }
        }

        #region read functions
        private int ReadControl()
        {
            int result = -99;
            if (Client.Connected())
                result = Client.DBRead(6, 0, 26, CyclicControlBuffer);
            if (result == 0)
            {
                lock (StatusControlLock)
                {
                    // Horizontalna os
                    CONTROL.HorizontalnaOs.IdiUHome.GetValueFromGroupBuffer(CyclicControlBuffer);
                    CONTROL.HorizontalnaOs.IdiUPoziciju.GetValueFromGroupBuffer(CyclicControlBuffer);
                    CONTROL.HorizontalnaOs.IdiUPozicijuTicala.GetValueFromGroupBuffer(CyclicControlBuffer);
                    CONTROL.HorizontalnaOs.Reset.GetValueFromGroupBuffer(CyclicControlBuffer);
                    CONTROL.HorizontalnaOs.ZadanaPozicija.GetValueFromGroupBuffer(CyclicControlBuffer);
                    CONTROL.HorizontalnaOs.JogPlus.GetValueFromGroupBuffer(CyclicControlBuffer);
                    CONTROL.HorizontalnaOs.JogMinus.GetValueFromGroupBuffer(CyclicControlBuffer);

                    // Vertikalna os
                    CONTROL.VertikalnaOs.IdiUHome.GetValueFromGroupBuffer(CyclicControlBuffer);
                    CONTROL.VertikalnaOs.IdiUPoziciju.GetValueFromGroupBuffer(CyclicControlBuffer);
                    CONTROL.VertikalnaOs.IdiUSafePoziciju.GetValueFromGroupBuffer(CyclicControlBuffer);
                    CONTROL.VertikalnaOs.Reset.GetValueFromGroupBuffer(CyclicControlBuffer);
                    CONTROL.VertikalnaOs.ZadanaPozicija.GetValueFromGroupBuffer(CyclicControlBuffer);
                    CONTROL.VertikalnaOs.JogPlus.GetValueFromGroupBuffer(CyclicControlBuffer);
                    CONTROL.VertikalnaOs.JogMinus.GetValueFromGroupBuffer(CyclicControlBuffer);

                    // Rotaciona os
                    CONTROL.RotacijskaOs.IdiUHome.GetValueFromGroupBuffer(CyclicControlBuffer);
                    CONTROL.RotacijskaOs.IdiUPoziciju.GetValueFromGroupBuffer(CyclicControlBuffer);
                    CONTROL.RotacijskaOs.Referenciraj.GetValueFromGroupBuffer(CyclicControlBuffer);
                    CONTROL.RotacijskaOs.Reset.GetValueFromGroupBuffer(CyclicControlBuffer);
                    CONTROL.RotacijskaOs.ZadanaPozicija.GetValueFromGroupBuffer(CyclicControlBuffer);
                    CONTROL.RotacijskaOs.JogPlus.GetValueFromGroupBuffer(CyclicControlBuffer);
                    CONTROL.RotacijskaOs.JogMinus.GetValueFromGroupBuffer(CyclicControlBuffer);

                    // Ticalo
                    CONTROL.Ticalo.TicaloGore.GetValueFromGroupBuffer(CyclicControlBuffer);
                    CONTROL.Ticalo.TicaloDolje.GetValueFromGroupBuffer(CyclicControlBuffer);
                    CONTROL.Ticalo.Nuliraj.GetValueFromGroupBuffer(CyclicControlBuffer);
                
                }
            }
            return result;
        }

        private int ReadStatus()
        {
            int result = -99;
            if (Client.Connected())
                result = Client.DBRead(11, 0, 22, CyclicStatusBuffer); 
            if (result == 0)
            {
                lock (StatusControlLock)
                {
                    STATUS.HorizontalnaOs.UHome.GetValueFromGroupBuffer(CyclicStatusBuffer);
                    STATUS.HorizontalnaOs.UPoziciji.GetValueFromGroupBuffer(CyclicStatusBuffer);
                    STATUS.HorizontalnaOs.UPozicijiTicala.GetValueFromGroupBuffer(CyclicStatusBuffer);
                    STATUS.HorizontalnaOs.Greska.GetValueFromGroupBuffer(CyclicStatusBuffer);
                    STATUS.HorizontalnaOs.AktualnaPozicija.GetValueFromGroupBuffer(CyclicStatusBuffer);

                    STATUS.VertikalnaOs.UHome.GetValueFromGroupBuffer(CyclicStatusBuffer);
                    STATUS.VertikalnaOs.UPoziciji.GetValueFromGroupBuffer(CyclicStatusBuffer);
                    STATUS.VertikalnaOs.USafePoziciji.GetValueFromGroupBuffer(CyclicStatusBuffer);
                    STATUS.VertikalnaOs.Greska.GetValueFromGroupBuffer(CyclicStatusBuffer);
                    STATUS.VertikalnaOs.AktualnaPozicija.GetValueFromGroupBuffer(CyclicStatusBuffer);
                   
                    STATUS.RotacijskaOs.UHome.GetValueFromGroupBuffer(CyclicStatusBuffer);
                    STATUS.RotacijskaOs.UPoziciji.GetValueFromGroupBuffer(CyclicStatusBuffer);
                    STATUS.RotacijskaOs.UPozicijiTicala.GetValueFromGroupBuffer(CyclicStatusBuffer);
                    STATUS.RotacijskaOs.Referencirana.GetValueFromGroupBuffer(CyclicStatusBuffer);
                    STATUS.RotacijskaOs.Greska.GetValueFromGroupBuffer(CyclicStatusBuffer);
                    STATUS.RotacijskaOs.AktualnaPozicija.GetValueFromGroupBuffer(CyclicStatusBuffer);

                    STATUS.Ticalo.TicaloDolje.GetValueFromGroupBuffer(CyclicStatusBuffer);
                    STATUS.Ticalo.TicaloGore.GetValueFromGroupBuffer(CyclicStatusBuffer);
                    STATUS.Ticalo.Referencirano.GetValueFromGroupBuffer(CyclicStatusBuffer);
                }
            }
            return result;
        }

        public byte[] ReadCustom(int dbNumber, int startByte, int size)
        {
            int result = -99;
            lock (PLCInterface.TimerLock)
            {
                if (Client.Connected())
                {
                    result = Client.DBRead(dbNumber, startByte, size, ReadBuffer);
                }
            }
            return ReadBuffer;
        }
        #endregion

        #region write functions
        /// <summary>
        /// Writes one bit in DBmemory location, returns result of operation
        /// </summary>
        /// <param name="dbNumber"> data block number </param>
        /// <param name="startByte"> byte address in data block </param>
        /// <param name="bitInWord"> bit address in data block </param>
        /// <param name="operation"> operation parameter: acceptible values are "set", "reset", "toggle" </param>
        /// <returns></returns>
        public int WriteBit(int dbNumber, int startByte, int bitInWord, string operation)
        {
            byte[] _tempBuffer = new byte[2];
            int result = -99;
            lock (PLCInterface.TimerLock)
            {
                if (Client.Connected())
                {
                    result = Client.DBRead(dbNumber, startByte, 2, _tempBuffer);
                    switch (operation)
                    {
                        case "set":
                            S7.SetBitAt(ref _tempBuffer, 0, bitInWord, true);
                            break;
                        case "reset":
                            S7.SetBitAt(ref _tempBuffer, 0, bitInWord, false);
                            break;
                        case "toggle":
                            S7.SetBitAt(ref _tempBuffer, 0, bitInWord, !S7.GetBitAt(_tempBuffer, 0, bitInWord));
                            break;
                        default:
                            break;
                    }
                    result += Client.DBWrite(dbNumber, startByte, 2, _tempBuffer);
                }
                try
                {
                    if (result != 0)
                        throw new System.InvalidOperationException("write error");
                }
                finally
                {
                }
            }
            return result;
        }

        public int WriteTag(plcTag tag, object value)
        {
            byte[] _tempBuffer = new byte[4];
            int result = -99;
            lock (PLCInterface.TimerLock)
            {
                if (Client.Connected())
                {
                    switch (tag.VType)
                    {
                        case varType.BOOL:
                            result = Client.DBRead(tag.DbNumber, tag.Offset.ByteOffset, 2, _tempBuffer);
                            S7.SetBitAt(ref _tempBuffer, 0, tag.Offset.BitOffset, (bool)value);
                            result += Client.DBWrite(tag.DbNumber, tag.Offset.ByteOffset, 2, _tempBuffer);
                            break;
                        case varType.BYTE:
                            result = Client.DBRead(tag.DbNumber, tag.Offset.ByteOffset, 2, _tempBuffer);
                            S7.SetByteAt(_tempBuffer, 0, (byte)value);
                            result += Client.DBWrite(tag.DbNumber, tag.Offset.ByteOffset, 2, _tempBuffer);
                            break;
                        case varType.WORD:
                            result = Client.DBRead(tag.DbNumber, tag.Offset.ByteOffset, 2, _tempBuffer);
                            S7.SetWordAt(_tempBuffer, 0, (ushort)value);
                            result += Client.DBWrite(tag.DbNumber, tag.Offset.ByteOffset, 2, _tempBuffer);
                            break;
                        case varType.DWORD:
                            result = Client.DBRead(tag.DbNumber, tag.Offset.ByteOffset, 4, _tempBuffer);
                            S7.SetDWordAt(_tempBuffer, 0, (uint)value);
                            result += Client.DBWrite(tag.DbNumber, tag.Offset.ByteOffset, 4, _tempBuffer);
                            break;
                        case varType.INT:
                            result = Client.DBRead(tag.DbNumber, tag.Offset.ByteOffset, 2, _tempBuffer);
                            S7.SetIntAt(_tempBuffer, 0, (short)value);
                            result += Client.DBWrite(tag.DbNumber, tag.Offset.ByteOffset, 2, _tempBuffer);
                            break;
                        case varType.DINT:
                            result = Client.DBRead(tag.DbNumber, tag.Offset.ByteOffset, 4, _tempBuffer);
                            S7.SetDIntAt(_tempBuffer, 0, (int)value);
                            result += Client.DBWrite(tag.DbNumber, tag.Offset.ByteOffset, 4, _tempBuffer);
                            break;
                        case varType.REAL:
                            result = Client.DBRead(tag.DbNumber, tag.Offset.ByteOffset, 4, _tempBuffer);
                            S7.SetRealAt(_tempBuffer, 0, (float)value);
                            result += Client.DBWrite(tag.DbNumber, tag.Offset.ByteOffset, 4, _tempBuffer);
                            break;
                    }
                }
                try
                {
                    if (result != 0)
                        throw new System.InvalidOperationException("write error");
                }
                catch { }
                finally
                {
                }
            }
            return result;
        }

        public int WriteToggle(plcTag tag)
        {
            byte[] _tempBuffer = new byte[4];
            int result = -99;
            lock (PLCInterface.TimerLock)
            {
                if (Client.Connected())
                {
                    if (tag.VType == varType.BOOL)
                    {
                        if (tag.DType == dataType.DB)
                        {
                            result = Client.DBRead(tag.DbNumber, tag.Offset.ByteOffset, 2, _tempBuffer);
                            S7.SetBitAt(ref _tempBuffer, 0, tag.Offset.BitOffset, !S7.GetBitAt(_tempBuffer, 0, tag.Offset.BitOffset));
                            result += Client.DBWrite(tag.DbNumber, tag.Offset.ByteOffset, 2, _tempBuffer);
                        }
                        if (tag.DType == dataType.Q)
                        {
                            result = Client.ABRead(tag.Offset.ByteOffset,2, _tempBuffer);
                            S7.SetBitAt(ref _tempBuffer, 0, tag.Offset.BitOffset, !S7.GetBitAt(_tempBuffer, 0, tag.Offset.BitOffset));
                            result += Client.ABWrite(tag.Offset.ByteOffset,2,_tempBuffer);
                        }
                        
                    }
                }
                try
                {
                    if (result != 0)
                        throw new System.InvalidOperationException("write error");
                }
                catch { }
                finally
                {
                }
            }
            return result;
        }

        //public int WriteCameraOutput(PrepoznavanjeOblika.CameraOutputType cameraOutput)
        //{
        //    //byte[] _tempBuffer = new byte[22];
        //    //int result = -99;
        //    //lock (PLCInterface.TimerLock)
        //    //{
               
               
        //    //    if (Client.Connected())
        //    //    {
        //    //        S7.SetRealAt(_tempBuffer, 0, cameraOutput.POINT1.X + (float)STATUS.HorizontalnaOs.ActualPosition.X.Value);
        //    //        S7.SetRealAt(_tempBuffer, 4, cameraOutput.POINT1.Y + (float)STATUS.HorizontalnaOs.ActualPosition.Y.Value);
        //    //        S7.SetRealAt(_tempBuffer, 8, cameraOutput.POINT2.X + (float)STATUS.HorizontalnaOs.ActualPosition.X.Value);
        //    //        S7.SetRealAt(_tempBuffer, 12, cameraOutput.POINT2.Y + (float)STATUS.HorizontalnaOs.ActualPosition.Y.Value);
        //    //        S7.SetRealAt(_tempBuffer, 16, cameraOutput.PARAMETER);
        //    //        S7.SetIntAt(_tempBuffer, 20, (short)cameraOutput.TYPE);
        //    //        result = Client.DBWrite(28, 12, 22, _tempBuffer);
        //    //    }
        //    //}
        //    //return result;
        //}

        #endregion

        private void onClock100msTick(Object source, System.Timers.ElapsedEventArgs e)
        {
            //Thread.CurrentThread.Name = "PLCinterface_100msTick_Thread_" + second_counter.ToString();
            //second_counter++;

            
            int result;
            lock (TimerLock)
            {
                result = ReadStatus();
            }
            PLCInterfaceEventArgs p1 = new PLCInterfaceEventArgs();
            p1.StatusData = STATUS;
            p1.CyclicStatusBuffer = CyclicStatusBuffer;
            if (Update_100_ms != null)
                Update_100_ms(this, p1);

            if (updateCounter == 10)
            {
                result = 0;
                lock (TimerLock)
                {
                   result = ReadControl();
                    //result += ReadManual();
                }
                PLCInterfaceEventArgs p2 = new PLCInterfaceEventArgs();
                p2.ControlData = CONTROL;
                p2.CyclicControlBuffer = CyclicControlBuffer;
                //p2.StatusData = STATUS;

                if ((Update_1_s != null)&&(result==0))
                    Update_1_s(this, p2);

                updateCounter = 0;
            }

            updateCounter++;
            Clock_100_ms.Start();
        }

        private void onClockWatchdogTick(Object source, System.Timers.ElapsedEventArgs e)
        {
            //Thread.CurrentThread.Name = "PLCinterface_WatchdogTick_Thread" + PLCInterface.third_counter.ToString();
            //PLCInterface.third_counter++;

            lock (TimerLock)
            {
                int result = -99;
                Array.Clear(WatchdogBuffer, 0, WatchdogBuffer.Length);
                switch (activeScreen)
                {
                    case 0:
                        break;
                    case 1:
                        S7.SetBitAt(ref WatchdogBuffer, 0, 3, true);
                        break;
                    case 2:
                        S7.SetBitAt(ref WatchdogBuffer, 0, 4, true);
                        break;
                    case 3:
                        S7.SetBitAt(ref WatchdogBuffer, 0, 5, true);
                        break;
                    case 4:
                        S7.SetBitAt(ref WatchdogBuffer, 0, 6, true);
                        break;
                    case 5:
                        S7.SetBitAt(ref WatchdogBuffer, 0, 7, true);
                        break;
                    case 6:
                        S7.SetBitAt(ref WatchdogBuffer, 1, 0, true);
                        break;
                    case 7:
                        S7.SetBitAt(ref WatchdogBuffer, 1, 1, true);
                        break;
                    case 8:
                        S7.SetBitAt(ref WatchdogBuffer, 1, 2, true);
                        break;
                }
                S7.SetBitAt(ref WatchdogBuffer, 0, 1, true);
                result = Client.DBWrite(10, 0, 2, WatchdogBuffer);
                if (result == 0)
                    Errorcode = Client.DBRead(12, 0, 1, WatchdogBuffer);
                else
                    OnlineMark = false;
               
                if (Errorcode == 0)
                {
                    OnlineMark = S7.GetBitAt(WatchdogBuffer, 0, 0);
                }
                else
                {
                    OnlineMark = false;
                   
                }
            }
            OnlineMarkerEventArgs p = new OnlineMarkerEventArgs();
            p.OnlineMark = OnlineMark;
            if (Update_Online_Flag != null)
                Update_Online_Flag(this, p);
            if (OnlineMark)
            {
                WatchDogTimer.Start();
            }
            else
            {
                Errorcode = Errorcode+0;               
                RestartInterface();
            }
        }
    }

    #region Control and Status definitions
    public class Control
    {
        public horizontalnaOs HorizontalnaOs { get; set; } = new horizontalnaOs();
        public vertikalnaOs VertikalnaOs { get; set; } = new vertikalnaOs();
        public rotacijskaOs RotacijskaOs { get; set; } = new rotacijskaOs();
        public ticalo Ticalo { get; set; } = new ticalo();

        

        public Control()
        {
        }

        public class horizontalnaOs
        {
            public plcTag IdiUHome { get; set; } = new plcTag(varType.BOOL, dataType.DB, 6, new Offset(0, 0), false);
            public plcTag IdiUPoziciju { get; set; } = new plcTag(varType.BOOL, dataType.DB, 6, new Offset(0, 1), false);
            public plcTag IdiUPozicijuTicala { get; set; } = new plcTag(varType.BOOL, dataType.DB, 6, new Offset(0, 2), false);
            public plcTag Reset { get; set; } = new plcTag(varType.BOOL, dataType.DB, 6, new Offset(0, 3), false);
            public plcTag ZadanaPozicija { get; set; } = new plcTag(varType.REAL, dataType.DB, 6, new Offset(2, 0), 0.0f);
            public plcTag JogPlus { get; set; } = new plcTag(varType.BOOL, dataType.DB, 6, new Offset(6, 0), false);
            public plcTag JogMinus { get; set; } = new plcTag(varType.BOOL, dataType.DB, 6, new Offset(6, 1), false);
        }
        public class vertikalnaOs
        {
            public plcTag IdiUHome { get; set; } = new plcTag(varType.BOOL, dataType.DB, 6, new Offset(8, 0), false);
            public plcTag IdiUPoziciju { get; set; } = new plcTag(varType.BOOL, dataType.DB, 6, new Offset(8, 1), false);
            public plcTag IdiUSafePoziciju { get; set; } = new plcTag(varType.BOOL, dataType.DB, 6, new Offset(8, 2), false);
            public plcTag Reset { get; set; } = new plcTag(varType.BOOL, dataType.DB, 6, new Offset(8, 3), false);
            public plcTag ZadanaPozicija { get; set; } = new plcTag(varType.REAL, dataType.DB, 6, new Offset(10, 0), 0.0f);
            public plcTag JogPlus { get; set; } = new plcTag(varType.BOOL, dataType.DB, 6, new Offset(14, 0), false);
            public plcTag JogMinus { get; set; } = new plcTag(varType.BOOL, dataType.DB, 6, new Offset(14, 1), false);
        }
        public class rotacijskaOs
        {
            public plcTag IdiUHome { get; set; } = new plcTag(varType.BOOL, dataType.DB, 6, new Offset(16, 0), false);
            public plcTag IdiUPoziciju { get; set; } = new plcTag(varType.BOOL, dataType.DB, 6, new Offset(16, 1), false);
            public plcTag Referenciraj { get; set; } = new plcTag(varType.BOOL, dataType.DB, 6, new Offset(16, 2), false);
            public plcTag Reset { get; set; } = new plcTag(varType.BOOL, dataType.DB, 6, new Offset(16, 3), false);
            public plcTag ZadanaPozicija { get; set; } = new plcTag(varType.REAL, dataType.DB, 6, new Offset(18, 0), 0.0f);
            public plcTag JogPlus { get; set; } = new plcTag(varType.BOOL, dataType.DB, 6, new Offset(22, 0), false);
            public plcTag JogMinus { get; set; } = new plcTag(varType.BOOL, dataType.DB, 6, new Offset(22, 1), false);
        }
        public class ticalo
        {
            public plcTag TicaloGore { get; set; } = new plcTag(varType.BOOL, dataType.DB, 6, new Offset(24, 0), false);
            public plcTag TicaloDolje { get; set; } = new plcTag(varType.BOOL, dataType.DB, 6, new Offset(24, 1), false);
            public plcTag Nuliraj { get; set; } = new plcTag(varType.BOOL, dataType.DB, 6, new Offset(24, 2), false);
        }
        
    }

    public class Status
    {
        public horizontalnaOs HorizontalnaOs { get; set; } = new horizontalnaOs();
        public vertikalnaOs VertikalnaOs { get; set; } = new vertikalnaOs();
        public rotacijskaOs RotacijskaOs { get; set; } = new rotacijskaOs();
        public ticalo Ticalo { get; set; } = new ticalo();
    

        public Status()
        {
            
            //HorizontalnaOs.UHome.Value = false;
            //HorizontalnaOs.UPoziciji.Value = false;
            //HorizontalnaOs.UPozicijiTicala.Value = false;
            //HorizontalnaOs.AktualnaPozicija.Value = false;
           
        }

        public class horizontalnaOs
        {
           
            public plcTag UHome { get; set; } = new plcTag(varType.BOOL, dataType.DB, 11, new Offset(0, 0), false);
            public plcTag UPoziciji { get; set; } = new plcTag(varType.BOOL, dataType.DB, 11, new Offset(0, 1), false);
            public plcTag UPozicijiTicala { get; set; } = new plcTag(varType.BOOL, dataType.DB, 11, new Offset(0, 2), false);
            public plcTag Greska { get; set; } = new plcTag(varType.BOOL, dataType.DB, 11, new Offset(0, 3), false);
            public plcTag AktualnaPozicija { get; set; } = new plcTag(varType.REAL, dataType.DB, 11, new Offset(2, 0), 0.0f);
        
        }

        public class vertikalnaOs
        {
            public plcTag UHome { get; set; } = new plcTag(varType.BOOL, dataType.DB, 11, new Offset(6, 0), false);
            public plcTag UPoziciji { get; set; } = new plcTag(varType.BOOL, dataType.DB, 11, new Offset(6, 1), false);
            public plcTag USafePoziciji { get; set; } = new plcTag(varType.BOOL, dataType.DB, 11, new Offset(6, 2), false);
            public plcTag Greska { get; set; } = new plcTag(varType.BOOL, dataType.DB, 11, new Offset(6, 3), false);
            public plcTag AktualnaPozicija { get; set; } = new plcTag(varType.REAL, dataType.DB, 11, new Offset(8, 0), 0.0f);
        }
        public class rotacijskaOs
        {
            public plcTag UHome { get; set; } = new plcTag(varType.BOOL, dataType.DB, 11, new Offset(12, 0), false);
            public plcTag UPoziciji { get; set; } = new plcTag(varType.BOOL, dataType.DB, 11, new Offset(12, 1), false);
            public plcTag UPozicijiTicala { get; set; } = new plcTag(varType.BOOL, dataType.DB, 11, new Offset(12, 2), false);
            public plcTag Greska { get; set; } = new plcTag(varType.BOOL, dataType.DB, 11, new Offset(12, 3), false);
            public plcTag AktualnaPozicija { get; set; } = new plcTag(varType.REAL, dataType.DB, 11, new Offset(14, 0), 0.0f);
            public plcTag Referencirana { get; set; } = new plcTag(varType.BOOL, dataType.DB, 11, new Offset(18, 0), false);
        }

        public class ticalo
        {
            public plcTag TicaloDolje { get; set; } = new plcTag(varType.BOOL, dataType.DB, 11, new Offset(20, 0), false);
            public plcTag TicaloGore { get; set; } = new plcTag(varType.BOOL, dataType.DB, 11, new Offset(20, 1), false);
            public plcTag Referencirano { get; set; } = new plcTag(varType.BOOL, dataType.DB, 11, new Offset(20, 2), false);
            
        }
    }

    #endregion

    #region plcTag definition
    public class plcTag
    {
        varType vType;
        public varType VType
        {
            get
            {
                return vType;
            }
        }

        dataType dType;
        public dataType DType
        {
            get
            {
                return dType;
            }
        }

        int dbNumber;
        public int DbNumber
        {
            get
            {
                return dbNumber;
            }
        }

        Offset offset;
        public Offset Offset
        {
            get { return offset; }
        }


        object value;
        public object Value { get; set; }


        public plcTag(varType _vType, dataType _dType, int _dbNumber, Offset _offset, object _value)
        {
            vType = _vType;
            dType = _dType;
            offset = _offset;
            value = _value;
            if (dType != dataType.DB)
            {
                dbNumber = 0;
            }
            else
            {
                dbNumber = _dbNumber;
            }
        }
        /// <summary>
        /// extract value from raw buffer
        /// </summary>
        /// <param name="buffer"> buffer length must be greater than Offset.ByteOffset+4, else do nothing </param>
        public void GetValueFromGroupBuffer(byte[] buffer)
        {
            if (buffer.Length<Offset.ByteOffset + 4)
                return;
            switch (VType)
            {
                case varType.BOOL:
                    Value = S7.GetBitAt(buffer, Offset.ByteOffset, Offset.BitOffset);
                    break;
                case varType.BYTE:
                    Value = S7.GetByteAt(buffer, Offset.ByteOffset);
                    break;
                case varType.WORD:
                    Value = S7.GetWordAt(buffer, Offset.ByteOffset);
                    break;
                case varType.DWORD:
                    Value = S7.GetDWordAt(buffer, Offset.ByteOffset);
                    break;
                case varType.INT:
                    Value = S7.GetIntAt(buffer, Offset.ByteOffset);
                    break;
                case varType.DINT:
                    Value = S7.GetDIntAt(buffer, Offset.ByteOffset);
                    break;
                case varType.REAL:
                    Value = S7.GetRealAt(buffer, Offset.ByteOffset);
                    break;
            }
        }
    }
    public struct Offset
    {
        short byteOffset;
        public short ByteOffset
        {
            get { return byteOffset; }
            set { byteOffset = value; }
        }

        short bitOffset;
        public short BitOffset
        {
            get { return bitOffset; }
            set { bitOffset = value; }
        }
        public Offset(short _byteOffset, short _bitOffset)
        {
            byteOffset = _byteOffset;
            bitOffset = _bitOffset;
        }
    }
    public enum varType { BOOL, BYTE, WORD, DWORD, INT, DINT, REAL };
    public enum dataType { DB, I, Q, M, L, T };
    
    #endregion
}
