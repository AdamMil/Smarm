/*
Smarm is an editor for the game Swarm, which was written by Jim Crawford. 
http://www.adammil.net/
Copyright (C) 2003-2004 Adam Milazzo

This program is free software; you can redistribute it and/or
modify it under the terms of the GNU General Public License
as published by the Free Software Foundation; either version 2
of the License, or (at your option) any later version.
This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.
You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
*/

// TODO: make shift-click align object with the last object placed
using System;
using System.Collections;
using System.Drawing;
using GameLib;
using GameLib.Forms;
using GameLib.Input;
using GameLib.Video;

namespace Smarm
{

#region MenuLabel
class MenuLabel : Label
{ public MenuLabel() { Style |= ControlStyle.Clickable; TextPadding=1; }

  public MenuBase Menu { get { return menu; } set { menu=value; } }

  protected override void OnMouseEnter(EventArgs e) { over=true;  Invalidate(); }
  protected override void OnMouseLeave(EventArgs e) { over=false; Invalidate(); }
  protected override void OnPaintBackground(PaintEventArgs e)
  { base.OnPaintBackground(e);
    if(over && menu!=null && menu.Controls.Count>0)
      Helpers.DrawBorder(e.Surface, DisplayRect, BorderStyle.Fixed3D, BackColor, false);
  }
  protected override void OnMouseDown(ClickEventArgs e)
  { if(!e.Handled && e.CE.Button==MouseButton.Left && menu!=null && menu.Controls.Count>0)
    { menu.Show(this, new Point(TextAlign==ContentAlignment.TopLeft ? 0 : Width-30, Height), true);
      e.Handled=true;
    }
    base.OnMouseClick(e);
  }

  MenuBase menu;
  bool over;
}
#endregion

#region TopBar
class TopBar : ContainerControl
{ public TopBar()
  { BackColor = Color.FromArgb(48, 48, 48);

    #region Add controls
    menuBar.Bounds = new Rectangle(0, 1, 140, 30);
    menuBar.VerticalPadding=4;

    MenuBase menu = menuBar.Add(new Menu("File", new KeyCombo(KeyMod.Alt, 'F')));
    menu.Add(new MenuItem("New", 'N', new KeyCombo(KeyMod.Ctrl, 'N'))).Click += new EventHandler(new_OnClick);
    menu.Add(new MenuItem("Load", 'L', new KeyCombo(KeyMod.Ctrl, 'L'))).Click += new EventHandler(load_OnClick);
    menu.Add(new MenuItem("Save", 'S', new KeyCombo(KeyMod.Ctrl, 'S'))).Click += new EventHandler(save_OnClick);
    menu.Add(new MenuItem("Save As...", 'A')).Click += new EventHandler(saveAs_OnClick);
    menu.Add(new MenuItem("Compile...", 'C')).Click += new EventHandler(compileInto_OnClick);
    menu.Add(new MenuItem("Recompile", 'R', new KeyCombo(KeyMod.Ctrl, 'R'))).Click += new EventHandler(compile_OnClick);
    menu.Add(new MenuItem("Exit", 'X', new KeyCombo(KeyMod.Ctrl, 'X'))).Click += new EventHandler(exit_OnClick);

    menu = menuBar.Add(new Menu("Edit", new KeyCombo(KeyMod.Alt, 'E')));
    menu.Add(new MenuItem("Edit in paint program", 'E', new KeyCombo(Key.F5))).Click += new EventHandler(editRect_OnClick);
    menu.Add(new MenuItem("Move rectangle", 'M', new KeyCombo(Key.F8))).Click += new EventHandler(moveRect_OnClick);
    menu.Add(new MenuItem("Object properties...", 'O', new KeyCombo(Key.F4))).Click += new EventHandler(objectProps_OnClick);
    menu.Add(new MenuItem("Level properties...", 'L')).Click += new EventHandler(levelProps_OnClick);
    menu.Add(new MenuItem("Smarm properties...", 'S')).Click += new EventHandler(smarmProps_OnClick);
    
    menu = menuBar.Add(new Menu("View", new KeyCombo(KeyMod.Alt, 'V')));
    menu.Add(new MenuItem("Toggle fullscreen", 'F', new KeyCombo(KeyMod.Alt, Key.Enter))).Click += new EventHandler(toggleFullscreen_OnClick);
    menu.Add(new MenuItem("Toggle objects", 'O', new KeyCombo(Key.F2))).Click += new EventHandler(toggleObjects_OnClick);
    menu.Add(new MenuItem("Toggle polygons", 'P', new KeyCombo(Key.F3))).Click += new EventHandler(togglePolygons_OnClick);

    lblLayer.Menu = new Menu();
    lblLayer.Menu.Add(new MenuItem("Dummy item"));
    lblLayer.Menu.Popup += new EventHandler(layerMenu_Popup);

    { EventHandler click = new EventHandler(zoom_OnClick);
      lblZoom.Menu = new Menu();
      lblZoom.Menu.Add(new MenuItem("Tiny size (1/4x)", 'T')).Click += click;
      lblZoom.Menu.Add(new MenuItem("Normal size (1x)", 'N')).Click += click;
      lblZoom.Menu.Add(new MenuItem("Full size (4x)", 'F')).Click += click;
    }
    
    { EventHandler click = new EventHandler(mode_OnClick);
      lblMode.Menu = new Menu();
      lblMode.Menu.Add(new MenuItem("Objects", 'O')).Click += click;
      lblMode.Menu.Add(new MenuItem("Polygons", 'P')).Click += click;
      lblMode.Menu.Add(new MenuItem("View only", 'V')).Click += click;
    }
    
    lblType.Menu = new Menu();

    foreach(Menu m in menuBar.Menus)
    { m.BackColor = BackColor;
      m.SelectedBackColor = Color.FromArgb(80, 80, 80);
      m.SelectedForeColor = Color.White;
    }
    foreach(Menu m in new MenuBase[] { lblLayer.Menu, lblZoom.Menu, lblMode.Menu, lblType.Menu })
    { m.BackColor = BackColor;
      m.SelectedBackColor = Color.FromArgb(80, 80, 80);
      m.SelectedForeColor = Color.White;
    }

    lblLayer.Bounds = new Rectangle(Width-lblWidth, 0, lblWidth, lblHeight);
    lblMouse.Bounds = new Rectangle(Width-lblWidth, lblHeight, lblWidth, lblHeight);
    lblMode.Bounds  = new Rectangle(Width-lblWidth*2-lblPadding, 0, lblWidth, lblHeight);
    lblZoom.Bounds  = new Rectangle(Width-lblWidth*2-lblPadding, lblHeight, lblWidth, lblHeight);
    lblType.Bounds  = new Rectangle(Width-lblWidth*4-lblPadding*2, 0, lblWidth*2, lblHeight);
    lblType.TextAlign = ContentAlignment.TopRight;
    lblLayer.Anchor = lblMouse.Anchor = lblMode.Anchor = lblZoom.Anchor = lblType.Anchor = AnchorStyle.TopRight;

    MouseText = "0x0";
    ZoomText = "Full";
    Controls.AddRange(lblLayer, lblMouse, lblMode, lblZoom, lblType, menuBar);
    #endregion
  }

  public string LayerText { set { lblLayer.Text=value; } }
  public string ModeText { set { lblMode.Text="Mode: "+value; } }
  public string MouseText { set { lblMouse.Text=value; } }
  public string ZoomText { set { lblZoom.Text="Zoom: "+value; } }

  public MenuBar MenuBar { get { return menuBar; } }
  public MenuLabel TypeMenu { get { return lblType; } }

