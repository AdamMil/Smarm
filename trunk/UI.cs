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
    fileMenu.Add(new MenuItem("Load", 'L')).Click += new EventHandler(load_OnClick);
    fileMenu.Add(new MenuItem("Save", 'S')).Click += new EventHandler(save_OnClick);
    fileMenu.Add(new MenuItem("Save As", 'A')).Click += new EventHandler(saveAs_OnClick);
    fileMenu.Add(new MenuItem("Exit", 'X')).Click += new EventHandler(exit_OnClick);

    editMenu = new Menu();
    editMenu.Add(new MenuItem("Export Rect", 'E')).Click += new EventHandler(exportRect_OnClick);
    
    lblLayer.Menu = new Menu();
    lblLayer.Menu.Popup += new EventHandler(layerMenu_Popup);
    
    lblZoom.Menu = new Menu();
    lblZoom.Menu.Add(new MenuItem("Size to window", 'S'));
    lblZoom.Menu.Add(new MenuItem("Render size (0.5x)", 'R'));
    lblZoom.Menu.Add(new MenuItem("Full size (1x)", 'F'));
    
    lblMode.Menu = new Menu();
    lblMode.Menu.Add(new MenuItem("Objects", 'O'));
    lblMode.Menu.Add(new MenuItem("Polygons", 'P'));

    foreach(object o in new MenuBase[] { fileMenu, editMenu, lblLayer.Menu, lblZoom.Menu, lblMode.Menu })
    { Menu menu = (Menu)o;
      menu.BackColor = BackColor;
      menu.SelectedBackColor = Color.FromArgb(80, 80, 80);
      menu.SelectedForeColor = Color.White;
    }

    btnFile.BackColor = btnEdit.BackColor = Color.FromArgb(80, 80, 80);
    btnFile.TextAlign = btnEdit.TextAlign = ContentAlignment.TopCenter; // HACK: the arial font doesn't align properly

    btnFile.Bounds = new Rectangle(4, 6, 40, 20);
    btnEdit.Bounds = new Rectangle(btnFile.Right+4, 6, 40, 20);

    btnFile.Tag = fileMenu;
    btnEdit.Tag = editMenu;
    { ClickEventHandler eh = new ClickEventHandler(btnMenu_OnClick);
      btnFile.Click += eh;
      btnEdit.Click += eh;
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
    Controls.AddRange(lblLayer, lblMouse, lblMode, lblZoom, btnFile, btnEdit);
    #endregion
  }

  public string MouseText { set { lblMouse.Text=value; } }

  public void OpenFileMenu() { OpenMenu(fileMenu, btnFile); }
  public void OpenEditMenu() { OpenMenu(editMenu, btnEdit); }

  protected override void OnPaintBackground(PaintEventArgs e)
  { base.OnPaintBackground(e);
    Color color = Color.FromArgb(80, 80, 80);
    Primitives.HLine(e.Surface, e.DisplayRect.X, e.DisplayRect.Right-1, DisplayRect.Bottom-1, color);
    Primitives.VLine(e.Surface, Width-lblWidth*2-lblPadding*3/2, 0, Height-1, color);
    Primitives.VLine(e.Surface, Width-lblWidth-lblPadding/2, 0, Height-1, color);
  }

  void OpenMenu(Menu menu, Button button) { if(menu.Parent==null) menu.Show(button, new Point(0, button.Height)); }

  void Load()
  {
  }

  void Save()
  {
  }

  void SaveAs()
  {
  }

  void Exit()
  { if(!App.Desktop.World.ChangedSinceSave) GameLib.Events.Events.PushEvent(new GameLib.Events.QuitEvent());
  }

  void ExportRect()
  {
  }

  #region Event handlers
  void btnMenu_OnClick(object sender, ClickEventArgs e)
  { Button button = (Button)sender;
    OpenMenu((Menu)button.Tag, button);
  }

  void load_OnClick(object sender, EventArgs e)   { Load(); }
  void save_OnClick(object sender, EventArgs e)   { Save(); }
  void saveAs_OnClick(object sender, EventArgs e) { SaveAs(); }
  void exit_OnClick(object sender, EventArgs e)   { Exit(); }

  void exportRect_OnClick(object sender, EventArgs e) { ExportRect(); }
  #endregion

  const int lblWidth=64, lblHeight=16, lblPadding=6;
  Button btnFile=new Button("File"), btnEdit=new Button("Edit");
  MenuLabel lblLayer=new MenuLabel(), lblMode=new MenuLabel(), lblZoom=new MenuLabel();
  Label  lblMouse=new Label();
  Menu   fileMenu, editMenu;

  private void layerMenu_Popup(object sender, EventArgs e)
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
    Font.Render(e.Surface, "World goes here", DisplayRect, ContentAlignment.MiddleCenter);
  }

  protected override void OnMouseMove(GameLib.Events.MouseMoveEvent e)
  { App.Desktop.TopBar.MouseText=e.X.ToString()+'x'+e.Y.ToString();
  }
  
  World world = new World();
}
#endregion

#region SmarmDesktop
class SmarmDesktop : DesktopControl
{ public SmarmDesktop()
  { AutoFocusing = AutoFocus.None;
    KeyPreview   = true;
    ForeColor    = Color.White;
    BackColor    = Color.Black;
    Controls.AddRange(topBar, bottomBar, world);
  }

  public TopBar TopBar { get { return topBar; } }
  public string StatusText { get { return bottomBar.StatusText; } set { bottomBar.StatusText=value; } }
  public World World { get { return world.World; } }

  protected override void OnKeyPress(KeyEventArgs e)
  { if(!e.Handled && e.KE.Down && e.KE.HasOnlyKeys(KeyMod.Alt))
    { e.Handled = true;
      if(e.KE.Key==Key.Return || e.KE.Key==Key.KpEnter) App.Fullscreen = !App.Fullscreen;
      else if(char.ToUpper(e.KE.Char)=='F') topBar.OpenFileMenu();
      else if(char.ToUpper(e.KE.Char)=='E') topBar.OpenEditMenu();
      else e.Handled = false;
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