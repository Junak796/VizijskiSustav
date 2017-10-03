//
// HALCON/.NET (C#) image acquisition callback example
//
// © 2013-2014 MVTec Software GmbH
//
// IACallback.cs: Shows the usage of the image acquistion callbacks
//
// Example to demonstrate the usage of the HALCON user-specific callback
// functionality. The user has to adapt the device specific information section 
// to open the required HALCON Image Acquisition interface with the corresponding  
// device. Please see also the HALCON Image Acquisition Interface documentation 
// that could be found at the following web link: 
// http://www.mvtec.de/halcon/image-acquisition/                           
// The documentation describes also the supported callback types.
//


using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;

using HalconDotNet;

namespace IACallback
{
    // Enumeration to handle the different kinds of callback types
    enum myCallbackTypes
    {
        OTHER=0,
        EXPOSURE_END,
        TRANSFER_END
    };
    
    class Program
    {
        // Device specific information, e.g. GigEVision 
        // Please check the documentation to figure out which HALCON Image Acquisition 
        // Interface supports the callback functionality
        static readonly HTuple cInterface = "GigEVision"; //  "1394IIDC", "ADLINK", "BitFlow", "GenICamTL", 
                                                          //  "LinX", "MILLite", "MultiCam", "pylon", "SaperaLT"...
        // In some cases you have to adapt the following lines to open your desired device
        static readonly HTuple cCameraType = "default";   // e.g. Path to the camera configuration file
        static readonly HTuple cDevice = "default";       // e.g. Type of the device
        static readonly HTuple cPort = -1;                // e.g. Set the specific port information

        // Internal used structures
        static string[] gDevices;       // List of available devices
        static string[] gCallbackTypes; // List of available callback types
        static HObject  gImage;

        // Handles to assure a correct thread synchronisation
        static HMutex gAcqMutex;
        static HCondition gAcqCondition;
        static Thread gWorkerThread;

        static bool gThreadRunning;
        static int gCountCallbacks = 0;
        static readonly int cMaxGrabImage = 10;

        // User specific callback function
        public static int MyCallbackFunction(IntPtr handle, IntPtr user_context, IntPtr context)
        {
            // Please note, the userContext comprise only a new instance of IntPtr that is specified as a 
            // 32-bit value in C# (.net). You can use the userContext to refer further data e.g. by HashTables
            int int_context = context.ToInt32();

            if (int_context == (int)myCallbackTypes.EXPOSURE_END ||
                int_context == (int)myCallbackTypes.TRANSFER_END)
            {
                HOperatorSet.LockMutex(gAcqMutex);
                HOperatorSet.SignalCondition(gAcqCondition);
                gCountCallbacks++;
                Console.WriteLine("Send signal to the worker thread");
                HOperatorSet.UnlockMutex(gAcqMutex);
            }

            Console.WriteLine("User specific callback function executed");
            return 0;
        }

        // Worker thread to do the callback specific operations
        static void DoWork(object AcqHandle)
        {
            HOperatorSet.LockMutex(gAcqMutex);
          while (true)
          {
              HOperatorSet.WaitCondition(gAcqCondition, gAcqMutex);

              if (!gThreadRunning || gCountCallbacks > cMaxGrabImage)
                  break;

              Console.WriteLine("Thread specific calculations");
              try
              {
                  HOperatorSet.GrabImageAsync(out gImage, (HTuple)AcqHandle, -1);
              }
              catch (HalconException ex)
              {
                  Console.WriteLine(ex.GetErrorMessage());
              }

          }
          HOperatorSet.UnlockMutex(gAcqMutex);
          Console.WriteLine("End acquisition thread\n");
        }

        // This reference must be kept alive until callback is no longer
        // used by the interface (or else crashes after garbage collection)
        static HalconDotNet.HalconAPI.HFramegrabberCallback delegateCallback = MyCallbackFunction;