  public void New()
  { if(CanUnloadLevel())
    { App.Desktop.World.Clear();
      lastPath    = null;
      lastCompile = null;
    }
  }

  public void Load()
  { App.Desktop.StopKeyRepeat();
    string file = FileChooser.Load(Desktop, FileType.Directory, lastPath);
    if(file!="")
    { App.Desktop.World.Load(file);
      lastPath = file;
      lastCompile = null;
    }
  }

  public bool Save()
  { if(lastPath==null) return SaveAs();
    App.Desktop.World.Save(lastPath, false);
    App.Desktop.StatusText = lastPath+" saved.";
    return true;
  }

  public bool SaveAs()
  { string file = FileChooser.Save(Desktop, FileType.Directory, lastPath);
    if(file=="") return false;
    lastPath = file;
    return Save();
  }

  public void Compile()
  { if(lastCompile==null) { CompileInto(); }
    else if(MessageBox.Show(Desktop, "Compile?", "Compile level into '"+lastCompile+"'?", MessageBoxButtons.YesNo)==0)
    { App.Desktop.World.Save(lastCompile, true);
      App.Desktop.StatusText = "Level compiled into "+lastCompile+".";
    }
  }

  public void CompileInto()
  { string file = FileChooser.Save(Desktop, FileType.Directory, lastCompile);
    if(file=="") return;
    lastCompile = file;
    Compile();
  }

  public void Exit() { if(CanUnloadLevel()) GameLib.Events.Events.PushEvent(new SmarmQuitEvent()); }

  protected override void OnPaintBackground(PaintEventArgs e)
  { base.OnPaintBackground(e);
    Color color = Color.FromArgb(80, 80, 80);
    Primitives.HLine(e.Surface, e.DisplayRect.X, e.DisplayRect.Right-1, DisplayRect.Bottom-1, color);
    Primitives.VLine(e.Surface, Width-lblWidth*2-lblPadding*3/2, 0, Height-1, color);
    Primitives.VLine(e.Surface, Width-lblWidth-lblPadding/2, 0, Height-1, color);
  }

  bool CanUnloadLevel()
  { if(App.Desktop.World.World.ChangedSinceSave)
    { int button = MessageBox.Show(Desktop, "Save changes?", "This level has been altered. Save changes?",
                                   MessageBoxButtons.YesNoCancel);
      if(button==0) return Save();
      else if(button==1) return true;
      else return false;
    }
    return true;
  }

  #region Event handlers
  void new_OnClick(object sender, EventArgs e)   { New(); }
  void load_OnClick(object sender, EventArgs e)   { Load(); }
  void save_OnClick(object sender, EventArgs e)   { Save(); }
  void saveAs_OnClick(object sender, EventArgs e) { SaveAs(); }
  void compile_OnClick(object sender, EventArgs e) { Compile(); }
  void compileInto_OnClick(object sender, EventArgs e) { CompileInto(); }
  void exit_OnClick(object sender, EventArgs e)   { Exit(); }

  void editRect_OnClick(object sender, EventArgs e) { App.Desktop.World.EditRect(); }
  void moveRect_OnClick(object sender, EventArgs e) { App.Desktop.World.MoveRect(); }
  void objectProps_OnClick(object sender, EventArgs e) { App.Desktop.World.ShowObjectProperties(); }
  void levelProps_OnClick(object sender, EventArgs e) { App.Desktop.World.ShowLevelProperties(); }
  void smarmProps_OnClick(object sender, EventArgs e)
  { if(new ObjectProperties(App.SetupObject).Show(Desktop)) App.PropertiesUpdated();
  }

  void toggleFullscreen_OnClick(object sender, EventArgs e) { App.Fullscreen = !App.Fullscreen; }
  void toggleObjects_OnClick(object sender, EventArgs e)
  { App.Desktop.World.ShowObjects = !App.Desktop.World.ShowObjects;
  }
  void togglePolygons_OnClick(object sender, EventArgs e)
  { App.Desktop.World.ShowPolygons = !App.Desktop.World.ShowPolygons;
  }

  void toggleAntialias_OnClick(object sender, EventArgs e) { App.AntialiasText = !App.AntialiasText; }

  void layerMenu_Popup(object sender, EventArgs e)
  { Menu menu = (Menu)sender;
    Layer[] layers = App.Desktop.World.World.Layers;
    if(menu.Controls.Count != layers.Length+2)
    { EventHandler click = new EventHandler(layer_OnClick);
      MenuItemBase item;
      menu.Clear();
      for(int i=0; i<layers.Length; i++)
      { item = menu.Add(new MenuItem("Layer "+i));
        item.Click += click;
        item.Tag    = i;
      }
      menu.Add(new MenuItem("New Layer")).Click += click;
      item = menu.Add(new MenuItem("All Layers"));
      item.Click += click;
      item.Tag = -1;
    }
  }

  void mode_OnClick(object sender, EventArgs e)
  { MenuItemBase item = (MenuItemBase)sender;
    switch(item.HotKey)
    { case 'O': App.Desktop.World.EditMode = EditMode.Objects;  break;
      case 'P': App.Desktop.World.EditMode = EditMode.Polygons; break;
      case 'V': App.Desktop.World.EditMode = EditMode.ViewOnly; break;
    }
  }
  
  void layer_OnClick(object sender, EventArgs e)
  { MenuItemBase item = (MenuItemBase)sender;
    if(item.Tag==null) App.Desktop.World.AddLayer();
    else App.Desktop.World.SelectedLayer = (int)item.Tag;
  }

  void zoom_OnClick(object sender, EventArgs e)
  { MenuItemBase item = (MenuItemBase)sender;
    switch(item.HotKey)
    { case 'T': App.Desktop.World.ZoomMode = ZoomMode.Tiny;  break;
      case 'N': App.Desktop.World.ZoomMode = ZoomMode.Normal; break;
      case 'F': App.Desktop.World.ZoomMode = ZoomMode.Full; break;
    }
  }
  #endregion

  const int lblWidth=68, lblHeight=16, lblPadding=6;
  MenuLabel lblLayer=new MenuLabel(), lblMode=new MenuLabel(), lblZoom=new MenuLabel(), lblType=new MenuLabel();
  Label  lblMouse=new Label();
  MenuBar menuBar=new MenuBar();
  
  string lastPath, lastCompile;
}
#endregion

#region BottomBar
class BottomBar : ContainerControl
{ public BottomBar()
  { BackColor=Color.FromArgb(32, 32, 32);
    Controls.Add(lblStatus);
  }

  public string StatusText { get { return lblStatus.Text; } set { lblStatus.Text=value; } }

  protected override void OnPaintBackground(PaintEventArgs e)
  { base.OnPaintBackground(e);
    Primitives.HLine(e.Surface, e.DisplayRect.X, e.DisplayRect.Right-1, DisplayRect.Top, Color.FromArgb(64, 64, 64));
  }

  protected override void OnResize(EventArgs e)
  { lblStatus.Bounds = new Rectangle(3, 2, Width-6, Height-4);
    base.OnResize(e);
  }
  
  Label lblStatus = new Label(string.Format("Smarm version {0} loaded.", App.Version));
}
#endregion

#region WorldDisplay
enum EditMode { Objects, Polygons, ViewOnly };
enum ZoomMode { Full=1, Normal=4, Tiny=16 };
class WorldDisplay : Control
{ public WorldDisplay()
  { Style=ControlStyle.Clickable|ControlStyle.Draggable|ControlStyle.CanFocus|ControlStyle.BackingSurface;
    BackColor = Color.Black;
    dragThreshold = 4;
  }

