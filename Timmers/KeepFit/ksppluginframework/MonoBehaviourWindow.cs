﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using KSP;
using UnityEngine;

/// <summary>
/// An Extended version of the UnityEngine.MonoBehaviour Class
/// Basically a template for a Window, has the MonoBehaviourExtended properties and extra bits to make drawing a window easier
/// </summary>
public abstract class MonoBehaviourWindow : MonoBehaviourExtended
{
    #region "Constructors"
    internal MonoBehaviourWindow()
        : base()
    {
        this.WindowID = UnityEngine.Random.Range(1000, 2000000);
        this.Hide();
        LogFormatted_DebugOnly("WindowID:{0}", WindowID);
    }
    ///CANT USE THE ONES BELOW HERE AS WE NEED TO INSTANTIATE THE WINDOW USING AddComponent()
    //internal MonoBehaviourWindow(string Caption)
    //    : this()
    //{
    //    this.WindowCaption = Caption;
    //}
    //internal MonoBehaviourWindow(string Caption, Rect Position)
    //    : this(Caption)
    //{
    //    this.WindowRect = Position;
    //}

    //TODO: Look at using this
    //  http://answers.unity3d.com/questions/445444/add-component-in-one-line-with-parameters.html

    //internal static MonoBehaviourWindow CreateComponent(GameObject AttachTo)
    //{
    //    MonoBehaviourWindow monoReturn;
    //    monoReturn = AttachTo.AddComponent<MonoBehaviourWindow>();
    //    return monoReturn;
    //}

    #endregion

    internal override void Awake()
    {
        //just some debugging stuff here
        LogFormatted_DebugOnly("New MBWindow Awakened");
        //base.Awake();
    }

    /// <summary>
    /// WindowID variable - randomly set at window creation
    /// </summary>
    internal Int32 WindowID { get; private set; }
    /// <summary>
    /// Window position on screen, is fed in to the Window routine and the resulting position after GUILayout is what you read
    /// </summary>
    internal Rect WindowRect;

    /// <summary>
    /// Caption of the Window
    /// </summary>
    internal string WindowCaption = null;
    /// <summary>
    /// Style of the Window
    /// </summary>
    internal GUIStyle WindowStyle = null;
    /// <summary>
    /// Layout Options for the GUILayout.Window function
    /// </summary>
    internal GUILayoutOption[] WindowOptions = null;

    /// <summary>
    /// Whether the window is draggable by mouse
    /// </summary>
    internal Boolean DragEnabled = false;
    /// <summary>
    /// A defined area (like a handle) where you can drag from. This is from the top left corner of the window. Lets you make isso it can be only draggable from a certain point, icon, title bar, etc
    /// </summary>
    internal Rect DragRect;

    /// <summary>
    /// Whether the window can be moved off the visible screen
    /// </summary>
    internal Boolean ClampToScreen = true;
    /// <summary>
    /// How close to the edges it can get if clamping is enabled - this can be negative if you want to allow it to go off screen by a certain amount
    /// </summary>
    internal RectOffset ClampToScreenOffset = new RectOffset(0, 0, 0, 0);

    private bool Visible;

    public bool isVisible()
    {
        return Visible;
    }

    public void Show()
    {
        Visible = true;
    }

    public void Hide()
    {
        Visible = false;
    }

    override internal void OnGUIEvery()
    {
        if (Visible)
        {
            if (Event.current.type == EventType.Repaint || Event.current.isMouse)
            {
                //myPreDrawQueue(); // Your current on preDrawQueue code
            }
            DrawGUI(); // Your current on postDrawQueue code
        }
    }

    /// <summary>
    /// This is the Code that draws the window and sets the skin
    /// !!!! You have to set the skin before drawing the window or you will scratch your head for ever
    /// </summary>
    private void DrawGUI()
    {
        //this sets the skin on each draw loop
        GUI.skin = SkinsLibrary.CurrentSkin;

        //keep the window locked to the screen if its supposed to be
        if (ClampToScreen)
            WindowRect=WindowRect.ClampToScreen(ClampToScreenOffset);
        
        //Are we using a custom style of the skin style for the window
        if (WindowStyle == null) {
            WindowRect = GUILayout.Window(WindowID, WindowRect, DrawWindowInternal, WindowCaption, WindowOptions);
        } else {
            WindowRect = GUILayout.Window(WindowID, WindowRect, DrawWindowInternal, WindowCaption, WindowStyle, WindowOptions);
        }
        
        //Draw the tooltip of its there to be drawn
        if (TooltipsEnabled)
            DrawToolTip();
    }

    /// <summary>
    /// Private function that handles wrapping the drawwindow functionality with the supplementary stuff - dragging, tooltips, etc
    /// </summary>
    /// <param name="id">The ID of the Window being drawn</param>
    private void DrawWindowInternal(Int32 id)
    {
        //This calls the must be overridden code
        DrawWindow(id);

        //Set the Tooltip variable based on whats in this window
        if (TooltipsEnabled)
            SetTooltipText();

        //Are we allowing window drag
        if (DragEnabled)
            if (DragRect.height == 0 && DragRect.width == 0)
                GUI.DragWindow();
            else
                GUI.DragWindow(DragRect);
    }