        static void Main(string[] args)
        {
            HTuple AcqHandle = 0;
            int num_devices;
            bool init_device = false;
            int callback_number = 0;
            int num_callback_types;
            bool reg_rallback = false;

            try
            {
              // Show all available devices
              num_devices = showAvailableDevices();
              if (num_devices == 0)
                Environment.Exit(0);

              gAcqCondition = new HCondition("", "");
              gAcqMutex = new HMutex("", "");

              // Select a specific device and call open_framegrabber()
              init_device = initDevice(out AcqHandle, num_devices);
              if (!init_device)
                exitProgram(AcqHandle, init_device, false, 0);

              // Use a thread to decouple cpu intensiv calculations
              gWorkerThread = new Thread(new ParameterizedThreadStart(DoWork));
              gWorkerThread.Start(AcqHandle);
              gThreadRunning = true;

              HOperatorSet.LockMutex(gAcqMutex);
              // Show the available callback types and register a specific one
              num_callback_types = showAvailableCallbackTypes(AcqHandle);
              if (num_callback_types == 0)
                exitProgram(AcqHandle, init_device, false, 0);

              reg_rallback =registerCallbackType(AcqHandle, num_callback_types, out callback_number);
              if (!reg_rallback)
                exitProgram(AcqHandle, init_device, reg_rallback, 0);

              // Place your code here, to force the device to signal a specific callback type
              Console.WriteLine("\nIf the registered event is signaled, the previous registered \n" +
                                "user-specific callback function will be executed.\n\n");

              // e.g. 'exposure_end'
              Console.WriteLine("Start grabbing an image.\n" +
                                "If e.g. an 'exposure_end' callback type was registered, the image grabbing will\n" +
                                "force the execution of the user-specific callback function.\n\n");

              try
              {
                HOperatorSet.GrabImageStart(AcqHandle, -1);
              }
              catch (HalconException ex)
              {
                Console.WriteLine(ex.GetErrorMessage());
              }

              HOperatorSet.UnlockMutex(gAcqMutex);
            }
            catch (HOperatorException exc)
            {
              Console.WriteLine(exc.GetErrorMessage());

              // Check if extended error information available
              if ((exc.GetExtendedErrorCode() != 0) || (exc.GetExtendedErrorMessage().Length != 0))
              {
                Console.WriteLine("Extended error code: " + exc.GetExtendedErrorCode());
                Console.WriteLine("Extended error message: " + exc.GetExtendedErrorMessage());
              }
            }
            Console.WriteLine("\nPress ENTER to exit\n");
            Console.ReadLine();

            exitProgram(AcqHandle, init_device, reg_rallback, callback_number);
        }

        // Display all available devices. The detected device list will be saved.
        // The user has the possibility to select a device from the list, and to
        // check if the device supports a HALCON Image Acquisition callback
        static int showAvailableDevices()
        {
            HTuple hv_Information, hv_BoardList;
            HOperatorSet.InfoFramegrabber(cInterface, "device", out hv_Information, out hv_BoardList);

            int numDevices = hv_BoardList.Length;

            if (numDevices > 0)
                Console.WriteLine("Available devices: \n");
            else
            {
                Console.WriteLine("Found no devices.\n");
                return 0;
            }

            gDevices = new string[numDevices];
            for (int i = 0; i < numDevices; i++)
            {
                gDevices.SetValue(hv_BoardList.TupleSelect(i).S, i);
                Console.WriteLine(i + 1 + ") " + hv_BoardList.TupleSelect(i).S);
            }
            return numDevices;
        }

        // Open a selected device
        static bool initDevice(out HTuple AcqHandle, int numDevices)
        {
            Console.WriteLine("\nEnter the number of the device you want to connect to: ");

            int deviceNumber = readUserSelection(numDevices);

            Console.WriteLine("\nOpen the specified device by calling open_framegrabber(...).\n");

            HOperatorSet.OpenFramegrabber(cInterface, 0, 0, 0, 0, 0, 0, "default", -1, "gray",
               -1, "default", cCameraType, gDevices[deviceNumber - 1], cPort, 0, out AcqHandle);

            return true;
        }

        // Shows the supported HALCON Image Acquisition callbacks
        static int showAvailableCallbackTypes(HTuple hAcqHandle)
        {
            HTuple Value;
            Console.WriteLine("Get the supported callback types using " +
                              "the parameter 'available_callback_types'.\n" +
                              "Show the available callback types.\n");

            HOperatorSet.GetFramegrabberParam(hAcqHandle, "available_callback_types", out Value);

            int num_callback_types = Value.Length;
            if(num_callback_types > 0)
                Console.WriteLine("\nAvailable callback types:\n");
            else
            {
                Console.WriteLine("\nNo callback types supported by the selected device.\n");
                return 0;
            }

            gCallbackTypes = new string[num_callback_types];
            for(int i=0; i<num_callback_types; i++)
            {
                gCallbackTypes.SetValue(Value.TupleSelect(i).S, i);
                Console.WriteLine(i + 1 + ") " + Value.TupleSelect(i).S);
            }
            return num_callback_types;
        }

