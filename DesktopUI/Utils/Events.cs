﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Speckle.Core.Api;

namespace Speckle.DesktopUI.Utils
{
  public class EventBase
  {
    public string Notification { get; set; }
    public dynamic DynamicInfo { get; set; }
  }

  public class ShowNotificationEvent : EventBase { }

  public class StreamAddedEvent : EventBase
  {
    public Stream NewStream { get; set; }
  }

  public class UpdateSelectionCountEvent : EventBase
  {
    public int SelectionCount { get; set; }
  }

  public class  UpdateSelectionEvent : EventBase
  {
    public List<string> ObjectIds { get; set; }
  }

  public class ApplicationEvent : EventBase
  {
    public enum EventType
    {
      ViewActivated, // is this Revit specific?
      DocumentOpened,
      DocumentClosed,
      DocumentModified,
      ApplicationIdling
    }
    public EventType Type { get; set; }
  }
}
