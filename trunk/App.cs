using System;
using GameLib;
using GameLib.Events;
using GameLib.Forms;
using GameLib.Video;

namespace Smarm
{

class App
{ private App() { }

  public static SmarmDesktop Desktop { get { return desktop; } }

  public static bool Fullscreen
  { get { return fullscreen; }
    set
    { if(value!=fullscreen)
      { fullscreen=value;
        if(fullscreen)
        { oldWidth=desktop.Width; oldHeight=desktop.Height;
          SetMode(1024, 768);
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

  static void SetMode(int width, int height)
  { Video.SetMode(width, height, 32, fullscreen ? SurfaceFlag.Fullscreen : SurfaceFlag.Resizeable);
    desktop.Surface = Video.DisplaySurface;
    desktop.Bounds  = Video.DisplaySurface.Bounds;
    desktop.Invalidate();
  }

  static void Main()
  { GameLib.Fonts.TrueTypeFont font = new GameLib.Fonts.TrueTypeFont(SmarmPath+"arial.ttf", 12);
    WM.WindowTitle = "Smarm "+Version;
    font.RenderStyle = GameLib.Fonts.RenderStyle.Blended;
    desktop.Font = font;

    Video.Initialize();
    SetMode(640, 480);

    Events.Initialize();
    Event e;
    while(true)
    { e = Events.NextEvent();
      if(desktop.ProcessEvent(e) != FilterAction.Drop)
      { if(e is RepaintEvent) Video.Flip();
        else if(e is ResizeEvent)
        { ResizeEvent re = (ResizeEvent)e;
          SetMode(re.Width, re.Height);
        }
        else if(e is ExceptionEvent) throw ((ExceptionEvent)e).Exception;
        else if(e is QuitEvent) break;
      }
      else if(desktop.Updated)
      { Video.UpdateRects(desktop.UpdatedAreas, desktop.NumUpdatedAreas);
        desktop.Updated=false;
      }
    }
  }

  static SmarmDesktop desktop = new SmarmDesktop();
  static string smarmPath="c:/code/smarm/data/", spritePath="c:/games/swarm3/images/sprites/";
  static string stampPath="c:/games/swarm3/images/stamps/";
  static int    oldHeight, oldWidth;
  static bool   fullscreen;
}

} // namespace Smarm
