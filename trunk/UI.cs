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
  protected override void OnMouseClick(ClickEventArgs e)
  { if(!e.Handled && menu!=null)
    { menu.Show(this, new Point(0, Height));
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
    fileMenu = new Menu();
    fileMenu.Add(new MenuItem("New", 'N')).Click += new EventHandler(new_OnClick);
    fileMenu.Add(new MenuItem("Load", 'L')).Click += new EventHandler(load_OnClick);
    fileMenu.Add(new MenuItem("Save", 'S')).Click += new EventHandler(save_OnClick);
    fileMenu.Add(new MenuItem("Save As", 'A')).Click += new EventHandler(saveAs_OnClick);
    fileMenu.Add(new MenuItem("Exit", 'X')).Click += new EventHandler(exit_OnClick);

    editMenu = new Menu();
    editMenu.Add(new MenuItem("Export Rect", 'E')).Click += new EventHandler(exportRect_OnClick);
    
    viewMenu = new Menu();
    viewMenu.Popup += new EventHandler(viewMenu_Popup);
    viewMenu.Add(new MenuItem("Toggle Fullscreen", 'F')).Click += new EventHandler(toggleFullscreen_OnClick);
    viewMenu.Add(new MenuItem("Toggle Antialias", 'A')).Click += new EventHandler(toggleAntialias_OnClick);
    
    lblLayer.Menu = new Menu();
    lblLayer.Menu.Popup += new EventHandler(layerMenu_Popup);
    
    lblZoom.Menu = new Menu();
    lblZoom.Menu.Add(new MenuItem("Size to window", 'S'));
    lblZoom.Menu.Add(new MenuItem("Render size (0.5x)", 'R'));
    lblZoom.Menu.Add(new MenuItem("Full size (1x)", 'F'));
    
    lblMode.Menu = new Menu();
    lblMode.Menu.Add(new MenuItem("Objects", 'O'));
    lblMode.Menu.Add(new MenuItem("Polygons", 'P'));

    foreach(object o in new MenuBase[] { fileMenu, editMenu, viewMenu, lblLayer.Menu, lblZoom.Menu, lblMode.Menu })
    { Menu menu = (Menu)o;
      menu.BackColor = BackColor;
      menu.SelectedBackColor = Color.FromArgb(80, 80, 80);
      menu.SelectedForeColor = Color.White;
    }

    btnFile.BackColor = btnEdit.BackColor = btnView.BackColor = Color.FromArgb(80, 80, 80);
    btnFile.TextAlign = btnEdit.TextAlign = btnView.TextAlign = ContentAlignment.TopCenter; // HACK: the arial font doesn't align properly

    btnFile.Bounds = new Rectangle(4, 6, 40, 20);
    btnEdit.Bounds = new Rectangle(btnFile.Right+4, 6, 40, 20);
    btnView.Bounds = new Rectangle(btnEdit.Right+4, 6, 40, 20);

    btnFile.Tag = fileMenu;
    btnEdit.Tag = editMenu;
    btnView.Tag = viewMenu;
    { ClickEventHandler eh = new ClickEventHandler(btnMenu_OnClick);
      btnFile.Click += eh;
      btnEdit.Click += eh;
      btnView.Click += eh;
    }

    lblLayer.Bounds = new Rectangle(Width-lblWidth, 0, lblWidth, lblHeight);
    lblMouse.Bounds = new Rectangle(Width-lblWidth, lblHeight, lblWidth, lblHeight);
    lblMode.Bounds  = new Rectangle(Width-lblWidth*2-lblPadding, 0, lblWidth, lblHeight);
    lblZoom.Bounds  = new Rectangle(Width-lblWidth*2-lblPadding, lblHeight, lblWidth, lblHeight);
    lblLayer.Anchor = lblMouse.Anchor = lblMode.Anchor = lblZoom.Anchor = AnchorStyle.TopRight;

    lblLayer.Text = "Layer 0";
    lblMouse.Text = "1274x763";
    lblMode.Text  = "Mode: Obj";
    lblZoom.Text  = "Zoom: Full";
    Controls.AddRange(lblLayer, lblMouse, lblMode, lblZoom, btnFile, btnEdit, btnView);
    #endregion
  }

  public string MouseText { set { lblMouse.Text=value; } }

  public void OpenFileMenu() { OpenMenu(fileMenu, btnFile); }
  public void OpenEditMenu() { OpenMenu(editMenu, btnEdit); }
  public void OpenViewMenu() { OpenMenu(viewMenu, btnView); }

  protected override void OnPaintBackground(PaintEventArgs e)
  { base.OnPaintBackground(e);
    Color color = Color.FromArgb(80, 80, 80);
    Primitives.HLine(e.Surface, e.DisplayRect.X, e.DisplayRect.Right-1, DisplayRect.Bottom-1, color);
    Primitives.VLine(e.Surface, Width-lblWidth*2-lblPadding*3/2, 0, Height-1, color);
    Primitives.VLine(e.Surface, Width-lblWidth-lblPadding/2, 0, Height-1, color);
  }

  void OpenMenu(Menu menu, Button button)
  { if(menu.Parent==null)
    { menu.Show(button, new Point(0, button.Height));
      button.Pressed = false;
    }
  }

  void New()
  { if(CanUnloadLevel())
    { App.Desktop.World.Clear();
      App.Desktop.WorldDisplay.Invalidate();
    }
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

      App.Desktop.WorldDisplay.Invalidate();
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
  { if(App.Desktop.World.ChangedSinceSave)
    { int button = MessageBox.Show(Desktop, "Save changes?", "This level has been altered. Save changes?",
                                   MessageBoxButtons.YesNoCancel);
      if(button==0) return Save();
      else if(button==1) return true;
      else return false;
    }
    return true;
  }

  #region Event handlers
  void btnMenu_OnClick(object sender, ClickEventArgs e)
  { Button button = (Button)sender;
    OpenMenu((Menu)button.Tag, button);
  }

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
  #endregion

  const int lblWidth=64, lblHeight=16, lblPadding=6;
  Button btnFile=new Button("File"), btnEdit=new Button("Edit"), btnView=new Button("View");
  MenuLabel lblLayer=new MenuLabel(), lblMode=new MenuLabel(), lblZoom=new MenuLabel();
  Label  lblMouse=new Label();
  Menu   fileMenu, editMenu, viewMenu;
  
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
class WorldDisplay : Control
{ public World World { get { return world; } }

  protected override void OnPaint(PaintEventArgs e)
  { base.OnPaint(e);
    GameLib.Fonts.Font font = Font;
    font.Color = ForeColor;
    font.BackColor = BackColor;
    Font.Render(e.Surface, "World goes here", DisplayRect, ContentAlignment.MiddleCenter);
  }

  protected override void OnMouseMove(GameLib.Events.MouseMoveEvent e)
  { App.Desktop.TopBar.MouseText=e.X.ToString()+'x'+e.Y.ToString();
  }
  
  World world = new World();
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
  }

  public TopBar TopBar { get { return topBar; } }
  public string StatusText { get { return bottomBar.StatusText; } set { bottomBar.StatusText=value; } }
  public World World { get { return world.World; } }
  public WorldDisplay WorldDisplay { get { return world; } }

  protected override void OnKeyPress(KeyEventArgs e)
  { if(!e.Handled && e.KE.Down && e.KE.HasOnlyKeys(KeyMod.Alt))
    { e.Handled = true;
      if(e.KE.Key==Key.Return || e.KE.Key==Key.KpEnter)
      { if(enterKey==Key.None) { enterKey=e.KE.Key; App.Fullscreen = !App.Fullscreen; }
      }
      else if(char.ToUpper(e.KE.Char)=='F') topBar.OpenFileMenu();
      else if(char.ToUpper(e.KE.Char)=='E') topBar.OpenEditMenu();
      else if(char.ToUpper(e.KE.Char)=='V') topBar.OpenViewMenu();
      else e.Handled = false;
    }
  }

  protected override void OnKeyUp(KeyEventArgs e) { if(e.KE.Key==enterKey) enterKey=Key.None; }

  protected override void OnResize(EventArgs e)
  { topBar.Bounds = new Rectangle(0, 0, Width, 32);
    bottomBar.Bounds = new Rectangle(0, Height-32, Width, 32);
    world.Bounds = new Rectangle(0, 32, Width, Height-32-32);
    base.OnResize(e);
  }

  TopBar topBar = new TopBar();
  BottomBar bottomBar = new BottomBar();
  WorldDisplay world = new WorldDisplay();
  Key enterKey=Key.None;
}
#endregion

} // namespace Smarm