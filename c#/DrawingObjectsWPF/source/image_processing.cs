//
//  File generated by HDevelop for HALCON/DOTNET (C#) Version 12.0
//
//  This file is intended to be used with the HDevelopTemplate or
//  HDevelopTemplateWPF projects located under %HALCONEXAMPLES%\c#

using System;
using HalconDotNet;

public partial class HDevelopExport
{
  public HTuple hv_ExpDefaultWinHandle;

  // Procedures 
  // Chapter: Develop
  // Short Description: Switch dev_update_pc, dev_update_var and dev_update_window to 'off'. 
  public void dev_update_off ()
  {
    // Initialize local and output iconic variables 

    //This procedure sets different update settings to 'off'.
    //This is useful to get the best performance and reduce overhead.
    //
    // dev_update_pc(...); only in hdevelop
    // dev_update_var(...); only in hdevelop
    // dev_update_window(...); only in hdevelop

    return;
  }

  // Local procedures 
  public void add_new_drawing_object (HTuple hv_Type, HTuple hv_WindowHandle, out HTuple hv_DrawID)
  {

    // Initialize local and output iconic variables 

    hv_DrawID = new HTuple();
    //Create a drawing object DrawID of the specified Type
    //and attach it to the graphics window WindowHandle
    //
    if ((int)(new HTuple(hv_Type.TupleEqual("rectangle1"))) != 0)
    {
      HOperatorSet.CreateDrawingObjectRectangle1(100, 100, 200, 200, out hv_DrawID);
    }
    else if ((int)(new HTuple(hv_Type.TupleEqual("circle"))) != 0)
    {
      HOperatorSet.CreateDrawingObjectCircle(200, 200, 120, out hv_DrawID);
    }
    else if ((int)(new HTuple(hv_Type.TupleEqual("rectangle2"))) != 0)
    {
      HOperatorSet.CreateDrawingObjectRectangle2(200, 200, 0, 100, 100, out hv_DrawID);
    }
    else if ((int)(new HTuple(hv_Type.TupleEqual("ellipse"))) != 0)
    {
      HOperatorSet.CreateDrawingObjectEllipse(200, 200, 0, 100, 60, out hv_DrawID);
    }
    else
    {
      throw new HalconException(
          (new HTuple("Unrecognized drawing object type.")).TupleConcat("Either not a valid type or not supported by this procedure"));
    }

    return;
  }

  public void process_image (HObject ho_Image, out HObject ho_EdgeAmplitude, HTuple hv_WindowHandle, 
      HTuple hv_DrawID)
  {



    // Local iconic variables 

    HObject ho_Region, ho_ImageReduced;

    // Initialize local and output iconic variables 
    HOperatorSet.GenEmptyObj(out ho_EdgeAmplitude);
    HOperatorSet.GenEmptyObj(out ho_Region);
    HOperatorSet.GenEmptyObj(out ho_ImageReduced);

    try
    {
      //Apply an Sobel edge filter on the background
      //image within the region of interest defined
      //by the drawing object.
      ho_Region.Dispose();
      HOperatorSet.GetDrawingObjectIconic(out ho_Region, hv_DrawID);
      ho_ImageReduced.Dispose();
      HOperatorSet.ReduceDomain(ho_Image, ho_Region, out ho_ImageReduced);
      ho_EdgeAmplitude.Dispose();
      HOperatorSet.SobelAmp(ho_ImageReduced, out ho_EdgeAmplitude, "sum_abs", 3);
      ho_Region.Dispose();
      ho_ImageReduced.Dispose();

      return;
    }
    catch (HalconException HDevExpDefaultException)
    {
      ho_Region.Dispose();
      ho_ImageReduced.Dispose();

      throw HDevExpDefaultException;
    }
  }

  public void display_results (HObject ho_EdgeAmplitude)
  {

    // Initialize local and output iconic variables 

    //Display the filtered image
    HOperatorSet.SetSystem("flush_graphic", "false");
    HOperatorSet.ClearWindow(hv_ExpDefaultWinHandle);
    HOperatorSet.DispObj(ho_EdgeAmplitude, hv_ExpDefaultWinHandle);
    HOperatorSet.SetSystem("flush_graphic", "true");

    return;
  }

  // Main procedure 
  private void action()
  {

    // Local iconic variables 

    HObject ho_Image, ho_EdgeAmplitude=null;


    // Local control variables 

    HTuple hv_WindowHandle = new HTuple(), hv_DrawID = null;

    // Initialize local and output iconic variables 
    HOperatorSet.GenEmptyObj(out ho_Image);
    HOperatorSet.GenEmptyObj(out ho_EdgeAmplitude);

    try
    {
      //Initialize visualization
      dev_update_off();
      //dev_close_window(...);
      //dev_open_window(...);
      HOperatorSet.SetPart(hv_ExpDefaultWinHandle, 0, 0, 511, 511);
      //
      //Read background image and attach it to the window
      ho_Image.Dispose();
      HOperatorSet.ReadImage(out ho_Image, "fabrik");
      HOperatorSet.AttachBackgroundToWindow(ho_Image, hv_ExpDefaultWinHandle);
      //
      //Add a drawing object and start the processing loop
      add_new_drawing_object("rectangle1", hv_ExpDefaultWinHandle, out hv_DrawID);
      while ((int)(1) != 0)
      {
        ho_EdgeAmplitude.Dispose();
        process_image(ho_Image, out ho_EdgeAmplitude, hv_ExpDefaultWinHandle, hv_DrawID);
        display_results(ho_EdgeAmplitude);
      }
    }
    catch (HalconException HDevExpDefaultException)
    {
      ho_Image.Dispose();
      ho_EdgeAmplitude.Dispose();

      throw HDevExpDefaultException;
    }
    ho_Image.Dispose();
    ho_EdgeAmplitude.Dispose();

  }

  public void InitHalcon()
  {
    // Default settings used in HDevelop 
    HOperatorSet.SetSystem("width", 512);
    HOperatorSet.SetSystem("height", 512);
  }

  public void RunHalcon(HTuple Window)
  {
    hv_ExpDefaultWinHandle = Window;
    action();
  }

}