    /// <summary>
    /// This is the must be overridden function that equates to the content of the window
    /// </summary>
    /// <param name="id">The ID of the Window being drawn</param>
    internal abstract void DrawWindow(Int32 id);

    #region Tooltip Work
    /// <summary>
    /// Whether tooltips should be displayed for this window
    /// </summary>
    internal Boolean TooltipsEnabled = false;

    /// <summary>
    /// Is a Tooltip currently showing
    /// </summary>
    internal Boolean TooltipShown { get; private set; }
    /// <summary>
    /// Whereis the tooltip positioned
    /// </summary>
    internal Rect TooltipPosition { get { return _TooltipPosition; } }
    private Rect _TooltipPosition = new Rect();

    /// <summary>
    /// An offset from the mouse position to put the top left of the tooltip. Use this to get the tooltip out from under the cursor
    /// </summary>
    internal Vector2d TooltipMouseOffset = new Vector2d();

    /// <summary>
    /// Whether the Tooltip should stay where first drawn or follow the mouse
    /// </summary>
    internal Boolean TooltipStatic = false;

    /// <summary>
    /// How long the tooltips should be displayed before auto-hiding
    /// 
    /// Set to 0 to have them last for ever
    /// </summary>
    internal Int32 TooltipDisplayForSecs = 15;

    /// <summary>
    /// Maximum width in pixels of the tooltip window. Text will wrap inside that width.
    /// 
    /// Set to 0 for unity default behaviour
    /// </summary>
    internal Int32 TooltipMaxWidth = 250;

    //Store the tooltip text from throughout the code
    private string strToolTipText = "";
    private string strLastTooltipText = "";

    //store how long the tooltip has been displayed for
    private Single fltTooltipTime = 0f;

    /// <summary>
    /// This is the meat of drawing the tooltip on screen
    /// </summary>
    private void DrawToolTip()
    {
        //Added drawing check to turn off tooltips when window hides
        if (TooltipsEnabled && Visible && (strToolTipText != "") && ((TooltipDisplayForSecs == 0) || (fltTooltipTime < (Single)TooltipDisplayForSecs)))
        {
            GUIContent contTooltip = new GUIContent(strToolTipText);
            GUIStyle styleTooltip = SkinsLibrary.CurrentTooltip;

            //if the content of the tooltip changes then reset the counter
            if (!TooltipShown || (strToolTipText != strLastTooltipText))
                fltTooltipTime = 0f;

            //Calc the size of the Tooltip
            _TooltipPosition.x = Event.current.mousePosition.x + (Single)TooltipMouseOffset.x;
            _TooltipPosition.y = Event.current.mousePosition.y + (Single)TooltipMouseOffset.y;
                
            //do max width calc if needed
            if (TooltipMaxWidth > 0) {
                //calc the new width and height
                float minwidth, maxwidth;
                SkinsLibrary.CurrentTooltip.CalcMinMaxWidth(contTooltip, out minwidth, out maxwidth); // figure out how wide one line would be
                _TooltipPosition.width = Math.Min(TooltipMaxWidth - SkinsLibrary.CurrentTooltip.padding.horizontal, maxwidth); //then work out the height with a max width
                _TooltipPosition.height = SkinsLibrary.CurrentTooltip.CalcHeight(contTooltip, TooltipPosition.width); // heres the result
            }
            else
            {
                //calc the new width and height
                Vector2 Size = SkinsLibrary.CurrentTooltip.CalcSize(contTooltip);
                _TooltipPosition.width = Size.x;
                _TooltipPosition.height= Size.y;

            }
            //set the style props for text layout
            styleTooltip.stretchHeight = !(TooltipMaxWidth > 0);
            styleTooltip.stretchWidth = !(TooltipMaxWidth > 0);
            styleTooltip.wordWrap = (TooltipMaxWidth > 0);

            //clamp it accordingly
            if (ClampToScreen)
                _TooltipPosition = _TooltipPosition.ClampToScreen(ClampToScreenOffset);
            
            //Draw the Tooltip
            GUI.Label(TooltipPosition, contTooltip, styleTooltip);
            //On top of everything
            GUI.depth = 0;
            
            //update how long the tip has been on the screen and reset the flags
            fltTooltipTime += Time.deltaTime;
            TooltipShown = true;
        }
        else
        {
            //clear the flags
            TooltipShown = false;
        }

        strLastTooltipText = strToolTipText;
    }

    /// <summary>
    /// This function is run at the end of each draw loop to store the tooltiptext for later
    /// </summary>
    private void SetTooltipText()
    {
        if (Event.current.type == EventType.Repaint)
        {
            strToolTipText = GUI.tooltip;
        }
    }
    #endregion
}