  #region Properties
  public EditMode EditMode
  { get { return editMode; }
    set
    { if(value!=editMode)
      { App.Desktop.StatusText = "Entered "+value.ToString()+" mode.";
        subMode=SubMode.None;
        selectMode=SelectMode.None;
        editMode=value;

        if(typeSel==null) typeSel = new EventHandler(type_OnSelect);
        MenuLabel lbl = App.Desktop.TopBar.TypeMenu;
        lbl.Menu.Clear();
        if(editMode==EditMode.Objects)
        { foreach(ObjectDef obj in ObjectDef.Objects)
          { MenuItem item = new MenuItem(obj.Name);
            item.VerticalPadding = 1;
            item.Click += typeSel;
            item.Tag = obj;
            lbl.Menu.Add(item);
          }
          ZoomMode = ZoomMode.Normal;
          ShowObjects = true;
          App.Desktop.TopBar.ModeText = "Obj";
        }
        else if(editMode==EditMode.Polygons)
        { foreach(PolygonType type in PolygonType.Types)
          { MenuItem item = new MenuItem(type.Type);
            item.VerticalPadding = 1;
            item.Click += typeSel;
            item.Tag = type;
            lbl.Menu.Add(item);
          }
          ShowPolygons = true;
          App.Desktop.TopBar.ModeText = "Poly";
        }
        else
        { App.Desktop.TopBar.ModeText = "View";
          ShowPolygons = ShowObjects = true;
        }
        SelectedType = lbl.Menu.Controls.Count>0 ? lbl.Menu.Controls[0].Text : "";
        selected.Clear();
        if(layer==-1 && editMode!=EditMode.ViewOnly) SelectedLayer = 0;
        Invalidate();
      }
    }
  }

  public int SelectedLayer
  { get { return layer; }
    set
    { if(value!=layer)
      { if(value==-1) EditMode = EditMode.ViewOnly;
        layer = value;
        App.Desktop.TopBar.LayerText = layer==-1 ? "All Layers" : "Layer "+layer;
        selected.Clear();
        Invalidate();
      }
    }
  }

  public string SelectedType
  { get { return App.Desktop.TopBar.TypeMenu.Text; }
    set { App.Desktop.TopBar.TypeMenu.Text=value; }
  }
  
  public bool ShowObjects
  { get { return showObjs; }
    set { if(value!=showObjs) { showObjs=value; Invalidate(); } }
  }
  
  public bool ShowPolygons
  { get { return showPolys; }
    set { if(value!=showPolys) { showPolys=value; Invalidate(); } }
  }

  public World World { get { return world; } }
  
  public ZoomMode ZoomMode
  { get { return zoom; }
    set
    { if(value!=zoom)
      { Point cp = new Point(Width/2, Height/2), c1 = WindowToWorld(cp), c2;
        lastZoom = zoom;
        switch(zoom=value)
        { case ZoomMode.Full: App.Desktop.TopBar.ZoomText = "4x"; break;
          case ZoomMode.Normal: App.Desktop.TopBar.ZoomText = "1x"; break;
          case ZoomMode.Tiny: App.Desktop.TopBar.ZoomText = ".25x"; break;
        }
        if(zoom!=ZoomMode.Normal && EditMode==EditMode.Objects) EditMode=EditMode.ViewOnly;
        c2 = WindowToWorld(cp);
        x -= (c2.X-c1.X)*4; y -= (c2.Y-c1.Y)*4;
        Invalidate();
      }
    }
  }
  #endregion

  #region Public methods
  public void AddLayer()
  { int layer = new LayerPositionChooser().Show(Desktop);
    if(layer!=-1)
    { world.InsertLayer(-1);
      SelectedLayer = layer;
    }
    else App.Desktop.StatusText = "Layer creation aborted.";
  }

  public void Clear()
  { world.Clear();
    selected.Clear();
    x = y = 0;
    EditMode = EditMode.ViewOnly;
    SelectedLayer = -1;
    ZoomMode = ZoomMode.Full;
    lastZoom = ZoomMode.Normal;
    BackColor = world.BackColor;
    Invalidate();
  }

  public void EditRect()
  { if(!SelectRectangle("export")) goto abort;

    App.Desktop.StatusText = "Waiting for edit to complete...";
    try
    { ExportedImage image = world.ExportRect(WindowToWorld(selected.Rect), Font);
      bool fs = App.Fullscreen;
      if(fs) App.Fullscreen = false;
      System.Diagnostics.Process.Start(App.EditorPath, image.Filename);
      App.Fullscreen = fs;
      switch(MessageBox.Show(Desktop, "Import image",
                              "What do you want to do with the exported image ("+image.Filename+")?"+
                              " Choose an option after you're finished editing the image.",
                              new string[] { "Import & Delete", "Import & Keep", "Ignore & Delete", "Ignore & Keep" }))
      { case 0: world.ImportImage(image); System.IO.File.Delete(image.Filename); break;
        case 1: world.ImportImage(image); break;
        case 2: System.IO.File.Delete(image.Filename); break;
      }

      Invalidate();
      App.Desktop.StatusText = "Import completed.";
    }
    catch(Exception e)
    { App.Desktop.StatusText = "An error occurred during the export/import process.";
      MessageBox.Show(Desktop, "Error occurred", e.Message);
    }
    return;

    abort:
    App.Desktop.StatusText = "Edit process aborted.";
  }

  public void Load(string directory)
  { try
    { Clear();
      world.Load(directory);
      BackColor = world.BackColor;
      Invalidate();
      App.Desktop.StatusText = directory+" loaded.";
    }
    catch(Exception e)
    { Clear();
      MessageBox.Show(Desktop, "Error", string.Format("An error occurred while loading {0} -- {1}", directory,
                                                      e.Message));
      App.Desktop.StatusText = directory+" failed to load.";
    }
  }

  public void MoveRect()
  { if(!SelectRectangle("move")) goto abort;
    App.Desktop.StatusText = "Move the rectangle into position and left-click...";

    Rectangle rect = world.ExpandRect(WindowToWorld(selected.Rect));
    InvalidateObjs();
    InvalidatePolys();
    selected.Clear();
    
    { bool dontQuit=true;
      Rectangle wrect = rect;
      if(wrect.X<0) wrect.X=0;
      if(wrect.Y<0) wrect.Y=0;
      Size size = WorldToWindow(wrect).Size;

      dragImage = new Surface(size.Width, size.Height, 32);
      world.Render(dragImage, wrect.X*4, wrect.Y*4, dragImage.Bounds, zoom, World.AllLayers, null);
      while(dragImage!=null && (dontQuit=GameLib.Events.Events.PumpEvent()));
      if(!dontQuit) goto abort;
    }
    
    world.MoveRect(rect, selected.Rect.Location.X-rect.X, selected.Rect.Location.Y-rect.Y);
    App.Desktop.StatusText = "Move completed.";
    return;

    abort:
    App.Desktop.StatusText = "Move process aborted.";
  }

  public void Save(string directory, bool compile)
  { try
    { if(compile)
      { world.Compile(directory);
        if(App.CompilePost!="") System.Diagnostics.Process.Start(App.CompilePost, "\""+directory+'\"').WaitForExit();
      }
      else world.Save(directory);
    }
    catch(Exception e)
    { App.Desktop.StatusText = "An error occurred during the save/compile process.";
      MessageBox.Show(Desktop, "Error occurred", e.Message);
    }
  }
  
