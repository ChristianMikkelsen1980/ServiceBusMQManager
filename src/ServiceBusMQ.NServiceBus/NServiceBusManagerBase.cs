#region File Information
/********************************************************************
  Project: ServiceBusMQManager
  File:    NServiceBusMessageManager.cs
  Created: 2012-08-24

  Author(s):
    Daniel Halan

 (C) Copyright 2012 Ingenious Technology with Quality Sweden AB
     all rights reserved

********************************************************************/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Messaging;
using System.Text;
using System.Xml.Linq;

using ServiceBusMQ.Model;

using NServiceBus;
using NServiceBus.Tools.Management.Errors.ReturnToSourceQueue;
using ServiceBusMQ.Manager;
using System.Reflection;
using ServiceBusMQ;

namespace ServiceBusMQ.NServiceBus {

  public abstract class NServiceBusManagerBase : MessageManagerBase {


    protected string _ignoreMessageBody;

    protected List<MessageQueue> _eventQueues = new List<MessageQueue>();
    protected List<MessageQueue> _cmdQueues = new List<MessageQueue>();
    protected List<MessageQueue> _msgQueues = new List<MessageQueue>();
    protected List<MessageQueue> _errorQueues = new List<MessageQueue>();


    public NServiceBusManagerBase() {
    }

    public override void Init(string serverName, string[] commandQueues, string[] eventQueues,
                      string[] messageQueues, string[] errorQueues, CommandDefinition commandDef) {
      base.Init(serverName, commandQueues, eventQueues, messageQueues, errorQueues, commandDef);



      _ignoreMessageBody = new StreamReader(this.GetType().Assembly.GetManifestResourceStream("ServiceBusMQ.NServiceBus.CheckMessage.xml")).ReadToEnd();
    }




    public override bool IsIgnoredQueueItem(QueueItem itm) {
      return string.Compare(itm.Content, _ignoreMessageBody) == 0;
    }
    public override bool IsIgnoredQueue(string queueName) {
      return ( queueName.EndsWith(".subscriptions") || queueName.EndsWith(".retries") || queueName.EndsWith(".timeouts") );
    }

    public override void MoveErrorItemToOriginQueue(QueueItem itm) {
      if( string.IsNullOrEmpty(itm.Id) )
        throw new ArgumentException("MessageId can not be null or empty");

      if( itm.QueueType != QueueType.Error )
        throw new ArgumentException("Queue is not of type Error, " + itm.QueueType);

      var mgr = new ErrorManager();

      // TODO:
      // Check if Clustered Queue, due if Clustered && NonTransactional, then Error

      mgr.InputQueue = Address.Parse(itm.QueueName);

      mgr.ReturnMessageToSourceQueue(itm.Id);
    }
    public override void MoveAllErrorItemsToOriginQueue(string errorQueue) {
      var mgr = new ErrorManager();

      // TODO:
      // Check if Clustered Queue, due if Clustered && NonTransactional, then Error

      mgr.InputQueue = Address.Parse(errorQueue);

      mgr.ReturnAll();
    }


    protected string ReadMessageStream(Stream s) {
      using( StreamReader r = new StreamReader(s, Encoding.Default) )
        return r.ReadToEnd().Replace("\0", "");
    }


    protected override IEnumerable<QueueItem> FetchQueueItems(QueueType type, IList<QueueItem> currentItems) {
      return DoFetchQueueItems(GetQueueListByType(type), type, currentItems);
   }
    protected abstract IEnumerable<QueueItem> DoFetchQueueItems(IList<MessageQueue> queues, QueueType type, IList<QueueItem> currentItems);

    protected List<MessageQueue> GetQueueListByType(QueueType type) {

      if( type == QueueType.Command )
        return _cmdQueues;

      else if( type == QueueType.Event )
        return _eventQueues;

      else if( type == QueueType.Message )
        return _msgQueues;

      else if( type == QueueType.Error )
        return _errorQueues;

      return null;
    }


    protected string GetSubscriptionType(string xml) {
      List<string> r = new List<string>();
      try {
        XDocument doc = XDocument.Parse(xml);

        var e = doc.Root as XElement;
        return e.Value;


      } catch { }

      return string.Empty;
    }


    protected string[] GetMessageNames(string xml, bool includeNamespace) {
      List<string> r = new List<string>();
      try {
        XDocument doc = XDocument.Parse(xml);
        string ns = string.Empty;

        if( includeNamespace ) {
          ns = doc.Root.Attribute("xmlns").Value.Remove(0, 19) + ".";
        }

        foreach( XElement e in doc.Root.Elements() ) {
          r.Add(ns + e.Name.LocalName);
        }

      } catch { }

      return r.ToArray();
    }

    protected string MergeStringArray(string[] arr) {
      StringBuilder sb = new StringBuilder();
      foreach( var str in arr ) {
        if( sb.Length > 0 ) sb.Append(", ");

        sb.Append(str);
      }

      return sb.ToString();
    }


    public override Type[] GetAvailableCommands(string[] asmPaths) {
      List<Type> arr = new List<Type>();

      foreach( var path in asmPaths )
        foreach( var dll in Directory.GetFiles(path, "*.dll") ) {

          try {
            var asm = Assembly.LoadFrom(dll);

            foreach( Type t in asm.GetTypes() ) {

              if( _commandDef.IsCommand(t) ) 
                arr.Add(t);

              //if( typeof(ICommand).IsAssignableFrom(t) ) {
              //  arr.Add(t);

              //} else if( t.AssemblyQualifiedName.Contains("Commands") ) {

              //  arr.Add(t);
              //}

            }

          } catch { }

        }

      return arr.ToArray();
    }

    IBus _bus;

   

    public override void SetupBus(string[] assemblyPaths) {

      List<Assembly> asms = new List<Assembly>();

      foreach( string path in assemblyPaths ) {

        foreach( string file in Directory.GetFiles(path, "*.dll") ) {
          try {
            asms.Add(Assembly.LoadFrom(file));
          } catch { }
        }

      }



      _bus = Configure.With(asms)
                .DefineEndpointName("SBMQM_NSB")
                .DefaultBuilder()
        //.MsmqSubscriptionStorage()
          .DefiningCommandsAs(t => _commandDef.IsCommand(t))
                .XmlSerializer()
                .MsmqTransport()
                .UnicastBus()
            .SendOnly();

    }
    public override void SendCommand(string destinationServer, string destinationQueue, object message) {



      if( Tools.IsLocalHost(destinationServer) )
        destinationServer = null;

      string dest = !string.IsNullOrEmpty(destinationServer) ? destinationQueue + "@" + destinationServer : destinationQueue;


      //var assemblies = message.GetType().Assembly
      // .GetReferencedAssemblies()
      // .Select(n => Assembly.Load(n))
      // .ToList();
      //assemblies.Add(GetType().Assembly);


      if( message != null )
        _bus.Send(dest, message);
      else OnError("Can not send an incomplete message", false);

    }


  }
}
