﻿#region File Information
/********************************************************************
  Project: ServiceBusMQManager
  File:    MessageManagerBase.cs
  Created: 2012-09-23

  Author(s):
    Daniel Halan

 (C) Copyright 2012 Ingenious Technology with Quality Sweden AB
     all rights reserved

********************************************************************/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ServiceBusMQ.Model;

namespace ServiceBusMQ.Manager {

  //public delegate void Error

  public abstract class MessageManagerBase : IMessageManager {

    protected List<QueueItem> EMPTY_LIST = new List<QueueItem>();

    protected volatile object _itemsLock = new object();
    protected volatile List<QueueItem> _items = new List<QueueItem>();

    public List<QueueItem> Items { get { return _items; } }

    protected string _serverName;

    protected string[] _watchEventQueues;
    protected string[] _watchCommandQueues;
    protected string[] _watchMessageQueues;
    protected string[] _watchErrorQueues;


    public string[] EventQueues { get { return _watchEventQueues; } }
    public string[] CommandQueues { get { return _watchCommandQueues; } }
    public string[] MessageQueues { get { return _watchMessageQueues; } }
    public string[] ErrorQueues { get { return _watchErrorQueues; } }

    protected CommandDefinition _commandDef;


    bool _monitorCommands, _monitorEvents, _monitorMessages, _monitorErrors;

    public bool MonitorCommands { get { return _monitorCommands; } set { _monitorCommands = value; UpdateItems(QueueType.Command, value); } }
    public bool MonitorEvents { get { return _monitorEvents; } set { _monitorEvents = value; UpdateItems(QueueType.Event, value); } }
    public bool MonitorMessages { get { return _monitorMessages; } set { _monitorMessages = value; UpdateItems(QueueType.Message, value); } }
    public bool MonitorErrors { get { return _monitorErrors; } set { _monitorErrors = value; UpdateItems(QueueType.Error, value); } }


    protected EventHandler _itemsChanged;

    public event EventHandler ItemsChanged {
      [MethodImpl(MethodImplOptions.Synchronized)]
      add {
        _itemsChanged = (EventHandler)Delegate.Combine(_itemsChanged, value);
      }
      [MethodImpl(MethodImplOptions.Synchronized)]
      remove {
        _itemsChanged = (EventHandler)Delegate.Remove(_itemsChanged, value);
      }
    }

    protected void OnItemsChanged() {
      if( _itemsChanged != null )
        _itemsChanged(this, EventArgs.Empty);
    }


    public virtual void Init(string serverName, string[] watchCommandQueues, string[] watchEventQueues, string[] watchMessageQueues, string[] watchErrorQueues, CommandDefinition commandDef) {

      _serverName = serverName;
      _watchCommandQueues = watchCommandQueues;
      _watchEventQueues = watchEventQueues;
      _watchMessageQueues = watchMessageQueues;
      _watchErrorQueues = watchErrorQueues;

      _commandDef = commandDef;

      LoadQueues();
    }
    public virtual void Dispose() { }


    private void UpdateItems(QueueType type, bool value) {

      if( !value )
        foreach( var itm in _items.Where(i => i.QueueType == type).ToArray() )
          _items.Remove(itm);
    }

    protected abstract void LoadQueues();

    public abstract string[] GetAllAvailableQueueNames(string server);
    public abstract bool CanAccessQueue(string server, string queueName);


    IEnumerable<QueueItem> ProcessQueue(QueueType type) {

      if( IsMonitoring(type) ) {
        var fetched = FetchQueueItems(type, _items);

        return fetched;

      } else {

        return EMPTY_LIST;
      }
    }

    protected bool IsMonitoring(QueueType type) {
      switch( type ) {
        case QueueType.Command: return MonitorCommands;
        case QueueType.Event: return MonitorEvents;
        case QueueType.Message: return MonitorMessages;
        case QueueType.Error: return MonitorErrors;
      }

      return false;
    }

    protected abstract IEnumerable<QueueItem> GetProcessedQueueItems(QueueType type, DateTime since, IList<QueueItem> currentItems);


