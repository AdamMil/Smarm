using System;
using System.IO;
using GameLib;
using GameLib.Events;
using GameLib.Forms;
using GameLib.Video;

namespace Smarm
{

class App
{ private App() { }
  static App() { Init(); }

  public static SmarmDesktop Desktop { get { return desktop; } }

  public static bool Fullscreen
  { get { return fullscreen; }
    set
    { if(value!=fullscreen)
      { fullscreen=value;
        if(fullscreen)
        { oldWidth=desktop.Width; oldHeight=desktop.Height;
          SetMode(640, 480);
        }
        else SetMode(oldWidth, oldHeight);
      }
    }
  }

  public static string SmarmPath  { get { return (string)setup["dataPath"]; } }
  public static string SpritePath { get { return (string)setup["spritePath"]; } }
  public static Object SetupObject { get { return setup; } }

  public static string Version
  { get
    { System.Version v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
      return string.Format("{0}.{1}", v.Major, v.Minor);
    }
  }

  static bool EventProc(Event e)
  { if(desktop.ProcessEvent(e) != FilterAction.Drop)
    { if(e is RepaintEvent) Video.Flip();
      else if(e is ResizeEvent)
      { ResizeEvent re = (ResizeEvent)e;
        SetMode(re.Width, re.Height);
      }
      else if(e is ExceptionEvent) throw ((ExceptionEvent)e).Exception;
      else if(e is QuitEvent) return false;
    }
    else if(desktop.Updated)
    { Video.UpdateRects(desktop.UpdatedAreas, desktop.NumUpdatedAreas);
      desktop.Updated=false;
    }
    return true;
  }

  static void Main()
  { WM.WindowTitle = "Smarm "+Version;
    desktop.Font = new GameLib.Fonts.TrueTypeFont(SmarmPath+"font.ttf", 10);
    desktop.World.Clear(); // start a new level

    Video.Initialize();
    SetMode(640, 480);

    Events.Initialize();
    Events.PumpEvents(new EventProcedure(EventProc));

    StreamWriter file = new StreamWriter("setup");
    setup.Save(file);
    file.Close();
  }

  static void SetMode(int width, int height)
  { Video.SetMode(width, height, 32, fullscreen ? SurfaceFlag.Fullscreen : SurfaceFlag.Resizeable);
    desktop.Surface = Video.DisplaySurface;
    desktop.Bounds  = Video.DisplaySurface.Bounds;
    desktop.Invalidate();
  }

  static void Init()
  { ObjectDef def = new ObjectDef(new List(new MemoryStream(System.Text.Encoding.ASCII.GetBytes(
      @"(smarm-setup-data (prop 'dataPath' 'string' (default './'))
                          (prop 'spritePath' 'string' (default './images/sprites/'))
                          (prop 'antialias' 'bool'))"))), null);
    if(File.Exists("setup"))
    { FileStream file = File.Open("setup", FileMode.Open, FileAccess.Read);
      setup = new Object(def, new List(file));
      file.Close();
    }
    else
    { StreamWriter file = new StreamWriter("setup");
      setup = new Object(def);
      setup.Save(file);
      file.Close();
    }

    FileStream objs = File.Open(SmarmPath+"objects", FileMode.Open, FileAccess.Read);
    List objList = new List(objs);
    ObjectDef.LoadDefs(objList["objects"]);
    PolygonType.LoadDefs(objList["polygon-types"]);
    objs.Close();
  }

  static SmarmDesktop desktop = new SmarmDesktop();
  static Object setup;
  static int    oldHeight, oldWidth;
  static bool   fullscreen;
}

} // namespace Smarm