  public void ShowLevelProperties()
  { if(new ObjectProperties(world.Options).Show(Desktop))
    { BackColor = world.BackColor;
      world.ChangedSinceSave = true;
    }
  }

  public void ShowObjectProperties()
  { if(selected.Objs.Count>1) App.Desktop.StatusText = "Too many objects selected.";
    else if(selected.Objs.Count==0) App.Desktop.StatusText = "No object selected.";
    else
    { if(selected.Obj.Type.Properties.Length>0)
      { ObjectProperties props = new ObjectProperties(selected.Obj);
        if(props.Show(Desktop)) Invalidate(selected.Obj);
        SelectObject(selected.Obj); // just to update the text
      }
      else App.Desktop.StatusText = "Object '" + selected.Obj.Name + "' has no editable properties.";
    }
  }
  #endregion

  #region Painting
  protected override void OnPaint(PaintEventArgs e)
  { base.OnPaint(e);
    
    int xoff=e.DisplayRect.X, yoff=e.DisplayRect.Y;
    if(zoom==ZoomMode.Normal) { xoff*=4; yoff*=4; }
    else if(zoom==ZoomMode.Tiny) { xoff*=16; yoff*=16; }
    world.Render(e.Surface, x+xoff, y+yoff, e.DisplayRect, zoom,
                 showAll ? World.AllLayers : showObjs ? layer : World.NoLayer,
                 (Object[])selected.Objs.ToArray(typeof(Object)));

    if(!showPolys) return;
    foreach(Polygon poly in world.Polygons)
    { Rectangle rect = WorldToWindow(poly.Bounds);
      rect.Inflate(1, 1);
      if(!rect.IntersectsWith(e.WindowRect)) continue;

      Color c;
      if(poly.Points.Length<3)
        c = (selected.Polys.Contains(poly) ? Color.FromArgb(255, 0, 0) : Color.FromArgb(192, 0, 0));
      else
        c = selected.Polys.Contains(poly) ? poly.Color
                                          : Color.FromArgb(poly.Color.R*3/4, poly.Color.G*3/4, poly.Color.B*3/4);

      if(poly.Points.Length<3)
      { if(poly.Points.Length>1)
          Primitives.Line(e.Surface, WorldToWindow(poly.Points[0]), WorldToWindow(poly.Points[1]), c);
        for(int i=0; i<poly.Points.Length; i++)
          Primitives.Circle(e.Surface, WorldToWindow(new Point(poly.Points[i].X, poly.Points[i].Y)), 4, c);
      }
      else
      { Point[] points = (Point[])poly.Points.Clone();
        for(int i=0; i<points.Length; i++) points[i] = WorldToWindow(points[i]);
        Primitives.FilledPolygon(e.Surface, points, Color.FromArgb(64, c));
        for(int i=0; i<points.Length; i++) Primitives.Circle(e.Surface, points[i], 4, c);
      }
    }
    
    if(subMode==SubMode.DragRectangle) Primitives.Box(e.Surface, selected.Rect, Color.FromArgb(0, 255, 0));
    if(dragImage!=null)
      dragImage.Blit(e.Surface, WorldToWindow(world.ExpandRect(new Rectangle(mousePoint, new Size())).Location));
  }
  #endregion

  #region Other events
  protected override void OnMouseMove(GameLib.Events.MouseMoveEvent e)
  { if(!Focused) Focus();
    mousePoint = WindowToWorld(e.Point);
    App.Desktop.TopBar.MouseText = mousePoint.X.ToString()+'x'+mousePoint.Y.ToString();
    if(dragImage!=null) Invalidate();
    base.OnMouseMove(e);
  }
  
  protected override void OnMouseDown(ClickEventArgs e)
  { if(e.CE.MouseWheel)
    { if(e.CE.Button==MouseButton.WheelDown)
      { if(zoom==ZoomMode.Full) ZoomMode=ZoomMode.Normal;
        else if(zoom==ZoomMode.Normal) ZoomMode=ZoomMode.Tiny;
      }
      else if(e.CE.Button==MouseButton.WheelUp)
      { if(zoom==ZoomMode.Normal) ZoomMode=ZoomMode.Full;
        else if(zoom==ZoomMode.Tiny) ZoomMode=ZoomMode.Normal;
      }
      e.Handled = true;
    }
    base.OnMouseDown(e);
  }

  protected override void OnMouseClick(ClickEventArgs e)
  { if(dragImage!=null && e.CE.Button==MouseButton.Left)
    { selected.Rect.Location = WindowToWorld(e.CE.Point);
      dragImage.Dispose();
      dragImage = null;
      e.Handled = true;
    }
    else if(subMode==SubMode.DragRectangle || selectMode!=SelectMode.None)
    { // do nothing (but prevent the other if blocks from executing)
    }
    else if(editMode==EditMode.Polygons)
    { if(e.CE.Button==MouseButton.Left)
      { e.CE.Point = WindowToWorld(e.CE.Point);
        if(subMode==SubMode.None)
        { if(Keyboard.HasOnlyKeys(KeyMod.Ctrl) || !ClickPolygon(e.CE.Point))
          { InvalidatePolys();
            selected.Poly = new Polygon(SelectedType);
            world.Polygons.Add(selected.Poly);
            subMode = SubMode.NewPoly;
          }
        }
        if(subMode==SubMode.NewPoly)
        { if(Keyboard.HasOnlyKeys(KeyMod.Shift) && selected.Poly.Points.Length>0)
          { Point last = selected.Poly.Points[selected.Poly.Points.Length-1];
            int xd = Math.Abs(e.CE.X-last.X), yd = Math.Abs(e.CE.Y-last.Y);
            if(yd>xd) e.CE.X=last.X;
            else e.CE.Y=last.Y;
          }
          selected.Poly.AddPoint(e.CE.Point);
          Invalidate(selected.Poly);
          world.ChangedSinceSave = true;
        }
        e.Handled = true;
      }
      else if(e.CE.Button==MouseButton.Middle && subMode==SubMode.NewPoly)
      { RemoveLastPoint();
        e.Handled=true;
      }
    }
    else if(editMode==EditMode.Objects)
    { if(e.CE.Button==MouseButton.Left)
      { e.CE.Point = WindowToWorld(e.CE.Point);
        if(!ClickObject(e.CE.Point))
        { Object obj = new Object(SelectedType);
          e.CE.X -= obj.Width/2;
          e.CE.Y -= obj.Height/2;
          obj.Location = e.CE.Point;
          world.Layers[layer].Objects.Add(obj);
          world.ChangedSinceSave = true;
          SelectObject(obj);
        }
        InvalidateObjs();
        e.Handled=true;
      }
    }
    base.OnMouseClick(e);
  }