    public void LoadProcessedQueueItems(TimeSpan timeSpan) {
      if( _watchEventQueues.Length == 0 && _watchCommandQueues.Length == 0 && _watchMessageQueues.Length == 0 && _watchErrorQueues.Length == 0 )
        return;

      List<QueueItem> items = new List<QueueItem>();

      // TODO: Solve why we can not iterate thru Remote MQ, 
      // both GetMessageEnumerator2() and GetAllMessages() should be available for
      // Remote computer and direct format name, but returns zero (0) messages always
      //if( !Tools.IsLocalHost(_serverName) )
      //  return;
      DateTime since = DateTime.Now - timeSpan;

      foreach( QueueType t in Enum.GetValues(typeof(QueueType)) )
        if( IsMonitoring(t) )
          items.AddRange(GetProcessedQueueItems(t, since, _items));

      bool changed = false;
      lock( _itemsLock ) {

        // Add new items
        foreach( var itm in items )
          if( !_items.Any(i => i.Id == itm.Id) ) {
            _items.Insert(0, itm);
            changed = true;
          }

        // Mark removed as deleted messages
        foreach( var itm in _items )
          if( !items.Any(i2 => i2.Id == itm.Id) ) {

            if( !itm.Processed ) {
              itm.Processed = true;
              changed = true;
            }
          }

      }

      if( changed )
        OnItemsChanged();
    }


    public void RefreshQueueItems() {

      if( _watchEventQueues.Length == 0 && _watchCommandQueues.Length == 0 && _watchMessageQueues.Length == 0 && _watchErrorQueues.Length == 0 )
        return;

      List<QueueItem> items = new List<QueueItem>();

      // TODO: Solve why we can not iterate thru Remote MQ, 
      // both GetMessageEnumerator2() and GetAllMessages() should be available for
      // Remote computer and direct format name, but returns zero (0) messages always
      //if( !Tools.IsLocalHost(_serverName) )
      //  return;

      foreach( QueueType t in Enum.GetValues(typeof(QueueType)) )
        items.AddRange(ProcessQueue(t));

      bool changed = false;
      lock( _itemsLock ) {

        // Add new items
        foreach( var itm in items )
          if( !_items.Any(i => i.Id == itm.Id) ) {
            _items.Insert(0, itm);
            changed = true;
          }

        // Mark removed as deleted messages
        foreach( var itm in _items )
          if( !items.Any(i2 => i2.Id == itm.Id) ) {

            if( !itm.Processed ) {
              itm.Processed = true;
              changed = true;
            }
          }

      }

      if( changed )
        OnItemsChanged();


    }

    public void ClearProcessedItems() {
      foreach( var itm in _items.Where(i => i.Processed).ToArray() )
        _items.Remove(itm);
    }


    protected abstract IEnumerable<QueueItem> FetchQueueItems(QueueType type, IList<QueueItem> currentItems);

    public abstract string LoadMessageContent(QueueItem itm);

    public abstract MessageContentFormat MessageContentFormat { get; }

    public abstract bool IsIgnoredQueue(string queueName);
    public abstract bool IsIgnoredQueueItem(QueueItem itm);


    public abstract void PurgeMessage(QueueItem itm);
    public abstract void PurgeAllMessages();

    public abstract void PurgeErrorMessages(string queueName);
    public abstract void PurgeErrorAllMessages();


    public abstract void MoveErrorItemToOriginQueue(QueueItem itm);
    public abstract void MoveAllErrorItemsToOriginQueue(string errorQueue);

    public abstract string BusName { get; }
    public abstract string BusQueueType { get; }

    public event EventHandler<ErrorArgs> ErrorOccured;

    protected void OnError(string message, string stackTrace, bool fatal) {
      if( ErrorOccured != null )
        ErrorOccured(this, new ErrorArgs(message, stackTrace, fatal));
    }
    protected void OnError(string message, Exception e, bool fatal) {
      OnError(string.Format("{0}\n\r {1} ({2})", message, e.Message, e.GetType().Name), e.StackTrace, fatal);
    }

    public abstract Type[] GetAvailableCommands(string[] asmPaths);
    public abstract Type[] GetAvailableCommands(string[] asmPaths, CommandDefinition commandDef);
    public abstract void SetupBus(string[] asmPaths);
    public abstract void SendCommand(string destinationServer, string destinationQueue, object message);


    public abstract MessageSubscription[] GetMessageSubscriptions(string server);



  }

}
