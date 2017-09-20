using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Snap7;

namespace VizijskiSustav
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
    public class PLCInterface
    {
        private int activeScreen;
        public int ActiveScreen
        {
            get { return activeScreen; }
            set { activeScreen = value; }
        }

        int second_counter = 0;
        static int third_counter = 0;

        public static object StatusControlLock = new object();
        public static object TimerLock = new object();
        
        public delegate void UpdateHandler(PLCInterface sender, PLCInterfaceEventArgs e);
        public delegate void OnlineMarker(PLCInterface sender, OnlineMarkerEventArgs e);
        
        public event UpdateHandler Update_1_s;
        public event UpdateHandler Update_100_ms;
        public event OnlineMarker Update_Online_Flag; 


        public bool OnlineMark;
        public S7Client Client;

        System.Timers.Timer Clock_100_ms;
        System.Timers.Timer WatchDogTimer;

        private byte[] CyclicReadBuffer = new byte[65536];
        private byte[] ReadBuffer = new byte[65536];
        private byte[] CyclicWriteBuffer = new byte[65536];
        private byte[] WriteBuffer = new byte[65536];
        private byte[] WatchdogBuffer = new byte[2];
        private short updateCounter = 0;
        
        public Control CONTROL = new Control();
        public Status STATUS = new Status();
        //public Axes_DB AXES = new Axes_DB();
      
        public PLCInterface()
        {
            Client = new S7Client();
            //int timeout = 50;
            //Client.SetParam(Snap7.S7Consts.p_i32_PingTimeout, ref timeout);
            //Client.SetParam(Snap7.S7Consts.p_i32_SendTimeout, ref timeout);
            //Client.SetParam(Snap7.S7Consts.p_i32_RecvTimeout, ref timeout);

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
                result = Client.DBRead(33, 0, 2, CyclicReadBuffer);
            if (result == 0)
            {
                lock (StatusControlLock)
                {
                    // Ripple
                    CONTROL.Test.StartVerticalUp.GetValueFromGroupBuffer(CyclicReadBuffer);
                    CONTROL.Test.StartVerticalDown.GetValueFromGroupBuffer(CyclicReadBuffer);
                    CONTROL.Test.StartHorizontalUp.GetValueFromGroupBuffer(CyclicReadBuffer);
                    CONTROL.Test.StartHorizontalDown.GetValueFromGroupBuffer(CyclicReadBuffer);
                    
                    
                  
                }
            }
            return result;
        }

        private int ReadStatus()
        {
            int result = -99;
            if (Client.Connected())
                //result = Client.DBRead(29, 0, 1536, CyclicReadBuffer); // was 1118
            if (result == 0)
            {
                lock (StatusControlLock)
                {
                    
                    //STATUS.Ripple.AutomaticActive.GetValueFromGroupBuffer(CyclicReadBuffer);
                    //STATUS.Ripple.SheetAbsent.GetValueFromGroupBuffer(CyclicReadBuffer);
                    //STATUS.Ripple.LaserActualValue.GetValueFromGroupBuffer(CyclicReadBuffer);
                    //STATUS.Ripple.FirstPoint.X.GetValueFromGroupBuffer(CyclicReadBuffer);
                    //STATUS.Ripple.FirstPoint.Y.GetValueFromGroupBuffer(CyclicReadBuffer);
                    //STATUS.Ripple.LastPoint.X.GetValueFromGroupBuffer(CyclicReadBuffer);
                    //STATUS.Ripple.LastPoint.Y.GetValueFromGroupBuffer(CyclicReadBuffer);
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

      

        #endregion

        private void onClock100msTick(Object source, System.Timers.ElapsedEventArgs e)
        {
            Thread.CurrentThread.Name = "PLCinterface_100msTick_Thread_" + second_counter.ToString();
            second_counter++;

            int result;
            lock (TimerLock)
            {
                result = ReadStatus();
            }
            PLCInterfaceEventArgs p1 = new PLCInterfaceEventArgs();
            p1.StatusData = STATUS;
            if (Update_100_ms != null)
                Update_100_ms(this, p1);

            if (updateCounter == 10)
            {
                result = 0;
                lock (TimerLock)
                {
                    result = ReadControl();
                   
                }
                PLCInterfaceEventArgs p2 = new PLCInterfaceEventArgs();
                p2.ControlData = CONTROL;
                p2.StatusData = STATUS;
             
                
                if ((Update_1_s != null)&&(result==0))
                    Update_1_s(this, p2);

                updateCounter = 0;
            }

            updateCounter++;
            Clock_100_ms.Start();
        }

        private void onClockWatchdogTick(Object source, System.Timers.ElapsedEventArgs e)
        {
            Thread.CurrentThread.Name = "PLCinterface_WatchdogTick_Thread" + PLCInterface.third_counter.ToString();
            PLCInterface.third_counter++;

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
                    result = Client.DBRead(10, 0, 2, WatchdogBuffer);
                else
                    OnlineMark = false;
                if (result == 0)
                {
                    OnlineMark = S7.GetBitAt(WatchdogBuffer, 0, 2);
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
                RestartInterface();
            }
        }
    }

    #region Control and Status definitions
    public class Control
    {
        public test Test = new test();
     

        public Control()
        {
         
        }
        public class test
        {
            public plcTag StartVerticalUp           = new plcTag(varType.BOOL, dataType.DB, 33, new Offset(0, 0), false);
            public plcTag StartVerticalDown          = new plcTag(varType.BOOL, dataType.DB, 33, new Offset(0, 1), false);
            public plcTag StartHorizontalUp             = new plcTag(varType.BOOL, dataType.DB, 33, new Offset(0, 2), false);
            public plcTag StartHorizontalDown            = new plcTag(varType.BOOL, dataType.DB, 33, new Offset(0, 3), false);
        }
        
    }

    public class Status
    {
        //public ripple Ripple = new ripple();

        public Status()
        {
            //Ripple.AutomaticActive.Value = false;
            //Ripple.SheetAbsent.Value = false;
            //Ripple.LaserActualValue.Value = 0.0f;
            //Ripple.FirstPoint.X.Value = 0.0f;
            //Ripple.FirstPoint.Y.Value = 0.0f;
            //Ripple.LastPoint.X.Value = 0.0f;
            //Ripple.LastPoint.Y.Value = 0.0f;
        }
       

        //public class ripple
        //{
        //    public plcTag AutomaticActive = new plcTag(varType.BOOL, dataType.DB, 29, new Offset(18, 0), false);
        //    public plcTag SheetAbsent = new plcTag(varType.BOOL, dataType.DB, 29, new Offset(18, 1), false);
        //    public plcTag LaserActualValue = new plcTag(varType.REAL, dataType.DB, 29, new Offset(20, 0), 0.0f);
        //    public class firstPoint
        //    {
        //        public plcTag X = new plcTag(varType.REAL, dataType.DB, 29, new Offset(24, 0), 0.0f);
        //        public plcTag Y = new plcTag(varType.REAL, dataType.DB, 29, new Offset(28, 0), 0.0f);
        //    }
        //    public class lastPoint
        //    {
        //        public plcTag X = new plcTag(varType.REAL, dataType.DB, 29, new Offset(32, 0), 0.0f);
        //        public plcTag Y = new plcTag(varType.REAL, dataType.DB, 29, new Offset(36, 0), 0.0f);
        //    }
        //    public firstPoint FirstPoint = new firstPoint();
        //    public lastPoint LastPoint = new lastPoint();
        //}
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