  protected override void OnKeyDown(KeyEventArgs e)
  { if(selectMode!=SelectMode.None || subMode==SubMode.DragRectangle)
    { if(e.KE.Key==Key.Escape)
      { selectMode = SelectMode.None;
        subMode = SubMode.None;
        Invalidate();
        e.Handled=true;
      }
    }
    else if(e.KE.Key==Key.Tab && !showAll)
    { showAll=true;
      Invalidate();
      e.Handled=true;
    }
    else if(e.KE.KeyMods==KeyMod.None)
    { e.Handled = true;
      if(e.KE.Key==Key.Left)       { x -= Width*9/10*(int)zoom; Invalidate(); }
      else if(e.KE.Key==Key.Right) { x += Width*9/10*(int)zoom; Invalidate(); }
      else if(e.KE.Key==Key.Up)    { y -= Height*9/10*(int)zoom; Invalidate(); }
      else if(e.KE.Key==Key.Down)  { y += Height*9/10*(int)zoom; Invalidate(); }
      else if(e.KE.Key==Key.Home)     { x -= Width*4*(int)zoom; Invalidate(); }
      else if(e.KE.Key==Key.End)      { x += Width*4*(int)zoom; Invalidate(); }
      else if(e.KE.Key==Key.PageUp)   { y -= Height*4*(int)zoom; Invalidate(); }
      else if(e.KE.Key==Key.PageDown) { y += Height*4*(int)zoom; Invalidate(); }
      else if(e.KE.Key==Key.Space) ZoomMode = lastZoom;
      else if(e.KE.Char=='[')
      { switch(zoom)
        { case ZoomMode.Full:   ZoomMode=ZoomMode.Normal; break;
          case ZoomMode.Normal: ZoomMode=ZoomMode.Tiny; break;
          case ZoomMode.Tiny:   ZoomMode=ZoomMode.Full; break;
        }
      }
      else if(e.KE.Char==']')
      { switch(zoom)
        { case ZoomMode.Full:   ZoomMode=ZoomMode.Tiny; break;
          case ZoomMode.Normal: ZoomMode=ZoomMode.Full; break;
          case ZoomMode.Tiny:   ZoomMode=ZoomMode.Normal; break;
        }
      }
      else if(e.KE.Char=='p') EditMode = EditMode.Polygons;
      else if(e.KE.Char=='o') EditMode = EditMode.Objects;
      else if(e.KE.Key==Key.Backquote) SelectedLayer = 0;
      else if(e.KE.Char>='0' && e.KE.Char<='9')
      { int layer = e.KE.Char-'0';
        if(layer<world.Layers.Length) SelectedLayer = layer;
      }
      else e.Handled = false;
    }
    if(e.Handled) goto done;
    if(editMode==EditMode.Polygons)
    { e.Handled=true;
      if(e.KE.Key==Key.Delete) RemoveSelectedPolys();
      else if((e.KE.Key==Key.Enter || e.KE.Key==Key.KpEnter) && selected.Polys.Count>0)
      { InvalidatePolys();
        selected.Polys.Clear();
        subMode = SubMode.None;
      }
      else if(subMode==SubMode.NewPoly)
      { if(e.KE.Key==Key.Backspace) RemoveLastPoint();
        else if(e.KE.Key==Key.Escape) RemoveSelectedPolys();
        else e.Handled=false;
      }
    }
    else if(editMode==EditMode.Objects)
    { e.Handled=true;
      if(e.KE.Key==Key.Delete && selected.Objs.Count>0)
      { InvalidateObjs();
        foreach(Object obj in selected.Objs) world.Layers[layer].Objects.Remove(obj);
        selected.Objs.Clear();
        world.ChangedSinceSave = true;
      }
      else if((e.KE.Key==Key.Enter || e.KE.Key==Key.KpEnter) && selected.Objs.Count>0)
      { InvalidateObjs();
        selected.Objs.Clear();
      }
      else e.Handled=false;
    }
    done:
    if(e.Handled) Desktop.StopKeyRepeat();
    base.OnKeyDown(e);
  }

  protected override void OnKeyUp(KeyEventArgs e)
  { if(e.KE.Key==Key.Tab && showAll)
    { showAll=false;
      Invalidate();
      e.Handled=true;
    }
    base.OnKeyUp(e);
  }

  protected override void OnDragStart(DragEventArgs e)
  { Point pt = WindowToWorld(e.Start);
    if(editMode==EditMode.Polygons)
    { if(subMode==SubMode.None || subMode==SubMode.NewPoly)
      { if(e.OnlyPressed(MouseButton.Left) && (subMode==SubMode.NewPoly ? ClickVertex(pt) : ClickPolygon(pt)))
        { oldSubMode=subMode;
          subMode=SubMode.DragSelected;
          goto done;
        }
        else if(subMode==SubMode.NewPoly && !e.OnlyPressed(MouseButton.Right))
        { e.Cancel=true;
          goto done;
        }
      }
    }
    else if(editMode==EditMode.Objects && subMode==SubMode.None && e.OnlyPressed(MouseButton.Left) && ClickObject(pt))
    { subMode=SubMode.DragSelected;
      goto done;
    }

    if(e.OnlyPressed(MouseButton.Left)) subMode = SubMode.DragRectangle;
    else if(!e.OnlyPressed(MouseButton.Right)) e.Cancel=true;

    done:
    base.OnDragStart(e);
  }
  
  protected override void OnDragMove(DragEventArgs e)
  { if(e.Pressed(MouseButton.Right)) DragScroll(e);
    else if(e.Pressed(MouseButton.Left))
    { if(subMode==SubMode.DragSelected)
      { if(editMode==EditMode.Polygons) DragPoly(e);
        else if(editMode==EditMode.Objects) DragObject(e);
      }
      else if(subMode==SubMode.DragRectangle) DragRect(e);
    }
    base.OnDragMove(e);
  }

  protected override void OnDragEnd(DragEventArgs e)
  { if(e.Pressed(MouseButton.Right)) DragScroll(e);
    else if(e.Pressed(MouseButton.Left))
    { if(subMode==SubMode.DragSelected)
      { if(editMode==EditMode.Polygons) { DragPoly(e); subMode=oldSubMode; }
        else
        { if(editMode==EditMode.Objects) DragObject(e);
          subMode = SubMode.None;
        }
      }
      else if(subMode==SubMode.DragRectangle)
      { selected.Rect = e.Rectangle;
        subMode = SubMode.None;
        if(selectMode==SelectMode.None)
        { selected.Rect = WindowToWorld(selected.Rect);
          if(editMode==EditMode.Objects) SelectObjects(selected.Rect);
          else if(editMode==EditMode.Polygons) SelectPolygons(selected.Rect);
        }
        else selectMode = SelectMode.Done;
        Invalidate();
      }
    }
    base.OnDragEnd(e);
  }

  void type_OnSelect(object sender, EventArgs e)
  { if(editMode==EditMode.Objects)
    { ObjectDef def = (ObjectDef)((MenuItemBase)sender).Tag;
      App.Desktop.TopBar.TypeMenu.Text = def.Name;
    }
    else if(editMode==EditMode.Polygons)
    { PolygonType def = (PolygonType)((MenuItemBase)sender).Tag;
      App.Desktop.TopBar.TypeMenu.Text = def.Type;
    }
  }
  #endregion

  enum SubMode { None, NewPoly, DragSelected, DragRectangle };
  enum SelectMode { None, Selecting, Done };

  class Selection
  { public void Clear()
    { Rect = new Rectangle();
      Objs.Clear();
      Polys.Clear();
    }

    public Object Obj
    { get
      { if(Objs.Count!=1) throw new InvalidOperationException("There can be only one!");
        return (Object)Objs[0];
      }
      set
      { if(Objs.Count!=1 || Objs[0]!=value)
        { Objs.Clear();
          Objs.Add(value);
        }
      }
    }

    public Polygon Poly
    { get
      { if(Polys.Count!=1) throw new InvalidOperationException("There can be only one!");
        return (Polygon)Polys[0];
      }
      set
      { if(Polys.Count!=1 || Polys[0]!=value)
        { Polys.Clear();
          Polys.Add(value);
        }
      }
    }

    public Rectangle Rect;
    public ArrayList Objs = new ArrayList(), Polys = new ArrayList();
  }

