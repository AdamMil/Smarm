/*
Smarm is an editor for the game Swarm, which was written by Jim Crawford. 
http://www.adammil.net
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

using System;
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
  { if(!e.Handled && e.CE.Button==0 && menu!=null && menu.Controls.Count>0)
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
    menu.Add(new MenuItem("Compile...", 'C')).Click += new EventHandler(compile_OnClick);
    menu.Add(new MenuItem("Exit", 'X', new KeyCombo(KeyMod.Ctrl, 'X'))).Click += new EventHandler(exit_OnClick);

    menu = menuBar.Add(new Menu("Edit", new KeyCombo(KeyMod.Alt, 'E')));
    menu.Add(new MenuItem("Edit in paint program", 'E')).Click += new EventHandler(exportRect_OnClick);
    menu.Add(new MenuItem("Object properties...", 'O', new KeyCombo(KeyMod.None, Key.F4))).Click += new EventHandler(objectProps_OnClick);
    menu.Add(new MenuItem("Level properties...", 'L'));
    menu.Add(new MenuItem("Smarm properties...", 'S')).Click += new EventHandler(smarmProps_OnClick);
    
    menu = menuBar.Add(new Menu("View", new KeyCombo(KeyMod.Alt, 'V')));
    menu.Add(new MenuItem("Toggle Fullscreen", 'F', new KeyCombo(KeyMod.Alt, Key.Enter))).Click += new EventHandler(toggleFullscreen_OnClick);

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

  public bool AntialiasText
  { get { return ((GameLib.Fonts.TrueTypeFont)Desktop.Font).RenderStyle == GameLib.Fonts.RenderStyle.Shaded; }
    set
    { if(value!=AntialiasText)
      { GameLib.Fonts.TrueTypeFont font = (GameLib.Fonts.TrueTypeFont)Desktop.Font;
        font.RenderStyle = value ? GameLib.Fonts.RenderStyle.Shaded : GameLib.Fonts.RenderStyle.Solid;
        Desktop.Invalidate();
      }
    }
  }

  public string LayerText { set { lblLayer.Text=value; } }
  public string ModeText { set { lblMode.Text="Mode: "+value; } }
  public string MouseText { set { lblMouse.Text=value; } }
  public string ZoomText { set { lblZoom.Text="Zoom: "+value; } }

  public MenuBar MenuBar { get { return menuBar; } }
  public MenuLabel TypeMenu { get { return lblType; } }

  public void New() { if(CanUnloadLevel()) App.Desktop.World.Clear(); }

  public void Load()
  { App.Desktop.StopKeyRepeat();
    string file = FileChooser.Load(Desktop, FileType.Directory, lastPath);
    if(file!="")
    { try
      { App.Desktop.World.Load(file);
        lastPath = file;
        App.Desktop.StatusText = lastPath+" loaded.";
      }
      catch(Exception e)
      { App.Desktop.World.Clear();
        MessageBox.Show(Desktop, "Error", string.Format("An error occurred while loading {0} -- {1}", file,
                                                        e.Message));
        App.Desktop.StatusText = file+" failed to load.";
      }
    }
  }

  public bool Save()
  { if(lastPath==null) return SaveAs();
    App.Desktop.StopKeyRepeat();
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
  { App.Desktop.StopKeyRepeat();
    string file = FileChooser.Save(Desktop, FileType.Directory);
    if(file=="") MessageBox.Show(Desktop, "Aborted", "Compilation aborted.");
    else if(MessageBox.Show(Desktop, "Compile?", "Compile level into '"+file+"'?", MessageBoxButtons.YesNo)==0)
    { App.Desktop.World.Save(file, true);
      App.Desktop.StatusText = "Level compiled into "+file+".";
    }
  }

  public void Exit() { if(CanUnloadLevel()) GameLib.Events.Events.PushEvent(new GameLib.Events.QuitEvent()); }

  public void ExportRect()
  {
  }
  
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
  void exit_OnClick(object sender, EventArgs e)   { Exit(); }

  void exportRect_OnClick(object sender, EventArgs e) { ExportRect(); }
  void objectProps_OnClick(object sender, EventArgs e) { App.Desktop.World.ShowObjectProperties(); }
  void smarmProps_OnClick(object sender, EventArgs e)
  { if(new ObjectProperties(App.SetupObject).Show(Desktop))
    { object value = App.SetupObject["antialias"];
      AntialiasText = value!=null && (bool)value ? true : false;
    }
  }

  void toggleFullscreen_OnClick(object sender, EventArgs e) { App.Fullscreen = !App.Fullscreen; }

  void toggleAntialias_OnClick(object sender, EventArgs e) { AntialiasText = !AntialiasText; }

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
  
  string lastPath;
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
          App.Desktop.TopBar.ModeText = "Poly";
        }
        else App.Desktop.TopBar.ModeText = "View";
        SelectedType = lbl.Menu.Controls.Count>0 ? lbl.Menu.Controls[0].Text : "";
        selectedPoly = null;
        selectedObject = null;
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
        Invalidate();
      }
    }
  }

  public string SelectedType
  { get { return App.Desktop.TopBar.TypeMenu.Text; }
    set { App.Desktop.TopBar.TypeMenu.Text=value; }
  }

  public World World { get { return world; } }
  
  public ZoomMode ZoomMode
  { get { return zoom; }
    set
    { if(value!=zoom)
      { int z=(int)zoom, v=(int)value;
        switch(zoom=value)
        { case ZoomMode.Full: App.Desktop.TopBar.ZoomText = "4x"; break;
          case ZoomMode.Normal: App.Desktop.TopBar.ZoomText = "1x"; break;
          case ZoomMode.Tiny: App.Desktop.TopBar.ZoomText = ".25x"; break;
        }
        if(zoom!=ZoomMode.Normal && EditMode==EditMode.Objects) EditMode=EditMode.ViewOnly;
        Invalidate();
      }
    }
  }
  #endregion

  #region Public methods
  public void AddLayer()
  { world.AddLayer();
    SelectedLayer = world.Layers.Length-1;
  }

  public void Clear()
  { world.Clear();
    x = y = 0;
    EditMode = EditMode.ViewOnly;
    SelectedLayer = -1;
    ZoomMode = ZoomMode.Full;
    BackColor = world.BackColor;
    Invalidate();
  }

  public void Load(string directory)
  { Clear();
    world.Load(directory);
    BackColor = world.BackColor;
    Invalidate();
  }

  public void Save(string directory, bool compile)
  { if(compile) world.Compile(directory);
    else world.Save(directory);
  }
  
  public void ShowObjectProperties()
  { if(selectedObject!=null)
    { if(selectedObject.Type.Properties.Length>0)
      { ObjectProperties props = new ObjectProperties(selectedObject);
        if(props.Show(Desktop)) Invalidate(selectedObject);
        SelectObject(selectedObject); // just to update the text
      }
      else App.Desktop.StatusText = "Object '" + selectedObject.Name + "' has no editable properties.";
    }
    else App.Desktop.StatusText = "No object selected.";
  }
  #endregion

  #region Painting
  protected override void OnPaint(PaintEventArgs e)
  { base.OnPaint(e);
    
    int xoff=e.DisplayRect.X, yoff=e.DisplayRect.Y;
    if(zoom==ZoomMode.Normal) { xoff*=4; yoff*=4; }
    else if(zoom==ZoomMode.Tiny) { xoff*=16; yoff*=16; }
    if(!drawAll && editMode==EditMode.Polygons) world.Render(e.Surface, x+xoff, y+yoff, e.DisplayRect, zoom);
    else world.Render(e.Surface, x+xoff, y+yoff, e.DisplayRect, zoom, drawAll ? -1 : layer, selectedObject);

    foreach(Polygon poly in world.Polygons)
    { Rectangle rect = WorldToWindow(poly.Bounds);
      rect.Inflate(1, 1);
      if(!rect.IntersectsWith(e.WindowRect)) continue;

      Color c;
      if(poly.Points.Length<3) c = (poly==selectedPoly ? Color.FromArgb(255, 0, 0) : Color.FromArgb(192, 0, 0));
      else
        c = poly==selectedPoly ? poly.Color : Color.FromArgb(poly.Color.R*3/4, poly.Color.G*3/4, poly.Color.B*3/4);

      if(poly.Points.Length<3)
      { if(poly.Points.Length>1)
          Primitives.Line(e.Surface, WorldToWindow(poly.Points[0]), WorldToWindow(poly.Points[1]), c);
        for(int i=0; i<poly.Points.Length; i++)
        { Point p = WorldToWindow(new Point(poly.Points[i].X, poly.Points[i].Y));
          Primitives.Box(e.Surface, p.X-1, p.Y-1, p.X+1, p.Y+1, c);
        }
      }
      else
      { Point[] points = (Point[])poly.Points.Clone();
        for(int i=0; i<points.Length; i++) points[i] = WorldToWindow(points[i]);
        Primitives.FilledPolygon(e.Surface, points, Color.FromArgb(64, c));
        for(int i=0; i<points.Length; i++)
        { Point p=points[i];
          Primitives.Box(e.Surface, p.X-1, p.Y-1, p.X+1, p.Y+1, c);
        }
      }
    }
  }
  #endregion

  #region Other events
  protected override void OnMouseMove(GameLib.Events.MouseMoveEvent e)
  { if(!Focused) Focus();
    Point pt = WindowToWorld(e.Point);
    App.Desktop.TopBar.MouseText = pt.X.ToString()+'x'+pt.Y.ToString();
    base.OnMouseMove(e);
  }
  
  protected override void OnMouseClick(ClickEventArgs e)
  { if(editMode==EditMode.Polygons)
    { if(e.CE.Button==0)
      { e.CE.Point = WindowToWorld(e.CE.Point);
        if(subMode==SubMode.None)
        { if(Keyboard.HasOnlyKeys(KeyMod.Shift) || !ClickVertex(e.CE.Point))
          { if(selectedPoly!=null) Invalidate(selectedPoly);
            selectedPoly = new Polygon(SelectedType);
            world.Polygons.Add(selectedPoly);
            subMode = SubMode.NewPoly;
          }
        }
        if(subMode==SubMode.NewPoly)
        { selectedPoly.AddPoint(e.CE.Point);
          Invalidate(selectedPoly);
          world.ChangedSinceSave = true;
        }
        e.Handled = true;
      }
      else if(e.CE.Button==1 && subMode==SubMode.NewPoly)
      { RemoveLastPoint();
        e.Handled=true;
      }
    }
    else if(editMode==EditMode.Objects)
    { if(e.CE.Button==0)
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
        Invalidate(selectedObject);
        e.Handled=true;
      }
    }
    base.OnMouseClick(e);
  }

  protected override void OnKeyDown(KeyEventArgs e)
  { if(e.KE.Key==Key.Tab && e.KE.KeyMods==KeyMod.None && !drawAll)
    { drawAll=true;
      Invalidate();
      e.Handled=true;
    }
    else if(e.KE.Key==Key.Home && e.KE.KeyMods==KeyMod.None)
    { x = y = 0;
      Invalidate();
      e.Handled = true;
    }
    else if(editMode==EditMode.Polygons)
    { e.Handled=true;
      if(e.KE.Key==Key.Delete && selectedPoly!=null) RemoveSelectedPoly();
      else if((e.KE.Key==Key.Enter || e.KE.Key==Key.KpEnter) && selectedPoly!=null)
      { Invalidate(selectedPoly);
        selectedPoly = null;
        subMode = SubMode.None;
      }
      else if(subMode==SubMode.NewPoly)
      { if(e.KE.Key==Key.Backspace)
        { if(selectedPoly.Points.Length==1) RemoveSelectedPoly();
          else RemoveLastPoint();
        }
        else if(e.KE.Key==Key.Escape) RemoveSelectedPoly();
        else e.Handled=false;
      }
    }
    else if(editMode==EditMode.Objects)
    { e.Handled=true;
      if(e.KE.Key==Key.Delete && selectedObject!=null)
      { world.Layers[layer].Objects.Remove(selectedObject);
        SelectObject(null);
        world.ChangedSinceSave = true;
      }
      else if((e.KE.Key==Key.Enter || e.KE.Key==Key.KpEnter) && selectedObject!=null) SelectObject(null);
      else e.Handled=false;
    }
    if(e.Handled) Desktop.StopKeyRepeat();
    base.OnKeyDown(e);
  }

  protected override void OnKeyUp(KeyEventArgs e)
  { if(e.KE.Key==Key.Tab && drawAll)
    { drawAll = false;
      Invalidate();
      e.Handled = true;
    }
    base.OnKeyUp(e);
  }
  
  protected override void OnDragStart(DragEventArgs e)
  { if(editMode==EditMode.Polygons)
    { if(subMode==SubMode.None)
      { if(e.Buttons==1)
        { if(ClickVertex(WindowToWorld(e.Start))) subMode=SubMode.DragSelected;
          else e.Cancel=true;
          goto done;
        }
      }
    }
    else if(editMode==EditMode.Objects)
    { if(subMode==SubMode.None)
      { if(e.Buttons==1)
        { if(ClickObject(WindowToWorld(e.Start))) subMode=SubMode.DragSelected;
          else e.Cancel=true;
          goto done;
        }
      }
    }
    if(e.Buttons != 4) e.Cancel=true;
    done:
    base.OnDragStart(e);
  }
  
  protected override void OnDragMove(DragEventArgs e)
  { if(e.Buttons==4) DragScroll(e);
    else if(e.Buttons==1 && subMode==SubMode.DragSelected)
    { if(editMode==EditMode.Polygons) DragPoint(e);
      else if(editMode==EditMode.Objects) DragObject(e);
    }
    base.OnDragMove(e);
  }

  protected override void OnDragEnd(DragEventArgs e)
  { if(e.Buttons==4) DragScroll(e);
    else if(e.Buttons==1 && subMode==SubMode.DragSelected)
    { if(editMode==EditMode.Polygons) DragPoint(e);
      else if(editMode==EditMode.Objects) DragObject(e);
      subMode = SubMode.None;
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

  enum SubMode { None, NewPoly, DragSelected };

  #region Private methods
  bool ClickVertex(Point pt)
  { foreach(Polygon poly in world.Polygons)
      for(int i=0; i<poly.Points.Length; i++)
      { Point p = poly.Points[i];
        if(pt.X>=p.X-1 && pt.X<=p.X+1 && pt.Y>=p.Y-1 && pt.Y<=p.Y+1)
        { selectedPoly = poly;
          selectedPoint = i;
          Invalidate(poly);
          return true;
        }
      }
    return false;
  }

  bool ClickObject(Point pt)
  { foreach(Object obj in world.Layers[layer].Objects)
      if(obj.Bounds.Contains(pt))
      { if(obj!=selectedObject) SelectObject(obj);
        return true;
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
  
  void DragPoint(DragEventArgs e)
  { int xd=e.End.X-e.Start.X, yd=e.End.Y-e.Start.Y;
    if(zoom==ZoomMode.Full) { xd/=4; yd/=4; }
    else if(zoom==ZoomMode.Tiny) { xd*=4; yd*=4; }
    if(xd!=0 || yd!=0)
    { Invalidate(selectedPoly);
      selectedPoly.Points[selectedPoint].X += xd;
      selectedPoly.Points[selectedPoint].Y += yd;
      Invalidate(selectedPoly);
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
    { Point pos = selectedObject.Location;
      pos.Offset(xd, yd);
      Invalidate(selectedObject);
      selectedObject.Location = pos;
      Invalidate(selectedObject);
      if(zoom==ZoomMode.Full) { e.Start.X += xd*4; e.Start.Y += yd*4; }
      else e.Start = e.End;
      world.ChangedSinceSave = true;
    }
  }

  void Invalidate(Object obj)
  { Rectangle rect = WorldToWindow(obj.Bounds);
    rect.Inflate(4, 4);
    Invalidate(rect);
  }
  
  void Invalidate(Polygon poly)
  { Rectangle rect = WorldToWindow(poly.Bounds);
    rect.Inflate(4, 4);
    Invalidate(rect);
  }

  void RemoveLastPoint()
  { if(selectedPoly.Points.Length==1) RemoveSelectedPoly();
    else
    { Invalidate(selectedPoly);
      selectedPoly.RemoveLastPoint();
      world.ChangedSinceSave = true;
    }
  }
  
  void RemoveSelectedPoly()
  { Invalidate(selectedPoly);
    world.Polygons.Remove(selectedPoly);
    selectedPoly = null;
    subMode = SubMode.None;
    world.ChangedSinceSave = true;
  }
  
  void SelectObject(Object obj)
  { if(obj != selectedObject)
    { if(selectedObject!=null) Invalidate(selectedObject);
      selectedObject=obj;
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
  Polygon   selectedPoly;
  Object    selectedObject;
  int x, y, layer, selectedPoint;
  EditMode editMode;
  SubMode  subMode;
  ZoomMode zoom;
  bool     drawAll;
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
          if(obj[prop.Name]!=null) tb.Text = obj[prop.Name].ToString();
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