// TODO: fix display of "toggle antialias" menu item
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
{ public MenuLabel() { Style |= ControlStyle.Clickable; }
  
  public MenuBase Menu { get { return menu; } set { menu=value; } }

  protected override void OnMouseEnter(EventArgs e) { over=true;  Invalidate(); }
  protected override void OnMouseLeave(EventArgs e) { over=false; Invalidate(); }
  protected override void OnPaintBackground(PaintEventArgs e)
  { if(over)
    { Color old = RawBackColor, back = BackColor;
      BackColor = Color.FromArgb(back.R+(255-back.R)/8, back.G+(255-back.G)/8, back.B+(255-back.B)/8);
      base.OnPaintBackground(e);
      BackColor = old;
    }
    else base.OnPaintBackground(e);
  }
  protected override void OnMouseDown(ClickEventArgs e)
  { if(!e.Handled && e.CE.Button==0 && menu!=null)
    { menu.Show(this, new Point(0, Height), true);
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

    MenuBase menu = menuBar.Add(new Menu("File", new KeyCombo(KeyMod.Alt, 'F')));
    menu.Add(new MenuItem("New", 'N', new KeyCombo(KeyMod.Ctrl, 'N'))).Click += new EventHandler(new_OnClick);
    menu.Add(new MenuItem("Load", 'L', new KeyCombo(KeyMod.Ctrl, 'L'))).Click += new EventHandler(load_OnClick);
    menu.Add(new MenuItem("Save", 'S', new KeyCombo(KeyMod.Ctrl, 'S'))).Click += new EventHandler(save_OnClick);
    menu.Add(new MenuItem("Save As", 'A')).Click += new EventHandler(saveAs_OnClick);
    menu.Add(new MenuItem("Exit", 'X', new KeyCombo(KeyMod.Ctrl, 'X'))).Click += new EventHandler(exit_OnClick);

    menu = menuBar.Add(new Menu("Edit", new KeyCombo(KeyMod.Alt, 'E')));
    menu.Add(new MenuItem("Export Rect", 'E')).Click += new EventHandler(exportRect_OnClick);
    
    menu = menuBar.Add(new Menu("View", new KeyCombo(KeyMod.Alt, 'V')));
    menu.Popup += new EventHandler(viewMenu_Popup);
    menu.Add(new MenuItem("Toggle Fullscreen", 'F')).Click += new EventHandler(toggleFullscreen_OnClick);
    menu.Add(new MenuItem("Toggle Antialias", 'A')).Click += new EventHandler(toggleAntialias_OnClick);

    lblLayer.Menu = new Menu();
    lblLayer.Menu.Popup += new EventHandler(layerMenu_Popup);
    
    lblZoom.Menu = new Menu();
    lblZoom.Menu.Add(new MenuItem("Size to window", 'S'));
    lblZoom.Menu.Add(new MenuItem("Render size (0.5x)", 'R'));
    lblZoom.Menu.Add(new MenuItem("Full size (1x)", 'F'));
    
    { EventHandler click = new EventHandler(mode_OnClick);
      lblMode.Menu = new Menu();
      lblMode.Menu.Add(new MenuItem("Objects", 'O')).Click += click;
      lblMode.Menu.Add(new MenuItem("Polygons", 'P')).Click += click;
      lblMode.Menu.Add(new MenuItem("View only", 'V')).Click += click;
    }

    foreach(Menu m in menuBar.Menus)
    { m.BackColor = BackColor;
      m.SelectedBackColor = Color.FromArgb(80, 80, 80);
      m.SelectedForeColor = Color.White;
    }
    foreach(Menu m in new MenuBase[] { lblLayer.Menu, lblZoom.Menu, lblMode.Menu })
    { m.BackColor = BackColor;
      m.SelectedBackColor = Color.FromArgb(80, 80, 80);
      m.SelectedForeColor = Color.White;
    }

    lblLayer.Bounds = new Rectangle(Width-lblWidth, 0, lblWidth, lblHeight);
    lblMouse.Bounds = new Rectangle(Width-lblWidth, lblHeight, lblWidth, lblHeight);
    lblMode.Bounds  = new Rectangle(Width-lblWidth*2-lblPadding, 0, lblWidth, lblHeight);
    lblZoom.Bounds  = new Rectangle(Width-lblWidth*2-lblPadding, lblHeight, lblWidth, lblHeight);
    lblLayer.Anchor = lblMouse.Anchor = lblMode.Anchor = lblZoom.Anchor = AnchorStyle.TopRight;

    lblLayer.Text = "Layer 0";
    lblMouse.Text = "0x0";
    lblMode.Text  = "Mode: View";
    lblZoom.Text  = "Zoom: Full";
    Controls.AddRange(lblLayer, lblMouse, lblMode, lblZoom, menuBar);
    #endregion
  }

  public MenuBar MenuBar { get { return menuBar; } }
  public string MouseText { set { lblMouse.Text=value; } }

  protected override void OnPaintBackground(PaintEventArgs e)
  { base.OnPaintBackground(e);
    Color color = Color.FromArgb(80, 80, 80);
    Primitives.HLine(e.Surface, e.DisplayRect.X, e.DisplayRect.Right-1, DisplayRect.Bottom-1, color);
    Primitives.VLine(e.Surface, Width-lblWidth*2-lblPadding*3/2, 0, Height-1, color);
    Primitives.VLine(e.Surface, Width-lblWidth-lblPadding/2, 0, Height-1, color);
  }

  void New()
  { if(CanUnloadLevel()) App.Desktop.World.Clear();
  }

  void Load()
  { string file = FileChooser.Load(Desktop, FileType.Directory, lastPath);
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

  bool Save()
  { if(lastPath==null) SaveAs();
    App.Desktop.StatusText = lastPath+" saved.";
    return false;
  }

  bool SaveAs()
  { string file = FileChooser.Save(Desktop, FileType.Directory);
    if(file=="") return false;
    lastPath = file;
    return Save();
  }

  void Exit() { if(CanUnloadLevel()) GameLib.Events.Events.PushEvent(new GameLib.Events.QuitEvent()); }

  void ExportRect()
  {
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
  void exit_OnClick(object sender, EventArgs e)   { Exit(); }

  void exportRect_OnClick(object sender, EventArgs e) { ExportRect(); }

  void toggleFullscreen_OnClick(object sender, EventArgs e) { App.Fullscreen = !App.Fullscreen; }

  void toggleAntialias_OnClick(object sender, EventArgs e)
  { GameLib.Fonts.TrueTypeFont font = (GameLib.Fonts.TrueTypeFont)Desktop.Font;
    font.RenderStyle = font.RenderStyle==GameLib.Fonts.RenderStyle.Shaded ? GameLib.Fonts.RenderStyle.Solid : GameLib.Fonts.RenderStyle.Shaded;
    Desktop.Invalidate();
  }

  void layerMenu_Popup(object sender, EventArgs e)
  { Menu menu = (Menu)sender;
    menu.Clear();
    // TODO: populate from world
    menu.Add(new MenuItem("Layer 0", '0'));
    menu.Add(new MenuItem("Layer 1", '1'));
    menu.Add(new MenuItem("Layer 2", '2'));
    menu.Add(new MenuItem("Layer 3", '3'));
    menu.Add(new MenuItem("Layer 4", '4'));
    menu.Add(new MenuItem("New Layer", 'N'));
  }
  
  void viewMenu_Popup(object sender, EventArgs e)
  { GameLib.Fonts.TrueTypeFont font = (GameLib.Fonts.TrueTypeFont)Desktop.Font;
    Menu menu = (Menu)sender;
    menu.Controls[1].Text = "Toggle Antialiasing ("+(font.RenderStyle==GameLib.Fonts.RenderStyle.Shaded ? "on" : "off")+')';
  }
  
  void mode_OnClick(object sender, EventArgs e)
  { MenuItemBase item = (MenuItemBase)sender;
    switch(item.HotKey)
    { case 'O': App.Desktop.World.EditMode = EditMode.Objects;  lblMode.Text = "Mode: Obj";  break;
      case 'P': App.Desktop.World.EditMode = EditMode.Polygons; lblMode.Text = "Mode: Poly"; break;
      case 'V': App.Desktop.World.EditMode = EditMode.ViewOnly; lblMode.Text = "Mode: View"; break;
    }
  }
  #endregion

  const int lblWidth=64, lblHeight=16, lblPadding=6;
  MenuLabel lblLayer=new MenuLabel(), lblMode=new MenuLabel(), lblZoom=new MenuLabel();
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
class WorldDisplay : Control
{ public WorldDisplay()
  { Style=ControlStyle.Clickable|ControlStyle.Draggable|ControlStyle.CanFocus|ControlStyle.BackingSurface;
    BackColor=Color.Black;
  }

  public EditMode EditMode
  { get { return editMode; }
    set
    { if(value!=editMode)
      { subMode=SubMode.None;
        editMode=value;
        Invalidate();
      }
    }
  }

  public World World { get { return world; } }

  public void Clear() { world.Clear(); x=y=0; Invalidate(); }
  public void Load(string directory) { world.Load(directory); x=y=0; Invalidate(); }

  protected override void OnPaint(PaintEventArgs e)
  { base.OnPaint(e);
    world.Render(e.Surface, x+e.DisplayRect.X, y+e.DisplayRect.Y, e.DisplayRect);
    if(editMode==EditMode.Polygons)
    { foreach(Polygon poly in world.Polygons)
      { Color c = poly.Points.Length<3 ? poly==selectedPoly ? Color.FromArgb(255, 0, 0) : Color.FromArgb(192, 0, 0)
                                       : poly==selectedPoly ? Color.FromArgb(0, 255, 0) : Color.FromArgb(0, 192, 0);
        if(poly.Points.Length<3)
        { if(poly.Points.Length>1)
            Primitives.Line(e.Surface, poly.Points[0].X-x, poly.Points[0].Y-y,
                            poly.Points[1].X-x, poly.Points[1].Y-y, c);
        }
        else
        { Point[] points = (Point[])poly.Points.Clone();
          for(int i=0; i<points.Length; i++) points[i].Offset(-x, -y);
          Primitives.FilledPolygon(e.Surface, points, Color.FromArgb(64, c));
        }
        for(int i=0; i<poly.Points.Length; i++)
        { Point p = new Point(poly.Points[i].X-x, poly.Points[i].Y-y);
          Primitives.Box(e.Surface, p.X-1, p.Y-1, p.X+1, p.Y+1, c);
        }
      }
    }
  }

  protected override void OnMouseMove(GameLib.Events.MouseMoveEvent e)
  { if(!Focused) Focus();
    App.Desktop.TopBar.MouseText=(x+e.X).ToString()+'x'+(y+e.Y).ToString();
    base.OnMouseMove(e);
  }
  
  protected override void OnMouseClick(ClickEventArgs e)
  { if(editMode==EditMode.Polygons)
    { if(e.CE.Button==0)
      { e.CE.X+=x; e.CE.Y+=y;
        if(subMode==SubMode.None)
        { if(!ClickVertex(e.CE.Point))
          { selectedPoly = new Polygon();
            world.Polygons.Add(selectedPoly);
            subMode = SubMode.NewPoly;
          }
        }
        if(subMode==SubMode.NewPoly) selectedPoly.AddPoint(e.CE.Point);
        Invalidate();
        e.Handled = true;
      }
      else if(e.CE.Button==1 && subMode==SubMode.NewPoly)
      { selectedPoly.RemoveLastPoint();
        Invalidate();
        e.Handled=true;
      }
    }
    base.OnMouseClick(e);
  }

  protected override void OnKeyDown(KeyEventArgs e)
  { if(editMode==EditMode.Polygons)
    { e.Handled=true;
      if(e.KE.Key==Key.Delete && selectedPoly!=null) RemoveSelectedPoly();
      else if(e.KE.Key==Key.Return || e.KE.Key==Key.KpEnter)
      { selectedPoly = null;
        subMode = SubMode.None;
        Invalidate();
      }
      else if(subMode==SubMode.NewPoly)
      { if(e.KE.Key==Key.Backspace)
        { if(selectedPoly.Points.Length==1) RemoveSelectedPoly();
          else selectedPoly.RemoveLastPoint();
          Invalidate();
        }
        else if(e.KE.Key==Key.Escape) RemoveSelectedPoly();
        else e.Handled=false;
      }
    }
    base.OnKeyDown(e);
  }
  
  protected override void OnDragStart(DragEventArgs e)
  { if(editMode==EditMode.Polygons)
    { if(subMode==SubMode.None)
      { if(e.Buttons==1)
        { e.Start.X += x;
          e.Start.Y += y;
          if(ClickVertex(e.Start)) subMode=SubMode.DragPoint;
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
    else if(e.Buttons==1 && editMode==EditMode.Polygons && subMode==SubMode.DragPoint) DragPoint(e);
    base.OnDragMove(e);
  }
  protected override void OnDragEnd(DragEventArgs e)
  { if(e.Buttons==4) DragScroll(e);
    else if(e.Buttons==1 && editMode==EditMode.Polygons && subMode==SubMode.DragPoint)
    { DragPoint(e);
      subMode = SubMode.None;
    }
    base.OnDragEnd(e);
  }

  enum SubMode { None, NewPoly, DragPoint };

  bool ClickVertex(Point pt)
  { foreach(Polygon poly in world.Polygons)
      for(int i=0; i<poly.Points.Length; i++)
      { Point p = poly.Points[i];
        if(pt.X>=p.X-1 && pt.X<=p.X+1 && pt.Y>=p.Y-1 && pt.Y<=p.Y+1)
        { selectedPoly = poly;
          selectedPoint = i;
          return true;
        }
      }
    return false;
  }

  void DragScroll(DragEventArgs e)
  { x -= e.End.X-e.Start.X;
    y -= e.End.Y-e.Start.Y;
    if(x<0) x=0;
    else if(x>world.Width-Width) x=world.Width-Width;
    if(y<0) y=0;
    else if(y>world.Height-Height) y=world.Height-Height;
    e.Start = e.End;
    Invalidate();
  }
  
  void DragPoint(DragEventArgs e)
  { e.End.X += x; e.End.Y += y;
    selectedPoly.Points[selectedPoint].X += e.End.X-e.Start.X;
    selectedPoly.Points[selectedPoint].Y += e.End.Y-e.Start.Y;
    e.Start = e.End;
    Invalidate();
  }
  
  void RemoveSelectedPoly()
  { world.Polygons.Remove(selectedPoly);
    selectedPoly = null;
    Invalidate();
    subMode = SubMode.None;
  }

  World world = new World();
  int x, y, selectedPoint;
  EditMode editMode;
  SubMode  subMode;
  Polygon  selectedPoly;
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
      path.Focus();
      path.CaretPosition = path.Text.Length;
    }
    ShowDialog(desktop);
    return path.Text;
  }

  protected override void OnKeyDown(KeyEventArgs e)
  { if(!e.Handled && e.KE.KeyMods==KeyMod.None)
    { if(e.KE.Key==Key.Return || e.KE.Key==Key.KpEnter)
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
                  done:
                  if(--i>0)
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

  protected override void OnKeyPress(KeyEventArgs e)
  { if(!e.Handled && e.KE.Down && e.KE.HasOnlyKeys(KeyMod.Alt|KeyMod.Ctrl))
    { e.Handled = true;
      if(e.KE.Key==Key.Return || e.KE.Key==Key.KpEnter)
      { StopKeyRepeat();
        App.Fullscreen = !App.Fullscreen;
      }
      else if(ModalWindow!=null || !topBar.MenuBar.HandleKey(e.KE)) e.Handled = false;
    }
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

} // namespace Smarm