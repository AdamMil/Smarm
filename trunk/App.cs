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
  static App() { LoadObjects(); }

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

  public static string SmarmPath  { get { return smarmPath; } }
  public static string SpritePath { get { return spritePath; } }
  public static string StampPath  { get { return stampPath; } }

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
  }

  static void SetMode(int width, int height)
  { Video.SetMode(width, height, 32, fullscreen ? SurfaceFlag.Fullscreen : SurfaceFlag.Resizeable);
    desktop.Surface = Video.DisplaySurface;
    desktop.Bounds  = Video.DisplaySurface.Bounds;
    desktop.Invalidate();
  }

  static void LoadObjects()
  { FileStream file = File.Open(SmarmPath+"objects", FileMode.Open, FileAccess.Read);
    List objList = new List(file);
    ObjectDef.LoadDefs(objList["objects"]);
    PolygonType.LoadDefs(objList["polygon-types"]);
    file.Close();
  }

  static SmarmDesktop desktop = new SmarmDesktop();
  static string smarmPath="c:/code/smarm/data/", spritePath="c:/games/swarm3/images/sprites/";
  static string stampPath="c:/games/swarm3/images/stamps/";
  static int    oldHeight, oldWidth;
  static bool   fullscreen;
}

} // namespace Smarm