  #region Private methods
  bool ClickVertex(Point point)
  { point = WorldToWindow(point);
    foreach(Polygon poly in world.Polygons)
      for(int i=0; i<poly.Points.Length; i++)
      { Point p = WorldToWindow(poly.Points[i]);
        int xd=p.X-point.X, yd=p.Y-point.Y;
        if(xd*xd+yd*yd<=32)
        { InvalidatePolys();
          selected.Poly = poly;
          selectedPoint = i;
          Invalidate(poly);
          return true;
        }
      }
    return false;
  }

  bool ClickObject(Point point)
  { foreach(Object obj in world.Layers[layer].Objects)
      if(obj.Bounds.Contains(point))
      { if(!selected.Objs.Contains(obj))
        { selected.Objs.Add(obj);
          Invalidate(obj);
        }
        return true;
      }
    return false;
  }

  bool ClickPolygon(Point point)
  { if(ClickVertex(point)) return true;
    selectedPoint = -1;
    foreach(Polygon poly in world.Polygons)
      if(poly.Points.Length>2 && poly.Bounds.Contains(point))
      { try
        { foreach(GameLib.Mathematics.TwoD.Polygon glcvPoly in poly.ToGLPolygon().SplitIntoConvexPolygons())
            if(glcvPoly.ConvexContains(point))
            { if(!selected.Polys.Contains(poly)) selected.Polys.Add(poly);
              Invalidate(poly);
              return true;
            }
        }
        catch(Exception e) { MessageBox.Show(Desktop, "Error", e.Message); }
      }
    return false;
  }

  void DragScroll(DragEventArgs e)
  { int xd=e.End.X-e.Start.X, yd=e.End.Y-e.Start.Y;
    if(zoom==ZoomMode.Normal) { xd*=4; yd*=4; }
    else if(zoom==ZoomMode.Tiny) { xd*=16; yd*=16; }
    x -= xd; y -= yd;
    e.Start = e.End;
    Invalidate();
  }
  
  void DragPoly(DragEventArgs e)
  { int xd=e.End.X-e.Start.X, yd=e.End.Y-e.Start.Y;
    if(zoom==ZoomMode.Full) { xd/=4; yd/=4; }
    else if(zoom==ZoomMode.Tiny) { xd*=4; yd*=4; }
    if(xd!=0 || yd!=0)
    { InvalidatePolys();
      if(selectedPoint!=-1)
      { selected.Poly.Points[selectedPoint].X += xd;
        selected.Poly.Points[selectedPoint].Y += yd;
      }
      else
        foreach(Polygon poly in selected.Polys)
          for(int i=0; i<poly.Points.Length; i++) poly.Points[i].Offset(xd, yd);
      InvalidatePolys();
      if(zoom==ZoomMode.Full) { e.Start.X += xd*4; e.Start.Y += yd*4; }
      else e.Start = e.End;
      world.ChangedSinceSave = true;
    }
  }
  
  void DragObject(DragEventArgs e)
  { int xd=e.End.X-e.Start.X, yd=e.End.Y-e.Start.Y;
    if(zoom==ZoomMode.Full) { xd/=4; yd/=4; }
    else if(zoom==ZoomMode.Tiny) { xd*=4; yd*=4; }
    if(xd!=0 || yd!=0)
    { foreach(Object obj in selected.Objs)
      { Point pos = obj.Location;
        pos.Offset(xd, yd);
        Invalidate(obj);
        obj.Location = pos;
        Invalidate(obj);
      }
      if(zoom==ZoomMode.Full) { e.Start.X += xd*4; e.Start.Y += yd*4; }
      else e.Start = e.End;
      world.ChangedSinceSave = true;
    }
  }

  void DragRect(DragEventArgs e)
  { Invalidate(selected.Rect);
    Invalidate(e.Rectangle);
    selected.Rect = e.Rectangle;
  }

  void Invalidate(Object obj) { Invalidate(WorldToWindow(obj.Bounds)); }
  
  void Invalidate(Polygon poly)
  { Rectangle rect = WorldToWindow(poly.Bounds);
    rect.Inflate(5, 5); // to account for the point markers
    Invalidate(rect);
  }

  void InvalidateObjs() { foreach(Object obj in selected.Objs) Invalidate(obj); }
  void InvalidatePolys() { foreach(Polygon poly in selected.Polys) Invalidate(poly); }

  void RemoveLastPoint()
  { if(selected.Poly.Points.Length==1) RemoveSelectedPolys();
    else
    { Invalidate(selected.Poly);
      selected.Poly.RemoveLastPoint();
      world.ChangedSinceSave = true;
    }
  }
  
  void RemoveSelectedPolys()
  { foreach(Polygon poly in selected.Polys)
    { Invalidate(poly);
      world.Polygons.Remove(poly);
    }
    selected.Polys.Clear();
    subMode = SubMode.None;
    world.ChangedSinceSave = true;
  }
  
  void SelectObject(Object obj)
  { if(selected.Objs.Count==1 && selected.Obj!=obj || !selected.Objs.Contains(obj))
    { InvalidateObjs();
      selected.Obj = obj;
      if(obj!=null) Invalidate(obj);
    }
    if(obj==null) App.Desktop.StatusText="";
    else
    { string text = obj.Name;
      Property[] props = obj.Type.Properties;
      for(int i=0; i<props.Length; i++)
      { text += (i==0 ? ':' : ',');
        text += " "+props[i].Name+'='+obj[props[i].Name];
      }
      App.Desktop.StatusText = text;
    }
  }

  void SelectObjects(Rectangle rect)
  { foreach(Object obj in world.Layers[layer].Objects)
    { Point pt = obj.Location;
      pt.Offset(obj.Width/2, obj.Height/2);
      if(rect.Contains(pt) && !selected.Objs.Contains(obj)) selected.Objs.Add(obj);
    }
  }

  void SelectPolygons(Rectangle rect)
  { foreach(Polygon poly in world.Polygons)
      if(rect.Contains(poly.Centroid) && !selected.Polys.Contains(poly)) selected.Polys.Add(poly);
  }

  bool SelectRectangle(string action)
  { bool dontQuit=true;
    selectMode = SelectMode.Selecting;

    App.Desktop.StatusText = "Select the rectangle to " + action + "...";
    while(selectMode==SelectMode.Selecting && (dontQuit=GameLib.Events.Events.PumpEvent()));
    if(selectMode==SelectMode.None || !dontQuit) goto abort;
    return true;

    abort:
    App.Desktop.StatusText = "Selection cancelled.";
    return false;
  }

  Point WindowToWorld(Point windowPoint)
  { if(zoom==ZoomMode.Normal) { windowPoint.X += (x+2)/4; windowPoint.Y += (y+2)/4; }
    else if(zoom==ZoomMode.Full) { windowPoint.X = (windowPoint.X+x+2)/4; windowPoint.Y = (windowPoint.Y+y+2)/4; }
    else if(zoom==ZoomMode.Tiny)
    { windowPoint.X = (windowPoint.X+(x+8)/16)*4;
      windowPoint.Y = (windowPoint.Y+(y+8)/16)*4;
    }
    return windowPoint;
  }
  Rectangle WindowToWorld(Rectangle windowRect)
  { windowRect.Location = WindowToWorld(windowRect.Location);
    if(zoom==ZoomMode.Full) { windowRect.Width=(windowRect.Width+2)/4; windowRect.Height=(windowRect.Height+2)/4; }
    else if(zoom==ZoomMode.Tiny) { windowRect.Width*=4; windowRect.Height*=4; }
    return windowRect;
  }
  Point WorldToWindow(Point worldPoint)
  { if(zoom==ZoomMode.Normal) { worldPoint.X -= x/4; worldPoint.Y -= y/4; }
    else if(zoom==ZoomMode.Full) { worldPoint.X = worldPoint.X*4-x; worldPoint.Y = worldPoint.Y*4-y; }
    else if(zoom==ZoomMode.Tiny) { worldPoint.X = worldPoint.X/4-x/16; worldPoint.Y = worldPoint.Y/4-y/16; }
    return worldPoint;
  }
  Rectangle WorldToWindow(Rectangle worldRect)
  { worldRect.Location = WorldToWindow(worldRect.Location);
    if(zoom==ZoomMode.Full) { worldRect.Width*=4; worldRect.Height*=4; }
    else if(zoom==ZoomMode.Tiny)
    { worldRect.Width=(worldRect.Width+2)/4;
      worldRect.Height=(worldRect.Height+2)/4;
    }
    return worldRect;
  }
  #endregion