        // Register the callback type the user has selected
        static bool registerCallbackType(HTuple AcqHandle, int num_callback_types, out int callback_number)
        {
            Console.WriteLine("\nEnter the number of the callback type you like to register: ");

            callback_number = readUserSelection(num_callback_types);
         
            Console.WriteLine("\nThe function 'MyCallbackFunction' is the user-specified callback function."); 

            // e.g. Activate the GigEVision GenApi event's. If the selected callback type isn't based on 
            // GenApi, it's recommended to skip the following lines.
            if( String.Compare("GigEVision" ,cInterface, true) == 0)
            {
                bool TransferEnd = (String.Compare("transfer_end",gCallbackTypes[callback_number - 1], false) == 0);
                bool EventOverflow = (String.Compare("event_queue_overflow", gCallbackTypes[callback_number - 1], false) == 0);
                bool DeviceLost = (String.Compare("device_lost", gCallbackTypes[callback_number - 1], false) == 0);

                if (TransferEnd == false && EventOverflow == false && DeviceLost == false)
                {
                  // We have to select the event inside the GenApi
                  HOperatorSet.SetFramegrabberParam(AcqHandle, "EventSelector", gCallbackTypes[callback_number - 1]);

                  // We have to enable the 'EventNotification', first we have to query the supported types
                  try
                  {
                      HTuple Value;
                      HOperatorSet.GetFramegrabberParam(AcqHandle, "EventNotification_values", out Value);
                      HOperatorSet.SetFramegrabberParam(AcqHandle, "EventNotification", Value.TupleSelect(1).S);
                  }
                  catch (HalconException)
                  {
                      Console.WriteLine("\nEventNotification_values is not supported by this device,\n");
                      Console.WriteLine("we try acq.SetFramegrabberParam(\"EventNotification\",\"On\").\n");
                      HOperatorSet.SetFramegrabberParam(AcqHandle, "EventNotification", "On");
                  }
               }
            }

            // Register the selected callback function, in this example the user-context is 0 . 
            // Please note, the userContext comprise only a new instance of IntPtr that is specified as a 
            // 32-bit value in C# (.net). You can use the userContext to refer further data e.g. by HashTables
            IntPtr ptr = Marshal.GetFunctionPointerForDelegate(delegateCallback);
            
            int myContext = (int)myCallbackTypes.OTHER;
            if (String.Compare("ExposureEnd".ToLower(), gCallbackTypes[callback_number - 1].ToLower()) == 0)
                myContext = (int)myCallbackTypes.EXPOSURE_END;
            if (String.Compare("transfer_end".ToLower(), gCallbackTypes[callback_number - 1].ToLower()) == 0)
                myContext = (int)myCallbackTypes.TRANSFER_END;

            HOperatorSet.SetFramegrabberCallback(AcqHandle, gCallbackTypes[callback_number - 1], ptr, myContext);
            return true;
        }

        // Read the user input. Used by the device and callback type selection.
        static int readUserSelection(int num_devices)
        {
            int  number = 0;
            do
            {
                string line = Console.ReadLine();

                if (int.TryParse(line, out number))
                {
                    if (number < 1 || number > num_devices)
                        Console.WriteLine("Invalid input.\nSelect a listed entry:");
                }
                else
                {
                    number = 0;
                    Console.WriteLine("Invalid input. Only numbers are allowed.\nSelect a listed entry:");
                }
            }
            while (number < 1 || number > num_devices);

            return number;
        }

        static void exitProgram(HTuple AcqHandle, bool init_device, bool reg_rallback, int callback_number)
        {
            HOperatorSet.LockMutex(gAcqMutex);
            if (reg_rallback)
                HOperatorSet.SetFramegrabberCallback(AcqHandle, gCallbackTypes[callback_number - 1], 0, 0);
            HOperatorSet.UnlockMutex(gAcqMutex);

            if (init_device)
            {
                gThreadRunning = false;
                HOperatorSet.SignalCondition(gAcqCondition);

                gWorkerThread.Join();

                HOperatorSet.ClearMutex(gAcqMutex);
                HOperatorSet.ClearCondition(gAcqCondition);
                HOperatorSet.CloseFramegrabber(AcqHandle);
            }
            Environment.Exit(0);
        }
    }
}