  World world = new World();
  EventHandler typeSel;
  Selection selected = new Selection();
  Point mousePoint;
  int x, y, layer, selectedPoint;
  EditMode editMode;
  SubMode  subMode, oldSubMode;
  ZoomMode zoom, lastZoom;
  SelectMode selectMode;
  Surface dragImage;
  bool     showAll, showObjs, showPolys;
}
#endregion

#region FileChooser
[Flags] enum FileType { File=1, Directory=2, Both=File|Directory }
class FileChooser : Form
{ public FileChooser() : this(WithSlash(System.IO.Directory.GetCurrentDirectory())) { }
  public FileChooser(string initialPath)
  { path.Text=initialPath.Replace('\\', '/');
    KeyPreview=true;
    Controls.AddRange(label, path);
  }

  public FileType AllowedTypes { get { return type; } set { type=value; } }
  public bool ExistingOnly { get { return existing; } set { existing=value; } }
  public bool OverwriteWarning { get { return warn; } set { warn=value; } }

  public string Show(DesktopControl desktop)
  { GameLib.Fonts.Font font = RawFont==null ? desktop.Font : RawFont;
    if(font!=null)
    { int pad=6, sep=4;
      SetDefaultLabel();
      int width  = Math.Max(font.CalculateSize(label.Text).Width+pad*2, desktop.Width/2);
      int height = pad*2+sep+font.LineSkip*5/2;

      Bounds = new Rectangle((desktop.Width-width)/2, (desktop.Height-height)/2, width, height);
      label.Bounds = new Rectangle(pad, pad, Width-pad*2, font.LineSkip+1);
      path.Bounds = new Rectangle(pad, label.Bottom+sep, Width-pad*2, font.LineSkip*3/2);
      path.SelectOnFocus = false;
      path.Focus();
      path.CaretPosition = path.Text.Length;
    }
    ShowDialog(desktop);
    return path.Text;
  }

  protected override void OnKeyDown(KeyEventArgs e)
  { if(!e.Handled && e.KE.KeyMods==KeyMod.None)
    { if(e.KE.Key==Key.Enter || e.KE.Key==Key.KpEnter)
      { Desktop.StopKeyRepeat();
        try
        { bool exists =
            (type&FileType.Directory)!=0 && System.IO.Directory.Exists(path.Text) ||
            (type&FileType.File)!=0 && System.IO.File.Exists(path.Text);
          if(exists && warn &&
             MessageBox.Show(Desktop, "File exists", "That "+TypeString+" already exists. Overwrite?",
                             MessageBoxButtons.YesNo)==1)
            goto abort;
          if(existing && !exists) { label.Text = "That "+TypeString+" does not exist."; goto abort; }
          Close();
        }
        catch { }
        abort:
        e.Handled=true;
      }
      else if(e.KE.Key==Key.Tab)
      { Desktop.StopKeyRepeat();
        string path = this.path.Text;
        try
        { if(!System.IO.File.Exists(path))
          { int pos = path.LastIndexOfAny(new char[] { '/', '\\' });
            if(pos!=-1)
            { string dirname = path.Substring(0, pos);
              if(pos==2 && dirname[1]==':') dirname += '/';
              System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(dirname);
              string[] names;
              { string search = pos==path.Length-1 ? null : path.Substring(pos+1)+'*';
                System.IO.FileInfo[] files = (type&FileType.File)==0 ? null : search==null ? dir.GetFiles() : dir.GetFiles(search);
                System.IO.DirectoryInfo[] dirs = search==null ? dir.GetDirectories() : dir.GetDirectories(search);
                names = new string[(files==null ? 0 : files.Length) + dirs.Length];
                int i=0;
                foreach(System.IO.DirectoryInfo d in dirs) names[i++] = d.Name+'/';
                if(files!=null) foreach(System.IO.FileInfo f in files) names[i++] = f.Name;
                if(names.Length>0)
                { if(names.Length==1) { SetDefaultLabel(); i=names[0].Length+1; }
                  else
                  { string name = names[0].ToLower();
                    for(i=0; i<=name.Length; i++)
                    { char c = char.ToLower(name[i]);
                      for(int j=1; j<names.Length; j++)
                        if(char.ToLower(names[j][i])!=c)
                        { string alts = string.Empty;
                          if(i!=0) c = char.ToLower(name[i-1]);
                          for(j=0; j<names.Length; j++)
                            if(i==0 || char.ToLower(names[j][i-1])==c) alts += (alts.Length>0 ? " " : "") + names[j];
                          label.Text = alts;
                          goto done;
                        }
                    }
                    SetDefaultLabel();
                  }
                  i--;
                  done:
                  if(i>0)
                  { this.path.Text = path.Substring(0, pos+1)+names[0].Substring(0, i);
                    this.path.Select(this.path.Text.Length, 0);
                  }
                }
              }
            }
          }
        }
        catch { }
        e.Handled=true;
      }
      else if(e.KE.Key==Key.Escape)
      { path.Text="";
        Close();
        e.Handled=true;
      }
    }
    base.OnKeyDown(e);
  }
  
  string TypeString
  { get
    { if(type==FileType.File) return "file";
      else if(type==FileType.Directory) return "directory";
      else return "file/directory";
    }
  }

  void SetDefaultLabel() { label.Text = "Choose a "+TypeString+" and press enter:"; }
  
  static string WithSlash(string s)
  { char c=s[s.Length-1];
    if(c!='/' && c!='\\') s += '/';
    return s;
  }

  public static string Load(DesktopControl desktop, FileType type) { return Load(desktop, type, null); }
  public static string Load(DesktopControl desktop, FileType type, string initialPath)
  { FileChooser file = initialPath==null ? new FileChooser() : new FileChooser(initialPath);
    file.ExistingOnly = true;
    file.AllowedTypes = type;
    return file.Show(desktop);
  }
  
  public static string Save(DesktopControl desktop, FileType type) { return Save(desktop, type, null); }
  public static string Save(DesktopControl desktop, FileType type, string initialPath)
  { FileChooser file = initialPath==null ? new FileChooser() : new FileChooser(initialPath);
    file.OverwriteWarning = true;
    file.AllowedTypes = type;
    return file.Show(desktop);
  }

  Label    label = new Label();
  TextBox  path  = new TextBox();
  FileType type  = FileType.File;
  bool    existing, warn;
}
#endregion

#region LayerPositionChooser
class LayerPositionChooser : Form
{ public LayerPositionChooser()
  { pos.Text = App.Desktop.World.World.Layers.Length.ToString();
    label.Text = string.Format("Where should this layer be inserted (0 to {0})?", pos.Text);
    KeyPreview = true;
    Controls.AddRange(label, pos);
  }

  public int Show(DesktopControl desktop)
  { GameLib.Fonts.Font font = RawFont==null ? desktop.Font : RawFont;
    if(font!=null) // TODO: I should really use a base class that both this and FileChooser derive from...
    { int pad=6, sep=4;
      int width  = Math.Max(font.CalculateSize(label.Text).Width+pad*2, desktop.Width/2);
      int height = pad*2+sep+font.LineSkip*5/2;

      Bounds = new Rectangle((desktop.Width-width)/2, (desktop.Height-height)/2, width, height);
      label.Bounds = new Rectangle(pad, pad, Width-pad*2, font.LineSkip+1);
      pos.Bounds = new Rectangle(pad, label.Bottom+sep, Width-pad*2, font.LineSkip*3/2);
      pos.SelectOnFocus = false;
      pos.Focus();
    }
    ShowDialog(desktop);
    return int.Parse(pos.Text);
  }

  protected override void OnKeyDown(KeyEventArgs e)
  { if(!e.Handled && e.KE.KeyMods==KeyMod.None)
    { if(e.KE.Key==Key.Enter || e.KE.Key==Key.KpEnter)
      { Desktop.StopKeyRepeat();
        try
        { int pos = int.Parse(this.pos.Text), max = App.Desktop.World.World.Layers.Length;
          if(pos<0 || pos>max) label.Text = string.Format("Out of range! (Choose from 0 to {0})", max);
          else Close();
        }
        catch { label.Text = "The position must be a valid integer."; }
        e.Handled=true;
      }
      else if(e.KE.Key==Key.Escape)
      { pos.Text="-1";
        Close();
        e.Handled=true;
      }
    }
    base.OnKeyDown(e);
  }
  
  Label    label = new Label();
  TextBox  pos   = new TextBox();
}
#endregion

#region SmarmDesktop
class SmarmDesktop : DesktopControl
{ public SmarmDesktop()
  { AutoFocusing = AutoFocus.None;
    KeyPreview   = true;
    KeyRepeatDelay = 350;
    ForeColor    = Color.White;
    BackColor    = Color.Black;
    Controls.AddRange(topBar, bottomBar, world);
    world.Focus();
  }

  public TopBar TopBar { get { return topBar; } }
  public string StatusText { get { return bottomBar.StatusText; } set { bottomBar.StatusText=value; } }
  public WorldDisplay World { get { return world; } }

  protected override void OnKeyDown(KeyEventArgs e)
  { if(!e.Handled && e.KE.Down && e.KE.HasOnlyKeys(KeyMod.Alt|KeyMod.Ctrl))
    { if(e.KE.Key==Key.Enter || e.KE.Key==Key.KpEnter)
      { StopKeyRepeat();
        App.Fullscreen = !App.Fullscreen;
        e.Handled = true;
      }
    }
    if(!e.Handled && ModalWindow==null && topBar.MenuBar.HandleKey(e.KE)) e.Handled = true;
    base.OnKeyDown(e);
  }

  protected override void OnResize(EventArgs e)
  { topBar.Bounds = new Rectangle(0, 0, Width, 32);
    bottomBar.Bounds = new Rectangle(0, Height-32, Width, 32);
    world.Bounds = new Rectangle(0, 32, Width, Height-32-32);
    base.OnResize(e);
  }

  TopBar topBar = new TopBar();
  BottomBar bottomBar = new BottomBar();
  WorldDisplay world = new WorldDisplay();
}
#endregion

#region ObjectProperties
class ObjectProperties : Form
{ public ObjectProperties(Object obj) { this.obj=obj; KeyPreview=true; }

  public bool Show(DesktopControl desktop)
  { AutoFocus focus = desktop.AutoFocusing;
    GameLib.Fonts.Font font = RawFont==null ? desktop.Font : RawFont;
    if(font!=null)
    { int xpad=font.LineSkip, ypad=font.LineSkip, width=0, height=ypad, yinc = Math.Max(font.LineSkip, font.Height)+6;
      int tab=0;
      foreach(Property prop in obj.Type.Properties)
      { int w = font.CalculateSize(prop.Name+':').Width;
        if(w>width) width=w;
      }
      foreach(Property prop in obj.Type.Properties)
      { Label label = new Label(prop.Name+':');
        label.Bounds = new Rectangle(xpad-font.LineSkip/2, height, width+font.LineSkip/2, yinc);
        label.TextAlign = ContentAlignment.MiddleRight;

        Control ctl;
        if(prop.Type=="bool")
        { CheckBox check = new CheckBox();
          if(obj[prop.Name]!=null) check.Checked = (bool)obj[prop.Name];
          ctl = check;
        }
        else
        { TextBox tb = new TextBox();
          if(obj[prop.Name]!=null)
          { if(prop.Type=="color")
            { Color c = (Color)obj[prop.Name];
              tb.Text = string.Format("{0},{1},{2}", c.R, c.G, c.B);
            }
            else tb.Text = obj[prop.Name].ToString();
          }
          ctl = tb;
        }
        ctl.Bounds = new Rectangle(label.Right+xpad, height, 200, yinc);
        ctl.Name = prop.Name;
        ctl.TabIndex = tab++;
        Controls.AddRange(label, ctl);
        if(tab==1) ctl.Focus();

        height += yinc;
      }

      width += xpad*3 + 200;
      height += ypad;

      { int btnHeight = font.LineSkip*3/2;
        Button ok = new Button("Ok");
        ok.Bounds = new Rectangle(width/6, height, 40, btnHeight);
        ok.Click += new ClickEventHandler(ok_Click);
        ok.TabIndex = tab++;

        Button cancel = new Button("Cancel");
        cancel.Click += new ClickEventHandler(cancel_Click);
        cancel.Bounds = new Rectangle(width-width/6-60, height, 60, btnHeight);
        cancel.TabIndex = tab++;

        Controls.AddRange(ok, cancel);
        height += btnHeight;
      }

      Size = new Size(width, height+ypad);
      Location = new Point((desktop.Width-Width)/2, (desktop.Height-Height)/2);
    }
    DialogResult = false;
    if(focus==AutoFocus.None) desktop.AutoFocusing = AutoFocus.Click;
    object ret = ShowDialog(desktop);
    desktop.AutoFocusing = focus;
    return (bool)ret;
  }

  protected override void OnKeyDown(KeyEventArgs e)
  { if(e.KE.KeyMods==KeyMod.None)
    { if(e.KE.Key==Key.Enter) { ok_Click(null, null); e.Handled=true; }
      else if(e.KE.Key==Key.Escape) { cancel_Click(null, null); e.Handled=true; }
    }
    base.OnKeyDown(e);
  }

  private void ok_Click(object sender, ClickEventArgs e)
  { foreach(Property prop in obj.Type.Properties)
    { if(prop.Type!="bool")
      { TextBox tb = (TextBox)Controls.Find(prop.Name);
        string error = prop.Validate(tb.Text);
        if(error!=null)
        { MessageBox.Show(Desktop, "Validation Error", string.Format("{0}: {1}", prop.Name, error));
          return;
        }
      }
    }
    foreach(Property prop in obj.Type.Properties)
    { if(prop.Type=="bool") obj[prop.Name] = ((CheckBox)Controls.Find(prop.Name)).Checked;
      else obj[prop.Name] = ((TextBox)Controls.Find(prop.Name)).Text;
    }
    DialogResult = true;
    Close();
  }

  private void cancel_Click(object sender, ClickEventArgs e) { Close(); }
  
  Object obj;
}
#endregion

} // namespace Smarm